using System;
using System.IO;
using UnityEngine;

[Serializable] internal sealed class AgentAdventureProfileHead
{
    public int formatVersion = 1;
    public long generation;
    public string owner, integrity;
    public AgentAdventureProfileSnapshot profile;
    public AgentAdventureReplay journal;
}

// Only storage/transaction boundaries live here. Wallet and ability rules stay in their formal services.
internal sealed class AgentAdventureProfileStore : IDisposable
{
    internal const int SizeLimit = 32 * 1024 * 1024;
    private readonly AgentAdventureProfile profile;
    private readonly FileStream lease;
    private readonly string path;
    internal AgentAdventureProfileHead Head { get; private set; }

    internal AgentAdventureProfileStore(AgentAdventureProfile profile)
    {
        if (!profile.Persistent) throw new ArgumentException("A persistent agent profile is required.");
        this.profile = profile;
        Directory.CreateDirectory(profile.StorageDirectory);
        path = Path.Combine(profile.StorageDirectory, "committed.json");
        // A second host cannot mutate this profile while a session/recovery owns it.
        lease = new FileStream(Path.Combine(profile.StorageDirectory, "lease.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        try
        {
            if (File.Exists(path))
            {
                Head = AgentAdventureAtomicFile.Read<AgentAdventureProfileHead>(path, SizeLimit);
                if (Head.formatVersion != 1 || Head.profile == null || Head.generation < 0 || Head.integrity != Integrity(Head))
                    throw new ArgumentException("Unsupported or invalid committed profile.");
                // These two formal-service files are working copies. Roll back any interrupted
                // multi-file mutation to the last single-file commit before reading gameplay bonuses.
                profile.RestoreSnapshot(Head.profile);
            }
            else
            {
                Head = new AgentAdventureProfileHead { profile = profile.Capture() };
                Head.integrity = Integrity(Head);
                AgentAdventureAtomicFile.Write(path, Head, SizeLimit);
            }
        }
        catch { lease.Dispose(); throw; }
    }

    internal void Commit(string owner, AgentAdventureReplay journal, bool advancesProgression)
    {
        var next = new AgentAdventureProfileHead { generation = checked(Head.generation + (advancesProgression ? 1 : 0)),
            owner = owner, profile = profile.Capture(), journal = journal };
        next.integrity = Integrity(next);
        AgentAdventureAtomicFile.Write(path, next, SizeLimit);
        Head = next;
    }

    public void Dispose() => lease.Dispose();

    private static string Integrity(AgentAdventureProfileHead head)
    {
        var copy = AgentAdventureReplayService.Clone(head); copy.integrity = "";
        return AgentAdventureAtomicFile.Digest(copy);
    }
}
