using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public sealed class MerchantCatalogLiveEditorTests
{
    private MerchantCatalog_SO catalog;

    [SetUp]
    public void SetUp()
    {
        var source = AssetDatabase.LoadAssetAtPath<MerchantCatalog_SO>(MerchantCatalogLiveData.CatalogPath);
        Assert.IsNotNull(source);
        catalog = Object.Instantiate(source);
    }

    [TearDown]
    public void TearDown() { if (catalog != null) Object.DestroyImmediate(catalog); }

    [TestCase("loot")]
    [TestCase("weapon")]
    [TestCase("blueprint")]
    [TestCase("relic")]
    public void EntryEditsRoundTripForEveryCategoryWithoutChangingOtherRows(string category)
    {
        var before = MerchantCatalogLiveData.Read(catalog);
        var row = before.rows.First(r => r.category == category);
        var change = Edit(row);
        change.minPrice = 33; change.maxPrice = 47; change.weight = 13;
        var after = Apply(before, change);
        var edited = after.rows.First(r => r.category == category && r.index == row.index);
        Assert.AreEqual(33, edited.minPrice);
        Assert.AreEqual(47, edited.maxPrice);
        Assert.AreEqual(13, edited.weight);
        Assert.AreEqual(row.asset.guid, edited.asset.guid);
        foreach (var other in before.rows.Where(r => r.category != category || r.index != row.index))
            Assert.AreEqual(JsonUtility.ToJson(other), JsonUtility.ToJson(after.rows.First(r => r.category == other.category && r.index == other.index)));
    }

    [Test]
    public void InvalidBatchDoesNotPartlyApplyEarlierValidChanges()
    {
        var doc = MerchantCatalogLiveData.Read(catalog);
        string original = EditorJsonUtility.ToJson(catalog);
        var change = Edit(doc.rows.First()); change.weight = 71;
        Assert.Throws<ArgumentException>(() => Apply(doc, change, new MerchantCatalogLiveData.Change
        { operation = "settings", minTotalGoodsCount = 10, maxTotalGoodsCount = 2 }));
        Assert.AreEqual(original, EditorJsonUtility.ToJson(catalog));
    }

    [Test]
    public void StaleRevisionPreservesExternalConfigurationChanges()
    {
        var doc = MerchantCatalogLiveData.Read(catalog);
        catalog.maxTotalGoodsCount += 1;
        string external = EditorJsonUtility.ToJson(catalog);
        Assert.Throws<MerchantCatalogLiveData.ConflictException>(() => Apply(doc, Edit(doc.rows.First())));
        Assert.AreEqual(external, EditorJsonUtility.ToJson(catalog));
    }

    [Test]
    public void RowGuidGuardRejectsTheWrongAssetEvenWithAValidRevision()
    {
        var doc = MerchantCatalogLiveData.Read(catalog);
        var row = doc.rows.First(r => r.category == "loot");
        var change = Edit(row);
        change.assetGuid = doc.rows.First(r => r.category == "loot" && r.asset.guid != row.asset.guid).asset.guid;
        string before = EditorJsonUtility.ToJson(catalog);
        Assert.Throws<MerchantCatalogLiveData.ConflictException>(() => Apply(doc, change));
        Assert.AreEqual(before, EditorJsonUtility.ToJson(catalog));
    }

    [Test]
    public void DuplicateGoodsIdsDoNotCauseAnotherAssetToBeEdited()
    {
        catalog.lootEntries[0].goodsId = "same-id";
        catalog.lootEntries[1].goodsId = "same-id";
        var doc = MerchantCatalogLiveData.Read(catalog);
        var change = Edit(doc.rows.First(r => r.category == "loot" && r.index == 1));
        change.weight = 29;
        int firstWeight = catalog.lootEntries[0].weight;
        Apply(doc, change);
        Assert.AreEqual(firstWeight, catalog.lootEntries[0].weight);
        Assert.AreEqual(29, catalog.lootEntries[1].weight);
    }

    [Test]
    public void RemovalAndAdditionUpdateCoverageAndKeepAssetIdentity()
    {
        var before = MerchantCatalogLiveData.Read(catalog);
        var row = before.rows.First(r => r.category == "loot");
        int coverage = before.categories.First(c => c.key == "loot").coveredCount;
        var removed = Apply(before, new MerchantCatalogLiveData.Change
        { operation = "remove", category = "loot", index = row.index, assetGuid = row.asset.guid });
        Assert.AreEqual(before.rows.Count - 1, removed.rows.Count);
        Assert.AreEqual(coverage - 1, removed.categories.First(c => c.key == "loot").coveredCount);
        var added = Apply(removed, new MerchantCatalogLiveData.Change
        { operation = "add", category = "loot", assetGuid = row.asset.guid, minPrice = 8, maxPrice = 12, weight = 10 });
        Assert.AreEqual(coverage, added.categories.First(c => c.key == "loot").coveredCount);
        Assert.AreEqual(row.asset.guid, added.rows.Last(r => r.category == "loot").asset.guid);
        Assert.AreEqual(before.rows.Count, added.rows.Count);
    }

    [Test]
    public void AdditionRejectsDuplicatesAndMismatchedAssetTypes()
    {
        var doc = MerchantCatalogLiveData.Read(catalog);
        var change = new MerchantCatalogLiveData.Change
        { operation = "add", category = "loot", assetGuid = doc.rows.First(r => r.category == "loot").asset.guid, minPrice = 1, maxPrice = 2, weight = 1 };
        Assert.Throws<ArgumentException>(() => Apply(doc, change));
        change.assetGuid = doc.rows.First(r => r.category == "weapon").asset.guid;
        Assert.Throws<ArgumentException>(() => Apply(doc, change));
    }

    [TestCase(0, 12, 3)]
    [TestCase(8, 7, 3)]
    [TestCase(8, 12, 0)]
    [TestCase(8, 12, -1)]
    public void InvalidPricesAndWeightsAreRejectedWithoutWriting(int min, int max, int weight)
    {
        var doc = MerchantCatalogLiveData.Read(catalog);
        var change = Edit(doc.rows.First());
        change.minPrice = min; change.maxPrice = max; change.weight = weight;
        string before = EditorJsonUtility.ToJson(catalog);
        Assert.Throws<ArgumentException>(() => Apply(doc, change));
        Assert.AreEqual(before, EditorJsonUtility.ToJson(catalog));
    }

    [Test]
    public void SettingsAndCategoryEditsPreserveTheProductPools()
    {
        var doc = MerchantCatalogLiveData.Read(catalog);
        string rowsBefore = string.Join("\n", doc.rows.Select(r => JsonUtility.ToJson(r)));
        var after = Apply(doc, new MerchantCatalogLiveData.Change { operation = "settings", minTotalGoodsCount = 7, maxTotalGoodsCount = 12 },
            new MerchantCatalogLiveData.Change { operation = "category", category = "loot", minimumCount = 5, randomWeight = 0 });
        Assert.AreEqual(7, after.minTotalGoodsCount);
        Assert.AreEqual(12, after.maxTotalGoodsCount);
        Assert.AreEqual(5, after.categories.First(c => c.key == "loot").minimumCount);
        Assert.AreEqual(0, after.categories.First(c => c.key == "loot").randomWeight);
        Assert.AreEqual(rowsBefore, string.Join("\n", after.rows.Select(r => JsonUtility.ToJson(r))));
    }

    [Test]
    public void UnsavedUnityChangesAreVisibleInTheNextRead()
    {
        var before = MerchantCatalogLiveData.Read(catalog);
        catalog.lootEntries[0].weight += 7;
        EditorUtility.SetDirty(catalog);
        var after = MerchantCatalogLiveData.Read(catalog);
        Assert.AreNotEqual(before.revision, after.revision);
        Assert.AreEqual(catalog.lootEntries[0].weight, after.rows.First(r => r.category == "loot").weight);
        Assert.IsTrue(after.unsavedUnityChanges);
    }

    private MerchantCatalogLiveData.Document Apply(MerchantCatalogLiveData.Document doc, params MerchantCatalogLiveData.Change[] changes)
    {
        return MerchantCatalogLiveData.Apply(catalog, new MerchantCatalogLiveData.Patch
        { baseRevision = doc.revision, changes = new List<MerchantCatalogLiveData.Change>(changes) }, false);
    }

    private static MerchantCatalogLiveData.Change Edit(MerchantCatalogLiveData.Row row)
    {
        return new MerchantCatalogLiveData.Change { operation = "entry", category = row.category, index = row.index,
            assetGuid = row.asset.guid, goodsId = row.goodsId, minPrice = row.minPrice, maxPrice = row.maxPrice, weight = row.weight };
    }
}
