using NUnit.Framework;

public class BattleMissEffectPlanServiceTests
{
    [Test]
    public void Resolve_UsesOnMissTrigger()
    {
        BattleMissEffectPlan plan = BattleMissEffectPlanService.Resolve();

        Assert.AreEqual(EnemySkill_SO.DiceEffectTrigger.OnMiss, plan.Trigger);
    }
}
