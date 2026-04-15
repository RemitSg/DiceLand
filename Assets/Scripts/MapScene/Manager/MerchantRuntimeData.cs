using System;
using UnityEngine;

[Serializable]
public sealed class MerchantOfferState
{
    public MerchantItemType itemType;
    public LootItem_SO lootItem;
    public Weapon_SO weaponData;
    public CraftingBlueprint_SO blueprintData;
    public Relic_SO relicData;
    public int price;
    public bool isSoldOut;

    public string DisplayName
    {
        get
        {
            switch (itemType)
            {
                case MerchantItemType.Loot:
                    return lootItem != null ? lootItem.lootName : "Loot";
                case MerchantItemType.Weapon:
                    return weaponData != null ? weaponData.weaponName : "Weapon";
                case MerchantItemType.Blueprint:
                    return blueprintData != null ? blueprintData.DisplayName : "Blueprint";
                case MerchantItemType.Relic:
                    return relicData != null ? relicData.DisplayName : "Relic";
                default:
                    return "Goods";
            }
        }
    }

    public string Description
    {
        get
        {
            switch (itemType)
            {
                case MerchantItemType.Loot:
                    return lootItem != null ? lootItem.description : string.Empty;
                case MerchantItemType.Weapon:
                    return weaponData != null ? weaponData.description : string.Empty;
                case MerchantItemType.Blueprint:
                    return blueprintData != null ? blueprintData.description : string.Empty;
                case MerchantItemType.Relic:
                    return relicData != null ? relicData.description : string.Empty;
                default:
                    return string.Empty;
            }
        }
    }

    public Sprite DisplayIcon
    {
        get
        {
            switch (itemType)
            {
                case MerchantItemType.Loot:
                    return lootItem != null ? lootItem.icon : null;
                case MerchantItemType.Weapon:
                    return weaponData != null ? weaponData.icon : null;
                case MerchantItemType.Blueprint:
                    return blueprintData != null ? blueprintData.DisplayIcon : null;
                case MerchantItemType.Relic:
                    return relicData != null ? relicData.icon : null;
                default:
                    return null;
            }
        }
    }

    public MerchantOfferState Clone()
    {
        return new MerchantOfferState
        {
            itemType = itemType,
            lootItem = lootItem,
            weaponData = weaponData,
            blueprintData = blueprintData,
            relicData = relicData,
            price = price,
            isSoldOut = isSoldOut
        };
    }
}
