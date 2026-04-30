public struct BattleMissEffectPlan
{
    public EnemySkill_SO.DiceEffectTrigger Trigger;
}

public static class BattleMissEffectPlanService
{
    public static BattleMissEffectPlan Resolve()
    {
        return new BattleMissEffectPlan
        {
            Trigger = EnemySkill_SO.DiceEffectTrigger.OnMiss
        };
    }
}
