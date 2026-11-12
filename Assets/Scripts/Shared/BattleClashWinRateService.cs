using System;
using System.Collections.Generic;

public static class BattleClashWinRateService
{
    /// <summary>Estimates friendly clash wins in combat order using the current planning state.</summary>
    public static double?[] Estimate(IReadOnlyList<BattleCombatAction> actions)
    {
        var estimates = new double?[actions.Count];
        var plannedRelicProcs = new HashSet<string>(StringComparer.Ordinal);
        var usedAllyBonuses = new HashSet<BattleAllyController>();
        var usedEnemyBonuses = new HashSet<BattleEnemyController>();
        var plannedSkillUses = new Dictionary<BattleAllyController, BattleAllyController.ConsecutiveSkillUsage>();
        bool hasRelics = BattleEncounterRuntime.ActiveEncounter?.relicInventory?.Count > 0;

        for (int i = 0; i < actions.Count; i++)
        {
            BattleCombatAction action = actions[i];
            EnemySkill_SO allySkill = action.Ally?.GetSelectedSkillForButton(action.AllyButton);
            EnemySkill_SO enemySkill = action.Enemy?.GetSelectedSkillForButton(action.EnemyButton);
            BattleAllyController.ConsecutiveSkillUsage usage = default;
            if (action.Ally != null)
            {
                if (!plannedSkillUses.TryGetValue(action.Ally, out usage))
                    usage = action.Ally.SkillUsage;
                usage.Register(allySkill);
            }
            BattleDiceRangePreview ally = null;
            if (action.Type == BattleCombatActionType.Clash)
            {
                // Both sides see the same starting relic counters. Only the ally preview
                // advances the forecast's counters for the next scheduled action.
                var enemy = BattleDiceRangePreview.Build(enemySkill, null, action.Enemy, action.EnemyButton,
                    new HashSet<string>(plannedRelicProcs, StringComparer.Ordinal), !usedEnemyBonuses.Contains(action.Enemy));
                ally = BattleDiceRangePreview.Build(allySkill, action.Ally, null, action.AllyButton,
                    plannedRelicProcs, !usedAllyBonuses.Contains(action.Ally), usage.GetUses(allySkill));
                estimates[i] = EstimateMatchedDice(ally.Dice, enemy.Dice);
                if (ally.Dice.Length > 0)
                    usedAllyBonuses.Add(action.Ally);
                if (enemySkill != null && !enemySkill.IsDefenseSkill
                    && enemySkill.GetDiceSkillCategory(0) == EnemySkill_SO.SkillCategory.Attack)
                    usedEnemyBonuses.Add(action.Enemy);
            }
            else if (action.Type == BattleCombatActionType.AllySingleAttack && HasAttackDice(allySkill))
            {
                if (hasRelics)
                    ally = BattleDiceRangePreview.Build(allySkill, action.Ally, null, action.AllyButton,
                        plannedRelicProcs, !usedAllyBonuses.Contains(action.Ally), usage.GetUses(allySkill));
                usedAllyBonuses.Add(action.Ally);
            }
            else if (action.Type == BattleCombatActionType.EnemySingleAttack && HasAttackDice(enemySkill) && !enemySkill.IsDefenseSkill)
                usedEnemyBonuses.Add(action.Enemy);

            if (action.Ally != null)
            {
                if (ally?.ResetsConsecutiveSkill == true)
                    usage.ResetOnNextUse(allySkill);
                plannedSkillUses[action.Ally] = usage;
            }
        }

        return estimates;
    }

    internal static bool HasAttackDice(EnemySkill_SO skill)
    {
        return BattleSingleAttackPresentationStartService.FindFirstAttackDiceIndex(skill, skill?.diceCount ?? 0)
            != BattleSingleAttackPresentationStartService.NoAttackDice;
    }

    private static double? EstimateMatchedDice(BattleDiceRangeBreakdown[] ally, BattleDiceRangeBreakdown[] enemy)
    {
        int count = Math.Min(ally.Length, enemy.Length);
        if (count == 0)
            return null;

        // Each pair contributes +1 for a win, -1 for a loss, or 0 for a tie.
        // Extra dice resolve separately and do not decide the clash.
        var margins = new double[2 * count + 1];
        var nextMargins = new double[margins.Length];
        margins[count] = 1;
        for (int i = 0; i < count; i++)
        {
            double win = ChanceToRollHigher(ally[i], enemy[i]);
            double loss = ChanceToRollHigher(enemy[i], ally[i]);
            double tie = Math.Max(0, 1 - win - loss);
            Array.Clear(nextMargins, 0, nextMargins.Length);
            for (int margin = count - i; margin <= count + i; margin++)
            {
                double chance = margins[margin];
                nextMargins[margin + 1] += chance * win;
                nextMargins[margin - 1] += chance * loss;
                nextMargins[margin] += chance * tie;
            }
            (margins, nextMargins) = (nextMargins, margins);
        }

        double total = 0;
        for (int i = count + 1; i < margins.Length; i++)
            total += margins[i];
        return Math.Min(1, total);
    }

    private static double ChanceToRollHigher(BattleDiceRangeBreakdown source, BattleDiceRangeBreakdown target)
    {
        long sourceMin = (long)source.RollRange.minPower + source.PowerBonus;
        long sourceMax = (long)source.RollRange.maxPower + source.PowerBonus;
        long targetMin = (long)target.RollRange.minPower + target.PowerBonus;
        long targetMax = (long)target.RollRange.maxPower + target.PowerBonus;
        long sourceFaces = sourceMax - sourceMin + 1;
        long targetFaces = targetMax - targetMin + 1;

        // Final power is floored at 1. To win, an unclamped source face must be
        // at least 2 and beat the target's unclamped face.
        long first = Math.Max(2, sourceMin);
        long rampStart = Math.Max(first, targetMin + 1);
        long rampEnd = Math.Min(sourceMax, targetMax);
        double rampWins = rampEnd >= rampStart
            ? (rampEnd - rampStart + 1) * (rampStart + rampEnd - 2.0 * targetMin) / 2
            : 0;
        long fullStart = Math.Max(first, targetMax + 1);
        double fullWins = Math.Max(0, sourceMax - fullStart + 1) * (double)targetFaces;
        return (rampWins + fullWins) / (sourceFaces * (double)targetFaces);
    }
}
