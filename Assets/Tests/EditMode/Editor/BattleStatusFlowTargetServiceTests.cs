using NUnit.Framework;

public class BattleStatusFlowTargetServiceTests
{
    [Test]
    public void ShouldConsumeStatusEffects_RequiresNonNullAndAlive()
    {
        Assert.IsFalse(BattleStatusFlowTargetService.ShouldConsumeStatusEffects<object>(null, _ => false));
        Assert.IsFalse(BattleStatusFlowTargetService.ShouldConsumeStatusEffects("dead", _ => true));
        Assert.IsTrue(BattleStatusFlowTargetService.ShouldConsumeStatusEffects("alive", _ => false));
    }

    [Test]
    public void ShouldAttemptStatusDrivenStageTransition_RequiresAvailableTargetAndNoPendingTransition()
    {
        Assert.IsFalse(BattleStatusFlowTargetService.ShouldAttemptStatusDrivenStageTransition<object>(null, _ => false, alreadyAppliedThisRoundStart: false));
        Assert.IsFalse(BattleStatusFlowTargetService.ShouldAttemptStatusDrivenStageTransition("dead", _ => true, alreadyAppliedThisRoundStart: false));
        Assert.IsFalse(BattleStatusFlowTargetService.ShouldAttemptStatusDrivenStageTransition("ready", _ => false, alreadyAppliedThisRoundStart: true));
        Assert.IsTrue(BattleStatusFlowTargetService.ShouldAttemptStatusDrivenStageTransition("ready", _ => false, alreadyAppliedThisRoundStart: false));
    }

    [Test]
    public void BuildStatusDrivenStageTransitionLog_UsesOneBasedStageNumbers()
    {
        Assert.AreEqual(
            "阶段切换：Enemy 因状态条件由第 1 阶段切换到第 3 阶段",
            BattleStatusFlowTargetService.BuildStatusDrivenStageTransitionLog("Enemy", previousStageIndex: 0, currentStageIndex: 2));
    }
}
