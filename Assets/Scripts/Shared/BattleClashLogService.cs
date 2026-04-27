public static class BattleClashLogService
{
    public static string FormatClashDice(
        int diceIndex,
        string allyName,
        int basePower,
        int bonusPower,
        string enemyName,
        int enemyBasePower,
        int enemyBonusPower)
    {
        return $"第 {diceIndex + 1} 段拼点：{allyName}({BattlePowerCalculationService.FormatPowerWithBonus(basePower, bonusPower)}) vs {enemyName}({BattlePowerCalculationService.FormatPowerWithBonus(enemyBasePower, enemyBonusPower)})";
    }

    public static string FormatAllyAttackWinFollowUp(
        int followUpBasePower,
        int winAttackBonus,
        string targetName,
        int hitPower,
        int targetCurrentHp,
        int targetMaxHp)
    {
        return $"-> 友方胜！追击威力 {BattlePowerCalculationService.FormatPowerWithBonus(followUpBasePower, winAttackBonus)} ，对 {targetName} 造成 {hitPower} 伤害，剩余HP {targetCurrentHp}/{targetMaxHp}";
    }

    public static string FormatEnemyAttackWinFollowUp(
        int followUpBasePower,
        int winAttackBonus,
        string enemyName,
        string targetName,
        int hitPower,
        int targetCurrentHp,
        int targetMaxHp)
    {
        return $"-> 敌方胜！追击威力 {BattlePowerCalculationService.FormatPowerWithBonus(followUpBasePower, winAttackBonus)} ，{enemyName} 对 {targetName} 造成 {hitPower} 伤害，剩余HP {targetCurrentHp}/{targetMaxHp}";
    }
}
