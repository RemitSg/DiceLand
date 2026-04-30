using System;

public static class BattleTriggeredEffectDispatcher
{
    public static void Dispatch(BattleTriggeredEffectPlan plan, Action<BattleTriggeredEffectStep> applyStep)
    {
        for (int i = 0; plan.Steps != null && i < plan.Steps.Length; i++)
            applyStep?.Invoke(plan.Steps[i]);
    }
}
