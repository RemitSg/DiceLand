using System;
using UnityEngine;

public static class BattlePowerModifierService
{
    public static int EvaluatePowerModifier(
        EnemySkill_SO skill,
        int diceIndex,
        EnemySkill_SO.DiceEffectTrigger trigger,
        int selfSpeed,
        int targetSpeed,
        Func<BattleStatus_SO, int> getSelfStatusStacksByAsset,
        Func<string, int> getSelfStatusStacksById,
        Func<BattleStatus_SO, int> getTargetStatusStacksByAsset,
        Func<string, int> getTargetStatusStacksById,
        EnemySkill_SO targetSkill = null)
    {
        if (skill == null)
            return 0;

        EnemySkill_SO.DiceEffectSettings settings = skill.GetDiceEffectSettings(diceIndex);
        if (settings.powerModifiers == null || settings.powerModifiers.Length == 0)
            return 0;

        int totalBonus = 0;
        for (int i = 0; i < settings.powerModifiers.Length; i++)
        {
            EnemySkill_SO.ConditionalPowerModifierEntry modifier = settings.powerModifiers[i];
            if (modifier.trigger != trigger)
                continue;

            if (!modifier.useCondition)
            {
                if (modifier.powerBonus != 0)
                    totalBonus += modifier.powerBonus;
                continue;
            }

            totalBonus += EvaluateConditionalPowerModifier(
                modifier,
                selfSpeed,
                targetSpeed,
                getSelfStatusStacksByAsset,
                getSelfStatusStacksById,
                getTargetStatusStacksByAsset,
                getTargetStatusStacksById,
                targetSkill);
        }

        return totalBonus;
    }

    private static int EvaluateConditionalPowerModifier(
        EnemySkill_SO.ConditionalPowerModifierEntry modifier,
        int selfSpeed,
        int targetSpeed,
        Func<BattleStatus_SO, int> getSelfStatusStacksByAsset,
        Func<string, int> getSelfStatusStacksById,
        Func<BattleStatus_SO, int> getTargetStatusStacksByAsset,
        Func<string, int> getTargetStatusStacksById,
        EnemySkill_SO targetSkill)
    {
        switch (modifier.condition)
        {
            case EnemySkill_SO.PowerModifierCondition.SelfHasStatus:
                return ResolveSelfStatusStacks(modifier, getSelfStatusStacksByAsset, getSelfStatusStacksById) > 0
                    ? modifier.powerBonus
                    : 0;
            case EnemySkill_SO.PowerModifierCondition.SelfStatusStacksGreaterThan:
                return ResolveSelfStatusStacks(modifier, getSelfStatusStacksByAsset, getSelfStatusStacksById) > Mathf.Max(0, modifier.requiredStatusStacks)
                    ? modifier.powerBonus
                    : 0;
            case EnemySkill_SO.PowerModifierCondition.SelfStatusStacksGreaterOrEqual:
                return ResolveSelfStatusStacks(modifier, getSelfStatusStacksByAsset, getSelfStatusStacksById) >= Mathf.Max(1, modifier.requiredStatusStacks)
                    ? modifier.powerBonus
                    : 0;
            case EnemySkill_SO.PowerModifierCondition.SelfStatusStacksLessThan:
                return ResolveSelfStatusStacks(modifier, getSelfStatusStacksByAsset, getSelfStatusStacksById) < Mathf.Max(1, modifier.requiredStatusStacks)
                    ? modifier.powerBonus
                    : 0;
            case EnemySkill_SO.PowerModifierCondition.SelfStatusStacksLessOrEqual:
                return ResolveSelfStatusStacks(modifier, getSelfStatusStacksByAsset, getSelfStatusStacksById) <= Mathf.Max(0, modifier.requiredStatusStacks)
                    ? modifier.powerBonus
                    : 0;
            case EnemySkill_SO.PowerModifierCondition.TargetSkillIsSpecific:
                return modifier.targetSkillData != null && targetSkill == modifier.targetSkillData
                    ? modifier.powerBonus
                    : 0;
            case EnemySkill_SO.PowerModifierCondition.TargetHasStatus:
                return ResolveTargetStatusStacks(modifier, getTargetStatusStacksByAsset, getTargetStatusStacksById) > 0
                    ? modifier.powerBonus
                    : 0;
            case EnemySkill_SO.PowerModifierCondition.TargetStatusStacksAtLeast:
                return ResolveTargetStatusStacks(modifier, getTargetStatusStacksByAsset, getTargetStatusStacksById) >= Mathf.Max(1, modifier.requiredStatusStacks)
                    ? modifier.powerBonus
                    : 0;
            case EnemySkill_SO.PowerModifierCondition.TargetStatusStacksPerThreshold:
                {
                    int stacks = ResolveTargetStatusStacks(modifier, getTargetStatusStacksByAsset, getTargetStatusStacksById);
                    int threshold = Mathf.Max(1, modifier.requiredStatusStacks);
                    int steps = stacks / threshold;
                    int bonus = Mathf.Max(0, steps) * modifier.powerBonusPerStep;
                    return modifier.maxPowerBonus > 0 ? Mathf.Min(bonus, modifier.maxPowerBonus) : bonus;
                }
            case EnemySkill_SO.PowerModifierCondition.TargetNegativeStatusTypesAtLeast:
                return BattleNegativeStatusService.HasAtLeastNegativeStatusTypes(
                    id => ResolveTargetStatusStacksByIdForNegativeCheck(id, getTargetStatusStacksByAsset, getTargetStatusStacksById),
                    modifier.requiredStatusStacks)
                    ? modifier.powerBonus
                    : 0;
            default:
                if (modifier.powerBonusPerStep != 0 && TryEvaluateSpeedDifferenceBonus(modifier, selfSpeed, targetSpeed, out int speedDifferenceBonus))
                    return speedDifferenceBonus;

                return BattleModifierConditionService.IsConditionMet(modifier.condition, selfSpeed, targetSpeed, modifier.requiredSpeedValue, modifier.requiredSpeedDifference)
                    ? modifier.powerBonus
                    : 0;
        }
    }

    private static bool TryEvaluateSpeedDifferenceBonus(
        EnemySkill_SO.ConditionalPowerModifierEntry modifier,
        int selfSpeed,
        int targetSpeed,
        out int bonus)
    {
        bonus = 0;

        int steps;
        switch (modifier.condition)
        {
            case EnemySkill_SO.PowerModifierCondition.SelfSpeedHigherThanTarget:
                steps = selfSpeed - targetSpeed;
                break;
            case EnemySkill_SO.PowerModifierCondition.SelfSpeedLowerThanTarget:
                steps = targetSpeed - selfSpeed;
                break;
            default:
                return false;
        }

        if (steps < Mathf.Max(1, modifier.requiredSpeedDifference))
            return true;

        bonus = Mathf.Max(0, steps) * modifier.powerBonusPerStep;
        if (modifier.maxPowerBonus > 0 && bonus > 0)
            bonus = Mathf.Min(bonus, modifier.maxPowerBonus);
        return true;
    }

    private static int ResolveSelfStatusStacks(
        EnemySkill_SO.ConditionalPowerModifierEntry modifier,
        Func<BattleStatus_SO, int> getSelfStatusStacksByAsset,
        Func<string, int> getSelfStatusStacksById)
    {
        if (modifier.targetStatusData != null && getSelfStatusStacksByAsset != null)
            return Mathf.Max(0, getSelfStatusStacksByAsset(modifier.targetStatusData));

        if (modifier.targetStatusType == EnemySkill_SO.DiceStatusType.Burn && getSelfStatusStacksById != null)
            return Mathf.Max(0, getSelfStatusStacksById("burn"));

        return 0;
    }

    private static int ResolveTargetStatusStacks(
        EnemySkill_SO.ConditionalPowerModifierEntry modifier,
        Func<BattleStatus_SO, int> getTargetStatusStacksByAsset,
        Func<string, int> getTargetStatusStacksById)
    {
        if (modifier.targetStatusData != null && getTargetStatusStacksByAsset != null)
            return Mathf.Max(0, getTargetStatusStacksByAsset(modifier.targetStatusData));

        if (modifier.targetStatusType == EnemySkill_SO.DiceStatusType.Burn && getTargetStatusStacksById != null)
            return Mathf.Max(0, getTargetStatusStacksById("burn"));

        return 0;
    }

    private static int ResolveTargetStatusStacksByIdForNegativeCheck(
        string statusId,
        Func<BattleStatus_SO, int> getTargetStatusStacksByAsset,
        Func<string, int> getTargetStatusStacksById)
    {
        if (getTargetStatusStacksById != null && !string.IsNullOrWhiteSpace(statusId))
            return Mathf.Max(0, getTargetStatusStacksById(statusId));

        return 0;
    }
}
