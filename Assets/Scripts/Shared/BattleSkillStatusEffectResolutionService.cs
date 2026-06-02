using System;
using System.Collections.Generic;

public static class BattleSkillStatusEffectResolutionService
{
    public static IReadOnlyList<TStatusEffect> FilterEffects<TStatusEffect>(
        IEnumerable<TStatusEffect> effects,
        bool conditionalOnly,
        Func<TStatusEffect, bool> isConditional)
    {
        List<TStatusEffect> result = new List<TStatusEffect>();
        if (effects == null)
            return result;

        foreach (TStatusEffect effect in effects)
        {
            if (conditionalOnly && (isConditional == null || !isConditional(effect)))
                continue;

            result.Add(effect);
        }

        return result;
    }

    public static IReadOnlyList<BattleIndexedEffect<EnemySkill_SO.StatusEffectEntry>> BuildEligibleStatusEffects(
        EnemySkill_SO.StatusEffectEntry[] effects,
        EnemySkill_SO.DiceEffectTrigger trigger,
        bool conditionalOnly)
    {
        List<BattleIndexedEffect<EnemySkill_SO.StatusEffectEntry>> result = new List<BattleIndexedEffect<EnemySkill_SO.StatusEffectEntry>>();
        if (effects == null)
            return result;

        for (int i = 0; i < effects.Length; i++)
        {
            EnemySkill_SO.StatusEffectEntry effect = effects[i];
            if (!BattleStatusEffectEligibilityService.ShouldProcess(effect, trigger, conditionalOnly))
                continue;

            result.Add(new BattleIndexedEffect<EnemySkill_SO.StatusEffectEntry>(i, effect));
        }

        return result;
    }

    public static bool ShouldEvaluateConditionAgainstCurrentTarget(EnemySkill_SO.DamageTargetMode targetMode)
    {
        return targetMode == EnemySkill_SO.DamageTargetMode.AllOpponentsExceptCurrent;
    }
}
