using NUnit.Framework;

public class BattleEnemyHitDamagePipelinePlanServiceTests
{
    [Test]
    public void Resolve_ClashHit_UsesCalamityThenIncomingAttackRelics()
    {
        BattleEnemyHitDamagePipelinePlan plan = BattleEnemyHitDamagePipelinePlanService.Resolve(isSingleAttack: false);

        CollectionAssert.AreEqual(
            new[]
            {
                BattleEnemyHitDamagePipelineStep.ApplyCalamityModifiers,
                BattleEnemyHitDamagePipelineStep.ApplyIncomingAttackRelics
            },
            plan.Steps);
    }

    [Test]
    public void Resolve_SingleAttack_AddsIncomingSingleAttackRelicsLast()
    {
        BattleEnemyHitDamagePipelinePlan plan = BattleEnemyHitDamagePipelinePlanService.Resolve(isSingleAttack: true);

        CollectionAssert.AreEqual(
            new[]
            {
                BattleEnemyHitDamagePipelineStep.ApplyCalamityModifiers,
                BattleEnemyHitDamagePipelineStep.ApplyIncomingAttackRelics,
                BattleEnemyHitDamagePipelineStep.ApplyIncomingSingleAttackRelics
            },
            plan.Steps);
    }
}
