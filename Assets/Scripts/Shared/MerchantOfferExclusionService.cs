using System.Collections.Generic;

public static class MerchantOfferExclusionService
{
    public static HashSet<Relic_SO> BuildOwnedRelicSet(RunState runState)
    {
        HashSet<Relic_SO> owned = new HashSet<Relic_SO>();
        if (runState == null || runState.relicInventory == null)
            return owned;

        for (int i = 0; i < runState.relicInventory.Count; i++)
        {
            RunRelicState relic = runState.relicInventory[i];
            if (relic == null || relic.relicData == null || relic.amount <= 0)
                continue;

            owned.Add(relic.relicData);
        }

        return owned;
    }

    public static HashSet<Weapon_SO> BuildOwnedWeaponSet(RunState runState)
    {
        HashSet<Weapon_SO> owned = new HashSet<Weapon_SO>();
        if (runState == null || runState.weaponInventory == null)
            return owned;

        for (int i = 0; i < runState.weaponInventory.Count; i++)
        {
            RunWeaponState weapon = runState.weaponInventory[i];
            if (weapon == null || weapon.weaponData == null || weapon.amount <= 0)
                continue;

            owned.Add(weapon.weaponData);
        }

        return owned;
    }

    public static HashSet<CraftingBlueprint_SO> BuildOwnedBlueprintSet(RunState runState)
    {
        HashSet<CraftingBlueprint_SO> owned = new HashSet<CraftingBlueprint_SO>();
        if (runState == null || runState.blueprintInventory == null)
            return owned;

        for (int i = 0; i < runState.blueprintInventory.Count; i++)
        {
            RunBlueprintState blueprint = runState.blueprintInventory[i];
            if (blueprint == null || blueprint.blueprintData == null || blueprint.amount <= 0)
                continue;

            owned.Add(blueprint.blueprintData);
        }

        return owned;
    }

    public static HashSet<CraftingBlueprint_SO> BuildUnavailableBlueprintSet(
        RunState runState,
        IReadOnlyList<MerchantCatalog_SO.BlueprintGoodsEntry> blueprintEntries)
    {
        HashSet<CraftingBlueprint_SO> unavailable = BuildOwnedBlueprintSet(runState);
        AppendBlueprintsForOwnedWeapons(runState, blueprintEntries, unavailable);
        return unavailable;
    }

    public static void AppendBlueprintsForOwnedWeapons(
        RunState runState,
        IReadOnlyList<MerchantCatalog_SO.BlueprintGoodsEntry> blueprintEntries,
        HashSet<CraftingBlueprint_SO> targetSet)
    {
        if (runState == null || blueprintEntries == null || targetSet == null)
            return;

        HashSet<Weapon_SO> ownedWeapons = BuildOwnedWeaponSet(runState);
        if (ownedWeapons.Count == 0)
            return;

        for (int i = 0; i < blueprintEntries.Count; i++)
        {
            MerchantCatalog_SO.BlueprintGoodsEntry entry = blueprintEntries[i];
            CraftingBlueprint_SO blueprint = entry != null ? entry.blueprintData : null;
            if (blueprint == null || blueprint.targetWeapon == null)
                continue;

            if (ownedWeapons.Contains(blueprint.targetWeapon))
                targetSet.Add(blueprint);
        }
    }

    public static HashSet<LootItem_SO> BuildCurrentMerchantLootSet(
        BoardTileData tile,
        MerchantOfferState ignoreOffer,
        bool includeSoldOut = false)
    {
        HashSet<LootItem_SO> offered = new HashSet<LootItem_SO>();
        if (tile == null || tile.merchantOffers == null)
            return offered;

        for (int i = 0; i < tile.merchantOffers.Count; i++)
        {
            MerchantOfferState offer = tile.merchantOffers[i];
            if (offer == null || offer == ignoreOffer || (!includeSoldOut && offer.isSoldOut) || offer.itemType != MerchantItemType.Loot || offer.lootItem == null)
                continue;

            offered.Add(offer.lootItem);
        }

        return offered;
    }

    public static void AppendCurrentMerchantWeapons(
        BoardTileData tile,
        HashSet<Weapon_SO> targetSet,
        MerchantOfferState ignoreOffer,
        bool includeSoldOut = false)
    {
        if (tile == null || tile.merchantOffers == null || targetSet == null)
            return;

        for (int i = 0; i < tile.merchantOffers.Count; i++)
        {
            MerchantOfferState offer = tile.merchantOffers[i];
            if (offer == null || offer == ignoreOffer || (!includeSoldOut && offer.isSoldOut) || offer.itemType != MerchantItemType.Weapon || offer.weaponData == null)
                continue;

            targetSet.Add(offer.weaponData);
        }
    }

    public static void AppendCurrentMerchantBlueprints(
        BoardTileData tile,
        HashSet<CraftingBlueprint_SO> targetSet,
        MerchantOfferState ignoreOffer,
        bool includeSoldOut = false)
    {
        if (tile == null || tile.merchantOffers == null || targetSet == null)
            return;

        for (int i = 0; i < tile.merchantOffers.Count; i++)
        {
            MerchantOfferState offer = tile.merchantOffers[i];
            if (offer == null || offer == ignoreOffer || (!includeSoldOut && offer.isSoldOut) || offer.itemType != MerchantItemType.Blueprint || offer.blueprintData == null)
                continue;

            targetSet.Add(offer.blueprintData);
        }
    }

    public static void AppendCurrentMerchantRelics(
        BoardTileData tile,
        HashSet<Relic_SO> targetSet,
        MerchantOfferState ignoreOffer,
        bool includeSoldOut = false)
    {
        if (tile == null || tile.merchantOffers == null || targetSet == null)
            return;

        for (int i = 0; i < tile.merchantOffers.Count; i++)
        {
            MerchantOfferState offer = tile.merchantOffers[i];
            if (offer == null || offer == ignoreOffer || (!includeSoldOut && offer.isSoldOut) || offer.itemType != MerchantItemType.Relic || offer.relicData == null)
                continue;

            targetSet.Add(offer.relicData);
        }
    }
}
