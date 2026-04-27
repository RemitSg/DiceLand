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
            relicRewards = optionData.relicRewards != null ? new List<MapEventRelicRewardEntry>(optionData.relicRewards) : new List<MapEventRelicRewardEntry>(),
            relicPickCountMin = optionData.relicPickCountMin,
            relicPickCountMax = optionData.relicPickCountMax,
            blueprintRewards = optionData.blueprintRewards != null ? new List<MapEventBlueprintRewardEntry>(optionData.blueprintRewards) : new List<MapEventBlueprintRewardEntry>(),
            blueprintPickCountMin = optionData.blueprintPickCountMin,
            blueprintPickCountMax = optionData.blueprintPickCountMax,
            encounterSelectionMode = optionData.encounterSelectionMode,
            encounterEnemyGroups = optionData.encounterEnemyGroups != null ? new List<EnemyGroup_SO>(optionData.encounterEnemyGroups) : new List<EnemyGroup_SO>(),
            encounterEnemies = optionData.encounterEnemies != null ? new List<EnemyData_SO>(optionData.encounterEnemies) : new List<EnemyData_SO>(),
            encounterEnemyCountMin = optionData.encounterEnemyCountMin,
            encounterEnemyCountMax = optionData.encounterEnemyCountMax
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
                return true;
            default:
                return false;
        }
    }
}
