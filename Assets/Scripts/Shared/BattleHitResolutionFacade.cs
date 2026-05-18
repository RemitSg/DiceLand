using System;

public static class BattleHitResolutionFacade
{
    public static void Resolve(
        int dealtDamage,
        bool hitConfirmed,
        Action onZeroDamage,
        Action onSuccessfulHit,
        Action onMiss)
    {
        BattleHitOutcomePlan outcomePlan = BattleHitOutcomePlanService.Resolve(dealtDamage, hitConfirmed);
        BattleHitOutcomeDispatcher.Dispatch(
            outcomePlan,
            onZeroDamage,
            onSuccessfulHit,
            onMiss);
    }
}
