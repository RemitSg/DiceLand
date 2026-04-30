using System.Collections.Generic;
using NUnit.Framework;

public class BattleTriggeredEffectDispatcherTests
{
    [Test]
    public void Dispatch_RunsStepsInOrder()
    {
        List<string> calls = new List<string>();

        BattleTriggeredEffectDispatcher.Dispatch(
            BattleTriggeredEffectPlanService.Resolve(),
            step => calls.Add(step.ToString()));

        CollectionAssert.AreEqual(
            new[] { "ImmediateEffects", "NonConditionalStatus" },
            calls);
    }
}
