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
    public int moneyBeforeSelection;
    public int selectionMoneyCost;
}

public sealed class MapEventBuiltInEffectExecutionResult
{
    public bool success;
    public string message;
    public string resultContent;
    public MapEvent_SO nextEventOverride;
    public bool hasResolveEventOverride;
    public bool resolveEventOverride;
    public BattleEncounterData pendingEncounterData;
    public SpecialDungeon_SO pendingSpecialDungeon;
}

public static class MapEventBuiltInEffectExecutorService
{
    // Check every possible branch without drawing the outcome or changing the live state.
    // Check both income bounds, including losses above 100%, without spending the live wallet.
    public static bool CanAffordMoneyEffects(RunState state, MapEventOptionData option, int selectionCost, out string message)
    {
        message = null;
        if (state == null || option == null) return false;
        var branches = new List<List<MapEventBuiltInEffectData>>();
        if (option.possibleResults != null)
            foreach (MapEventPossibleResultData branch in option.possibleResults)
                if (branch != null && branch.weight > 0f) branches.Add(MapEventEffectSequenceBuilder.BuildAllEffects(branch));
        if (branches.Count == 0) branches.Add(MapEventEffectSequenceBuilder.BuildAllEffects(option));
        UnityEngine.Random.State outside = UnityEngine.Random.state;
        try
        {
            foreach (var effects in branches)
            foreach (bool maximum in new[] { false, true })
            {
                var copy = new RunState { money = state.money };
                if (selectionCost > 0 && !copy.TrySpendMoney(selectionCost))
                { message = $"金钱不足，需要 {selectionCost} 金钱。"; return false; }
                var request = new MapEventBuiltInEffectExecutionRequest { runState = copy, moneyBeforeSelection = Mathf.Max(0, state.money) };
                foreach (MapEventBuiltInEffectData effect in effects)
                {
                    bool success = true;
                    switch (effect.effectType)
                    {
                        case MapEventOptionBuiltInEffectType.GainMoney: ExecuteGainMoney(copy, effect, out _); break;
                        case MapEventOptionBuiltInEffectType.GainRandomMoney:
                            var minimumMoney = MapEventEffectSequenceBuilder.CloneEffect(effect);
                            if (maximum) minimumMoney.randomMoneyMin = Mathf.Max(minimumMoney.randomMoneyMin, minimumMoney.randomMoneyMax);
                            minimumMoney.randomMoneyMax = minimumMoney.randomMoneyMin;
                            ExecuteGainRandomMoney(copy, minimumMoney, out _); break;
                        case MapEventOptionBuiltInEffectType.GainMoneyPercentOfPreSelectionMoney:
                            ExecuteGainMoneyPercentOfPreSelectionMoney(request, effect, out _); break;
                        case MapEventOptionBuiltInEffectType.RerollAllMoneyByPercentRange:
                            var minimumPercent = MapEventEffectSequenceBuilder.CloneEffect(effect);
                            if (maximum) minimumPercent.randomMoneyPercentMin = Mathf.Max(minimumPercent.randomMoneyPercentMin, minimumPercent.randomMoneyPercentMax);
                            minimumPercent.randomMoneyPercentMax = minimumPercent.randomMoneyPercentMin;
                            ExecuteRerollAllMoneyByPercentRange(copy, minimumPercent, out _); break;
                        case MapEventOptionBuiltInEffectType.LoseMoney: success = ExecuteLoseMoney(copy, effect, out message); break;
                        case MapEventOptionBuiltInEffectType.LoseMoneyPercent: success = ExecuteLoseMoneyPercent(copy, effect, out message); break;
                    }
                    if (!success) return false;
                }
            }
            return true;
        }
        finally { UnityEngine.Random.state = outside; }
    }

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
        MapEventPossibleResultData selectedResult = SelectPossibleResult(request.optionData);
        List<MapEventBuiltInEffectData> allEffects = selectedResult != null
            ? MapEventEffectSequenceBuilder.BuildAllEffects(selectedResult)
            : MapEventEffectSequenceBuilder.BuildAllEffects(request.optionData);
        if (selectedResult != null)
        {
            result.resultContent = selectedResult.resultContent;
            result.nextEventOverride = selectedResult.nextEvent;
            result.hasResolveEventOverride = true;
            result.resolveEventOverride = selectedResult.resolveEvent;
        }

        int encounterEffectIndex = FindFirstEncounterEffectIndex(allEffects);
        if (encounterEffectIndex >= 0)
        {
            if (!TryExecuteEffect(request, encounterEffectIndex, allEffects[encounterEffectIndex], result, out string encounterMessage))
            {
                result.message = encounterMessage;
                return false;
            }

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
            {
                result.message = effectMessage;
                return false;
            }

            AddMessage(effectMessages, effectMessage);
        }

        result.message = effectMessages.Count > 0
            ? string.Join("\n", effectMessages)
            : BuildFallbackMessage(request.optionData, selectedResult);
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
            case MapEventOptionBuiltInEffectType.GainMoneyPercentOfPreSelectionMoney:
                return ExecuteGainMoneyPercentOfPreSelectionMoney(request, effectData, out message);
            case MapEventOptionBuiltInEffectType.RerollAllMoneyByPercentRange:
                return ExecuteRerollAllMoneyByPercentRange(request.runState, effectData, out message);
            case MapEventOptionBuiltInEffectType.GainExperience:
                return ExecuteGainExperience(request.runState, request.levelTable, effectData, out message);
            case MapEventOptionBuiltInEffectType.LoseMoney:
                return ExecuteLoseMoney(request.runState, effectData, out message);
            case MapEventOptionBuiltInEffectType.LoseMoneyPercent:
                return ExecuteLoseMoneyPercent(request.runState, effectData, out message);
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
            case MapEventOptionBuiltInEffectType.GainWeapons:
                return MapEventRewardService.TryExecuteWeaponReward(request.runState, effectData, out message);
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
            case MapEventOptionBuiltInEffectType.ReforgeOwnedRandomRelicsSameRarity:
                return ExecuteReforgeOwnedRandomRelicsSameRarity(request.runState, effectData, out message);
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

    private static bool ExecuteGainMoneyPercentOfPreSelectionMoney(
        MapEventBuiltInEffectExecutionRequest request,
        MapEventBuiltInEffectData effectData,
        out string message)
    {
        int percent = Mathf.Max(0, effectData.moneyPercent);
        int baseMoney = Mathf.Max(0, request != null ? request.moneyBeforeSelection : 0);
        int gainedAmount = Mathf.FloorToInt(baseMoney * (percent / 100f));
        if (gainedAmount > 0)
            request.runState.AddMoney(gainedAmount);

        message = $"获得 {gainedAmount} 金钱。";
        return true;
    }

    private static bool ExecuteRerollAllMoneyByPercentRange(
        RunState runState,
        MapEventBuiltInEffectData effectData,
        out string message)
    {
        int moneyBefore = Mathf.Max(0, runState != null ? runState.money : 0);
        if (moneyBefore > 0)
            runState.TrySpendMoney(moneyBefore);

        int minPercent = Mathf.Max(0, effectData.randomMoneyPercentMin);
        int maxPercent = Mathf.Max(minPercent, effectData.randomMoneyPercentMax);
        int rolledPercent = UnityEngine.Random.Range(minPercent, maxPercent + 1);
        int gainedAmount = Mathf.FloorToInt(moneyBefore * (rolledPercent / 100f));
        if (gainedAmount > 0)
            runState.AddMoney(gainedAmount);

        message = $"赌局重算了所有金钱：失去 {moneyBefore} 金钱，按 {rolledPercent}% 找回 {gainedAmount} 金钱。";
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

    private static bool ExecuteLoseMoneyPercent(RunState runState, MapEventBuiltInEffectData effectData, out string message)
    {
        int percent = Mathf.Max(0, effectData.moneyPercent);
        int currentMoney = Mathf.Max(0, runState != null ? runState.money : 0);
        int cost = Mathf.FloorToInt(currentMoney * (percent / 100f));
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

    private static bool ExecuteReforgeOwnedRandomRelicsSameRarity(
        RunState runState,
        MapEventBuiltInEffectData effectData,
        out string message)
    {
        if (runState == null || runState.relicInventory == null || runState.relicInventory.Count == 0)
        {
            message = "当前没有可重铸的遗物。";
            return true;
        }

        if (effectData == null || effectData.relicTable == null || effectData.relicTable.Relics == null)
        {
            message = "该事件效果未配置遗物表。";
            return false;
        }

        int reforgeCount = Mathf.Max(1, effectData.effectAmount);
        List<Relic_SO> ownedRelics = BuildOwnedRelicPool(runState);
        List<string> descriptions = new List<string>();
        for (int i = 0; i < reforgeCount && ownedRelics.Count > 0; i++)
        {
            int selectedOwnedIndex = UnityEngine.Random.Range(0, ownedRelics.Count);
            Relic_SO oldRelic = ownedRelics[selectedOwnedIndex];
            ownedRelics.RemoveAt(selectedOwnedIndex);
            if (oldRelic == null || !runState.TryConsumeRelic(oldRelic, 1))
                continue;

            Relic_SO newRelic = DrawRelicOfRarity(effectData.relicTable, oldRelic.rarity, oldRelic);
            if (newRelic == null)
            {
                runState.AddRelic(oldRelic, 1);
                continue;
            }

            runState.AddRelic(newRelic, 1);
            descriptions.Add($"{oldRelic.DisplayName} -> {newRelic.DisplayName}");
        }

        message = descriptions.Count > 0
            ? $"遗物被同品质重铸：{string.Join("，", descriptions)}。"
            : "没有遗物发生变化。";
        return true;
    }

    private static List<Relic_SO> BuildOwnedRelicPool(RunState runState)
    {
        List<Relic_SO> ownedRelics = new List<Relic_SO>();
        if (runState == null || runState.relicInventory == null)
            return ownedRelics;

        for (int i = 0; i < runState.relicInventory.Count; i++)
        {
            RunRelicState stack = runState.relicInventory[i];
            if (stack == null || stack.relicData == null)
                continue;

            int amount = Mathf.Max(0, stack.amount);
            for (int j = 0; j < amount; j++)
                ownedRelics.Add(stack.relicData);
        }

        return ownedRelics;
    }

    private static Relic_SO DrawRelicOfRarity(RelicTable_SO relicTable, LootRarity rarity, Relic_SO excludedRelic)
    {
        if (relicTable == null || relicTable.Relics == null)
            return null;

        List<Relic_SO> candidates = new List<Relic_SO>();
        IReadOnlyList<Relic_SO> relics = relicTable.Relics;
        for (int i = 0; i < relics.Count; i++)
        {
            Relic_SO relic = relics[i];
            if (relic != null && relic != excludedRelic && relic.rarity == rarity)
                candidates.Add(relic);
        }

        if (candidates.Count == 0)
            return null;

        return candidates[UnityEngine.Random.Range(0, candidates.Count)];
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

    private static MapEventPossibleResultData SelectPossibleResult(MapEventOptionData optionData)
    {
        if (optionData == null || optionData.possibleResults == null || optionData.possibleResults.Count == 0)
            return null;

        float totalWeight = 0f;
        for (int i = 0; i < optionData.possibleResults.Count; i++)
        {
            MapEventPossibleResultData possibleResult = optionData.possibleResults[i];
            if (possibleResult != null)
                totalWeight += Mathf.Max(0f, possibleResult.weight);
        }

        if (totalWeight <= 0f)
            return null;

        float roll = UnityEngine.Random.Range(0f, totalWeight);
        float cumulative = 0f;
        for (int i = 0; i < optionData.possibleResults.Count; i++)
        {
            MapEventPossibleResultData possibleResult = optionData.possibleResults[i];
            if (possibleResult == null)
                continue;

            cumulative += Mathf.Max(0f, possibleResult.weight);
            if (roll <= cumulative)
                return possibleResult;
        }

        return optionData.possibleResults[optionData.possibleResults.Count - 1];
    }

    private static string BuildFallbackMessage(MapEventOptionData optionData, MapEventPossibleResultData selectedResult)
    {
        if (selectedResult != null && !string.IsNullOrWhiteSpace(selectedResult.consequenceDescription))
            return selectedResult.consequenceDescription;

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
            ApplyExperienceToPartyMember(runState, runState.partyMembers[i], levelTable, amount);

        runState.SyncLegacyPlayerFieldsFromParty();
    }

    private static void ApplyExperienceToPartyMember(RunState runState, RunPartyMemberState member, LevelTable_SO levelTable, int amount)
    {
        if (member == null || amount <= 0)
            return;

        int currentLevel = Mathf.Max(1, member.level);
        int currentExperience = Mathf.Max(0, member.experience);
        RunPartyMemberMaxHpService.InitializeBaseTracking(member);
        int currentBaseMaxHP = Mathf.Max(1, member.preCalamityMaxHP - member.maxHPFlatBonus);
        int currentHP = Mathf.Clamp(member.currentHP, 0, Mathf.Max(1, member.maxHP));
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

            currentBaseMaxHP = ResolveMaxHPForLevel(levelTable, member.characterData, currentLevel, currentBaseMaxHP);
            if (currentHP > 0)
            {
                int upgradedPreCalamityMaxHP = Mathf.Max(1, currentBaseMaxHP + member.maxHPFlatBonus);
                int upgradedEffectiveMaxHP = CalamitySystem.ApplyAllyMaxHpModifiers(
                    upgradedPreCalamityMaxHP,
                    runState != null ? runState.activeCalamities : null);
                int healAmount = Mathf.CeilToInt(Mathf.Max(1, upgradedEffectiveMaxHP) * 0.5f);
                currentHP = Mathf.Clamp(currentHP + healAmount, 0, Mathf.Max(1, upgradedEffectiveMaxHP));
            }
        }

        member.level = currentLevel;
        member.experience = currentExperience;
        member.currentHP = currentHP;
        RunPartyMemberMaxHpService.SetBaseMaxHp(member, currentBaseMaxHP, runState != null ? runState.activeCalamities : null);
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
