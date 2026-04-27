using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

public sealed partial class RelicExecutionContext
{
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

    public static RelicExecutionContext CreatePlayerTookSingleAttackDamage(
        RunState runState,
        EnemySkill_SO skillData,
        int diceIndex,
        int resolvedDamage,
        int incomingSourceSpeed,
        int incomingTargetSpeed,
        int currentRound,
        string eventInstanceKey,
        Object source,
        Object target)
    {
        return new RelicExecutionContext
        {
            trigger = Relic_SO.RelicTrigger.OnPlayerTookSingleAttackDamage,
            runState = runState,
            sourceIsPlayer = false,
            source = source,
            target = target,
            skillData = skillData,
            diceIndex = diceIndex,
            resolvedDamage = Mathf.Max(0, resolvedDamage),
            incomingSourceSpeed = Mathf.Max(0, incomingSourceSpeed),
            incomingTargetSpeed = Mathf.Max(0, incomingTargetSpeed),
            currentRound = Mathf.Max(1, currentRound),
            eventInstanceKey = eventInstanceKey,
            feedbackMessages = new List<string>()
        };
    }

    public static RelicExecutionContext CreatePlayerHitEnemy(
        RunState runState,
        EnemySkill_SO skillData,
        int diceIndex,
        int resolvedDamage,
        int sourceSpeed,
        int targetSpeed,
        int currentRound,
        string eventInstanceKey,
        Object source,
        Object target)
    {
        return new RelicExecutionContext
        {
            trigger = Relic_SO.RelicTrigger.OnPlayerHitEnemy,
            runState = runState,
            sourceIsPlayer = true,
            source = source,
            target = target,
            skillData = skillData,
            diceIndex = diceIndex,
            resolvedDamage = Mathf.Max(0, resolvedDamage),
            incomingSourceSpeed = Mathf.Max(0, sourceSpeed),
            incomingTargetSpeed = Mathf.Max(0, targetSpeed),
            currentRound = Mathf.Max(1, currentRound),
            eventInstanceKey = eventInstanceKey,
            feedbackMessages = new List<string>()
        };
    }

    public static RelicExecutionContext CreatePlayerDefenseClashWin(
        RunState runState,
        EnemySkill_SO skillData,
        int diceIndex,
        int currentRound,
        Object source,
        Object target)
    {
        return new RelicExecutionContext
        {
            trigger = Relic_SO.RelicTrigger.OnPlayerDefenseClashWin,
            runState = runState,
            sourceIsPlayer = true,
            source = source,
            target = target,
            skillData = skillData,
            diceIndex = diceIndex,
            currentRound = Mathf.Max(1, currentRound),
            feedbackMessages = new List<string>()
        };
    }

    public static RelicExecutionContext CreateEnemyKilledByPlayer(
        RunState runState,
        EnemySkill_SO skillData,
        int diceIndex,
        int resolvedDamage,
        Object source,
        Object target)
    {
        return new RelicExecutionContext
        {
            trigger = Relic_SO.RelicTrigger.OnEnemyKilledByPlayer,
            runState = runState,
            sourceIsPlayer = true,
            source = source,
            target = target,
            skillData = skillData,
            diceIndex = diceIndex,
            resolvedDamage = Mathf.Max(0, resolvedDamage),
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

    public static RelicExecutionContext CreatePlayerAboutToTakeDamage(
        RunState runState,
        EnemySkill_SO skillData,
        int diceIndex,
        int incomingDamage,
        int currentSpeed,
        Object source,
        Object target)
    {
        return new RelicExecutionContext
        {
            trigger = Relic_SO.RelicTrigger.OnPlayerAboutToTakeDamage,
            runState = runState,
            sourceIsPlayer = false,
            source = source,
            target = target,
            skillData = skillData,
            diceIndex = diceIndex,
            incomingDamage = Mathf.Max(0, incomingDamage),
            currentSpeed = Mathf.Max(0, currentSpeed),
            minimumSurvivingHp = 1,
            feedbackMessages = new List<string>()
        };
    }

    public static RelicExecutionContext CreatePlayerAboutToDealDamage(
        RunState runState,
        EnemySkill_SO skillData,
        int diceIndex,
        int outgoingDamage,
        Object source,
        Object target)
    {
        return new RelicExecutionContext
        {
            trigger = Relic_SO.RelicTrigger.OnPlayerAboutToDealDamage,
            runState = runState,
            sourceIsPlayer = true,
            source = source,
            target = target,
            skillData = skillData,
            diceIndex = diceIndex,
            outgoingDamage = Mathf.Max(0, outgoingDamage),
            feedbackMessages = new List<string>()
        };
    }

    public static RelicExecutionContext CreateEnemyDeath(
        RunState runState,
        BattleEnemyController defeatedEnemy,
        BattleAllyController[] allies,
        BattleEnemyController[] enemies)
    {
        return new RelicExecutionContext
        {
            trigger = Relic_SO.RelicTrigger.OnEnemyDeath,
            runState = runState,
            sourceIsPlayer = false,
            target = defeatedEnemy,
            allies = allies,
            enemies = enemies,
            feedbackMessages = new List<string>()
        };
    }

    public static RelicExecutionContext CreatePlayerAboutToTakeLethalDamage(
        RunState runState,
        int incomingDamage,
        Object source,
        Object target)
    {
        return new RelicExecutionContext
        {
            trigger = Relic_SO.RelicTrigger.OnPlayerAboutToTakeLethalDamage,
            runState = runState,
            sourceIsPlayer = false,
            source = source,
            target = target,
            incomingDamage = Mathf.Max(0, incomingDamage),
            minimumSurvivingHp = 1,
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

    public static RelicExecutionContext CreateBattleSetup(
        RunState runState,
        int currentRound,
        BattleAllyController[] allies,
        BattleEnemyController[] enemies)
    {
        return new RelicExecutionContext
        {
            trigger = Relic_SO.RelicTrigger.OnBattleSetup,
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
}
