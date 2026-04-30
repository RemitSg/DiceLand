using System;
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
}
