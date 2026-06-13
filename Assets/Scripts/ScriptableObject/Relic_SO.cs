using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewRelic", menuName = "Relics/Relic")]
public class Relic_SO : ScriptableObject
{
    public enum RelicStatusApplyTiming
    {
        Immediate = 0,
        NextRoundStart = 1
    }

    public enum RelicTrigger
    {
        OnMoveCostAboutToSpend = 0,
        OnStatusApplied = 1,
        OnPlayerDicePrepared = 2,
        OnPlayerAboutToTakeSingleAttackDamage = 3,
        OnMerchantPriceCalculated = 4,
        OnPlayerRoundStart = 5,
        OnBattleRoundStart = 6,
        OnPlayerDeath = 7,
        OnBattleRoundEnd = 8,
        OnPlayerAboutToTakeAttackDamage = 9,
        OnRelicReforge = 10,
        OnLootReforge = 11,
        OnRestReforgeLimitCalculated = 12,
        OnPlayerAboutToTakeDamage = 13,
        OnPlayerAboutToDealDamage = 14,
        OnEnemyDeath = 15,
        OnPlayerAboutToTakeLethalDamage = 16,
        OnMerchantPurchaseCompleted = 17,
        OnKeyCollected = 18,
        OnCalamityApplied = 19,
        OnPlayerTookSingleAttackDamage = 20,
        OnPlayerHitEnemy = 21,
        OnPlayerDefenseClashWin = 22,
        OnEnemyKilledByPlayer = 23,
        OnBattleLootRewardGranted = 24,
        OnBattleSetup = 25,
        OnRelicAcquired = 26,
        OnActionPointsSpent = 27,
        OnMerchantStockCountCalculated = 28,
        OnMerchantPurchaseAffordabilityChecked = 29,
        OnRelicChoiceRewardGenerated = 30,
        OnPlayerClashLost = 31,
        OnRestActionPointRestored = 32,
        OnRestReforgeCompleted = 33,
        OnRestHealCompleted = 34,
        OnPlayerSkillUsed = 35,
        OnBattleCompletion = 36,
        OnBattleDefeat = 37,
        OnPlayerDiceRolled = 38
    }

    public enum RelicBuiltInEffectType
    {
        None = 0,
        ChanceToIgnoreMoveCost = 1,
        AddExtraStatusStacksOnApply = 2,
        ModifyPlayerDiceWhenSelfHasStatus = 3,
        ReduceIncomingSingleAttackDamageFlat = 4,
        ModifyPlayerDiceBasedOnLastRoundSkillMatch = 5,
        ReduceMerchantPricePercent = 6,
        ApplyStatusToPlayerWhenSpeedEquals = 7,
        ApplyStatusToAllEnemiesOnSpecificRound = 8,
        ApplyStatusToAllAlliesOnSpecificRound = 9,
        ApplyStatusToRemainingAlliesOnPlayerDeath = 10,
        HealRandomDamagedAllyOnBattleRoundEnd = 11,
        ReduceIncomingSingleAttackDamagePercentWhenAttackerSlowerThanTarget = 12,
        ReduceIncomingAttackDamagePercentWhenAttackerSlowerThanTarget = 13,
        ModifyPlayerClashDiceWhenFasterThanTarget = 14,
        PreserveRandomRelicMaterialOnReforge = 15,
        GrantRandomLootOfSameRarityOnLootReforge = 16,
        IncreaseRestReforgeLimit = 17,
        IgnoreMoveCostWhenDestinationVisited = 18,
        ReduceTargetClashMinPowerWhenTargetHasStatus = 19,
        ReduceIncomingDamagePercentWhenSpeedGreaterThan = 20,
        AddMerchantPurchaseDiscountStackOnPurchase = 21,
        ReduceMerchantPricePercentPerPurchaseStack = 22,
        HealAllAlliesOnEnemyDeath = 23,
        ApplyStatusToRandomAllyOnSpecificRound = 24,
        ModifyOutgoingDamagePercentWhenTargetHasDistinctStatuses = 25,
        ApplyStatusToRandomEnemyOnSpecificRound = 26,
        ModifyOutgoingDamageFlatWhenTargetHasStatus = 27,
        SurviveLethalDamageOncePerBattleAndApplyStatus = 28,
        ModifyPlayerDiceWhenSkillUsedConsecutively = 29,
        HealPartyPercentOnEnteringUnvisitedMerchantTile = 30,
        ReduceIncomingAttackDamagePercentWhenAttackerHasStatus = 31,
        RestoreActionPointsOnKeyCollectedByPercentMax = 32,
        ApplyStatusToAllAlliesOnOddRounds = 33,
        ReduceIncomingDamageFlatPerStatusStacks = 34,
        ApplyStatusToAttackerAfterTakingSingleAttackDamageOncePerRound = 35,
        ReflectSingleAttackDamageToAttackerPercent = 36,
        ChanceToQueueStatusOnHitWhenSlowerOncePerSkillUse = 37,
        ApplyStatusToSelfOnDefenseClashWin = 38,
        ApplyStatusToSelfOnEnemyKilledByPlayer = 39,
        GrantExtraRandomLootOfSameRarityAfterBattle = 40,
        QueueNextBattleAllyStatusOnCalamity = 41,
        IncreasePartyMaxHpAndHealOnCalamity = 42,
        TransferStatusToHighestHpAllyOnPlayerDeath = 43,
        RestoreSharedEnergyAtBattleStartInEliteOrBoss = 44,
        GrantTemporaryExtraAllySpeedButtonsOnFirstRound = 45,
        TransferSpeedButtonsToLowestHpAllyOnPlayerDeath = 46,
        ModifyDefenseSkillMaxPower = 47,
        SetEnemyBurnRoundStartDecayPercent = 48,
        ApplyStatusToLowestHpEnemyOnBattleRoundStart = 49,
        ApplyStatusToEnemyOnFirstPlayerHitEachRound = 50,
        TriggerStatusDamageWhenEnemyStatusReachesStacksOncePerBattle = 51,
        ChanceApplyStatusToEnemiesWithStatusOnBattleRoundStart = 52,
        ApplyStatusToAttackerAfterTakingSingleAttackDamageLimitedPerRound = 53,
        ModifyPlayerClashDiceWhenTargetHasStatus = 54,
        HealSourceWhenPlayerAppliesStatusToEnemy = 55,
        ApplyStatusToAllAlliesWhenStatusBearerCountAtLeast = 56,
        TransferStatusToLowestHpEnemyOnEnemyDeath = 57,
        HealSourceWhenDamagingEnemyWithStatus = 58,
        PreventLethalDamageByConsumingEnemyStatusTotal = 59,
        SetEnemyStatusMinimumStacks = 60,
        ChanceCopyAppliedStatusToOtherEnemy = 61,
        ChanceRepeatAppliedStatusToSameEnemy = 62,
        ApplyStatusToEnemiesBasedOnConditionStacksOnRoundStart = 63,
        ApplyStatusToEnemiesWhenConditionStacksAtLeastOnBattleRoundStart = 64,
        DealDamageToEnemiesEqualToStatusStacksOnBattleRoundEnd = 65,
        RestoreSharedEnergyByEnemyStatusStacksOnBattleRoundEnd = 66,
        ApplyStatusesToHighestAndLowestHpRatioEnemiesOnBattleRoundStart = 67,
        ApplyStatusToEnemyWithConditionOnFirstPlayerHitEachRound = 68,
        SetEnemyLinkedStatusRoundStartStacks = 69,
        TriggerAdditionalDamageWhenEnemyStatusTicksWithConditionStatus = 70,
        PreventAllyStatusApplication = 71,
        ApplyStatusToLowestSpeedAllyWhenAnyEnemyHasStatusOnBattleRoundStart = 72,
        ReduceIncomingAttackDamageFlatWhenAttackerHasStatus = 73,
        ModifyOutgoingDamagePercentByTargetStatusStacks = 74,
        GrantMoneyOnAcquire = 75,
        IncreasePartyMaxHpOnAcquire = 76,
        RestoreSharedEnergyOnSpecificRound = 77,
        ApplyStatusToFullHpAlliesOnBattleRoundStart = 78,
        ApplyStatusToLowHpAlliesOnBattleRoundStart = 79,
        QueueStatusToAllEnemiesOnPlayerDeathNextRound = 80,
        GrantMoneyOnActionPointsSpent = 81,
        SetMerchantInitialStockCount = 82,
        AllowMerchantPurchaseWithPositiveMoney = 83,
        GuaranteeBattleLootDrop = 84,
        IncreaseRelicChoiceRefreshCount = 85,
        GrantRandomRelicsOnAcquire = 86,
        QueueStatusToPlayerOnClashLostAgainstStatusTarget = 87,
        IncreaseSharedEnergyMaxAtBattleStart = 88,
        GrantMoney = 89,
        IgnoreMoveCostWhenDestinationUnvisitedRestTile = 90,
        GrantTemporarySharedEnergyOnSpecificRound = 91,
        GrantTemporarySharedEnergyEachRound = 92,
        ApplyStatusToTemporarySkillUser = 93,
        PreventBattleDefeatAndFullHealPartyOncePerRun = 94,
        ModifyBattleExperiencePercentAndHealMissingHp = 95,
        SummonTemporaryAllyFromEnemyDataAtBattleStart = 96,
        QueueNextRoundMinPowerBonusOnMinRollOncePerRound = 97,
        HealPartyPercentAfterBattleInSpecialDungeon = 98,
        DealDamageWhenEnemyStatusReachesStacksOncePerRound = 99,
        TransferStatusToAllEnemiesOnEnemyDeath = 100,
        HealRandomDamagedAllyOnEnemyDeathWithStatus = 101,
        ReflectEnemyStatusStacksDamageAfterTakingAttackDamage = 102,
        ApplyStatusToEnemyOnEnemyClashWinWithStatusOncePerRound = 103,
        PromotePlayerOnHitStatusToSkillUse = 104,
        SetEnemyStatusRoundEndDecayPercent = 105,
        PreserveEnemyStatusWhenAllLivingEnemiesHaveStatus = 106,
        PreventEnemyHealingWhenHasStatus = 107,
        PreventLethalDamageByTransferringHighestHpAllyHealth = 108,
        ApplyStatusOnTransferredLethalProtectionWhenEnemyStatusAtLeast = 109,
        ModifyPlayerClashDiceWhenTargetHasStatusOncePerRound = 110,
        ModifyOutgoingDamagePercentWhenTargetHasStatus = 111
    }

    [Serializable]
    public class RelicBuiltInEffectEntry
    {
        public string effectName;
        public RelicTrigger trigger = RelicTrigger.OnMoveCostAboutToSpend;
        public RelicBuiltInEffectType effectType = RelicBuiltInEffectType.None;
        [Range(0f, 1f)] public float chance = 1f;
        public bool requireUnvisitedTile = true;
        public BattleStatus_SO statusData;
        [Min(0)] public int amount = 1;
        public BattleStatus_SO conditionStatusData;
        [Min(1)] public int requiredStatusStacks = 1;
        public int requiredSpeed = 1;
        public int requiredSpeedMin = 1;
        public int requiredSpeedMax = 1;
        public RelicStatusApplyTiming statusApplyTiming = RelicStatusApplyTiming.Immediate;
        [Min(1)] public int applyAfterRoundStarts = 1;
        [Min(1)] public int requiredRound = 1;
        [Min(1)] public int requiredRoundMin = 1;
        [Min(1)] public int requiredRoundMax = 1;
        public int powerAmount;
        public int minPowerAmount;
        public int maxPowerAmount;
        [Min(1)] public int maxStacks = 1;
        [Min(1)] public int requiredDistinctStatusCount = 1;
        [Min(1)] public int requiredConsecutiveSkillUses = 1;
        public RelicTable_SO relicTable;
        public EnemyData_SO enemyData;
    }

    [Header("Identity")]
    public string relicId;
    public string relicName;

    [TextArea(2, 5)]
    public string description;

    [TextArea(2, 5)]
    public string additionalDescription;

    [Header("Presentation")]
    public LootRarity rarity = LootRarity.Common;
    public Sprite icon;

    [Header("Effects")]
    public List<RelicBuiltInEffectEntry> builtInEffects = new List<RelicBuiltInEffectEntry>();
    public RelicEffectHandler_SO customEffectHandler;

    public string DisplayName => !string.IsNullOrWhiteSpace(relicName) ? relicName : name;

    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(relicId))
            relicId = name;
        if (builtInEffects == null)
            builtInEffects = new List<RelicBuiltInEffectEntry>();

        for (int i = 0; i < builtInEffects.Count; i++)
        {
            RelicBuiltInEffectEntry effect = builtInEffects[i];
            if (effect == null)
                continue;

            effect.requiredSpeed = Mathf.Max(0, effect.requiredSpeed);
            effect.requiredSpeedMin = Mathf.Max(0, effect.requiredSpeedMin);
            effect.requiredSpeedMax = Mathf.Max(0, effect.requiredSpeedMax);
            effect.applyAfterRoundStarts = Mathf.Max(1, effect.applyAfterRoundStarts);
            effect.requiredRound = Mathf.Max(1, effect.requiredRound);
            effect.requiredRoundMin = Mathf.Max(1, effect.requiredRoundMin);
            effect.requiredRoundMax = Mathf.Max(1, effect.requiredRoundMax);
            effect.maxStacks = Mathf.Max(1, effect.maxStacks);
            effect.requiredDistinctStatusCount = Mathf.Max(1, effect.requiredDistinctStatusCount);
            effect.requiredConsecutiveSkillUses = Mathf.Max(1, effect.requiredConsecutiveSkillUses);

            if (effect.requiredSpeed != 1 && effect.requiredSpeedMin == 1 && effect.requiredSpeedMax == 1)
            {
                effect.requiredSpeedMin = effect.requiredSpeed;
                effect.requiredSpeedMax = effect.requiredSpeed;
            }

            if (effect.requiredRound != 1 && effect.requiredRoundMin == 1 && effect.requiredRoundMax == 1)
            {
                effect.requiredRoundMin = effect.requiredRound;
                effect.requiredRoundMax = effect.requiredRound;
            }

            if (effect.requiredRoundMax < effect.requiredRoundMin)
                effect.requiredRoundMax = effect.requiredRoundMin;
        }
    }
}
