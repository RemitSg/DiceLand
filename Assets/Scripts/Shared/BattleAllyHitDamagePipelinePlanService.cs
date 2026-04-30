public enum BattleAllyHitDamagePipelineStep
{
    ApplyOutgoingRelics
}

public struct BattleAllyHitDamagePipelinePlan
{
    public BattleAllyHitDamagePipelineStep[] Steps;
}

public static class BattleAllyHitDamagePipelinePlanService
{
    public static BattleAllyHitDamagePipelinePlan Resolve()
    {
        return new BattleAllyHitDamagePipelinePlan
        {
            Steps = new[] { BattleAllyHitDamagePipelineStep.ApplyOutgoingRelics }
        };
    }
}
