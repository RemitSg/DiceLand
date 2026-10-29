using UnityEngine;

public static partial class RelicSystem
{
    public static bool TryResolveEnemyStatusReplacement(
        BattleStatus_SO incomingStatus, string incomingStatusId, int incomingStacks,
        out BattleResolvedStatus replacement, out int replacementStacks)
    {
        replacement = default;
        replacementStacks = 0;
        BattleEncounterData encounter = BattleEncounterRuntime.ActiveEncounter;
        if (incomingStacks <= 0 || encounter == null || encounter.relicInventory == null)
            return false;

        foreach (RunRelicState state in encounter.relicInventory)
        {
            if (state == null || state.amount <= 0 || state.relicData == null || state.relicData.builtInEffects == null)
                continue;

            foreach (Relic_SO.RelicBuiltInEffectEntry effect in state.relicData.builtInEffects)
            {
                if (effect == null || effect.effectType != Relic_SO.RelicBuiltInEffectType.ReplaceEnemyStatusApplication
                    || effect.conditionStatusData == null || effect.statusData == null || effect.amount <= 0)
                    continue;
                if (!StatusMatches(effect.conditionStatusData, incomingStatus, incomingStatusId)
                    || StatusMatches(effect.statusData, incomingStatus, incomingStatusId))
                    continue;

                // Replace one application, not each incoming stack or each copy
                // of the relic. Resolve only one hop to prevent conversion loops.
                replacement = new BattleResolvedStatus(effect.statusData, effect.statusData.StatusId, effect.statusData.DisplayName);
                replacementStacks = effect.amount;
                return true;
            }
        }

        return false;
    }
}
