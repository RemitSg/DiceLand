using NUnit.Framework;

public class BattleStatusApplicationPlanServiceTests
{
    [Test]
    public void BuildPlan_ReturnsInactiveWhenStacksAreZero()
    {
        BattleStatusApplicationPlan plan = BattleStatusApplicationPlanService.BuildPlan(
            EnemySkill_SO.StatusApplyTiming.Immediate,
            stackAmount: 0,
            applyAfterRoundStarts: 0,
            applyRepeatCount: 0);

        Assert.IsFalse(plan.ShouldApply);
    }

    [Test]
    public void BuildPlan_ClampsDelayAndRepeatForDelayedStatus()
    {
        BattleStatusApplicationPlan plan = BattleStatusApplicationPlanService.BuildPlan(
            EnemySkill_SO.StatusApplyTiming.NextRoundStart,
            stackAmount: 2,
            applyAfterRoundStarts: 0,
            applyRepeatCount: 0);

        Assert.IsTrue(plan.ShouldApply);
        Assert.IsTrue(plan.IsDelayed);
        Assert.AreEqual(2, plan.StackAmount);
        Assert.AreEqual(1, plan.DelayRounds);
        Assert.AreEqual(1, plan.RepeatCount);
    }

    [Test]
    public void BuildPlan_PreservesImmediateStackAmount()
    {
        BattleStatusApplicationPlan plan = BattleStatusApplicationPlanService.BuildPlan(
            EnemySkill_SO.StatusApplyTiming.Immediate,
            stackAmount: 3,
            applyAfterRoundStarts: 4,
            applyRepeatCount: 5);

        Assert.IsTrue(plan.ShouldApply);
        Assert.IsFalse(plan.IsDelayed);
        Assert.AreEqual(3, plan.StackAmount);
    }
}
