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

public static class BattleEnemyBuiltInEffectDispatcher
{
    public static void Dispatch<TOpponent>(
        IReadOnlyList<BattleIndexedEffect<EnemySkill_SO.BuiltInEffectEntry>> effects,
        TOpponent currentTarget,
        IReadOnlyList<TOpponent> opponents,
        Func<EnemySkill_SO.BuiltInEffectEntry, int, string> buildLabel,
        Func<EnemySkill_SO.DamageTargetMode, TOpponent, IReadOnlyList<TOpponent>, int, List<TOpponent>> selectTargets,
        Func<TOpponent, bool> canTarget,
        Action<EnemySkill_SO.BuiltInEffectEntry, int, string> applySelfHeal,
        Action<TOpponent, EnemySkill_SO.BuiltInEffectEntry, int, string> applyTargetHeal,
        Action<EnemySkill_SO.BuiltInEffectEntry, int, string> applyFriendlyHeal,
        Action<EnemySkill_SO.BuiltInEffectEntry, int, string> applySelfDamage,
        Action<TOpponent, EnemySkill_SO.BuiltInEffectEntry, int, string> applyTargetDamage,
        Action<EnemySkill_SO.BuiltInEffectEntry, int, string> applyFriendlyDamage,
        Action<EnemySkill_SO.BuiltInEffectEntry, int, string> applySelfStatus,
        Action<TOpponent, EnemySkill_SO.BuiltInEffectEntry, int, string> applyTargetStatus,
        Action<EnemySkill_SO.BuiltInEffectEntry, int, string> applyFriendlyStatus,
        Action<IReadOnlyList<TOpponent>, EnemySkill_SO.BuiltInEffectEntry, int, string> applyTransfer)
    {
        if (effects == null)
            return;

        for (int i = 0; i < effects.Count; i++)
        {
            BattleIndexedEffect<EnemySkill_SO.BuiltInEffectEntry> indexedEffect = effects[i];
            EnemySkill_SO.BuiltInEffectEntry effect = indexedEffect.Effect;
            string effectLabel = buildLabel != null ? buildLabel(effect, indexedEffect.Index) : string.Empty;

            switch (effect.effectType)
            {
                case EnemySkill_SO.BuiltInEffectType.Heal:
                    if (IsFriendlyTargetMode(effect.targetMode))
                    {
                        applyFriendlyHeal?.Invoke(effect, indexedEffect.Index, effectLabel);
                        break;
                    }
                    DispatchSelfOrTargets(effect, indexedEffect.Index, effectLabel, currentTarget, opponents, selectTargets, canTarget, applySelfHeal, applyTargetHeal);
                    break;
                case EnemySkill_SO.BuiltInEffectType.Damage:
                    if (IsFriendlyTargetMode(effect.targetMode))
                    {
                        applyFriendlyDamage?.Invoke(effect, indexedEffect.Index, effectLabel);
                        break;
                    }
                    DispatchSelfOrTargets(effect, indexedEffect.Index, effectLabel, currentTarget, opponents, selectTargets, canTarget, applySelfDamage, applyTargetDamage);
                    break;
                case EnemySkill_SO.BuiltInEffectType.ApplyStatus:
                    if (IsFriendlyTargetMode(effect.targetMode))
                    {
                        applyFriendlyStatus?.Invoke(effect, indexedEffect.Index, effectLabel);
                        break;
                    }
                    DispatchSelfOrTargets(effect, indexedEffect.Index, effectLabel, currentTarget, opponents, selectTargets, canTarget, applySelfStatus, applyTargetStatus);
                    break;
                case EnemySkill_SO.BuiltInEffectType.TransferRandomNegativeStatus:
                    applyTransfer?.Invoke(
                        Select(effect, currentTarget, opponents, selectTargets, canTarget),
                        effect,
                        indexedEffect.Index,
                        effectLabel);
                    break;
            }
        }
    }

    private static bool IsFriendlyTargetMode(EnemySkill_SO.DamageTargetMode targetMode)
    {
        return targetMode == EnemySkill_SO.DamageTargetMode.AllFriendlies
            || targetMode == EnemySkill_SO.DamageTargetMode.LowestHpFriendly;
    }

    private static void DispatchSelfOrTargets<TOpponent>(
        EnemySkill_SO.BuiltInEffectEntry effect,
        int effectIndex,
        string effectLabel,
        TOpponent currentTarget,
        IReadOnlyList<TOpponent> opponents,
        Func<EnemySkill_SO.DamageTargetMode, TOpponent, IReadOnlyList<TOpponent>, int, List<TOpponent>> selectTargets,
        Func<TOpponent, bool> canTarget,
        Action<EnemySkill_SO.BuiltInEffectEntry, int, string> applySelf,
        Action<TOpponent, EnemySkill_SO.BuiltInEffectEntry, int, string> applyTarget)
    {
        if (effect.targetMode == EnemySkill_SO.DamageTargetMode.Self)
        {
            applySelf?.Invoke(effect, effectIndex, effectLabel);
            return;
        }

        List<TOpponent> targets = Select(effect, currentTarget, opponents, selectTargets, canTarget);
        for (int i = 0; i < targets.Count; i++)
            applyTarget?.Invoke(targets[i], effect, effectIndex, effectLabel);
    }

    private static List<TOpponent> Select<TOpponent>(
        EnemySkill_SO.BuiltInEffectEntry effect,
        TOpponent currentTarget,
        IReadOnlyList<TOpponent> opponents,
        Func<EnemySkill_SO.DamageTargetMode, TOpponent, IReadOnlyList<TOpponent>, int, List<TOpponent>> selectTargets,
        Func<TOpponent, bool> canTarget)
    {
        List<TOpponent> selected = selectTargets != null
            ? selectTargets(effect.targetMode, currentTarget, opponents, effect.randomTargetCount)
            : new List<TOpponent>();
        if (selected == null || selected.Count == 0)
            return new List<TOpponent>();

        List<TOpponent> result = new List<TOpponent>();
        for (int i = 0; i < selected.Count; i++)
        {
            TOpponent target = selected[i];
            if (canTarget == null || canTarget(target))
                result.Add(target);
        }

        return result;
    }
}
