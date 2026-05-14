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

    public static bool TryForceOneTableLootDrop(
        LootTable_SO lootTable,
        System.Random rng,
        IDictionary<LootItem_SO, int> aggregated,
        IList<LootItem_SO> lootPool)
    {
        if (lootTable == null || aggregated == null || lootPool == null)
            return false;

        IReadOnlyList<LootTable_SO.LootDropEntry> drops = lootTable.Drops;
        if (drops == null || drops.Count == 0)
            return false;

        List<LootTable_SO.LootDropEntry> candidates = new List<LootTable_SO.LootDropEntry>();
        for (int i = 0; i < drops.Count; i++)
        {
            LootTable_SO.LootDropEntry drop = drops[i];
            if (drop != null && drop.lootItem != null && drop.maxAmount > 0)
                candidates.Add(drop);
        }

        if (candidates.Count == 0)
            return false;

        System.Random resolvedRng = rng ?? new System.Random();
        LootTable_SO.LootDropEntry selected = candidates[resolvedRng.Next(candidates.Count)];
        if (!lootPool.Contains(selected.lootItem))
            lootPool.Add(selected.lootItem);

        int amount = Mathf.Max(1, selected.minAmount);
        if (selected.maxAmount > selected.minAmount)
            amount = resolvedRng.Next(selected.minAmount, selected.maxAmount + 1);

        if (aggregated.ContainsKey(selected.lootItem))
            aggregated[selected.lootItem] += amount;
        else
            aggregated.Add(selected.lootItem, amount);

        return true;
    }
}
