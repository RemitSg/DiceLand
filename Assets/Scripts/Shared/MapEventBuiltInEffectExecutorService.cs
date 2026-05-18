using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class MapEventBuiltInEffectExecutionRequest
{
    public RunState runState;
    public ActionPointSystem actionPointSystem;
    public BoardTileData currentEventTile;
    public MapEventOptionData optionData;
    public LevelTable_SO levelTable;
    public string sourceSceneName;
    public string battleSceneName;
    public int encounterSeedSalt;
    public IDictionary<string, MapEventLootDiscardPreview> randomLootDiscardPreviewCache;
}

public sealed class MapEventBuiltInEffectExecutionResult
{
    public bool success;
    public string message;
    public BattleEncounterData pendingEncounterData;
    public SpecialDungeon_SO pendingSpecialDungeon;
}

public static class MapEventBuiltInEffectExecutorService
{
    public static bool TryExecute(
        MapEventBuiltInEffectExecutionRequest request,
        out MapEventBuiltInEffectExecutionResult result)
    {
        result = new MapEventBuiltInEffectExecutionResult();
        if (request == null || request.optionData == null || request.runState == null)
        {
            result.message = "事件执行失败。";
            return false;
        }

        List<string> effectMessages = new List<string>();
        List<MapEventBuiltInEffectData> allEffects = MapEventEffectSequenceBuilder.BuildAllEffects(request.optionData);

        int encounterEffectIndex = FindFirstEncounterEffectIndex(allEffects);
        if (encounterEffectIndex >= 0)
        {
            if (!TryExecuteEffect(request, encounterEffectIndex, allEffects[encounterEffectIndex], result, out string encounterMessage))
                return false;

            AddMessage(effectMessages, encounterMessage);
        }

        for (int i = 0; i < allEffects.Count; i++)
        {
            MapEventBuiltInEffectData effectData = allEffects[i];
            if (effectData == null || i == encounterEffectIndex)
                continue;

            if (encounterEffectIndex >= 0 && MapEventEffectSequenceBuilder.IsDeferredEncounterVictoryRewardEffect(effectData.effectType))
            {
                if (!MapEventRewardService.TryQueueEncounterVictoryReward(result.pendingEncounterData, effectData, out string deferredMessage))
                {
                    result.message = deferredMessage;
                    return false;
                }

                AddMessage(effectMessages, deferredMessage);
                continue;
            }

            if (!TryExecuteEffect(request, i, effectData, result, out string effectMessage))
                return false;

            AddMessage(effectMessages, effectMessage);
        }

        result.message = effectMessages.Count > 0
            ? string.Join("\n", effectMessages)
            : BuildFallbackMessage(request.optionData);
        result.success = true;
        return true;
    }

    private static bool TryExecuteEffect(
        MapEventBuiltInEffectExecutionRequest request,
        int effectIndex,
        MapEventBuiltInEffectData effectData,
        MapEventBuiltInEffectExecutionResult result,
        out string message)
    {
        message = string.Empty;
        if (effectData == null)
            return true;

        switch (effectData.effectType)
        {
            case MapEventOptionBuiltInEffectType.None:
                return true;
            case MapEventOptionBuiltInEffectType.GainMoney:
                return ExecuteGainMoney(request.runState, effectData, out message);
            case MapEventOptionBuiltInEffectType.GainRandomMoney:
                return ExecuteGainRandomMoney(request.runState, effectData, out message);
            case MapEventOptionBuiltInEffectType.GainExperience:
                return ExecuteGainExperience(request.runState, request.levelTable, effectData, out message);
            case MapEventOptionBuiltInEffectType.LoseMoney:
                return ExecuteLoseMoney(request.runState, effectData, out message);
            case MapEventOptionBuiltInEffectType.HealAllPartyMembers:
                return ExecuteHealAllPartyMembers(request.runState, out message);
            case MapEventOptionBuiltInEffectType.AdjustAllPartyMembersHpByMaxHpPercent:
                return ExecuteAdjustAllPartyMembersHpByMaxHpPercent(request.runState, effectData, out message);
            case MapEventOptionBuiltInEffectType.GainActionPoints:
                return ExecuteGainActionPoints(request.actionPointSystem, effectData, out message);
            case MapEventOptionBuiltInEffectType.LoseActionPoints:
                return ExecuteLoseActionPoints(request.actionPointSystem, effectData, out message);
            case MapEventOptionBuiltInEffectType.RestoreAllActionPoints:
                return ExecuteRestoreAllActionPoints(request.actionPointSystem, out message);
            case MapEventOptionBuiltInEffectType.GainLoot:
            case MapEventOptionBuiltInEffectType.GainRandomLootFromList:
                return MapEventRewardService.TryExecuteLootReward(request.runState, effectData, out message);
            case MapEventOptionBuiltInEffectType.GainRelics:
                return MapEventRewardService.TryExecuteRelicReward(request.runState, effectData, out message);
            case MapEventOptionBuiltInEffectType.GainBlueprints:
                return MapEventRewardService.TryExecuteBlueprintReward(request.runState, effectData, out message);
            case MapEventOptionBuiltInEffectType.LoseOwnedRandomLoot:
                return MapEventLootCostService.TryExecuteRandomLootDiscard(
                    request.runState,
                    request.optionData,
                    effectIndex,
                    effectData,
                    request.randomLootDiscardPreviewCache,
                    out message);
            case MapEventOptionBuiltInEffectType.LoseSpecificLoot:
                return MapEventLootCostService.TryExecuteSpecificLootDiscard(request.runState, effectData, out message);
            case MapEventOptionBuiltInEffectType.StartEncounter:
                return ExecuteStartEncounter(request, effectData, result, out message);
            case MapEventOptionBuiltInEffectType.StartSpecialDungeon:
                return ExecuteStartSpecialDungeon(effectData, result, out message);
            default:
                message = "未知事件效果。";
                return false;
        }
    }

    private static bool ExecuteGainMoney(RunState runState, MapEventBuiltInEffectData effectData, out string message)
    {
        int amount = Mathf.Max(0, effectData.effectAmount);
        if (amount > 0)
            runState.AddMoney(amount);

        message = $"获得 {amount} 金钱。";
        return true;
    }

    private static bool ExecuteGainRandomMoney(RunState runState, MapEventBuiltInEffectData effectData, out string message)
    {
        int minAmount = Mathf.Max(0, effectData.randomMoneyMin);
        int maxAmount = Mathf.Max(minAmount, effectData.randomMoneyMax);
        int gainedAmount = UnityEngine.Random.Range(minAmount, maxAmount + 1);
        if (gainedAmount > 0)
            runState.AddMoney(gainedAmount);

        message = $"获得 {gainedAmount} 金钱。";
        return true;
    }

    private static bool ExecuteGainExperience(
        RunState runState,
        LevelTable_SO levelTable,
        MapEventBuiltInEffectData effectData,
        out string message)
    {
        int gainedExperience = Mathf.Max(0, effectData.effectAmount);
        ApplyExperienceToAllPartyMembers(runState, levelTable, gainedExperience);
        message = $"所有角色获得 {gainedExperience} 经验。";
        return true;
    }

    private static bool ExecuteLoseMoney(RunState runState, MapEventBuiltInEffectData effectData, out string message)
    {
        int cost = Mathf.Max(0, effectData.effectAmount);
        if (!runState.TrySpendMoney(cost))
        {
            message = $"金钱不足，需要 {cost} 金钱。";
            return false;
        }

        message = $"失去 {cost} 金钱。";
        return true;
    }

    private static bool ExecuteHealAllPartyMembers(RunState runState, out string message)
    {
        if (runState.partyMembers != null)
        {
            for (int i = 0; i < runState.partyMembers.Count; i++)
            {
                RunPartyMemberState member = runState.partyMembers[i];
                if (member != null)
                    member.currentHP = Mathf.Max(0, member.maxHP);
            }
        }

        message = "所有角色已恢复至满血。";
        return true;
    }

    private static bool ExecuteAdjustAllPartyMembersHpByMaxHpPercent(
        RunState runState,
        MapEventBuiltInEffectData effectData,
        out string message)
    {
        int percent = effectData.effectAmount;
        if (runState.partyMembers != null)
        {
            for (int i = 0; i < runState.partyMembers.Count; i++)
            {
                RunPartyMemberState member = runState.partyMembers[i];
                if (member == null || member.maxHP <= 0)
                    continue;

                int hpDelta = Mathf.FloorToInt(member.maxHP * (Mathf.Abs(percent) / 100f));
                if (hpDelta <= 0)
                    continue;

                if (percent >= 0)
                    member.currentHP = Mathf.Clamp(member.currentHP + hpDelta, 0, member.maxHP);
                else
                    member.currentHP = Mathf.Clamp(member.currentHP - hpDelta, 0, member.maxHP);
            }
        }

        message = percent >= 0
            ? $"所有角色恢复了最大生命值的 {percent}% 。"
            : $"所有角色失去了最大生命值的 {Mathf.Abs(percent)}% 。";
        return true;
    }

    private static bool ExecuteGainActionPoints(
        ActionPointSystem actionPointSystem,
        MapEventBuiltInEffectData effectData,
        out string message)
    {
        int gainAmount = Mathf.Max(0, effectData.effectAmount);
        if (actionPointSystem == null)
        {
            message = "当前未找到行动点系统。";
            return false;
        }

        int pointsBeforeRestore = actionPointSystem.CurrentPoints;
        int restoredPoints = Mathf.Clamp(pointsBeforeRestore + gainAmount, 0, actionPointSystem.CurrentMaxPoints);
        int actualRestoredAmount = Mathf.Max(0, restoredPoints - pointsBeforeRestore);
        actionPointSystem.RestoreState(actionPointSystem.BasePoints, actionPointSystem.BonusPoints, restoredPoints);
        message = $"\u6062\u590d {actualRestoredAmount} \u70b9\u884c\u52a8\u70b9\u3002";
        return true;
    }

    private static bool ExecuteLoseActionPoints(
        ActionPointSystem actionPointSystem,
        MapEventBuiltInEffectData effectData,
        out string message)
    {
        int loseAmount = Mathf.Max(0, effectData.effectAmount);
        if (actionPointSystem == null)
        {
            message = "当前未找到行动点系统。";
            return false;
        }

        MapMovementService.SpendActionPointsWithOverflow(actionPointSystem, loseAmount);
        message = $"失去 {loseAmount} 点行动点。";
        return true;
    }

    private static bool ExecuteRestoreAllActionPoints(ActionPointSystem actionPointSystem, out string message)
    {
        if (actionPointSystem != null)
            actionPointSystem.RestoreState(actionPointSystem.BasePoints, actionPointSystem.BonusPoints, actionPointSystem.CurrentMaxPoints);

        message = "行动点已恢复至当前上限。";
        return true;
    }

    private static bool ExecuteStartEncounter(
        MapEventBuiltInEffectExecutionRequest request,
        MapEventBuiltInEffectData effectData,
        MapEventBuiltInEffectExecutionResult result,
        out string message)
    {
        bool prepared = MapEventEncounterService.TryPrepareEncounter(
            request.runState,
            request.currentEventTile,
            effectData,
            request.sourceSceneName,
            request.battleSceneName,
            request.encounterSeedSalt,
            out BattleEncounterData encounterData,
            out message);

        if (prepared)
            result.pendingEncounterData = encounterData;

        return prepared;
    }

    private static bool ExecuteStartSpecialDungeon(
        MapEventBuiltInEffectData effectData,
        MapEventBuiltInEffectExecutionResult result,
        out string message)
    {
        if (effectData == null || effectData.specialDungeon == null)
        {
            message = "特殊副本未配置。";
            return false;
        }

        result.pendingSpecialDungeon = effectData.specialDungeon;
        message = $"进入特殊副本：{effectData.specialDungeon.DisplayName}";
        return true;
    }

    private static int FindFirstEncounterEffectIndex(List<MapEventBuiltInEffectData> allEffects)
    {
        if (allEffects == null)
            return -1;

        for (int i = 0; i < allEffects.Count; i++)
        {
            if (allEffects[i] != null && allEffects[i].effectType == MapEventOptionBuiltInEffectType.StartEncounter)
                return i;
        }

        return -1;
    }

    private static string BuildFallbackMessage(MapEventOptionData optionData)
    {
        return string.IsNullOrWhiteSpace(optionData.consequenceDescription)
            ? $"已选择：{optionData.DisplayLabel}"
            : optionData.consequenceDescription;
    }

    private static void AddMessage(List<string> messages, string message)
    {
        if (!string.IsNullOrWhiteSpace(message))
            messages.Add(message);
    }

    private static void ApplyExperienceToAllPartyMembers(RunState runState, LevelTable_SO levelTable, int amount)
    {
        if (runState == null || runState.partyMembers == null || amount <= 0)
            return;

        for (int i = 0; i < runState.partyMembers.Count; i++)
            ApplyExperienceToPartyMember(runState.partyMembers[i], levelTable, amount);

        runState.SyncLegacyPlayerFieldsFromParty();
    }

    private static void ApplyExperienceToPartyMember(RunPartyMemberState member, LevelTable_SO levelTable, int amount)
    {
        if (member == null || amount <= 0)
            return;

        int currentLevel = Mathf.Max(1, member.level);
        int currentExperience = Mathf.Max(0, member.experience);
        int currentMaxHP = Mathf.Max(1, member.maxHP);
        int currentHP = Mathf.Clamp(member.currentHP, 0, currentMaxHP);
        int remaining = Mathf.Max(0, amount);

        while (remaining > 0)
        {
            int required = ResolveExperienceToNextLevel(levelTable, currentLevel);
            if (required <= 0)
                break;

            currentExperience = Mathf.Clamp(currentExperience, 0, required);
            int needed = Mathf.Max(0, required - currentExperience);
            if (remaining < needed)
            {
                currentExperience += remaining;
                remaining = 0;
                break;
            }

            remaining -= needed;
            currentLevel += 1;
            currentExperience = 0;

            int upgradedMaxHP = ResolveMaxHPForLevel(levelTable, member.characterData, currentLevel, currentMaxHP);
            currentMaxHP = upgradedMaxHP;
            if (currentHP > 0)
            {
                int healAmount = Mathf.CeilToInt(upgradedMaxHP * 0.5f);
                currentHP = Mathf.Clamp(currentHP + healAmount, 0, upgradedMaxHP);
            }
        }

        member.level = currentLevel;
        member.experience = currentExperience;
        member.maxHP = currentMaxHP;
        member.currentHP = Mathf.Clamp(currentHP, 0, currentMaxHP);
    }

    private static int ResolveExperienceToNextLevel(LevelTable_SO levelTable, int level)
    {
        return levelTable != null ? Mathf.Max(0, levelTable.GetExperienceToNextLevel(level)) : 100;
    }

    private static int ResolveMaxHPForLevel(
        LevelTable_SO levelTable,
        CharacterData_SO characterData,
        int level,
        int fallbackMaxHP)
    {
        return levelTable != null
            ? Mathf.Max(1, levelTable.GetMaxHP(characterData, level, fallbackMaxHP))
            : Mathf.Max(1, fallbackMaxHP);
    }
}
