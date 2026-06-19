using System;
using System.Collections.Generic;

public enum RelicBuiltInNumericRequirement
{
    None = 0,
    AmountPositive = 1,
    AnyPowerDeltaNonZero = 2,
    PowerAmountNonZero = 3,
    MaxPowerAmountOrAmountNonZero = 4
}

public sealed class RelicBuiltInEffectMetadata
{
    public RelicBuiltInEffectMetadata(
        Relic_SO.RelicBuiltInEffectType effectType,
        bool requiresStatusData,
        bool requiresConditionStatusData,
        RelicBuiltInNumericRequirement numericRequirement,
        string numericFieldPath)
    {
        EffectType = effectType;
        RequiresStatusData = requiresStatusData;
        RequiresConditionStatusData = requiresConditionStatusData;
        NumericRequirement = numericRequirement;
        NumericFieldPath = string.IsNullOrWhiteSpace(numericFieldPath) ? "amount" : numericFieldPath;
    }

    public Relic_SO.RelicBuiltInEffectType EffectType { get; private set; }
    public bool RequiresStatusData { get; private set; }
    public bool RequiresConditionStatusData { get; private set; }
    public RelicBuiltInNumericRequirement NumericRequirement { get; private set; }
    public string NumericFieldPath { get; private set; }

    public bool HasMeaningfulNumericValue(Relic_SO.RelicBuiltInEffectEntry effect)
    {
        if (effect == null)
            return false;

        switch (NumericRequirement)
        {
            case RelicBuiltInNumericRequirement.None:
                return true;
            case RelicBuiltInNumericRequirement.AnyPowerDeltaNonZero:
                return effect.powerAmount != 0 || effect.minPowerAmount != 0 || effect.maxPowerAmount != 0;
            case RelicBuiltInNumericRequirement.PowerAmountNonZero:
                return effect.powerAmount != 0;
            case RelicBuiltInNumericRequirement.MaxPowerAmountOrAmountNonZero:
                return effect.maxPowerAmount != 0 || effect.amount != 0;
            case RelicBuiltInNumericRequirement.AmountPositive:
            default:
                return effect.amount > 0;
        }
    }
}

public static class RelicBuiltInEffectMetadataCatalog
{
    private static readonly Dictionary<Relic_SO.RelicBuiltInEffectType, RelicBuiltInEffectMetadata> MetadataByType =
        CreateMetadata();

    public static bool TryGet(
        Relic_SO.RelicBuiltInEffectType effectType,
        out RelicBuiltInEffectMetadata metadata)
    {
        return MetadataByType.TryGetValue(effectType, out metadata);
    }

    public static List<Relic_SO.RelicBuiltInEffectType> GetMissingMetadataForDiagnostics()
    {
        List<Relic_SO.RelicBuiltInEffectType> missingTypes = new List<Relic_SO.RelicBuiltInEffectType>();
        Array values = Enum.GetValues(typeof(Relic_SO.RelicBuiltInEffectType));
        for (int i = 0; i < values.Length; i++)
        {
            Relic_SO.RelicBuiltInEffectType effectType = (Relic_SO.RelicBuiltInEffectType)values.GetValue(i);
            if (effectType == Relic_SO.RelicBuiltInEffectType.None)
                continue;

            if (!MetadataByType.ContainsKey(effectType))
                missingTypes.Add(effectType);
        }

        return missingTypes;
    }

    private static Dictionary<Relic_SO.RelicBuiltInEffectType, RelicBuiltInEffectMetadata> CreateMetadata()
    {
        Dictionary<Relic_SO.RelicBuiltInEffectType, RelicBuiltInEffectMetadata> metadata =
            new Dictionary<Relic_SO.RelicBuiltInEffectType, RelicBuiltInEffectMetadata>();

        Add(metadata, Relic_SO.RelicBuiltInEffectType.ChanceToIgnoreMoveCost, numericRequirement: RelicBuiltInNumericRequirement.None);
        Add(metadata, Relic_SO.RelicBuiltInEffectType.AddExtraStatusStacksOnApply);
        Add(metadata, Relic_SO.RelicBuiltInEffectType.ModifyPlayerDiceWhenSelfHasStatus, requiresConditionStatusData: true, numericRequirement: RelicBuiltInNumericRequirement.AnyPowerDeltaNonZero, numericFieldPath: "powerAmount/minPowerAmount/maxPowerAmount");
        Add(metadata, Relic_SO.RelicBuiltInEffectType.ReduceIncomingSingleAttackDamageFlat);
        Add(metadata, Relic_SO.RelicBuiltInEffectType.ModifyPlayerDiceBasedOnLastRoundSkillMatch);
        Add(metadata, Relic_SO.RelicBuiltInEffectType.ReduceMerchantPricePercent);
        Add(metadata, Relic_SO.RelicBuiltInEffectType.ApplyStatusToPlayerWhenSpeedEquals, requiresStatusData: true);
        Add(metadata, Relic_SO.RelicBuiltInEffectType.ApplyStatusToAllEnemiesOnSpecificRound, requiresStatusData: true);
        Add(metadata, Relic_SO.RelicBuiltInEffectType.ApplyStatusToAllAlliesOnSpecificRound, requiresStatusData: true);
        Add(metadata, Relic_SO.RelicBuiltInEffectType.ApplyStatusToRemainingAlliesOnPlayerDeath, requiresStatusData: true);
        Add(metadata, Relic_SO.RelicBuiltInEffectType.HealRandomDamagedAllyOnBattleRoundEnd);
        Add(metadata, Relic_SO.RelicBuiltInEffectType.ReduceIncomingSingleAttackDamagePercentWhenAttackerSlowerThanTarget);
        Add(metadata, Relic_SO.RelicBuiltInEffectType.ReduceIncomingAttackDamagePercentWhenAttackerSlowerThanTarget);
        Add(metadata, Relic_SO.RelicBuiltInEffectType.ModifyPlayerClashDiceWhenFasterThanTarget, numericRequirement: RelicBuiltInNumericRequirement.PowerAmountNonZero, numericFieldPath: "powerAmount");
        Add(metadata, Relic_SO.RelicBuiltInEffectType.PreserveRandomRelicMaterialOnReforge, numericRequirement: RelicBuiltInNumericRequirement.None);
        Add(metadata, Relic_SO.RelicBuiltInEffectType.GrantRandomLootOfSameRarityOnLootReforge, numericRequirement: RelicBuiltInNumericRequirement.None);
        Add(metadata, Relic_SO.RelicBuiltInEffectType.IncreaseRestReforgeLimit);
        Add(metadata, Relic_SO.RelicBuiltInEffectType.IgnoreMoveCostWhenDestinationVisited, numericRequirement: RelicBuiltInNumericRequirement.None);
        Add(metadata, Relic_SO.RelicBuiltInEffectType.ReduceTargetClashMinPowerWhenTargetHasStatus, requiresConditionStatusData: true);
        Add(metadata, Relic_SO.RelicBuiltInEffectType.ReduceIncomingDamagePercentWhenSpeedGreaterThan);
        Add(metadata, Relic_SO.RelicBuiltInEffectType.AddMerchantPurchaseDiscountStackOnPurchase, numericRequirement: RelicBuiltInNumericRequirement.None);
        Add(metadata, Relic_SO.RelicBuiltInEffectType.ReduceMerchantPricePercentPerPurchaseStack);
        Add(metadata, Relic_SO.RelicBuiltInEffectType.HealAllAlliesOnEnemyDeath);
        Add(metadata, Relic_SO.RelicBuiltInEffectType.ApplyStatusToRandomAllyOnSpecificRound, requiresStatusData: true);
        Add(metadata, Relic_SO.RelicBuiltInEffectType.ModifyOutgoingDamagePercentWhenTargetHasDistinctStatuses);
        Add(metadata, Relic_SO.RelicBuiltInEffectType.ApplyStatusToRandomEnemyOnSpecificRound, requiresStatusData: true);
        Add(metadata, Relic_SO.RelicBuiltInEffectType.ModifyOutgoingDamageFlatWhenTargetHasStatus, requiresConditionStatusData: true);
        Add(metadata, Relic_SO.RelicBuiltInEffectType.SurviveLethalDamageOncePerBattleAndApplyStatus, numericRequirement: RelicBuiltInNumericRequirement.None);
        Add(metadata, Relic_SO.RelicBuiltInEffectType.ModifyPlayerDiceWhenSkillUsedConsecutively, numericRequirement: RelicBuiltInNumericRequirement.PowerAmountNonZero, numericFieldPath: "powerAmount");
        Add(metadata, Relic_SO.RelicBuiltInEffectType.HealPartyPercentOnEnteringUnvisitedMerchantTile);
        Add(metadata, Relic_SO.RelicBuiltInEffectType.ReduceIncomingAttackDamagePercentWhenAttackerHasStatus, requiresConditionStatusData: true);
        Add(metadata, Relic_SO.RelicBuiltInEffectType.RestoreActionPointsOnKeyCollectedByPercentMax);
        Add(metadata, Relic_SO.RelicBuiltInEffectType.ApplyStatusToAllAlliesOnOddRounds, requiresStatusData: true);
        Add(metadata, Relic_SO.RelicBuiltInEffectType.ReduceIncomingDamageFlatPerStatusStacks, requiresConditionStatusData: true);
        Add(metadata, Relic_SO.RelicBuiltInEffectType.ApplyStatusToAttackerAfterTakingSingleAttackDamageOncePerRound, requiresStatusData: true);
        Add(metadata, Relic_SO.RelicBuiltInEffectType.ReflectSingleAttackDamageToAttackerPercent);
        Add(metadata, Relic_SO.RelicBuiltInEffectType.ChanceToQueueStatusOnHitWhenSlowerOncePerSkillUse, requiresStatusData: true);
        Add(metadata, Relic_SO.RelicBuiltInEffectType.ApplyStatusToSelfOnDefenseClashWin, requiresStatusData: true);
        Add(metadata, Relic_SO.RelicBuiltInEffectType.ApplyStatusToSelfOnEnemyKilledByPlayer, requiresStatusData: true);
        Add(metadata, Relic_SO.RelicBuiltInEffectType.GrantExtraRandomLootOfSameRarityAfterBattle, numericRequirement: RelicBuiltInNumericRequirement.None);
        Add(metadata, Relic_SO.RelicBuiltInEffectType.QueueNextBattleAllyStatusOnCalamity, requiresStatusData: true);
        Add(metadata, Relic_SO.RelicBuiltInEffectType.IncreasePartyMaxHpAndHealOnCalamity);
        Add(metadata, Relic_SO.RelicBuiltInEffectType.TransferStatusToHighestHpAllyOnPlayerDeath, requiresStatusData: true);
        Add(metadata, Relic_SO.RelicBuiltInEffectType.RestoreSharedEnergyAtBattleStartInEliteOrBoss);
        Add(metadata, Relic_SO.RelicBuiltInEffectType.GrantTemporaryExtraAllySpeedButtonsOnFirstRound);
        Add(metadata, Relic_SO.RelicBuiltInEffectType.TransferSpeedButtonsToLowestHpAllyOnPlayerDeath, numericRequirement: RelicBuiltInNumericRequirement.None);
        Add(metadata, Relic_SO.RelicBuiltInEffectType.ModifyDefenseSkillMaxPower, numericRequirement: RelicBuiltInNumericRequirement.MaxPowerAmountOrAmountNonZero, numericFieldPath: "maxPowerAmount/amount");
        Add(metadata, Relic_SO.RelicBuiltInEffectType.SetEnemyBurnRoundStartDecayPercent);
        Add(metadata, Relic_SO.RelicBuiltInEffectType.ApplyStatusToLowestHpEnemyOnBattleRoundStart, requiresStatusData: true);
        Add(metadata, Relic_SO.RelicBuiltInEffectType.ApplyStatusToEnemyOnFirstPlayerHitEachRound, requiresStatusData: true);
        Add(metadata, Relic_SO.RelicBuiltInEffectType.TriggerStatusDamageWhenEnemyStatusReachesStacksOncePerBattle, requiresConditionStatusData: true);
        Add(metadata, Relic_SO.RelicBuiltInEffectType.ChanceApplyStatusToEnemiesWithStatusOnBattleRoundStart, requiresStatusData: true, requiresConditionStatusData: true);
        Add(metadata, Relic_SO.RelicBuiltInEffectType.ApplyStatusToAttackerAfterTakingSingleAttackDamageLimitedPerRound, requiresStatusData: true);
        Add(metadata, Relic_SO.RelicBuiltInEffectType.ModifyPlayerClashDiceWhenTargetHasStatus, requiresConditionStatusData: true, numericRequirement: RelicBuiltInNumericRequirement.AnyPowerDeltaNonZero, numericFieldPath: "powerAmount/minPowerAmount/maxPowerAmount");
        Add(metadata, Relic_SO.RelicBuiltInEffectType.HealSourceWhenPlayerAppliesStatusToEnemy, requiresConditionStatusData: true);
        Add(metadata, Relic_SO.RelicBuiltInEffectType.ApplyStatusToAllAlliesWhenStatusBearerCountAtLeast, requiresStatusData: true, requiresConditionStatusData: true);
        Add(metadata, Relic_SO.RelicBuiltInEffectType.TransferStatusToLowestHpEnemyOnEnemyDeath, requiresStatusData: true, numericRequirement: RelicBuiltInNumericRequirement.None);
        Add(metadata, Relic_SO.RelicBuiltInEffectType.HealSourceWhenDamagingEnemyWithStatus, requiresConditionStatusData: true);
        Add(metadata, Relic_SO.RelicBuiltInEffectType.PreventLethalDamageByConsumingEnemyStatusTotal, requiresConditionStatusData: true);
        Add(metadata, Relic_SO.RelicBuiltInEffectType.SetEnemyStatusMinimumStacks, requiresConditionStatusData: true);
        Add(metadata, Relic_SO.RelicBuiltInEffectType.ChanceCopyAppliedStatusToOtherEnemy, requiresStatusData: true);
        Add(metadata, Relic_SO.RelicBuiltInEffectType.ChanceRepeatAppliedStatusToSameEnemy, requiresStatusData: true);
        Add(metadata, Relic_SO.RelicBuiltInEffectType.ApplyStatusToEnemiesBasedOnConditionStacksOnRoundStart, requiresStatusData: true, requiresConditionStatusData: true);
        Add(metadata, Relic_SO.RelicBuiltInEffectType.ApplyStatusToEnemiesWhenConditionStacksAtLeastOnBattleRoundStart, requiresStatusData: true, requiresConditionStatusData: true);
        Add(metadata, Relic_SO.RelicBuiltInEffectType.DealDamageToEnemiesEqualToStatusStacksOnBattleRoundEnd, requiresConditionStatusData: true);
        Add(metadata, Relic_SO.RelicBuiltInEffectType.RestoreSharedEnergyByEnemyStatusStacksOnBattleRoundEnd, requiresConditionStatusData: true);
        Add(metadata, Relic_SO.RelicBuiltInEffectType.ApplyStatusesToHighestAndLowestHpRatioEnemiesOnBattleRoundStart, requiresStatusData: true, requiresConditionStatusData: true);
        Add(metadata, Relic_SO.RelicBuiltInEffectType.ApplyStatusToEnemyWithConditionOnFirstPlayerHitEachRound, requiresStatusData: true, requiresConditionStatusData: true);
        Add(metadata, Relic_SO.RelicBuiltInEffectType.SetEnemyLinkedStatusRoundStartStacks, requiresStatusData: true, requiresConditionStatusData: true);
        Add(metadata, Relic_SO.RelicBuiltInEffectType.TriggerAdditionalDamageWhenEnemyStatusTicksWithConditionStatus, requiresStatusData: true, requiresConditionStatusData: true);
        Add(metadata, Relic_SO.RelicBuiltInEffectType.PreventAllyStatusApplication, requiresConditionStatusData: true, numericRequirement: RelicBuiltInNumericRequirement.None);
        Add(metadata, Relic_SO.RelicBuiltInEffectType.ApplyStatusToLowestSpeedAllyWhenAnyEnemyHasStatusOnBattleRoundStart, requiresStatusData: true, requiresConditionStatusData: true);
        Add(metadata, Relic_SO.RelicBuiltInEffectType.ReduceIncomingAttackDamageFlatWhenAttackerHasStatus, requiresConditionStatusData: true);
        Add(metadata, Relic_SO.RelicBuiltInEffectType.ModifyOutgoingDamagePercentByTargetStatusStacks, requiresConditionStatusData: true);
        Add(metadata, Relic_SO.RelicBuiltInEffectType.GrantMoneyOnAcquire);
        Add(metadata, Relic_SO.RelicBuiltInEffectType.IncreasePartyMaxHpOnAcquire);
        Add(metadata, Relic_SO.RelicBuiltInEffectType.RestoreSharedEnergyOnSpecificRound);
        Add(metadata, Relic_SO.RelicBuiltInEffectType.ApplyStatusToFullHpAlliesOnBattleRoundStart, requiresStatusData: true);
        Add(metadata, Relic_SO.RelicBuiltInEffectType.ApplyStatusToLowHpAlliesOnBattleRoundStart, requiresStatusData: true);
        Add(metadata, Relic_SO.RelicBuiltInEffectType.QueueStatusToAllEnemiesOnPlayerDeathNextRound, requiresStatusData: true);
        Add(metadata, Relic_SO.RelicBuiltInEffectType.GrantMoneyOnActionPointsSpent);
        Add(metadata, Relic_SO.RelicBuiltInEffectType.SetMerchantInitialStockCount);
        Add(metadata, Relic_SO.RelicBuiltInEffectType.AllowMerchantPurchaseWithPositiveMoney, numericRequirement: RelicBuiltInNumericRequirement.None);
        Add(metadata, Relic_SO.RelicBuiltInEffectType.GuaranteeBattleLootDrop, numericRequirement: RelicBuiltInNumericRequirement.None);
        Add(metadata, Relic_SO.RelicBuiltInEffectType.IncreaseRelicChoiceRefreshCount);
        Add(metadata, Relic_SO.RelicBuiltInEffectType.GrantRandomRelicsOnAcquire);
        Add(metadata, Relic_SO.RelicBuiltInEffectType.QueueStatusToPlayerOnClashLostAgainstStatusTarget, requiresStatusData: true, requiresConditionStatusData: true);
        Add(metadata, Relic_SO.RelicBuiltInEffectType.IncreaseSharedEnergyMaxAtBattleStart);
        Add(metadata, Relic_SO.RelicBuiltInEffectType.GrantMoney);
        Add(metadata, Relic_SO.RelicBuiltInEffectType.IgnoreMoveCostWhenDestinationUnvisitedRestTile, numericRequirement: RelicBuiltInNumericRequirement.None);
        Add(metadata, Relic_SO.RelicBuiltInEffectType.GrantTemporarySharedEnergyOnSpecificRound);
        Add(metadata, Relic_SO.RelicBuiltInEffectType.GrantTemporarySharedEnergyEachRound);
        Add(metadata, Relic_SO.RelicBuiltInEffectType.ApplyStatusToTemporarySkillUser, requiresStatusData: true);
        Add(metadata, Relic_SO.RelicBuiltInEffectType.PreventBattleDefeatAndFullHealPartyOncePerRun, numericRequirement: RelicBuiltInNumericRequirement.None);
        Add(metadata, Relic_SO.RelicBuiltInEffectType.ModifyBattleExperiencePercentAndHealMissingHp, numericRequirement: RelicBuiltInNumericRequirement.MaxPowerAmountOrAmountNonZero, numericFieldPath: "powerAmount/amount");
        Add(metadata, Relic_SO.RelicBuiltInEffectType.SummonTemporaryAllyFromEnemyDataAtBattleStart, numericRequirement: RelicBuiltInNumericRequirement.None);
        Add(metadata, Relic_SO.RelicBuiltInEffectType.QueueNextRoundMinPowerBonusOnMinRollOncePerRound);
        Add(metadata, Relic_SO.RelicBuiltInEffectType.HealPartyPercentAfterBattleInSpecialDungeon);
        Add(metadata, Relic_SO.RelicBuiltInEffectType.DealDamageWhenEnemyStatusReachesStacksOncePerRound, requiresConditionStatusData: true);
        Add(metadata, Relic_SO.RelicBuiltInEffectType.TransferStatusToAllEnemiesOnEnemyDeath, requiresStatusData: true, numericRequirement: RelicBuiltInNumericRequirement.None);
        Add(metadata, Relic_SO.RelicBuiltInEffectType.HealRandomDamagedAllyOnEnemyDeathWithStatus, requiresConditionStatusData: true);
        Add(metadata, Relic_SO.RelicBuiltInEffectType.ReflectEnemyStatusStacksDamageAfterTakingAttackDamage, requiresConditionStatusData: true, numericRequirement: RelicBuiltInNumericRequirement.None);
        Add(metadata, Relic_SO.RelicBuiltInEffectType.ApplyStatusToEnemyOnEnemyClashWinWithStatusOncePerRound, requiresStatusData: true, requiresConditionStatusData: true);
        Add(metadata, Relic_SO.RelicBuiltInEffectType.PromotePlayerOnHitStatusToSkillUse, requiresConditionStatusData: true, numericRequirement: RelicBuiltInNumericRequirement.None);
        Add(metadata, Relic_SO.RelicBuiltInEffectType.SetEnemyStatusRoundEndDecayPercent, requiresConditionStatusData: true);
        Add(metadata, Relic_SO.RelicBuiltInEffectType.PreserveEnemyStatusWhenAllLivingEnemiesHaveStatus, requiresConditionStatusData: true);
        Add(metadata, Relic_SO.RelicBuiltInEffectType.PreventEnemyHealingWhenHasStatus, requiresConditionStatusData: true, numericRequirement: RelicBuiltInNumericRequirement.None);
        Add(metadata, Relic_SO.RelicBuiltInEffectType.PreventLethalDamageByTransferringHighestHpAllyHealth, requiresConditionStatusData: true);
        Add(metadata, Relic_SO.RelicBuiltInEffectType.ApplyStatusOnTransferredLethalProtectionWhenEnemyStatusAtLeast, requiresStatusData: true, requiresConditionStatusData: true);
        Add(metadata, Relic_SO.RelicBuiltInEffectType.ModifyPlayerClashDiceWhenTargetHasStatusOncePerRound, requiresConditionStatusData: true, numericRequirement: RelicBuiltInNumericRequirement.AnyPowerDeltaNonZero, numericFieldPath: "powerAmount/minPowerAmount/maxPowerAmount");
        Add(metadata, Relic_SO.RelicBuiltInEffectType.ModifyOutgoingDamagePercentWhenTargetHasStatus, requiresConditionStatusData: true);
        Add(metadata, Relic_SO.RelicBuiltInEffectType.SetStatusBurstConsumptionPercent, requiresConditionStatusData: true);
        Add(metadata, Relic_SO.RelicBuiltInEffectType.ChanceTriggerNonConsumingStatusBurstWhenDamagingStatusTarget, requiresConditionStatusData: true);
        Add(metadata, Relic_SO.RelicBuiltInEffectType.TreatAllyWithStatusAsSpeedBonusAtRoundStart, requiresConditionStatusData: true);
        Add(metadata, Relic_SO.RelicBuiltInEffectType.SpreadPlayerStatusBurstDamageToOtherEnemies, requiresConditionStatusData: true);
        Add(metadata, Relic_SO.RelicBuiltInEffectType.ApplyStatusToTargetOnPlayerDamageWhenSourceHasStatus, requiresStatusData: true, requiresConditionStatusData: true);
        Add(metadata, Relic_SO.RelicBuiltInEffectType.DealDamageToEnemiesWithStatusStacksAtLeastOnBattleRoundEnd, requiresConditionStatusData: true);
        Add(metadata, Relic_SO.RelicBuiltInEffectType.QueueNextRoundStatusOnSourceWhenPlayerStatusBurstDamageAtLeast, requiresConditionStatusData: true);
        Add(metadata, Relic_SO.RelicBuiltInEffectType.ApplyStatusToAllAlliesAtBattleSetup, requiresStatusData: true);
        Add(metadata, Relic_SO.RelicBuiltInEffectType.ApplyStatusToAllAlliesWhenAllLivingAlliesHaveStatusOnBattleRoundStart, requiresStatusData: true, requiresConditionStatusData: true);
        Add(metadata, Relic_SO.RelicBuiltInEffectType.ApplyStatusToSourceOnPlayerStatusBurstDamage, requiresStatusData: true, requiresConditionStatusData: true);
        Add(metadata, Relic_SO.RelicBuiltInEffectType.DealDamageToPlayerStatusBurstTargetWithConditionStatus, requiresStatusData: true, requiresConditionStatusData: true);
        Add(metadata, Relic_SO.RelicBuiltInEffectType.PreventLethalDamageByConsumingAllyStatusOncePerBattle, requiresStatusData: true, requiresConditionStatusData: true);
        Add(metadata, Relic_SO.RelicBuiltInEffectType.RefundEnergyOnClashLostOncePerSkillPerRound);
        Add(metadata, Relic_SO.RelicBuiltInEffectType.MatchLowestAllySpeedToHighestOnBattleRoundStart, numericRequirement: RelicBuiltInNumericRequirement.None);
        Add(metadata, Relic_SO.RelicBuiltInEffectType.ApplyStatusToLowestHpAllyOnBattleRoundStart, requiresStatusData: true);
        Add(metadata, Relic_SO.RelicBuiltInEffectType.DamageRandomEnemyWhenAllyLosesShieldStacks);
        Add(metadata, Relic_SO.RelicBuiltInEffectType.ModifyDefenseDiceMinPowerWhenSelfHasStatus, requiresConditionStatusData: true, numericRequirement: RelicBuiltInNumericRequirement.MaxPowerAmountOrAmountNonZero, numericFieldPath: "minPowerAmount/amount");
        Add(metadata, Relic_SO.RelicBuiltInEffectType.GrantRandomRelicsAndBlueprintsOnAcquire);
        Add(metadata, Relic_SO.RelicBuiltInEffectType.EnableDiagonalMapMovement, numericRequirement: RelicBuiltInNumericRequirement.None);
        Add(metadata, Relic_SO.RelicBuiltInEffectType.KeepOnlyInitialPartyMemberAndDisableRecruitOnAcquire);
        Add(metadata, Relic_SO.RelicBuiltInEffectType.ChanceApplyStatusToAllAlliesOnBattleRoundStart, requiresStatusData: true);
        Add(metadata, Relic_SO.RelicBuiltInEffectType.DoubleFirstEnemyDamageEachBattle, numericRequirement: RelicBuiltInNumericRequirement.None);
        Add(metadata, Relic_SO.RelicBuiltInEffectType.ResetKeysAndRegenerateMapOnAcquire, numericRequirement: RelicBuiltInNumericRequirement.None);
        Add(metadata, Relic_SO.RelicBuiltInEffectType.ConfigureFixedRelicReforgePair, numericRequirement: RelicBuiltInNumericRequirement.None);

        return metadata;
    }

    private static void Add(
        Dictionary<Relic_SO.RelicBuiltInEffectType, RelicBuiltInEffectMetadata> metadata,
        Relic_SO.RelicBuiltInEffectType effectType,
        bool requiresStatusData = false,
        bool requiresConditionStatusData = false,
        RelicBuiltInNumericRequirement numericRequirement = RelicBuiltInNumericRequirement.AmountPositive,
        string numericFieldPath = "amount")
    {
        metadata.Add(
            effectType,
            new RelicBuiltInEffectMetadata(
                effectType,
                requiresStatusData,
                requiresConditionStatusData,
                numericRequirement,
                numericFieldPath));
    }
}
