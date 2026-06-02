using System.Collections.Generic;

public static class MapEventEffectSequenceBuilder
{
    public static List<MapEventBuiltInEffectData> BuildAllEffects(MapEventOptionData optionData)
    {
        List<MapEventBuiltInEffectData> allEffects = new List<MapEventBuiltInEffectData>();
        MapEventBuiltInEffectData primaryEffect = BuildPrimaryEffect(optionData);
        if (primaryEffect != null)
            allEffects.Add(primaryEffect);

        if (optionData != null && optionData.additionalBuiltInEffects != null)
        {
            for (int i = 0; i < optionData.additionalBuiltInEffects.Count; i++)
            {
                MapEventBuiltInEffectData effectData = optionData.additionalBuiltInEffects[i];
                if (effectData != null)
                    allEffects.Add(effectData);
            }
        }

        return allEffects;
    }

    public static List<MapEventBuiltInEffectData> BuildAllEffects(MapEventPossibleResultData resultData)
    {
        List<MapEventBuiltInEffectData> allEffects = new List<MapEventBuiltInEffectData>();
        if (resultData == null)
            return allEffects;

        MapEventBuiltInEffectData primaryEffect = CloneEffect(resultData.primaryEffect);
        if (primaryEffect != null)
            allEffects.Add(primaryEffect);

        if (resultData.additionalBuiltInEffects != null)
        {
            for (int i = 0; i < resultData.additionalBuiltInEffects.Count; i++)
            {
                MapEventBuiltInEffectData effectData = CloneEffect(resultData.additionalBuiltInEffects[i]);
                if (effectData != null)
                    allEffects.Add(effectData);
            }
        }

        return allEffects;
    }

    public static MapEventBuiltInEffectData BuildPrimaryEffect(MapEventOptionData optionData)
    {
        if (optionData == null)
            return null;

        return new MapEventBuiltInEffectData
        {
            effectType = optionData.builtInEffectType,
            effectAmount = optionData.effectAmount,
            randomMoneyMin = optionData.randomMoneyMin,
            randomMoneyMax = optionData.randomMoneyMax,
            randomOwnedLootAmountMin = optionData.randomOwnedLootAmountMin,
            randomOwnedLootAmountMax = optionData.randomOwnedLootAmountMax,
            lootRewards = optionData.lootRewards != null ? new List<MapEventLootRewardEntry>(optionData.lootRewards) : new List<MapEventLootRewardEntry>(),
            lootPickCountMin = optionData.lootPickCountMin,
            lootPickCountMax = optionData.lootPickCountMax,
            relicTable = optionData.relicTable,
            relicRarityWeightTable = optionData.relicRarityWeightTable,
            relicPickCountMin = optionData.relicPickCountMin,
            relicPickCountMax = optionData.relicPickCountMax,
            relicRewards = optionData.relicRewards != null ? new List<MapEventRelicRewardEntry>(optionData.relicRewards) : new List<MapEventRelicRewardEntry>(),
            blueprintTable = optionData.blueprintTable,
            blueprintRarityWeightTable = optionData.blueprintRarityWeightTable,
            blueprintPickCountMin = optionData.blueprintPickCountMin,
            blueprintPickCountMax = optionData.blueprintPickCountMax,
            blueprintRewards = optionData.blueprintRewards != null ? new List<MapEventBlueprintRewardEntry>(optionData.blueprintRewards) : new List<MapEventBlueprintRewardEntry>(),
            encounterSelectionMode = optionData.encounterSelectionMode,
            encounterEnemyGroups = optionData.encounterEnemyGroups != null ? new List<EnemyGroup_SO>(optionData.encounterEnemyGroups) : new List<EnemyGroup_SO>(),
            encounterEnemies = optionData.encounterEnemies != null ? new List<EnemyData_SO>(optionData.encounterEnemies) : new List<EnemyData_SO>(),
            encounterEnemyCountMin = optionData.encounterEnemyCountMin,
            encounterEnemyCountMax = optionData.encounterEnemyCountMax,
            specialDungeon = optionData.specialDungeon
        };
    }

    public static MapEventBuiltInEffectData CloneEffect(MapEventBuiltInEffectData effectData)
    {
        if (effectData == null)
            return null;

        return new MapEventBuiltInEffectData
        {
            effectType = effectData.effectType,
            effectAmount = effectData.effectAmount,
            randomMoneyMin = effectData.randomMoneyMin,
            randomMoneyMax = effectData.randomMoneyMax,
            randomOwnedLootAmountMin = effectData.randomOwnedLootAmountMin,
            randomOwnedLootAmountMax = effectData.randomOwnedLootAmountMax,
            lootRewards = effectData.lootRewards != null ? new List<MapEventLootRewardEntry>(effectData.lootRewards) : new List<MapEventLootRewardEntry>(),
            lootPickCountMin = effectData.lootPickCountMin,
            lootPickCountMax = effectData.lootPickCountMax,
            relicTable = effectData.relicTable,
            relicRarityWeightTable = effectData.relicRarityWeightTable,
            relicPickCountMin = effectData.relicPickCountMin,
            relicPickCountMax = effectData.relicPickCountMax,
            relicRewards = effectData.relicRewards != null ? new List<MapEventRelicRewardEntry>(effectData.relicRewards) : new List<MapEventRelicRewardEntry>(),
            blueprintTable = effectData.blueprintTable,
            blueprintRarityWeightTable = effectData.blueprintRarityWeightTable,
            blueprintPickCountMin = effectData.blueprintPickCountMin,
            blueprintPickCountMax = effectData.blueprintPickCountMax,
            blueprintRewards = effectData.blueprintRewards != null ? new List<MapEventBlueprintRewardEntry>(effectData.blueprintRewards) : new List<MapEventBlueprintRewardEntry>(),
            encounterSelectionMode = effectData.encounterSelectionMode,
            encounterEnemyGroups = effectData.encounterEnemyGroups != null ? new List<EnemyGroup_SO>(effectData.encounterEnemyGroups) : new List<EnemyGroup_SO>(),
            encounterEnemies = effectData.encounterEnemies != null ? new List<EnemyData_SO>(effectData.encounterEnemies) : new List<EnemyData_SO>(),
            encounterEnemyCountMin = effectData.encounterEnemyCountMin,
            encounterEnemyCountMax = effectData.encounterEnemyCountMax,
            specialDungeon = effectData.specialDungeon,
            moneyPercent = effectData.moneyPercent,
            randomMoneyPercentMin = effectData.randomMoneyPercentMin,
            randomMoneyPercentMax = effectData.randomMoneyPercentMax,
            weaponCatalog = effectData.weaponCatalog,
            weaponRarity = effectData.weaponRarity,
            weaponPickCountMin = effectData.weaponPickCountMin,
            weaponPickCountMax = effectData.weaponPickCountMax
        };
    }

    public static int GetFixedLootDiscardAmount(MapEventLootRewardEntry reward)
    {
        if (reward == null)
            return 0;

        return UnityEngine.Mathf.Max(1, reward.amountMin);
    }

    public static string BuildRandomLootDiscardPreviewKey(MapEventOptionData optionData, int effectIndex)
    {
        string optionId = optionData != null && !string.IsNullOrWhiteSpace(optionData.optionId) ? optionData.optionId : "Option";
        return $"{optionId}::{effectIndex}";
    }

    public static bool IsDeferredEncounterVictoryRewardEffect(MapEventOptionBuiltInEffectType effectType)
    {
        switch (effectType)
        {
            case MapEventOptionBuiltInEffectType.GainLoot:
            case MapEventOptionBuiltInEffectType.GainRandomLootFromList:
            case MapEventOptionBuiltInEffectType.GainRelics:
            case MapEventOptionBuiltInEffectType.GainBlueprints:
            case MapEventOptionBuiltInEffectType.GainWeapons:
                return true;
            default:
                return false;
        }
    }
}
