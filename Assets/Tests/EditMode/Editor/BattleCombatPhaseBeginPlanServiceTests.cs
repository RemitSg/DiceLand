using NUnit.Framework;

public class BattleCombatPhaseBeginPlanServiceTests
{
    [Test]
    public void Resolve_AlwaysLocksInputAndHidesCombatSelectionUi()
    {
        BattleCombatPhaseBeginPlan plan = BattleCombatPhaseBeginPlanService.Resolve();

        Assert.IsTrue(plan.MarkResolvingCombat);
        Assert.IsTrue(plan.CommitReservedEnergy);
        Assert.IsTrue(plan.LockCombatInput);
        Assert.IsTrue(plan.HideAllySpeedButtons);
        Assert.IsTrue(plan.DisableAllySpeedButtons);
        Assert.IsTrue(plan.HideEnemySpeedButtons);
        Assert.IsTrue(plan.HideSkillInfoOverlay);
    }
}
