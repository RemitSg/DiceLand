using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class MapEventEffectSequenceBuilderTests
{
    [Test]
    public void BuildAllEffects_PreservesPrimaryThenAdditionalEffects()
    {
        MapEventOptionData option = new MapEventOptionData
        {
            builtInEffectType = MapEventOptionBuiltInEffectType.GainMoney,
            effectAmount = 12,
            randomMoneyMin = 2,
            randomMoneyMax = 5,
            lootPickCountMin = 1,
            lootPickCountMax = 2,
            relicPickCountMin = 1,
            relicPickCountMax = 1,
            blueprintPickCountMin = 1,
            blueprintPickCountMax = 1,
            encounterEnemyCountMin = 1,
            encounterEnemyCountMax = 3,
            additionalBuiltInEffects = new List<MapEventBuiltInEffectData>
            {
                new MapEventBuiltInEffectData { effectType = MapEventOptionBuiltInEffectType.GainExperience, effectAmount = 8 },
                new MapEventBuiltInEffectData { effectType = MapEventOptionBuiltInEffectType.RestoreAllActionPoints }
            }
        };

        List<MapEventBuiltInEffectData> effects = MapEventEffectSequenceBuilder.BuildAllEffects(option);

        Assert.AreEqual(3, effects.Count);
        Assert.AreEqual(MapEventOptionBuiltInEffectType.GainMoney, effects[0].effectType);
        Assert.AreEqual(12, effects[0].effectAmount);
        Assert.AreEqual(2, effects[0].randomMoneyMin);
        Assert.AreEqual(5, effects[0].randomMoneyMax);
        Assert.AreEqual(1, effects[0].lootPickCountMin);
        Assert.AreEqual(2, effects[0].lootPickCountMax);
        Assert.AreEqual(3, effects[0].encounterEnemyCountMax);
        Assert.AreEqual(MapEventOptionBuiltInEffectType.GainExperience, effects[1].effectType);
        Assert.AreEqual(MapEventOptionBuiltInEffectType.RestoreAllActionPoints, effects[2].effectType);
    }

    [Test]
    public void BuildAllEffects_CopiesPrimaryRewardListsWithoutAliasingOptionLists()
    {
        MapEventLootRewardEntry lootReward = new MapEventLootRewardEntry { amountMin = 1, amountMax = 3 };
        RelicTable_SO relicTable = ScriptableObject.CreateInstance<RelicTable_SO>();
        LootRarityWeightTable_SO relicRarityWeightTable = ScriptableObject.CreateInstance<LootRarityWeightTable_SO>();
        BlueprintTable_SO blueprintTable = ScriptableObject.CreateInstance<BlueprintTable_SO>();
        LootRarityWeightTable_SO blueprintRarityWeightTable = ScriptableObject.CreateInstance<LootRarityWeightTable_SO>();
        MapEventOptionData option = new MapEventOptionData
        {
            builtInEffectType = MapEventOptionBuiltInEffectType.GainLoot,
            lootRewards = new List<MapEventLootRewardEntry> { lootReward },
            relicTable = relicTable,
            relicRarityWeightTable = relicRarityWeightTable,
            blueprintTable = blueprintTable,
            blueprintRarityWeightTable = blueprintRarityWeightTable
        };

        MapEventBuiltInEffectData primary = MapEventEffectSequenceBuilder.BuildPrimaryEffect(option);

        Assert.AreEqual(1, primary.lootRewards.Count);
        Assert.AreSame(lootReward, primary.lootRewards[0]);
        Assert.AreNotSame(option.lootRewards, primary.lootRewards);
        Assert.AreSame(relicTable, primary.relicTable);
        Assert.AreSame(relicRarityWeightTable, primary.relicRarityWeightTable);
        Assert.AreSame(blueprintTable, primary.blueprintTable);
        Assert.AreSame(blueprintRarityWeightTable, primary.blueprintRarityWeightTable);

        Object.DestroyImmediate(relicTable);
        Object.DestroyImmediate(relicRarityWeightTable);
        Object.DestroyImmediate(blueprintTable);
        Object.DestroyImmediate(blueprintRarityWeightTable);
    }

    [Test]
    public void IsDeferredEncounterVictoryRewardEffect_OnlyReturnsTrueForRewardEffects()
    {
        Assert.IsTrue(MapEventEffectSequenceBuilder.IsDeferredEncounterVictoryRewardEffect(MapEventOptionBuiltInEffectType.GainLoot));
        Assert.IsTrue(MapEventEffectSequenceBuilder.IsDeferredEncounterVictoryRewardEffect(MapEventOptionBuiltInEffectType.GainRandomLootFromList));
        Assert.IsTrue(MapEventEffectSequenceBuilder.IsDeferredEncounterVictoryRewardEffect(MapEventOptionBuiltInEffectType.GainRelics));
        Assert.IsTrue(MapEventEffectSequenceBuilder.IsDeferredEncounterVictoryRewardEffect(MapEventOptionBuiltInEffectType.GainBlueprints));
        Assert.IsFalse(MapEventEffectSequenceBuilder.IsDeferredEncounterVictoryRewardEffect(MapEventOptionBuiltInEffectType.GainMoney));
        Assert.IsFalse(MapEventEffectSequenceBuilder.IsDeferredEncounterVictoryRewardEffect(MapEventOptionBuiltInEffectType.StartEncounter));
        Assert.IsFalse(MapEventEffectSequenceBuilder.IsDeferredEncounterVictoryRewardEffect(MapEventOptionBuiltInEffectType.LoseSpecificLoot));
    }
}
