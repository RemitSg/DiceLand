using System;
using System.Collections.Generic;
using UnityEngine;

public static class MerchantOfferSelectionService
{
    public static int ResolveTargetTotalCount(MerchantCatalog_SO catalog, int totalAvailableCount, System.Random rng)
    {
        if (catalog == null || totalAvailableCount <= 0)
            return 0;

        int minimumRequiredCount =
            Mathf.Max(0, catalog.lootMinimumCount != null ? catalog.lootMinimumCount.ResolveMinimumCount() : 0) +
            Mathf.Max(0, catalog.weaponMinimumCount != null ? catalog.weaponMinimumCount.ResolveMinimumCount() : 0) +
            Mathf.Max(0, catalog.blueprintMinimumCount != null ? catalog.blueprintMinimumCount.ResolveMinimumCount() : 0) +
            Mathf.Max(0, catalog.relicMinimumCount != null ? catalog.relicMinimumCount.ResolveMinimumCount() : 0);

        int minCount = Mathf.Clamp(Mathf.Max(minimumRequiredCount, catalog.minTotalGoodsCount), 0, totalAvailableCount);
        int maxCount = Mathf.Clamp(Mathf.Max(minCount, catalog.maxTotalGoodsCount), minCount, totalAvailableCount);
        return maxCount <= minCount || rng == null ? minCount : rng.Next(minCount, maxCount + 1);
    }

    public static int GetAvailableCategoryWeight<TEntry>(
        IReadOnlyList<TEntry> pool,
        MerchantCatalog_SO.CategoryMinimumEntry categoryEntry)
    {
        if (pool == null || pool.Count == 0 || categoryEntry == null)
            return 0;

        return Mathf.Max(0, categoryEntry.ResolveRandomWeight());
    }

    public static int RollWeightedEntryIndex<TEntry>(
        IReadOnlyList<TEntry> pool,
        Func<TEntry, int> weightSelector,
        System.Random rng)
    {
        int totalWeight = 0;
        for (int i = 0; i < pool.Count; i++)
            totalWeight += Mathf.Max(1, weightSelector(pool[i]));

        int roll = rng.Next(0, totalWeight);
        for (int i = 0; i < pool.Count; i++)
        {
            roll -= Mathf.Max(1, weightSelector(pool[i]));
            if (roll < 0)
                return i;
        }

        return Mathf.Clamp(pool.Count - 1, 0, int.MaxValue);
    }

    public static void ShuffleOffers(List<MerchantOfferState> offers, System.Random rng)
    {
        if (offers == null || rng == null)
            return;

        for (int i = offers.Count - 1; i > 0; i--)
        {
            int swapIndex = rng.Next(0, i + 1);
            MerchantOfferState temp = offers[i];
            offers[i] = offers[swapIndex];
            offers[swapIndex] = temp;
        }
    }
}
