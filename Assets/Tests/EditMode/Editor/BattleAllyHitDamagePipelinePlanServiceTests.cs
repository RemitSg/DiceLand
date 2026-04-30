using NUnit.Framework;

public class BattleAllyHitDamagePipelinePlanServiceTests
{
    [Test]
    public void Resolve_UsesOutgoingRelicsStep()
    {
        BattleAllyHitDamagePipelinePlan plan = BattleAllyHitDamagePipelinePlanService.Resolve();

        CollectionAssert.AreEqual(
            new[] { BattleAllyHitDamagePipelineStep.ApplyOutgoingRelics },
            plan.Steps);
    }
}
