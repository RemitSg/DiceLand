using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

public class RewardCandidateFilterServiceTests
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
    public void BuildLootCandidatesWithRarity_ReturnsOnlyMatchingNonNullLoot()
    {
        LootItem_SO commonA = CreateLoot("CommonA", LootRarity.Common);
        LootItem_SO rare = CreateLoot("Rare", LootRarity.Rare);
        LootItem_SO commonB = CreateLoot("CommonB", LootRarity.Common);
        List<LootItem_SO> pool = new List<LootItem_SO> { commonA, null, rare, commonB };

        List<LootItem_SO> candidates = RewardCandidateFilterService.BuildLootCandidatesWithRarity(pool, LootRarity.Common);

        CollectionAssert.AreEqual(new[] { commonA, commonB }, candidates);
    }

    [Test]
    public void BuildLootCandidatesWithRarity_ReturnsEmptyListForMissingPoolOrRarity()
    {
        LootItem_SO rare = CreateLoot("Rare", LootRarity.Rare);

        List<LootItem_SO> nullCandidates = RewardCandidateFilterService.BuildLootCandidatesWithRarity(null, LootRarity.Common);
        List<LootItem_SO> missingRarityCandidates = RewardCandidateFilterService.BuildLootCandidatesWithRarity(
            new List<LootItem_SO> { rare },
            LootRarity.Common);

        Assert.IsNotNull(nullCandidates);
        Assert.IsEmpty(nullCandidates);
        Assert.IsEmpty(missingRarityCandidates);
    }

    [Test]
    public void BuildRelicCandidates_ExcludesNullAndOwnedRelics()
    {
        Relic_SO owned = CreateRelic("Owned", LootRarity.Common);
        Relic_SO availableA = CreateRelic("AvailableA", LootRarity.Rare);
        Relic_SO availableB = CreateRelic("AvailableB", LootRarity.Legendary);
        List<Relic_SO> pool = new List<Relic_SO> { owned, null, availableA, availableB };
        HashSet<Relic_SO> excluded = new HashSet<Relic_SO> { owned };

        List<Relic_SO> candidates = RewardCandidateFilterService.BuildRelicCandidates(pool, excluded);

        CollectionAssert.AreEqual(new[] { availableA, availableB }, candidates);
    }

    [Test]
    public void HasAvailableRelicCandidate_UsesRelicTableAndExclusionSet()
    {
        Relic_SO owned = CreateRelic("Owned", LootRarity.Common);
        Relic_SO available = CreateRelic("Available", LootRarity.Uncommon);
        RelicTable_SO table = CreateRelicTable(owned, available);

        Assert.IsTrue(RewardCandidateFilterService.HasAvailableRelicCandidate(table, new HashSet<Relic_SO> { owned }));
        Assert.IsFalse(RewardCandidateFilterService.HasAvailableRelicCandidate(table, new HashSet<Relic_SO> { owned, available }));
        Assert.IsFalse(RewardCandidateFilterService.HasAvailableRelicCandidate(null, null));
    }

    [Test]
    public void BuildAvailableMerchantRelicGoodsEntries_FiltersTableAndLegacyEntries()
    {
        Relic_SO owned = CreateRelic("Owned", LootRarity.Common);
        Relic_SO tableAvailable = CreateRelic("TableAvailable", LootRarity.Uncommon);
        Relic_SO legacyAvailable = CreateRelic("LegacyAvailable", LootRarity.Rare);
        MerchantCatalog_SO.RelicGoodsEntry exhaustedTableEntry = CreateRelicGoodsEntry(CreateRelicTable(owned), null);
        MerchantCatalog_SO.RelicGoodsEntry tableEntry = CreateRelicGoodsEntry(CreateRelicTable(owned, tableAvailable), null);
        MerchantCatalog_SO.RelicGoodsEntry legacyEntry = CreateRelicGoodsEntry(null, legacyAvailable);
        MerchantCatalog_SO.RelicGoodsEntry ownedLegacyEntry = CreateRelicGoodsEntry(null, owned);
        List<MerchantCatalog_SO.RelicGoodsEntry> source = new List<MerchantCatalog_SO.RelicGoodsEntry>
        {
            null,
            exhaustedTableEntry,
            tableEntry,
            legacyEntry,
            ownedLegacyEntry
        };

        List<MerchantCatalog_SO.RelicGoodsEntry> candidates =
            RewardCandidateFilterService.BuildAvailableMerchantRelicGoodsEntries(source, new HashSet<Relic_SO> { owned });

        CollectionAssert.AreEqual(new[] { tableEntry, legacyEntry }, candidates);
    }

    [Test]
    public void HasAvailableMerchantRelicCandidate_FallsBackToLegacyRelicWhenTableMissing()
    {
        Relic_SO owned = CreateRelic("Owned", LootRarity.Common);
        Relic_SO available = CreateRelic("Available", LootRarity.Rare);
        MerchantCatalog_SO.RelicGoodsEntry ownedLegacyEntry = CreateRelicGoodsEntry(null, owned);
        MerchantCatalog_SO.RelicGoodsEntry availableLegacyEntry = CreateRelicGoodsEntry(null, available);

        Assert.IsFalse(RewardCandidateFilterService.HasAvailableMerchantRelicCandidate(ownedLegacyEntry, new HashSet<Relic_SO> { owned }));
        Assert.IsTrue(RewardCandidateFilterService.HasAvailableMerchantRelicCandidate(availableLegacyEntry, new HashSet<Relic_SO> { owned }));
        Assert.IsFalse(RewardCandidateFilterService.HasAvailableMerchantRelicCandidate(null, null));
    }

    private LootItem_SO CreateLoot(string name, LootRarity rarity)
    {
        LootItem_SO loot = ScriptableObject.CreateInstance<LootItem_SO>();
        loot.lootName = name;
        loot.rarity = rarity;
        createdObjects.Add(loot);
        return loot;
    }

    private Relic_SO CreateRelic(string name, LootRarity rarity)
    {
        Relic_SO relic = ScriptableObject.CreateInstance<Relic_SO>();
        relic.relicId = name;
        relic.relicName = name;
        relic.rarity = rarity;
        createdObjects.Add(relic);
        return relic;
    }

    private RelicTable_SO CreateRelicTable(params Relic_SO[] relics)
    {
        RelicTable_SO table = ScriptableObject.CreateInstance<RelicTable_SO>();
        table.tableName = "Candidate Table";
        SetPrivateField(table, "relics", new List<Relic_SO>(relics));
        createdObjects.Add(table);
        return table;
    }

    private static MerchantCatalog_SO.RelicGoodsEntry CreateRelicGoodsEntry(RelicTable_SO relicTable, Relic_SO legacyRelic)
    {
        MerchantCatalog_SO.RelicGoodsEntry entry = new MerchantCatalog_SO.RelicGoodsEntry
        {
            relicTable = relicTable,
            weight = 1
        };
        SetPrivateField(entry, "legacyRelicData", legacyRelic);
        return entry;
    }

    private static void SetPrivateField<T>(object target, string fieldName, T value)
    {
        Assert.IsNotNull(target, "Missing target object.");
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field, $"Missing field {fieldName}.");
        field.SetValue(target, value);
    }
}
