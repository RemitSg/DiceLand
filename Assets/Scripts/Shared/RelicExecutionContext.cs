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
    public int additionalPowerBonus;
    public int additionalMinPowerBonus;
    public int additionalMaxPowerBonus;
    public int incomingDamage;
    public int incomingDamageFlatModifier;
    public int currentSpeed;
    public int currentRound;
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
            feedbackMessages = new List<string>()
        };
    }

    public static RelicExecutionContext CreatePlayerAboutToTakeSingleAttackDamage(
        RunState runState,
        EnemySkill_SO skillData,
        int diceIndex,
        int incomingDamage,
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
        BattleEnemyController[] enemies)
    {
        return new RelicExecutionContext
        {
            trigger = Relic_SO.RelicTrigger.OnBattleRoundStart,
            runState = runState,
            currentRound = Mathf.Max(1, currentRound),
            enemies = enemies,
            feedbackMessages = new List<string>()
        };
    }
}
