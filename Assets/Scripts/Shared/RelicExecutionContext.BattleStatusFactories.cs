using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

public sealed partial class RelicExecutionContext
{
    public static RelicExecutionContext CreatePlayerShieldStacksLost(
        RunState runState,
        BattleStatus_SO statusData,
        int lostStacks,
        int currentRound,
        Object source,
        Object target,
        BattleAllyController[] allies,
        BattleEnemyController[] enemies)
    {
        return new RelicExecutionContext
        {
            trigger = Relic_SO.RelicTrigger.OnPlayerShieldStacksLost,
            runState = runState,
            sourceIsPlayer = true,
            statusData = statusData,
            statusId = statusData != null ? statusData.StatusId : string.Empty,
            statusName = statusData != null ? statusData.DisplayName : string.Empty,
            statusStacks = Mathf.Max(0, lostStacks),
            currentRound = Mathf.Max(1, currentRound),
            source = source,
            target = target,
            allies = allies,
            enemies = enemies,
            feedbackMessages = new List<string>()
        };
    }

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
