using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

public class MerchantOfferSelectionServiceTests
{
    private MerchantCatalog_SO catalog;

    [TearDown]
    public void TearDown()
    {
        if (catalog != null)
            Object.DestroyImmediate(catalog);
    }

    [Test]
    public void ResolveTargetTotalCount_ClampsToAvailableAndMinimumRequirements()
    {
        catalog = ScriptableObject.CreateInstance<MerchantCatalog_SO>();
        catalog.minTotalGoodsCount = 2;
        catalog.maxTotalGoodsCount = 6;
        catalog.lootMinimumCount.minimumCount = 3;
        catalog.weaponMinimumCount.minimumCount = 2;
        catalog.blueprintMinimumCount.minimumCount = 0;
        catalog.relicMinimumCount.minimumCount = 0;

        int count = MerchantOfferSelectionService.ResolveTargetTotalCount(catalog, 4, new System.Random(1));

        Assert.AreEqual(4, count);
    }

    [Test]
    public void ResolveTargetTotalCount_ReturnsConfiguredMinimumWhenRangeCollapses()
    {
        catalog = ScriptableObject.CreateInstance<MerchantCatalog_SO>();
        catalog.minTotalGoodsCount = 3;
        catalog.maxTotalGoodsCount = 3;

        int count = MerchantOfferSelectionService.ResolveTargetTotalCount(catalog, 10, new System.Random(2));

        Assert.AreEqual(3, count);
    }

    [Test]
    public void GetAvailableCategoryWeight_ReturnsZeroForMissingPoolOrCategory()
    {
        MerchantCatalog_SO.CategoryMinimumEntry category = new MerchantCatalog_SO.CategoryMinimumEntry
        {
            randomWeight = 5
        };

        Assert.AreEqual(0, MerchantOfferSelectionService.GetAvailableCategoryWeight<string>(null, category));
        Assert.AreEqual(0, MerchantOfferSelectionService.GetAvailableCategoryWeight(new List<string>(), category));
        Assert.AreEqual(0, MerchantOfferSelectionService.GetAvailableCategoryWeight(new List<string> { "A" }, null));
        Assert.AreEqual(5, MerchantOfferSelectionService.GetAvailableCategoryWeight(new List<string> { "A" }, category));
    }

    [Test]
    public void RollWeightedEntryIndex_ReturnsOnlyIndexForSingleEntry()
    {
        List<MerchantCatalog_SO.LootGoodsEntry> pool = new List<MerchantCatalog_SO.LootGoodsEntry>
        {
            new MerchantCatalog_SO.LootGoodsEntry { weight = -10 }
        };

        int index = MerchantOfferSelectionService.RollWeightedEntryIndex(pool, entry => entry.weight, new System.Random(3));

        Assert.AreEqual(0, index);
    }

    [Test]
    public void ShuffleOffers_KeepsSameOfferInstances()
    {
        MerchantOfferState offerA = new MerchantOfferState { itemType = MerchantItemType.Loot };
        MerchantOfferState offerB = new MerchantOfferState { itemType = MerchantItemType.Weapon };
        MerchantOfferState offerC = new MerchantOfferState { itemType = MerchantItemType.Relic };
        List<MerchantOfferState> offers = new List<MerchantOfferState> { offerA, offerB, offerC };

        MerchantOfferSelectionService.ShuffleOffers(offers, new System.Random(4));

        Assert.AreEqual(3, offers.Count);
        CollectionAssert.Contains(offers, offerA);
        CollectionAssert.Contains(offers, offerB);
        CollectionAssert.Contains(offers, offerC);
    }
}
