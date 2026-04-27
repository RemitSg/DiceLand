using System.Collections.Generic;

public static partial class RelicSystem
{
    public static void Process(RunState runState, Relic_SO.RelicTrigger trigger, RelicExecutionContext context)
    {
        if (runState == null)
            return;

        context.trigger = trigger;
        context.runState = runState;
        Process(runState.relicInventory, context);
    }

    public static void Process(IReadOnlyList<RunRelicState> relicInventory, RelicExecutionContext context)
    {
        if (context == null || relicInventory == null || relicInventory.Count == 0)
            return;

        for (int i = 0; i < relicInventory.Count; i++)
        {
            RunRelicState relicState = relicInventory[i];
            if (relicState == null || relicState.relicData == null || relicState.amount <= 0)
                continue;

            ApplyBuiltInEffects(relicState.relicData, relicState, context);

            if (relicState.relicData.customEffectHandler == null)
                continue;

            relicState.relicData.customEffectHandler.TryApply(relicState.relicData, relicState, context, out _);
        }
    }
}
