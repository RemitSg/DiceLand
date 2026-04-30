using NUnit.Framework;

public class BattleHitOutcomePlanServiceTests
{
    [Test]
    public void Resolve_PositiveDamage_DispatchesSuccessfulHitOnly()
    {
        BattleHitOutcomePlan plan = BattleHitOutcomePlanService.Resolve(dealtDamage: 3);

        Assert.IsFalse(plan.ShowZeroDamageFeedback);
        Assert.IsTrue(plan.ApplySuccessfulHitEffects);
        Assert.IsFalse(plan.ApplyMissEffects);
    }

    [Test]
    public void Resolve_ZeroDamage_ShowsFeedbackAndAppliesMissEffects()
    {
        BattleHitOutcomePlan plan = BattleHitOutcomePlanService.Resolve(dealtDamage: 0);

        Assert.IsTrue(plan.ShowZeroDamageFeedback);
        Assert.IsFalse(plan.ApplySuccessfulHitEffects);
        Assert.IsTrue(plan.ApplyMissEffects);
    }

    [Test]
    public void Resolve_NegativeDamage_ShowsFeedbackAndAppliesMissEffects()
    {
        BattleHitOutcomePlan plan = BattleHitOutcomePlanService.Resolve(dealtDamage: -2);

        Assert.IsTrue(plan.ShowZeroDamageFeedback);
        Assert.IsFalse(plan.ApplySuccessfulHitEffects);
        Assert.IsTrue(plan.ApplyMissEffects);
    }
}
