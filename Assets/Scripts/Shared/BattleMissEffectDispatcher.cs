using System;

public static class BattleMissEffectDispatcher
{
    public static void Dispatch(
        BattleMissEffectPlan plan,
        int dealtDamage,
        Action<EnemySkill_SO.DiceEffectTrigger, int> applyTriggeredEffects)
    {
        applyTriggeredEffects?.Invoke(plan.Trigger, dealtDamage);
    }
}
