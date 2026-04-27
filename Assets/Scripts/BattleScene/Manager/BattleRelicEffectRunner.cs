using System;

public static class BattleRelicEffectRunner
{
    public static bool HasRelics(BattleEncounterData encounter)
    {
        return encounter != null
            && encounter.relicInventory != null
            && encounter.relicInventory.Count > 0;
    }

    public static void Process(BattleEncounterData encounter, RelicExecutionContext context, Action<string> logFeedback = null)
    {
        if (!HasRelics(encounter) || context == null)
            return;

        RelicTriggerDispatcher.ProcessBattleRelics(encounter, context, logFeedback);
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
