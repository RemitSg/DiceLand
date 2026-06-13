using NUnit.Framework;

public class RunSettlementStatsPersistenceTests
{
    [Test]
    public void RunStateClone_PreservesSettlementStatsWithoutSharingInstance()
    {
        RunState runState = new RunState();
        runState.settlementStats.runStartedUnixSeconds = 123;
        runState.settlementStats.normalBattleCoreCleared = 2;
        runState.settlementStats.totalMoneyGained = 345;
        runState.settlementStats.bossesDefeated = 1;

        RunState clone = runState.Clone();

        Assert.AreNotSame(runState.settlementStats, clone.settlementStats);
        Assert.AreEqual(123, clone.settlementStats.runStartedUnixSeconds);
        Assert.AreEqual(2, clone.settlementStats.normalBattleCoreCleared);
        Assert.AreEqual(345, clone.settlementStats.totalMoneyGained);
        Assert.AreEqual(1, clone.settlementStats.bossesDefeated);
    }

    [Test]
    public void AddMoney_CanExcludeInitialOrRefundMoneyFromSettlementStats()
    {
        RunState runState = new RunState();

        runState.AddMoney(100, countAsRunGain: false);
        runState.AddMoney(40);

        Assert.AreEqual(140, runState.money);
        Assert.AreEqual(40, runState.settlementStats.totalMoneyGained);
    }
}
