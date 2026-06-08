using System.Collections.Generic;
using UnityEngine;

public static class MapTutorialMerchantStockService
{
    private const int TutorialOfferPrice = 1;

    public static bool TryApplyTutorialStock(BoardTileData tile, CraftingBlueprint_SO tutorialBlueprint)
    {
        if (tile == null
            || tile.tileType != MapTileType.Merchant
            || tile.merchantStockGenerated
            || tutorialBlueprint == null)
        {
            return false;
        }

        List<MerchantOfferState> offers = new List<MerchantOfferState>
        {
            new MerchantOfferState
            {
                itemType = MerchantItemType.Blueprint,
                blueprintData = tutorialBlueprint,
                price = TutorialOfferPrice,
                isSoldOut = false
            }
        };

        if (tutorialBlueprint.materialRequirements != null)
        {
            for (int i = 0; i < tutorialBlueprint.materialRequirements.Count; i++)
            {
                CraftingMaterialRequirement requirement = tutorialBlueprint.materialRequirements[i];
                if (requirement == null || requirement.lootItem == null)
                    continue;

                int amount = Mathf.Max(1, requirement.amount);
                for (int copy = 0; copy < amount; copy++)
                    offers.Add(CreateLootOffer(requirement.lootItem));
            }
        }

        tile.merchantOffers = offers;
        tile.merchantStockGenerated = true;
        return true;
    }

    private static MerchantOfferState CreateLootOffer(LootItem_SO lootItem)
    {
        return new MerchantOfferState
        {
            itemType = MerchantItemType.Loot,
            lootItem = lootItem,
            price = TutorialOfferPrice,
            isSoldOut = false
        };
    }
}
