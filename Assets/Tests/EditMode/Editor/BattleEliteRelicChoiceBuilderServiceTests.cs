using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class BattleEliteRelicChoiceBuilderServiceTests
{
    [Test]
    public void TryBuildFromTable_UsesRelicTableChoices()
    {
        Relic_SO first = CreateRelic("A", LootRarity.Rare);
        Relic_SO second = CreateRelic("B", LootRarity.Rare);
        RelicTable_SO table = CreateTable(first, second);

        bool built = BattleEliteRelicChoiceBuilderService.TryBuildFromTable(
            out BattleSettlementRelicChoiceData choiceData,
            table,
            new System.Random(1),
            null,
            2);

        Assert.IsTrue(built);
        Assert.NotNull(choiceData);
        Assert.AreEqual(2, choiceData.choices.Count);
        Assert.IsTrue(choiceData.HasChoices);
    }

    [Test]
    public void TryBuildFromTable_ExcludesOwnedRelics()
    {
        Relic_SO owned = CreateRelic("Owned", LootRarity.Common);
        Relic_SO available = CreateRelic("Available", LootRarity.Common);
        RelicTable_SO table = CreateTable(owned, available);

        bool built = BattleEliteRelicChoiceBuilderService.TryBuildFromTable(
            out BattleSettlementRelicChoiceData choiceData,
            table,
            new System.Random(1),
            new HashSet<Relic_SO> { owned },
            1);

        Assert.IsTrue(built);
        Assert.AreEqual(1, choiceData.choices.Count);
        Assert.AreSame(available, choiceData.choices[0].relicData);
    }

    [Test]
    public void BuildRarityBuckets_SkipsNullAndOwnedRelics()
    {
        Relic_SO owned = CreateRelic("Owned", LootRarity.Common);
        Relic_SO available = CreateRelic("Available", LootRarity.Rare);

        Dictionary<LootRarity, List<Relic_SO>> buckets = BattleEliteRelicChoiceBuilderService.BuildRarityBuckets(
            new List<Relic_SO> { null, owned, available },
            new HashSet<Relic_SO> { owned });

        Assert.IsFalse(buckets.ContainsKey(LootRarity.Common));
        Assert.AreEqual(1, buckets[LootRarity.Rare].Count);
        Assert.AreSame(available, buckets[LootRarity.Rare][0]);
    }

    [Test]
    public void TryBuildFromRarityBuckets_UsesSelectedBucket()
    {
        Relic_SO first = CreateRelic("A", LootRarity.Epic);
        Relic_SO second = CreateRelic("B", LootRarity.Epic);
        Dictionary<LootRarity, List<Relic_SO>> buckets = new Dictionary<LootRarity, List<Relic_SO>>
        {
            { LootRarity.Epic, new List<Relic_SO> { first, second } }
        };

        bool built = BattleEliteRelicChoiceBuilderService.TryBuildFromRarityBuckets(
            out BattleSettlementRelicChoiceData choiceData,
            buckets,
            new System.Random(1),
            2);

        Assert.IsTrue(built);
        Assert.AreEqual(2, choiceData.choices.Count);
    }

    [Test]
    public void TryBuildFromRelicsWithRarity_BuildsOnlySelectedRarityChoices()
    {
        Relic_SO common = CreateRelic("Common", LootRarity.Common);
        Relic_SO firstRare = CreateRelic("Rare A", LootRarity.Rare);
        Relic_SO secondRare = CreateRelic("Rare B", LootRarity.Rare);
        Relic_SO thirdRare = CreateRelic("Rare C", LootRarity.Rare);

        bool built = BattleEliteRelicChoiceBuilderService.TryBuildFromRelicsWithRarity(
            out BattleSettlementRelicChoiceData choiceData,
            new List<Relic_SO> { common, firstRare, secondRare, thirdRare },
            LootRarity.Rare,
            new System.Random(1),
            null,
            3);

        Assert.IsTrue(built);
        Assert.NotNull(choiceData);
        Assert.AreEqual(3, choiceData.choices.Count);
        Assert.IsTrue(choiceData.choices.TrueForAll(choice => choice.rarity == LootRarity.Rare));
    }

    [Test]
    public void ResolveFallbackTable_PrefersEncounterTableThenConfiguredFallback()
    {
        RelicTable_SO encounterTable = CreateTable(CreateRelic("Encounter", LootRarity.Common));
        RelicTable_SO fallbackTable = CreateTable(CreateRelic("Fallback", LootRarity.Common));

        Assert.AreSame(
            encounterTable,
            BattleEliteRelicChoiceBuilderService.ResolveFallbackTable(
                new BattleEncounterData { eliteRelicChoiceTable = encounterTable },
                fallbackTable));
        Assert.AreSame(
            fallbackTable,
            BattleEliteRelicChoiceBuilderService.ResolveFallbackTable(
                new BattleEncounterData(),
                fallbackTable));
    }

    [Test]
    public void TryBuildAndApplyFromTable_AppliesChoiceDataOnlyWhenBuilt()
    {
        Relic_SO relic = CreateRelic("A", LootRarity.Rare);
        BattleSettlementRelicChoiceData applied = null;

        bool built = BattleEliteRelicChoiceBuilderService.TryBuildAndApplyFromTable(
            CreateTable(relic),
            new System.Random(1),
            null,
            1,
            choiceData => applied = choiceData);

        Assert.IsTrue(built);
        Assert.NotNull(applied);
        Assert.IsTrue(applied.HasChoices);
    }

    [Test]
    public void SelectFirstValidFallbackTable_SkipsNullAndInvalidTables()
    {
        RelicTable_SO valid = CreateTable(CreateRelic("Valid", LootRarity.Common));

        Assert.AreSame(valid, BattleEliteRelicChoiceBuilderService.SelectFirstValidFallbackTable(
            new List<RelicTable_SO> { null, ScriptableObject.CreateInstance<RelicTable_SO>(), valid }));
    }

    [Test]
    public void TryBuildAndApplyFromRelics_BuildsFromRarityBuckets()
    {
        Relic_SO first = CreateRelic("A", LootRarity.Rare);
        Relic_SO second = CreateRelic("B", LootRarity.Rare);
        BattleSettlementRelicChoiceData applied = null;

        bool built = BattleEliteRelicChoiceBuilderService.TryBuildAndApplyFromRelics(
            new List<Relic_SO> { first, second },
            new System.Random(1),
            null,
            2,
            choiceData => applied = choiceData);

        Assert.IsTrue(built);
        Assert.NotNull(applied);
        Assert.AreEqual(2, applied.choices.Count);
    }

    private static Relic_SO CreateRelic(string name, LootRarity rarity)
    {
        Relic_SO relic = ScriptableObject.CreateInstance<Relic_SO>();
        relic.relicName = name;
        relic.rarity = rarity;
        return relic;
    }

    private static RelicTable_SO CreateTable(params Relic_SO[] relics)
    {
        RelicTable_SO table = ScriptableObject.CreateInstance<RelicTable_SO>();
        typeof(RelicTable_SO)
            .GetField("relics", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(table, new List<Relic_SO>(relics));
        typeof(RelicTable_SO)
            .GetField("rarityWeights", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(table, new List<RelicTable_SO.RarityWeightEntry>
            {
                new RelicTable_SO.RarityWeightEntry { rarity = LootRarity.Common, weight = 1f },
                new RelicTable_SO.RarityWeightEntry { rarity = LootRarity.Rare, weight = 1f },
                new RelicTable_SO.RarityWeightEntry { rarity = LootRarity.Epic, weight = 1f }
            });
        return table;
    }
}
