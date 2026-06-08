using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

public class MapTutorialMerchantStockServiceTests
{
    private readonly List<Object> createdObjects = new List<Object>();

    [TearDown]
    public void TearDown()
    {
        for (int i = createdObjects.Count - 1; i >= 0; i--)
        {
            if (createdObjects[i] != null)
                Object.DestroyImmediate(createdObjects[i]);
        }

        createdObjects.Clear();
    }

    [Test]
    public void TryApplyTutorialStock_AddsBlueprintAndRequiredMaterialsAtOneGold()
    {
        LootItem_SO ember = CreateLoot("Ember");
        LootItem_SO iron = CreateLoot("Iron");
        CraftingBlueprint_SO blueprint = CreateBlueprint(
            "Molten Scroll",
            new CraftingMaterialRequirement { lootItem = ember, amount = 2 },
            new CraftingMaterialRequirement { lootItem = iron, amount = 1 });
        BoardTileData tile = new BoardTileData { tileType = MapTileType.Merchant };

        bool applied = MapTutorialMerchantStockService.TryApplyTutorialStock(tile, blueprint);

        Assert.IsTrue(applied);
        Assert.IsTrue(tile.merchantStockGenerated);
        Assert.AreEqual(4, tile.merchantOffers.Count);
        AssertOffer(tile.merchantOffers[0], MerchantItemType.Blueprint, blueprint, null);
        AssertOffer(tile.merchantOffers[1], MerchantItemType.Loot, null, ember);
        AssertOffer(tile.merchantOffers[2], MerchantItemType.Loot, null, ember);
        AssertOffer(tile.merchantOffers[3], MerchantItemType.Loot, null, iron);
    }

    [Test]
    public void TryApplyTutorialStock_DoesNotOverwriteExistingGeneratedStock()
    {
        CraftingBlueprint_SO blueprint = CreateBlueprint("Molten Scroll");
        MerchantOfferState existingOffer = new MerchantOfferState { itemType = MerchantItemType.Relic, price = 9 };
        BoardTileData tile = new BoardTileData
        {
            tileType = MapTileType.Merchant,
            merchantStockGenerated = true,
            merchantOffers = new List<MerchantOfferState> { existingOffer }
        };

        bool applied = MapTutorialMerchantStockService.TryApplyTutorialStock(tile, blueprint);

        Assert.IsFalse(applied);
        Assert.AreEqual(1, tile.merchantOffers.Count);
        Assert.AreSame(existingOffer, tile.merchantOffers[0]);
    }

    [Test]
    public void TryApplyTutorialStock_IgnoresNonMerchantOrMissingBlueprint()
    {
        Assert.IsFalse(MapTutorialMerchantStockService.TryApplyTutorialStock(new BoardTileData { tileType = MapTileType.Forge }, CreateBlueprint("Molten Scroll")));
        Assert.IsFalse(MapTutorialMerchantStockService.TryApplyTutorialStock(new BoardTileData { tileType = MapTileType.Merchant }, null));
    }

    private LootItem_SO CreateLoot(string name)
    {
        LootItem_SO loot = ScriptableObject.CreateInstance<LootItem_SO>();
        loot.lootName = name;
        createdObjects.Add(loot);
        return loot;
    }

    private CraftingBlueprint_SO CreateBlueprint(string name, params CraftingMaterialRequirement[] requirements)
    {
        CraftingBlueprint_SO blueprint = ScriptableObject.CreateInstance<CraftingBlueprint_SO>();
        blueprint.blueprintName = name;
        blueprint.materialRequirements = new List<CraftingMaterialRequirement>(requirements);
        createdObjects.Add(blueprint);
        return blueprint;
    }

    private static void AssertOffer(
        MerchantOfferState offer,
        MerchantItemType expectedType,
        CraftingBlueprint_SO expectedBlueprint,
        LootItem_SO expectedLoot)
    {
        Assert.IsNotNull(offer);
        Assert.AreEqual(expectedType, offer.itemType);
        Assert.AreEqual(1, offer.price);
        Assert.IsFalse(offer.isSoldOut);
        if (expectedBlueprint != null)
            Assert.AreSame(expectedBlueprint, offer.blueprintData);
        if (expectedLoot != null)
            Assert.AreSame(expectedLoot, offer.lootItem);
    }
}
