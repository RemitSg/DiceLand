using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Collection history belongs to the player, across individual adventures.</summary>
public sealed class EncyclopediaProgress
{
    private const string SaveKey = "diceland.encyclopedia.v1";
    private static EncyclopediaProgress current;
    private readonly string saveKey;
    private readonly Dictionary<string, string> idsByKey = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> unlocked = new HashSet<string>(StringComparer.Ordinal);
    private bool dirty;

    [Serializable]
    private sealed class SaveData
    {
        public List<string> unlockedIds;
    }

    public static EncyclopediaProgress Current
    {
        get
        {
            if (current == null)
            {
                var identities = Resources.Load<TextAsset>(EncyclopediaCatalog.IdentityPath);
                current = new EncyclopediaProgress(identities != null ? identities.text : string.Empty, SaveKey);
            }
            return current;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset() => current = null;

    public EncyclopediaProgress(string identityIndex, string saveKey)
    {
        this.saveKey = saveKey;
        foreach (string line in identityIndex.Split('\n'))
        {
            int tab = line.IndexOf('\t');
            if (tab > 0)
                idsByKey[line.Substring(0, tab)] = line.Substring(tab + 1).TrimEnd('\r');
        }

        string json = PlayerPrefs.GetString(saveKey, string.Empty);
        if (string.IsNullOrEmpty(json))
            return;

        try
        {
            SaveData data = JsonUtility.FromJson<SaveData>(json);
            if (data?.unlockedIds != null)
                unlocked.UnionWith(data.unlockedIds);
        }
        catch (ArgumentException ex)
        {
            Debug.LogWarning($"[Encyclopedia] Failed to read collection: {ex.Message}");
        }
    }

    public bool IsUnlocked(EncyclopediaCatalog.Entry entry) => entry != null && unlocked.Contains(entry.Id);

    public void Record(ScriptableObject asset)
    {
        Add(asset);
        SaveChanges();
    }

    public void Record(IEnumerable<ScriptableObject> assets)
    {
        foreach (ScriptableObject asset in assets)
            Add(asset);
        SaveChanges();
    }

    public void RecordOwned(RunState state)
    {
        if (state == null)
            return;

        if (state.partyMembers != null)
            foreach (RunPartyMemberState member in state.partyMembers)
                Add(member?.weaponData);
        if (state.weaponInventory != null)
            foreach (RunWeaponState item in state.weaponInventory)
                if (item != null && item.amount > 0)
                    Add(item.weaponData);
        if (state.relicInventory != null)
            foreach (RunRelicState item in state.relicInventory)
                if (item != null && item.amount > 0)
                    Add(item.relicData);
        if (state.lootInventory != null)
            foreach (RunLootState item in state.lootInventory)
                if (item != null && item.amount > 0)
                    Add(item.lootItem);
        if (state.activeCalamities != null)
            foreach (RunCalamityState item in state.activeCalamities)
                Add(item?.calamityData);

        SaveChanges();
    }

    private void Add(ScriptableObject asset)
    {
        string key = EncyclopediaCatalog.AssetKey(asset);
        if (key != null && idsByKey.TryGetValue(key, out string id) && unlocked.Add(id))
            dirty = true;
    }

    private void SaveChanges()
    {
        if (!dirty)
            return;

        try
        {
            var ids = new List<string>(unlocked);
            ids.Sort(StringComparer.Ordinal);
            PlayerPrefs.SetString(saveKey, JsonUtility.ToJson(new SaveData { unlockedIds = ids }));
            PlayerPrefs.Save();
            dirty = false;
        }
        catch (PlayerPrefsException ex)
        {
            Debug.LogWarning($"[Encyclopedia] Failed to save collection: {ex.Message}");
        }
    }
}
