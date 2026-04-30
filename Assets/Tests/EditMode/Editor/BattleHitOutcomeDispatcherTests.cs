using System.Collections.Generic;
using NUnit.Framework;

public class BattleHitOutcomeDispatcherTests
{
    [Test]
    public void Dispatch_PositiveDamage_RunsSuccessfulHitOnly()
    {
        List<string> calls = new List<string>();
        BattleHitOutcomePlan plan = BattleHitOutcomePlanService.Resolve(dealtDamage: 4);

        BattleHitOutcomeDispatcher.Dispatch(
            plan,
            () => calls.Add("zero-feedback"),
            () => calls.Add("successful-hit"),
            () => calls.Add("miss"));

        CollectionAssert.AreEqual(new[] { "successful-hit" }, calls);
    }

    [Test]
    public void Dispatch_ZeroDamage_RunsFeedbackThenMiss()
    {
        List<string> calls = new List<string>();
        BattleHitOutcomePlan plan = BattleHitOutcomePlanService.Resolve(dealtDamage: 0);

        BattleHitOutcomeDispatcher.Dispatch(
            plan,
            () => calls.Add("zero-feedback"),
            () => calls.Add("successful-hit"),
            () => calls.Add("miss"));

        CollectionAssert.AreEqual(new[] { "zero-feedback", "miss" }, calls);
    }
}
