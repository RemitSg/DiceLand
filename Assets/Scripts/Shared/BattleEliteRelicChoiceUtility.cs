using System.Collections.Generic;
using UnityEngine;

public static class BattleEliteRelicChoiceUtility
{
    public static float GetDefaultRarityWeight(LootRarity rarity)
    {
        switch (rarity)
        {
            case LootRarity.Uncommon:
                return 10f;
            case LootRarity.Rare:
                return 5f;
            case LootRarity.Epic:
                return 2f;
            case LootRarity.Legendary:
                return 1f;
            case LootRarity.Common:
            default:
                return 20f;
        }
    }

    public static LootRarity SelectFallbackRarity(
        Dictionary<LootRarity, List<Relic_SO>> rarityBuckets,
        System.Random rng)
    {
        float totalWeight = 0f;
        List<LootRarity> rarities = new List<LootRarity>();
        List<float> cumulativeWeights = new List<float>();
        for (int rarityIndex = (int)LootRarity.Common; rarityIndex <= (int)LootRarity.Legendary; rarityIndex++)
        {
            LootRarity rarity = (LootRarity)rarityIndex;
            if (!rarityBuckets.TryGetValue(rarity, out List<Relic_SO> bucket) || bucket == null || bucket.Count == 0)
                continue;

            totalWeight += GetDefaultRarityWeight(rarity);
            rarities.Add(rarity);
            cumulativeWeights.Add(totalWeight);
        }

        if (rarities.Count == 0)
            return LootRarity.Common;

        double roll = (rng ?? new System.Random()).NextDouble() * totalWeight;
        for (int i = 0; i < cumulativeWeights.Count; i++)
        {
            if (roll <= cumulativeWeights[i])
                return rarities[i];
        }

        return rarities[rarities.Count - 1];
    }

    public static bool TryGetBucketForChoice(
        Dictionary<LootRarity, List<Relic_SO>> rarityBuckets,
        LootRarity selectedRarity,
        int desiredCount,
        out List<Relic_SO> bucket)
    {
        bucket = null;
        desiredCount = Mathf.Max(1, desiredCount);

        if (TryGetNonEmptyBucket(rarityBuckets, selectedRarity, desiredCount, out bucket))
            return true;

        for (int rarityIndex = (int)selectedRarity - 1; rarityIndex >= (int)LootRarity.Common; rarityIndex--)
        {
            if (TryGetNonEmptyBucket(rarityBuckets, (LootRarity)rarityIndex, desiredCount, out bucket))
                return true;
        }

        for (int rarityIndex = (int)selectedRarity + 1; rarityIndex <= (int)LootRarity.Legendary; rarityIndex++)
        {
            if (TryGetNonEmptyBucket(rarityBuckets, (LootRarity)rarityIndex, desiredCount, out bucket))
                return true;
        }

        return false;
    }

    public static bool TryGetNonEmptyBucket(
        Dictionary<LootRarity, List<Relic_SO>> rarityBuckets,
        LootRarity rarity,
        int desiredCount,
        out List<Relic_SO> bucket)
    {
        bucket = null;
        if (rarityBuckets == null)
            return false;

        if (!rarityBuckets.TryGetValue(rarity, out List<Relic_SO> foundBucket) || foundBucket == null || foundBucket.Count < desiredCount)
            return false;

        bucket = foundBucket;
        return true;
    }
}
