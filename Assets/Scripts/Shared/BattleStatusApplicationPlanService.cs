using UnityEngine;

public readonly struct BattleStatusApplicationPlan
{
    public BattleStatusApplicationPlan(bool shouldApply, bool isDelayed, int stackAmount, int delayRounds, int repeatCount)
    {
        ShouldApply = shouldApply;
        IsDelayed = isDelayed;
        StackAmount = stackAmount;
        DelayRounds = delayRounds;
        RepeatCount = repeatCount;
    }

    public bool ShouldApply { get; }
    public bool IsDelayed { get; }
    public int StackAmount { get; }
    public int DelayRounds { get; }
    public int RepeatCount { get; }
}

public static class BattleStatusApplicationPlanService
{
    public static BattleStatusApplicationPlan BuildPlan(
        EnemySkill_SO.StatusApplyTiming applyTiming,
        int stackAmount,
        int applyAfterRoundStarts,
        int applyRepeatCount)
    {
        int resolvedStacks = Mathf.Max(0, stackAmount);
        if (resolvedStacks <= 0)
            return new BattleStatusApplicationPlan(false, false, 0, 1, 1);

        bool isDelayed = applyTiming == EnemySkill_SO.StatusApplyTiming.NextRoundStart;
        return new BattleStatusApplicationPlan(
            true,
            isDelayed,
            resolvedStacks,
            Mathf.Max(1, applyAfterRoundStarts),
            Mathf.Max(1, applyRepeatCount));
    }
}
