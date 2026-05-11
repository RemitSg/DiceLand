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
    public void BuildLootReforgeCandidates_IncludesSameRarityAttributeMaterials()
    {
        LootAttribute_SO blood = CreateAttribute("Blood");
        LootAttribute_SO fire = CreateAttribute("Fire");
        LootItem_SO clotspore = CreateLoot("凝血孢", LootRarity.Common, blood);
        LootItem_SO fang = CreateLoot("秽蚀尖牙", LootRarity.Common, blood);
        LootItem_SO uncommonBlood = CreateLoot("UncommonBlood", LootRarity.Uncommon, blood);
        LootItem_SO commonFire = CreateLoot("CommonFire", LootRarity.Common, fire);
        List<LootItem_SO> pool = new List<LootItem_SO>
        {
            clotspore,
            null,
            fang,
            uncommonBlood,
            commonFire
        };

        List<LootItem_SO> candidates = RewardCandidateFilterService.BuildLootReforgeCandidates(
            pool,
            LootRarity.Common,
            blood);

        CollectionAssert.AreEqual(new[] { clotspore, fang }, candidates);
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

    [Test]
    public void BuildAvailableMerchantLootGoodsEntries_FiltersMissingAndExcludedLoot()
    {
        LootItem_SO reserved = CreateLoot("Reserved", LootRarity.Common);
        LootItem_SO availableA = CreateLoot("AvailableA", LootRarity.Uncommon);
        LootItem_SO availableB = CreateLoot("AvailableB", LootRarity.Rare);
        MerchantCatalog_SO.LootGoodsEntry reservedEntry = CreateLootGoodsEntry(reserved);
        MerchantCatalog_SO.LootGoodsEntry availableEntryA = CreateLootGoodsEntry(availableA);
        MerchantCatalog_SO.LootGoodsEntry missingEntry = CreateLootGoodsEntry(null);
        MerchantCatalog_SO.LootGoodsEntry availableEntryB = CreateLootGoodsEntry(availableB);
        List<MerchantCatalog_SO.LootGoodsEntry> source = new List<MerchantCatalog_SO.LootGoodsEntry>
        {
            null,
            reservedEntry,
            availableEntryA,
            missingEntry,
            availableEntryB
        };

        List<MerchantCatalog_SO.LootGoodsEntry> candidates =
            RewardCandidateFilterService.BuildAvailableMerchantLootGoodsEntries(source, new HashSet<LootItem_SO> { reserved });

        CollectionAssert.AreEqual(new[] { availableEntryA, availableEntryB }, candidates);
        Assert.IsFalse(RewardCandidateFilterService.HasAvailableMerchantLootCandidate(reservedEntry, new HashSet<LootItem_SO> { reserved }));
        Assert.IsFalse(RewardCandidateFilterService.HasAvailableMerchantLootCandidate(missingEntry, null));
        Assert.IsFalse(RewardCandidateFilterService.HasAvailableMerchantLootCandidate(null, null));
    }

    [Test]
    public void BuildAvailableMerchantWeaponGoodsEntries_FiltersMissingAndExcludedWeapons()
    {
        Weapon_SO owned = CreateWeapon("Owned");
        Weapon_SO availableA = CreateWeapon("AvailableA");
        Weapon_SO availableB = CreateWeapon("AvailableB");
        MerchantCatalog_SO.WeaponGoodsEntry ownedEntry = CreateWeaponGoodsEntry(owned);
        MerchantCatalog_SO.WeaponGoodsEntry availableEntryA = CreateWeaponGoodsEntry(availableA);
        MerchantCatalog_SO.WeaponGoodsEntry missingEntry = CreateWeaponGoodsEntry(null);
        MerchantCatalog_SO.WeaponGoodsEntry availableEntryB = CreateWeaponGoodsEntry(availableB);
        List<MerchantCatalog_SO.WeaponGoodsEntry> source = new List<MerchantCatalog_SO.WeaponGoodsEntry>
        {
            null,
            ownedEntry,
            availableEntryA,
            missingEntry,
            availableEntryB
        };

        List<MerchantCatalog_SO.WeaponGoodsEntry> candidates =
            RewardCandidateFilterService.BuildAvailableMerchantWeaponGoodsEntries(source, new HashSet<Weapon_SO> { owned });

        CollectionAssert.AreEqual(new[] { availableEntryA, availableEntryB }, candidates);
        Assert.IsFalse(RewardCandidateFilterService.HasAvailableMerchantWeaponCandidate(ownedEntry, new HashSet<Weapon_SO> { owned }));
        Assert.IsFalse(RewardCandidateFilterService.HasAvailableMerchantWeaponCandidate(missingEntry, null));
        Assert.IsFalse(RewardCandidateFilterService.HasAvailableMerchantWeaponCandidate(null, null));
    }

    [Test]
    public void BuildAvailableMerchantBlueprintGoodsEntries_FiltersMissingAndExcludedBlueprints()
    {
        CraftingBlueprint_SO owned = CreateBlueprint("Owned");
        CraftingBlueprint_SO availableA = CreateBlueprint("AvailableA");
        CraftingBlueprint_SO availableB = CreateBlueprint("AvailableB");
        MerchantCatalog_SO.BlueprintGoodsEntry ownedEntry = CreateBlueprintGoodsEntry(owned);
        MerchantCatalog_SO.BlueprintGoodsEntry availableEntryA = CreateBlueprintGoodsEntry(availableA);
        MerchantCatalog_SO.BlueprintGoodsEntry missingEntry = CreateBlueprintGoodsEntry(null);
        MerchantCatalog_SO.BlueprintGoodsEntry availableEntryB = CreateBlueprintGoodsEntry(availableB);
        List<MerchantCatalog_SO.BlueprintGoodsEntry> source = new List<MerchantCatalog_SO.BlueprintGoodsEntry>
        {
            null,
            ownedEntry,
            availableEntryA,
            missingEntry,
            availableEntryB
        };

        List<MerchantCatalog_SO.BlueprintGoodsEntry> candidates =
            RewardCandidateFilterService.BuildAvailableMerchantBlueprintGoodsEntries(source, new HashSet<CraftingBlueprint_SO> { owned });

        CollectionAssert.AreEqual(new[] { availableEntryA, availableEntryB }, candidates);
        Assert.IsFalse(RewardCandidateFilterService.HasAvailableMerchantBlueprintCandidate(ownedEntry, new HashSet<CraftingBlueprint_SO> { owned }));
        Assert.IsFalse(RewardCandidateFilterService.HasAvailableMerchantBlueprintCandidate(missingEntry, null));
        Assert.IsFalse(RewardCandidateFilterService.HasAvailableMerchantBlueprintCandidate(null, null));
    }

    private LootItem_SO CreateLoot(string name, LootRarity rarity)
    {
        LootItem_SO loot = ScriptableObject.CreateInstance<LootItem_SO>();
        loot.lootName = name;
        loot.rarity = rarity;
        createdObjects.Add(loot);
        return loot;
    }

    private LootItem_SO CreateLoot(string name, LootRarity rarity, LootAttribute_SO attribute)
    {
        LootItem_SO loot = CreateLoot(name, rarity);
        loot.attribute = attribute;
        return loot;
    }

    private LootAttribute_SO CreateAttribute(string name)
    {
        LootAttribute_SO attribute = ScriptableObject.CreateInstance<LootAttribute_SO>();
        attribute.attributeName = name;
        createdObjects.Add(attribute);
        return attribute;
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

    private Weapon_SO CreateWeapon(string name)
    {
        Weapon_SO weapon = ScriptableObject.CreateInstance<Weapon_SO>();
        weapon.weaponName = name;
        createdObjects.Add(weapon);
        return weapon;
    }

    private CraftingBlueprint_SO CreateBlueprint(string name)
    {
        CraftingBlueprint_SO blueprint = ScriptableObject.CreateInstance<CraftingBlueprint_SO>();
        blueprint.blueprintName = name;
        createdObjects.Add(blueprint);
        return blueprint;
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

    private static MerchantCatalog_SO.LootGoodsEntry CreateLootGoodsEntry(LootItem_SO loot)
    {
        return new MerchantCatalog_SO.LootGoodsEntry
        {
            lootItem = loot,
            weight = 1
        };
    }

    private static MerchantCatalog_SO.WeaponGoodsEntry CreateWeaponGoodsEntry(Weapon_SO weapon)
    {
        return new MerchantCatalog_SO.WeaponGoodsEntry
        {
            weaponData = weapon,
            weight = 1
        };
    }

    private static MerchantCatalog_SO.BlueprintGoodsEntry CreateBlueprintGoodsEntry(CraftingBlueprint_SO blueprint)
    {
        return new MerchantCatalog_SO.BlueprintGoodsEntry
        {
            blueprintData = blueprint,
            weight = 1
        };
    }

    private static void SetPrivateField<T>(object target, string fieldName, T value)
    {
        Assert.IsNotNull(target, "Missing target object.");
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field, $"Missing field {fieldName}.");
        field.SetValue(target, value);
    }
}
