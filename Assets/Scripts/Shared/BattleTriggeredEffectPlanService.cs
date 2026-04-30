public enum BattleTriggeredEffectStep
{
    ImmediateEffects,
    NonConditionalStatus
}

public struct BattleTriggeredEffectPlan
{
    public BattleTriggeredEffectStep[] Steps;
}

public static class BattleTriggeredEffectPlanService
{
    public static BattleTriggeredEffectPlan Resolve()
    {
        return new BattleTriggeredEffectPlan
        {
            Steps = new[]
            {
                BattleTriggeredEffectStep.ImmediateEffects,
                BattleTriggeredEffectStep.NonConditionalStatus
            }
        };
    }
}
