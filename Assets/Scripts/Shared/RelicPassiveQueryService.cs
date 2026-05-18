public static class RelicPassiveQueryService
{
    public static bool HasBuiltInEffect(
        BattleEncounterData encounter,
        Relic_SO.RelicBuiltInEffectType effectType,
        Relic_SO.RelicTrigger trigger)
    {
        if (encounter == null || encounter.relicInventory == null)
            return false;

        for (int i = 0; i < encounter.relicInventory.Count; i++)
        {
            RunRelicState relicState = encounter.relicInventory[i];
            Relic_SO relic = relicState != null ? relicState.relicData : null;
            if (relic == null || relicState.amount <= 0 || relic.builtInEffects == null)
                continue;

            for (int j = 0; j < relic.builtInEffects.Count; j++)
            {
                Relic_SO.RelicBuiltInEffectEntry effect = relic.builtInEffects[j];
                if (effect != null && effect.trigger == trigger && effect.effectType == effectType)
                    return true;
            }
        }

        return false;
    }
}
