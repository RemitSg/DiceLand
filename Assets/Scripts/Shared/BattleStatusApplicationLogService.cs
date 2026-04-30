public static class BattleStatusApplicationLogService
{
    public static string BuildDelayedStatusLog(
        string attackerName,
        string targetName,
        string effectLabel,
        string statusName,
        int delayRounds,
        int repeatCount,
        int stackAmount)
    {
        string schedule = repeatCount > 1
            ? $"接下来 {repeatCount} 个回合开始时"
            : $"下 {delayRounds} 个回合开始时";
        return $"词条触发：{attackerName}[{effectLabel}] 将在 {schedule} 使 {targetName} 获得 {stackAmount} 层 {statusName}";
    }

    public static string BuildImmuneStatusBlockedLog(
        string attackerName,
        string targetName,
        string effectLabel,
        string statusName)
    {
        return $"词条触发：{attackerName}[{effectLabel}] 试图使 {targetName} 获得 {statusName}，但目标免疫该状态效果";
    }
}
