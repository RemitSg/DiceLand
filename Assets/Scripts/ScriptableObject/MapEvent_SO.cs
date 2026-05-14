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
    StartSpecialDungeon = 17
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
}

[Serializable]
public class MapEventOptionData
{
    public string optionId = "Option";
    public string optionLabel = "选项";
    [Min(0)] public int requiredMoney;
    [TextArea(2, 5)] public string consequenceDescription;
    [TextArea(3, 8)] public string resultContent;
    public MapEventOptionBuiltInEffectType builtInEffectType = MapEventOptionBuiltInEffectType.None;
    public int effectAmount;
    public int randomMoneyMin;
    public int randomMoneyMax;
    [Min(1)] public int randomOwnedLootAmountMin = 1;
    [Min(1)] public int randomOwnedLootAmountMax = 1;
    public List<MapEventLootRewardEntry> lootRewards = new List<MapEventLootRewardEntry>();
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
    public bool resolveEvent = true;
    public MapEventOptionHandler_SO customHandler;

    public string DisplayLabel => string.IsNullOrWhiteSpace(optionLabel) ? "未命名选项" : optionLabel;
}

[CreateAssetMenu(fileName = "MapEvent", menuName = "Events/Map Event")]
public class MapEvent_SO : ScriptableObject
{
    public string eventId = "Event";
    public string eventName = "事件";
    [TextArea(4, 10)] public string eventContent;
    public Sprite eventIllustration;
    public List<MapEventOptionData> options = new List<MapEventOptionData>();

    public string DisplayName => string.IsNullOrWhiteSpace(eventName) ? name : eventName;

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
