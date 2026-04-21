using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public enum MerchantItemType
{
    Loot = 0,
    Weapon = 1,
    Blueprint = 2,
    Relic = 3
}

[CreateAssetMenu(fileName = "MerchantCatalog", menuName = "Merchant/Merchant Catalog")]
public class MerchantCatalog_SO : ScriptableObject
{
    [Serializable]
    public sealed class CategoryMinimumEntry
    {
        [Min(0)] public int minimumCount = 0;
        [Min(0)] public int randomWeight = 1;

        public int ResolveMinimumCount()
        {
            return Mathf.Max(0, minimumCount);
        }

        public int ResolveRandomWeight()
        {
            return Mathf.Max(0, randomWeight);
        }
    }

    [Serializable]
    public sealed class LootGoodsEntry
    {
        public string goodsId;
        public LootItem_SO lootItem;
        [Min(1)] public int minPrice = 1;
        [Min(1)] public int maxPrice = 5;
        [Min(1)] public int weight = 1;

        public bool IsValid => lootItem != null;
    }

    [Serializable]
    public sealed class WeaponGoodsEntry
    {
        public string goodsId;
        public Weapon_SO weaponData;
        [Min(1)] public int minPrice = 1;
        [Min(1)] public int maxPrice = 5;
        [Min(1)] public int weight = 1;

        public bool IsValid => weaponData != null;
    }

    [Serializable]
    public sealed class BlueprintGoodsEntry
    {
        public string goodsId;
        public CraftingBlueprint_SO blueprintData;
        [Min(1)] public int minPrice = 1;
        [Min(1)] public int maxPrice = 5;
        [Min(1)] public int weight = 1;

        public bool IsValid => blueprintData != null;
    }

    [Serializable]
    public sealed class RelicGoodsEntry
    {
        public string goodsId;
        public RelicTable_SO relicTable;
        [FormerlySerializedAs("relicData")]
        [SerializeField, HideInInspector] private Relic_SO legacyRelicData;
        [Min(1)] public int minPrice = 1;
        [Min(1)] public int maxPrice = 5;
        [Min(1)] public int weight = 1;

        public Relic_SO LegacyRelicData => legacyRelicData;
        public bool IsValid => (relicTable != null && relicTable.IsValid) || legacyRelicData != null;
    }

    [Serializable]
    private sealed class LegacyMerchantGoodsEntry
    {
        public string goodsId;
        public MerchantItemType itemType;
        public LootItem_SO lootItem;
        public Weapon_SO weaponData;
        public CraftingBlueprint_SO blueprintData;
        [Min(1)] public int minPrice = 1;
        [Min(1)] public int maxPrice = 5;
        [Min(1)] public int weight = 1;
    }

    [Header("Total Stock Count")]
    [Min(0)] public int minTotalGoodsCount = 4;
    [Min(0)] public int maxTotalGoodsCount = 6;

    [Header("Guaranteed Count By Type")]
    public CategoryMinimumEntry lootMinimumCount = new CategoryMinimumEntry { minimumCount = 1, randomWeight = 4 };
    public CategoryMinimumEntry weaponMinimumCount = new CategoryMinimumEntry { minimumCount = 1, randomWeight = 2 };
    public CategoryMinimumEntry blueprintMinimumCount = new CategoryMinimumEntry { minimumCount = 0, randomWeight = 1 };
    public CategoryMinimumEntry relicMinimumCount = new CategoryMinimumEntry { minimumCount = 0, randomWeight = 1 };

    [Header("Loot Pool")]
    public List<LootGoodsEntry> lootEntries = new List<LootGoodsEntry>();

    [Header("Weapon Pool")]
    public List<WeaponGoodsEntry> weaponEntries = new List<WeaponGoodsEntry>();

    [Header("Blueprint Pool")]
    public List<BlueprintGoodsEntry> blueprintEntries = new List<BlueprintGoodsEntry>();

    [Header("Relic Table Pool")]
    public List<RelicGoodsEntry> relicEntries = new List<RelicGoodsEntry>();

    [SerializeField, HideInInspector] private List<LegacyMerchantGoodsEntry> goodsEntries = new List<LegacyMerchantGoodsEntry>();

    public IReadOnlyList<LootGoodsEntry> GetValidLootEntries()
    {
        return GetValidEntries(lootEntries, entry => entry != null && entry.IsValid && Mathf.Max(1, entry.weight) > 0);
    }

    public IReadOnlyList<WeaponGoodsEntry> GetValidWeaponEntries()
    {
        return GetValidEntries(weaponEntries, entry => entry != null && entry.IsValid && Mathf.Max(1, entry.weight) > 0);
    }

    public IReadOnlyList<BlueprintGoodsEntry> GetValidBlueprintEntries()
    {
        return GetValidEntries(blueprintEntries, entry => entry != null && entry.IsValid && Mathf.Max(1, entry.weight) > 0);
    }

    public IReadOnlyList<RelicGoodsEntry> GetValidRelicEntries()
    {
        return GetValidEntries(relicEntries, entry => entry != null && entry.IsValid && Mathf.Max(1, entry.weight) > 0);
    }

    private static IReadOnlyList<TEntry> GetValidEntries<TEntry>(List<TEntry> source, Predicate<TEntry> predicate) where TEntry : class
    {
        if (source == null)
            return Array.Empty<TEntry>();

        List<TEntry> valid = new List<TEntry>();
        for (int i = 0; i < source.Count; i++)
        {
            TEntry entry = source[i];
            if (predicate(entry))
                valid.Add(entry);
        }

        return valid;
    }

    private void OnValidate()
    {
        minTotalGoodsCount = Mathf.Max(0, minTotalGoodsCount);
        maxTotalGoodsCount = Mathf.Max(minTotalGoodsCount, maxTotalGoodsCount);

        lootMinimumCount = lootMinimumCount ?? new CategoryMinimumEntry();
        weaponMinimumCount = weaponMinimumCount ?? new CategoryMinimumEntry();
        blueprintMinimumCount = blueprintMinimumCount ?? new CategoryMinimumEntry();
        relicMinimumCount = relicMinimumCount ?? new CategoryMinimumEntry();

        ClampMinimumEntry(lootMinimumCount);
        ClampMinimumEntry(weaponMinimumCount);
        ClampMinimumEntry(blueprintMinimumCount);
        ClampMinimumEntry(relicMinimumCount);

        if (lootEntries == null)
            lootEntries = new List<LootGoodsEntry>();
        if (weaponEntries == null)
            weaponEntries = new List<WeaponGoodsEntry>();
        if (blueprintEntries == null)
            blueprintEntries = new List<BlueprintGoodsEntry>();
        if (relicEntries == null)
            relicEntries = new List<RelicGoodsEntry>();

        MigrateLegacyEntries();
        ClampPricesAndWeights();
    }

    private static void ClampMinimumEntry(CategoryMinimumEntry entry)
    {
        if (entry == null)
            return;

        entry.minimumCount = Mathf.Max(0, entry.minimumCount);
        entry.randomWeight = Mathf.Max(0, entry.randomWeight);
    }

    private void ClampPricesAndWeights()
    {
        ClampEntries(lootEntries, entry =>
        {
            entry.minPrice = Mathf.Max(1, entry.minPrice);
            entry.maxPrice = Mathf.Max(entry.minPrice, entry.maxPrice);
            entry.weight = Mathf.Max(1, entry.weight);
        });

        ClampEntries(weaponEntries, entry =>
        {
            entry.minPrice = Mathf.Max(1, entry.minPrice);
            entry.maxPrice = Mathf.Max(entry.minPrice, entry.maxPrice);
            entry.weight = Mathf.Max(1, entry.weight);
        });

        ClampEntries(blueprintEntries, entry =>
        {
            entry.minPrice = Mathf.Max(1, entry.minPrice);
            entry.maxPrice = Mathf.Max(entry.minPrice, entry.maxPrice);
            entry.weight = Mathf.Max(1, entry.weight);
        });

        ClampEntries(relicEntries, entry =>
        {
            entry.minPrice = Mathf.Max(1, entry.minPrice);
            entry.maxPrice = Mathf.Max(entry.minPrice, entry.maxPrice);
            entry.weight = Mathf.Max(1, entry.weight);
        });
    }

    private static void ClampEntries<TEntry>(List<TEntry> source, Action<TEntry> clampAction)
    {
        if (source == null)
            return;

        for (int i = 0; i < source.Count; i++)
        {
            if (source[i] == null)
                continue;

            clampAction(source[i]);
        }
    }

    private void MigrateLegacyEntries()
    {
        if (goodsEntries == null || goodsEntries.Count == 0)
            return;

        for (int i = 0; i < goodsEntries.Count; i++)
        {
            LegacyMerchantGoodsEntry legacy = goodsEntries[i];
            if (legacy == null)
                continue;

            switch (legacy.itemType)
            {
                case MerchantItemType.Loot:
                    if (legacy.lootItem != null && !ContainsLootEntry(legacy.lootItem))
                    {
                        lootEntries.Add(new LootGoodsEntry
                        {
                            goodsId = legacy.goodsId,
                            lootItem = legacy.lootItem,
                            minPrice = legacy.minPrice,
                            maxPrice = legacy.maxPrice,
                            weight = legacy.weight
                        });
                    }
                    break;
                case MerchantItemType.Weapon:
                    if (legacy.weaponData != null && !ContainsWeaponEntry(legacy.weaponData))
                    {
                        weaponEntries.Add(new WeaponGoodsEntry
                        {
                            goodsId = legacy.goodsId,
                            weaponData = legacy.weaponData,
                            minPrice = legacy.minPrice,
                            maxPrice = legacy.maxPrice,
                            weight = legacy.weight
                        });
                    }
                    break;
                case MerchantItemType.Blueprint:
                    if (legacy.blueprintData != null && !ContainsBlueprintEntry(legacy.blueprintData))
                    {
                        blueprintEntries.Add(new BlueprintGoodsEntry
                        {
                            goodsId = legacy.goodsId,
                            blueprintData = legacy.blueprintData,
                            minPrice = legacy.minPrice,
                            maxPrice = legacy.maxPrice,
                            weight = legacy.weight
                        });
                    }
                    break;
            }
        }

        goodsEntries.Clear();
    }

    private bool ContainsLootEntry(LootItem_SO lootItem)
    {
        for (int i = 0; i < lootEntries.Count; i++)
        {
            if (lootEntries[i] != null && lootEntries[i].lootItem == lootItem)
                return true;
        }

        return false;
    }

    private bool ContainsWeaponEntry(Weapon_SO weaponData)
    {
        for (int i = 0; i < weaponEntries.Count; i++)
        {
            if (weaponEntries[i] != null && weaponEntries[i].weaponData == weaponData)
                return true;
        }

        return false;
    }

    private bool ContainsBlueprintEntry(CraftingBlueprint_SO blueprintData)
    {
        for (int i = 0; i < blueprintEntries.Count; i++)
        {
            if (blueprintEntries[i] != null && blueprintEntries[i].blueprintData == blueprintData)
                return true;
        }

        return false;
    }
}
