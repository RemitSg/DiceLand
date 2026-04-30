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

    public static string FormatAttackTie(int allyPower, int enemyPower)
    {
        return $"-> 平局（{allyPower} = {enemyPower}），双方无伤害";
    }

    public static string FormatAllyAttackEnemyDefenseResult(
        bool attackBreaksDefense,
        string allyName,
        string enemyName,
        int allyHitPower,
        int enemyCurrentHp,
        int enemyMaxHp,
        int allyPower,
        int enemyPower)
    {
        if (attackBreaksDefense)
            return $"-> 敌方防御骰拼点落败，{allyName} 在还原拼点修正后造成 {allyHitPower} 伤害，{enemyName} 剩余HP {enemyCurrentHp}/{enemyMaxHp}";

        if (enemyPower > allyPower)
            return $"-> 敌方防御成功！完全免疫本次伤害与命中时词条（{enemyPower} > {allyPower}）";

        return $"-> 敌方格挡住了本次攻击，最终伤害为 0（{enemyPower} = {allyPower}）";
    }

    public static string FormatAllyDefenseEnemyAttackResult(
        bool attackBreaksDefense,
        string enemyName,
        string allyName,
        int enemyHitPower,
        int allyCurrentHp,
        int allyMaxHp,
        int allyPower,
        int enemyPower,
        int restoredDefenseEnergy)
    {
        if (attackBreaksDefense)
            return $"-> 友方防御骰拼点落败，{enemyName} 在还原拼点修正后造成 {enemyHitPower} 伤害，{allyName} 剩余HP {allyCurrentHp}/{allyMaxHp}";

        if (allyPower > enemyPower)
            return $"-> 友方防御成功！完全免疫本次伤害与命中时词条（{allyPower} > {enemyPower}），并恢复 {restoredDefenseEnergy} 能量";

        return $"-> 友方格挡住了本次攻击，最终伤害为 0（{allyPower} = {enemyPower}）";
    }

    public static string FormatBothDefense(int allyPower, int enemyPower)
    {
        return $"-> 双方均为防御技能，本次拼点不会造成伤害（{allyPower} vs {enemyPower}）";
    }
}
