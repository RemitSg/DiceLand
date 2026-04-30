using NUnit.Framework;

public class BattleStatusTickLogServiceTests
{
    [Test]
    public void BuildDamageLog_IncludesHpAndStacks()
    {
        string log = BattleStatusTickLogService.BuildDamageLog("A", "燃烧", damage: 3, stacks: 4, currentHP: 5, maxHP: 9);

        StringAssert.Contains("A 受到 3 点 燃烧 伤害", log);
        StringAssert.Contains("当前 燃烧 4 层", log);
        StringAssert.Contains("剩余HP 5/9", log);
    }

    [Test]
    public void BuildSpeedLog_HandlesPositiveAndNegativeSpeedBonus()
    {
        string positive = BattleStatusTickLogService.BuildSpeedLog("A", "迅捷", speedBonus: 2, stacks: 1);
        string negative = BattleStatusTickLogService.BuildSpeedLog("A", "迟缓", speedBonus: -3, stacks: 2);

        StringAssert.Contains("获得 2 点 迅捷 速度提升", positive);
        StringAssert.Contains("受到 3 点 迟缓 速度降低", negative);
    }

    [Test]
    public void BuildAppliedStatusLog_IncludesAppliedStatusName()
    {
        string log = BattleStatusTickLogService.BuildAppliedStatusLog("A", "燃烧", 2);

        StringAssert.Contains("A 获得 2 层 燃烧", log);
    }
}
