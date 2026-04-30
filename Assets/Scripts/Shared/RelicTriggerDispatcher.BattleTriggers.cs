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
}
