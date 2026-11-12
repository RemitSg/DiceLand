using System;
using System.Collections.Generic;
using UnityEngine;
using Behavior = BattleStatus_SO.StatusBehaviorType;

internal static class BattleStatusDicePowerService
{
    public static EnemySkill_SO.DicePowerRange ApplyRangeModifiers(
        EnemySkill_SO.DicePowerRange range,
        bool isDefense,
        IReadOnlyList<BattleStatusRuntimeEntry> statuses,
        BattleDiceRangeBreakdown breakdown,
        Func<BattleStatusRuntimeEntry, bool> isSuppressed = null)
    {
        Behavior flatBonus = isDefense ? Behavior.DefensePowerBonusFlat : Behavior.PowerBonusFlat;
        Behavior flatPenalty = isDefense ? Behavior.DefensePowerPenaltyFlat : Behavior.PowerPenaltyFlat;
        int minimumDelta = 0;
        int maximumDelta = 0;
        for (int i = 0; i < statuses.Count; i++)
        {
            BattleStatusRuntimeEntry entry = statuses[i];
            if (entry == null || entry.stacks <= 0 || entry.statusData == null || isSuppressed?.Invoke(entry) == true)
                continue;

            Behavior behavior = entry.statusData.BehaviorType;
            int amount = entry.stacks * Mathf.Max(0, entry.statusData.PowerPerStack);
            int minimum = 0;
            int maximum = 0;
            if (behavior == flatBonus)
                minimum = maximum = amount;
            else if (behavior == flatPenalty)
                minimum = maximum = -amount;
            else if (behavior == Behavior.MinPowerPenalty)
                minimum = -amount;
            else if (behavior == Behavior.MaxPowerPenalty)
                maximum = -amount;

            minimumDelta += minimum;
            maximumDelta += maximum;
            breakdown?.AddStatus(entry, minimum, maximum);
        }

        return BattleDiceRollService.ApplyRangeBonus(range, minimumDelta, maximumDelta);
    }

    public static BattleDiceRollService.ForcedRollMode ResolveForcedRollMode(
        IReadOnlyList<BattleStatusRuntimeEntry> statuses,
        out BattleStatusRuntimeEntry source,
        Func<BattleStatusRuntimeEntry, bool> isSuppressed = null)
    {
        source = null;
        for (int i = 0; i < statuses.Count; i++)
        {
            BattleStatusRuntimeEntry entry = statuses[i];
            if (entry == null || entry.stacks <= 0 || entry.statusData == null || isSuppressed?.Invoke(entry) == true)
                continue;

            if (entry.statusData.BehaviorType == Behavior.ForceRollToMin)
            {
                source = entry;
                return BattleDiceRollService.ForcedRollMode.Min;
            }
            if (source == null && entry.statusData.BehaviorType == Behavior.ForceRollToMax)
                source = entry;
        }

        return source != null ? BattleDiceRollService.ForcedRollMode.Max : BattleDiceRollService.ForcedRollMode.None;
    }
}
