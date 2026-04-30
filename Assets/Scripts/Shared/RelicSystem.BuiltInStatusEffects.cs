using UnityEngine;

public static partial class RelicSystem
{
    private static void HandleAddExtraStatusStacksOnApply(
        Relic_SO relicData,
        RunRelicState relicState,
        RelicExecutionContext context,
        Relic_SO.RelicBuiltInEffectEntry effect)
    {
        if (context.trigger != Relic_SO.RelicTrigger.OnStatusApplied || !context.sourceIsPlayer)
            return;
        if (effect.statusData != null && !StatusMatches(effect.statusData, context.statusData, context.statusId))
            return;

        context.additionalStatusStacks += Mathf.Max(0, effect.amount * Mathf.Max(1, relicState.amount));
    }

    private static int CalculateScaledStatusStacks(RunRelicState relicState, Relic_SO.RelicBuiltInEffectEntry effect)
    {
        return Mathf.Max(0, effect.amount * Mathf.Max(1, relicState.amount));
    }

    private static void ResolveRequiredRoundRange(
        Relic_SO.RelicBuiltInEffectEntry effect,
        out int requiredRoundMin,
        out int requiredRoundMax)
    {
        requiredRoundMin = Mathf.Max(1, effect.requiredRoundMin);
        requiredRoundMax = Mathf.Max(requiredRoundMin, effect.requiredRoundMax);
        if (requiredRoundMin == 1 && requiredRoundMax == 1 && effect.requiredRound != 1)
        {
            requiredRoundMin = Mathf.Max(1, effect.requiredRound);
            requiredRoundMax = requiredRoundMin;
        }
    }

    private static bool IsCurrentRoundWithinRange(
        RelicExecutionContext context,
        Relic_SO.RelicBuiltInEffectEntry effect,
        out int requiredRoundMin,
        out int requiredRoundMax)
    {
        ResolveRequiredRoundRange(effect, out requiredRoundMin, out requiredRoundMax);
        return context.currentRound >= requiredRoundMin && context.currentRound <= requiredRoundMax;
    }

    private static bool TryApplyStatus(
        BattleAllyController target,
        Relic_SO.RelicBuiltInEffectEntry effect,
        int stacks)
    {
        if (target == null || effect.statusData == null || stacks <= 0)
            return false;

        return effect.statusApplyTiming == Relic_SO.RelicStatusApplyTiming.NextRoundStart
            ? target.QueueStatusForRoundStart(
                effect.statusData,
                effect.statusData.StatusId,
                effect.statusData.DisplayName,
                stacks,
                Mathf.Max(1, effect.applyAfterRoundStarts),
                1)
            : target.AddStatus(
                effect.statusData,
                effect.statusData.StatusId,
                effect.statusData.DisplayName,
                stacks);
    }

    private static bool TryApplyStatus(
        BattleEnemyController target,
        Relic_SO.RelicBuiltInEffectEntry effect,
        int stacks)
    {
        if (target == null || effect.statusData == null || stacks <= 0)
            return false;

        return effect.statusApplyTiming == Relic_SO.RelicStatusApplyTiming.NextRoundStart
            ? target.QueueStatusForRoundStart(
                effect.statusData,
                effect.statusData.StatusId,
                effect.statusData.DisplayName,
                stacks,
                Mathf.Max(1, effect.applyAfterRoundStarts),
                1)
            : target.AddStatus(
                effect.statusData,
                effect.statusData.StatusId,
                effect.statusData.DisplayName,
                stacks);
    }
}
