using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New LootTable", menuName = "Loot/Loot Table")]
public class LootTable_SO : ScriptableObject
{
    [Serializable]
    public class LootDropEntry
    {
        public LootItem_SO lootItem;
        [Min(0f)] public float dropChance = 1f;
        [Min(1)] public int minAmount = 1;
        [Min(1)] public int maxAmount = 1;
    }

    [Serializable]
    public class RelicDropEntry
    {
        public Relic_SO relicData;
        [Min(0f)] public float dropChance = 1f;
        [Min(1)] public int minAmount = 1;
        [Min(1)] public int maxAmount = 1;
    }

    [Header("Table Info")]
    public int tableID;
    public string tableName;

    [TextArea(2, 4)]
    public string description;

    [Header("Drops")]
    [SerializeField] private List<LootDropEntry> drops = new List<LootDropEntry>();
    [SerializeField] private List<RelicDropEntry> relicDrops = new List<RelicDropEntry>();

    public IReadOnlyList<LootDropEntry> Drops => drops;
    public IReadOnlyList<RelicDropEntry> RelicDrops => relicDrops;

    public bool IsValid =>
        (drops != null && drops.Count > 0) ||
        (relicDrops != null && relicDrops.Count > 0);

    private void OnValidate()
    {
        if (drops == null)
        {
            drops = new List<LootDropEntry>();
        }

        for (int i = drops.Count - 1; i >= 0; i--)
        {
            LootDropEntry entry = drops[i];
            if (entry == null)
            {
                drops[i] = new LootDropEntry();
                entry = drops[i];
            }

            entry.dropChance = Mathf.Max(0f, entry.dropChance);
            entry.minAmount = Mathf.Max(1, entry.minAmount);
            entry.maxAmount = Mathf.Max(entry.minAmount, entry.maxAmount);
        }

        if (relicDrops == null)
            relicDrops = new List<RelicDropEntry>();

        for (int i = relicDrops.Count - 1; i >= 0; i--)
        {
            RelicDropEntry entry = relicDrops[i];
            if (entry == null)
            {
                relicDrops[i] = new RelicDropEntry();
                entry = relicDrops[i];
            }

            entry.dropChance = Mathf.Max(0f, entry.dropChance);
            entry.minAmount = Mathf.Max(1, entry.minAmount);
            entry.maxAmount = Mathf.Max(entry.minAmount, entry.maxAmount);
        }
    }
}
