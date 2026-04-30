using System;

public static class BattleRelicProcessDispatcher
{
    public static void Process(
        BattleEncounterData encounter,
        RelicExecutionContext context,
        Action<BattleEncounterData, RelicExecutionContext> process)
    {
        process?.Invoke(encounter, context);
    }
}
