using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

public class MapEventRewardServiceTests
{
    private LootItem_SO loot;
    private Relic_SO relic;
    private Relic_SO rareRelic;
    private RelicTable_SO relicTable;
    private LootRarityWeightTable_SO relicRarityWeightTable;
    private CraftingBlueprint_SO blueprint;
    private CraftingBlueprint_SO rareBlueprint;
    private BlueprintTable_SO blueprintTable;
    private LootRarityWeightTable_SO rarityWeightTable;

    [TearDown]
    public void TearDown()
    {
        if (loot != null)
            Object.DestroyImmediate(loot);
        if (relic != null)
            Object.DestroyImmediate(relic);
        if (rareRelic != null)
            Object.DestroyImmediate(rareRelic);
        if (relicTable != null)
            Object.DestroyImmediate(relicTable);
        if (relicRarityWeightTable != null)
            Object.DestroyImmediate(relicRarityWeightTable);
        if (blueprint != null)
            Object.DestroyImmediate(blueprint);
        if (rareBlueprint != null)
            Object.DestroyImmediate(rareBlueprint);
        if (blueprintTable != null)
            Object.DestroyImmediate(blueprintTable);
        if (rarityWeightTable != null)
            Object.DestroyImmediate(rarityWeightTable);
    }

    [Test]
    public void TryExecuteLootReward_AddsConfiguredLootToRunState()
    {
        loot = ScriptableObject.CreateInstance<LootItem_SO>();
        loot.lootName = "Amber";
        RunState runState = new RunState();
        MapEventBuiltInEffectData effect = new MapEventBuiltInEffectData
        {
            effectType = MapEventOptionBuiltInEffectType.GainLoot,
            lootPickCountMin = 1,
            lootPickCountMax = 1,
            lootRewards =
            {
                new MapEventLootRewardEntry
                {
                    lootItem = loot,
                    amountMin = 2,
                    amountMax = 2
                }
            }
        };

        bool result = MapEventRewardService.TryExecuteLootReward(runState, effect, out string message);

        Assert.IsTrue(result);
        Assert.AreEqual(2, runState.GetLootAmount(loot));
        StringAssert.Contains("Amber x2", message);
    }

    [Test]
    public void TryExecuteRelicReward_AddsConfiguredRelicToRunState()
    {
        relic = CreateRelic("Dawn", LootRarity.Common);
        relicTable = CreateRelicTable(relic);
        RunState runState = new RunState();
        MapEventBuiltInEffectData effect = new MapEventBuiltInEffectData
        {
            effectType = MapEventOptionBuiltInEffectType.GainRelics,
            relicPickCountMin = 1,
            relicPickCountMax = 1,
            relicTable = relicTable
        };

        bool result = MapEventRewardService.TryExecuteRelicReward(runState, effect, out string message);

        Assert.IsTrue(result);
        Assert.AreEqual(1, runState.GetRelicAmount(relic));
        StringAssert.Contains("Dawn", message);
    }

    [Test]
    public void TryExecuteRelicReward_UsesRarityWeightTableWhenConfigured()
    {
        relic = CreateRelic("CommonRelic", LootRarity.Common);
        rareRelic = CreateRelic("RareRelic", LootRarity.Rare);
        relicTable = CreateRelicTable(relic, rareRelic);
        relicRarityWeightTable = CreateRarityWeightTable(
            new LootRarityWeightTable_SO.RarityWeightEntry { rarity = LootRarity.Common, weight = 0f },
            new LootRarityWeightTable_SO.RarityWeightEntry { rarity = LootRarity.Rare, weight = 1f });
        RunState runState = new RunState();
        MapEventBuiltInEffectData effect = new MapEventBuiltInEffectData
        {
            effectType = MapEventOptionBuiltInEffectType.GainRelics,
            relicPickCountMin = 1,
            relicPickCountMax = 1,
            relicTable = relicTable,
            relicRarityWeightTable = relicRarityWeightTable
        };

        bool result = MapEventRewardService.TryExecuteRelicReward(runState, effect, out string message);

        Assert.IsTrue(result);
        Assert.AreEqual(0, runState.GetRelicAmount(relic));
        Assert.AreEqual(1, runState.GetRelicAmount(rareRelic));
        StringAssert.Contains("RareRelic", message);
    }

    [Test]
    public void TryExecuteRelicReward_FallsBackToRelicTableWeightsWhenRarityWeightTableIsMissing()
    {
        relic = CreateRelic("CommonRelic", LootRarity.Common);
        rareRelic = CreateRelic("RareRelic", LootRarity.Rare);
        relicTable = CreateRelicTable(
            new List<RelicTable_SO.RarityWeightEntry>
            {
                new RelicTable_SO.RarityWeightEntry { rarity = LootRarity.Common, weight = 0f },
                new RelicTable_SO.RarityWeightEntry { rarity = LootRarity.Rare, weight = 1f }
            },
            relic,
            rareRelic);
        RunState runState = new RunState();
        MapEventBuiltInEffectData effect = new MapEventBuiltInEffectData
        {
            effectType = MapEventOptionBuiltInEffectType.GainRelics,
            relicPickCountMin = 1,
            relicPickCountMax = 1,
            relicTable = relicTable
        };

        bool result = MapEventRewardService.TryExecuteRelicReward(runState, effect, out string message);

        Assert.IsTrue(result);
        Assert.AreEqual(0, runState.GetRelicAmount(relic));
        Assert.AreEqual(1, runState.GetRelicAmount(rareRelic));
        StringAssert.Contains("RareRelic", message);
    }

    [Test]
    public void TryExecuteBlueprintReward_AddsConfiguredBlueprintToRunState()
    {
        blueprint = CreateBlueprint("Needle", LootRarity.Common);
        blueprintTable = CreateBlueprintTable(blueprint);
        RunState runState = new RunState();
        MapEventBuiltInEffectData effect = new MapEventBuiltInEffectData
        {
            effectType = MapEventOptionBuiltInEffectType.GainBlueprints,
            blueprintPickCountMin = 1,
            blueprintPickCountMax = 1,
            blueprintTable = blueprintTable
        };

        bool result = MapEventRewardService.TryExecuteBlueprintReward(runState, effect, out string message);

        Assert.IsTrue(result);
        Assert.AreEqual(1, runState.GetBlueprintAmount(blueprint));
        StringAssert.Contains("Needle", message);
    }

    [Test]
    public void TryExecuteBlueprintReward_UsesRarityWeightTableWhenConfigured()
    {
        blueprint = CreateBlueprint("Common", LootRarity.Common);
        rareBlueprint = CreateBlueprint("Rare", LootRarity.Rare);
        blueprintTable = CreateBlueprintTable(blueprint, rareBlueprint);
        rarityWeightTable = CreateRarityWeightTable(
            new LootRarityWeightTable_SO.RarityWeightEntry { rarity = LootRarity.Common, weight = 0f },
            new LootRarityWeightTable_SO.RarityWeightEntry { rarity = LootRarity.Rare, weight = 1f });
        RunState runState = new RunState();
        MapEventBuiltInEffectData effect = new MapEventBuiltInEffectData
        {
            effectType = MapEventOptionBuiltInEffectType.GainBlueprints,
            blueprintPickCountMin = 1,
            blueprintPickCountMax = 1,
            blueprintTable = blueprintTable,
            blueprintRarityWeightTable = rarityWeightTable
        };

        bool result = MapEventRewardService.TryExecuteBlueprintReward(runState, effect, out string message);

        Assert.IsTrue(result);
        Assert.AreEqual(0, runState.GetBlueprintAmount(blueprint));
        Assert.AreEqual(1, runState.GetBlueprintAmount(rareBlueprint));
        StringAssert.Contains("Rare", message);
    }

    [Test]
    public void TryQueueEncounterVictoryReward_AddsLootToPendingEncounterRewards()
    {
        loot = ScriptableObject.CreateInstance<LootItem_SO>();
        loot.lootName = "Bone";
        BattleEncounterData encounter = new BattleEncounterData();
        MapEventBuiltInEffectData effect = new MapEventBuiltInEffectData
        {
            effectType = MapEventOptionBuiltInEffectType.GainLoot,
            lootPickCountMin = 1,
            lootPickCountMax = 1,
            lootRewards =
            {
                new MapEventLootRewardEntry
                {
                    lootItem = loot,
                    amountMin = 3,
                    amountMax = 3
                }
            }
        };

        bool result = MapEventRewardService.TryQueueEncounterVictoryReward(encounter, effect, out string message);

        Assert.IsTrue(result);
        Assert.AreEqual(1, encounter.pendingVictoryLootRewards.Count);
        Assert.AreSame(loot, encounter.pendingVictoryLootRewards[0].lootItem);
        Assert.AreEqual(3, encounter.pendingVictoryLootRewards[0].amount);
        StringAssert.Contains("Bone x3", message);
    }

    [Test]
    public void TryQueueEncounterVictoryReward_MissingEncounterFails()
    {
        MapEventBuiltInEffectData effect = new MapEventBuiltInEffectData
        {
            effectType = MapEventOptionBuiltInEffectType.GainRelics
        };

        bool result = MapEventRewardService.TryQueueEncounterVictoryReward(null, effect, out string message);

        Assert.IsFalse(result);
        Assert.IsFalse(string.IsNullOrWhiteSpace(message));
    }

    private static CraftingBlueprint_SO CreateBlueprint(string name, LootRarity rarity)
    {
        CraftingBlueprint_SO result = ScriptableObject.CreateInstance<CraftingBlueprint_SO>();
        result.blueprintName = name;
        result.rarity = rarity;
        return result;
    }

    private static Relic_SO CreateRelic(string name, LootRarity rarity)
    {
        Relic_SO result = ScriptableObject.CreateInstance<Relic_SO>();
        result.relicName = name;
        result.rarity = rarity;
        return result;
    }

    private static RelicTable_SO CreateRelicTable(params Relic_SO[] relics)
    {
        RelicTable_SO table = ScriptableObject.CreateInstance<RelicTable_SO>();
        SetPrivateField(table, "relics", new List<Relic_SO>(relics));
        return table;
    }

    private static RelicTable_SO CreateRelicTable(List<RelicTable_SO.RarityWeightEntry> weights, params Relic_SO[] relics)
    {
        RelicTable_SO table = CreateRelicTable(relics);
        SetPrivateField(table, "rarityWeights", weights);
        return table;
    }

    private static BlueprintTable_SO CreateBlueprintTable(params CraftingBlueprint_SO[] blueprints)
    {
        BlueprintTable_SO table = ScriptableObject.CreateInstance<BlueprintTable_SO>();
        SetPrivateField(table, "blueprints", new List<CraftingBlueprint_SO>(blueprints));
        return table;
    }

    private static LootRarityWeightTable_SO CreateRarityWeightTable(params LootRarityWeightTable_SO.RarityWeightEntry[] weights)
    {
        LootRarityWeightTable_SO table = ScriptableObject.CreateInstance<LootRarityWeightTable_SO>();
        SetPrivateField(table, "rarityWeights", new List<LootRarityWeightTable_SO.RarityWeightEntry>(weights));
        return table;
    }

    private static void SetPrivateField<T>(object target, string fieldName, T value)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field, $"Missing field {fieldName} on {target.GetType().Name}.");
        field.SetValue(target, value);
    }
}
