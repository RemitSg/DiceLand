using System;
using UnityEngine;

[Serializable]
public sealed class BattleStatusRuntimeEntry
{
    public BattleStatus_SO statusData;
    public string fallbackStatusId;
    public string displayName;
    public int stacks;

    public string Key
    {
        get
        {
            if (statusData != null && !string.IsNullOrWhiteSpace(statusData.StatusId))
                return statusData.StatusId;
            if (!string.IsNullOrWhiteSpace(fallbackStatusId))
                return fallbackStatusId;
            return displayName ?? string.Empty;
        }
    }

    public string DisplayName
    {
        get
        {
            if (statusData != null && !string.IsNullOrWhiteSpace(statusData.DisplayName))
                return statusData.DisplayName;
            return string.IsNullOrWhiteSpace(displayName) ? Key : displayName;
        }
    }

    public Sprite IconSprite => statusData != null ? statusData.IconSprite : null;
    public Color AccentColor => statusData != null ? statusData.AccentColor : Color.white;
}

[Serializable]
public sealed class PendingBattleStatusApplication
{
    public BattleStatus_SO statusData;
    public string fallbackStatusId;
    public string displayName;
    public int stacks;
    public int remainingRoundStarts;
    public int repeatCountRemaining;
}

[Serializable]
public struct BattleStatusTickResult
{
    public BattleStatus_SO statusData;
    public string statusId;
    public string displayName;
    public int stacks;
    public int damage;
    public int speedBonus;
    public BattleStatus_SO appliedStatusData;
    public string appliedStatusName;
    public int appliedStacks;
}
