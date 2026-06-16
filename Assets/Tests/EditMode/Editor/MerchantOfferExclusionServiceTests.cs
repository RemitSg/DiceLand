using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

public class MerchantOfferExclusionServiceTests
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
    public void BuildOwnedSets_IgnoreNullAndNonPositiveInventoryEntries()
    {
        Relic_SO relic = CreateRelic();
        Weapon_SO weapon = CreateWeapon();
        CraftingBlueprint_SO blueprint = CreateBlueprint();
        RunState runState = new RunState
        {
            relicInventory = new List<RunRelicState>
            {
                null,
                new RunRelicState { relicData = relic, amount = 1 },
                new RunRelicState { relicData = CreateRelic(), amount = 0 }
            },
            weaponInventory = new List<RunWeaponState>
            {
                null,
                new RunWeaponState { weaponData = weapon, amount = 2 },
                new RunWeaponState { weaponData = CreateWeapon(), amount = -1 }
            },
            blueprintInventory = new List<RunBlueprintState>
            {
                null,
                new RunBlueprintState { blueprintData = blueprint, amount = 1 },
                new RunBlueprintState { blueprintData = CreateBlueprint(), amount = 0 }
            }
        };

        HashSet<Relic_SO> relics = MerchantOfferExclusionService.BuildOwnedRelicSet(runState);
        HashSet<Weapon_SO> weapons = MerchantOfferExclusionService.BuildOwnedWeaponSet(runState);
        HashSet<CraftingBlueprint_SO> blueprints = MerchantOfferExclusionService.BuildOwnedBlueprintSet(runState);

        CollectionAssert.AreEqual(new[] { relic }, relics);
        CollectionAssert.AreEqual(new[] { weapon }, weapons);
        CollectionAssert.AreEqual(new[] { blueprint }, blueprints);
    }

    [Test]
    public void BuildUnavailableBlueprintSet_IncludesBlueprintsForOwnedWeapons()
    {
        Weapon_SO ownedWeapon = CreateWeapon();
        Weapon_SO missingWeapon = CreateWeapon();
        CraftingBlueprint_SO ownedBlueprint = CreateBlueprint();
        CraftingBlueprint_SO ownedWeaponBlueprint = CreateBlueprint(ownedWeapon);
        CraftingBlueprint_SO availableBlueprint = CreateBlueprint(missingWeapon);
        RunState runState = new RunState
        {
            weaponInventory = new List<RunWeaponState>
            {
                new RunWeaponState { weaponData = ownedWeapon, amount = 1 }
            },
            blueprintInventory = new List<RunBlueprintState>
            {
                new RunBlueprintState { blueprintData = ownedBlueprint, amount = 1 }
            }
        };
        List<MerchantCatalog_SO.BlueprintGoodsEntry> source = new List<MerchantCatalog_SO.BlueprintGoodsEntry>
        {
            new MerchantCatalog_SO.BlueprintGoodsEntry { blueprintData = ownedBlueprint },
            new MerchantCatalog_SO.BlueprintGoodsEntry { blueprintData = ownedWeaponBlueprint },
            new MerchantCatalog_SO.BlueprintGoodsEntry { blueprintData = availableBlueprint }
        };

        HashSet<CraftingBlueprint_SO> unavailable = MerchantOfferExclusionService.BuildUnavailableBlueprintSet(runState, source);

        CollectionAssert.AreEquivalent(new[] { ownedBlueprint, ownedWeaponBlueprint }, unavailable);
        Assert.IsFalse(unavailable.Contains(availableBlueprint));
    }

    [Test]
    public void BuildCurrentMerchantLootSet_HonorsIgnoreOfferAndSoldOutFilter()
    {
        LootItem_SO activeLoot = CreateLoot();
        LootItem_SO soldOutLoot = CreateLoot();
        LootItem_SO ignoredLoot = CreateLoot();
        MerchantOfferState ignoredOffer = new MerchantOfferState
        {
            itemType = MerchantItemType.Loot,
            lootItem = ignoredLoot
        };
        BoardTileData tile = new BoardTileData
        {
            merchantOffers = new List<MerchantOfferState>
            {
                null,
                new MerchantOfferState { itemType = MerchantItemType.Loot, lootItem = activeLoot },
                new MerchantOfferState { itemType = MerchantItemType.Loot, lootItem = soldOutLoot, isSoldOut = true },
                ignoredOffer,
                new MerchantOfferState { itemType = MerchantItemType.Weapon, weaponData = CreateWeapon() }
            }
        };

        HashSet<LootItem_SO> activeOnly = MerchantOfferExclusionService.BuildCurrentMerchantLootSet(tile, ignoredOffer);
        HashSet<LootItem_SO> includeSoldOut = MerchantOfferExclusionService.BuildCurrentMerchantLootSet(tile, ignoredOffer, true);

        CollectionAssert.AreEqual(new[] { activeLoot }, activeOnly);
        CollectionAssert.AreEquivalent(new[] { activeLoot, soldOutLoot }, includeSoldOut);
    }

    [Test]
    public void AppendCurrentMerchantEquipmentAndRelics_HonorsIgnoreOfferAndSoldOutFilter()
    {
        Weapon_SO activeWeapon = CreateWeapon();
        Weapon_SO soldOutWeapon = CreateWeapon();
        CraftingBlueprint_SO activeBlueprint = CreateBlueprint();
        CraftingBlueprint_SO soldOutBlueprint = CreateBlueprint();
        Relic_SO activeRelic = CreateRelic();
        Relic_SO soldOutRelic = CreateRelic();
        MerchantOfferState ignoredWeaponOffer = new MerchantOfferState
        {
            itemType = MerchantItemType.Weapon,
            weaponData = CreateWeapon()
        };
        BoardTileData tile = new BoardTileData
        {
            merchantOffers = new List<MerchantOfferState>
            {
                ignoredWeaponOffer,
                new MerchantOfferState { itemType = MerchantItemType.Weapon, weaponData = activeWeapon },
                new MerchantOfferState { itemType = MerchantItemType.Weapon, weaponData = soldOutWeapon, isSoldOut = true },
                new MerchantOfferState { itemType = MerchantItemType.Blueprint, blueprintData = activeBlueprint },
                new MerchantOfferState { itemType = MerchantItemType.Blueprint, blueprintData = soldOutBlueprint, isSoldOut = true },
                new MerchantOfferState { itemType = MerchantItemType.Relic, relicData = activeRelic },
                new MerchantOfferState { itemType = MerchantItemType.Relic, relicData = soldOutRelic, isSoldOut = true }
            }
        };
        HashSet<Weapon_SO> weapons = new HashSet<Weapon_SO>();
        HashSet<CraftingBlueprint_SO> blueprints = new HashSet<CraftingBlueprint_SO>();
        HashSet<Relic_SO> relics = new HashSet<Relic_SO>();

        MerchantOfferExclusionService.AppendCurrentMerchantWeapons(tile, weapons, ignoredWeaponOffer, true);
        MerchantOfferExclusionService.AppendCurrentMerchantBlueprints(tile, blueprints, null);
        MerchantOfferExclusionService.AppendCurrentMerchantRelics(tile, relics, null, true);

        CollectionAssert.AreEquivalent(new[] { activeWeapon, soldOutWeapon }, weapons);
        CollectionAssert.AreEqual(new[] { activeBlueprint }, blueprints);
        CollectionAssert.AreEquivalent(new[] { activeRelic, soldOutRelic }, relics);
    }

    private LootItem_SO CreateLoot()
    {
        LootItem_SO asset = ScriptableObject.CreateInstance<LootItem_SO>();
        createdObjects.Add(asset);
        return asset;
    }

    private Weapon_SO CreateWeapon()
    {
        Weapon_SO asset = ScriptableObject.CreateInstance<Weapon_SO>();
        createdObjects.Add(asset);
        return asset;
    }

    private CraftingBlueprint_SO CreateBlueprint()
    {
        CraftingBlueprint_SO asset = ScriptableObject.CreateInstance<CraftingBlueprint_SO>();
        createdObjects.Add(asset);
        return asset;
    }

    private CraftingBlueprint_SO CreateBlueprint(Weapon_SO targetWeapon)
    {
        CraftingBlueprint_SO asset = CreateBlueprint();
        asset.targetWeapon = targetWeapon;
        return asset;
    }

    private Relic_SO CreateRelic()
    {
        Relic_SO asset = ScriptableObject.CreateInstance<Relic_SO>();
        createdObjects.Add(asset);
        return asset;
    }
}
