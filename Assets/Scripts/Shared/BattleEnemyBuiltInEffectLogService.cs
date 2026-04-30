public static class BattleEnemyBuiltInEffectLogService
{
    public static string BuildHealLog(string attackerName, string effectLabel, string targetName, int healedAmount, int currentHP, int maxHP)
    {
        string targetPart = string.IsNullOrWhiteSpace(targetName)
            ? string.Empty
            : $"使 {targetName} ";
        string hpLabel = string.IsNullOrWhiteSpace(targetName) ? "当前HP" : "当前 HP";
        return $"词条触发：{attackerName}[{effectLabel}] {targetPart}恢复 {healedAmount} 点生命，{hpLabel} {currentHP}/{maxHP}";
    }

    public static string BuildDamageLog(string attackerName, string effectLabel, string targetName, int dealtDamage)
    {
        string targetPart = string.IsNullOrWhiteSpace(targetName)
            ? "自己"
            : targetName;
        string action = string.IsNullOrWhiteSpace(targetName) ? "造成" : "追加造成";
        return $"词条触发：{attackerName}[{effectLabel}] 对 {targetPart} {action} {dealtDamage} 点伤害";
    }

    public static string BuildTransferLog(string attackerName, string effectLabel, string statusName, int removedStacks, string targetName)
    {
        return $"词条触发：{attackerName}[{effectLabel}] 将 {removedStacks} 层 {statusName} 转移给 {targetName}";
    }
}
