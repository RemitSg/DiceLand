using NUnit.Framework;

public class BattleClashLogServiceTests
{
    [Test]
    public void FormatClashDice_UsesFormattedAllyAndEnemyPower()
    {
        string log = BattleClashLogService.FormatClashDice(
            1,
            "Jenny",
            basePower: 4,
            bonusPower: 2,
            enemyName: "Goblin",
            enemyBasePower: 5,
            enemyBonusPower: -1);

        Assert.AreEqual("第 2 段拼点：Jenny(4+2=6) vs Goblin(5-1=4)", log);
    }

    [Test]
    public void FormatAllyAttackWinFollowUp_FormatsFollowUpPowerAndTargetHp()
    {
        string log = BattleClashLogService.FormatAllyAttackWinFollowUp(
            followUpBasePower: 3,
            winAttackBonus: 2,
            targetName: "Goblin",
            hitPower: 5,
            targetCurrentHp: 4,
            targetMaxHp: 9);

        Assert.AreEqual("-> 友方胜！追击威力 3+2=5 ，对 Goblin 造成 5 伤害，剩余HP 4/9", log);
    }

    [Test]
    public void FormatEnemyAttackWinFollowUp_FormatsAttackerTargetAndHp()
    {
        string log = BattleClashLogService.FormatEnemyAttackWinFollowUp(
            followUpBasePower: 6,
            winAttackBonus: -2,
            enemyName: "Goblin",
            targetName: "Jenny",
            hitPower: 4,
            targetCurrentHp: 8,
            targetMaxHp: 12);

        Assert.AreEqual("-> 敌方胜！追击威力 6-2=4 ，Goblin 对 Jenny 造成 4 伤害，剩余HP 8/12", log);
    }

    [Test]
    public void FormatAttackTie_FormatsEqualPowerNoDamage()
    {
        string log = BattleClashLogService.FormatAttackTie(5, 5);

        Assert.AreEqual("-> 平局（5 = 5），双方无伤害", log);
    }

    [Test]
    public void FormatAllyAttackEnemyDefenseResult_FormatsBreakWinAndTieBranches()
    {
        string breakLog = BattleClashLogService.FormatAllyAttackEnemyDefenseResult(
            attackBreaksDefense: true,
            allyName: "Jenny",
            enemyName: "Shield",
            allyHitPower: 4,
            enemyCurrentHp: 3,
            enemyMaxHp: 9,
            allyPower: 8,
            enemyPower: 4);
        string defenseWinLog = BattleClashLogService.FormatAllyAttackEnemyDefenseResult(
            attackBreaksDefense: false,
            allyName: "Jenny",
            enemyName: "Shield",
            allyHitPower: 0,
            enemyCurrentHp: 3,
            enemyMaxHp: 9,
            allyPower: 3,
            enemyPower: 5);
        string defenseTieLog = BattleClashLogService.FormatAllyAttackEnemyDefenseResult(
            attackBreaksDefense: false,
            allyName: "Jenny",
            enemyName: "Shield",
            allyHitPower: 0,
            enemyCurrentHp: 3,
            enemyMaxHp: 9,
            allyPower: 5,
            enemyPower: 5);

        Assert.AreEqual("-> 敌方防御骰拼点落败，Jenny 在还原拼点修正后造成 4 伤害，Shield 剩余HP 3/9", breakLog);
        Assert.AreEqual("-> 敌方防御成功！完全免疫本次伤害与命中时词条（5 > 3）", defenseWinLog);
        Assert.AreEqual("-> 敌方格挡住了本次攻击，最终伤害为 0（5 = 5）", defenseTieLog);
    }

    [Test]
    public void FormatAllyDefenseEnemyAttackResult_FormatsBreakWinAndTieBranches()
    {
        string breakLog = BattleClashLogService.FormatAllyDefenseEnemyAttackResult(
            attackBreaksDefense: true,
            enemyName: "Blade",
            allyName: "Jenny",
            enemyHitPower: 6,
            allyCurrentHp: 7,
            allyMaxHp: 12,
            allyPower: 4,
            enemyPower: 9,
            restoredDefenseEnergy: 0);
        string defenseWinLog = BattleClashLogService.FormatAllyDefenseEnemyAttackResult(
            attackBreaksDefense: false,
            enemyName: "Blade",
            allyName: "Jenny",
            enemyHitPower: 0,
            allyCurrentHp: 7,
            allyMaxHp: 12,
            allyPower: 8,
            enemyPower: 5,
            restoredDefenseEnergy: 3);
        string defenseTieLog = BattleClashLogService.FormatAllyDefenseEnemyAttackResult(
            attackBreaksDefense: false,
            enemyName: "Blade",
            allyName: "Jenny",
            enemyHitPower: 0,
            allyCurrentHp: 7,
            allyMaxHp: 12,
            allyPower: 5,
            enemyPower: 5,
            restoredDefenseEnergy: 0);

        Assert.AreEqual("-> 友方防御骰拼点落败，Blade 在还原拼点修正后造成 6 伤害，Jenny 剩余HP 7/12", breakLog);
        Assert.AreEqual("-> 友方防御成功！完全免疫本次伤害与命中时词条（8 > 5），并恢复 3 能量", defenseWinLog);
        Assert.AreEqual("-> 友方格挡住了本次攻击，最终伤害为 0（5 = 5）", defenseTieLog);
    }

    [Test]
    public void FormatBothDefense_FormatsNoDamage()
    {
        string log = BattleClashLogService.FormatBothDefense(allyPower: 4, enemyPower: 6);

        Assert.AreEqual("-> 双方均为防御技能，本次拼点不会造成伤害（4 vs 6）", log);
    }
}
