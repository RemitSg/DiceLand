using System;
using System.Collections.Generic;
using UnityEngine;

// Reward mutations shared by the settlement UI and non-graphical drivers.
public static class BattleRelicChoiceSelectionService
{
    public static bool TryApply(BattleEncounterData encounter, BattleSettlementRelicChoiceData choice, out string error)
    {
        error = null;
        if (choice == null || !choice.HasChoices) return true;
        if (choice.rewardApplied) return true;
        if (choice.selectedChoice == null || choice.selectedChoice.relicData == null || !choice.choices.Contains(choice.selectedChoice))
        {
            error = "请先选择一个遗物。";
            return false;
        }
        if (encounter == null) { error = "战斗结算上下文缺失。"; return false; }
        if (encounter.resolvedRelics == null) encounter.resolvedRelics = new List<RunRelicState>();
        RunRelicState existing = encounter.resolvedRelics.Find(r => r != null && r.relicData == choice.selectedChoice.relicData);
        if (existing != null) existing.amount++;
        else encounter.resolvedRelics.Add(new RunRelicState { relicData = choice.selectedChoice.relicData, amount = 1 });
        choice.rewardApplied = true;
        return true;
    }

    public static HashSet<Relic_SO> RefreshExclusions(BattleSettlementRelicChoiceData choice)
    {
        var excluded = new HashSet<Relic_SO>();
        if (choice?.choices != null)
            foreach (BattleSettlementLootEntry entry in choice.choices)
                if (entry?.relicData != null) excluded.Add(entry.relicData);
        return excluded;
    }

    public static bool TryRefresh(BattleSettlementRelicChoiceData choice, System.Random random)
    {
        if (!CanRefresh(choice)) return false;
        List<Relic_SO> relics = choice.sourceTable.DrawRandomRelicChoices(random, Mathf.Max(1, choice.choiceCount), RefreshExclusions(choice));
        BattleSettlementRelicChoiceData refreshed = BattleSettlementRelicChoiceBuilder.Build(relics);
        if (refreshed == null || !refreshed.HasChoices) return false;
        choice.refreshesRemaining--;
        choice.choices = refreshed.choices;
        choice.selectedChoice = null;
        return true;
    }

    public static bool CanRefresh(BattleSettlementRelicChoiceData choice)
    {
        if (choice == null || choice.rewardApplied || choice.refreshesRemaining <= 0 || choice.sourceTable?.Relics == null) return false;
        HashSet<Relic_SO> excluded = RefreshExclusions(choice);
        foreach (Relic_SO relic in choice.sourceTable.Relics)
            if (relic != null && !excluded.Contains(relic)) return true;
        return false;
    }
}
