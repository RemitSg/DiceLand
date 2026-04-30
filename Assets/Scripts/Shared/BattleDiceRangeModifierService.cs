using System;
using UnityEngine;

public static class BattleDiceRangeModifierService
{
    public static EnemySkill_SO.DicePowerRange ApplyRangeModifiers(
        EnemySkill_SO skill,
        int diceIndex,
        EnemySkill_SO.DicePowerRange range,
        int selfSpeed,
        int targetSpeed,
        Func<BattleStatus_SO, int> getSelfStatusStacksByAsset,
        Func<string, int> getSelfStatusStacksById,
        Func<BattleStatus_SO, int> getTargetStatusStacksByAsset,
        Func<string, int> getTargetStatusStacksById,
        EnemySkill_SO targetSkill = null)
    {
        if (skill == null)
            return range;

        EnemySkill_SO.DiceEffectSettings settings = skill.GetDiceEffectSettings(diceIndex);
        if (settings.rangeModifiers == null || settings.rangeModifiers.Length == 0)
            return range;

        for (int i = 0; i < settings.rangeModifiers.Length; i++)
        {
            range = EvaluateConditionalRangeModifier(
                range,
                settings.rangeModifiers[i],
                selfSpeed,
                targetSpeed,
                getSelfStatusStacksByAsset,
                getSelfStatusStacksById,
                getTargetStatusStacksByAsset,
                getTargetStatusStacksById,
                targetSkill);
        }

        return range;
    }

    private static EnemySkill_SO.DicePowerRange EvaluateConditionalRangeModifier(
        EnemySkill_SO.DicePowerRange range,
        EnemySkill_SO.ConditionalDiceRangeModifierEntry modifier,
        int selfSpeed,
        int targetSpeed,
        Func<BattleStatus_SO, int> getSelfStatusStacksByAsset,
        Func<string, int> getSelfStatusStacksById,
        Func<BattleStatus_SO, int> getTargetStatusStacksByAsset,
        Func<string, int> getTargetStatusStacksById,
        EnemySkill_SO targetSkill)
    {
        bool conditionMet = !modifier.useCondition;
        if (!conditionMet)
        {
            switch (modifier.condition)
            {
                case EnemySkill_SO.PowerModifierCondition.SelfHasStatus:
                    conditionMet = ResolveSelfStatusStacks(modifier, getSelfStatusStacksByAsset, getSelfStatusStacksById) > 0;
                    break;
                case EnemySkill_SO.PowerModifierCondition.SelfStatusStacksGreaterThan:
                    conditionMet = ResolveSelfStatusStacks(modifier, getSelfStatusStacksByAsset, getSelfStatusStacksById) > Mathf.Max(0, modifier.requiredStatusStacks);
                    break;
                case EnemySkill_SO.PowerModifierCondition.SelfStatusStacksGreaterOrEqual:
                    conditionMet = ResolveSelfStatusStacks(modifier, getSelfStatusStacksByAsset, getSelfStatusStacksById) >= Mathf.Max(1, modifier.requiredStatusStacks);
                    break;
                case EnemySkill_SO.PowerModifierCondition.SelfStatusStacksLessThan:
                    conditionMet = ResolveSelfStatusStacks(modifier, getSelfStatusStacksByAsset, getSelfStatusStacksById) < Mathf.Max(1, modifier.requiredStatusStacks);
                    break;
                case EnemySkill_SO.PowerModifierCondition.SelfStatusStacksLessOrEqual:
                    conditionMet = ResolveSelfStatusStacks(modifier, getSelfStatusStacksByAsset, getSelfStatusStacksById) <= Mathf.Max(0, modifier.requiredStatusStacks);
                    break;
                case EnemySkill_SO.PowerModifierCondition.TargetSkillIsSpecific:
                    conditionMet = modifier.targetSkillData != null && targetSkill == modifier.targetSkillData;
                    break;
                case EnemySkill_SO.PowerModifierCondition.TargetHasStatus:
                    conditionMet = ResolveTargetStatusStacks(modifier, getTargetStatusStacksByAsset, getTargetStatusStacksById) > 0;
                    break;
                case EnemySkill_SO.PowerModifierCondition.TargetStatusStacksAtLeast:
                    conditionMet = ResolveTargetStatusStacks(modifier, getTargetStatusStacksByAsset, getTargetStatusStacksById) >= Mathf.Max(1, modifier.requiredStatusStacks);
                    break;
                case EnemySkill_SO.PowerModifierCondition.TargetStatusStacksPerThreshold:
                    return ApplyPerThresholdRangeModifier(
                        range,
                        modifier,
                        ResolveTargetStatusStacks(modifier, getTargetStatusStacksByAsset, getTargetStatusStacksById));
                default:
                    conditionMet = BattleModifierConditionService.IsConditionMet(modifier.condition, selfSpeed, targetSpeed, modifier.requiredSpeedValue, modifier.requiredSpeedDifference);
                    break;
            }
        }

        if (!conditionMet)
            return range;

        range.minPower = Mathf.Max(0, range.minPower + modifier.minPowerBonus);
        range.maxPower = Mathf.Max(range.minPower, range.maxPower + modifier.maxPowerBonus);
        return range;
    }

    private static EnemySkill_SO.DicePowerRange ApplyPerThresholdRangeModifier(
        EnemySkill_SO.DicePowerRange range,
        EnemySkill_SO.ConditionalDiceRangeModifierEntry modifier,
        int statusStacks)
    {
        int threshold = Mathf.Max(1, modifier.requiredStatusStacks);
        int steps = Mathf.Max(0, statusStacks) / threshold;
        if (steps <= 0)
            return range;

        int minBonus = steps * modifier.minPowerBonus;
        int maxBonus = steps * modifier.maxPowerBonus;
        if (modifier.minPowerBonusCap > 0 && minBonus > 0)
            minBonus = Mathf.Min(minBonus, modifier.minPowerBonusCap);
        if (modifier.maxPowerBonusCap > 0 && maxBonus > 0)
            maxBonus = Mathf.Min(maxBonus, modifier.maxPowerBonusCap);

        range.minPower = Mathf.Max(0, range.minPower + minBonus);
        range.maxPower = Mathf.Max(range.minPower, range.maxPower + maxBonus);
        return range;
    }

    private static int ResolveSelfStatusStacks(
        EnemySkill_SO.ConditionalDiceRangeModifierEntry modifier,
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
        EnemySkill_SO.ConditionalDiceRangeModifierEntry modifier,
        Func<BattleStatus_SO, int> getTargetStatusStacksByAsset,
        Func<string, int> getTargetStatusStacksById)
    {
        if (modifier.targetStatusData != null && getTargetStatusStacksByAsset != null)
            return Mathf.Max(0, getTargetStatusStacksByAsset(modifier.targetStatusData));

        if (modifier.targetStatusType == EnemySkill_SO.DiceStatusType.Burn && getTargetStatusStacksById != null)
            return Mathf.Max(0, getTargetStatusStacksById("burn"));

        return 0;
    }
}
