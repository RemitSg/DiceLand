public static class RelicFixedReforgeService
{
    public static bool TryResolveFixedResult(
        RunState runState,
        Relic_SO firstRelic,
        Relic_SO secondRelic,
        out Relic_SO resultRelic)
    {
        resultRelic = null;
        if (firstRelic == null || secondRelic == null || firstRelic == secondRelic)
            return false;

        if (!TryGetConfiguredFixedResult(firstRelic, out Relic_SO firstResult))
            return false;
        if (!TryGetConfiguredFixedResult(secondRelic, out Relic_SO secondResult))
            return false;
        if (firstResult == null || firstResult != secondResult)
            return false;

        resultRelic = firstResult;
        return true;
    }

    public static bool IsFixedResult(
        RunState runState,
        Relic_SO firstRelic,
        Relic_SO secondRelic,
        Relic_SO resultRelic)
    {
        return TryResolveFixedResult(runState, firstRelic, secondRelic, out Relic_SO fixedResult)
            && fixedResult == resultRelic;
    }

    private static bool TryGetConfiguredFixedResult(Relic_SO relicData, out Relic_SO resultRelic)
    {
        resultRelic = null;
        if (relicData == null || relicData.builtInEffects == null)
            return false;

        for (int i = 0; i < relicData.builtInEffects.Count; i++)
        {
            Relic_SO.RelicBuiltInEffectEntry effect = relicData.builtInEffects[i];
            if (effect == null
                || effect.effectType != Relic_SO.RelicBuiltInEffectType.ConfigureFixedRelicReforgePair
                || effect.relicData == null)
            {
                continue;
            }

            resultRelic = effect.relicData;
            return true;
        }

        return false;
    }
}
