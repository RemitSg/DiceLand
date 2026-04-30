public enum BattleImmediateEffectStep
{
    Damage,
    StatusBurst,
    Healing,
    BuiltIn,
    ConditionalStatus,
    StatusStackOperation,
    Summon,
    StageTransition
}

public struct BattleImmediateEffectPlan
{
    public BattleImmediateEffectStep[] Steps;
}

public static class BattleImmediateEffectPlanService
{
    public static BattleImmediateEffectPlan ResolveAlly()
    {
        return new BattleImmediateEffectPlan
        {
            Steps = new[]
            {
                BattleImmediateEffectStep.Damage,
                BattleImmediateEffectStep.StatusBurst,
                BattleImmediateEffectStep.Healing,
                BattleImmediateEffectStep.ConditionalStatus,
                BattleImmediateEffectStep.StatusStackOperation
            }
        };
    }

    public static BattleImmediateEffectPlan ResolveEnemy()
    {
        return new BattleImmediateEffectPlan
        {
            Steps = new[]
            {
                BattleImmediateEffectStep.Damage,
                BattleImmediateEffectStep.StatusBurst,
                BattleImmediateEffectStep.Healing,
                BattleImmediateEffectStep.BuiltIn,
                BattleImmediateEffectStep.ConditionalStatus,
                BattleImmediateEffectStep.StatusStackOperation,
                BattleImmediateEffectStep.Summon,
                BattleImmediateEffectStep.StageTransition
            }
        };
    }
}
