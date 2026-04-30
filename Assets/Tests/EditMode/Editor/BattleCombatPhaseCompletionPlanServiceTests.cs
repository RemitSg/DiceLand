using NUnit.Framework;

public class BattleCombatPhaseCompletionPlanServiceTests
{
    [Test]
    public void Resolve_WhenBattleCompletedDuringAction_KeepsCombatLockedAndDoesNotAdvanceRound()
    {
        BattleCombatPhaseCompletionPlan plan =
            BattleCombatPhaseCompletionPlanService.Resolve(battleCompletedDuringAction: true);

        Assert.IsTrue(plan.ClearActiveRoundCombatants);
        Assert.IsTrue(plan.FinishResolvingCombat);
        Assert.IsFalse(plan.UnlockCombatInput);
        Assert.IsFalse(plan.AdvanceRound);
        Assert.IsFalse(plan.ReturnToReadyPhase);
    }

    [Test]
    public void Resolve_WhenRoundEndsNormally_UnlocksCombatAndAdvancesRound()
    {
        BattleCombatPhaseCompletionPlan plan =
            BattleCombatPhaseCompletionPlanService.Resolve(battleCompletedDuringAction: false);

        Assert.IsTrue(plan.ClearActiveRoundCombatants);
        Assert.IsTrue(plan.FinishResolvingCombat);
        Assert.IsTrue(plan.UnlockCombatInput);
        Assert.IsTrue(plan.AdvanceRound);
        Assert.IsTrue(plan.ReturnToReadyPhase);
    }
}
