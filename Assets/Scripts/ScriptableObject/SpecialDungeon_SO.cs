using System;
using System.Collections.Generic;
using UnityEngine;

public enum SpecialDungeonTreasureRewardType
{
    Money = 0,
    Blueprint = 1,
    Relic = 2,
    RelicBag = 3
}

[Serializable]
public sealed class SpecialDungeonMerchantOfferEntry
{
    public MerchantItemType itemType;
    [Min(0)] public int price;
    public LootItem_SO lootItem;
    public Weapon_SO weaponData;
    public CraftingBlueprint_SO blueprintData;
    public Relic_SO relicData;

    public bool IsConfigured
    {
        get
        {
            switch (itemType)
            {
                case MerchantItemType.Loot:
                    return lootItem != null;
                case MerchantItemType.Weapon:
                    return weaponData != null;
                case MerchantItemType.Blueprint:
                    return blueprintData != null;
                case MerchantItemType.Relic:
                    return relicData != null;
                default:
                    return false;
            }
        }
    }
}

[Serializable]
public sealed class SpecialDungeonTreasureOptionEntry
{
    public string title = "Reward";
    [TextArea(2, 5)] public string description;
    public SpecialDungeonTreasureRewardType rewardType = SpecialDungeonTreasureRewardType.Money;
    public Sprite iconOverride;
    [Min(0)] public int moneyAmount;
    public CraftingBlueprint_SO blueprintData;
    public Relic_SO relicData;
    public RelicTable_SO relicTable;
    public LootRarity relicBagRarity = LootRarity.Common;
    [Min(1)] public int relicBagChoiceCount = 3;

    public bool IsConfigured
    {
        get
        {
            switch (rewardType)
            {
                case SpecialDungeonTreasureRewardType.Money:
                    return moneyAmount >= 0;
                case SpecialDungeonTreasureRewardType.Blueprint:
                    return blueprintData != null;
                case SpecialDungeonTreasureRewardType.Relic:
                    return relicData != null;
                case SpecialDungeonTreasureRewardType.RelicBag:
                    return relicTable != null && relicTable.IsValid && relicBagChoiceCount > 0;
                default:
                    return false;
            }
        }
    }
}

[Serializable]
public sealed class SpecialDungeonTileEntry
{
    public MapTileType tileType = MapTileType.Battle;
    public string previewTag;
    public EnemyGroup_SO enemyGroup;
    public List<SpecialDungeonMerchantOfferEntry> merchantOffers = new List<SpecialDungeonMerchantOfferEntry>();
    public List<SpecialDungeonTreasureOptionEntry> treasureOptions = new List<SpecialDungeonTreasureOptionEntry>();
    public MapEvent_SO eventData;

    public bool IsSupportedTileType
    {
        get
        {
            return tileType == MapTileType.Battle
                || tileType == MapTileType.Elite
                || tileType == MapTileType.Merchant
                || tileType == MapTileType.Treasure
                || tileType == MapTileType.Event;
        }
    }
}

[CreateAssetMenu(fileName = "SpecialDungeon", menuName = "Events/Special Dungeon")]
public class SpecialDungeon_SO : ScriptableObject
{
    public string dungeonId = "SpecialDungeon";
    public string dungeonName = "Special Dungeon";
    [TextArea(3, 8)] public string description;
    public List<SpecialDungeonTileEntry> entries = new List<SpecialDungeonTileEntry>();

    public string DisplayName => string.IsNullOrWhiteSpace(dungeonName) ? name : dungeonName;

    public IReadOnlyList<SpecialDungeonTileEntry> GetValidEntries()
    {
        if (entries == null || entries.Count == 0)
            return Array.Empty<SpecialDungeonTileEntry>();

        List<SpecialDungeonTileEntry> valid = new List<SpecialDungeonTileEntry>();
        for (int i = 0; i < entries.Count; i++)
        {
            SpecialDungeonTileEntry entry = entries[i];
            if (entry != null && entry.IsSupportedTileType)
                valid.Add(entry);
        }

        return valid;
    }

    private void OnValidate()
    {
        if (entries == null)
            entries = new List<SpecialDungeonTileEntry>();

        for (int i = 0; i < entries.Count; i++)
        {
            SpecialDungeonTileEntry entry = entries[i];
            if (entry == null)
                continue;

            if (entry.merchantOffers == null)
                entry.merchantOffers = new List<SpecialDungeonMerchantOfferEntry>();
            if (entry.treasureOptions == null)
                entry.treasureOptions = new List<SpecialDungeonTreasureOptionEntry>();

            ClampMerchantOffers(entry.merchantOffers);
            ClampTreasureOptions(entry.treasureOptions);
        }
    }

    private static void ClampMerchantOffers(List<SpecialDungeonMerchantOfferEntry> offers)
    {
        if (offers == null)
            return;

        for (int i = 0; i < offers.Count; i++)
        {
            SpecialDungeonMerchantOfferEntry offer = offers[i];
            if (offer != null)
                offer.price = Mathf.Max(0, offer.price);
        }
    }

    private static void ClampTreasureOptions(List<SpecialDungeonTreasureOptionEntry> options)
    {
        if (options == null)
            return;

        for (int i = 0; i < options.Count; i++)
        {
            SpecialDungeonTreasureOptionEntry option = options[i];
            if (option == null)
                continue;

            option.moneyAmount = Mathf.Max(0, option.moneyAmount);
            option.relicBagChoiceCount = Mathf.Max(1, option.relicBagChoiceCount);
        }
    }
}
