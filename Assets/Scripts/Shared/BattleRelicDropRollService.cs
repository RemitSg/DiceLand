using System;
using System.Collections.Generic;
using UnityEngine;

public static class BattleRelicDropRollService
{
    public static bool RollTableRelicDrops(
        LootTable_SO lootTable,
        System.Random rng,
        bool useEliteRelicChoice,
        bool createdEliteRelicChoice,
        ICollection<Relic_SO> ownedRelics,
        int choiceCount,
        IDictionary<Relic_SO, int> aggregatedRelics,
        Action<BattleSettlementRelicChoiceData> applyRelicChoice)
    {
        if (lootTable == null || aggregatedRelics == null)
            return createdEliteRelicChoice;

        IReadOnlyList<LootTable_SO.RelicDropEntry> relicDrops = lootTable.RelicDrops;
        if (relicDrops == null)
            return createdEliteRelicChoice;

        System.Random resolvedRng = rng ?? new System.Random();
        bool choiceCreated = createdEliteRelicChoice;
        for (int i = 0; i < relicDrops.Count; i++)
        {
            LootTable_SO.RelicDropEntry drop = relicDrops[i];
            if (drop == null || drop.relicTable == null || drop.dropChance <= 0f)
                continue;

            if (resolvedRng.NextDouble() > Mathf.Clamp01(drop.dropChance))
                continue;

            int amount = drop.minAmount;
            if (drop.maxAmount > drop.minAmount)
                amount = resolvedRng.Next(drop.minAmount, drop.maxAmount + 1);

            if (amount <= 0)
                continue;

            for (int rewardIndex = 0; rewardIndex < amount; rewardIndex++)
            {
                if (useEliteRelicChoice)
                {
                    if (!choiceCreated &&
                        BattleEliteRelicChoiceBuilderService.TryBuildFromTable(
                            out BattleSettlementRelicChoiceData choiceData,
                            drop.relicTable,
                            resolvedRng,
                            ownedRelics,
                            choiceCount))
                    {
                        choiceCreated = true;
                        applyRelicChoice?.Invoke(choiceData);
                    }

                    continue;
                }

                Relic_SO rolledRelic = drop.relicTable.DrawRandomRelic(resolvedRng, ownedRelics);
                if (rolledRelic == null)
                    continue;

                if (aggregatedRelics.ContainsKey(rolledRelic))
                    aggregatedRelics[rolledRelic] += 1;
                else
                    aggregatedRelics.Add(rolledRelic, 1);
            }
        }

        return choiceCreated;
    }
}
