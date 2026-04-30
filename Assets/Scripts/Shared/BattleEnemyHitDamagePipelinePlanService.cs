public enum BattleEnemyHitDamagePipelineStep
{
    ApplyCalamityModifiers,
    ApplyIncomingAttackRelics,
    ApplyIncomingSingleAttackRelics
}

public struct BattleEnemyHitDamagePipelinePlan
{
    public BattleEnemyHitDamagePipelineStep[] Steps;
}

public static class BattleEnemyHitDamagePipelinePlanService
{
    public static BattleEnemyHitDamagePipelinePlan Resolve(bool isSingleAttack)
    {
        return new BattleEnemyHitDamagePipelinePlan
        {
            Steps = isSingleAttack
                ? new[]
                {
                    BattleEnemyHitDamagePipelineStep.ApplyCalamityModifiers,
                    BattleEnemyHitDamagePipelineStep.ApplyIncomingAttackRelics,
                    BattleEnemyHitDamagePipelineStep.ApplyIncomingSingleAttackRelics
                }
                : new[]
                {
                    BattleEnemyHitDamagePipelineStep.ApplyCalamityModifiers,
                    BattleEnemyHitDamagePipelineStep.ApplyIncomingAttackRelics
                }
        };
    }
}
