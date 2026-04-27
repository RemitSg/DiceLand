using UnityEngine;

public static class RelicRestReforgeService
{
    public static int CalculateLimit(RunState runState, int baseLimit)
    {
        int resolvedBaseLimit = Mathf.Max(0, baseLimit);
        if (runState == null)
            return resolvedBaseLimit;

        RelicExecutionContext context = RelicExecutionContext.CreateRestReforgeLimitCalculation(runState, resolvedBaseLimit);
        RelicSystem.Process(runState, Relic_SO.RelicTrigger.OnRestReforgeLimitCalculated, context);
        return Mathf.Max(0, context.baseRestReforgeCount + context.additionalRestReforgeCount);
    }
}
