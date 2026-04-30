using NUnit.Framework;

public class BattleImmediateEffectPlanServiceTests
{
    [Test]
    public void ResolveAlly_UsesCurrentAllyImmediateOrder()
    {
        BattleImmediateEffectPlan plan = BattleImmediateEffectPlanService.ResolveAlly();

        CollectionAssert.AreEqual(
            new[]
            {
                BattleImmediateEffectStep.Damage,
                BattleImmediateEffectStep.StatusBurst,
                BattleImmediateEffectStep.Healing,
                BattleImmediateEffectStep.ConditionalStatus,
                BattleImmediateEffectStep.StatusStackOperation
            },
            plan.Steps);
    }

    [Test]
    public void ResolveEnemy_UsesCurrentEnemyImmediateOrder()
    {
        BattleImmediateEffectPlan plan = BattleImmediateEffectPlanService.ResolveEnemy();

        CollectionAssert.AreEqual(
            new[]
            {
                BattleImmediateEffectStep.Damage,
                BattleImmediateEffectStep.StatusBurst,
                BattleImmediateEffectStep.Healing,
                BattleImmediateEffectStep.BuiltIn,
                BattleImmediateEffectStep.ConditionalStatus,
                BattleImmediateEffectStep.StatusStackOperation,
                BattleImmediateEffectStep.Summon,
                BattleImmediateEffectStep.StageTransition
            },
            plan.Steps);
    }
}
