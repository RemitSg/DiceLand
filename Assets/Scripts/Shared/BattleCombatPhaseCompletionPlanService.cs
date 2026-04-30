public readonly struct BattleCombatPhaseCompletionPlan
{
    public readonly bool ClearActiveRoundCombatants;
    public readonly bool FinishResolvingCombat;
    public readonly bool UnlockCombatInput;
    public readonly bool AdvanceRound;
    public readonly bool ReturnToReadyPhase;

    public BattleCombatPhaseCompletionPlan(
        bool clearActiveRoundCombatants,
        bool finishResolvingCombat,
        bool unlockCombatInput,
        bool advanceRound,
        bool returnToReadyPhase)
    {
        ClearActiveRoundCombatants = clearActiveRoundCombatants;
        FinishResolvingCombat = finishResolvingCombat;
        UnlockCombatInput = unlockCombatInput;
        AdvanceRound = advanceRound;
        ReturnToReadyPhase = returnToReadyPhase;
    }
}

public static class BattleCombatPhaseCompletionPlanService
{
    public static BattleCombatPhaseCompletionPlan Resolve(bool battleCompletedDuringAction)
    {
        return battleCompletedDuringAction
            ? new BattleCombatPhaseCompletionPlan(
                clearActiveRoundCombatants: true,
                finishResolvingCombat: true,
                unlockCombatInput: false,
                advanceRound: false,
                returnToReadyPhase: false)
            : new BattleCombatPhaseCompletionPlan(
                clearActiveRoundCombatants: true,
                finishResolvingCombat: true,
                unlockCombatInput: true,
                advanceRound: true,
                returnToReadyPhase: true);
    }
}
