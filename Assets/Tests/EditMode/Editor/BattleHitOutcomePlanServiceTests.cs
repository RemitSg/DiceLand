using NUnit.Framework;

public class BattleHitOutcomePlanServiceTests
{
    [Test]
    public void Resolve_PositiveDamage_DispatchesSuccessfulHitOnly()
    {
        BattleHitOutcomePlan plan = BattleHitOutcomePlanService.Resolve(dealtDamage: 3, hitConfirmed: true);

        Assert.IsFalse(plan.ShowZeroDamageFeedback);
        Assert.IsTrue(plan.ApplySuccessfulHitEffects);
        Assert.IsFalse(plan.ApplyMissEffects);
    }

    [Test]
    public void Resolve_ConfirmedHitWithZeroDamage_ShowsFeedbackAndAppliesSuccessfulHitEffects()
    {
        BattleHitOutcomePlan plan = BattleHitOutcomePlanService.Resolve(dealtDamage: 0, hitConfirmed: true);

        Assert.IsTrue(plan.ShowZeroDamageFeedback);
        Assert.IsTrue(plan.ApplySuccessfulHitEffects);
        Assert.IsFalse(plan.ApplyMissEffects);
    }

    [Test]
    public void Resolve_UnconfirmedHitWithZeroDamage_ShowsFeedbackAndAppliesMissEffects()
    {
        BattleHitOutcomePlan plan = BattleHitOutcomePlanService.Resolve(dealtDamage: 0, hitConfirmed: false);

        Assert.IsTrue(plan.ShowZeroDamageFeedback);
        Assert.IsFalse(plan.ApplySuccessfulHitEffects);
        Assert.IsTrue(plan.ApplyMissEffects);
    }

    [Test]
    public void Resolve_NegativeDamage_ShowsFeedbackAndAppliesMissEffects()
    {
        BattleHitOutcomePlan plan = BattleHitOutcomePlanService.Resolve(dealtDamage: -2, hitConfirmed: false);

        Assert.IsTrue(plan.ShowZeroDamageFeedback);
        Assert.IsFalse(plan.ApplySuccessfulHitEffects);
        Assert.IsTrue(plan.ApplyMissEffects);
    }
}
