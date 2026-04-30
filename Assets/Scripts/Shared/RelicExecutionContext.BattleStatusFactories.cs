using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

public sealed partial class RelicExecutionContext
{
    public static RelicExecutionContext CreateStatusApplied(
        RunState runState,
        RunGameManager runGameManager,
        BattleStatus_SO statusData,
        int statusStacks,
        bool sourceIsPlayer,
        Object source,
        Object target,
        string statusId = null,
        string statusName = null)
    {
        return new RelicExecutionContext
        {
            trigger = Relic_SO.RelicTrigger.OnStatusApplied,
            runState = runState,
            runGameManager = runGameManager,
            statusData = statusData,
            statusId = !string.IsNullOrWhiteSpace(statusId)
                ? statusId
                : (statusData != null ? statusData.StatusId : string.Empty),
            statusName = !string.IsNullOrWhiteSpace(statusName)
                ? statusName
                : (statusData != null ? statusData.DisplayName : string.Empty),
            statusStacks = Mathf.Max(0, statusStacks),
            sourceIsPlayer = sourceIsPlayer,
            source = source,
            target = target,
            feedbackMessages = new List<string>()
        };
    }
}
