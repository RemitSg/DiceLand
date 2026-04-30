using System;

public static class BattleHitOutcomeDispatcher
{
    public static void Dispatch(
        BattleHitOutcomePlan plan,
        Action showZeroDamageFeedback,
        Action applySuccessfulHitEffects,
        Action applyMissEffects)
    {
        if (plan.ShowZeroDamageFeedback)
            showZeroDamageFeedback?.Invoke();

        if (plan.ApplySuccessfulHitEffects)
        {
            applySuccessfulHitEffects?.Invoke();
            return;
        }

        if (plan.ApplyMissEffects)
            applyMissEffects?.Invoke();
    }
}
