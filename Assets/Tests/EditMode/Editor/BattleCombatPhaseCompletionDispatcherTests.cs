using System.Collections.Generic;
using NUnit.Framework;

public class BattleCombatPhaseCompletionDispatcherTests
{
    [Test]
    public void DispatchNormalRoundEnd_WhenPlanUnlocksInput_UnlocksBeforePromptAndPlanApplication()
    {
        BattleCombatPhaseCompletionPlan plan = BattleCombatPhaseCompletionPlanService.Resolve(false);
        List<string> calls = new List<string>();

        BattleCombatPhaseCompletionDispatcher.DispatchNormalRoundEnd(
            plan,
            () => calls.Add("unlock"),
            () => calls.Add("prompt"),
            appliedPlan => calls.Add($"apply:{appliedPlan.ReturnToReadyPhase}:{appliedPlan.AdvanceRound}"));

        CollectionAssert.AreEqual(
            new[]
            {
                "unlock",
                "prompt",
                "apply:True:True"
            },
            calls);
    }

    [Test]
    public void DispatchNormalRoundEnd_WhenPlanKeepsInputLocked_DoesNotUnlock()
    {
        BattleCombatPhaseCompletionPlan plan = BattleCombatPhaseCompletionPlanService.Resolve(true);
        List<string> calls = new List<string>();

        BattleCombatPhaseCompletionDispatcher.DispatchNormalRoundEnd(
            plan,
            () => calls.Add("unlock"),
            () => calls.Add("prompt"),
            appliedPlan => calls.Add($"apply:{appliedPlan.ReturnToReadyPhase}:{appliedPlan.AdvanceRound}"));

        CollectionAssert.AreEqual(
            new[]
            {
                "prompt",
                "apply:False:False"
            },
            calls);
    }

    [Test]
    public void DispatchNormalRoundEnd_AllowsMissingCallbacks()
    {
        Assert.DoesNotThrow(() => BattleCombatPhaseCompletionDispatcher.DispatchNormalRoundEnd(
            BattleCombatPhaseCompletionPlanService.Resolve(false),
            null,
            null,
            null));
    }

    [Test]
    public void DispatchBattleCompletedDuringAction_AppliesPlanWithoutRoundEndPromptOrUnlock()
    {
        BattleCombatPhaseCompletionPlan plan = BattleCombatPhaseCompletionPlanService.Resolve(true);
        List<string> calls = new List<string>();

        BattleCombatPhaseCompletionDispatcher.DispatchBattleCompletedDuringAction(
            plan,
            appliedPlan => calls.Add($"apply:{appliedPlan.UnlockCombatInput}:{appliedPlan.ReturnToReadyPhase}:{appliedPlan.AdvanceRound}"));

        CollectionAssert.AreEqual(
            new[]
            {
                "apply:False:False:False"
            },
            calls);
    }

    [Test]
    public void DispatchBattleCompletedDuringAction_AllowsMissingCallbacks()
    {
        Assert.DoesNotThrow(() => BattleCombatPhaseCompletionDispatcher.DispatchBattleCompletedDuringAction(
            BattleCombatPhaseCompletionPlanService.Resolve(true),
            null));
    }
}
