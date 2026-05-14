using System;
using UnityEngine;

public static class BattleStatusResistanceService
{
    public const string ResistanceStatusId = "resistance";

    public static bool ShouldConsumeResistanceForIncomingStatus(
        BattleStatus_SO incomingStatusData,
        string fallbackStatusId,
        Func<string, int> getStatusStacks)
    {
        if (getStatusStacks == null)
            return false;

        string incomingStatusId = ResolveStatusId(incomingStatusData, fallbackStatusId);
        if (!BattleNegativeStatusService.IsNegativeStatusId(incomingStatusId))
            return false;

        return Mathf.Max(0, getStatusStacks(ResistanceStatusId)) > 0;
    }

    private static string ResolveStatusId(BattleStatus_SO statusData, string fallbackStatusId)
    {
        if (statusData != null && !string.IsNullOrWhiteSpace(statusData.StatusId))
            return statusData.StatusId;

        return fallbackStatusId ?? string.Empty;
    }
}
