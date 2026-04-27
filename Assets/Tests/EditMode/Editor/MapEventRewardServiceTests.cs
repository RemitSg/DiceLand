using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

public class MapEventRewardServiceTests
{
    private LootItem_SO loot;
    private Relic_SO relic;
    private CraftingBlueprint_SO blueprint;

    [TearDown]
    public void TearDown()
    {
        if (loot != null)
            Object.DestroyImmediate(loot);
        if (relic != null)
            Object.DestroyImmediate(relic);
        if (blueprint != null)
            Object.DestroyImmediate(blueprint);
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
        StringAssert.Contains("获得战利品：Amber x2", message);
    }

    [Test]
    public void TryExecuteRelicReward_AddsConfiguredRelicToRunState()
    {
        relic = ScriptableObject.CreateInstance<Relic_SO>();
        relic.relicName = "Dawn";
        RunState runState = new RunState();
        MapEventBuiltInEffectData effect = new MapEventBuiltInEffectData
        {
            effectType = MapEventOptionBuiltInEffectType.GainRelics,
            relicPickCountMin = 1,
            relicPickCountMax = 1,
            relicRewards =
            {
                new MapEventRelicRewardEntry { relicData = relic }
            }
        };

        bool result = MapEventRewardService.TryExecuteRelicReward(runState, effect, out string message);

        Assert.IsTrue(result);
        Assert.AreEqual(1, runState.GetRelicAmount(relic));
        StringAssert.Contains("获得遗物：Dawn", message);
    }

    [Test]
    public void TryExecuteBlueprintReward_AddsConfiguredBlueprintToRunState()
    {
        blueprint = ScriptableObject.CreateInstance<CraftingBlueprint_SO>();
        blueprint.blueprintName = "Needle";
        RunState runState = new RunState();
        MapEventBuiltInEffectData effect = new MapEventBuiltInEffectData
        {
            effectType = MapEventOptionBuiltInEffectType.GainBlueprints,
            blueprintPickCountMin = 1,
            blueprintPickCountMax = 1,
            blueprintRewards =
            {
                new MapEventBlueprintRewardEntry { blueprintData = blueprint }
            }
        };

        bool result = MapEventRewardService.TryExecuteBlueprintReward(runState, effect, out string message);

        Assert.IsTrue(result);
        Assert.AreEqual(1, runState.GetBlueprintAmount(blueprint));
        StringAssert.Contains("获得图纸：Needle", message);
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
        StringAssert.Contains("战斗胜利后将获得战利品：Bone x3", message);
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
        Assert.AreEqual("未找到待触发的事件遭遇。", message);
    }
}
