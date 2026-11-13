using System;
using System.Collections.Generic;
using UnityEngine;

public enum MapEventOptionBuiltInEffectType
{
    None = 0,
    GainMoney = 1,
    LoseMoney = 2,
    HealAllPartyMembers = 3,
    RestoreAllActionPoints = 4,
    GainLoot = 5,
    AdjustAllPartyMembersHpByMaxHpPercent = 6,
    GainActionPoints = 7,
    GainRandomLootFromList = 8,
    LoseActionPoints = 9,
    StartEncounter = 10,
    GainRelics = 11,
    GainBlueprints = 12,
    GainRandomMoney = 13,
    GainExperience = 14,
    LoseOwnedRandomLoot = 15,
    LoseSpecificLoot = 16,
    StartSpecialDungeon = 17,
    LoseMoneyPercent = 18,
    GainMoneyPercentOfPreSelectionMoney = 19,
    RerollAllMoneyByPercentRange = 20,
    GainWeapons = 21,
    ReforgeOwnedRandomRelicsSameRarity = 22,
    ReforgeSelectedRelicSameRarity = 23,
    ReforgeSelectedBlueprintSameRarity = 24,
    ReforgeSelectedLootSameRarity = 25
}

public enum MapEventEncounterSelectionMode
{
    RandomEnemyGroup = 0,
    RandomEnemies = 1
}

[Serializable]
public class MapEventLootRewardEntry
{
    public LootItem_SO lootItem;
    [Min(1)] public int amountMin = 1;
    [Min(1)] public int amountMax = 1;
}

[Serializable]
public class MapEventRelicRewardEntry
{
    public Relic_SO relicData;
    [Min(1)] public int amount = 1;
}

[Serializable]
public class MapEventBlueprintRewardEntry
{
    public CraftingBlueprint_SO blueprintData;
}

[Serializable]
public class MapEventBuiltInEffectData
{
    public MapEventOptionBuiltInEffectType effectType = MapEventOptionBuiltInEffectType.None;
    public int effectAmount;
    public int randomMoneyMin;
    public int randomMoneyMax;
    [Min(1)] public int randomOwnedLootAmountMin = 1;
    [Min(1)] public int randomOwnedLootAmountMax = 1;
    public List<MapEventLootRewardEntry> lootRewards = new List<MapEventLootRewardEntry>();
    public LootItemTable_SO lootReforgeTable;
    [Min(1)] public int lootPickCountMin = 1;
    [Min(1)] public int lootPickCountMax = 1;
    public RelicTable_SO relicTable;
    public LootRarityWeightTable_SO relicRarityWeightTable;
    [Min(1)] public int relicPickCountMin = 1;
    [Min(1)] public int relicPickCountMax = 1;
    [HideInInspector] public List<MapEventRelicRewardEntry> relicRewards = new List<MapEventRelicRewardEntry>();
    public BlueprintTable_SO blueprintTable;
    public LootRarityWeightTable_SO blueprintRarityWeightTable;
    [Min(1)] public int blueprintPickCountMin = 1;
    [Min(1)] public int blueprintPickCountMax = 1;
    [HideInInspector] public List<MapEventBlueprintRewardEntry> blueprintRewards = new List<MapEventBlueprintRewardEntry>();
    public MapEventEncounterSelectionMode encounterSelectionMode = MapEventEncounterSelectionMode.RandomEnemyGroup;
    public List<EnemyGroup_SO> encounterEnemyGroups = new List<EnemyGroup_SO>();
    public List<EnemyData_SO> encounterEnemies = new List<EnemyData_SO>();
    [Min(1)] public int encounterEnemyCountMin = 1;
    [Min(1)] public int encounterEnemyCountMax = 1;
    public SpecialDungeon_SO specialDungeon;
    [Min(0)] public int moneyPercent;
    [Min(0)] public int randomMoneyPercentMin;
    [Min(0)] public int randomMoneyPercentMax;
    public WeaponCatalog_SO weaponCatalog;
    public Rarity weaponRarity = Rarity.Legendary;
    [Min(1)] public int weaponPickCountMin = 1;
    [Min(1)] public int weaponPickCountMax = 1;
}

[Serializable]
public class MapEventPossibleResultData
{
    public string resultId = "Result";
    [Min(0f)] public float weight = 1f;
    [TextArea(2, 5)] public string consequenceDescription;
    [TextArea(3, 8)] public string resultContent;
    public MapEventBuiltInEffectData primaryEffect = new MapEventBuiltInEffectData();
    public List<MapEventBuiltInEffectData> additionalBuiltInEffects = new List<MapEventBuiltInEffectData>();
    [Header("Follow-up Event")]
    public MapEvent_SO nextEvent;
    public bool resolveEvent = true;
}

[Serializable]
public class MapEventOptionData
{
    public string optionId = "Option";
    public string optionLabel = "选项";
    [Min(0)] public int requiredMoney;
    [Min(0)] public int requiredMoneyPercent;
    [TextArea(2, 5)] public string consequenceDescription;
    [TextArea(3, 8)] public string resultContent;
    public MapEventOptionBuiltInEffectType builtInEffectType = MapEventOptionBuiltInEffectType.None;
    public int effectAmount;
    public int randomMoneyMin;
    public int randomMoneyMax;
    [Min(1)] public int randomOwnedLootAmountMin = 1;
    [Min(1)] public int randomOwnedLootAmountMax = 1;
    public List<MapEventLootRewardEntry> lootRewards = new List<MapEventLootRewardEntry>();
    public LootItemTable_SO lootReforgeTable;
    [Min(1)] public int lootPickCountMin = 1;
    [Min(1)] public int lootPickCountMax = 1;
    public RelicTable_SO relicTable;
    public LootRarityWeightTable_SO relicRarityWeightTable;
    [Min(1)] public int relicPickCountMin = 1;
    [Min(1)] public int relicPickCountMax = 1;
    [HideInInspector] public List<MapEventRelicRewardEntry> relicRewards = new List<MapEventRelicRewardEntry>();
    public BlueprintTable_SO blueprintTable;
    public LootRarityWeightTable_SO blueprintRarityWeightTable;
    [Min(1)] public int blueprintPickCountMin = 1;
    [Min(1)] public int blueprintPickCountMax = 1;
    [HideInInspector] public List<MapEventBlueprintRewardEntry> blueprintRewards = new List<MapEventBlueprintRewardEntry>();
    public MapEventEncounterSelectionMode encounterSelectionMode = MapEventEncounterSelectionMode.RandomEnemyGroup;
    public List<EnemyGroup_SO> encounterEnemyGroups = new List<EnemyGroup_SO>();
    public List<EnemyData_SO> encounterEnemies = new List<EnemyData_SO>();
    [Min(1)] public int encounterEnemyCountMin = 1;
    [Min(1)] public int encounterEnemyCountMax = 1;
    public SpecialDungeon_SO specialDungeon;
    public List<MapEventBuiltInEffectData> additionalBuiltInEffects = new List<MapEventBuiltInEffectData>();
    public List<MapEventPossibleResultData> possibleResults = new List<MapEventPossibleResultData>();
    [Header("Follow-up Event")]
    public MapEvent_SO nextEvent;
    [Tooltip("选择后直接进入后续事件，省略中间结果页，并隐藏后续事件预告。")]
    public bool openFollowUpImmediately;
    public bool resolveEvent = true;
    public MapEventOptionHandler_SO customHandler;
    [Tooltip("特殊副本跳转：-1 不跳转，-2 完成并离开副本，0 起为有效格子列表索引。")]
    public int specialDungeonDestinationEntryIndex = -1;

    public string DisplayLabel => string.IsNullOrWhiteSpace(optionLabel)
        ? GameText.Get("ui.event.unnamed_choice", "未命名选项") : optionLabel;
}

[CreateAssetMenu(fileName = "MapEvent", menuName = "Events/Map Event")]
public class MapEvent_SO : ScriptableObject
{
    public string eventId = "Event";
    public string eventName = "事件";
    [TextArea(4, 10)] public string eventContent;
    public Sprite eventIllustration;
    public bool hideConsequences;
    public List<MapEventOptionData> options = new List<MapEventOptionData>();

    public string DisplayName => string.IsNullOrWhiteSpace(eventName) ? name : eventName;
    public string LocalizedName => Localize("name", DisplayName);

    public string Localize(string field, string source)
    {
        return string.IsNullOrWhiteSpace(eventId) ? source : GameText.Content("event." + eventId, field, source);
    }

    public string LocalizeOption(MapEventOptionData option, string field, string source)
    {
        return option != null && !string.IsNullOrWhiteSpace(option.optionId)
            ? Localize($"option.{option.optionId}.{field}", source) : source;
    }

    public string LocalizeResult(MapEventOptionData option, MapEventPossibleResultData result, string field, string source)
    {
        return result != null && !string.IsNullOrWhiteSpace(result.resultId)
            ? LocalizeOption(option, $"result.{result.resultId}.{field}", source) : source;
    }

    public IReadOnlyList<MapEventOptionData> GetValidOptions()
    {
        if (options == null || options.Count == 0)
            return Array.Empty<MapEventOptionData>();

        List<MapEventOptionData> valid = new List<MapEventOptionData>(options.Count);
        for (int i = 0; i < options.Count; i++)
        {
            MapEventOptionData option = options[i];
            if (option != null)
                valid.Add(option);
        }

        return valid;
    }
}
