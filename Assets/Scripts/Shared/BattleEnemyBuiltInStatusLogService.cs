public static class BattleEnemyBuiltInStatusLogService
{
    public static string BuildDelayedLog(string attackerName, string effectLabel, string targetName, string statusName, int stackAmount)
    {
        string resolvedTargetName = string.IsNullOrWhiteSpace(targetName) ? "自己" : targetName;
        return $"词条触发：{attackerName}[{effectLabel}] 使 {resolvedTargetName} 在下回合开始时获得 {stackAmount} 层 {statusName}";
    }

    public static string BuildImmediateLog(string attackerName, string effectLabel, string targetName, string statusName, int stackAmount, int currentStacks, string prefix)
    {
        string resolvedPrefix = string.IsNullOrWhiteSpace(prefix) ? string.Empty : $"{prefix}：";
        string resolvedTargetName = string.IsNullOrWhiteSpace(targetName) ? "自己" : targetName;
        return $"词条触发：{attackerName}[{effectLabel}] {resolvedPrefix}使 {resolvedTargetName} 获得 {stackAmount} 层 {statusName}，当前 {statusName} {currentStacks} 层";
    }
}
