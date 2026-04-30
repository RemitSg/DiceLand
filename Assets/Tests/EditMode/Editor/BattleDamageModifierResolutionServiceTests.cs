using NUnit.Framework;

public class BattleDamageModifierResolutionServiceTests
{
    [Test]
    public void ResolveIncoming_UsesContextModifiersAndFeedbackPlan()
    {
        RelicExecutionContext context = new RelicExecutionContext
        {
            incomingDamageFlatModifier = 0,
            incomingDamagePercentModifier = 50
        };

        BattleDamageModifierResolution resolution = BattleDamageModifierResolutionService.ResolveIncoming(10, context);

        Assert.AreEqual(5, resolution.AdjustedDamage);
        Assert.IsTrue(resolution.FeedbackPlan.FlushRelicFeedback);
    }

    [Test]
    public void ResolveOutgoing_UsesContextModifiersAndFeedbackPlan()
    {
        RelicExecutionContext context = new RelicExecutionContext
        {
            outgoingDamageFlatModifier = 2,
            outgoingDamagePercentModifier = 50
        };

        BattleDamageModifierResolution resolution = BattleDamageModifierResolutionService.ResolveOutgoing(10, context);

        Assert.AreEqual(18, resolution.AdjustedDamage);
        Assert.IsTrue(resolution.FeedbackPlan.FlushRelicFeedback);
    }
}
