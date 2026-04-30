using NUnit.Framework;

public class BattleTargetingPhaseLogServiceTests
{
    [Test]
    public void BuildPhaseHeader_IncludesLivingCounts()
    {
        string log = BattleTargetingPhaseLogService.BuildPhaseHeader(livingAllies: 2, livingEnemies: 3);

        StringAssert.Contains("2", log);
        StringAssert.Contains("3", log);
    }

    [Test]
    public void BuildAllySpeedLog_IncludesNameSpeedAndHp()
    {
        string log = BattleTargetingPhaseLogService.BuildAllySpeedLog("Jenny", speed: 5, currentHp: 7, maxHp: 10);

        StringAssert.Contains("Jenny", log);
        StringAssert.Contains("5", log);
        StringAssert.Contains("7/10", log);
    }

    [Test]
    public void BuildEnemyAutoTargetCompleteLog_ReturnsInstruction()
    {
        string log = BattleTargetingPhaseLogService.BuildEnemyAutoTargetCompleteLog();

        Assert.IsFalse(string.IsNullOrEmpty(log));
    }
}
