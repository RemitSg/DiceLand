public struct BattleStatusEffectApplicationPlan
{
    public BattleStatus_SO StatusData;
    public string StatusId;
    public string StatusName;
    public BattleStatusApplicationPlan ApplicationPlan;
}

public static class BattleStatusEffectApplicationService
{
    public static bool TryBuildPlan(
        EnemySkill_SO.StatusEffectEntry effect,
        int adjustedStacks,
        out BattleStatusEffectApplicationPlan result)
    {
        result = default;
        if (!BattleStatusResolutionService.TryResolve(effect.statusData, effect.statusType, out BattleResolvedStatus resolvedStatus))
            return false;

        result.StatusData = resolvedStatus.StatusData;
        result.StatusId = resolvedStatus.StatusId;
        result.StatusName = resolvedStatus.StatusName;
        result.ApplicationPlan = BattleStatusApplicationPlanService.BuildPlan(
            effect.applyTiming,
            adjustedStacks,
            effect.applyAfterRoundStarts,
            effect.applyRepeatCount);
        return result.ApplicationPlan.ShouldApply;
    }

    public static string BuildImmediateAppliedLog(
        string attackerName,
        string targetName,
        string effectLabel,
        string statusName,
        int appliedStacks,
        int currentStacks)
    {
        return $"词条触发：{attackerName}[{effectLabel}] 使 {targetName} 获得 {appliedStacks} 层 {statusName}，当前 {statusName} {currentStacks} 层";
    }
}
