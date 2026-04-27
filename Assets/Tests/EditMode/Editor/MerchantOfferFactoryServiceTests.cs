using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

public class MerchantOfferFactoryServiceTests
{
    private LootItem_SO loot;
    private Weapon_SO weapon;
    private CraftingBlueprint_SO blueprint;
    private Relic_SO relic;
    private RelicTable_SO relicTable;
    private DifficultyConfig_SO difficultyConfig;

    [TearDown]
    public void TearDown()
    {
        if (loot != null)
            Object.DestroyImmediate(loot);
        if (weapon != null)
            Object.DestroyImmediate(weapon);
        if (blueprint != null)
            Object.DestroyImmediate(blueprint);
        if (relic != null)
            Object.DestroyImmediate(relic);
        if (relicTable != null)
            Object.DestroyImmediate(relicTable);
        if (difficultyConfig != null)
            Object.DestroyImmediate(difficultyConfig);
    }

    [Test]
    public void TryCreateLootOffer_CreatesUnsoldOfferAndRollsClampedPrice()
    {
        loot = ScriptableObject.CreateInstance<LootItem_SO>();
        MerchantCatalog_SO.LootGoodsEntry goods = new MerchantCatalog_SO.LootGoodsEntry
        {
            lootItem = loot,
            minPrice = -5,
            maxPrice = -1
        };

        bool created = MerchantOfferFactoryService.TryCreateLootOffer(goods, new System.Random(1), out MerchantOfferState offer);

        Assert.IsTrue(created);
        Assert.AreEqual(MerchantItemType.Loot, offer.itemType);
        Assert.AreSame(loot, offer.lootItem);
        Assert.IsNull(offer.weaponData);
        Assert.IsNull(offer.blueprintData);
        Assert.IsNull(offer.relicData);
        Assert.AreEqual(1, offer.price);
        Assert.IsFalse(offer.isSoldOut);
    }

    [Test]
    public void TryCreateWeaponOffer_CreatesUnsoldOfferAndUsesEntryPriceRange()
    {
        weapon = ScriptableObject.CreateInstance<Weapon_SO>();
        MerchantCatalog_SO.WeaponGoodsEntry goods = new MerchantCatalog_SO.WeaponGoodsEntry
        {
            weaponData = weapon,
            minPrice = 7,
            maxPrice = 7
        };

        bool created = MerchantOfferFactoryService.TryCreateWeaponOffer(goods, new System.Random(2), out MerchantOfferState offer);

        Assert.IsTrue(created);
        Assert.AreEqual(MerchantItemType.Weapon, offer.itemType);
        Assert.AreSame(weapon, offer.weaponData);
        Assert.IsNull(offer.lootItem);
        Assert.IsNull(offer.blueprintData);
        Assert.IsNull(offer.relicData);
        Assert.AreEqual(7, offer.price);
        Assert.IsFalse(offer.isSoldOut);
    }

    [Test]
    public void TryCreateBlueprintOffer_CreatesUnsoldOfferAndUsesEntryPriceRange()
    {
        blueprint = ScriptableObject.CreateInstance<CraftingBlueprint_SO>();
        MerchantCatalog_SO.BlueprintGoodsEntry goods = new MerchantCatalog_SO.BlueprintGoodsEntry
        {
            blueprintData = blueprint,
            minPrice = 11,
            maxPrice = 11
        };

        bool created = MerchantOfferFactoryService.TryCreateBlueprintOffer(goods, new System.Random(3), out MerchantOfferState offer);

        Assert.IsTrue(created);
        Assert.AreEqual(MerchantItemType.Blueprint, offer.itemType);
        Assert.AreSame(blueprint, offer.blueprintData);
        Assert.IsNull(offer.lootItem);
        Assert.IsNull(offer.weaponData);
        Assert.IsNull(offer.relicData);
        Assert.AreEqual(11, offer.price);
        Assert.IsFalse(offer.isSoldOut);
    }

    [Test]
    public void ApplyOfferData_ClearsPreviousPayloadAndResetsSoldOut()
    {
        loot = ScriptableObject.CreateInstance<LootItem_SO>();
        weapon = ScriptableObject.CreateInstance<Weapon_SO>();
        MerchantOfferState target = new MerchantOfferState
        {
            itemType = MerchantItemType.Loot,
            lootItem = loot,
            price = 99,
            isSoldOut = true
        };
        MerchantCatalog_SO.WeaponGoodsEntry goods = new MerchantCatalog_SO.WeaponGoodsEntry
        {
            weaponData = weapon,
            minPrice = 5,
            maxPrice = 5
        };
        MerchantOfferFactoryService.TryCreateWeaponOffer(goods, new System.Random(4), out MerchantOfferState source);

        MerchantOfferFactoryService.ApplyOfferData(target, source);

        Assert.AreEqual(MerchantItemType.Weapon, target.itemType);
        Assert.IsNull(target.lootItem);
        Assert.AreSame(weapon, target.weaponData);
        Assert.IsNull(target.blueprintData);
        Assert.IsNull(target.relicData);
        Assert.AreEqual(5, target.price);
        Assert.IsFalse(target.isSoldOut);
    }

    [Test]
    public void TryCreateRelicOffer_UsesLegacyRelicDifficultyPriceAndAddsExclusion()
    {
        relic = CreateRelic(LootRarity.Rare);
        difficultyConfig = CreateDifficultyWithFixedRelicPrice(LootRarity.Rare, 23);
        HashSet<Relic_SO> excludedRelics = new HashSet<Relic_SO>();
        MerchantCatalog_SO.RelicGoodsEntry goods = CreateRelicGoodsEntry(null, relic);

        bool created = MerchantOfferFactoryService.TryCreateRelicOffer(
            goods,
            excludedRelics,
            difficultyConfig,
            new System.Random(5),
            out MerchantOfferState offer);

        Assert.IsTrue(created);
        Assert.AreEqual(MerchantItemType.Relic, offer.itemType);
        Assert.AreSame(relic, offer.relicData);
        Assert.IsNull(offer.lootItem);
        Assert.IsNull(offer.weaponData);
        Assert.IsNull(offer.blueprintData);
        Assert.AreEqual(23, offer.price);
        Assert.IsFalse(offer.isSoldOut);
        CollectionAssert.Contains(excludedRelics, relic);
    }

    [Test]
    public void TryCreateRelicOffer_DrawsFromTableAndSkipsExcludedRelics()
    {
        relic = CreateRelic(LootRarity.Uncommon);
        relicTable = CreateRelicTable(relic);
        difficultyConfig = CreateDifficultyWithFixedRelicPrice(LootRarity.Uncommon, 17);
        HashSet<Relic_SO> excludedRelics = new HashSet<Relic_SO>();
        MerchantCatalog_SO.RelicGoodsEntry goods = CreateRelicGoodsEntry(relicTable, null);

        bool created = MerchantOfferFactoryService.TryCreateRelicOffer(
            goods,
            excludedRelics,
            difficultyConfig,
            new System.Random(6),
            out MerchantOfferState offer);

        Assert.IsTrue(created);
        Assert.AreSame(relic, offer.relicData);
        Assert.AreEqual(17, offer.price);
        CollectionAssert.Contains(excludedRelics, relic);

        Assert.IsFalse(MerchantOfferFactoryService.TryCreateRelicOffer(
            goods,
            excludedRelics,
            difficultyConfig,
            new System.Random(7),
            out MerchantOfferState exhaustedOffer));
        Assert.IsNull(exhaustedOffer);
    }

    [Test]
    public void TryCreateOffer_ReturnsFalseForMissingDataOrRandom()
    {
        Assert.IsFalse(MerchantOfferFactoryService.TryCreateLootOffer(null, new System.Random(5), out MerchantOfferState nullGoodsOffer));
        Assert.IsNull(nullGoodsOffer);

        MerchantCatalog_SO.WeaponGoodsEntry missingWeapon = new MerchantCatalog_SO.WeaponGoodsEntry();
        Assert.IsFalse(MerchantOfferFactoryService.TryCreateWeaponOffer(missingWeapon, new System.Random(6), out MerchantOfferState missingDataOffer));
        Assert.IsNull(missingDataOffer);

        blueprint = ScriptableObject.CreateInstance<CraftingBlueprint_SO>();
        MerchantCatalog_SO.BlueprintGoodsEntry goods = new MerchantCatalog_SO.BlueprintGoodsEntry { blueprintData = blueprint };
        Assert.IsFalse(MerchantOfferFactoryService.TryCreateBlueprintOffer(goods, null, out MerchantOfferState nullRandomOffer));
        Assert.IsNull(nullRandomOffer);

        relic = CreateRelic(LootRarity.Common);
        MerchantCatalog_SO.RelicGoodsEntry relicGoods = CreateRelicGoodsEntry(null, relic);
        Assert.IsFalse(MerchantOfferFactoryService.TryCreateRelicOffer(relicGoods, null, difficultyConfig, null, out MerchantOfferState relicNullRandomOffer));
        Assert.IsNull(relicNullRandomOffer);
    }

    private static MerchantCatalog_SO.RelicGoodsEntry CreateRelicGoodsEntry(RelicTable_SO table, Relic_SO legacyRelic)
    {
        MerchantCatalog_SO.RelicGoodsEntry entry = new MerchantCatalog_SO.RelicGoodsEntry
        {
            relicTable = table,
            weight = 1
        };
        SetPrivateField(entry, "legacyRelicData", legacyRelic);
        return entry;
    }

    private Relic_SO CreateRelic(LootRarity rarity)
    {
        Relic_SO createdRelic = ScriptableObject.CreateInstance<Relic_SO>();
        createdRelic.relicName = rarity.ToString();
        createdRelic.rarity = rarity;
        return createdRelic;
    }

    private RelicTable_SO CreateRelicTable(params Relic_SO[] relics)
    {
        RelicTable_SO table = ScriptableObject.CreateInstance<RelicTable_SO>();
        SetPrivateField(table, "relics", new List<Relic_SO>(relics));
        SetPrivateField(table, "rarityWeights", new List<RelicTable_SO.RarityWeightEntry>
        {
            new RelicTable_SO.RarityWeightEntry { rarity = LootRarity.Common, weight = 1f },
            new RelicTable_SO.RarityWeightEntry { rarity = LootRarity.Uncommon, weight = 1f },
            new RelicTable_SO.RarityWeightEntry { rarity = LootRarity.Rare, weight = 1f },
            new RelicTable_SO.RarityWeightEntry { rarity = LootRarity.Epic, weight = 1f },
            new RelicTable_SO.RarityWeightEntry { rarity = LootRarity.Legendary, weight = 1f }
        });
        return table;
    }

    private static DifficultyConfig_SO CreateDifficultyWithFixedRelicPrice(LootRarity rarity, int price)
    {
        DifficultyConfig_SO config = ScriptableObject.CreateInstance<DifficultyConfig_SO>();
        config.merchantRelicPrices = new List<DifficultyConfig_SO.MerchantRelicPriceEntry>
        {
            new DifficultyConfig_SO.MerchantRelicPriceEntry
            {
                rarity = rarity,
                minPrice = price,
                maxPrice = price
            }
        };
        return config;
    }

    private static void SetPrivateField<T>(object target, string fieldName, T value)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field, $"Missing field {fieldName}.");
        field.SetValue(target, value);
    }
}
