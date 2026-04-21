using UnityEngine;
using System.Collections.Generic;

public sealed class RelicExecutionContext
{
    public Relic_SO.RelicTrigger trigger;
    public RunState runState;
    public RunGameManager runGameManager;
    public BoardTileData tile;
    public int moveCost;
    public bool isDestinationUnvisited;
    public bool ignoreMoveCost;
    public BattleStatus_SO statusData;
    public int statusStacks;
    public int additionalStatusStacks;
    public bool sourceIsPlayer;
    public Object source;
    public Object target;
    public EnemySkill_SO skillData;
    public MerchantOfferState merchantOffer;
    public int merchantBasePrice;
    public int merchantDiscountPercent;
    public int diceIndex;
    public bool isClashPreparation;
    public int additionalPowerBonus;
    public int additionalMinPowerBonus;
    public int additionalMaxPowerBonus;
    public int additionalTargetMinPowerBonus;
    public int incomingDamage;
    public int incomingDamageFlatModifier;
    public int incomingDamagePercentModifier;
    public int incomingSourceSpeed;
    public int incomingTargetSpeed;
    public LootItem_SO firstReforgeLootMaterial;
    public LootItem_SO secondReforgeLootMaterial;
    public LootItem_SO reforgeResultLoot;
    public IReadOnlyList<LootItem_SO> lootReforgePool;
    public Relic_SO firstReforgeRelicMaterial;
    public Relic_SO secondReforgeRelicMaterial;
    public Relic_SO reforgeResultRelic;
    public int preservedReforgeMaterialSlot = -1;
    public List<LootItem_SO> bonusLootReforgeRewards;
    public int baseRestReforgeCount;
    public int additionalRestReforgeCount;
    public int currentSpeed;
    public int currentRound;
    public BattleAllyController[] allies;
    public BattleEnemyController[] enemies;
    public List<string> feedbackMessages;

    public static RelicExecutionContext CreateMove(RunState runState, RunGameManager runGameManager, BoardTileData tile, int moveCost, bool isDestinationUnvisited)
    {
        return new RelicExecutionContext
        {
            trigger = Relic_SO.RelicTrigger.OnMoveCostAboutToSpend,
            runState = runState,
            runGameManager = runGameManager,
            tile = tile,
            moveCost = moveCost,
            isDestinationUnvisited = isDestinationUnvisited,
            feedbackMessages = new List<string>()
        };
    }

    public static RelicExecutionContext CreateStatusApplied(
        RunState runState,
        RunGameManager runGameManager,
        BattleStatus_SO statusData,
        int statusStacks,
        bool sourceIsPlayer,
        Object source,
        Object target)
    {
        return new RelicExecutionContext
        {
            trigger = Relic_SO.RelicTrigger.OnStatusApplied,
            runState = runState,
            runGameManager = runGameManager,
            statusData = statusData,
            statusStacks = Mathf.Max(0, statusStacks),
            sourceIsPlayer = sourceIsPlayer,
            source = source,
            target = target,
            feedbackMessages = new List<string>()
        };
    }

    public static RelicExecutionContext CreatePlayerDicePrepared(
        RunState runState,
        EnemySkill_SO skillData,
        int diceIndex,
        bool isClashPreparation,
        Object source,
        Object target)
    {
        return new RelicExecutionContext
        {
            trigger = Relic_SO.RelicTrigger.OnPlayerDicePrepared,
            runState = runState,
            sourceIsPlayer = true,
            source = source,
            target = target,
            skillData = skillData,
            diceIndex = diceIndex,
            isClashPreparation = isClashPreparation,
            feedbackMessages = new List<string>()
        };
    }

    public static RelicExecutionContext CreatePlayerAboutToTakeSingleAttackDamage(
        RunState runState,
        EnemySkill_SO skillData,
        int diceIndex,
        int incomingDamage,
        int incomingSourceSpeed,
        int incomingTargetSpeed,
        Object source,
        Object target)
    {
        return new RelicExecutionContext
        {
            trigger = Relic_SO.RelicTrigger.OnPlayerAboutToTakeSingleAttackDamage,
            runState = runState,
            sourceIsPlayer = false,
            source = source,
            target = target,
            skillData = skillData,
            diceIndex = diceIndex,
            incomingDamage = Mathf.Max(0, incomingDamage),
            incomingSourceSpeed = Mathf.Max(0, incomingSourceSpeed),
            incomingTargetSpeed = Mathf.Max(0, incomingTargetSpeed),
            feedbackMessages = new List<string>()
        };
    }

    public static RelicExecutionContext CreatePlayerAboutToTakeAttackDamage(
        RunState runState,
        EnemySkill_SO skillData,
        int diceIndex,
        int incomingDamage,
        int incomingSourceSpeed,
        int incomingTargetSpeed,
        Object source,
        Object target)
    {
        return new RelicExecutionContext
        {
            trigger = Relic_SO.RelicTrigger.OnPlayerAboutToTakeAttackDamage,
            runState = runState,
            sourceIsPlayer = false,
            source = source,
            target = target,
            skillData = skillData,
            diceIndex = diceIndex,
            incomingDamage = Mathf.Max(0, incomingDamage),
            incomingSourceSpeed = Mathf.Max(0, incomingSourceSpeed),
            incomingTargetSpeed = Mathf.Max(0, incomingTargetSpeed),
            feedbackMessages = new List<string>()
        };
    }

    public static RelicExecutionContext CreateMerchantPriceCalculation(
        RunState runState,
        MerchantOfferState merchantOffer,
        int merchantBasePrice)
    {
        return new RelicExecutionContext
        {
            trigger = Relic_SO.RelicTrigger.OnMerchantPriceCalculated,
            runState = runState,
            merchantOffer = merchantOffer,
            merchantBasePrice = Mathf.Max(0, merchantBasePrice),
            feedbackMessages = new List<string>()
        };
    }

    public static RelicExecutionContext CreatePlayerRoundStart(
        RunState runState,
        Object source,
        int currentSpeed,
        int currentRound = 1)
    {
        return new RelicExecutionContext
        {
            trigger = Relic_SO.RelicTrigger.OnPlayerRoundStart,
            runState = runState,
            sourceIsPlayer = true,
            source = source,
            currentSpeed = Mathf.Max(0, currentSpeed),
            currentRound = Mathf.Max(1, currentRound),
            feedbackMessages = new List<string>()
        };
    }

    public static RelicExecutionContext CreateBattleRoundStart(
        RunState runState,
        int currentRound,
        BattleAllyController[] allies,
        BattleEnemyController[] enemies)
    {
        return new RelicExecutionContext
        {
            trigger = Relic_SO.RelicTrigger.OnBattleRoundStart,
            runState = runState,
            currentRound = Mathf.Max(1, currentRound),
            allies = allies,
            enemies = enemies,
            feedbackMessages = new List<string>()
        };
    }

    public static RelicExecutionContext CreatePlayerDeath(
        RunState runState,
        BattleAllyController defeatedAlly,
        BattleAllyController[] allies)
    {
        return new RelicExecutionContext
        {
            trigger = Relic_SO.RelicTrigger.OnPlayerDeath,
            runState = runState,
            sourceIsPlayer = true,
            target = defeatedAlly,
            allies = allies,
            feedbackMessages = new List<string>()
        };
    }

    public static RelicExecutionContext CreateBattleRoundEnd(
        RunState runState,
        int currentRound,
        BattleAllyController[] allies,
        BattleEnemyController[] enemies)
    {
        return new RelicExecutionContext
        {
            trigger = Relic_SO.RelicTrigger.OnBattleRoundEnd,
            runState = runState,
            currentRound = Mathf.Max(1, currentRound),
            allies = allies,
            enemies = enemies,
            feedbackMessages = new List<string>()
        };
    }

    public static RelicExecutionContext CreateLootReforge(
        RunState runState,
        LootItem_SO firstLootMaterial,
        LootItem_SO secondLootMaterial,
        LootItem_SO resultLoot,
        IReadOnlyList<LootItem_SO> lootPool)
    {
        return new RelicExecutionContext
        {
            trigger = Relic_SO.RelicTrigger.OnLootReforge,
            runState = runState,
            firstReforgeLootMaterial = firstLootMaterial,
            secondReforgeLootMaterial = secondLootMaterial,
            reforgeResultLoot = resultLoot,
            lootReforgePool = lootPool,
            bonusLootReforgeRewards = new List<LootItem_SO>(),
            feedbackMessages = new List<string>()
        };
    }

    public static RelicExecutionContext CreateRelicReforge(
        RunState runState,
        Relic_SO firstRelicMaterial,
        Relic_SO secondRelicMaterial,
        Relic_SO resultRelic)
    {
        return new RelicExecutionContext
        {
            trigger = Relic_SO.RelicTrigger.OnRelicReforge,
            runState = runState,
            firstReforgeRelicMaterial = firstRelicMaterial,
            secondReforgeRelicMaterial = secondRelicMaterial,
            reforgeResultRelic = resultRelic,
            preservedReforgeMaterialSlot = -1,
            feedbackMessages = new List<string>()
        };
    }

    public static RelicExecutionContext CreateRestReforgeLimitCalculation(
        RunState runState,
        int baseRestReforgeCount)
    {
        return new RelicExecutionContext
        {
            trigger = Relic_SO.RelicTrigger.OnRestReforgeLimitCalculated,
            runState = runState,
            baseRestReforgeCount = Mathf.Max(0, baseRestReforgeCount),
            feedbackMessages = new List<string>()
        };
    }
}
