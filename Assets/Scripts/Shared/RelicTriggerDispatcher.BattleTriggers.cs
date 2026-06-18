using System;
using Object = UnityEngine.Object;

public static partial class RelicTriggerDispatcher
{
    public static RelicExecutionContext ProcessStatusApplied(
        BattleEncounterData encounter,
        RunState runState,
        BattleStatus_SO statusData,
        int statusStacks,
        bool sourceIsPlayer,
        Object source,
        Object target,
        string statusId = null,
        string statusName = null,
        Action<string> logFeedback = null)
    {
        RelicExecutionContext context = RelicExecutionContext.CreateStatusApplied(
            runState,
            null,
            statusData,
            statusStacks,
            sourceIsPlayer,
            source,
            target,
            statusId,
            statusName);
        ProcessBattleRelics(encounter, context, logFeedback);
        return context;
    }

    public static RelicExecutionContext ProcessPlayerDicePrepared(
        BattleEncounterData encounter,
        RunState runState,
        EnemySkill_SO skillData,
        int diceIndex,
        bool isClashPreparation,
        Object source,
        Object target,
        Action<string> logFeedback = null)
    {
        RelicExecutionContext context = RelicExecutionContext.CreatePlayerDicePrepared(
            runState,
            skillData,
            diceIndex,
            isClashPreparation,
            source,
            target);
        ProcessBattleRelics(encounter, context, logFeedback);
        return context;
    }

    public static RelicExecutionContext ProcessBattleRoundStart(
        BattleEncounterData encounter,
        RunState runState,
        int currentRound,
        BattleAllyController[] allies,
        BattleEnemyController[] enemies,
        Action<string> logFeedback = null)
    {
        RelicExecutionContext context = RelicExecutionContext.CreateBattleRoundStart(
            runState,
            currentRound,
            allies,
            enemies);
        ProcessBattleRelics(encounter, context, logFeedback);
        return context;
    }

    public static RelicExecutionContext ProcessBattleSetup(
        BattleEncounterData encounter,
        RunState runState,
        int currentRound,
        BattleAllyController[] allies,
        BattleEnemyController[] enemies,
        Action<string> logFeedback = null)
    {
        RelicExecutionContext context = RelicExecutionContext.CreateBattleSetup(
            runState,
            currentRound,
            allies,
            enemies);
        ProcessBattleRelics(encounter, context, logFeedback);
        return context;
    }

    public static RelicExecutionContext ProcessBattleRoundEnd(
        BattleEncounterData encounter,
        RunState runState,
        int currentRound,
        BattleAllyController[] allies,
        BattleEnemyController[] enemies,
        Action<string> logFeedback = null)
    {
        RelicExecutionContext context = RelicExecutionContext.CreateBattleRoundEnd(
            runState,
            currentRound,
            allies,
            enemies);
        ProcessBattleRelics(encounter, context, logFeedback);
        return context;
    }

    public static RelicExecutionContext ProcessPlayerClashLost(
        BattleEncounterData encounter,
        RunState runState,
        EnemySkill_SO skillData,
        int diceIndex,
        Object source,
        Object target,
        int currentRound = 1,
        Action<string> logFeedback = null)
    {
        RelicExecutionContext context = RelicExecutionContext.CreatePlayerClashLost(
            runState,
            skillData,
            diceIndex,
            source,
            target);
        context.currentRound = Math.Max(1, currentRound);
        ProcessBattleRelics(encounter, context, logFeedback);
        return context;
    }

    public static RelicExecutionContext ProcessPlayerSkillUsed(
        BattleEncounterData encounter,
        RunState runState,
        EnemySkill_SO skillData,
        Object source,
        Object target,
        Action<string> logFeedback = null)
    {
        RelicExecutionContext context = RelicExecutionContext.CreatePlayerSkillUsed(
            runState,
            skillData,
            source,
            target);
        ProcessBattleRelics(encounter, context, logFeedback);
        return context;
    }

    public static RelicExecutionContext ProcessPlayerDiceRolled(
        BattleEncounterData encounter,
        RunState runState,
        EnemySkill_SO skillData,
        int diceIndex,
        int rolledDiceBasePower,
        int rolledDiceMinPower,
        Object source,
        Object target,
        Action<string> logFeedback = null)
    {
        RelicExecutionContext context = RelicExecutionContext.CreatePlayerDiceRolled(
            runState,
            skillData,
            diceIndex,
            rolledDiceBasePower,
            rolledDiceMinPower,
            source,
            target);
        ProcessBattleRelics(encounter, context, logFeedback);
        return context;
    }

    public static RelicExecutionContext ProcessBattleCompletion(
        BattleEncounterData encounter,
        RunState runState,
        bool victory,
        int baseBattleExperience,
        BattleAllyController[] allies,
        BattleEnemyController[] enemies,
        bool inSpecialDungeon,
        Action<string> logFeedback = null)
    {
        RelicExecutionContext context = RelicExecutionContext.CreateBattleCompletion(
            runState,
            victory,
            baseBattleExperience,
            allies,
            enemies,
            inSpecialDungeon);
        ProcessBattleRelics(encounter, context, logFeedback);
        return context;
    }

    public static RelicExecutionContext ProcessBattleDefeat(
        BattleEncounterData encounter,
        RunState runState,
        BattleAllyController[] allies,
        BattleEnemyController[] enemies,
        Action<string> logFeedback = null)
    {
        RelicExecutionContext context = RelicExecutionContext.CreateBattleDefeat(
            runState,
            allies,
            enemies);
        ProcessBattleRelics(encounter, context, logFeedback);
        return context;
    }
}
