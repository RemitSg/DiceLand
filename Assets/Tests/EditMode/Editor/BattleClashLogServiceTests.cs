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
}
