using System;
using System.Collections.Generic;
using UnityEngine;

public static class BattleStatusBurstDamageService
{
    public static int CalculateBurstDamage(int stacks, int damagePerStack)
    {
        return Mathf.Max(0, stacks) * Mathf.Max(0, damagePerStack);
    }
}

public static class BattleStatusBurstDispatcher
{
    public static int DispatchBurst(
        int stacks,
        int damagePerStack,
        Func<int, int> modifyDamage,
        Action<int, int> applyDamage,
        Action clearStacks,
        bool clearStacksAfterBurst,
        Action<int, int> logApplied)
    {
        int burstDamage = BattleStatusBurstDamageService.CalculateBurstDamage(stacks, damagePerStack);
        if (burstDamage <= 0)
            return 0;

        int modifiedDamage = modifyDamage != null ? modifyDamage(burstDamage) : burstDamage;
        applyDamage?.Invoke(modifiedDamage, burstDamage);

        if (clearStacksAfterBurst)
            clearStacks?.Invoke();

        logApplied?.Invoke(modifiedDamage, burstDamage);
        return modifiedDamage;
    }

    public static void DispatchEffects<TTarget>(
        EnemySkill_SO.StatusBurstDamageEffectEntry[] effects,
        EnemySkill_SO.DiceEffectTrigger trigger,
        TTarget currentTarget,
        IReadOnlyList<TTarget> opponents,
        Func<EnemySkill_SO.DamageTargetMode, TTarget, IReadOnlyList<TTarget>, int, List<TTarget>> selectTargets,
        Func<TTarget, bool> canTarget,
        Action<EnemySkill_SO.StatusBurstDamageEffectEntry, int> applySelf,
        Action<TTarget, EnemySkill_SO.StatusBurstDamageEffectEntry, int> applyTarget)
    {
        if (effects == null)
            return;

        for (int i = 0; i < effects.Length; i++)
        {
            EnemySkill_SO.StatusBurstDamageEffectEntry effect = effects[i];
            if (effect.trigger != trigger || effect.damagePerStack <= 0)
                continue;

            if (effect.targetMode == EnemySkill_SO.DamageTargetMode.Self)
            {
                applySelf?.Invoke(effect, i);
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
                if (canTarget == null || canTarget(target))
                    applyTarget?.Invoke(target, effect, i);
            }
        }
    }
}
