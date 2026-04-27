using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

public sealed partial class RelicExecutionContext
{
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

}
