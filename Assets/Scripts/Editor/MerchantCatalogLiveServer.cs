using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class MerchantCatalogLiveServer
{
    private const string EnabledKey = "DiceLand.MerchantLiveEditor.Enabled";
    private const string PortKey = "DiceLand.MerchantLiveEditor.Port";
    private sealed class Pending { public HttpListenerContext context; public string body; }
    [Serializable] private sealed class Error { public string error, message; public MerchantCatalogLiveData.Document current; }
    private static readonly ConcurrentQueue<Pending> Queue = new ConcurrentQueue<Pending>();
    private static HttpListener listener;
    private static Thread worker;
    private static int pendingCount;
    private static string webRoot;
    private static string catalogFile;
    private static long observedWriteTicks;
    public static bool IsRunning => listener != null && listener.IsListening;
    public static string Url { get; private set; }

    static MerchantCatalogLiveServer()
    {
        AssemblyReloadEvents.beforeAssemblyReload += StopForReload;
        EditorApplication.quitting += StopForReload;
        if (SessionState.GetBool(EnabledKey, false)) EditorApplication.delayCall += Resume;
    }

    [MenuItem("Tools/Economy/Open Live Merchant Editor")]
    public static void Open() { Application.OpenURL(Start()); }
    [MenuItem("Tools/Economy/Stop Live Merchant Editor")]
    public static void Stop() { SessionState.SetBool(EnabledKey, false); StopForReload(); }

    public static string Start(int port = 8092)
    {
        if (IsRunning) return Url;
        if (port < 1024 || port > 65535) throw new ArgumentOutOfRangeException(nameof(port));
        webRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "../tools/merchant-catalog/dist"));
        catalogFile = Path.GetFullPath(Path.Combine(Application.dataPath, "..", MerchantCatalogLiveData.CatalogPath));
        if (!File.Exists(Path.Combine(webRoot, "index.html"))) throw new FileNotFoundException("找不到商店编辑页。", webRoot);
        var current = new HttpListener();
        Url = "http://127.0.0.1:" + port + "/";
        current.Prefixes.Add(Url);
        try { current.Start(); }
        catch { current.Close(); throw; }
        listener = current;
        observedWriteTicks = File.GetLastWriteTimeUtc(catalogFile).Ticks;
        SessionState.SetBool(EnabledKey, true);
        SessionState.SetInt(PortKey, port);
        EditorApplication.update += Pump;
        worker = new Thread(() => Accept(current)) { IsBackground = true, Name = "DiceLand Merchant Editor" };
        worker.Start();
        return Url;
    }

    private static void Resume()
    {
        if (!SessionState.GetBool(EnabledKey, false)) return;
        try { Start(SessionState.GetInt(PortKey, 8092)); }
        catch (Exception e) { Debug.LogWarning("商店实时编辑页未能恢复：" + e.Message); }
    }

    private static void StopForReload()
    {
        EditorApplication.update -= Pump;
        var previous = listener;
        listener = null;
        previous?.Close();
        if (worker != null && worker.IsAlive) worker.Join(250);
        worker = null;
        while (Queue.TryDequeue(out Pending pending))
        {
            Interlocked.Decrement(ref pendingCount);
            Write(pending.context, 503, "application/json", Encoding.UTF8.GetBytes("{\"error\":\"reloading\",\"message\":\"Unity 正在重载，请稍后重试。\"}"));
        }
    }

    private static void Accept(HttpListener current)
    {
        while (current.IsListening)
        {
            HttpListenerContext context;
            try { context = current.GetContext(); }
            catch (HttpListenerException) { return; }
            catch (ObjectDisposedException) { return; }
            try
            {
                string path = context.Request.Url.AbsolutePath;
                string method = context.Request.HttpMethod;
                if (method == "GET" && (path == "/" || path == "/index.html" || path == "/app.js" || path == "/style.css"))
                {
                    string name = path == "/" ? "index.html" : path.Substring(1);
                    string mime = name.EndsWith(".js") ? "text/javascript" : name.EndsWith(".css") ? "text/css" : "text/html";
                    Write(context, 200, mime, File.ReadAllBytes(Path.Combine(webRoot, name)));
                    continue;
                }
                if (path != "/api/catalog" || (method != "GET" && method != "POST")) { Reject(context, 404, "未知地址。"); continue; }
                string body = null;
                if (method == "POST")
                {
                    string origin = context.Request.Headers["Origin"];
                    if (context.Request.Headers["X-DiceLand-Editor"] != "merchant" ||
                        (!string.IsNullOrEmpty(origin) && origin != Url.TrimEnd('/')) ||
                        !(context.Request.ContentType ?? "").StartsWith("application/json", StringComparison.OrdinalIgnoreCase))
                    { Reject(context, 403, "请从商店编辑页提交修改。"); continue; }
                    if (context.Request.ContentLength64 < 0 || context.Request.ContentLength64 > 65536) { Reject(context, 413, "修改内容过大。"); continue; }
                    using (var reader = new StreamReader(context.Request.InputStream, Encoding.UTF8)) body = reader.ReadToEnd();
                }
                if (Interlocked.Increment(ref pendingCount) > 32)
                { Interlocked.Decrement(ref pendingCount); Reject(context, 429, "请求较多，请稍后重试。"); continue; }
                Queue.Enqueue(new Pending { context = context, body = body });
            }
            catch (Exception) { Reject(context, 400, "请求无法读取。"); }
        }
    }

    private static void Pump()
    {
        if (!Queue.TryDequeue(out Pending pending)) return;
        Interlocked.Decrement(ref pendingCount);
        try
        {
            var catalog = LoadCatalog();
            MerchantCatalogLiveData.Document document;
            if (pending.body == null) document = MerchantCatalogLiveData.Read(catalog);
            else
            {
                var patch = JsonUtility.FromJson<MerchantCatalogLiveData.Patch>(pending.body);
                document = MerchantCatalogLiveData.Apply(catalog, patch);
                observedWriteTicks = File.GetLastWriteTimeUtc(catalogFile).Ticks;
            }
            Write(pending.context, 200, "application/json", Encoding.UTF8.GetBytes(JsonUtility.ToJson(document)));
        }
        catch (MerchantCatalogLiveData.ConflictException e)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<MerchantCatalog_SO>(MerchantCatalogLiveData.CatalogPath);
            Write(pending.context, 409, "application/json", Encoding.UTF8.GetBytes(JsonUtility.ToJson(new Error
            { error = "conflict", message = e.Message, current = catalog != null ? MerchantCatalogLiveData.Read(catalog) : null })));
        }
        catch (ArgumentException e) { Reject(pending.context, 400, e.Message); }
        catch (Exception e) { Reject(pending.context, 503, e.Message); }
    }

    private static MerchantCatalog_SO LoadCatalog()
    {
        string path = MerchantCatalogLiveData.CatalogPath;
        var catalog = AssetDatabase.LoadAssetAtPath<MerchantCatalog_SO>(path);
        long ticks = File.GetLastWriteTimeUtc(catalogFile).Ticks;
        if (observedWriteTicks != ticks)
        {
            if (catalog != null && EditorUtility.IsDirty(catalog))
                throw new InvalidOperationException("磁盘配置与 Unity 未保存的修改同时变化，请先在 Unity 处理并保存。");
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            catalog = AssetDatabase.LoadAssetAtPath<MerchantCatalog_SO>(path);
            observedWriteTicks = ticks;
        }
        if (catalog == null) throw new FileNotFoundException("找不到商店配置资产。", path);
        return catalog;
    }

    private static void Reject(HttpListenerContext context, int status, string message)
    {
        // Serialize without Unity APIs on the HTTP thread.
        string escaped = (message ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n").Replace("\t", "\\t");
        Write(context, status, "application/json", Encoding.UTF8.GetBytes("{\"error\":\"request_error\",\"message\":\"" + escaped + "\"}"));
    }

    private static void Write(HttpListenerContext context, int status, string mime, byte[] bytes)
    {
        try
        {
            context.Response.StatusCode = status;
            context.Response.ContentType = mime + "; charset=utf-8";
            context.Response.Headers["Cache-Control"] = "no-store";
            context.Response.Headers["X-Content-Type-Options"] = "nosniff";
            context.Response.ContentLength64 = bytes.Length;
            context.Response.OutputStream.Write(bytes, 0, bytes.Length);
            context.Response.Close();
        }
        catch (Exception) { try { context.Response.Abort(); } catch (Exception) { } }
    }
}
