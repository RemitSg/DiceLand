using System;

public static partial class RelicTriggerDispatcher
{
    public static void ProcessRunRelics(RunState runState, RelicExecutionContext context, Action<string> logFeedback = null)
    {
        if (runState == null || context == null)
            return;

        if (context.runState == null)
            context.runState = runState;

        RelicSystem.Process(runState.relicInventory, context);
        FlushFeedback(context, logFeedback);
    }

    public static void ProcessBattleRelics(BattleEncounterData encounter, RelicExecutionContext context, Action<string> logFeedback = null)
    {
        if (encounter == null || context == null)
            return;

        RelicSystem.Process(encounter.relicInventory, context);
        FlushFeedback(context, logFeedback);
    }

    public static void FlushFeedback(RelicExecutionContext context, Action<string> logFeedback)
    {
        if (context == null || context.feedbackMessages == null || logFeedback == null)
            return;

        for (int i = 0; i < context.feedbackMessages.Count; i++)
        {
            if (!string.IsNullOrWhiteSpace(context.feedbackMessages[i]))
                logFeedback(context.feedbackMessages[i]);
        }
    }
}
