using UnityEngine;

public static partial class RelicSystem
{
    private static void HandleApplyStatusToAttackerAfterTakingSingleAttackDamageOncePerRound(
        Relic_SO relicData,
        RunRelicState relicState,
        RelicExecutionContext context,
        Relic_SO.RelicBuiltInEffectEntry effect)
    {
        if (context.trigger != Relic_SO.RelicTrigger.OnPlayerTookSingleAttackDamage)
            return;

        BattleEnemyController singleAttackAttacker = context.source as BattleEnemyController;
        if (singleAttackAttacker == null || effect.statusData == null || context.resolvedDamage <= 0)
            return;
        if (!TryRegisterRoundScopedProc(relicData, $"single-attack-status:{context.currentRound}"))
            return;

        int attackerStatusStacks = CalculateScaledStatusStacks(relicState, effect);
        if (attackerStatusStacks <= 0)
            return;

        bool attackerStatusApplied = TryApplyStatus(singleAttackAttacker, effect, attackerStatusStacks);
        if (attackerStatusApplied)
            AddRelicFeedback(context, relicData, $"{singleAttackAttacker.GetDebugDisplayName()} 获得 {attackerStatusStacks} 层 {GetStatusDisplayName(effect.statusData)}。");
    }

    private static void HandleChanceToQueueStatusOnHitWhenSlowerOncePerSkillUse(
        Relic_SO relicData,
        RunRelicState relicState,
        RelicExecutionContext context,
        Relic_SO.RelicBuiltInEffectEntry effect)
    {
        if (context.trigger != Relic_SO.RelicTrigger.OnPlayerHitEnemy)
            return;

        BattleAllyController slowerHitSource = context.source as BattleAllyController;
        BattleEnemyController slowerHitTarget = context.target as BattleEnemyController;
        if (slowerHitSource == null || slowerHitTarget == null || effect.statusData == null || context.resolvedDamage <= 0)
            return;
        if (context.incomingSourceSpeed >= context.incomingTargetSpeed)
            return;
        if (!TryRegisterRoundScopedProc(relicData, $"slower-hit:{context.eventInstanceKey}"))
            return;

        int slowerHitStacks = CalculateScaledStatusStacks(relicState, effect);
        if (slowerHitStacks <= 0)
            return;

        bool slowerStatusApplied = effect.statusApplyTiming == Relic_SO.RelicStatusApplyTiming.Immediate
            ? slowerHitTarget.AddStatus(
                effect.statusData,
                effect.statusData.StatusId,
                effect.statusData.DisplayName,
                slowerHitStacks)
            : slowerHitTarget.QueueStatusForRoundStart(
                effect.statusData,
                effect.statusData.StatusId,
                effect.statusData.DisplayName,
                slowerHitStacks,
                Mathf.Max(1, effect.applyAfterRoundStarts),
                1);

        if (slowerStatusApplied)
            AddRelicFeedback(context, relicData, $"{slowerHitTarget.GetDebugDisplayName()} 将获得 {slowerHitStacks} 层 {GetStatusDisplayName(effect.statusData)}。");
    }

    private static void HandleApplyStatusToSelfOnDefenseClashWin(
        Relic_SO relicData,
        RunRelicState relicState,
        RelicExecutionContext context,
        Relic_SO.RelicBuiltInEffectEntry effect)
    {
        if (context.trigger != Relic_SO.RelicTrigger.OnPlayerDefenseClashWin)
            return;

        BattleAllyController defenseWinSource = context.source as BattleAllyController;
        if (defenseWinSource == null || effect.statusData == null)
            return;

        int defenseWinStacks = CalculateScaledStatusStacks(relicState, effect);
        if (defenseWinStacks <= 0)
            return;

        bool defenseWinApplied = TryApplyStatus(defenseWinSource, effect, defenseWinStacks);
        if (defenseWinApplied)
            AddRelicFeedback(context, relicData, $"{defenseWinSource.GetDebugDisplayName()} 获得 {defenseWinStacks} 层 {GetStatusDisplayName(effect.statusData)}。");
    }

    private static void HandleApplyStatusToSelfOnEnemyKilledByPlayer(
        Relic_SO relicData,
        RunRelicState relicState,
        RelicExecutionContext context,
        Relic_SO.RelicBuiltInEffectEntry effect)
    {
        if (context.trigger != Relic_SO.RelicTrigger.OnEnemyKilledByPlayer)
            return;

        BattleAllyController killSource = context.source as BattleAllyController;
        if (killSource == null || effect.statusData == null)
            return;

        int killStacks = CalculateScaledStatusStacks(relicState, effect);
        if (killStacks <= 0)
            return;

        if (!killSource.AddStatus(effect.statusData, effect.statusData.StatusId, effect.statusData.DisplayName, killStacks))
            return;

        AddRelicFeedback(context, relicData, $"{killSource.GetDebugDisplayName()} 获得 {killStacks} 层 {GetStatusDisplayName(effect.statusData)}。");
    }
}
