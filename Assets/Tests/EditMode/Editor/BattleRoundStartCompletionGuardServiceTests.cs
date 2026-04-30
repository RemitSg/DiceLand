using NUnit.Framework;

public class BattleRoundStartCompletionGuardServiceTests
{
    [Test]
    public void Resolve_WhenRetreatSettlementHandled_DoesNotCheckBattleCompletion()
    {
        bool completionChecked = false;

        BattleRoundStartCompletionGuardResult result = BattleRoundStartCompletionGuardService.Resolve(
            handleRetreatSettlement: () => true,
            handleBattleCompletion: () =>
            {
                completionChecked = true;
                return false;
            });

        Assert.IsTrue(result.ShouldStopTargetingSetup);
        Assert.AreEqual(BattleRoundStartCompletionGuardReason.RetreatSettlement, result.Reason);
        Assert.IsFalse(completionChecked);
    }

    [Test]
    public void Resolve_WhenBattleCompletionHandled_StopsTargetingSetup()
    {
        BattleRoundStartCompletionGuardResult result = BattleRoundStartCompletionGuardService.Resolve(
            handleRetreatSettlement: () => false,
            handleBattleCompletion: () => true);

        Assert.IsTrue(result.ShouldStopTargetingSetup);
        Assert.AreEqual(BattleRoundStartCompletionGuardReason.BattleCompletion, result.Reason);
    }

    [Test]
    public void Resolve_WhenNoGuardHandled_AllowsTargetingSetup()
    {
        BattleRoundStartCompletionGuardResult result = BattleRoundStartCompletionGuardService.Resolve(
            handleRetreatSettlement: () => false,
            handleBattleCompletion: () => false);

        Assert.IsFalse(result.ShouldStopTargetingSetup);
        Assert.AreEqual(BattleRoundStartCompletionGuardReason.None, result.Reason);
    }
}
