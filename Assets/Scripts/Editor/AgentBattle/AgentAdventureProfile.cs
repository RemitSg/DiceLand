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
