using System;

public static class BattleRelicBridge
{
    public static bool Process(
        BattleEncounterData encounter,
        RelicExecutionContext context,
        Action<BattleEncounterData, RelicExecutionContext> process)
    {
        if (!BattleEncounterRelicAvailabilityService.HasBattleRelics(encounter))
            return false;

        process?.Invoke(encounter, context);
        return true;
    }

    public static bool ProcessAndFlush(
        BattleEncounterData encounter,
        RelicExecutionContext context,
        Action<BattleEncounterData, RelicExecutionContext> process,
        Action<RelicExecutionContext> flush)
    {
        if (!Process(encounter, context, process))
            return false;

        flush?.Invoke(context);
        return true;
    }
}
