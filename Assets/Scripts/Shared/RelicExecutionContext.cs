using System.Collections.Generic;
using UnityEngine;

public sealed partial class RelicExecutionContext
{
    // Core routing.
    public Relic_SO.RelicTrigger trigger;
    public RunState runState;

    // Map and run-level context.
    public RunGameManager runGameManager;
    public BoardTileData tile;
    public ActionPointSystem actionPointSystem;
    public int moveCost;
    public bool isDestinationUnvisited;
    public bool ignoreMoveCost;

    // Status application context.
    public BattleStatus_SO statusData;
    public string statusId;
    public string statusName;
    public int statusStacks;
    public int additionalStatusStacks;

    // Generic source/target context.
    public bool sourceIsPlayer;
    public Object source;
    public Object target;
    public EnemySkill_SO skillData;

    // Merchant context.
    public MerchantOfferState merchantOffer;
    public int merchantBasePrice;
    public int merchantDiscountPercent;
    public float merchantPriceMultiplier = 1f;
    public int merchantStockTargetCount;
    public bool allowMerchantPurchaseWithPositiveMoney;

    // Dice and power context.
    public int diceIndex;
    public bool isClashPreparation;
    public int additionalPowerBonus;
    public int additionalMinPowerBonus;
    public int additionalMaxPowerBonus;
    public int additionalTargetMinPowerBonus;

    // Incoming damage context.
    public int incomingDamage;
    public int resolvedDamage;
    public int incomingDamageFlatModifier;
    public int incomingDamagePercentModifier;
    public int incomingSourceSpeed;
    public int incomingTargetSpeed;
    public List<BattleDamageFormulaModifierSource> incomingDamageFlatModifierSources;
    public List<BattleDamageFormulaModifierSource> incomingDamagePercentModifierSources;

    // Outgoing damage context.
    public int outgoingDamage;
    public int outgoingDamageFlatModifier;
    public int outgoingDamagePercentModifier;
    public List<BattleDamageFormulaModifierSource> outgoingDamageFlatModifierSources;
    public List<BattleDamageFormulaModifierSource> outgoingDamagePercentModifierSources;

    // Lethal damage prevention context.
    public bool preventLethalDamage;
    public int minimumSurvivingHp = 1;

    // Reforge context.
    public LootItem_SO firstReforgeLootMaterial;
    public LootItem_SO secondReforgeLootMaterial;
    public LootItem_SO reforgeResultLoot;
    public IReadOnlyList<LootItem_SO> lootReforgePool;
    public Relic_SO firstReforgeRelicMaterial;
    public Relic_SO secondReforgeRelicMaterial;
    public Relic_SO reforgeResultRelic;
    public int preservedReforgeMaterialSlot = -1;
    public List<LootItem_SO> bonusLootReforgeRewards;

    // Battle reward context.
    public LootItem_SO grantedBattleLoot;
    public IReadOnlyList<LootItem_SO> battleLootPool;
    public List<LootItem_SO> bonusBattleLootRewards;
    public bool guaranteeBattleLootDrop;

    // Relic acquisition and reward-choice context.
    public Relic_SO acquiredRelicData;
    public int acquiredRelicAmount;
    public RelicTable_SO relicRewardTable;
    public int additionalRelicChoiceRefreshCount;
    public int actionPointsSpent;

    // Rest and battle setup output context.
    public int baseRestReforgeCount;
    public int additionalRestReforgeCount;
    public int additionalSharedEnergyRecovery;
    public int additionalTemporaryAllySpeedButtons;
    public int additionalSharedEnergyMax;
    public int additionalTemporarySharedEnergy;

    // Round, speed, and once-per-event context.
    public int currentSpeed;
    public int currentRound;
    public string eventInstanceKey;
    public int rolledDiceBasePower;
    public int rolledDiceMinPower;
    public bool rolledDiceWasMinimum;

    // Calamity context.
    public Calamity_SO calamityData;
    public int calamityTier;

    // Battle participants.
    public BattleAllyController[] allies;
    public BattleEnemyController[] enemies;
    public bool battleVictory;
    public bool inSpecialDungeon;
    public int baseBattleExperience;
    public int modifiedBattleExperience;
    public bool preventBattleDefeat;
    public bool fullHealPartyOnDefeat;
    public bool forceReturnToMapOnDefeat;
    public EnemyData_SO summonAllyEnemyData;

    // User-facing feedback collected by relic effects.
    public List<string> feedbackMessages;

    public void AddIncomingDamageFlatModifierSource(int amount, string sourceName)
    {
        if (amount == 0)
            return;

        if (incomingDamageFlatModifierSources == null)
            incomingDamageFlatModifierSources = new List<BattleDamageFormulaModifierSource>();
        incomingDamageFlatModifierSources.Add(new BattleDamageFormulaModifierSource(amount, sourceName));
    }

    public void AddIncomingDamagePercentModifierSource(int amount, string sourceName)
    {
        if (amount == 0)
            return;

        if (incomingDamagePercentModifierSources == null)
            incomingDamagePercentModifierSources = new List<BattleDamageFormulaModifierSource>();
        incomingDamagePercentModifierSources.Add(new BattleDamageFormulaModifierSource(amount, sourceName));
    }

    public void AddOutgoingDamageFlatModifierSource(int amount, string sourceName)
    {
        if (amount == 0)
            return;

        if (outgoingDamageFlatModifierSources == null)
            outgoingDamageFlatModifierSources = new List<BattleDamageFormulaModifierSource>();
        outgoingDamageFlatModifierSources.Add(new BattleDamageFormulaModifierSource(amount, sourceName));
    }

    public void AddOutgoingDamagePercentModifierSource(int amount, string sourceName)
    {
        if (amount == 0)
            return;

        if (outgoingDamagePercentModifierSources == null)
            outgoingDamagePercentModifierSources = new List<BattleDamageFormulaModifierSource>();
        outgoingDamagePercentModifierSources.Add(new BattleDamageFormulaModifierSource(amount, sourceName));
    }
}
