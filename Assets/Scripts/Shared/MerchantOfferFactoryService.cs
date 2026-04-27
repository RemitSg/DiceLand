using UnityEngine;

public static class MerchantOfferFactoryService
{
    public static bool TryCreateLootOffer(
        MerchantCatalog_SO.LootGoodsEntry goods,
        System.Random rng,
        out MerchantOfferState offer)
    {
        offer = null;
        if (goods == null || goods.lootItem == null || rng == null)
            return false;

        offer = new MerchantOfferState
        {
            itemType = MerchantItemType.Loot,
            lootItem = goods.lootItem,
            price = RollPrice(goods.minPrice, goods.maxPrice, rng),
            isSoldOut = false
        };
        return true;
    }

    public static bool TryCreateWeaponOffer(
        MerchantCatalog_SO.WeaponGoodsEntry goods,
        System.Random rng,
        out MerchantOfferState offer)
    {
        offer = null;
        if (goods == null || goods.weaponData == null || rng == null)
            return false;

        offer = new MerchantOfferState
        {
            itemType = MerchantItemType.Weapon,
            weaponData = goods.weaponData,
            price = RollPrice(goods.minPrice, goods.maxPrice, rng),
            isSoldOut = false
        };
        return true;
    }

    public static bool TryCreateBlueprintOffer(
        MerchantCatalog_SO.BlueprintGoodsEntry goods,
        System.Random rng,
        out MerchantOfferState offer)
    {
        offer = null;
        if (goods == null || goods.blueprintData == null || rng == null)
            return false;

        offer = new MerchantOfferState
        {
            itemType = MerchantItemType.Blueprint,
            blueprintData = goods.blueprintData,
            price = RollPrice(goods.minPrice, goods.maxPrice, rng),
            isSoldOut = false
        };
        return true;
    }

    public static bool TryCreateRelicOffer(
        MerchantCatalog_SO.RelicGoodsEntry goods,
        System.Collections.Generic.ICollection<Relic_SO> excludedRelics,
        DifficultyConfig_SO merchantDifficulty,
        System.Random rng,
        out MerchantOfferState offer)
    {
        offer = null;
        if (goods == null || rng == null)
            return false;

        Relic_SO relicData = null;
        if (goods.relicTable != null && goods.relicTable.IsValid)
            relicData = goods.relicTable.DrawRandomRelic(rng, excludedRelics);
        else
            relicData = goods.LegacyRelicData;

        if (relicData == null)
            return false;
        if (excludedRelics != null && excludedRelics.Contains(relicData))
            return false;

        excludedRelics?.Add(relicData);
        offer = new MerchantOfferState
        {
            itemType = MerchantItemType.Relic,
            relicData = relicData,
            price = ResolveRelicPrice(merchantDifficulty, relicData, rng),
            isSoldOut = false
        };
        return true;
    }

    public static void ApplyOfferData(MerchantOfferState target, MerchantOfferState source)
    {
        if (target == null || source == null)
            return;

        target.itemType = source.itemType;
        target.lootItem = source.lootItem;
        target.weaponData = source.weaponData;
        target.blueprintData = source.blueprintData;
        target.relicData = source.relicData;
        target.price = Mathf.Max(0, source.price);
        target.isSoldOut = false;
    }

    public static int RollPrice(int minPrice, int maxPrice, System.Random rng)
    {
        int min = Mathf.Max(1, minPrice);
        int max = Mathf.Max(min, maxPrice);
        return max <= min || rng == null ? min : rng.Next(min, max + 1);
    }

    public static int ResolveRelicPrice(DifficultyConfig_SO merchantDifficulty, Relic_SO relicData, System.Random rng)
    {
        if (relicData == null)
            return 0;

        return merchantDifficulty != null
            ? Mathf.Max(0, merchantDifficulty.GetMerchantRelicPrice(relicData.rarity, rng))
            : 0;
    }
}
