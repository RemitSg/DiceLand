using System.Collections.Generic;
using UnityEngine;
using System;

public static class BattleEliteRelicChoiceBuilderService
{
    public static RelicTable_SO ResolveFallbackTable(BattleEncounterData encounter, RelicTable_SO configuredFallbackTable)
    {
        if (encounter != null && encounter.eliteRelicChoiceTable != null)
            return encounter.eliteRelicChoiceTable;

        return configuredFallbackTable;
    }

    public static bool TryBuildAndApplyFromTable(
        RelicTable_SO relicTable,
        System.Random rng,
        ICollection<Relic_SO> ownedRelics,
        int choiceCount,
        Action<BattleSettlementRelicChoiceData> applyChoiceData)
    {
        if (!TryBuildFromTable(out BattleSettlementRelicChoiceData choiceData, relicTable, rng, ownedRelics, choiceCount))
            return false;

        applyChoiceData?.Invoke(choiceData);
        return choiceData != null && choiceData.HasChoices;
    }

    public static RelicTable_SO SelectFirstValidFallbackTable(IEnumerable<RelicTable_SO> tables)
    {
        if (tables == null)
            return null;

        foreach (RelicTable_SO table in tables)
        {
            if (table != null && table.IsValid)
                return table;
        }

        return null;
    }

    public static bool TryBuildAndApplyFromRelics(
        IReadOnlyList<Relic_SO> relics,
        System.Random rng,
        ICollection<Relic_SO> ownedRelics,
        int choiceCount,
        Action<BattleSettlementRelicChoiceData> applyChoiceData)
    {
        Dictionary<LootRarity, List<Relic_SO>> rarityBuckets = BuildRarityBuckets(relics, ownedRelics);
        if (!TryBuildFromRarityBuckets(out BattleSettlementRelicChoiceData choiceData, rarityBuckets, rng, choiceCount))
            return false;

        applyChoiceData?.Invoke(choiceData);
        return choiceData != null && choiceData.HasChoices;
    }

    public static bool TryBuildFromTable(
        out BattleSettlementRelicChoiceData choiceData,
        RelicTable_SO relicTable,
        System.Random rng,
        ICollection<Relic_SO> ownedRelics,
        int choiceCount)
    {
        choiceData = null;
        if (relicTable == null)
            return false;

        List<Relic_SO> choiceRelics = relicTable.DrawRandomRelicChoices(
            rng,
            Mathf.Max(1, choiceCount),
            ownedRelics);
        if (choiceRelics == null || choiceRelics.Count == 0)
            return false;

        choiceData = BattleSettlementRelicChoiceBuilder.Build(choiceRelics, relicTable, Mathf.Max(1, choiceCount));
        return choiceData != null && choiceData.HasChoices;
    }

    public static bool TryBuildFromRarityBuckets(
        out BattleSettlementRelicChoiceData choiceData,
        Dictionary<LootRarity, List<Relic_SO>> rarityBuckets,
        System.Random rng,
        int choiceCount)
    {
        choiceData = null;
        int normalizedChoiceCount = Mathf.Max(1, choiceCount);
        List<Relic_SO> selectedBucket = SelectFallbackRelicBucket(rarityBuckets, rng, normalizedChoiceCount);
        if (selectedBucket == null || selectedBucket.Count == 0)
            return false;

        List<Relic_SO> remaining = new List<Relic_SO>(selectedBucket);
        List<Relic_SO> choices = new List<Relic_SO>();
        int count = Mathf.Min(normalizedChoiceCount, remaining.Count);
        System.Random resolvedRng = rng ?? new System.Random();
        for (int i = 0; i < count; i++)
        {
            int index = resolvedRng.Next(remaining.Count);
            choices.Add(remaining[index]);
            remaining.RemoveAt(index);
        }

        choiceData = BattleSettlementRelicChoiceBuilder.Build(choices);
        return choiceData != null && choiceData.HasChoices;
    }

    public static bool TryBuildFromRelicsWithRarity(
        out BattleSettlementRelicChoiceData choiceData,
        IReadOnlyList<Relic_SO> relics,
        LootRarity selectedRarity,
        System.Random rng,
        ICollection<Relic_SO> ownedRelics,
        int choiceCount)
    {
        choiceData = null;
        Dictionary<LootRarity, List<Relic_SO>> rarityBuckets = BuildRarityBuckets(relics, ownedRelics);
        int normalizedChoiceCount = Mathf.Max(1, choiceCount);
        if (!BattleEliteRelicChoiceUtility.TryGetBucketForChoice(
                rarityBuckets,
                selectedRarity,
                normalizedChoiceCount,
                out List<Relic_SO> selectedBucket) ||
            selectedBucket == null ||
            selectedBucket.Count == 0)
            return false;

        List<Relic_SO> remaining = new List<Relic_SO>(selectedBucket);
        List<Relic_SO> choices = new List<Relic_SO>();
        int count = Mathf.Min(normalizedChoiceCount, remaining.Count);
        System.Random resolvedRng = rng ?? new System.Random();
        for (int i = 0; i < count; i++)
        {
            int index = resolvedRng.Next(remaining.Count);
            choices.Add(remaining[index]);
            remaining.RemoveAt(index);
        }

        choiceData = BattleSettlementRelicChoiceBuilder.Build(choices);
        if (choiceData != null)
        {
            choiceData.refreshPool = new List<Relic_SO>(selectedBucket);
            choiceData.choiceCount = normalizedChoiceCount;
        }
        return choiceData != null && choiceData.HasChoices;
    }

    public static Dictionary<LootRarity, List<Relic_SO>> BuildRarityBuckets(
        IReadOnlyList<Relic_SO> relics,
        ICollection<Relic_SO> ownedRelics)
    {
        Dictionary<LootRarity, List<Relic_SO>> rarityBuckets = new Dictionary<LootRarity, List<Relic_SO>>();
        if (relics == null)
            return rarityBuckets;

        for (int i = 0; i < relics.Count; i++)
        {
            Relic_SO relic = relics[i];
            if (relic == null)
                continue;
            if (ownedRelics != null && ownedRelics.Contains(relic))
                continue;

            if (!rarityBuckets.TryGetValue(relic.rarity, out List<Relic_SO> bucket))
            {
                bucket = new List<Relic_SO>();
                rarityBuckets.Add(relic.rarity, bucket);
            }

            bucket.Add(relic);
        }

        return rarityBuckets;
    }

    public static List<Relic_SO> SelectFallbackRelicBucket(
        Dictionary<LootRarity, List<Relic_SO>> rarityBuckets,
        System.Random rng,
        int desiredCount)
    {
        if (rarityBuckets == null || rarityBuckets.Count == 0)
            return null;

        LootRarity selectedRarity = BattleEliteRelicChoiceUtility.SelectFallbackRarity(rarityBuckets, rng);
        if (BattleEliteRelicChoiceUtility.TryGetBucketForChoice(rarityBuckets, selectedRarity, desiredCount, out List<Relic_SO> selectedBucket))
            return selectedBucket;

        foreach (KeyValuePair<LootRarity, List<Relic_SO>> pair in rarityBuckets)
        {
            if (pair.Value != null && pair.Value.Count > 0)
                return pair.Value;
        }

        return null;
    }
}
