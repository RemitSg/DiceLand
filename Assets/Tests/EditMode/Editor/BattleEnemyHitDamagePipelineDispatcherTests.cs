using System.Collections.Generic;
using NUnit.Framework;

public class BattleEnemyHitDamagePipelineDispatcherTests
{
    [Test]
    public void Dispatch_RunsStepsInPlanOrderAndCarriesDamageForward()
    {
        List<string> calls = new List<string>();
        BattleEnemyHitDamagePipelinePlan plan = BattleEnemyHitDamagePipelinePlanService.Resolve(isSingleAttack: true);

        int damage = BattleEnemyHitDamagePipelineDispatcher.Dispatch(
            baseDamage: 5,
            plan,
            value =>
            {
                calls.Add("calamity");
                return value + 1;
            },
            value =>
            {
                calls.Add("incoming");
                return value + 2;
            },
            value =>
            {
                calls.Add("single");
                return value + 3;
            });

        Assert.AreEqual(11, damage);
        CollectionAssert.AreEqual(new[] { "calamity", "incoming", "single" }, calls);
    }
}
