using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

// Storage adapter only: the formal wallet/progression service owns costs and bonuses.
public sealed class AgentAdventureProfile : IDisposable
{
    public string Id { get; }
    public bool Persistent { get; }
    public AbilityPointWallet Wallet { get; }
    public AbilityProgressionService Progression { get; }
    private readonly AbilityTreeConfig_SO config;
    private readonly string directory;
    internal string StorageDirectory => directory;

    public AgentAdventureProfile(AbilityTreeConfig_SO config, string profileId)
    {
        ValidateId(profileId);
        Persistent = !string.IsNullOrWhiteSpace(profileId);
        Id = Persistent ? profileId : Guid.NewGuid().ToString("N");
        this.config = config;
        directory = Path.GetFullPath(Path.Combine("Library", "AgentAdventure", Persistent ? "Profiles" : "Sessions", Id));
        Wallet = new AbilityPointWallet(Path.Combine(directory, "ability_points.json"));
        Progression = new AbilityProgressionService(config, Wallet, Path.Combine(directory, "ability_progression.json"));
    }

    public static void ValidateId(string id)
    {
        if (!string.IsNullOrWhiteSpace(id) && (!Regex.IsMatch(id, "\\A[A-Za-z0-9_-]{1,64}\\z")
            || Regex.IsMatch(id, "\\A(CON|PRN|AUX|NUL|COM[0-9]|LPT[0-9])\\z", RegexOptions.IgnoreCase)))
            throw new ArgumentException("profileId must be 1-64 letters, digits, underscores or hyphens, excluding Windows device names.");
    }

    public int AddPoints(int amount)
    {
        int result = Wallet.AddPoints(amount);
        if (Wallet.GetCurrentPoints() != result) throw new IOException("The isolated ability wallet could not be persisted.");
        return result;
    }

    internal AgentAdventureProfileSnapshot Capture()
    {
        string progressionJson = File.Exists(Progression.FilePath) ? File.ReadAllText(Progression.FilePath) : null;
        if ((File.Exists(Wallet.FilePath) && !AbilityPointWallet.IsValidSaveJson(File.ReadAllText(Wallet.FilePath)))
            || (progressionJson != null && !AbilityProgressionService.IsValidSaveJson(progressionJson)))
            throw new IOException("The isolated profile contains an invalid or unsupported save; it was not imported as zero progress.");
        return new AgentAdventureProfileSnapshot { points = Wallet.GetCurrentPoints(), progressionJson = progressionJson };
    }

    internal void InitializeReplay(AgentAdventureProfileSnapshot snapshot)
    {
        if (Persistent) throw new InvalidOperationException("Replay initialization requires a temporary profile.");
        RestoreSnapshot(snapshot);
    }

    internal void RestoreSnapshot(AgentAdventureProfileSnapshot snapshot)
    {
        if (snapshot == null || snapshot.points < 0 || (snapshot.progressionJson?.Length ?? 0) > 131072
            || (!string.IsNullOrWhiteSpace(snapshot.progressionJson) && !AbilityProgressionService.IsValidSaveJson(snapshot.progressionJson)))
            throw new ArgumentException("Invalid initial replay profile.");
        Wallet.SetPoints(snapshot.points);
        if (Wallet.GetCurrentPoints() != snapshot.points) throw new IOException("Replay wallet could not be initialized.");
        if (!string.IsNullOrWhiteSpace(snapshot.progressionJson))
        {
            // Only the existing progression service interprets its saved data; replay hashes verify the result.
            File.WriteAllText(Progression.FilePath, snapshot.progressionJson);
        }
        else if (File.Exists(Progression.FilePath)) File.Delete(Progression.FilePath);
    }

    public AgentAdventureMeta Describe()
    {
        return new AgentAdventureMeta
        {
            profileId = Persistent ? Id : "", persistent = Persistent, abilityPoints = Wallet.GetCurrentPoints(),
            nodes = (config?.nodes ?? new System.Collections.Generic.List<AbilityNodeDefinition>()).Where(n => n != null).Select(n => new AgentAdventureAbility
            {
                id = n.nodeId, name = n.displayName, description = n.description, cost = n.cost,
                prerequisites = n.prerequisiteNodeIds?.ToArray() ?? Array.Empty<string>(),
                state = Progression.GetNodeState(n.nodeId).ToString(), toggleable = n.isToggleable,
                enabled = Progression.IsNodeEnabled(n.nodeId)
            }).ToArray()
        };
    }

    public void Dispose()
    {
        if (Persistent) return;
        // Exact files in this adapter's generated directory only; never recurse or use player defaults.
        if (File.Exists(Wallet.FilePath)) File.Delete(Wallet.FilePath);
        if (File.Exists(Progression.FilePath)) File.Delete(Progression.FilePath);
        if (Directory.Exists(directory) && !Directory.EnumerateFileSystemEntries(directory).Any()) Directory.Delete(directory, false);
    }
}
