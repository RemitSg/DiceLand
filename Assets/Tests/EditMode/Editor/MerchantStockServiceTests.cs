using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

public class MerchantStockServiceTests
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
    public void TryGenerateInitialStock_EmptyCatalogMarksTileGeneratedWithEmptyOffers()
    {
        MerchantCatalog_SO catalog = CreateCatalog(0, 0);
        BoardTileData tile = new BoardTileData { tileType = MapTileType.Merchant };

        bool generated = MerchantStockService.TryGenerateInitialStock(
            tile,
            catalog,
            new RunState(),
            CreateDifficulty(),
            new System.Random(1));

        Assert.IsTrue(generated);
        Assert.IsTrue(tile.merchantStockGenerated);
        Assert.IsNotNull(tile.merchantOffers);
        Assert.IsEmpty(tile.merchantOffers);
    }

    [Test]
    public void TryGenerateInitialStock_CreatesGuaranteedOffersAndExcludesOwnedRelics()
    {
        LootItem_SO loot = CreateLoot("Loot");
        Weapon_SO weapon = CreateWeapon("Weapon");
        Weapon_SO ownedBlueprintWeapon = CreateWeapon("OwnedBlueprintWeapon");
        CraftingBlueprint_SO ownedWeaponBlueprint = CreateBlueprint("OwnedWeaponBlueprint", ownedBlueprintWeapon);
        CraftingBlueprint_SO blueprint = CreateBlueprint("Blueprint", weapon);
        Relic_SO ownedRelic = CreateRelic("Owned", LootRarity.Common);
        Relic_SO availableRelic = CreateRelic("Available", LootRarity.Rare);
        MerchantCatalog_SO catalog = CreateCatalog(4, 4);
        catalog.lootMinimumCount.minimumCount = 1;
        catalog.weaponMinimumCount.minimumCount = 1;
        catalog.blueprintMinimumCount.minimumCount = 1;
        catalog.relicMinimumCount.minimumCount = 1;
        catalog.lootEntries.Add(new MerchantCatalog_SO.LootGoodsEntry { lootItem = loot, minPrice = 1, maxPrice = 1, weight = 1 });
        catalog.weaponEntries.Add(new MerchantCatalog_SO.WeaponGoodsEntry { weaponData = weapon, minPrice = 2, maxPrice = 2, weight = 1 });
        catalog.blueprintEntries.Add(new MerchantCatalog_SO.BlueprintGoodsEntry { blueprintData = ownedWeaponBlueprint, minPrice = 3, maxPrice = 3, weight = 100 });
        catalog.blueprintEntries.Add(new MerchantCatalog_SO.BlueprintGoodsEntry { blueprintData = blueprint, minPrice = 3, maxPrice = 3, weight = 1 });
        catalog.relicEntries.Add(CreateRelicGoodsEntry(null, ownedRelic));
        catalog.relicEntries.Add(CreateRelicGoodsEntry(null, availableRelic));
        RunState runState = new RunState
        {
            weaponInventory = new List<RunWeaponState>
            {
                new RunWeaponState { weaponData = ownedBlueprintWeapon, amount = 1 }
            },
            relicInventory = new List<RunRelicState>
            {
                new RunRelicState { relicData = ownedRelic, amount = 1 }
            }
        };
        BoardTileData tile = new BoardTileData { tileType = MapTileType.Merchant };

        bool generated = MerchantStockService.TryGenerateInitialStock(
            tile,
            catalog,
            runState,
            CreateDifficulty(),
            new System.Random(2));

        Assert.IsTrue(generated);
        Assert.IsTrue(tile.merchantStockGenerated);
        Assert.AreEqual(4, tile.merchantOffers.Count);
        AssertOfferWithType(tile.merchantOffers, MerchantItemType.Loot);
        AssertOfferWithType(tile.merchantOffers, MerchantItemType.Weapon);
        Assert.AreSame(blueprint, FindOfferWithType(tile.merchantOffers, MerchantItemType.Blueprint).blueprintData);
        MerchantOfferState relicOffer = FindOfferWithType(tile.merchantOffers, MerchantItemType.Relic);
        Assert.AreSame(availableRelic, relicOffer.relicData);
    }

    [Test]
    public void RefreshOffers_ReplacesAllOffersWithSameTypesAndRespectsOwnedExclusions()
    {
        LootItem_SO oldLoot = CreateLoot("OldLoot");
        LootItem_SO newLoot = CreateLoot("NewLoot");
        Weapon_SO oldWeapon = CreateWeapon("OldWeapon");
        Weapon_SO ownedWeapon = CreateWeapon("OwnedWeapon");
        Weapon_SO newWeapon = CreateWeapon("NewWeapon");
        Weapon_SO ownedBlueprintWeapon = CreateWeapon("OwnedBlueprintWeapon");
        CraftingBlueprint_SO oldBlueprint = CreateBlueprint("OldBlueprint");
        CraftingBlueprint_SO ownedBlueprint = CreateBlueprint("OwnedBlueprint");
        CraftingBlueprint_SO ownedWeaponBlueprint = CreateBlueprint("OwnedWeaponBlueprint", ownedBlueprintWeapon);
        CraftingBlueprint_SO newBlueprint = CreateBlueprint("NewBlueprint", newWeapon);
        Relic_SO oldRelic = CreateRelic("OldRelic", LootRarity.Common);
        Relic_SO ownedRelic = CreateRelic("OwnedRelic", LootRarity.Common);
        Relic_SO newRelic = CreateRelic("NewRelic", LootRarity.Common);
        MerchantCatalog_SO catalog = CreateCatalog(4, 4);
        catalog.lootEntries.Add(new MerchantCatalog_SO.LootGoodsEntry { lootItem = oldLoot, minPrice = 1, maxPrice = 1, weight = 1 });
        catalog.lootEntries.Add(new MerchantCatalog_SO.LootGoodsEntry { lootItem = newLoot, minPrice = 1, maxPrice = 1, weight = 1 });
        catalog.weaponEntries.Add(new MerchantCatalog_SO.WeaponGoodsEntry { weaponData = oldWeapon, minPrice = 2, maxPrice = 2, weight = 1 });
        catalog.weaponEntries.Add(new MerchantCatalog_SO.WeaponGoodsEntry { weaponData = ownedWeapon, minPrice = 2, maxPrice = 2, weight = 1 });
        catalog.weaponEntries.Add(new MerchantCatalog_SO.WeaponGoodsEntry { weaponData = newWeapon, minPrice = 2, maxPrice = 2, weight = 1 });
        catalog.blueprintEntries.Add(new MerchantCatalog_SO.BlueprintGoodsEntry { blueprintData = oldBlueprint, minPrice = 3, maxPrice = 3, weight = 1 });
        catalog.blueprintEntries.Add(new MerchantCatalog_SO.BlueprintGoodsEntry { blueprintData = ownedBlueprint, minPrice = 3, maxPrice = 3, weight = 1 });
        catalog.blueprintEntries.Add(new MerchantCatalog_SO.BlueprintGoodsEntry { blueprintData = ownedWeaponBlueprint, minPrice = 3, maxPrice = 3, weight = 100 });
        catalog.blueprintEntries.Add(new MerchantCatalog_SO.BlueprintGoodsEntry { blueprintData = newBlueprint, minPrice = 3, maxPrice = 3, weight = 1 });
        catalog.relicEntries.Add(CreateRelicGoodsEntry(null, oldRelic));
        catalog.relicEntries.Add(CreateRelicGoodsEntry(null, ownedRelic));
        catalog.relicEntries.Add(CreateRelicGoodsEntry(null, newRelic));
        RunState runState = new RunState
        {
            weaponInventory = new List<RunWeaponState>
            {
                new RunWeaponState { weaponData = ownedWeapon, amount = 1 },
                new RunWeaponState { weaponData = ownedBlueprintWeapon, amount = 1 }
            },
            blueprintInventory = new List<RunBlueprintState>
            {
                new RunBlueprintState { blueprintData = ownedBlueprint, amount = 1 }
            },
            relicInventory = new List<RunRelicState>
            {
                new RunRelicState { relicData = ownedRelic, amount = 1 }
            }
        };
        BoardTileData tile = new BoardTileData
        {
            merchantOffers = new List<MerchantOfferState>
            {
                new MerchantOfferState { itemType = MerchantItemType.Loot, lootItem = oldLoot, isSoldOut = true },
                new MerchantOfferState { itemType = MerchantItemType.Weapon, weaponData = oldWeapon, isSoldOut = true },
                new MerchantOfferState { itemType = MerchantItemType.Blueprint, blueprintData = oldBlueprint, isSoldOut = true },
                new MerchantOfferState { itemType = MerchantItemType.Relic, relicData = oldRelic, isSoldOut = true }
            }
        };

        int refreshedCount = MerchantStockService.RefreshOffers(
            tile,
            catalog,
            runState,
            CreateDifficulty(),
            new System.Random(3));

        Assert.AreEqual(4, refreshedCount);
        Assert.AreSame(newLoot, FindOfferWithType(tile.merchantOffers, MerchantItemType.Loot).lootItem);
        Assert.AreSame(newWeapon, FindOfferWithType(tile.merchantOffers, MerchantItemType.Weapon).weaponData);
        Assert.AreSame(newBlueprint, FindOfferWithType(tile.merchantOffers, MerchantItemType.Blueprint).blueprintData);
        Assert.AreSame(newRelic, FindOfferWithType(tile.merchantOffers, MerchantItemType.Relic).relicData);
    }

    [Test]
    public void CountRefreshableOffers_CountsNonNullSlots()
    {
        BoardTileData tile = new BoardTileData
        {
            merchantOffers = new List<MerchantOfferState>
            {
                null,
                new MerchantOfferState(),
                new MerchantOfferState { isSoldOut = true }
            }
        };

        Assert.AreEqual(2, MerchantStockService.CountRefreshableOffers(tile));
    }

    private MerchantCatalog_SO CreateCatalog(int minTotal, int maxTotal)
    {
        MerchantCatalog_SO catalog = ScriptableObject.CreateInstance<MerchantCatalog_SO>();
        catalog.minTotalGoodsCount = minTotal;
        catalog.maxTotalGoodsCount = maxTotal;
        catalog.lootMinimumCount.minimumCount = 0;
        catalog.weaponMinimumCount.minimumCount = 0;
        catalog.blueprintMinimumCount.minimumCount = 0;
        catalog.relicMinimumCount.minimumCount = 0;
        createdObjects.Add(catalog);
        return catalog;
    }

    private DifficultyConfig_SO CreateDifficulty()
    {
        DifficultyConfig_SO config = ScriptableObject.CreateInstance<DifficultyConfig_SO>();
        config.merchantRelicPrices = new List<DifficultyConfig_SO.MerchantRelicPriceEntry>
        {
            new DifficultyConfig_SO.MerchantRelicPriceEntry { rarity = LootRarity.Common, minPrice = 5, maxPrice = 5 },
            new DifficultyConfig_SO.MerchantRelicPriceEntry { rarity = LootRarity.Rare, minPrice = 9, maxPrice = 9 }
        };
        createdObjects.Add(config);
        return config;
    }

    private LootItem_SO CreateLoot(string name)
    {
        LootItem_SO loot = ScriptableObject.CreateInstance<LootItem_SO>();
        loot.lootName = name;
        createdObjects.Add(loot);
        return loot;
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

    private CraftingBlueprint_SO CreateBlueprint(string name, Weapon_SO targetWeapon)
    {
        CraftingBlueprint_SO blueprint = CreateBlueprint(name);
        blueprint.targetWeapon = targetWeapon;
        return blueprint;
    }

    private Relic_SO CreateRelic(string name, LootRarity rarity)
    {
        Relic_SO relic = ScriptableObject.CreateInstance<Relic_SO>();
        relic.relicName = name;
        relic.rarity = rarity;
        createdObjects.Add(relic);
        return relic;
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

    private static MerchantOfferState FindOfferWithType(List<MerchantOfferState> offers, MerchantItemType itemType)
    {
        for (int i = 0; i < offers.Count; i++)
        {
            if (offers[i] != null && offers[i].itemType == itemType)
                return offers[i];
        }

        return null;
    }

    private static void AssertOfferWithType(List<MerchantOfferState> offers, MerchantItemType itemType)
    {
        Assert.IsNotNull(FindOfferWithType(offers, itemType), $"Missing offer type {itemType}.");
    }

    private static void SetPrivateField<T>(object target, string fieldName, T value)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field, $"Missing field {fieldName}.");
        field.SetValue(target, value);
    }
}
