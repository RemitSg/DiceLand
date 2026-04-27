using NUnit.Framework;

public class BattleSingleAttackLogServiceTests
{
    [Test]
    public void FormatDefenseOnly_IncludesRemainingDiceContextAndUseTimeNote()
    {
        string log = BattleSingleAttackLogService.FormatDefenseOnly(
            1,
            "友方余骰单方面",
            "Jenny",
            "守势[防御]",
            "2+1=3");

        Assert.AreEqual("第 2 段（友方余骰单方面）：Jenny[守势[防御]](2+1=3) 为防御技能，未与攻击骰拼点，不会造成拼点/伤害结算，但【使用时】词条已生效", log);
    }

    [Test]
    public void FormatHit_IncludesOptionalContextAndTargetHp()
    {
        string log = BattleSingleAttackLogService.FormatHit(
            0,
            "敌方余骰单方面",
            "Goblin",
            "挥砍",
            "3-1=2",
            "Jenny",
            7,
            12);

        Assert.AreEqual("第 1 段（敌方余骰单方面）：Goblin[挥砍](3-1=2) -> Jenny，剩余HP 7/12", log);
    }

    [Test]
    public void FormatHit_OmitsContextForFullSingleAttack()
    {
        string log = BattleSingleAttackLogService.FormatHit(
            2,
            null,
            "Jenny",
            "突刺",
            "4+2=6",
            "Goblin",
            3,
            9);

        Assert.AreEqual("第 3 段：Jenny[突刺](4+2=6) -> Goblin，剩余HP 3/9", log);
    }
}
