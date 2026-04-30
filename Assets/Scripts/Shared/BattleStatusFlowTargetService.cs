using System;

public static class BattleStatusFlowTargetService
{
    public static bool ShouldConsumeStatusEffects<T>(T target, Func<T, bool> isDead)
    {
        return target != null && !(isDead != null && isDead(target));
    }

    public static bool ShouldAttemptStatusDrivenStageTransition<T>(T target, Func<T, bool> isDead, bool alreadyAppliedThisRoundStart)
    {
        return ShouldConsumeStatusEffects(target, isDead) && !alreadyAppliedThisRoundStart;
    }

    public static string BuildStatusDrivenStageTransitionLog(string displayName, int previousStageIndex, int currentStageIndex)
    {
        return $"阶段切换：{displayName} 因状态条件由第 {previousStageIndex + 1} 阶段切换到第 {currentStageIndex + 1} 阶段";
    }
}
