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
        AddRelicFeedback(context, relicData, GameText.Format("relic.feedback.queue_next_battle_ally_status_on_calamity", "下一场战斗时，所有友方将获得 {calamityQueuedStacks} 层 {status}。", ("calamityQueuedStacks", calamityQueuedStacks), ("status", GetStatusDisplayName(effect.statusData))));
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
        AddRelicFeedback(context, relicData, GameText.Format("relic.feedback.increase_party_max_hp_and_heal_on_calamity", "所有友方最大生命 +{hpGain}，并回复相同数值。", ("hpGain", hpGain)));
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
        AddRelicFeedback(context, relicData, GameText.Format("relic.feedback.restore_shared_energy_at_battle_start_in_elite_or_boss", "本回合额外回复 {extraEnergy} 点能量。", ("extraEnergy", extraEnergy)));
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
        AddRelicFeedback(context, relicData, GameText.Format("relic.feedback.grant_temporary_extra_ally_speed_buttons_on_first_round", "首回合所有友方额外获得 {extraButtons} 个速度按钮。", ("extraButtons", extraButtons)));
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

        AddRelicFeedback(context, relicData, GameText.Format("relic.feedback.transfer_speed_buttons_to_lowest_hp_ally_on_player_death", "{actor} 的速度按钮转移给了 {target}。", ("actor", GetActorDisplayName(defeatedSpeedSource)), ("target", GetActorDisplayName(lowestHpRecipient))));
        return;
    }
}
