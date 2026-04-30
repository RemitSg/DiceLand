using System;
using UnityEngine;

public static class BattleHealingEffectResolutionService
{
    public static bool ShouldApplyEffect(
        EnemySkill_SO.HealingEffectEntry effect,
        EnemySkill_SO.DiceEffectTrigger trigger,
        int selfSpeed,
        int targetSpeed,
        Func<BattleStatus_SO, int> resolveConditionStatusDataStacks,
        Func<string, int> resolveConditionStatusIdStacks)
    {
        return effect.trigger == trigger
            && IsConditionMet(
                effect,
                selfSpeed,
                targetSpeed,
                resolveConditionStatusDataStacks,
                resolveConditionStatusIdStacks);
    }

    public static int ResolveHealingAmount(
        EnemySkill_SO.HealingEffectEntry effect,
        int dealtDamage,
        Func<EnemySkill_SO.DamageTargetMode, int> resolveStatusStacks)
    {
        switch (effect.amountMode)
        {
            case EnemySkill_SO.HealAmountMode.DamageDealt:
                return Mathf.Max(0, dealtDamage);
            case EnemySkill_SO.HealAmountMode.StatusStacks:
                int stacks = resolveStatusStacks != null ? resolveStatusStacks(effect.statusStackSourceMode) : 0;
                return Mathf.Max(0, stacks * Mathf.Max(1, effect.healAmount));
            default:
                return Mathf.Max(0, effect.healAmount);
        }
    }

    public static int ResolveStatusStacks(
        EnemySkill_SO.HealingEffectEntry effect,
        Func<BattleStatus_SO, int> resolveStatusDataStacks,
        Func<string, int> resolveStatusIdStacks)
    {
        if (effect.statusData != null)
            return Mathf.Max(0, resolveStatusDataStacks != null ? resolveStatusDataStacks(effect.statusData) : 0);

        if (effect.statusType == EnemySkill_SO.DiceStatusType.Burn)
            return Mathf.Max(0, resolveStatusIdStacks != null ? resolveStatusIdStacks("burn") : 0);

        return 0;
    }

    public static bool IsConditionMet(
        EnemySkill_SO.HealingEffectEntry effect,
        int selfSpeed,
        int targetSpeed,
        Func<BattleStatus_SO, int> resolveConditionStatusDataStacks,
        Func<string, int> resolveConditionStatusIdStacks)
    {
        if (!effect.useCondition)
            return true;

        switch (effect.condition)
        {
            case EnemySkill_SO.PowerModifierCondition.TargetHasStatus:
                return ResolveConditionStatusStacks(effect, resolveConditionStatusDataStacks, resolveConditionStatusIdStacks) > 0;
            case EnemySkill_SO.PowerModifierCondition.TargetStatusStacksAtLeast:
            case EnemySkill_SO.PowerModifierCondition.TargetStatusStacksPerThreshold:
                return ResolveConditionStatusStacks(effect, resolveConditionStatusDataStacks, resolveConditionStatusIdStacks)
                    >= Mathf.Max(1, effect.requiredConditionStatusStacks);
            default:
                return BattleModifierConditionService.IsConditionMet(
                    effect.condition,
                    selfSpeed,
                    targetSpeed,
                    effect.requiredConditionSpeedValue,
                    effect.requiredConditionSpeedDifference);
        }
    }

    public static int ResolveConditionStatusStacks(
        EnemySkill_SO.HealingEffectEntry effect,
        Func<BattleStatus_SO, int> resolveConditionStatusDataStacks,
        Func<string, int> resolveConditionStatusIdStacks)
    {
        if (effect.conditionStatusData != null)
            return Mathf.Max(0, resolveConditionStatusDataStacks != null ? resolveConditionStatusDataStacks(effect.conditionStatusData) : 0);

        if (effect.statusData != null)
            return Mathf.Max(0, resolveConditionStatusDataStacks != null ? resolveConditionStatusDataStacks(effect.statusData) : 0);

        if (effect.conditionStatusType == EnemySkill_SO.DiceStatusType.Burn || effect.statusType == EnemySkill_SO.DiceStatusType.Burn)
            return Mathf.Max(0, resolveConditionStatusIdStacks != null ? resolveConditionStatusIdStacks("burn") : 0);

        return 0;
    }
}

public static class BattleHealingEffectDispatcher
{
    public static int DispatchHeal(int healAmount, Func<int, int> heal, Action<int> logApplied)
    {
        if (heal == null)
            return 0;

        int actualHeal = heal(Mathf.Max(0, healAmount));
        if (actualHeal > 0)
            logApplied?.Invoke(actualHeal);

        return Mathf.Max(0, actualHeal);
    }
}
