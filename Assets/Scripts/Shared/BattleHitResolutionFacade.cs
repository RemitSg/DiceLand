using System;

public static class BattleHitResolutionFacade
{
    public static void Resolve(
        int dealtDamage,
        Action onZeroDamage,
        Action onSuccessfulHit,
        Action onMiss)
    {
        BattleHitOutcomePlan outcomePlan = BattleHitOutcomePlanService.Resolve(dealtDamage);
        BattleHitOutcomeDispatcher.Dispatch(
            outcomePlan,
            onZeroDamage,
            onSuccessfulHit,
            onMiss);
    }
}
