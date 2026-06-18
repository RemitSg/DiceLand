using NUnit.Framework;

public class RunSettlementFlowServiceTests
{
    [SetUp]
    public void SetUp()
    {
        RunSettlementRuntime.Clear();
    }

    [TearDown]
    public void TearDown()
    {
        RunSettlementRuntime.Clear();
    }

    [Test]
    public void ExecuteSettlement_AwardsAbilityPointsAndStoresRuntimeData()
    {
        int walletPoints = 2;
        bool deletedSave = false;
        bool clearedRuntime = false;
        string transitionedScene = null;
        RunState runState = new RunState();
        runState.settlementStats.runStartedUnixSeconds = 0;
        runState.settlementStats.bossesDefeated = 1;

        RunSettlementFlowService.ExecuteSettlement(new RunSettlementFlowRequest
        {
            runState = runState,
            runCleared = true,
            settlementUnixSeconds = 3600,
            settlementSceneName = "RunSettlementScene",
            getAbilityPoints = () => walletPoints,
            addAbilityPoints = amount =>
            {
                walletPoints += amount;
                return walletPoints;
            },
            deleteSavedRun = () => deletedSave = true,
            clearRuntimeState = () => clearedRuntime = true,
            transitionToScene = scene => transitionedScene = scene
        });

        Assert.AreEqual("RunSettlementScene", transitionedScene);
        Assert.IsTrue(deletedSave);
        Assert.IsTrue(clearedRuntime);
        Assert.IsTrue(RunSettlementRuntime.HasData);
        Assert.AreEqual(1, RunSettlementRuntime.Current.gainedAbilityPoints);
        Assert.AreEqual(2, RunSettlementRuntime.Current.abilityPointsBefore);
        Assert.AreEqual(3, RunSettlementRuntime.Current.abilityPointsAfter);
    }

    [Test]
    public void ExecuteSettlement_DoesNotAwardTwiceForSameRuntimeData()
    {
        int walletPoints = 0;
        RunState runState = new RunState();
        runState.settlementStats.runStartedUnixSeconds = 0;
        runState.settlementStats.bossesDefeated = 1;

        RunSettlementFlowRequest request = new RunSettlementFlowRequest
        {
            runState = runState,
            runCleared = true,
            settlementUnixSeconds = 3600,
            getAbilityPoints = () => walletPoints,
            addAbilityPoints = amount =>
            {
                walletPoints += amount;
                return walletPoints;
            }
        };

        RunSettlementFlowService.ExecuteSettlement(request);
        RunSettlementFlowService.ExecuteSettlement(request);

        Assert.AreEqual(1, walletPoints);
    }
}
