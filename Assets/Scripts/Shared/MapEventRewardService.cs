using System.Collections.Generic;
using UnityEngine;

public static class MapEventRewardService
{
    public static bool TryExecuteLootReward(RunState runState, MapEventBuiltInEffectData effectData, out string message)
    {
        if (!TryBuildAvailableLootRewards(effectData, out List<MapEventLootRewardEntry> availableRewards, out message))
            return false;

        int pickCount = RollPickCount(effectData.lootPickCountMin, effectData.lootPickCountMax, availableRewards.Count);
        List<string> gainedLootDescriptions = new List<string>(pickCount);

        for (int i = 0; i < pickCount; i++)
        {
            int selectedIndex = UnityEngine.Random.Range(0, availableRewards.Count);
            MapEventLootRewardEntry selectedReward = availableRewards[selectedIndex];
            availableRewards.RemoveAt(selectedIndex);

            int amount = RollLootAmount(selectedReward);
            runState?.AddLoot(selectedReward.lootItem, amount);
            gainedLootDescriptions.Add($"{selectedReward.lootItem.lootName} x{amount}");
        }

        message = gainedLootDescriptions.Count > 0
            ? $"获得战利品：{string.Join("，", gainedLootDescriptions)}。"
            : "未获得任何战利品。";
        return true;
    }

    public static bool TryExecuteRelicReward(RunState runState, MapEventBuiltInEffectData effectData, out string message)
    {
        if (!TryBuildAvailableRelicRewards(effectData, out List<MapEventRelicRewardEntry> availableRewards, out message))
            return false;

        int pickCount = RollPickCount(effectData.relicPickCountMin, effectData.relicPickCountMax, availableRewards.Count);
        List<string> gainedRelicDescriptions = new List<string>(pickCount);

        for (int i = 0; i < pickCount; i++)
        {
            int selectedIndex = UnityEngine.Random.Range(0, availableRewards.Count);
            MapEventRelicRewardEntry selectedReward = availableRewards[selectedIndex];
            availableRewards.RemoveAt(selectedIndex);

            runState?.AddRelic(selectedReward.relicData, 1);
            gainedRelicDescriptions.Add(selectedReward.relicData.DisplayName);
        }

        message = gainedRelicDescriptions.Count > 0
            ? $"获得遗物：{string.Join("，", gainedRelicDescriptions)}。"
            : "未获得任何遗物。";
        return true;
    }

    public static bool TryExecuteBlueprintReward(RunState runState, MapEventBuiltInEffectData effectData, out string message)
    {
        if (!TryBuildAvailableBlueprintRewards(effectData, out List<MapEventBlueprintRewardEntry> availableRewards, out message))
            return false;

        int pickCount = RollPickCount(effectData.blueprintPickCountMin, effectData.blueprintPickCountMax, availableRewards.Count);
        List<string> gainedBlueprintDescriptions = new List<string>(pickCount);

        for (int i = 0; i < pickCount; i++)
        {
            int selectedIndex = UnityEngine.Random.Range(0, availableRewards.Count);
            MapEventBlueprintRewardEntry selectedReward = availableRewards[selectedIndex];
            availableRewards.RemoveAt(selectedIndex);

            runState?.AddBlueprint(selectedReward.blueprintData, 1);
            gainedBlueprintDescriptions.Add(selectedReward.blueprintData.DisplayName);
        }

        message = gainedBlueprintDescriptions.Count > 0
            ? $"获得图纸：{string.Join("，", gainedBlueprintDescriptions)}。"
            : "未获得任何图纸。";
        return true;
    }

    public static bool TryQueueEncounterVictoryReward(BattleEncounterData encounterData, MapEventBuiltInEffectData effectData, out string message)
    {
        if (encounterData == null)
        {
            message = "未找到待触发的事件遭遇。";
            return false;
        }

        switch (effectData.effectType)
        {
            case MapEventOptionBuiltInEffectType.GainLoot:
            case MapEventOptionBuiltInEffectType.GainRandomLootFromList:
                return TryQueueEncounterVictoryLootRewards(encounterData, effectData, out message);
            case MapEventOptionBuiltInEffectType.GainRelics:
                return TryQueueEncounterVictoryRelicRewards(encounterData, effectData, out message);
            case MapEventOptionBuiltInEffectType.GainBlueprints:
                return TryQueueEncounterVictoryBlueprintRewards(encounterData, effectData, out message);
            default:
                message = string.Empty;
                return true;
        }
    }

    private static bool TryQueueEncounterVictoryLootRewards(BattleEncounterData encounterData, MapEventBuiltInEffectData effectData, out string message)
    {
        if (!TryBuildAvailableLootRewards(effectData, out List<MapEventLootRewardEntry> availableRewards, out message))
            return false;

        int pickCount = RollPickCount(effectData.lootPickCountMin, effectData.lootPickCountMax, availableRewards.Count);
        List<string> descriptions = new List<string>(pickCount);

        for (int i = 0; i < pickCount; i++)
        {
            int selectedIndex = UnityEngine.Random.Range(0, availableRewards.Count);
            MapEventLootRewardEntry selectedReward = availableRewards[selectedIndex];
            availableRewards.RemoveAt(selectedIndex);

            int amount = RollLootAmount(selectedReward);
            encounterData.pendingVictoryLootRewards.Add(new RunLootState
            {
                lootItem = selectedReward.lootItem,
                amount = amount
            });
            descriptions.Add($"{selectedReward.lootItem.lootName} x{amount}");
        }

        message = descriptions.Count > 0
            ? $"战斗胜利后将获得战利品：{string.Join("，", descriptions)}。"
            : "战斗胜利后未配置任何战利品奖励。";
        return true;
    }

    private static bool TryQueueEncounterVictoryRelicRewards(BattleEncounterData encounterData, MapEventBuiltInEffectData effectData, out string message)
    {
        if (!TryBuildAvailableRelicRewards(effectData, out List<MapEventRelicRewardEntry> availableRewards, out message))
            return false;

        int pickCount = RollPickCount(effectData.relicPickCountMin, effectData.relicPickCountMax, availableRewards.Count);
        List<string> descriptions = new List<string>(pickCount);

        for (int i = 0; i < pickCount; i++)
        {
            int selectedIndex = UnityEngine.Random.Range(0, availableRewards.Count);
            MapEventRelicRewardEntry selectedReward = availableRewards[selectedIndex];
            availableRewards.RemoveAt(selectedIndex);

            encounterData.pendingVictoryRelicRewards.Add(new RunRelicState
            {
                relicData = selectedReward.relicData,
                amount = 1
            });
            descriptions.Add(selectedReward.relicData.DisplayName);
        }

        message = descriptions.Count > 0
            ? $"战斗胜利后将获得遗物：{string.Join("，", descriptions)}。"
            : "战斗胜利后未配置任何遗物奖励。";
        return true;
    }

    private static bool TryQueueEncounterVictoryBlueprintRewards(BattleEncounterData encounterData, MapEventBuiltInEffectData effectData, out string message)
    {
        if (!TryBuildAvailableBlueprintRewards(effectData, out List<MapEventBlueprintRewardEntry> availableRewards, out message))
            return false;

        int pickCount = RollPickCount(effectData.blueprintPickCountMin, effectData.blueprintPickCountMax, availableRewards.Count);
        List<string> descriptions = new List<string>(pickCount);

        for (int i = 0; i < pickCount; i++)
        {
            int selectedIndex = UnityEngine.Random.Range(0, availableRewards.Count);
            MapEventBlueprintRewardEntry selectedReward = availableRewards[selectedIndex];
            availableRewards.RemoveAt(selectedIndex);

            encounterData.pendingVictoryBlueprintRewards.Add(new RunBlueprintState
            {
                blueprintData = selectedReward.blueprintData,
                amount = 1
            });
            descriptions.Add(selectedReward.blueprintData.DisplayName);
        }

        message = descriptions.Count > 0
            ? $"战斗胜利后将获得图纸：{string.Join("，", descriptions)}。"
            : "战斗胜利后未配置任何图纸奖励。";
        return true;
    }

    private static bool TryBuildAvailableLootRewards(
        MapEventBuiltInEffectData effectData,
        out List<MapEventLootRewardEntry> availableRewards,
        out string message)
    {
        availableRewards = new List<MapEventLootRewardEntry>();
        if (effectData == null || effectData.lootRewards == null || effectData.lootRewards.Count == 0)
        {
            message = "该事件效果未配置战利品列表。";
            return false;
        }

        for (int i = 0; i < effectData.lootRewards.Count; i++)
        {
            MapEventLootRewardEntry reward = effectData.lootRewards[i];
            if (reward != null && reward.lootItem != null)
                availableRewards.Add(reward);
        }

        if (availableRewards.Count == 0)
        {
            message = "该事件效果未配置有效的战利品。";
            return false;
        }

        message = string.Empty;
        return true;
    }

    private static bool TryBuildAvailableRelicRewards(
        MapEventBuiltInEffectData effectData,
        out List<MapEventRelicRewardEntry> availableRewards,
        out string message)
    {
        availableRewards = new List<MapEventRelicRewardEntry>();
        if (effectData == null || effectData.relicRewards == null || effectData.relicRewards.Count == 0)
        {
            message = "该事件效果未配置遗物列表。";
            return false;
        }

        for (int i = 0; i < effectData.relicRewards.Count; i++)
        {
            MapEventRelicRewardEntry reward = effectData.relicRewards[i];
            if (reward != null && reward.relicData != null)
                availableRewards.Add(reward);
        }

        if (availableRewards.Count == 0)
        {
            message = "该事件效果未配置有效的遗物。";
            return false;
        }

        message = string.Empty;
        return true;
    }

    private static bool TryBuildAvailableBlueprintRewards(
        MapEventBuiltInEffectData effectData,
        out List<MapEventBlueprintRewardEntry> availableRewards,
        out string message)
    {
        availableRewards = new List<MapEventBlueprintRewardEntry>();
        if (effectData == null || effectData.blueprintRewards == null || effectData.blueprintRewards.Count == 0)
        {
            message = "该事件效果未配置图纸列表。";
            return false;
        }

        for (int i = 0; i < effectData.blueprintRewards.Count; i++)
        {
            MapEventBlueprintRewardEntry reward = effectData.blueprintRewards[i];
            if (reward != null && reward.blueprintData != null)
                availableRewards.Add(reward);
        }

        if (availableRewards.Count == 0)
        {
            message = "该事件效果未配置有效的图纸。";
            return false;
        }

        message = string.Empty;
        return true;
    }

    private static int RollPickCount(int minPickCount, int maxPickCount, int availableCount)
    {
        int min = Mathf.Max(1, minPickCount);
        int max = Mathf.Max(min, maxPickCount);
        return Mathf.Min(UnityEngine.Random.Range(min, max + 1), Mathf.Max(0, availableCount));
    }

    private static int RollLootAmount(MapEventLootRewardEntry reward)
    {
        int amountMin = Mathf.Max(1, reward.amountMin);
        int amountMax = Mathf.Max(amountMin, reward.amountMax);
        return UnityEngine.Random.Range(amountMin, amountMax + 1);
    }
}
