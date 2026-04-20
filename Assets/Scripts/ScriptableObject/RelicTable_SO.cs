using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New RelicTable", menuName = "Relics/Relic Table")]
public class RelicTable_SO : ScriptableObject
{
    [Serializable]
    public struct RarityWeightEntry
    {
        public LootRarity rarity;
        [Min(0f)] public float weight;
    }

    [Header("Identity")]
    public int tableID;
    public string tableName;

    [TextArea(2, 4)]
    public string description;

    [Header("Relics")]
    [Tooltip("All relics that can be rolled from this table.")]
    [SerializeField] private List<Relic_SO> relics = new List<Relic_SO>();

    [Header("Rarity Weights")]
    [Tooltip("All relics with the same rarity share the same selection weight.")]
    [SerializeField] private List<RarityWeightEntry> rarityWeights = new List<RarityWeightEntry>();

    public IReadOnlyList<Relic_SO> Relics => relics;
    public IReadOnlyList<RarityWeightEntry> RarityWeights => rarityWeights;

    public bool IsValid
    {
        get
        {
            if (relics == null)
                return false;

            for (int i = 0; i < relics.Count; i++)
            {
                if (relics[i] != null)
                    return true;
            }

            return false;
        }
    }

    public Relic_SO DrawRandomRelic(System.Random rng, ICollection<Relic_SO> excludedRelics = null)
    {
        if (rng == null)
            rng = new System.Random();
        if (relics == null || relics.Count == 0)
            return null;
        if (rarityWeights == null)
            rarityWeights = new List<RarityWeightEntry>();

        List<Relic_SO> validRelics = new List<Relic_SO>();
        Dictionary<LootRarity, List<Relic_SO>> rarityBuckets = new Dictionary<LootRarity, List<Relic_SO>>();
        for (int i = 0; i < relics.Count; i++)
        {
            Relic_SO relic = relics[i];
            if (relic == null)
                continue;
            if (excludedRelics != null && excludedRelics.Contains(relic))
                continue;

            validRelics.Add(relic);
            if (!rarityBuckets.TryGetValue(relic.rarity, out List<Relic_SO> bucket))
            {
                bucket = new List<Relic_SO>();
                rarityBuckets.Add(relic.rarity, bucket);
            }

            bucket.Add(relic);
        }

        if (validRelics.Count == 0)
            return null;

        float totalWeight = 0f;
        List<LootRarity> weightedRarities = new List<LootRarity>();
        List<float> cumulativeWeights = new List<float>();
        for (int rarityIndex = 0; rarityIndex <= (int)LootRarity.Legendary; rarityIndex++)
        {
            LootRarity rarity = (LootRarity)rarityIndex;
            float weight = Mathf.Max(0f, GetRarityWeight(rarity));
            if (weight <= 0f)
                continue;

            totalWeight += weight;
            weightedRarities.Add(rarity);
            cumulativeWeights.Add(totalWeight);
        }

        if (totalWeight <= 0f)
            return validRelics[rng.Next(validRelics.Count)];

        double roll = rng.NextDouble() * totalWeight;
        LootRarity selectedRarity = weightedRarities[weightedRarities.Count - 1];
        for (int i = 0; i < cumulativeWeights.Count; i++)
        {
            if (roll <= cumulativeWeights[i])
            {
                selectedRarity = weightedRarities[i];
                break;
            }
        }

        List<Relic_SO> selectedBucket = ResolveBucketWithRarityFallback(rarityBuckets, selectedRarity);
        if (selectedBucket == null || selectedBucket.Count == 0)
            return null;

        return selectedBucket[rng.Next(selectedBucket.Count)];
    }

    private static List<Relic_SO> ResolveBucketWithRarityFallback(
        Dictionary<LootRarity, List<Relic_SO>> rarityBuckets,
        LootRarity selectedRarity)
    {
        if (TryGetNonEmptyBucket(rarityBuckets, selectedRarity, out List<Relic_SO> directBucket))
            return directBucket;

        for (int rarityIndex = (int)selectedRarity - 1; rarityIndex >= (int)LootRarity.Common; rarityIndex--)
        {
            if (TryGetNonEmptyBucket(rarityBuckets, (LootRarity)rarityIndex, out List<Relic_SO> lowerBucket))
                return lowerBucket;
        }

        for (int rarityIndex = (int)selectedRarity + 1; rarityIndex <= (int)LootRarity.Legendary; rarityIndex++)
        {
            if (TryGetNonEmptyBucket(rarityBuckets, (LootRarity)rarityIndex, out List<Relic_SO> higherBucket))
                return higherBucket;
        }

        return null;
    }

    private static bool TryGetNonEmptyBucket(
        Dictionary<LootRarity, List<Relic_SO>> rarityBuckets,
        LootRarity rarity,
        out List<Relic_SO> bucket)
    {
        bucket = null;
        if (rarityBuckets == null)
            return false;

        if (!rarityBuckets.TryGetValue(rarity, out List<Relic_SO> foundBucket) || foundBucket == null || foundBucket.Count == 0)
            return false;

        bucket = foundBucket;
        return true;
    }

    private float GetRarityWeight(LootRarity rarity)
    {
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
        if (relics == null)
            relics = new List<Relic_SO>();
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
            int existingIndex = FindRarityWeightIndex(rarity);
            if (existingIndex >= 0)
                continue;

            rarityWeights.Add(new RarityWeightEntry
            {
                rarity = rarity,
                weight = 1f
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
