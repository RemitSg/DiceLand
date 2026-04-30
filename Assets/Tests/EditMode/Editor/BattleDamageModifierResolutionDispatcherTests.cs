using System.Collections.Generic;
using NUnit.Framework;

public class BattleDamageModifierResolutionDispatcherTests
{
    [Test]
    public void ResolveIncomingAndDispatchFeedback_FlushesWhenPlanRequires()
    {
        List<string> calls = new List<string>();
        RelicExecutionContext context = new RelicExecutionContext
        {
            incomingDamagePercentModifier = 50
        };

        int damage = BattleDamageModifierResolutionDispatcher.ResolveIncomingAndDispatchFeedback(
            originalDamage: 10,
            context,
            _ => calls.Add("flush"));

        Assert.AreEqual(5, damage);
        CollectionAssert.AreEqual(new[] { "flush" }, calls);
    }

    [Test]
    public void ResolveOutgoingAndDispatchFeedback_SkipsFlushWhenDamageUnchanged()
    {
        List<string> calls = new List<string>();
        RelicExecutionContext context = new RelicExecutionContext();

        int damage = BattleDamageModifierResolutionDispatcher.ResolveOutgoingAndDispatchFeedback(
            originalDamage: 10,
            context,
            _ => calls.Add("flush"));

        Assert.AreEqual(10, damage);
        Assert.IsEmpty(calls);
    }
}
