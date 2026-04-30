using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public static partial class RelicSystem
{
    private delegate void BuiltInEffectApplicator(
        Relic_SO relicData,
        RunRelicState relicState,
        RelicExecutionContext context,
        Relic_SO.RelicBuiltInEffectEntry effect);

    private static readonly Dictionary<Relic_SO.RelicBuiltInEffectType, BuiltInEffectApplicator> BuiltInEffectApplicators =
        CreateBuiltInEffectApplicators();

    public static bool IsBuiltInEffectRegistered(Relic_SO.RelicBuiltInEffectType effectType)
    {
        return effectType != Relic_SO.RelicBuiltInEffectType.None
            && BuiltInEffectApplicators.ContainsKey(effectType);
    }

    public static List<Relic_SO.RelicBuiltInEffectType> GetUnregisteredBuiltInEffectTypesForDiagnostics()
    {
        List<Relic_SO.RelicBuiltInEffectType> missingEffects = new List<Relic_SO.RelicBuiltInEffectType>();
        Array values = Enum.GetValues(typeof(Relic_SO.RelicBuiltInEffectType));
        for (int i = 0; i < values.Length; i++)
        {
            Relic_SO.RelicBuiltInEffectType effectType = (Relic_SO.RelicBuiltInEffectType)values.GetValue(i);
            if (effectType == Relic_SO.RelicBuiltInEffectType.None)
                continue;

            if (!BuiltInEffectApplicators.ContainsKey(effectType))
                missingEffects.Add(effectType);
        }

        return missingEffects;
    }

    private static Dictionary<Relic_SO.RelicBuiltInEffectType, BuiltInEffectApplicator> CreateBuiltInEffectApplicators()
    {
        return new Dictionary<Relic_SO.RelicBuiltInEffectType, BuiltInEffectApplicator>
        {
            { Relic_SO.RelicBuiltInEffectType.ChanceToIgnoreMoveCost, HandleChanceToIgnoreMoveCost },
            { Relic_SO.RelicBuiltInEffectType.IgnoreMoveCostWhenDestinationVisited, HandleIgnoreMoveCostWhenDestinationVisited },
            { Relic_SO.RelicBuiltInEffectType.AddExtraStatusStacksOnApply, HandleAddExtraStatusStacksOnApply },
            { Relic_SO.RelicBuiltInEffectType.ModifyPlayerDiceWhenSelfHasStatus, HandleModifyPlayerDiceWhenSelfHasStatus },
            { Relic_SO.RelicBuiltInEffectType.ReduceIncomingSingleAttackDamageFlat, HandleReduceIncomingSingleAttackDamageFlat },
            { Relic_SO.RelicBuiltInEffectType.ReduceIncomingSingleAttackDamagePercentWhenAttackerSlowerThanTarget, HandleReduceIncomingSingleAttackDamagePercentWhenAttackerSlowerThanTarget },
            { Relic_SO.RelicBuiltInEffectType.ReduceIncomingAttackDamagePercentWhenAttackerSlowerThanTarget, HandleReduceIncomingAttackDamagePercentWhenAttackerSlowerThanTarget },
            { Relic_SO.RelicBuiltInEffectType.ReduceIncomingDamagePercentWhenSpeedGreaterThan, HandleReduceIncomingDamagePercentWhenSpeedGreaterThan },
            { Relic_SO.RelicBuiltInEffectType.ModifyPlayerDiceBasedOnLastRoundSkillMatch, HandleModifyPlayerDiceBasedOnLastRoundSkillMatch },
            { Relic_SO.RelicBuiltInEffectType.ModifyPlayerClashDiceWhenFasterThanTarget, HandleModifyPlayerClashDiceWhenFasterThanTarget },
            { Relic_SO.RelicBuiltInEffectType.ReduceTargetClashMinPowerWhenTargetHasStatus, HandleReduceTargetClashMinPowerWhenTargetHasStatus },
            { Relic_SO.RelicBuiltInEffectType.PreserveRandomRelicMaterialOnReforge, HandlePreserveRandomRelicMaterialOnReforge },
            { Relic_SO.RelicBuiltInEffectType.GrantRandomLootOfSameRarityOnLootReforge, HandleGrantRandomLootOfSameRarityOnLootReforge },
            { Relic_SO.RelicBuiltInEffectType.IncreaseRestReforgeLimit, HandleIncreaseRestReforgeLimit },
            { Relic_SO.RelicBuiltInEffectType.AddMerchantPurchaseDiscountStackOnPurchase, HandleAddMerchantPurchaseDiscountStackOnPurchase },
            { Relic_SO.RelicBuiltInEffectType.ReduceMerchantPricePercentPerPurchaseStack, HandleReduceMerchantPricePercentPerPurchaseStack },
            { Relic_SO.RelicBuiltInEffectType.ReduceMerchantPricePercent, HandleReduceMerchantPricePercent },
            { Relic_SO.RelicBuiltInEffectType.HealAllAlliesOnEnemyDeath, HandleHealAllAlliesOnEnemyDeath },
            { Relic_SO.RelicBuiltInEffectType.ApplyStatusToPlayerWhenSpeedEquals, HandleApplyStatusToPlayerWhenSpeedEquals },
            { Relic_SO.RelicBuiltInEffectType.ApplyStatusToAllEnemiesOnSpecificRound, HandleApplyStatusToAllEnemiesOnSpecificRound },
            { Relic_SO.RelicBuiltInEffectType.ApplyStatusToAllAlliesOnSpecificRound, HandleApplyStatusToAllAlliesOnSpecificRound },
            { Relic_SO.RelicBuiltInEffectType.ApplyStatusToRandomAllyOnSpecificRound, HandleApplyStatusToRandomAllyOnSpecificRound },
            { Relic_SO.RelicBuiltInEffectType.ApplyStatusToRandomEnemyOnSpecificRound, HandleApplyStatusToRandomEnemyOnSpecificRound },
            { Relic_SO.RelicBuiltInEffectType.ApplyStatusToRemainingAlliesOnPlayerDeath, HandleApplyStatusToRemainingAlliesOnPlayerDeath },
            { Relic_SO.RelicBuiltInEffectType.HealRandomDamagedAllyOnBattleRoundEnd, HandleHealRandomDamagedAllyOnBattleRoundEnd },
            { Relic_SO.RelicBuiltInEffectType.ModifyOutgoingDamagePercentWhenTargetHasDistinctStatuses, HandleModifyOutgoingDamagePercentWhenTargetHasDistinctStatuses },
            { Relic_SO.RelicBuiltInEffectType.ModifyOutgoingDamageFlatWhenTargetHasStatus, HandleModifyOutgoingDamageFlatWhenTargetHasStatus },
            { Relic_SO.RelicBuiltInEffectType.SurviveLethalDamageOncePerBattleAndApplyStatus, HandleSurviveLethalDamageOncePerBattleAndApplyStatus },
            { Relic_SO.RelicBuiltInEffectType.ModifyPlayerDiceWhenSkillUsedConsecutively, HandleModifyPlayerDiceWhenSkillUsedConsecutively },
            { Relic_SO.RelicBuiltInEffectType.HealPartyPercentOnEnteringUnvisitedMerchantTile, HandleHealPartyPercentOnEnteringUnvisitedMerchantTile },
            { Relic_SO.RelicBuiltInEffectType.ReduceIncomingAttackDamagePercentWhenAttackerHasStatus, HandleReduceIncomingAttackDamagePercentWhenAttackerHasStatus },
            { Relic_SO.RelicBuiltInEffectType.RestoreActionPointsOnKeyCollectedByPercentMax, HandleRestoreActionPointsOnKeyCollectedByPercentMax },
            { Relic_SO.RelicBuiltInEffectType.ApplyStatusToAllAlliesOnOddRounds, HandleApplyStatusToAllAlliesOnOddRounds },
            { Relic_SO.RelicBuiltInEffectType.ReduceIncomingDamageFlatPerStatusStacks, HandleReduceIncomingDamageFlatPerStatusStacks },
            { Relic_SO.RelicBuiltInEffectType.ApplyStatusToAttackerAfterTakingSingleAttackDamageOncePerRound, HandleApplyStatusToAttackerAfterTakingSingleAttackDamageOncePerRound },
            { Relic_SO.RelicBuiltInEffectType.ReflectSingleAttackDamageToAttackerPercent, HandleReflectSingleAttackDamageToAttackerPercent },
            { Relic_SO.RelicBuiltInEffectType.ChanceToQueueStatusOnHitWhenSlowerOncePerSkillUse, HandleChanceToQueueStatusOnHitWhenSlowerOncePerSkillUse },
            { Relic_SO.RelicBuiltInEffectType.ApplyStatusToSelfOnDefenseClashWin, HandleApplyStatusToSelfOnDefenseClashWin },
            { Relic_SO.RelicBuiltInEffectType.ApplyStatusToSelfOnEnemyKilledByPlayer, HandleApplyStatusToSelfOnEnemyKilledByPlayer },
            { Relic_SO.RelicBuiltInEffectType.GrantExtraRandomLootOfSameRarityAfterBattle, HandleGrantExtraRandomLootOfSameRarityAfterBattle },
            { Relic_SO.RelicBuiltInEffectType.QueueNextBattleAllyStatusOnCalamity, HandleQueueNextBattleAllyStatusOnCalamity },
            { Relic_SO.RelicBuiltInEffectType.IncreasePartyMaxHpAndHealOnCalamity, HandleIncreasePartyMaxHpAndHealOnCalamity },
            { Relic_SO.RelicBuiltInEffectType.TransferStatusToHighestHpAllyOnPlayerDeath, HandleTransferStatusToHighestHpAllyOnPlayerDeath },
            { Relic_SO.RelicBuiltInEffectType.RestoreSharedEnergyAtBattleStartInEliteOrBoss, HandleRestoreSharedEnergyAtBattleStartInEliteOrBoss },
            { Relic_SO.RelicBuiltInEffectType.GrantTemporaryExtraAllySpeedButtonsOnFirstRound, HandleGrantTemporaryExtraAllySpeedButtonsOnFirstRound },
            { Relic_SO.RelicBuiltInEffectType.TransferSpeedButtonsToLowestHpAllyOnPlayerDeath, HandleTransferSpeedButtonsToLowestHpAllyOnPlayerDeath },
            { Relic_SO.RelicBuiltInEffectType.ModifyDefenseSkillMaxPower, HandleModifyDefenseSkillMaxPower },
            { Relic_SO.RelicBuiltInEffectType.SetEnemyBurnRoundStartDecayPercent, HandlePassiveMarker },
            { Relic_SO.RelicBuiltInEffectType.ApplyStatusToLowestHpEnemyOnBattleRoundStart, HandleApplyStatusToLowestHpEnemyOnBattleRoundStart },
            { Relic_SO.RelicBuiltInEffectType.ApplyStatusToEnemyOnFirstPlayerHitEachRound, HandleApplyStatusToEnemyOnFirstPlayerHitEachRound },
            { Relic_SO.RelicBuiltInEffectType.TriggerStatusDamageWhenEnemyStatusReachesStacksOncePerBattle, HandleTriggerStatusDamageWhenEnemyStatusReachesStacksOncePerBattle },
            { Relic_SO.RelicBuiltInEffectType.ChanceApplyStatusToEnemiesWithStatusOnBattleRoundStart, HandleChanceApplyStatusToEnemiesWithStatusOnBattleRoundStart },
            { Relic_SO.RelicBuiltInEffectType.ApplyStatusToAttackerAfterTakingSingleAttackDamageLimitedPerRound, HandleApplyStatusToAttackerAfterTakingSingleAttackDamageLimitedPerRound },
            { Relic_SO.RelicBuiltInEffectType.ModifyPlayerClashDiceWhenTargetHasStatus, HandleModifyPlayerClashDiceWhenTargetHasStatus },
            { Relic_SO.RelicBuiltInEffectType.HealSourceWhenPlayerAppliesStatusToEnemy, HandleHealSourceWhenPlayerAppliesStatusToEnemy },
            { Relic_SO.RelicBuiltInEffectType.ApplyStatusToAllAlliesWhenStatusBearerCountAtLeast, HandleApplyStatusToAllAlliesWhenStatusBearerCountAtLeast },
            { Relic_SO.RelicBuiltInEffectType.TransferStatusToLowestHpEnemyOnEnemyDeath, HandleTransferStatusToLowestHpEnemyOnEnemyDeath },
            { Relic_SO.RelicBuiltInEffectType.HealSourceWhenDamagingEnemyWithStatus, HandleHealSourceWhenDamagingEnemyWithStatus },
            { Relic_SO.RelicBuiltInEffectType.PreventLethalDamageByConsumingEnemyStatusTotal, HandlePreventLethalDamageByConsumingEnemyStatusTotal },
            { Relic_SO.RelicBuiltInEffectType.SetEnemyStatusMinimumStacks, HandlePassiveMarker },
            { Relic_SO.RelicBuiltInEffectType.ChanceCopyAppliedStatusToOtherEnemy, HandleChanceCopyAppliedStatusToOtherEnemy },
            { Relic_SO.RelicBuiltInEffectType.ChanceRepeatAppliedStatusToSameEnemy, HandleChanceRepeatAppliedStatusToSameEnemy },
            { Relic_SO.RelicBuiltInEffectType.ApplyStatusToEnemiesBasedOnConditionStacksOnRoundStart, HandleApplyStatusToEnemiesBasedOnConditionStacksOnRoundStart },
        };
    }

    private static void ApplyBuiltInEffects(Relic_SO relicData, RunRelicState relicState, RelicExecutionContext context)
    {
        List<Relic_SO.RelicBuiltInEffectEntry> effects = relicData.builtInEffects;
        if (effects == null || effects.Count == 0)
            return;

        for (int i = 0; i < effects.Count; i++)
        {
            Relic_SO.RelicBuiltInEffectEntry effect = effects[i];
            if (effect == null || effect.trigger != context.trigger)
                continue;

            if (effect.chance < 1f && Random.value > Mathf.Clamp01(effect.chance))
                continue;

            if (!BuiltInEffectApplicators.TryGetValue(effect.effectType, out BuiltInEffectApplicator applicator))
                continue;

            applicator(relicData, relicState, context, effect);
        }
    }
}
