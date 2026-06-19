using System.Collections.Generic;
using UnityEngine;

public static class BlueprintRewardRollService
{
    public static CraftingBlueprint_SO DrawRandomBlueprint(
        BlueprintTable_SO blueprintTable,
        LootRarityWeightTable_SO rarityWeightTable,
        System.Random rng,
        ICollection<CraftingBlueprint_SO> excludedBlueprints = null)
    {
        if (blueprintTable == null)
            return null;
        if (rng == null)
            rng = new System.Random();

        IReadOnlyList<CraftingBlueprint_SO> blueprints = blueprintTable.GetValidBlueprints();
        if (blueprints == null || blueprints.Count == 0)
            return null;

        Dictionary<LootRarity, List<CraftingBlueprint_SO>> buckets = new Dictionary<LootRarity, List<CraftingBlueprint_SO>>();
        List<CraftingBlueprint_SO> valid = new List<CraftingBlueprint_SO>();
        for (int i = 0; i < blueprints.Count; i++)
        {
            CraftingBlueprint_SO blueprint = blueprints[i];
            if (blueprint == null)
                continue;
            if (excludedBlueprints != null && excludedBlueprints.Contains(blueprint))
                continue;

            valid.Add(blueprint);
            if (!buckets.TryGetValue(blueprint.rarity, out List<CraftingBlueprint_SO> bucket))
            {
                bucket = new List<CraftingBlueprint_SO>();
                buckets.Add(blueprint.rarity, bucket);
            }

            bucket.Add(blueprint);
        }

        if (valid.Count == 0)
            return null;

        LootRarity selectedRarity = SelectWeightedRarity(rarityWeightTable, rng, out bool hasWeightedSelection);
        if (!hasWeightedSelection)
            return valid[rng.Next(valid.Count)];

        List<CraftingBlueprint_SO> selectedBucket = ResolveBucketWithRarityFallback(buckets, selectedRarity);
        if (selectedBucket == null || selectedBucket.Count == 0)
            return null;

        return selectedBucket[rng.Next(selectedBucket.Count)];
    }

    private static LootRarity SelectWeightedRarity(
        LootRarityWeightTable_SO weightTable,
        System.Random rng,
        out bool hasWeightedSelection)
    {
        hasWeightedSelection = false;
        if (weightTable == null || !weightTable.HasPositiveWeights)
            return LootRarity.Common;

        float totalWeight = 0f;
        List<LootRarity> rarities = new List<LootRarity>();
        List<float> cumulativeWeights = new List<float>();
        for (int rarityIndex = 0; rarityIndex <= (int)LootRarity.Legendary; rarityIndex++)
        {
            LootRarity rarity = (LootRarity)rarityIndex;
            float weight = Mathf.Max(0f, weightTable.GetWeight(rarity));
            if (weight <= 0f)
                continue;

            totalWeight += weight;
            rarities.Add(rarity);
            cumulativeWeights.Add(totalWeight);
        }

        if (totalWeight <= 0f || rarities.Count == 0)
            return LootRarity.Common;

        double roll = rng.NextDouble() * totalWeight;
        for (int i = 0; i < cumulativeWeights.Count; i++)
        {
            if (roll <= cumulativeWeights[i])
            {
                hasWeightedSelection = true;
                return rarities[i];
            }
        }

        hasWeightedSelection = true;
        return rarities[rarities.Count - 1];
    }

    private static List<CraftingBlueprint_SO> ResolveBucketWithRarityFallback(
        Dictionary<LootRarity, List<CraftingBlueprint_SO>> buckets,
        LootRarity selectedRarity)
    {
        if (TryGetNonEmptyBucket(buckets, selectedRarity, out List<CraftingBlueprint_SO> directBucket))
            return directBucket;

        for (int rarityIndex = (int)selectedRarity - 1; rarityIndex >= (int)LootRarity.Common; rarityIndex--)
        {
            if (TryGetNonEmptyBucket(buckets, (LootRarity)rarityIndex, out List<CraftingBlueprint_SO> lowerBucket))
                return lowerBucket;
        }

        for (int rarityIndex = (int)selectedRarity + 1; rarityIndex <= (int)LootRarity.Legendary; rarityIndex++)
        {
            if (TryGetNonEmptyBucket(buckets, (LootRarity)rarityIndex, out List<CraftingBlueprint_SO> higherBucket))
                return higherBucket;
        }

        return null;
    }

    private static bool TryGetNonEmptyBucket(
        Dictionary<LootRarity, List<CraftingBlueprint_SO>> buckets,
        LootRarity rarity,
        out List<CraftingBlueprint_SO> bucket)
    {
        bucket = null;
        return buckets != null
            && buckets.TryGetValue(rarity, out bucket)
            && bucket != null
            && bucket.Count > 0;
    }
}
