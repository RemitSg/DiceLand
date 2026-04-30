using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class BattleRelicDropRollServiceTests
{
    [Test]
    public void RollTableRelicDrops_AggregatesNormalRelicDrops()
    {
        Relic_SO relic = CreateRelic("Relic", LootRarity.Common);
        RelicTable_SO relicTable = CreateRelicTable(relic);
        LootTable_SO lootTable = CreateLootTable(
            new LootTable_SO.RelicDropEntry { relicTable = relicTable, dropChance = 1f, minAmount = 2, maxAmount = 2 });
        Dictionary<Relic_SO, int> aggregated = new Dictionary<Relic_SO, int>();

        bool createdChoice = BattleRelicDropRollService.RollTableRelicDrops(
            lootTable,
            new System.Random(1),
            false,
            false,
            null,
            3,
            aggregated,
            _ => Assert.Fail("Normal relic drops should not create a choice."));

        Assert.IsFalse(createdChoice);
        Assert.AreEqual(2, aggregated[relic]);
    }

    [Test]
    public void RollTableRelicDrops_CreatesEliteChoiceOnlyOnce()
    {
        Relic_SO first = CreateRelic("A", LootRarity.Rare);
        Relic_SO second = CreateRelic("B", LootRarity.Rare);
        RelicTable_SO relicTable = CreateRelicTable(first, second);
        LootTable_SO lootTable = CreateLootTable(
            new LootTable_SO.RelicDropEntry { relicTable = relicTable, dropChance = 1f, minAmount = 2, maxAmount = 2 });
        Dictionary<Relic_SO, int> aggregated = new Dictionary<Relic_SO, int>();
        int choiceApplyCount = 0;

        bool createdChoice = BattleRelicDropRollService.RollTableRelicDrops(
            lootTable,
            new System.Random(1),
            true,
            false,
            null,
            2,
            aggregated,
            choiceData =>
            {
                choiceApplyCount++;
                Assert.NotNull(choiceData);
                Assert.AreEqual(2, choiceData.choices.Count);
            });

        Assert.IsTrue(createdChoice);
        Assert.AreEqual(1, choiceApplyCount);
        Assert.AreEqual(0, aggregated.Count);
    }

    [Test]
    public void RollTableRelicDrops_DoesNotCreateSecondEliteChoice()
    {
        Relic_SO relic = CreateRelic("Relic", LootRarity.Rare);
        RelicTable_SO relicTable = CreateRelicTable(relic);
        LootTable_SO lootTable = CreateLootTable(
            new LootTable_SO.RelicDropEntry { relicTable = relicTable, dropChance = 1f, minAmount = 1, maxAmount = 1 });

        bool createdChoice = BattleRelicDropRollService.RollTableRelicDrops(
            lootTable,
            new System.Random(1),
            true,
            true,
            null,
            1,
            new Dictionary<Relic_SO, int>(),
            _ => Assert.Fail("Existing elite choices should not be rebuilt."));

        Assert.IsTrue(createdChoice);
    }

    private static Relic_SO CreateRelic(string name, LootRarity rarity)
    {
        Relic_SO relic = ScriptableObject.CreateInstance<Relic_SO>();
        relic.relicName = name;
        relic.rarity = rarity;
        return relic;
    }

    private static RelicTable_SO CreateRelicTable(params Relic_SO[] relics)
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
                new RelicTable_SO.RarityWeightEntry { rarity = LootRarity.Rare, weight = 1f }
            });
        return table;
    }

    private static LootTable_SO CreateLootTable(params LootTable_SO.RelicDropEntry[] relicDrops)
    {
        LootTable_SO table = ScriptableObject.CreateInstance<LootTable_SO>();
        typeof(LootTable_SO)
            .GetField("relicDrops", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(table, new List<LootTable_SO.RelicDropEntry>(relicDrops));
        return table;
    }
}
