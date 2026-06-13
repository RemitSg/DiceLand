using System;
using UnityEngine;

public static class BattleStatusEffectConditionService
{
    public static bool IsConditionMet(
        EnemySkill_SO.StatusStackOperationEffectEntry effect,
        int selfSpeed,
        int targetSpeed,
        Func<BattleStatus_SO, int> getSelfStatusStacksByAsset,
        Func<string, int> getSelfStatusStacksById,
        Func<BattleStatus_SO, int> getTargetStatusStacksByAsset,
        Func<string, int> getTargetStatusStacksById,
        EnemySkill_SO targetSkill = null)
    {
        if (!effect.useCondition)
            return true;

        switch (effect.condition)
        {
            case EnemySkill_SO.PowerModifierCondition.SelfHasStatus:
                return ResolveSelfStatusStacks(effect, getSelfStatusStacksByAsset, getSelfStatusStacksById) > 0;
            case EnemySkill_SO.PowerModifierCondition.SelfStatusStacksGreaterThan:
                return ResolveSelfStatusStacks(effect, getSelfStatusStacksByAsset, getSelfStatusStacksById) > Mathf.Max(0, effect.requiredConditionStatusStacks);
            case EnemySkill_SO.PowerModifierCondition.SelfStatusStacksGreaterOrEqual:
                return ResolveSelfStatusStacks(effect, getSelfStatusStacksByAsset, getSelfStatusStacksById) >= Mathf.Max(1, effect.requiredConditionStatusStacks);
            case EnemySkill_SO.PowerModifierCondition.SelfStatusStacksLessThan:
                return ResolveSelfStatusStacks(effect, getSelfStatusStacksByAsset, getSelfStatusStacksById) < Mathf.Max(1, effect.requiredConditionStatusStacks);
            case EnemySkill_SO.PowerModifierCondition.SelfStatusStacksLessOrEqual:
                return ResolveSelfStatusStacks(effect, getSelfStatusStacksByAsset, getSelfStatusStacksById) <= Mathf.Max(0, effect.requiredConditionStatusStacks);
            case EnemySkill_SO.PowerModifierCondition.TargetSkillIsSpecific:
                return effect.conditionTargetSkillData != null && targetSkill == effect.conditionTargetSkillData;
            case EnemySkill_SO.PowerModifierCondition.TargetHasStatus:
                return ResolveTargetStatusStacks(effect, getTargetStatusStacksByAsset, getTargetStatusStacksById) > 0;
            case EnemySkill_SO.PowerModifierCondition.TargetStatusStacksAtLeast:
                return ResolveTargetStatusStacks(effect, getTargetStatusStacksByAsset, getTargetStatusStacksById) >= Mathf.Max(1, effect.requiredConditionStatusStacks);
            case EnemySkill_SO.PowerModifierCondition.TargetStatusStacksLessOrEqual:
                return ResolveTargetStatusStacks(effect, getTargetStatusStacksByAsset, getTargetStatusStacksById) <= Mathf.Max(0, effect.requiredConditionStatusStacks);
            case EnemySkill_SO.PowerModifierCondition.TargetStatusStacksPerThreshold:
                return ResolveTargetStatusStacks(effect, getTargetStatusStacksByAsset, getTargetStatusStacksById) >= Mathf.Max(1, effect.requiredConditionStatusStacks);
            case EnemySkill_SO.PowerModifierCondition.TargetNegativeStatusTypesAtLeast:
                return BattleNegativeStatusService.HasAtLeastNegativeStatusTypes(getTargetStatusStacksById, effect.requiredConditionStatusStacks);
            default:
                return BattleModifierConditionService.IsConditionMet(
                    effect.condition,
                    selfSpeed,
                    targetSpeed,
                    effect.requiredConditionSpeedValue,
                    effect.requiredConditionSpeedDifference);
        }
    }

    private static int ResolveSelfStatusStacks(
        EnemySkill_SO.StatusStackOperationEffectEntry effect,
        Func<BattleStatus_SO, int> getSelfStatusStacksByAsset,
        Func<string, int> getSelfStatusStacksById)
    {
        if (effect.conditionStatusData != null && getSelfStatusStacksByAsset != null)
            return Mathf.Max(0, getSelfStatusStacksByAsset(effect.conditionStatusData));

        if (TryResolveFallbackStatusId(effect.conditionStatusType, out string statusId) && getSelfStatusStacksById != null)
            return Mathf.Max(0, getSelfStatusStacksById(statusId));

        return 0;
    }

    private static int ResolveTargetStatusStacks(
        EnemySkill_SO.StatusStackOperationEffectEntry effect,
        Func<BattleStatus_SO, int> getTargetStatusStacksByAsset,
        Func<string, int> getTargetStatusStacksById)
    {
        if (effect.conditionStatusData != null && getTargetStatusStacksByAsset != null)
            return Mathf.Max(0, getTargetStatusStacksByAsset(effect.conditionStatusData));

        if (TryResolveFallbackStatusId(effect.conditionStatusType, out string statusId) && getTargetStatusStacksById != null)
            return Mathf.Max(0, getTargetStatusStacksById(statusId));

        return 0;
    }

    public static bool IsConditionMet(
        EnemySkill_SO.StatusEffectEntry effect,
        int selfSpeed,
        int targetSpeed,
        Func<BattleStatus_SO, int> getSelfStatusStacksByAsset,
        Func<string, int> getSelfStatusStacksById,
        Func<BattleStatus_SO, int> getTargetStatusStacksByAsset,
        Func<string, int> getTargetStatusStacksById,
        EnemySkill_SO targetSkill = null)
    {
        if (!effect.useCondition)
            return true;

        switch (effect.condition)
        {
            case EnemySkill_SO.PowerModifierCondition.SelfHasStatus:
                return ResolveSelfStatusStacks(effect, getSelfStatusStacksByAsset, getSelfStatusStacksById) > 0;
            case EnemySkill_SO.PowerModifierCondition.SelfStatusStacksGreaterThan:
                return ResolveSelfStatusStacks(effect, getSelfStatusStacksByAsset, getSelfStatusStacksById) > Mathf.Max(0, effect.requiredConditionStatusStacks);
            case EnemySkill_SO.PowerModifierCondition.SelfStatusStacksGreaterOrEqual:
                return ResolveSelfStatusStacks(effect, getSelfStatusStacksByAsset, getSelfStatusStacksById) >= Mathf.Max(1, effect.requiredConditionStatusStacks);
            case EnemySkill_SO.PowerModifierCondition.SelfStatusStacksLessThan:
                return ResolveSelfStatusStacks(effect, getSelfStatusStacksByAsset, getSelfStatusStacksById) < Mathf.Max(1, effect.requiredConditionStatusStacks);
            case EnemySkill_SO.PowerModifierCondition.SelfStatusStacksLessOrEqual:
                return ResolveSelfStatusStacks(effect, getSelfStatusStacksByAsset, getSelfStatusStacksById) <= Mathf.Max(0, effect.requiredConditionStatusStacks);
            case EnemySkill_SO.PowerModifierCondition.TargetSkillIsSpecific:
                return effect.conditionTargetSkillData != null && targetSkill == effect.conditionTargetSkillData;
            case EnemySkill_SO.PowerModifierCondition.TargetHasStatus:
                return ResolveTargetStatusStacks(effect, getTargetStatusStacksByAsset, getTargetStatusStacksById) > 0;
            case EnemySkill_SO.PowerModifierCondition.TargetStatusStacksAtLeast:
                return ResolveTargetStatusStacks(effect, getTargetStatusStacksByAsset, getTargetStatusStacksById) >= Mathf.Max(1, effect.requiredConditionStatusStacks);
            case EnemySkill_SO.PowerModifierCondition.TargetStatusStacksLessOrEqual:
                return ResolveTargetStatusStacks(effect, getTargetStatusStacksByAsset, getTargetStatusStacksById) <= Mathf.Max(0, effect.requiredConditionStatusStacks);
            case EnemySkill_SO.PowerModifierCondition.TargetStatusStacksPerThreshold:
                return ResolveTargetStatusStacks(effect, getTargetStatusStacksByAsset, getTargetStatusStacksById) >= Mathf.Max(1, effect.requiredConditionStatusStacks);
            case EnemySkill_SO.PowerModifierCondition.TargetNegativeStatusTypesAtLeast:
                return BattleNegativeStatusService.HasAtLeastNegativeStatusTypes(getTargetStatusStacksById, effect.requiredConditionStatusStacks);
            default:
                return BattleModifierConditionService.IsConditionMet(
                    effect.condition,
                    selfSpeed,
                    targetSpeed,
                    effect.requiredConditionSpeedValue,
                    effect.requiredConditionSpeedDifference);
        }
    }

    private static int ResolveSelfStatusStacks(
        EnemySkill_SO.StatusEffectEntry effect,
        Func<BattleStatus_SO, int> getSelfStatusStacksByAsset,
        Func<string, int> getSelfStatusStacksById)
    {
        if (effect.conditionStatusData != null && getSelfStatusStacksByAsset != null)
            return Mathf.Max(0, getSelfStatusStacksByAsset(effect.conditionStatusData));

        if (TryResolveFallbackStatusId(effect.conditionStatusType, out string statusId) && getSelfStatusStacksById != null)
            return Mathf.Max(0, getSelfStatusStacksById(statusId));

        return 0;
    }

    private static int ResolveTargetStatusStacks(
        EnemySkill_SO.StatusEffectEntry effect,
        Func<BattleStatus_SO, int> getTargetStatusStacksByAsset,
        Func<string, int> getTargetStatusStacksById)
    {
        if (effect.conditionStatusData != null && getTargetStatusStacksByAsset != null)
            return Mathf.Max(0, getTargetStatusStacksByAsset(effect.conditionStatusData));

        if (TryResolveFallbackStatusId(effect.conditionStatusType, out string statusId) && getTargetStatusStacksById != null)
            return Mathf.Max(0, getTargetStatusStacksById(statusId));

        return 0;
    }

    private static bool TryResolveFallbackStatusId(EnemySkill_SO.DiceStatusType statusType, out string statusId)
    {
        switch (statusType)
        {
            case EnemySkill_SO.DiceStatusType.Burn:
                statusId = "burn";
                return true;
            case EnemySkill_SO.DiceStatusType.Electro:
                statusId = "current";
                return true;
            case EnemySkill_SO.DiceStatusType.Darkness:
                statusId = "darkness";
                return true;
            case EnemySkill_SO.DiceStatusType.Mark:
                statusId = "target";
                return true;
            default:
                statusId = string.Empty;
                return false;
        }
    }
}
