using System;
using System.Collections.Generic;

public static class BattleStatusGrantEligibilityService
{
    public static bool ShouldApplyGrant(BattleStatus_SO statusData, int stacks)
    {
        return statusData != null && stacks > 0;
    }
}

public static class BattleStatusGrantDispatcher
{
    public static void DispatchGrants<TTarget>(
        IEnumerable<PendingBattleStatusGrant> grants,
        IEnumerable<TTarget> targets,
        Func<TTarget, bool> isTargetUnavailable,
        Func<TTarget, PendingBattleStatusGrant, bool> addStatus,
        Action<TTarget, PendingBattleStatusGrant> logApplied)
    {
        if (grants == null || targets == null || addStatus == null)
            return;

        foreach (PendingBattleStatusGrant grant in grants)
        {
            if (grant == null || !BattleStatusGrantEligibilityService.ShouldApplyGrant(grant.statusData, grant.stacks))
                continue;

            foreach (TTarget target in targets)
            {
                if (isTargetUnavailable != null && isTargetUnavailable(target))
                    continue;

                if (addStatus(target, grant))
                    logApplied?.Invoke(target, grant);
            }
        }
    }
}

public static class BattleEnemyStatusGainDispatcher
{
    public static bool Dispatch(
        IEnumerable<EnemyData_SO.StatusGainEntry> entries,
        Func<EnemyData_SO.StatusGainEntry, bool> addStatus,
        Func<EnemyData_SO.StatusGainEntry, int> getCurrentStacks,
        Action<EnemyData_SO.StatusGainEntry, int> logApplied,
        Action<EnemyData_SO.StatusGainEntry> onApplied = null)
    {
        if (entries == null || addStatus == null)
            return false;

        bool appliedAny = false;
        foreach (EnemyData_SO.StatusGainEntry entry in entries)
        {
            if (!BattleStatusGrantEligibilityService.ShouldApplyGrant(entry.statusData, entry.stackAmount))
                continue;

            if (!addStatus(entry))
                continue;

            appliedAny = true;
            onApplied?.Invoke(entry);
            int currentStacks = getCurrentStacks != null ? getCurrentStacks(entry) : 0;
            logApplied?.Invoke(entry, currentStacks);
        }

        return appliedAny;
    }
}
