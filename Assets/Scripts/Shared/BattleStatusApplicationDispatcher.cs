using System;

public static class BattleStatusApplicationDispatcher
{
    public static bool Execute(
        BattleStatusApplicationPlan plan,
        Func<int, int, int, bool> queueDelayed,
        Func<int, bool> addImmediate,
        Action onBlocked)
    {
        if (!plan.ShouldApply)
            return false;

        bool applied = plan.IsDelayed
            ? queueDelayed != null && queueDelayed(plan.StackAmount, plan.DelayRounds, plan.RepeatCount)
            : addImmediate != null && addImmediate(plan.StackAmount);

        if (!applied)
            onBlocked?.Invoke();

        return applied;
    }
}
