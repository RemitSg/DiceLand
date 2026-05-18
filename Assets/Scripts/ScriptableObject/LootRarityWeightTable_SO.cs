using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New LootRarityWeightTable", menuName = "Loot/Rarity Weight Table")]
public class LootRarityWeightTable_SO : ScriptableObject
{
    [Serializable]
    public struct RarityWeightEntry
    {
        public LootRarity rarity;
        [Min(0f)] public float weight;
    }

    [Header("Table Info")]
    public int tableID;
    public string tableName;

    [TextArea(2, 4)]
    public string description;

    [Header("Rarity Weights")]
    [SerializeField] private List<RarityWeightEntry> rarityWeights = new List<RarityWeightEntry>();

    public IReadOnlyList<RarityWeightEntry> RarityWeights => rarityWeights;

    public bool HasPositiveWeights
    {
        get
        {
            if (rarityWeights == null)
                return false;

            for (int i = 0; i < rarityWeights.Count; i++)
            {
                if (rarityWeights[i].weight > 0f)
                    return true;
            }

            return false;
        }
    }

    public float GetWeight(LootRarity rarity)
    {
        if (rarityWeights == null)
            return 0f;

        for (int i = 0; i < rarityWeights.Count; i++)
        {
            RarityWeightEntry entry = rarityWeights[i];
            if (entry.rarity == rarity)
                return Mathf.Max(0f, entry.weight);
        }

        return 0f;
    }

    private void OnValidate()
    {
        if (rarityWeights == null)
            rarityWeights = new List<RarityWeightEntry>();

        EnsureRarityWeightEntries();

        for (int i = 0; i < rarityWeights.Count; i++)
        {
            RarityWeightEntry entry = rarityWeights[i];
            entry.weight = Mathf.Max(0f, entry.weight);
            rarityWeights[i] = entry;
        }
    }

    private void EnsureRarityWeightEntries()
    {
        Array enumValues = Enum.GetValues(typeof(LootRarity));
        for (int i = 0; i < enumValues.Length; i++)
        {
            LootRarity rarity = (LootRarity)enumValues.GetValue(i);
            if (FindRarityWeightIndex(rarity) >= 0)
                continue;

            rarityWeights.Add(new RarityWeightEntry
            {
                rarity = rarity,
                weight = rarity == LootRarity.Trash ? 0f : 1f
            });
        }

        HashSet<LootRarity> seen = new HashSet<LootRarity>();
        for (int i = rarityWeights.Count - 1; i >= 0; i--)
        {
            RarityWeightEntry entry = rarityWeights[i];
            if (seen.Contains(entry.rarity))
            {
                rarityWeights.RemoveAt(i);
                continue;
            }

            seen.Add(entry.rarity);
        }

        rarityWeights.Sort((a, b) => a.rarity.CompareTo(b.rarity));
    }

    private int FindRarityWeightIndex(LootRarity rarity)
    {
        for (int i = 0; i < rarityWeights.Count; i++)
        {
            if (rarityWeights[i].rarity == rarity)
                return i;
        }

        return -1;
    }
}
