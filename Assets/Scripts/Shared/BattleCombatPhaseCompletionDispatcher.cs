using System;

public static class BattleCombatPhaseCompletionDispatcher
{
    public static void DispatchBattleCompletedDuringAction(
        BattleCombatPhaseCompletionPlan plan,
        Action<BattleCombatPhaseCompletionPlan> applyCompletionPlan)
    {
        applyCompletionPlan?.Invoke(plan);
    }

    public static void DispatchNormalRoundEnd(
        BattleCombatPhaseCompletionPlan plan,
        Action unlockCombatInput,
        Action logNextRoundPrompt,
        Action<BattleCombatPhaseCompletionPlan> applyCompletionPlan)
    {
        if (plan.UnlockCombatInput)
            unlockCombatInput?.Invoke();

        logNextRoundPrompt?.Invoke();
        applyCompletionPlan?.Invoke(plan);
    }
}
