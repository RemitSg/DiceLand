using System;

public static class BattleStatusTickLogService
{
    public static string BuildDamageLog(string targetName, string statusName, int damage, int stacks, int currentHP, int maxHP)
    {
        return $"状态触发：{targetName} 受到 {damage} 点 {statusName} 伤害，当前 {statusName} {stacks} 层，剩余HP {currentHP}/{maxHP}";
    }

    public static string BuildSpeedLog(string targetName, string statusName, int speedBonus, int stacks)
    {
        if (speedBonus > 0)
            return $"状态触发：{targetName} 获得 {speedBonus} 点 {statusName} 速度提升，当前 {statusName} {stacks} 层";
        if (speedBonus < 0)
            return $"状态触发：{targetName} 受到 {Math.Abs(speedBonus)} 点 {statusName} 速度降低，当前 {statusName} {stacks} 层";
        return string.Empty;
    }

    public static string BuildAppliedStatusLog(string targetName, string appliedStatusName, int appliedStacks)
    {
        return $"状态触发：{targetName} 获得 {appliedStacks} 层 {appliedStatusName}";
    }
}
