using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

[Serializable] public sealed class AgentAdventureProfileSnapshot
{
    public int points;
    public string progressionJson;
}
[Serializable] public sealed class AgentAdventureReplayCommand
{
    public long utcSeconds;
    public AgentAdventureRequest request;
    public string stateHash;
}
[Serializable] public sealed class AgentAdventureReplay
{
    public int formatVersion = 1;
    public string apiVersion = "1", engineVersion, contentHash, initialStateHash, finalStateHash;
    public long startedUtcSeconds;
    public AgentAdventureRequest create;
    public AgentAdventureProfileSnapshot initialProfile;
    public AgentAdventureReplayCommand[] commands;
}
[Serializable] public sealed class AgentAdventureReplayVerification
{
    public bool verified;
    public int commandsChecked;
    public string finalStateHash;
    public AgentAdventureObservation finalState;
}

// A journal of calls to the same adapter, not a second rules engine or a serialized Unity scene.
public static class AgentAdventureReplayService
{
    private const int FileLimit = 16 * 1024 * 1024;

    public static string StateHash(AgentAdventureObservation state)
    {
        var copy = Clone(state);
        // These are transport/storage identities, regenerated for every isolated replay.
        copy.sessionId = null;
        if (copy.battle != null) copy.battle.sessionId = null;
        if (copy.meta != null) { copy.meta.profileId = ""; copy.meta.persistent = false; }
        return Hash(Encoding.UTF8.GetBytes(JsonUtility.ToJson(copy)));
    }

    public static string ContentHash()
    {
        // Saved gameplay definitions, code and referenced scene configuration. Include meta files
        // because asset identity is part of the protocol. No timestamps or Library import artifacts.
        var paths = new[] { "Assets/Data", "Assets/Resources", "Assets/Configs", "Assets/Scripts" }
            .Where(Directory.Exists).SelectMany(p => Directory.EnumerateFiles(p, "*", SearchOption.AllDirectories))
            .Where(p => new[] { ".cs", ".asset", ".meta", ".json", ".asmdef", ".txt", ".unity", ".scene", ".anim", ".controller" }
                .Contains(Path.GetExtension(p).ToLowerInvariant()))
            .Concat(new[] { "Assets/MapScene.scene", "Assets/MapScene.scene.meta", "Assets/Scenes/RestScene.scene", "Assets/Scenes/RestScene.scene.meta",
                "Assets/Scenes/Menu Selection.unity", "Assets/Scenes/Menu Selection.unity.meta",
                "Packages/manifest.json", "Packages/packages-lock.json", "ProjectSettings/ProjectVersion.txt" }.Where(File.Exists))
            .Select(p => p.Replace('\\', '/')).Distinct().OrderBy(p => p, StringComparer.Ordinal);
        var input = new StringBuilder();
        foreach (string path in paths)
            input.Append(path).Append(':').Append(Hash(File.ReadAllBytes(path))).Append('\n');
        return Hash(Encoding.UTF8.GetBytes(input.ToString()));
    }

    public static AgentAdventureReplayVerification Verify(AgentAdventureReplay replay)
    {
        using (var session = Rebuild(replay))
        {
            var state = session.Observe();
            return new AgentAdventureReplayVerification { verified = true, commandsChecked = replay.commands.Length,
                finalStateHash = StateHash(state), finalState = state };
        }
    }

    internal static AgentAdventureSession Rebuild(AgentAdventureReplay replay, Func<long> liveClock = null)
    {
        if (replay == null || replay.formatVersion != 1 || replay.apiVersion != "1")
            throw new ArgumentException("Unsupported adventure replay version.");
        if (replay.engineVersion != Application.unityVersion || replay.contentHash != ContentHash())
            throw new ArgumentException("Replay engine/content version does not match this host.");
        if (replay.create == null || replay.create.op != "create_run" || !string.IsNullOrEmpty(replay.create.profileId)
            || replay.initialProfile == null || replay.commands == null || replay.commands.Length > 10000)
            throw new ArgumentException("Invalid replay header or command count.");
        long now = replay.startedUtcSeconds;
        bool live = false;
        Func<long> realTime = liveClock ?? (() => DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        // The journal cannot select a persistent profile, even if a caller edits its JSON.
        var session = new AgentAdventureSession(Clone(replay.create), () => live ? realTime() : now, Clone(replay.initialProfile), false);
        try
        {
            Check(session.Observe(), replay.initialStateHash, 0);
            for (int i = 0; i < replay.commands.Length; i++)
            {
                AgentAdventureReplayCommand entry = replay.commands[i];
                if (entry?.request == null || entry.request.revision != i || string.IsNullOrWhiteSpace(entry.request.requestId))
                    throw new ArgumentException("Invalid replay command at revision " + (i + 1) + ".");
                now = entry.utcSeconds;
                var request = Clone(entry.request); request.sessionId = session.Id;
                try { Check(session.Act(request).state, entry.stateHash, i + 1); }
                catch (Exception e) { throw new ArgumentException("Replay failed at revision " + (i + 1) + ": " + e.Message, e); }
            }
            var state = session.Observe();
            Check(state, replay.finalStateHash, replay.commands.Length);
            session.AdoptReplay(replay);
            live = true;
            return session;
        }
        catch { session.Dispose(); throw; }
    }

    private static void Check(AgentAdventureObservation state, string expected, int revision)
    {
        if (StateHash(state) != expected) throw new ArgumentException("Replay state mismatch at revision " + revision + ".");
    }

    public static void Save(string slotId, AgentAdventureReplay replay)
    {
        AgentAdventureAtomicFile.Write(SlotPath(slotId), replay, FileLimit);
    }

    public static AgentAdventureReplay Load(string slotId)
    {
        return AgentAdventureAtomicFile.Read<AgentAdventureReplay>(SlotPath(slotId), FileLimit);
    }

    public static string SlotPath(string slotId)
    {
        if (string.IsNullOrWhiteSpace(slotId)) throw new ArgumentException("A replay slotId is required.");
        AgentAdventureProfile.ValidateId(slotId);
        return Path.GetFullPath(Path.Combine("Library", "AgentAdventure", "Replays", slotId + ".json"));
    }

    internal static T Clone<T>(T value) => JsonUtility.FromJson<T>(JsonUtility.ToJson(value));
    private static string Hash(byte[] input)
    {
        using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(input)).Replace("-", "").ToLowerInvariant();
    }
}
