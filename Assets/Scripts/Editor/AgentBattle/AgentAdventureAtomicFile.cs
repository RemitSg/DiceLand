using System;
using System.IO;
using System.Text;
using System.Security.Cryptography;
using System.Threading;
using UnityEngine;

// One replace is the commit point. The destination is supplied only by server-owned stores.
internal static class AgentAdventureAtomicFile
{
    internal static Action<string, string> FaultInjection;

    internal static string Digest<T>(T value)
    {
        using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(JsonUtility.ToJson(value)))).Replace("-", "").ToLowerInvariant();
    }

    internal static void Write<T>(string path, T value, int limit)
    {
        byte[] bytes = new UTF8Encoding(false).GetBytes(JsonUtility.ToJson(value));
        if (bytes.Length > limit) throw new ArgumentException("Agent save exceeds its size limit.");
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        string temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            { stream.Write(bytes, 0, bytes.Length); stream.Flush(true); }
            FaultInjection?.Invoke(path, "before_replace");
            Replace(temporary, path);
            FaultInjection?.Invoke(path, "after_replace");
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }

    private static void Replace(string temporary, string path)
    {
        for (int attempt = 0; ; attempt++)
        {
            try
            {
                FaultInjection?.Invoke(path, "replace_attempt");
                if (File.Exists(path)) File.Replace(temporary, path, null);
                else File.Move(temporary, path);
                return;
            }
            catch (IOException error)
            {
                int code = error.HResult & 0xffff;
                // Windows may briefly deny replacement while another reader scans the newly
                // written destination. Reuse the flushed temporary file; never delete the head.
                if (attempt >= 5 || (code != 32 && code != 33 && code != 1175 && code != 1176 && code != 1177)
                    || !File.Exists(temporary)) throw;
                Thread.Sleep(20 * (attempt + 1));
            }
        }
    }

    internal static T Read<T>(string path, int limit) where T : class
    {
        if (!File.Exists(path)) throw new ArgumentException("Unknown agent save.");
        if (new FileInfo(path).Length > limit) throw new ArgumentException("Agent save exceeds its size limit.");
        var value = JsonUtility.FromJson<T>(File.ReadAllText(path));
        return value ?? throw new ArgumentException("Invalid agent save.");
    }
}
