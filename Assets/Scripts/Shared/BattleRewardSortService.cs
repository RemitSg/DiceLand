using System;
using System.Collections.Generic;

public static class BattleRewardSortService
{
    public static int CompareLootRewards(RunLootState a, RunLootState b)
    {
        if (ReferenceEquals(a, b))
            return 0;
        if (a == null)
            return 1;
        if (b == null)
            return -1;
        if (a.lootItem == null)
            return 1;
        if (b.lootItem == null)
            return -1;

        int rarityCompare = b.lootItem.rarity.CompareTo(a.lootItem.rarity);
        if (rarityCompare != 0)
            return rarityCompare;

        return string.Compare(a.DisplayName, b.DisplayName, StringComparison.Ordinal);
    }

    public static int CompareRelicRewards(RunRelicState a, RunRelicState b)
    {
        if (ReferenceEquals(a, b))
            return 0;
        if (a == null)
            return 1;
        if (b == null)
            return -1;
        if (a.relicData == null)
            return 1;
        if (b.relicData == null)
            return -1;

        int rarityCompare = b.relicData.rarity.CompareTo(a.relicData.rarity);
        if (rarityCompare != 0)
            return rarityCompare;

        return string.Compare(a.DisplayName, b.DisplayName, StringComparison.Ordinal);
    }
}

public static class BattleRewardAggregationService
{
    public static List<RunLootState> BuildLootRewardsFromAggregation(IDictionary<LootItem_SO, int> aggregated)
    {
        List<RunLootState> rewards = new List<RunLootState>();
        if (aggregated == null)
            return rewards;

        foreach (KeyValuePair<LootItem_SO, int> pair in aggregated)
        {
            rewards.Add(new RunLootState
            {
                lootItem = pair.Key,
                amount = pair.Value
            });
        }

        rewards.Sort(BattleRewardSortService.CompareLootRewards);
        return rewards;
    }

    public static List<RunRelicState> BuildRelicRewardsFromAggregation(IDictionary<Relic_SO, int> aggregated)
    {
        List<RunRelicState> rewards = new List<RunRelicState>();
        if (aggregated == null)
            return rewards;

        foreach (KeyValuePair<Relic_SO, int> pair in aggregated)
        {
            rewards.Add(new RunRelicState
            {
                relicData = pair.Key,
                amount = pair.Value
            });
        }

        rewards.Sort(BattleRewardSortService.CompareRelicRewards);
        return rewards;
    }
}
