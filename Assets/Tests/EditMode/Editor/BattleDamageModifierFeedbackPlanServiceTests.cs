using NUnit.Framework;

public class BattleDamageModifierFeedbackPlanServiceTests
{
    [Test]
    public void ResolveIncoming_ShowsFeedbackOnlyWhenDamageWasReduced()
    {
        Assert.IsTrue(BattleDamageModifierFeedbackPlanService.ResolveIncoming(originalDamage: 10, adjustedDamage: 8).FlushRelicFeedback);
        Assert.IsFalse(BattleDamageModifierFeedbackPlanService.ResolveIncoming(originalDamage: 10, adjustedDamage: 10).FlushRelicFeedback);
        Assert.IsFalse(BattleDamageModifierFeedbackPlanService.ResolveIncoming(originalDamage: 10, adjustedDamage: 12).FlushRelicFeedback);
    }

    [Test]
    public void ResolveOutgoing_ShowsFeedbackOnlyWhenDamageWasIncreased()
    {
        Assert.IsTrue(BattleDamageModifierFeedbackPlanService.ResolveOutgoing(originalDamage: 10, adjustedDamage: 12).FlushRelicFeedback);
        Assert.IsFalse(BattleDamageModifierFeedbackPlanService.ResolveOutgoing(originalDamage: 10, adjustedDamage: 10).FlushRelicFeedback);
        Assert.IsFalse(BattleDamageModifierFeedbackPlanService.ResolveOutgoing(originalDamage: 10, adjustedDamage: 8).FlushRelicFeedback);
    }
}
