using System;
using UnityEngine;

public static class BattleNegativeStatusService
{
    private const string DefaultPolarityListResourcePath = "BattleStatusPolarityList";

    private static readonly string[] NegativeStatusIds =
    {
        "burn",
        "bleed",
        "poison",
        "erode",
        "misfortune",
        "vulnerable",
        "slowness",
        "weakness",
        "chill"
    };

    private static BattleStatusPolarityList_SO configuredPolarityList;
    private static bool hasAttemptedLoad;

    public static void SetPolarityListForRuntime(BattleStatusPolarityList_SO polarityList)
    {
        configuredPolarityList = polarityList;
        hasAttemptedLoad = true;
    }

    public static void ClearPolarityListForRuntime()
    {
        configuredPolarityList = null;
        hasAttemptedLoad = false;
    }

    public static int CountNegativeStatusTypes(Func<string, int> getStatusStacksById)
    {
        if (getStatusStacksById == null)
            return 0;

        BattleStatusPolarityList_SO polarityList = ResolvePolarityList();
        if (polarityList != null)
            return polarityList.CountNegativeStatusTypes(getStatusStacksById);

        int count = 0;
        for (int i = 0; i < NegativeStatusIds.Length; i++)
        {
            if (getStatusStacksById(NegativeStatusIds[i]) > 0)
                count++;
        }

        return count;
    }

    public static bool HasAtLeastNegativeStatusTypes(Func<string, int> getStatusStacksById, int requiredTypes)
    {
        return CountNegativeStatusTypes(getStatusStacksById) >= Math.Max(1, requiredTypes);
    }

    public static bool IsNegativeStatusId(string statusId)
    {
        if (string.IsNullOrWhiteSpace(statusId))
            return false;

        BattleStatusPolarityList_SO polarityList = ResolvePolarityList();
        if (polarityList != null)
            return polarityList.ContainsNegativeStatusId(statusId);

        for (int i = 0; i < NegativeStatusIds.Length; i++)
        {
            if (string.Equals(statusId, NegativeStatusIds[i], StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    public static bool IsPositiveStatusId(string statusId)
    {
        if (string.IsNullOrWhiteSpace(statusId))
            return false;

        BattleStatusPolarityList_SO polarityList = ResolvePolarityList();
        return polarityList != null && polarityList.ContainsPositiveStatusId(statusId);
    }

    private static BattleStatusPolarityList_SO ResolvePolarityList()
    {
        if (configuredPolarityList != null)
            return configuredPolarityList;

        if (hasAttemptedLoad)
            return null;

        configuredPolarityList = Resources.Load<BattleStatusPolarityList_SO>(DefaultPolarityListResourcePath);
        hasAttemptedLoad = true;
        return configuredPolarityList;
    }
}
