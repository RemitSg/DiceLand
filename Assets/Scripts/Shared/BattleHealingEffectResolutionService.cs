using System;
using System.Collections.Generic;
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

    public static void DispatchEffects<TTarget>(
        EnemySkill_SO.HealingEffectEntry[] effects,
        EnemySkill_SO.DiceEffectTrigger trigger,
        TTarget currentTarget,
        IReadOnlyList<TTarget> opponents,
        Func<EnemySkill_SO.HealingEffectEntry, bool> shouldApply,
        Func<EnemySkill_SO.HealingEffectEntry, int> resolveHealAmount,
        Func<EnemySkill_SO.DamageTargetMode, TTarget, IReadOnlyList<TTarget>, int, List<TTarget>> selectTargets,
        Func<TTarget, bool> canTarget,
        Func<int, int> healSelf,
        Func<TTarget, int, int> healTarget,
        Action<EnemySkill_SO.HealingEffectEntry, int, int> logSelf,
        Action<TTarget, EnemySkill_SO.HealingEffectEntry, int, int> logTarget)
    {
        if (effects == null)
            return;

        for (int i = 0; i < effects.Length; i++)
        {
            EnemySkill_SO.HealingEffectEntry effect = effects[i];
            if (effect.trigger != trigger)
                continue;
            if (shouldApply != null && !shouldApply(effect))
                continue;

            if (effect.targetMode == EnemySkill_SO.DamageTargetMode.Self)
            {
                int index = i;
                DispatchHeal(
                    resolveHealAmount != null ? resolveHealAmount(effect) : 0,
                    healSelf,
                    actualHeal => logSelf?.Invoke(effect, index, actualHeal));
                continue;
            }

            List<TTarget> targets = selectTargets != null
                ? selectTargets(effect.targetMode, currentTarget, opponents, effect.randomTargetCount)
                : new List<TTarget>();
            if (targets == null)
                continue;

            for (int j = 0; j < targets.Count; j++)
            {
                TTarget target = targets[j];
                if (canTarget != null && !canTarget(target))
                    continue;

                int index = i;
                DispatchHeal(
                    resolveHealAmount != null ? resolveHealAmount(effect) : 0,
                    amount => healTarget != null ? healTarget(target, amount) : 0,
                    actualHeal => logTarget?.Invoke(target, effect, index, actualHeal));
            }
        }
    }
}
