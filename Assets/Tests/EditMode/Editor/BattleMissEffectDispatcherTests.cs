using System.Collections.Generic;
using NUnit.Framework;

public class BattleMissEffectDispatcherTests
{
    [Test]
    public void Dispatch_ForwardsOnMissTriggerAndDamage()
    {
        List<string> calls = new List<string>();
        BattleMissEffectPlan plan = BattleMissEffectPlanService.Resolve();

        BattleMissEffectDispatcher.Dispatch(
            plan,
            dealtDamage: -1,
            (trigger, damage) => calls.Add($"{trigger}:{damage}"));

        CollectionAssert.AreEqual(new[] { "OnMiss:-1" }, calls);
    }
}
