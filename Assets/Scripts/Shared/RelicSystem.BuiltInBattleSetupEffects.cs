using UnityEngine;

public static partial class RelicSystem
{
    private static void HandleQueueNextBattleAllyStatusOnCalamity(
        Relic_SO relicData,
        RunRelicState relicState,
        RelicExecutionContext context,
        Relic_SO.RelicBuiltInEffectEntry effect)
    {
        if (context.trigger != Relic_SO.RelicTrigger.OnCalamityApplied)
            return;
        if (context.runState == null || effect.statusData == null)
            return;

        int calamityQueuedStacks = Mathf.Max(0, effect.amount * Mathf.Max(1, relicState.amount));
        if (calamityQueuedStacks <= 0)
            return;

        context.runState.QueueNextBattleAllyStatusGrant(effect.statusData, calamityQueuedStacks);
        AddRelicFeedback(context, relicData, $"下一场战斗时，所有友方将获得 {calamityQueuedStacks} 层 {GetStatusDisplayName(effect.statusData)}。");
        return;
    }

    private static void HandleIncreasePartyMaxHpAndHealOnCalamity(
        Relic_SO relicData,
        RunRelicState relicState,
        RelicExecutionContext context,
        Relic_SO.RelicBuiltInEffectEntry effect)
    {
        if (context.trigger != Relic_SO.RelicTrigger.OnCalamityApplied)
            return;
        if (context.runState == null || context.runState.partyMembers == null || context.runState.partyMembers.Count == 0)
            return;

        int hpGain = Mathf.Max(0, effect.amount * Mathf.Max(1, relicState.amount));
        if (hpGain <= 0)
            return;

        for (int memberIndex = 0; memberIndex < context.runState.partyMembers.Count; memberIndex++)
        {
            RunPartyMemberState member = context.runState.partyMembers[memberIndex];
            if (member == null)
                continue;

            RunPartyMemberMaxHpService.AddPreCalamityMaxHp(member, hpGain, context.runState.activeCalamities);
        }

        context.runState.SyncLegacyPlayerFieldsFromParty();
        AddRelicFeedback(context, relicData, $"所有友方最大生命 +{hpGain}，并回复相同数值。");
        return;
    }

    private static void HandleRestoreSharedEnergyAtBattleStartInEliteOrBoss(
        Relic_SO relicData,
        RunRelicState relicState,
        RelicExecutionContext context,
        Relic_SO.RelicBuiltInEffectEntry effect)
    {
        if (context.trigger != Relic_SO.RelicTrigger.OnBattleRoundStart)
            return;

        BattleEncounterData activeEncounter = BattleEncounterRuntime.ActiveEncounter;
        if (activeEncounter == null || (activeEncounter.tileType != MapTileType.Elite && activeEncounter.tileType != MapTileType.Boss))
            return;

        int extraEnergy = Mathf.Max(0, effect.amount * Mathf.Max(1, relicState.amount));
        if (extraEnergy <= 0)
            return;

        context.additionalSharedEnergyRecovery += extraEnergy;
        AddRelicFeedback(context, relicData, $"本回合额外回复 {extraEnergy} 点能量。");
        return;
    }

    private static void HandleGrantTemporaryExtraAllySpeedButtonsOnFirstRound(
        Relic_SO relicData,
        RunRelicState relicState,
        RelicExecutionContext context,
        Relic_SO.RelicBuiltInEffectEntry effect)
    {
        if (context.trigger != Relic_SO.RelicTrigger.OnBattleSetup || context.currentRound != 1)
            return;

        int extraButtons = Mathf.Max(0, effect.amount * Mathf.Max(1, relicState.amount));
        if (extraButtons <= 0)
            return;

        context.additionalTemporaryAllySpeedButtons += extraButtons;
        AddRelicFeedback(context, relicData, $"首回合所有友方额外获得 {extraButtons} 个速度按钮。");
        return;
    }

    private static void HandleTransferSpeedButtonsToLowestHpAllyOnPlayerDeath(
        Relic_SO relicData,
        RunRelicState relicState,
        RelicExecutionContext context,
        Relic_SO.RelicBuiltInEffectEntry effect)
    {
        if (context.trigger != Relic_SO.RelicTrigger.OnPlayerDeath)
            return;

        BattleAllyController defeatedSpeedSource = context.target as BattleAllyController;
        BattleAllyController lowestHpRecipient = ResolveLowestHpLivingAlly(context.allies, defeatedSpeedSource);
        if (defeatedSpeedSource == null || lowestHpRecipient == null)
            return;

        if (!defeatedSpeedSource.TransferInheritableSpeedButtonsTo(lowestHpRecipient))
            return;

        AddRelicFeedback(context, relicData, $"{defeatedSpeedSource.GetDebugDisplayName()} 的速度按钮转移给了 {lowestHpRecipient.GetDebugDisplayName()}。");
        return;
    }
}
