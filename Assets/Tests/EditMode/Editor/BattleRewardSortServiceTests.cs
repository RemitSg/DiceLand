using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class BattleRewardSortServiceTests
{
    [Test]
    public void CompareLootRewards_PutsNullsLast()
    {
        Assert.Greater(BattleRewardSortService.CompareLootRewards(null, new RunLootState()), 0);
        Assert.Less(BattleRewardSortService.CompareLootRewards(new RunLootState(), null), 0);
    }

    [Test]
    public void CompareRelicRewards_PutsNullsLast()
    {
        Assert.Greater(BattleRewardSortService.CompareRelicRewards(null, new RunRelicState()), 0);
        Assert.Less(BattleRewardSortService.CompareRelicRewards(new RunRelicState(), null), 0);
    }

    [Test]
    public void BuildLootRewardsFromAggregation_CreatesStatesAndSortsByRarity()
    {
        LootItem_SO common = ScriptableObject.CreateInstance<LootItem_SO>();
        common.lootName = "Common";
        common.rarity = LootRarity.Common;
        LootItem_SO rare = ScriptableObject.CreateInstance<LootItem_SO>();
        rare.lootName = "Rare";
        rare.rarity = LootRarity.Rare;

        List<RunLootState> rewards = BattleRewardAggregationService.BuildLootRewardsFromAggregation(
            new Dictionary<LootItem_SO, int>
            {
                { common, 1 },
                { rare, 2 }
            });

        Assert.AreEqual(2, rewards.Count);
        Assert.AreSame(rare, rewards[0].lootItem);
        Assert.AreEqual(2, rewards[0].amount);
    }

    [Test]
    public void BuildRelicRewardsFromAggregation_CreatesStatesAndSortsByRarity()
    {
        Relic_SO common = ScriptableObject.CreateInstance<Relic_SO>();
        common.relicName = "Common";
        common.rarity = LootRarity.Common;
        Relic_SO epic = ScriptableObject.CreateInstance<Relic_SO>();
        epic.relicName = "Epic";
        epic.rarity = LootRarity.Epic;

        List<RunRelicState> rewards = BattleRewardAggregationService.BuildRelicRewardsFromAggregation(
            new Dictionary<Relic_SO, int>
            {
                { common, 1 },
                { epic, 1 }
            });

        Assert.AreEqual(2, rewards.Count);
        Assert.AreSame(epic, rewards[0].relicData);
    }
}
