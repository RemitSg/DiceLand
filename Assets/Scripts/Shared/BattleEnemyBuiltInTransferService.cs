using System;
using System.Collections.Generic;

public sealed class BattleEnemyBuiltInTransferPlan
{
    public bool ShouldTransfer { get; }
    public BattleStatusRuntimeEntry SelectedStatus { get; }
    public int TargetIndex { get; }
    public int TransferStacks { get; }

    public BattleEnemyBuiltInTransferPlan(
        bool shouldTransfer,
        BattleStatusRuntimeEntry selectedStatus,
        int targetIndex,
        int transferStacks)
    {
        ShouldTransfer = shouldTransfer;
        SelectedStatus = selectedStatus;
        TargetIndex = targetIndex;
        TransferStacks = transferStacks;
    }

    public static BattleEnemyBuiltInTransferPlan NoTransfer()
    {
        return new BattleEnemyBuiltInTransferPlan(false, null, -1, 0);
    }
}

public static class BattleEnemyBuiltInTransferDispatcher
{
    public static bool TryDispatch(
        BattleEnemyBuiltInTransferPlan plan,
        Func<BattleStatusRuntimeEntry, int, int> removeStacks,
        Func<BattleStatusRuntimeEntry, int, bool> addStacks,
        Action<BattleStatusRuntimeEntry, int> logSuccess)
    {
        if (plan == null || !plan.ShouldTransfer || plan.SelectedStatus == null || plan.TransferStacks <= 0)
            return false;
        if (removeStacks == null || addStacks == null)
            return false;

        int removedStacks = removeStacks(plan.SelectedStatus, plan.TransferStacks);
        if (removedStacks <= 0)
            return false;

        if (!addStacks(plan.SelectedStatus, removedStacks))
            return false;

        logSuccess?.Invoke(plan.SelectedStatus, removedStacks);
        return true;
    }
}

public static class BattleEnemyBuiltInTransferService
{
    public static List<BattleStatusRuntimeEntry> BuildTransferableStatuses(IEnumerable<BattleStatusRuntimeEntry> entries)
    {
        List<BattleStatusRuntimeEntry> result = new List<BattleStatusRuntimeEntry>();
        if (entries == null)
            return result;

        foreach (BattleStatusRuntimeEntry entry in entries)
        {
            if (IsTransferableNegativeStatus(entry))
                result.Add(entry);
        }

        return result;
    }

    public static bool IsTransferableNegativeStatus(BattleStatusRuntimeEntry entry)
    {
        if (entry == null || entry.stacks <= 0)
            return false;

        string statusId = entry.Key;
        if (string.IsNullOrWhiteSpace(statusId))
            return false;

        return BattleNegativeStatusService.IsNegativeStatusId(statusId);
    }

    public static BattleEnemyBuiltInTransferPlan ResolveTransferPlan(
        IReadOnlyList<BattleStatusRuntimeEntry> transferableStatuses,
        int targetCount,
        int transferAmount,
        Func<int, int> randomRange)
    {
        if (transferableStatuses == null || transferableStatuses.Count <= 0 || targetCount <= 0)
            return BattleEnemyBuiltInTransferPlan.NoTransfer();

        int statusIndex = ClampIndex(randomRange != null ? randomRange(transferableStatuses.Count) : 0, transferableStatuses.Count);
        int targetIndex = ClampIndex(randomRange != null ? randomRange(targetCount) : 0, targetCount);
        int transferStacks = Math.Max(1, transferAmount);

        return new BattleEnemyBuiltInTransferPlan(
            true,
            transferableStatuses[statusIndex],
            targetIndex,
            transferStacks);
    }

    private static int ClampIndex(int index, int count)
    {
        if (count <= 0)
            return -1;
        if (index < 0)
            return 0;
        if (index >= count)
            return count - 1;
        return index;
    }
}
