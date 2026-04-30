using System;
using System.Collections.Generic;

public static class BattleStatusTickPlanService
{
    public static bool ShouldProcessTick(BattleStatusTickResult tick)
    {
        return tick.damage > 0 || tick.speedBonus != 0 || tick.appliedStacks > 0;
    }

    public static bool ShouldPlayTickEffect(BattleStatusTickResult tick)
    {
        return tick.statusData != null && tick.damage > 0;
    }
}

public static class BattleStatusTickDispatcher
{
    public static void DispatchRoundStartTicks(
        IEnumerable<BattleStatusTickResult> ticks,
        Action<BattleStatusTickResult> resolvePresentation,
        Action<BattleStatusTickResult> playTickEffect,
        Action<BattleStatusTickResult> logDamage,
        Action<BattleStatusTickResult> logSpeed,
        Action<BattleStatusTickResult> logAppliedStatus)
    {
        if (ticks == null)
            return;

        foreach (BattleStatusTickResult tick in ticks)
        {
            if (!BattleStatusTickPlanService.ShouldProcessTick(tick))
                continue;

            if (tick.statusData != null)
            {
                resolvePresentation?.Invoke(tick);
                if (BattleStatusTickPlanService.ShouldPlayTickEffect(tick))
                    playTickEffect?.Invoke(tick);
            }

            if (tick.damage > 0)
                logDamage?.Invoke(tick);
            if (tick.speedBonus != 0)
                logSpeed?.Invoke(tick);
            if (tick.appliedStacks > 0)
                logAppliedStatus?.Invoke(tick);
        }
    }
}
