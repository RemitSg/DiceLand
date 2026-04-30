using System.Collections.Generic;
using NUnit.Framework;

public class BattleAllyHitDamagePipelineDispatcherTests
{
    [Test]
    public void Dispatch_RunsOutgoingRelicsStep()
    {
        List<string> calls = new List<string>();
        BattleAllyHitDamagePipelinePlan plan = BattleAllyHitDamagePipelinePlanService.Resolve();

        int damage = BattleAllyHitDamagePipelineDispatcher.Dispatch(
            baseDamage: 6,
            plan,
            value =>
            {
                calls.Add("outgoing");
                return value + 4;
            });

        Assert.AreEqual(10, damage);
        CollectionAssert.AreEqual(new[] { "outgoing" }, calls);
    }
}
