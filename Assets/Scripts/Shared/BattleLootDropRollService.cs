using System.Collections.Generic;
using UnityEngine;

public static class BattleLootDropRollService
{
    public static void RollTableLootDrops(
        LootTable_SO lootTable,
        System.Random rng,
        IDictionary<LootItem_SO, int> aggregated,
        IList<LootItem_SO> lootPool)
    {
        if (lootTable == null || aggregated == null || lootPool == null)
            return;

        IReadOnlyList<LootTable_SO.LootDropEntry> drops = lootTable.Drops;
        if (drops == null)
            return;

        System.Random resolvedRng = rng ?? new System.Random();
        for (int i = 0; i < drops.Count; i++)
        {
            LootTable_SO.LootDropEntry drop = drops[i];
            if (drop == null || drop.lootItem == null || drop.dropChance <= 0f)
                continue;

            if (!lootPool.Contains(drop.lootItem))
                lootPool.Add(drop.lootItem);

            if (resolvedRng.NextDouble() > Mathf.Clamp01(drop.dropChance))
                continue;

            int amount = drop.minAmount;
            if (drop.maxAmount > drop.minAmount)
                amount = resolvedRng.Next(drop.minAmount, drop.maxAmount + 1);

            if (amount <= 0)
                continue;

            if (aggregated.ContainsKey(drop.lootItem))
                aggregated[drop.lootItem] += amount;
            else
                aggregated.Add(drop.lootItem, amount);
        }
    }
}
