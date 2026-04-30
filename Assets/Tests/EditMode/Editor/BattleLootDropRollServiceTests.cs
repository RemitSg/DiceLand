using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class BattleLootDropRollServiceTests
{
    [Test]
    public void RollTableLootDrops_AggregatesSuccessfulDrops()
    {
        LootItem_SO loot = CreateLoot("Ore", LootRarity.Common);
        LootTable_SO table = CreateTable(
            new LootTable_SO.LootDropEntry { lootItem = loot, dropChance = 1f, minAmount = 2, maxAmount = 2 },
            new LootTable_SO.LootDropEntry { lootItem = loot, dropChance = 1f, minAmount = 3, maxAmount = 3 });
        Dictionary<LootItem_SO, int> aggregated = new Dictionary<LootItem_SO, int>();
        List<LootItem_SO> lootPool = new List<LootItem_SO>();

        BattleLootDropRollService.RollTableLootDrops(table, new System.Random(1), aggregated, lootPool);

        Assert.AreEqual(5, aggregated[loot]);
        Assert.AreEqual(1, lootPool.Count);
        Assert.AreSame(loot, lootPool[0]);
    }

    [Test]
    public void RollTableLootDrops_AddsValidLootToPoolBeforeFailedChanceRoll()
    {
        LootItem_SO loot = CreateLoot("Gem", LootRarity.Rare);
        LootTable_SO table = CreateTable(
            new LootTable_SO.LootDropEntry { lootItem = loot, dropChance = 0.01f, minAmount = 1, maxAmount = 1 });
        Dictionary<LootItem_SO, int> aggregated = new Dictionary<LootItem_SO, int>();
        List<LootItem_SO> lootPool = new List<LootItem_SO>();

        BattleLootDropRollService.RollTableLootDrops(table, new System.Random(1), aggregated, lootPool);

        Assert.IsFalse(aggregated.ContainsKey(loot));
        Assert.AreEqual(1, lootPool.Count);
        Assert.AreSame(loot, lootPool[0]);
    }

    [Test]
    public void RollTableLootDrops_SkipsInvalidEntries()
    {
        LootTable_SO table = CreateTable(
            null,
            new LootTable_SO.LootDropEntry { lootItem = null, dropChance = 1f, minAmount = 1, maxAmount = 1 });
        Dictionary<LootItem_SO, int> aggregated = new Dictionary<LootItem_SO, int>();
        List<LootItem_SO> lootPool = new List<LootItem_SO>();

        BattleLootDropRollService.RollTableLootDrops(table, new System.Random(1), aggregated, lootPool);

        Assert.AreEqual(0, aggregated.Count);
        Assert.AreEqual(0, lootPool.Count);
    }

    private static LootItem_SO CreateLoot(string name, LootRarity rarity)
    {
        LootItem_SO loot = ScriptableObject.CreateInstance<LootItem_SO>();
        loot.lootName = name;
        loot.rarity = rarity;
        return loot;
    }

    private static LootTable_SO CreateTable(params LootTable_SO.LootDropEntry[] drops)
    {
        LootTable_SO table = ScriptableObject.CreateInstance<LootTable_SO>();
        typeof(LootTable_SO)
            .GetField("drops", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(table, new List<LootTable_SO.LootDropEntry>(drops));
        return table;
    }
}
