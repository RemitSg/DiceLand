using System;
using System.Collections.Generic;

public static class BattleBonusLootRewardService
{
    public static void ApplyBonusLootRewards(
        List<RunLootState> lootRewards,
        IReadOnlyList<LootItem_SO> lootPool,
        BattleEncounterData encounter,
        Action<BattleEncounterData, RelicExecutionContext> processRelics,
        Action<RelicExecutionContext> flushFeedback)
    {
        if (encounter == null
            || encounter.relicInventory == null
            || encounter.relicInventory.Count == 0
            || lootRewards == null
            || lootRewards.Count == 0
            || lootPool == null
            || lootPool.Count == 0)
            return;

        Dictionary<LootItem_SO, int> bonusLoot = new Dictionary<LootItem_SO, int>();
        for (int rewardIndex = 0; rewardIndex < lootRewards.Count; rewardIndex++)
        {
            RunLootState reward = lootRewards[rewardIndex];
            if (reward == null || reward.lootItem == null || reward.amount <= 0)
                continue;

            for (int amountIndex = 0; amountIndex < reward.amount; amountIndex++)
            {
                RelicExecutionContext lootContext = RelicExecutionContext.CreateBattleLootRewardGranted(
                    null,
                    reward.lootItem,
                    lootPool);
                processRelics?.Invoke(encounter, lootContext);
                CollectBonusLoot(lootContext, bonusLoot);
                flushFeedback?.Invoke(lootContext);
            }
        }

        MergeBonusLootRewards(lootRewards, bonusLoot);
    }

    private static void CollectBonusLoot(
        RelicExecutionContext lootContext,
        IDictionary<LootItem_SO, int> bonusLoot)
    {
        if (lootContext == null || lootContext.bonusBattleLootRewards == null || bonusLoot == null)
            return;

        for (int bonusIndex = 0; bonusIndex < lootContext.bonusBattleLootRewards.Count; bonusIndex++)
        {
            LootItem_SO bonusItem = lootContext.bonusBattleLootRewards[bonusIndex];
            if (bonusItem == null)
                continue;

            if (bonusLoot.ContainsKey(bonusItem))
                bonusLoot[bonusItem] += 1;
            else
                bonusLoot.Add(bonusItem, 1);
        }
    }

    private static void MergeBonusLootRewards(
        List<RunLootState> lootRewards,
        Dictionary<LootItem_SO, int> bonusLoot)
    {
        if (lootRewards == null || bonusLoot == null || bonusLoot.Count == 0)
            return;

        foreach (KeyValuePair<LootItem_SO, int> pair in bonusLoot)
        {
            bool merged = false;
            for (int i = 0; i < lootRewards.Count; i++)
            {
                RunLootState existing = lootRewards[i];
                if (existing == null || existing.lootItem != pair.Key)
                    continue;

                existing.amount += pair.Value;
                merged = true;
                break;
            }

            if (!merged)
            {
                lootRewards.Add(new RunLootState
                {
                    lootItem = pair.Key,
                    amount = pair.Value
                });
            }
        }

        lootRewards.Sort(BattleRewardSortService.CompareLootRewards);
    }
}
