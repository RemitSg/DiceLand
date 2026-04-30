using System;

public enum BattleRoundStartCompletionGuardReason
{
    None = 0,
    RetreatSettlement = 1,
    BattleCompletion = 2
}

public readonly struct BattleRoundStartCompletionGuardResult
{
    public BattleRoundStartCompletionGuardResult(BattleRoundStartCompletionGuardReason reason)
    {
        Reason = reason;
    }

    public BattleRoundStartCompletionGuardReason Reason { get; }
    public bool ShouldStopTargetingSetup => Reason != BattleRoundStartCompletionGuardReason.None;
}

public static class BattleRoundStartCompletionGuardService
{
    public static BattleRoundStartCompletionGuardResult Resolve(
        Func<bool> handleRetreatSettlement,
        Func<bool> handleBattleCompletion)
    {
        if (handleRetreatSettlement != null && handleRetreatSettlement())
            return new BattleRoundStartCompletionGuardResult(BattleRoundStartCompletionGuardReason.RetreatSettlement);

        if (handleBattleCompletion != null && handleBattleCompletion())
            return new BattleRoundStartCompletionGuardResult(BattleRoundStartCompletionGuardReason.BattleCompletion);

        return new BattleRoundStartCompletionGuardResult(BattleRoundStartCompletionGuardReason.None);
    }
}
