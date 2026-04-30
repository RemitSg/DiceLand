using System;
using System.Collections.Generic;
using UnityEngine;

public static class BattleStatusStackOperationService
{
    public static int NormalizeReduceAmount(int stackAmount)
    {
        return Mathf.Max(1, stackAmount);
    }

    public static string BuildActionLabel(
        EnemySkill_SO.StatusStackOperationType operationType,
        string statusName,
        int affectedStacks)
    {
        return operationType == EnemySkill_SO.StatusStackOperationType.ClearStacks
            ? $"清空 {statusName}"
            : $"使 {statusName} 减少 {Mathf.Max(0, affectedStacks)} 层";
    }
}

public static class BattleStatusStackOperationDispatcher
{
    public static int DispatchOperation(
        EnemySkill_SO.StatusStackOperationType operationType,
        int stackAmount,
        Func<int> getCurrentStacks,
        Func<int, int> removeStacks,
        Action clearStacks,
        Action<int> logApplied)
    {
        int affectedStacks = operationType == EnemySkill_SO.StatusStackOperationType.ClearStacks
            ? Mathf.Max(0, getCurrentStacks != null ? getCurrentStacks() : 0)
            : Mathf.Max(0, removeStacks != null ? removeStacks(BattleStatusStackOperationService.NormalizeReduceAmount(stackAmount)) : 0);

        if (operationType == EnemySkill_SO.StatusStackOperationType.ClearStacks)
            clearStacks?.Invoke();

        if (affectedStacks > 0)
            logApplied?.Invoke(affectedStacks);

        return affectedStacks;
    }

    public static void DispatchEffects<TTarget>(
        EnemySkill_SO.StatusStackOperationEffectEntry[] effects,
        EnemySkill_SO.DiceEffectTrigger trigger,
        TTarget currentTarget,
        IReadOnlyList<TTarget> opponents,
        Func<EnemySkill_SO.StatusStackOperationEffectEntry, bool> selfConditionMet,
        Func<EnemySkill_SO.StatusStackOperationEffectEntry, TTarget, bool> targetConditionMet,
        Func<EnemySkill_SO.DamageTargetMode, TTarget, IReadOnlyList<TTarget>, int, List<TTarget>> selectTargets,
        Func<TTarget, bool> canTarget,
        Action<EnemySkill_SO.StatusStackOperationEffectEntry, int> applySelf,
        Action<TTarget, EnemySkill_SO.StatusStackOperationEffectEntry, int> applyTarget)
    {
        if (effects == null)
            return;

        for (int i = 0; i < effects.Length; i++)
        {
            EnemySkill_SO.StatusStackOperationEffectEntry effect = effects[i];
            if (effect.trigger != trigger)
                continue;

            if (effect.targetMode == EnemySkill_SO.DamageTargetMode.Self)
            {
                if (selfConditionMet == null || selfConditionMet(effect))
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
                if (canTarget != null && !canTarget(target))
                    continue;
                if (targetConditionMet != null && !targetConditionMet(effect, target))
                    continue;

                applyTarget?.Invoke(target, effect, i);
            }
        }
    }
}
