using System;
using System.Collections.Generic;
using UnityEngine;

public static class BattleReflectionSkillCopyService
{
    public static EnemySkill_SO CreateRuntimeCopy(EnemySkill_SO template, EnemySkill_SO source)
    {
        if (template == null)
            return null;

        EnemySkill_SO copy = ScriptableObject.CreateInstance<EnemySkill_SO>();
        copy.name = $"{template.name}_RuntimeCopy";
        CopyTemplateIdentity(template, copy);
        CopySkillContent(source != null ? source : template, copy);
        return copy;
    }

    public static List<EnemySkill_SO> CreateRuntimeCopies(EnemySkill_SO template, IReadOnlyList<EnemySkill_SO> sources, int count)
    {
        List<EnemySkill_SO> copies = new List<EnemySkill_SO>();
        if (template == null || count <= 0)
            return copies;

        for (int i = 0; i < count; i++)
        {
            EnemySkill_SO source = sources != null && sources.Count > 0 ? sources[i % sources.Count] : template;
            EnemySkill_SO copy = CreateRuntimeCopy(template, source);
            if (copy != null)
                copies.Add(copy);
        }

        return copies;
    }

    private static void CopyTemplateIdentity(EnemySkill_SO template, EnemySkill_SO copy)
    {
        copy.skillName = template.skillName;
        copy.description = template.description;
        copy.energyCost = template.energyCost;
        copy.requiredSelfStatusData = template.requiredSelfStatusData;
        copy.requiredSelfStatusStacks = template.requiredSelfStatusStacks;
        copy.keepRequiredSelfStatusAfterUse = template.keepRequiredSelfStatusAfterUse;
        copy.transformEnemyToStageIndex = template.transformEnemyToStageIndex;
        copy.allowedEnemyStages = template.allowedEnemyStages != null ? new List<int>(template.allowedEnemyStages) : null;
        copy.targetPriorityMode = template.targetPriorityMode;
        copy.targetPriorityStatusData = template.targetPriorityStatusData;
        copy.targetPriorityStatusType = template.targetPriorityStatusType;
        copy.targetPriorityRequiredStacks = template.targetPriorityRequiredStacks;
    }

    private static void CopySkillContent(EnemySkill_SO source, EnemySkill_SO copy)
    {
        copy.skillCategory = source.skillCategory;
        copy.minPower = source.minPower;
        copy.maxPower = source.maxPower;
        copy.diceCount = Mathf.Max(1, source.diceCount);
        copy.dicePowerRanges = CloneArray(source.dicePowerRanges);
        copy.diceSkillCategories = CloneArray(source.diceSkillCategories);
        copy.diceEffectSettings = CloneDiceEffectSettings(source.diceEffectSettings);
        copy.dicePresentationSettings = new EnemySkill_SO.DicePresentationSettings[Mathf.Max(1, copy.diceCount)];
    }

    private static EnemySkill_SO.DiceEffectSettings[] CloneDiceEffectSettings(EnemySkill_SO.DiceEffectSettings[] source)
    {
        if (source == null)
            return Array.Empty<EnemySkill_SO.DiceEffectSettings>();

        EnemySkill_SO.DiceEffectSettings[] copy = new EnemySkill_SO.DiceEffectSettings[source.Length];
        for (int i = 0; i < source.Length; i++)
        {
            EnemySkill_SO.DiceEffectSettings settings = source[i];
            settings.damageEffects = CloneArray(settings.damageEffects);
            settings.statusEffects = CloneArray(settings.statusEffects);
            settings.powerModifiers = CloneArray(settings.powerModifiers);
            settings.statusBurstEffects = CloneArray(settings.statusBurstEffects);
            settings.healingEffects = CloneArray(settings.healingEffects);
            settings.builtInEffects = CloneArray(settings.builtInEffects);
            settings.rangeModifiers = CloneArray(settings.rangeModifiers);
            settings.statusStackOperationEffects = CloneArray(settings.statusStackOperationEffects);
            settings.enemyStageTransitionEffects = CloneArray(settings.enemyStageTransitionEffects);
            settings.summonEffects = CloneArray(settings.summonEffects);
            copy[i] = settings;
        }

        return copy;
    }

    private static T[] CloneArray<T>(T[] source)
    {
        if (source == null)
            return Array.Empty<T>();

        T[] copy = new T[source.Length];
        Array.Copy(source, copy, source.Length);
        return copy;
    }
}
