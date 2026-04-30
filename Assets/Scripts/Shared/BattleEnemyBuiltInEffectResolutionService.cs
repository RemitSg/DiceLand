using System;
using System.Collections.Generic;

public enum BattleEnemyBuiltInEffectKind
{
    Heal = 0,
    Damage = 1,
    Status = 2,
    Transfer = 3
}

public static class BattleEnemyBuiltInEffectResolutionService
{
    public static IReadOnlyList<TEffect> FilterByTrigger<TEffect>(
        IEnumerable<TEffect> effects,
        EnemySkill_SO.DiceEffectTrigger trigger,
        Func<TEffect, EnemySkill_SO.DiceEffectTrigger> getTrigger)
    {
        List<TEffect> result = new List<TEffect>();
        if (effects == null || getTrigger == null)
            return result;

        foreach (TEffect effect in effects)
        {
            if (getTrigger(effect) == trigger)
                result.Add(effect);
        }

        return result;
    }

    public static IReadOnlyList<BattleIndexedEffect<EnemySkill_SO.BuiltInEffectEntry>> BuildEligibleEffects(
        EnemySkill_SO.BuiltInEffectEntry[] effects,
        EnemySkill_SO.DiceEffectTrigger trigger,
        Func<EnemySkill_SO.BuiltInEffectEntry, bool> isConditionMet)
    {
        List<BattleIndexedEffect<EnemySkill_SO.BuiltInEffectEntry>> result = new List<BattleIndexedEffect<EnemySkill_SO.BuiltInEffectEntry>>();
        if (effects == null)
            return result;

        for (int i = 0; i < effects.Length; i++)
        {
            EnemySkill_SO.BuiltInEffectEntry effect = effects[i];
            if (!BattleEnemyBuiltInEffectEligibilityService.ShouldProcess(effect, trigger))
                continue;
            if (isConditionMet != null && !isConditionMet(effect))
                continue;

            result.Add(new BattleIndexedEffect<EnemySkill_SO.BuiltInEffectEntry>(i, effect));
        }

        return result;
    }
}
