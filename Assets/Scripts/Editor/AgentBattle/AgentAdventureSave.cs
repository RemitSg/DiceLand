using System;
using System.IO;
using System.Linq;
using UnityEngine;

[Serializable] public sealed class AgentAdventureSaveInfo
{
    public string slotId, profileId;
    public int revision;
    public long profileGeneration;
    public bool persistent, replayBacked = true;
}
[Serializable] internal sealed class AgentAdventureSave
{
    public int formatVersion = 1;
    public string profileId, owner;
    public long profileGeneration;
    public AgentAdventureProfileSnapshot finalProfile;
    public AgentAdventureReplay journal;
}

public static class AgentAdventureSaveService
{
    public static string SlotPath(string slotId)
    {
        if (string.IsNullOrWhiteSpace(slotId)) throw new ArgumentException("A save slotId is required.");
        AgentAdventureProfile.ValidateId(slotId);
        return Path.GetFullPath(Path.Combine("Library", "AgentAdventure", "Saves", slotId + ".json"));
    }

    public static AgentAdventureSession Load(string slotId, Func<long> utcNow = null)
    {
        var save = AgentAdventureAtomicFile.Read<AgentAdventureSave>(SlotPath(slotId), AgentAdventureProfileStore.SizeLimit);
        if (save.formatVersion != 1 || save.journal == null || save.finalProfile == null)
            throw new ArgumentException("Unsupported or invalid adventure save.");
        return Restore(save, utcNow);
    }

    public static AgentAdventureSession Recover(string profileId, Func<long> utcNow = null)
    {
        if (string.IsNullOrWhiteSpace(profileId)) throw new ArgumentException("A persistent profileId is required for recovery.");
        AgentAdventureProfile.ValidateId(profileId);
        var profile = new AgentAdventureProfile(AgentAdventureConfiguration.Load().abilities, profileId);
        if (!File.Exists(Path.Combine(profile.StorageDirectory, "committed.json")))
            throw new ArgumentException("This profile has no committed adventure to recover.");
        AgentAdventureProfileStore store = null;
        try
        {
            store = new AgentAdventureProfileStore(profile);
            if (store.Head.journal == null) throw new ArgumentException("This profile has no committed adventure to recover.");
            var save = new AgentAdventureSave { profileId = profileId, owner = store.Head.owner,
                profileGeneration = store.Head.generation, finalProfile = store.Head.profile, journal = store.Head.journal };
            return RestoreOwned(save, profile, store, utcNow);
        }
        catch { store?.Dispose(); profile.Dispose(); throw; }
    }

    private static AgentAdventureSession Restore(AgentAdventureSave save, Func<long> utcNow)
    {
        AgentAdventureProfile.ValidateId(save.profileId);
        if (string.IsNullOrWhiteSpace(save.profileId))
        {
            var session = AgentAdventureReplayService.Rebuild(save.journal, utcNow);
            try { session.ValidateRestoredProfile(save.finalProfile); return session; }
            catch { session.Dispose(); throw; }
        }
        var profile = new AgentAdventureProfile(AgentAdventureConfiguration.Load().abilities, save.profileId);
        if (!File.Exists(Path.Combine(profile.StorageDirectory, "committed.json")))
            throw new ArgumentException("The saved profile no longer exists.");
        AgentAdventureProfileStore store = null;
        try
        {
            store = new AgentAdventureProfileStore(profile);
            return RestoreOwned(save, profile, store, utcNow);
        }
        catch { store?.Dispose(); profile.Dispose(); throw; }
    }

    private static AgentAdventureSession RestoreOwned(AgentAdventureSave save, AgentAdventureProfile profile,
        AgentAdventureProfileStore store, Func<long> utcNow)
    {
        if (store.Head.owner != save.owner || store.Head.generation != save.profileGeneration)
            throw new ArgumentException("Stale save: this profile has a newer adventure or committed progression. Use run_recover for its latest command.");
        if (JsonUtility.ToJson(store.Head.profile) != JsonUtility.ToJson(save.finalProfile))
            throw new ArgumentException("Saved progression does not match the committed profile.");
        var session = AgentAdventureReplayService.Rebuild(save.journal, utcNow);
        try
        {
            session.ValidateRestoredProfile(save.finalProfile);
            session.AttachRestoredProfile(profile, store, save.owner);
            return session;
        }
        catch { session.Dispose(); throw; }
    }
}

public sealed partial class AgentAdventureSession
{
    public AgentAdventureSaveInfo Save(string slotId)
    {
        var save = new AgentAdventureSave { journal = Replay(), finalProfile = profile.Capture(),
            profileId = profile.Persistent ? profile.Id : "", owner = profileOwner,
            profileGeneration = profileStore?.Head.generation ?? 0 };
        AgentAdventureAtomicFile.Write(AgentAdventureSaveService.SlotPath(slotId), save, AgentAdventureProfileStore.SizeLimit);
        return new AgentAdventureSaveInfo { slotId = slotId, revision = revision, profileId = save.profileId,
            profileGeneration = save.profileGeneration, persistent = profile.Persistent };
    }

    internal void ValidateRestoredProfile(AgentAdventureProfileSnapshot expected)
    {
        if (JsonUtility.ToJson(profile.Capture()) != JsonUtility.ToJson(expected))
            throw new ArgumentException("Replayed progression does not match the saved profile.");
    }

    internal void AttachRestoredProfile(AgentAdventureProfile restored, AgentAdventureProfileStore store, string owner)
    {
        // Replaying settlement and ability operations affected only this temporary profile.
        profile.Dispose(); profile = restored; profileStore = store; profileOwner = owner;
        foreach (string key in responses.Keys.ToArray())
        {
            var response = JsonUtility.FromJson<AgentAdventureResponse>(responses[key]);
            if (response.state?.meta != null)
            {
                response.state.meta.profileId = restored.Id;
                response.state.meta.persistent = true;
                responses[key] = JsonUtility.ToJson(response);
            }
        }
        // Branching before any progression commit is permitted. Advance the owned head to this
        // exact saved decision so crash recovery follows the restored branch immediately.
        profileStore.Commit(profileOwner, CaptureReplay(), false);
    }
}
