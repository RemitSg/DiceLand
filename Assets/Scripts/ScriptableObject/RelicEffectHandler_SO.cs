using UnityEngine;

public abstract class RelicEffectHandler_SO : ScriptableObject
{
    public abstract bool TryApply(Relic_SO relicData, RunRelicState relicState, RelicExecutionContext context, out string message);
}
