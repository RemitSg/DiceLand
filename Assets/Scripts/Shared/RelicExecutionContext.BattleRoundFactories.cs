using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

public sealed partial class RelicExecutionContext
{
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
