using System.Collections.Generic;
using NUnit.Framework;

public class BattleImmediateEffectDispatcherTests
{
    [Test]
    public void Dispatch_RunsStepsInOrder()
    {
        List<string> calls = new List<string>();
        BattleImmediateEffectPlan plan = new BattleImmediateEffectPlan
        {
            Steps = new[]
            {
                BattleImmediateEffectStep.Damage,
                BattleImmediateEffectStep.Healing,
                BattleImmediateEffectStep.StageTransition
            }
        };

        BattleImmediateEffectDispatcher.Dispatch(
            plan,
            step => calls.Add(step.ToString()));

        CollectionAssert.AreEqual(new[] { "Damage", "Healing", "StageTransition" }, calls);
    }
}
