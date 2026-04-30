using NUnit.Framework;

public class BattleStatusApplicationLogServiceTests
{
    [Test]
    public void BuildDelayedStatusLog_UsesRepeatScheduleWhenRepeatCountGreaterThanOne()
    {
        string log = BattleStatusApplicationLogService.BuildDelayedStatusLog(
            "A",
            "B",
            "点燃",
            "燃烧",
            delayRounds: 2,
            repeatCount: 3,
            stackAmount: 4);

        StringAssert.Contains("A[点燃]", log);
        StringAssert.Contains("接下来 3 个回合开始时", log);
        StringAssert.Contains("B 获得 4 层 燃烧", log);
    }

    [Test]
    public void BuildDelayedStatusLog_UsesDelayScheduleWhenSingleRepeat()
    {
        string log = BattleStatusApplicationLogService.BuildDelayedStatusLog(
            "A",
            "自己",
            "蓄火",
            "燃烧",
            delayRounds: 2,
            repeatCount: 1,
            stackAmount: 1);

        StringAssert.Contains("下 2 个回合开始时", log);
    }

    [Test]
    public void BuildImmuneStatusBlockedLog_DescribesBlockedStatus()
    {
        string log = BattleStatusApplicationLogService.BuildImmuneStatusBlockedLog(
            "A",
            "B",
            "点燃",
            "燃烧");

        StringAssert.Contains("A[点燃]", log);
        StringAssert.Contains("B 获得 燃烧", log);
        StringAssert.Contains("免疫", log);
    }
}
