using System;
using System.Collections.Generic;

public static class BattleHealingStatusStackSourceService
{
    public static int ResolveStatusStackSource<TSelf, TTarget>(
        EnemySkill_SO.DamageTargetMode sourceMode,
        TSelf self,
        TTarget currentTarget,
        List<TTarget> candidates,
        int randomTargetCount,
        Func<TSelf, int> resolveSelfStacks,
        Func<TTarget, int> resolveTargetStacks,
        Func<EnemySkill_SO.DamageTargetMode, TTarget, List<TTarget>, int, List<TTarget>> selectTargets)
    {
        if (sourceMode == EnemySkill_SO.DamageTargetMode.Self)
            return resolveSelfStacks != null ? Math.Max(0, resolveSelfStacks(self)) : 0;

        List<TTarget> targets = selectTargets != null
            ? selectTargets(sourceMode, currentTarget, candidates, randomTargetCount)
            : new List<TTarget>();
        if (targets == null || targets.Count <= 0)
            return 0;

        return resolveTargetStacks != null ? Math.Max(0, resolveTargetStacks(targets[0])) : 0;
    }
}
