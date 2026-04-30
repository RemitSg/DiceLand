using NUnit.Framework;

public class BattleEnemyBuiltInStatusLogServiceTests
{
    [Test]
    public void BuildDelayedLog_FormatsSelfAndTarget()
    {
        string self = BattleEnemyBuiltInStatusLogService.BuildDelayedLog("Enemy", "Apply", null, "Burn", 2);
        string target = BattleEnemyBuiltInStatusLogService.BuildDelayedLog("Enemy", "Apply", "Ally", "Burn", 3);

        StringAssert.Contains("使 自己 在下回合开始时获得 2 层 Burn", self);
        StringAssert.Contains("使 Ally 在下回合开始时获得 3 层 Burn", target);
    }

    [Test]
    public void BuildImmediateLog_FormatsSelfAndTarget()
    {
        string self = BattleEnemyBuiltInStatusLogService.BuildImmediateLog("Enemy", "Apply", null, "Burn", 2, 5, null);
        string target = BattleEnemyBuiltInStatusLogService.BuildImmediateLog("Enemy", "Apply", "Ally", "Burn", 3, 7, "fallback");

        StringAssert.Contains("使 自己 获得 2 层 Burn，当前 Burn 5 层", self);
        StringAssert.Contains("fallback：使 Ally 获得 3 层 Burn，当前 Burn 7 层", target);
    }
}
