using UnityEngine;

public static partial class RelicSystem
{
    private static void HandleApplyStatusToRemainingAlliesOnPlayerDeath(
        Relic_SO relicData,
        RunRelicState relicState,
        RelicExecutionContext context,
        Relic_SO.RelicBuiltInEffectEntry effect)
    {
        if (context.trigger != Relic_SO.RelicTrigger.OnPlayerDeath)
            return;
        if (effect.statusData == null || context.allies == null || context.allies.Length == 0)
            return;

        BattleAllyController defeatedAlly = context.target as BattleAllyController;
        int allyDeathStacksToApply = CalculateScaledStatusStacks(relicState, effect);
        if (allyDeathStacksToApply <= 0)
            return;

        int affectedAllyCount = 0;
        for (int allyIndex = 0; allyIndex < context.allies.Length; allyIndex++)
        {
            BattleAllyController allyTarget = context.allies[allyIndex];
            if (allyTarget == null || allyTarget.IsDead || allyTarget == defeatedAlly)
                continue;

            if (TryApplyStatus(allyTarget, effect, allyDeathStacksToApply))
                affectedAllyCount += 1;
        }

        if (affectedAllyCount <= 0)
            return;

        string defeatedName = defeatedAlly != null ? defeatedAlly.GetDebugDisplayName() : "友方角色";
        if (effect.statusApplyTiming == Relic_SO.RelicStatusApplyTiming.NextRoundStart)
        {
            int delayRounds = Mathf.Max(1, effect.applyAfterRoundStarts);
            AddRelicFeedback(context, relicData, $"{defeatedName} 死亡后，其余 {affectedAllyCount} 名友方将于下 {delayRounds} 个回合开始时获得 {allyDeathStacksToApply} 层 {GetStatusDisplayName(effect.statusData)}。");
        }
        else
        {
            AddRelicFeedback(context, relicData, $"{defeatedName} 死亡后，其余 {affectedAllyCount} 名友方获得 {allyDeathStacksToApply} 层 {GetStatusDisplayName(effect.statusData)}。");
        }
    }

    private static void HandleTransferStatusToHighestHpAllyOnPlayerDeath(
        Relic_SO relicData,
        RunRelicState relicState,
        RelicExecutionContext context,
        Relic_SO.RelicBuiltInEffectEntry effect)
    {
        if (context.trigger != Relic_SO.RelicTrigger.OnPlayerDeath)
            return;

        BattleAllyController defeatedSource = context.target as BattleAllyController;
        BattleAllyController highestHpRecipient = ResolveHighestHpLivingAlly(context.allies, defeatedSource);
        if (defeatedSource == null || highestHpRecipient == null || effect.statusData == null)
            return;

        int transferredStacks = defeatedSource.GetStatusStacks(effect.statusData);
        if (transferredStacks <= 0)
            return;

        if (!highestHpRecipient.AddStatus(effect.statusData, effect.statusData.StatusId, effect.statusData.DisplayName, transferredStacks))
            return;

        AddRelicFeedback(
            context,
            relicData,
            $"{defeatedSource.GetDebugDisplayName()} 的 {transferredStacks} 层 {GetStatusDisplayName(effect.statusData)} 转移给了 {highestHpRecipient.GetDebugDisplayName()}。");
    }
}
