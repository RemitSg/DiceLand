using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using UnityEditor;
using UnityEngine;

// Transport only. All requests execute on Unity's main thread, through AgentBattleSession.
[InitializeOnLoad]
public static class AgentBattleServer
{
    private sealed class Pending { public HttpListenerContext context; public string body; }
    private static readonly ConcurrentQueue<Pending> queue = new ConcurrentQueue<Pending>();
    private static HttpListener listener;
    private static Thread worker;
    private static AgentBattleSession session;
    private static int pendingCount;
    public static bool IsRunning => listener != null && listener.IsListening;

    [MenuItem("Tools/Agent Battle/Start HTTP Server")]
    private static void StartFromMenu() { Debug.Log(IsRunning ? "Agent battle server is already running." : Start()); }
    [MenuItem("Tools/Agent Battle/Stop HTTP Server")]
    private static void StopFromMenu() { Stop(); }

    static AgentBattleServer()
    {
        AssemblyReloadEvents.beforeAssemblyReload += Stop;
        EditorApplication.quitting += Stop;
        EditorApplication.playModeStateChanged += state => { if (state == PlayModeStateChange.ExitingEditMode) Stop(); };
    }

    // Invoke via MCP execute_code or -batchmode -nographics -executeMethod AgentBattleServer.RunBatch.
    public static string Start(int port = 8091)
    {
        if (IsRunning) throw new InvalidOperationException("Agent battle server is already running.");
        if (port < 1024 || port > 65535) throw new ArgumentOutOfRangeException(nameof(port));
        listener = new HttpListener();
        listener.Prefixes.Add("http://127.0.0.1:" + port + "/");
        try { listener.Start(); }
        catch { listener.Close(); listener = null; throw; }
        worker = new Thread(Accept) { IsBackground = true, Name = "DiceLand Agent API" };
        worker.Start();
        EditorApplication.update += Pump;
        return "http://127.0.0.1:" + port + "/v1/command";
    }

    public static void RunBatch()
    {
        int port = 8091;
        string[] args = Environment.GetCommandLineArgs();
        for (int i = 0; i + 1 < args.Length; i++)
            if (args[i] == "-agentBattlePort") port = int.Parse(args[i + 1]);
        Debug.Log("DiceLand agent API started: " + Start(port));
    }

    public static void Stop()
    {
        EditorApplication.update -= Pump;
        HttpListener previous = listener;
        listener = null;
        previous?.Close();
        if (worker != null && worker.IsAlive) worker.Join(1000);
        worker = null;
        while (queue.TryDequeue(out Pending pending))
        {
            Interlocked.Decrement(ref pendingCount);
            Write(pending.context, 503, "{\"success\":false,\"error\":\"server_stopped\"}");
        }
        session?.Dispose(); session = null;
    }

    public static string Dispatch(string json)
    {
        AgentBattleRequest request = new AgentBattleRequest();
        try
        {
            JsonUtility.FromJsonOverwrite(json, request);
            AgentBattleResponse response;
            switch (request.op)
            {
                case "health":
                    response = new AgentBattleResponse { success = true, message = "editor-backed single-session battle API; no graphics; engine required; snapshot/restore and map simulation not yet supported" }; break;
                case "catalog":
                    response = new AgentBattleResponse { success = true, catalogue = AgentBattleSession.Catalogue() }; break;
                case "create":
                    if (session != null) throw new ArgumentException("Close the existing session before creating another.");
                    session = new AgentBattleSession(request);
                    response = new AgentBattleResponse { success = true, state = session.Observe() }; break;
                case "observe":
                    RequireSession(request.sessionId);
                    response = new AgentBattleResponse { success = true, state = session.Observe() }; break;
                case "replay":
                    RequireSession(request.sessionId);
                    response = new AgentBattleResponse { success = true, replay = session.Replay() }; break;
                case "close":
                    RequireSession(request.sessionId); session.Dispose(); session = null;
                    response = new AgentBattleResponse { success = true }; break;
                case "set_skill": case "target": case "commit": case "choose_relic": case "refresh_rewards":
                    RequireSession(request.sessionId); response = session.Act(request); break;
                default: throw new ArgumentException("Unknown op. Use health, catalog, create, observe, set_skill, target, commit, choose_relic, refresh_rewards, replay or close.");
            }
            return JsonUtility.ToJson(response);
        }
        catch (ArgumentException exception) { return JsonUtility.ToJson(new AgentBattleResponse { success = false, error = "invalid_request", message = exception.Message }); }
        catch (Exception exception)
        {
            return JsonUtility.ToJson(new AgentBattleResponse { success = false, error = "execution_error", message = exception.Message });
        }
    }

    private static void RequireSession(string id)
    {
        if (session == null || session.Id != id) throw new ArgumentException("Unknown sessionId.");
    }

    private static void Accept()
    {
        HttpListener current = listener;
        while (current.IsListening)
        {
            HttpListenerContext context;
            try { context = current.GetContext(); }
            catch (HttpListenerException) { return; }
            catch (ObjectDisposedException) { return; }
            try
            {
                if (context.Request.HttpMethod != "POST" || context.Request.Url.AbsolutePath != "/v1/command")
                { Write(context, 404, "{\"success\":false,\"error\":\"use_POST_v1_command\"}"); continue; }
                if (context.Request.ContentLength64 < 0 || context.Request.ContentLength64 > 65536)
                { Write(context, 413, "{\"success\":false,\"error\":\"body_limit\"}"); continue; }
                string body;
                using (var reader = new StreamReader(context.Request.InputStream, Encoding.UTF8)) body = reader.ReadToEnd();
                if (Interlocked.Increment(ref pendingCount) > 32)
                { Interlocked.Decrement(ref pendingCount); Write(context, 429, "{\"success\":false,\"error\":\"queue_full\"}"); continue; }
                queue.Enqueue(new Pending { context = context, body = body });
            }
            catch (Exception) { Write(context, 400, "{\"success\":false,\"error\":\"invalid_http_request\"}"); }
        }
    }

    private static void Pump()
    {
        if (!queue.TryDequeue(out Pending pending)) return;
        Interlocked.Decrement(ref pendingCount);
        string result = Dispatch(pending.body);
        AgentBattleResponse response = JsonUtility.FromJson<AgentBattleResponse>(result);
        Write(pending.context, response.success ? 200 : (response.error == "invalid_request" ? 400 : 500), result);
    }

    private static void Write(HttpListenerContext context, int status, string body)
    {
        try
        {
            byte[] bytes = Encoding.UTF8.GetBytes(body);
            context.Response.StatusCode = status;
            context.Response.ContentType = "application/json; charset=utf-8";
            context.Response.ContentLength64 = bytes.Length;
            context.Response.OutputStream.Write(bytes, 0, bytes.Length);
            context.Response.Close();
        }
        catch (Exception) { try { context.Response.Abort(); } catch (Exception) { } }
    }
}
