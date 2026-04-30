using System;

public static class BattleImmediateEffectDispatcher
{
    public static void Dispatch(BattleImmediateEffectPlan plan, Action<BattleImmediateEffectStep> applyStep)
    {
        for (int i = 0; plan.Steps != null && i < plan.Steps.Length; i++)
            applyStep?.Invoke(plan.Steps[i]);
    }
}
