using NUnit.Framework;

public class BattleTriggeredEffectPlanServiceTests
{
    [Test]
    public void Resolve_UsesImmediateThenNonConditionalStatus()
    {
        BattleTriggeredEffectPlan plan = BattleTriggeredEffectPlanService.Resolve();

        CollectionAssert.AreEqual(
            new[]
            {
                BattleTriggeredEffectStep.ImmediateEffects,
                BattleTriggeredEffectStep.NonConditionalStatus
            },
            plan.Steps);
    }
}
