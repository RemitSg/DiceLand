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

    public static RelicExecutionContext CreatePlayerClashLost(
        RunState runState,
        EnemySkill_SO skillData,
        int diceIndex,
        Object source,
        Object target)
    {
        return new RelicExecutionContext
        {
            trigger = Relic_SO.RelicTrigger.OnPlayerClashLost,
            runState = runState,
            sourceIsPlayer = true,
            skillData = skillData,
            diceIndex = Mathf.Max(0, diceIndex),
            source = source,
            target = target,
            feedbackMessages = new List<string>()
        };
    }

    public static RelicExecutionContext CreatePlayerSkillUsed(
        RunState runState,
        EnemySkill_SO skillData,
        Object source,
        Object target)
    {
        return new RelicExecutionContext
        {
            trigger = Relic_SO.RelicTrigger.OnPlayerSkillUsed,
            runState = runState,
            sourceIsPlayer = true,
            skillData = skillData,
            source = source,
            target = target,
            feedbackMessages = new List<string>()
        };
    }

    public static RelicExecutionContext CreatePlayerDiceRolled(
        RunState runState,
        EnemySkill_SO skillData,
        int diceIndex,
        int rolledDiceBasePower,
        int rolledDiceMinPower,
        Object source,
        Object target)
    {
        return new RelicExecutionContext
        {
            trigger = Relic_SO.RelicTrigger.OnPlayerDiceRolled,
            runState = runState,
            sourceIsPlayer = true,
            skillData = skillData,
            diceIndex = Mathf.Max(0, diceIndex),
            rolledDiceBasePower = rolledDiceBasePower,
            rolledDiceMinPower = rolledDiceMinPower,
            rolledDiceWasMinimum = rolledDiceBasePower <= rolledDiceMinPower,
            source = source,
            target = target,
            feedbackMessages = new List<string>()
        };
    }

    public static RelicExecutionContext CreateBattleCompletion(
        RunState runState,
        bool victory,
        int baseBattleExperience,
        BattleAllyController[] allies,
        BattleEnemyController[] enemies,
        bool inSpecialDungeon)
    {
        int safeExperience = Mathf.Max(0, baseBattleExperience);
        return new RelicExecutionContext
        {
            trigger = Relic_SO.RelicTrigger.OnBattleCompletion,
            runState = runState,
            battleVictory = victory,
            baseBattleExperience = safeExperience,
            modifiedBattleExperience = safeExperience,
            allies = allies,
            enemies = enemies,
            inSpecialDungeon = inSpecialDungeon,
            feedbackMessages = new List<string>()
        };
    }

    public static RelicExecutionContext CreateBattleDefeat(
        RunState runState,
        BattleAllyController[] allies,
        BattleEnemyController[] enemies)
    {
        return new RelicExecutionContext
        {
            trigger = Relic_SO.RelicTrigger.OnBattleDefeat,
            runState = runState,
            battleVictory = false,
            allies = allies,
            enemies = enemies,
            feedbackMessages = new List<string>()
        };
    }
}
