public static class BattleSingleAttackLogService
{
    public static string FormatDefenseOnly(
        int diceIndex,
        string contextLabel,
        string actorName,
        string skillSummary,
        string formattedPower)
    {
        return $"{FormatSegmentPrefix(diceIndex, contextLabel)}：{actorName}[{skillSummary}]({formattedPower}) 为防御技能，未与攻击骰拼点，不会造成拼点/伤害结算，但【使用时】词条已生效";
    }

    public static string FormatHit(
        int diceIndex,
        string contextLabel,
        string actorName,
        string skillSummary,
        string formattedPower,
        string targetName,
        int targetCurrentHp,
        int targetMaxHp)
    {
        return $"{FormatSegmentPrefix(diceIndex, contextLabel)}：{actorName}[{skillSummary}]({formattedPower}) -> {targetName}，剩余HP {targetCurrentHp}/{targetMaxHp}";
    }

    private static string FormatSegmentPrefix(int diceIndex, string contextLabel)
    {
        int segmentNumber = diceIndex + 1;
        return string.IsNullOrWhiteSpace(contextLabel)
            ? $"第 {segmentNumber} 段"
            : $"第 {segmentNumber} 段（{contextLabel}）";
    }
}
