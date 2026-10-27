using System.Collections.Generic;
using UnityEngine;

public static class MerchantStockService
{
    public static System.Random CreateRandom(BoardTileData tile, int refreshSalt)
    {
        unchecked { return new System.Random(((tile != null ? tile.encounterSeed : 0) * 397) ^ 0x2A57C9 ^ (refreshSalt * 486187739)); }
    }

    public static void EnsureStock(BoardTileData tile, MerchantCatalog_SO catalog, RunState state, DifficultyConfig_SO difficulty)
    {
        if (tile == null || tile.tileType != MapTileType.Merchant || tile.merchantStockGenerated || catalog == null) return;
        if (tile.sourceSpecialDungeon != null) { tile.merchantStockGenerated = true; return; }
        TryGenerateInitialStock(tile, catalog, state, difficulty, CreateRandom(tile, tile.merchantRefreshCount));
    }

    public static int RefreshCost(RunState state, BoardTileData tile, DifficultyConfig_SO difficulty)
    {
        int baseCost = difficulty != null ? Mathf.Max(0, difficulty.GetMerchantRefreshCost(tile != null ? Mathf.Max(0, tile.merchantRefreshCount) : 0)) : 0;
        return AbilityRunBonusRuntimeService.ResolveMerchantRefreshCost(state, tile, baseCost);
    }

    public static bool TryRefreshStock(RunState state, BoardTileData tile, MerchantCatalog_SO catalog, DifficultyConfig_SO difficulty, out string message)
    {
        message = "当前无法刷新商店。";
        if (state == null || tile == null || tile.tileType != MapTileType.Merchant || catalog == null) return false;
        if (tile.sourceSpecialDungeon != null) { message = "特殊副本商店商品固定，无法刷新。"; return false; }
        if (tile.merchantOffers == null || tile.merchantOffers.Count == 0) { message = "当前没有可刷新的商品。"; return false; }
        int cost = RefreshCost(state, tile, difficulty);
        if (state.money < cost) { message = $"金钱不足，需要 {cost} 金钱。"; return false; }
        int total = CountRefreshableOffers(tile);
        int refreshed = RefreshOffers(tile, catalog, state, difficulty, CreateRandom(tile, tile.merchantRefreshCount + 1));
        if (refreshed <= 0) { message = "当前没有可用的新商品可刷新。"; return false; }
        if (!state.TrySpendMoney(cost)) { message = $"金钱不足，需要 {cost} 金钱。"; return false; }
        tile.merchantRefreshCount++;
        message = refreshed >= total ? $"刷新成功：替换了 {refreshed} 个商品。" : $"部分刷新成功：替换了 {refreshed}/{total} 个商品。";
        return true;
    }

    public static bool TryGenerateInitialStock(
        BoardTileData tile,
        MerchantCatalog_SO catalog,
        RunState runState,
        DifficultyConfig_SO merchantDifficulty,
        System.Random rng)
    {
        if (tile == null || catalog == null || rng == null)
            return false;

        IReadOnlyList<MerchantCatalog_SO.LootGoodsEntry> validLootEntries = catalog.GetValidLootEntries();
        IReadOnlyList<MerchantCatalog_SO.WeaponGoodsEntry> validWeaponEntries = catalog.GetValidWeaponEntries();
        IReadOnlyList<MerchantCatalog_SO.BlueprintGoodsEntry> validBlueprintEntries = catalog.GetValidBlueprintEntries();
        IReadOnlyList<MerchantCatalog_SO.RelicGoodsEntry> validRelicEntries = catalog.GetValidRelicEntries();
        HashSet<Weapon_SO> excludedWeapons = MerchantOfferExclusionService.BuildOwnedWeaponSet(runState);
        HashSet<CraftingBlueprint_SO> excludedBlueprints = MerchantOfferExclusionService.BuildUnavailableBlueprintSet(runState, validBlueprintEntries);
        HashSet<Relic_SO> excludedRelics = MerchantOfferExclusionService.BuildOwnedRelicSet(runState);
        List<MerchantCatalog_SO.WeaponGoodsEntry> filteredWeaponEntries =
            RewardCandidateFilterService.BuildAvailableMerchantWeaponGoodsEntries(validWeaponEntries, excludedWeapons);
        List<MerchantCatalog_SO.BlueprintGoodsEntry> filteredBlueprintEntries =
            RewardCandidateFilterService.BuildAvailableMerchantBlueprintGoodsEntries(validBlueprintEntries, excludedBlueprints);
        List<MerchantCatalog_SO.RelicGoodsEntry> filteredRelicEntries =
            RewardCandidateFilterService.BuildAvailableMerchantRelicGoodsEntries(validRelicEntries, excludedRelics);

        bool hasAnyValidEntries =
            (validLootEntries != null && validLootEntries.Count > 0) ||
            (filteredWeaponEntries != null && filteredWeaponEntries.Count > 0) ||
            (filteredBlueprintEntries != null && filteredBlueprintEntries.Count > 0) ||
            (filteredRelicEntries != null && filteredRelicEntries.Count > 0);

        if (!hasAnyValidEntries)
        {
            tile.merchantOffers = new List<MerchantOfferState>();
            tile.merchantStockGenerated = true;
            return true;
        }

        List<MerchantOfferState> generated = new List<MerchantOfferState>();
        List<MerchantCatalog_SO.LootGoodsEntry> remainingLootEntries = new List<MerchantCatalog_SO.LootGoodsEntry>(validLootEntries);
        List<MerchantCatalog_SO.WeaponGoodsEntry> remainingWeaponEntries = filteredWeaponEntries != null
            ? new List<MerchantCatalog_SO.WeaponGoodsEntry>(filteredWeaponEntries)
            : new List<MerchantCatalog_SO.WeaponGoodsEntry>();
        List<MerchantCatalog_SO.BlueprintGoodsEntry> remainingBlueprintEntries = filteredBlueprintEntries != null
            ? new List<MerchantCatalog_SO.BlueprintGoodsEntry>(filteredBlueprintEntries)
            : new List<MerchantCatalog_SO.BlueprintGoodsEntry>();
        List<MerchantCatalog_SO.RelicGoodsEntry> remainingRelicEntries = filteredRelicEntries != null
            ? new List<MerchantCatalog_SO.RelicGoodsEntry>(filteredRelicEntries)
            : new List<MerchantCatalog_SO.RelicGoodsEntry>();

        int targetTotalCount = MerchantOfferSelectionService.ResolveTargetTotalCount(
            catalog,
            remainingLootEntries.Count + remainingWeaponEntries.Count + remainingBlueprintEntries.Count + remainingRelicEntries.Count,
            rng);
        targetTotalCount = Mathf.Min(
            remainingLootEntries.Count + remainingWeaponEntries.Count + remainingBlueprintEntries.Count + remainingRelicEntries.Count,
            RelicMerchantService.CalculateMerchantStockTargetCount(runState, tile, targetTotalCount));

        AddGuaranteedMerchantOffersFromLootPool(generated, remainingLootEntries, catalog.lootMinimumCount, rng);
        AddGuaranteedMerchantOffersFromWeaponPool(generated, remainingWeaponEntries, catalog.weaponMinimumCount, rng);
        AddGuaranteedMerchantOffersFromBlueprintPool(generated, remainingBlueprintEntries, catalog.blueprintMinimumCount, rng);
        AddGuaranteedMerchantOffersFromRelicPool(generated, remainingRelicEntries, catalog.relicMinimumCount, excludedRelics, merchantDifficulty, rng);

        FillRemainingMerchantOffers(
            generated,
            remainingLootEntries,
            remainingWeaponEntries,
            remainingBlueprintEntries,
            remainingRelicEntries,
            catalog,
            targetTotalCount,
            excludedRelics,
            merchantDifficulty,
            rng);

        MerchantOfferSelectionService.ShuffleOffers(generated, rng);
        tile.merchantOffers = generated;
        tile.merchantStockGenerated = true;
        return true;
    }

    public static int CountRefreshableOffers(BoardTileData tile)
    {
        if (tile == null || tile.merchantOffers == null)
            return 0;

        int count = 0;
        for (int i = 0; i < tile.merchantOffers.Count; i++)
        {
            if (tile.merchantOffers[i] != null)
                count += 1;
        }

        return count;
    }

    public static int RefreshOffers(
        BoardTileData tile,
        MerchantCatalog_SO catalog,
        RunState runState,
        DifficultyConfig_SO merchantDifficulty,
        System.Random rng)
    {
        if (tile == null || tile.merchantOffers == null || catalog == null || rng == null)
            return 0;

        HashSet<LootItem_SO> excludedLoot = MerchantOfferExclusionService.BuildCurrentMerchantLootSet(tile, null, true);
        HashSet<Weapon_SO> excludedWeapons = MerchantOfferExclusionService.BuildOwnedWeaponSet(runState);
        MerchantOfferExclusionService.AppendCurrentMerchantWeapons(tile, excludedWeapons, null, true);
        IReadOnlyList<MerchantCatalog_SO.BlueprintGoodsEntry> validBlueprintEntries = catalog.GetValidBlueprintEntries();
        HashSet<CraftingBlueprint_SO> excludedBlueprints = MerchantOfferExclusionService.BuildUnavailableBlueprintSet(runState, validBlueprintEntries);
        MerchantOfferExclusionService.AppendCurrentMerchantBlueprints(tile, excludedBlueprints, null, true);
        HashSet<Relic_SO> excludedRelics = MerchantOfferExclusionService.BuildOwnedRelicSet(runState);
        MerchantOfferExclusionService.AppendCurrentMerchantRelics(tile, excludedRelics, null, true);

        int refreshedCount = 0;
        for (int i = 0; i < tile.merchantOffers.Count; i++)
        {
            MerchantOfferState offer = tile.merchantOffers[i];
            if (offer == null)
                continue;

            if (TryRefreshOffer(catalog, merchantDifficulty, offer, excludedLoot, excludedWeapons, excludedBlueprints, excludedRelics, rng))
                refreshedCount += 1;
        }

        return refreshedCount;
    }

    private static void AddGuaranteedMerchantOffersFromLootPool(
        List<MerchantOfferState> generated,
        List<MerchantCatalog_SO.LootGoodsEntry> remainingEntries,
        MerchantCatalog_SO.CategoryMinimumEntry minimumEntry,
        System.Random rng)
    {
        if (generated == null || remainingEntries == null || remainingEntries.Count == 0 || minimumEntry == null)
            return;

        int targetCount = Mathf.Clamp(minimumEntry.ResolveMinimumCount(), 0, remainingEntries.Count);
        for (int i = 0; i < targetCount && remainingEntries.Count > 0; i++)
        {
            int selectedIndex = MerchantOfferSelectionService.RollWeightedEntryIndex(remainingEntries, entry => entry.weight, rng);
            MerchantCatalog_SO.LootGoodsEntry goods = remainingEntries[selectedIndex];
            remainingEntries.RemoveAt(selectedIndex);
            if (MerchantOfferFactoryService.TryCreateLootOffer(goods, rng, out MerchantOfferState offer))
                generated.Add(offer);
        }
    }

    private static void AddGuaranteedMerchantOffersFromWeaponPool(
        List<MerchantOfferState> generated,
        List<MerchantCatalog_SO.WeaponGoodsEntry> remainingEntries,
        MerchantCatalog_SO.CategoryMinimumEntry minimumEntry,
        System.Random rng)
    {
        if (generated == null || remainingEntries == null || remainingEntries.Count == 0 || minimumEntry == null)
            return;

        int targetCount = Mathf.Clamp(minimumEntry.ResolveMinimumCount(), 0, remainingEntries.Count);
        for (int i = 0; i < targetCount && remainingEntries.Count > 0; i++)
        {
            int selectedIndex = MerchantOfferSelectionService.RollWeightedEntryIndex(remainingEntries, entry => entry.weight, rng);
            MerchantCatalog_SO.WeaponGoodsEntry goods = remainingEntries[selectedIndex];
            remainingEntries.RemoveAt(selectedIndex);
            if (MerchantOfferFactoryService.TryCreateWeaponOffer(goods, rng, out MerchantOfferState offer))
                generated.Add(offer);
        }
    }

    private static void AddGuaranteedMerchantOffersFromBlueprintPool(
        List<MerchantOfferState> generated,
        List<MerchantCatalog_SO.BlueprintGoodsEntry> remainingEntries,
        MerchantCatalog_SO.CategoryMinimumEntry minimumEntry,
        System.Random rng)
    {
        if (generated == null || remainingEntries == null || remainingEntries.Count == 0 || minimumEntry == null)
            return;

        int targetCount = Mathf.Clamp(minimumEntry.ResolveMinimumCount(), 0, remainingEntries.Count);
        for (int i = 0; i < targetCount && remainingEntries.Count > 0; i++)
        {
            int selectedIndex = MerchantOfferSelectionService.RollWeightedEntryIndex(remainingEntries, entry => entry.weight, rng);
            MerchantCatalog_SO.BlueprintGoodsEntry goods = remainingEntries[selectedIndex];
            remainingEntries.RemoveAt(selectedIndex);
            if (MerchantOfferFactoryService.TryCreateBlueprintOffer(goods, rng, out MerchantOfferState offer))
                generated.Add(offer);
        }
    }

    private static void AddGuaranteedMerchantOffersFromRelicPool(
        List<MerchantOfferState> generated,
        List<MerchantCatalog_SO.RelicGoodsEntry> remainingEntries,
        MerchantCatalog_SO.CategoryMinimumEntry minimumEntry,
        HashSet<Relic_SO> excludedRelics,
        DifficultyConfig_SO merchantDifficulty,
        System.Random rng)
    {
        if (generated == null || remainingEntries == null || remainingEntries.Count == 0 || minimumEntry == null)
            return;

        int targetCount = Mathf.Clamp(minimumEntry.ResolveMinimumCount(), 0, remainingEntries.Count);
        for (int i = 0; i < targetCount && remainingEntries.Count > 0; i++)
        {
            List<MerchantCatalog_SO.RelicGoodsEntry> availableEntries =
                RewardCandidateFilterService.BuildAvailableMerchantRelicGoodsEntries(remainingEntries, excludedRelics);
            if (availableEntries.Count <= 0)
                break;

            int selectedIndex = MerchantOfferSelectionService.RollWeightedEntryIndex(availableEntries, entry => entry.weight, rng);
            MerchantCatalog_SO.RelicGoodsEntry goods = availableEntries[selectedIndex];
            remainingEntries.Remove(goods);

            if (MerchantOfferFactoryService.TryCreateRelicOffer(goods, excludedRelics, merchantDifficulty, rng, out MerchantOfferState relicOffer))
                generated.Add(relicOffer);
        }
    }

    private static void FillRemainingMerchantOffers(
        List<MerchantOfferState> generated,
        List<MerchantCatalog_SO.LootGoodsEntry> remainingLootEntries,
        List<MerchantCatalog_SO.WeaponGoodsEntry> remainingWeaponEntries,
        List<MerchantCatalog_SO.BlueprintGoodsEntry> remainingBlueprintEntries,
        List<MerchantCatalog_SO.RelicGoodsEntry> remainingRelicEntries,
        MerchantCatalog_SO catalog,
        int targetTotalCount,
        HashSet<Relic_SO> excludedRelics,
        DifficultyConfig_SO merchantDifficulty,
        System.Random rng)
    {
        if (generated == null || rng == null)
            return;

        while (generated.Count < targetTotalCount)
        {
            List<MerchantCatalog_SO.RelicGoodsEntry> availableRelicEntries =
                RewardCandidateFilterService.BuildAvailableMerchantRelicGoodsEntries(remainingRelicEntries, excludedRelics);
            int totalWeight = 0;
            int lootCategoryWeight = MerchantOfferSelectionService.GetAvailableCategoryWeight(remainingLootEntries, catalog != null ? catalog.lootMinimumCount : null);
            int weaponCategoryWeight = MerchantOfferSelectionService.GetAvailableCategoryWeight(remainingWeaponEntries, catalog != null ? catalog.weaponMinimumCount : null);
            int blueprintCategoryWeight = MerchantOfferSelectionService.GetAvailableCategoryWeight(remainingBlueprintEntries, catalog != null ? catalog.blueprintMinimumCount : null);
            int relicCategoryWeight = MerchantOfferSelectionService.GetAvailableCategoryWeight(availableRelicEntries, catalog != null ? catalog.relicMinimumCount : null);
            totalWeight += lootCategoryWeight;
            totalWeight += weaponCategoryWeight;
            totalWeight += blueprintCategoryWeight;
            totalWeight += relicCategoryWeight;

            if (totalWeight <= 0)
                break;

            int roll = rng.Next(0, totalWeight);
            if (roll < lootCategoryWeight)
            {
                int selectedIndex = MerchantOfferSelectionService.RollWeightedEntryIndex(remainingLootEntries, entry => entry.weight, rng);
                MerchantCatalog_SO.LootGoodsEntry goods = remainingLootEntries[selectedIndex];
                remainingLootEntries.RemoveAt(selectedIndex);
                if (MerchantOfferFactoryService.TryCreateLootOffer(goods, rng, out MerchantOfferState offer))
                    generated.Add(offer);
                continue;
            }

            roll -= lootCategoryWeight;
            if (roll < weaponCategoryWeight)
            {
                int selectedIndex = MerchantOfferSelectionService.RollWeightedEntryIndex(remainingWeaponEntries, entry => entry.weight, rng);
                MerchantCatalog_SO.WeaponGoodsEntry goods = remainingWeaponEntries[selectedIndex];
                remainingWeaponEntries.RemoveAt(selectedIndex);
                if (MerchantOfferFactoryService.TryCreateWeaponOffer(goods, rng, out MerchantOfferState offer))
                    generated.Add(offer);
                continue;
            }

            roll -= weaponCategoryWeight;
            if (roll < blueprintCategoryWeight)
            {
                int selectedIndex = MerchantOfferSelectionService.RollWeightedEntryIndex(remainingBlueprintEntries, entry => entry.weight, rng);
                MerchantCatalog_SO.BlueprintGoodsEntry goods = remainingBlueprintEntries[selectedIndex];
                remainingBlueprintEntries.RemoveAt(selectedIndex);
                if (MerchantOfferFactoryService.TryCreateBlueprintOffer(goods, rng, out MerchantOfferState offer))
                    generated.Add(offer);
                continue;
            }

            roll -= blueprintCategoryWeight;
            if (roll >= relicCategoryWeight || availableRelicEntries == null || availableRelicEntries.Count <= 0)
                break;

            int selectedRelicIndex = MerchantOfferSelectionService.RollWeightedEntryIndex(availableRelicEntries, entry => entry.weight, rng);
            MerchantCatalog_SO.RelicGoodsEntry relicGoods = availableRelicEntries[selectedRelicIndex];
            remainingRelicEntries.Remove(relicGoods);
            if (MerchantOfferFactoryService.TryCreateRelicOffer(relicGoods, excludedRelics, merchantDifficulty, rng, out MerchantOfferState relicOffer))
                generated.Add(relicOffer);
        }
    }

    private static bool TryRefreshOffer(
        MerchantCatalog_SO catalog,
        DifficultyConfig_SO merchantDifficulty,
        MerchantOfferState offer,
        HashSet<LootItem_SO> excludedLoot,
        HashSet<Weapon_SO> excludedWeapons,
        HashSet<CraftingBlueprint_SO> excludedBlueprints,
        HashSet<Relic_SO> excludedRelics,
        System.Random rng)
    {
        if (offer == null || catalog == null || rng == null)
            return false;

        switch (offer.itemType)
        {
            case MerchantItemType.Loot:
                return TryRefreshLootOffer(catalog, offer, excludedLoot, rng);
            case MerchantItemType.Weapon:
                return TryRefreshWeaponOffer(catalog, offer, excludedWeapons, rng);
            case MerchantItemType.Blueprint:
                return TryRefreshBlueprintOffer(catalog, offer, excludedBlueprints, rng);
            case MerchantItemType.Relic:
                return TryRefreshRelicOffer(catalog, merchantDifficulty, offer, excludedRelics, rng);
            default:
                return false;
        }
    }

    private static bool TryRefreshLootOffer(
        MerchantCatalog_SO catalog,
        MerchantOfferState offer,
        HashSet<LootItem_SO> excludedLoot,
        System.Random rng)
    {
        IReadOnlyList<MerchantCatalog_SO.LootGoodsEntry> validEntries = catalog.GetValidLootEntries();
        if (validEntries == null || validEntries.Count == 0)
            return false;

        List<MerchantCatalog_SO.LootGoodsEntry> candidates =
            RewardCandidateFilterService.BuildAvailableMerchantLootGoodsEntries(validEntries, excludedLoot);
        if (candidates.Count <= 0)
            return false;

        MerchantCatalog_SO.LootGoodsEntry selected =
            candidates[MerchantOfferSelectionService.RollWeightedEntryIndex(candidates, entry => entry.weight, rng)];
        if (!MerchantOfferFactoryService.TryCreateLootOffer(selected, rng, out MerchantOfferState refreshedOffer))
            return false;

        MerchantOfferFactoryService.ApplyOfferData(offer, refreshedOffer);
        excludedLoot?.Add(selected.lootItem);
        return true;
    }

    private static bool TryRefreshWeaponOffer(
        MerchantCatalog_SO catalog,
        MerchantOfferState offer,
        HashSet<Weapon_SO> excludedWeapons,
        System.Random rng)
    {
        IReadOnlyList<MerchantCatalog_SO.WeaponGoodsEntry> validEntries = catalog.GetValidWeaponEntries();
        if (validEntries == null || validEntries.Count == 0)
            return false;

        List<MerchantCatalog_SO.WeaponGoodsEntry> candidates =
            RewardCandidateFilterService.BuildAvailableMerchantWeaponGoodsEntries(validEntries, excludedWeapons);
        if (candidates.Count <= 0)
            return false;

        MerchantCatalog_SO.WeaponGoodsEntry selected =
            candidates[MerchantOfferSelectionService.RollWeightedEntryIndex(candidates, entry => entry.weight, rng)];
        if (!MerchantOfferFactoryService.TryCreateWeaponOffer(selected, rng, out MerchantOfferState refreshedOffer))
            return false;

        MerchantOfferFactoryService.ApplyOfferData(offer, refreshedOffer);
        excludedWeapons?.Add(selected.weaponData);
        return true;
    }

    private static bool TryRefreshBlueprintOffer(
        MerchantCatalog_SO catalog,
        MerchantOfferState offer,
        HashSet<CraftingBlueprint_SO> excludedBlueprints,
        System.Random rng)
    {
        IReadOnlyList<MerchantCatalog_SO.BlueprintGoodsEntry> validEntries = catalog.GetValidBlueprintEntries();
        if (validEntries == null || validEntries.Count == 0)
            return false;

        List<MerchantCatalog_SO.BlueprintGoodsEntry> candidates =
            RewardCandidateFilterService.BuildAvailableMerchantBlueprintGoodsEntries(validEntries, excludedBlueprints);
        if (candidates.Count <= 0)
            return false;

        MerchantCatalog_SO.BlueprintGoodsEntry selected =
            candidates[MerchantOfferSelectionService.RollWeightedEntryIndex(candidates, entry => entry.weight, rng)];
        if (!MerchantOfferFactoryService.TryCreateBlueprintOffer(selected, rng, out MerchantOfferState refreshedOffer))
            return false;

        MerchantOfferFactoryService.ApplyOfferData(offer, refreshedOffer);
        excludedBlueprints?.Add(selected.blueprintData);
        return true;
    }

    private static bool TryRefreshRelicOffer(
        MerchantCatalog_SO catalog,
        DifficultyConfig_SO merchantDifficulty,
        MerchantOfferState offer,
        HashSet<Relic_SO> excludedRelics,
        System.Random rng)
    {
        IReadOnlyList<MerchantCatalog_SO.RelicGoodsEntry> validEntries = catalog.GetValidRelicEntries();
        if (validEntries == null || validEntries.Count == 0)
            return false;

        List<MerchantCatalog_SO.RelicGoodsEntry> candidates =
            RewardCandidateFilterService.BuildAvailableMerchantRelicGoodsEntries(validEntries, excludedRelics);
        if (candidates.Count <= 0)
            return false;

        MerchantCatalog_SO.RelicGoodsEntry selected =
            candidates[MerchantOfferSelectionService.RollWeightedEntryIndex(candidates, entry => entry.weight, rng)];
        if (!MerchantOfferFactoryService.TryCreateRelicOffer(selected, excludedRelics, merchantDifficulty, rng, out MerchantOfferState refreshedOffer))
            return false;

        MerchantOfferFactoryService.ApplyOfferData(offer, refreshedOffer);
        return true;
    }
}
