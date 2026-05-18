using System.Collections.Generic;
using NUnit.Framework;

public class BattleHitOutcomeDispatcherTests
{
    [Test]
    public void Dispatch_PositiveDamage_RunsSuccessfulHitOnly()
    {
        List<string> calls = new List<string>();
        BattleHitOutcomePlan plan = BattleHitOutcomePlanService.Resolve(dealtDamage: 4, hitConfirmed: true);

        BattleHitOutcomeDispatcher.Dispatch(
            plan,
            () => calls.Add("zero-feedback"),
            () => calls.Add("successful-hit"),
            () => calls.Add("miss"));

        CollectionAssert.AreEqual(new[] { "successful-hit" }, calls);
    }

    [Test]
    public void Dispatch_ConfirmedZeroDamage_RunsFeedbackThenSuccessfulHit()
    {
        List<string> calls = new List<string>();
        BattleHitOutcomePlan plan = BattleHitOutcomePlanService.Resolve(dealtDamage: 0, hitConfirmed: true);

        BattleHitOutcomeDispatcher.Dispatch(
            plan,
            () => calls.Add("zero-feedback"),
            () => calls.Add("successful-hit"),
            () => calls.Add("miss"));

        CollectionAssert.AreEqual(new[] { "zero-feedback", "successful-hit" }, calls);
    }
}
