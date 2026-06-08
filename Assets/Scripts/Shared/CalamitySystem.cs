using System.Collections.Generic;
using UnityEngine;

public static class CalamitySystem
{
    public static int ApplyEnemyMaxHpModifiers(int baseValue, IReadOnlyList<RunCalamityState> activeCalamities)
    {
        return ApplyPercentageModifier(baseValue, activeCalamities, CalamityBuiltInEffectType.EnemyMaxHpPercentModifier, 1, true, ModifyEnemyMaxHpWithHandler);
    }

    public static int ApplyEnemyDamageModifiers(int baseValue, IReadOnlyList<RunCalamityState> activeCalamities)
    {
        return ApplyPercentageModifier(baseValue, activeCalamities, CalamityBuiltInEffectType.EnemyDamagePercentModifier, 1, true, ModifyEnemyDamageWithHandler);
    }

    public static List<BattleDamageFormulaModifierSource> GetEnemyDamageModifierSources(IReadOnlyList<RunCalamityState> activeCalamities)
    {
        return GetPercentageModifierSources(activeCalamities, CalamityBuiltInEffectType.EnemyDamagePercentModifier);
    }

    public static int ApplyBattleExperienceModifiers(int baseValue, IReadOnlyList<RunCalamityState> activeCalamities)
    {
        return ApplyPercentageModifier(baseValue, activeCalamities, CalamityBuiltInEffectType.BattleExperiencePercentModifier, 0, false, ModifyBattleExperienceWithHandler);
    }

    public static int ApplyBattleMoneyModifiers(int baseValue, IReadOnlyList<RunCalamityState> activeCalamities)
    {
        return ApplyPercentageModifier(baseValue, activeCalamities, CalamityBuiltInEffectType.BattleMoneyPercentModifier, 0, false, ModifyBattleMoneyWithHandler);
    }

    public static int ApplyMerchantPriceModifiers(int baseValue, IReadOnlyList<RunCalamityState> activeCalamities)
    {
        return ApplyPercentageModifier(baseValue, activeCalamities, CalamityBuiltInEffectType.MerchantPricePercentModifier, 0, false, ModifyMerchantPriceWithHandler);
    }

    public static int ApplyAllyDamageModifiers(int baseValue, IReadOnlyList<RunCalamityState> activeCalamities)
    {
        return ApplyPercentageModifier(baseValue, activeCalamities, CalamityBuiltInEffectType.AllyDamagePercentModifier, 0, false, ModifyAllyDamageWithHandler);
    }

    public static List<BattleDamageFormulaModifierSource> GetAllyDamageModifierSources(IReadOnlyList<RunCalamityState> activeCalamities)
    {
        return GetPercentageModifierSources(activeCalamities, CalamityBuiltInEffectType.AllyDamagePercentModifier);
    }

    public static int ApplyAllyMaxHpModifiers(int baseValue, IReadOnlyList<RunCalamityState> activeCalamities)
    {
        return ApplyPercentageModifier(baseValue, activeCalamities, CalamityBuiltInEffectType.AllyMaxHpPercentModifier, 1, true, ModifyAllyMaxHpWithHandler);
    }

    public static int ApplyEnemyIncomingDamageModifiers(int baseValue, IReadOnlyList<RunCalamityState> activeCalamities)
    {
        return ApplyPercentageModifier(baseValue, activeCalamities, CalamityBuiltInEffectType.EnemyIncomingDamagePercentModifier, 0, false, ModifyEnemyIncomingDamageWithHandler);
    }

    public static List<BattleDamageFormulaModifierSource> GetEnemyIncomingDamageModifierSources(IReadOnlyList<RunCalamityState> activeCalamities)
    {
        return GetPercentageModifierSources(activeCalamities, CalamityBuiltInEffectType.EnemyIncomingDamagePercentModifier);
    }

    public static int ApplyEnemySpeedModifiers(int baseValue, IReadOnlyList<RunCalamityState> activeCalamities)
    {
        return Mathf.Max(1, baseValue + SumFlatEffectAmounts(activeCalamities, CalamityBuiltInEffectType.EnemySpeedFlatModifier));
    }

    public static int ApplyForgeCostModifiers(int baseValue, IReadOnlyList<RunCalamityState> activeCalamities)
    {
        return Mathf.Max(0, baseValue + SumFlatEffectAmounts(activeCalamities, CalamityBuiltInEffectType.ForgeCostFlatModifier));
    }

    public static int ResolveBattleStartSharedEnergyDelta(IReadOnlyList<RunCalamityState> activeCalamities)
    {
        return SumFlatEffectAmounts(activeCalamities, CalamityBuiltInEffectType.BattleStartSharedEnergyFlatModifier);
    }

    public static int ResolveRestHealPercent(int defaultPercent, IReadOnlyList<RunCalamityState> activeCalamities)
    {
        return ResolveMinimumPercentOverride(defaultPercent, activeCalamities, CalamityBuiltInEffectType.RestHealPercentOverride);
    }

    public static int ResolveRestActionPointRestorePercent(int defaultPercent, IReadOnlyList<RunCalamityState> activeCalamities)
    {
        return ResolveMinimumPercentOverride(defaultPercent, activeCalamities, CalamityBuiltInEffectType.RestActionPointRestorePercentOverride);
    }

    public static List<CalamityBuiltInEffectEntry> GetEnemyBattleStartStatusEffects(IReadOnlyList<RunCalamityState> activeCalamities)
    {
        return GetPositiveStatusEffects(activeCalamities, CalamityBuiltInEffectType.EnemyBattleStartStatus);
    }

    public static int ResolveEnemyRoundEndHealAmount(IReadOnlyList<RunCalamityState> activeCalamities)
    {
        return Mathf.Max(0, SumFlatEffectAmounts(activeCalamities, CalamityBuiltInEffectType.EnemyRoundEndHealFlat));
    }

    public static int ResolveAllyBattleStartHpLoss(IReadOnlyList<RunCalamityState> activeCalamities)
    {
        return Mathf.Max(0, SumFlatEffectAmounts(activeCalamities, CalamityBuiltInEffectType.AllyBattleStartHpLossFlat));
    }

    public static List<CalamityBuiltInEffectEntry> GetAllyBattleStartStatusEffects(IReadOnlyList<RunCalamityState> activeCalamities)
    {
        return GetPositiveStatusEffects(activeCalamities, CalamityBuiltInEffectType.AllyBattleStartStatus);
    }

    public static List<CalamityBuiltInEffectEntry> GetEnemyDeathStatusEffects(IReadOnlyList<RunCalamityState> activeCalamities)
    {
        return GetPositiveStatusEffects(activeCalamities, CalamityBuiltInEffectType.EnemyDeathStatusToRemainingEnemies);
    }

    public static bool HasEnemyDelayedDeath(IReadOnlyList<RunCalamityState> activeCalamities)
    {
        return SumFlatEffectAmounts(activeCalamities, CalamityBuiltInEffectType.EnemyDelayDeathUntilNextRoundStart) > 0;
    }

    public static int ResolveVisitedEmptyTileAmbushChance(IReadOnlyList<RunCalamityState> activeCalamities)
    {
        return Mathf.Clamp(SumFlatEffectAmounts(activeCalamities, CalamityBuiltInEffectType.VisitedEmptyTileAmbushChance), 0, 100);
    }

    private static List<CalamityBuiltInEffectEntry> GetPositiveStatusEffects(
        IReadOnlyList<RunCalamityState> activeCalamities,
        CalamityBuiltInEffectType builtInType)
    {
        List<CalamityBuiltInEffectEntry> results = new List<CalamityBuiltInEffectEntry>();
        if (activeCalamities == null || activeCalamities.Count == 0)
            return results;

        for (int i = 0; i < activeCalamities.Count; i++)
        {
            RunCalamityState state = activeCalamities[i];
            if (state == null || state.calamityData == null)
                continue;

            IReadOnlyList<CalamityBuiltInEffectEntry> effects = state.calamityData.GetValidEffects();
            for (int j = 0; j < effects.Count; j++)
            {
                CalamityBuiltInEffectEntry effect = effects[j];
                if (effect == null
                    || effect.effectType != builtInType
                    || effect.statusData == null
                    || effect.effectAmount <= 0)
                    continue;

                results.Add(effect);
            }
        }

        return results;
    }

    private static int ApplyPercentageModifier(
        int baseValue,
        IReadOnlyList<RunCalamityState> activeCalamities,
        CalamityBuiltInEffectType builtInType,
        int minimumWhenPositive,
        bool preservePositiveMinimum,
        System.Func<int, RunCalamityState, int> customModifier)
    {
        if (baseValue <= 0 || activeCalamities == null || activeCalamities.Count == 0)
            return Mathf.Max(0, baseValue);

        int totalPercent = 0;
        for (int i = 0; i < activeCalamities.Count; i++)
        {
            RunCalamityState state = activeCalamities[i];
            if (state == null || state.calamityData == null)
                continue;

            IReadOnlyList<CalamityBuiltInEffectEntry> effects = state.calamityData.GetValidEffects();
            for (int j = 0; j < effects.Count; j++)
            {
                CalamityBuiltInEffectEntry effect = effects[j];
                if (effect == null || effect.effectType != builtInType)
                    continue;

                totalPercent += effect.effectAmount;
            }
        }

        int modifiedValue = baseValue;
        if (totalPercent != 0)
        {
            modifiedValue = Mathf.FloorToInt(baseValue * (1f + totalPercent / 100f));
            modifiedValue = Mathf.Max(0, modifiedValue);
            if (preservePositiveMinimum && baseValue > 0)
                modifiedValue = Mathf.Max(minimumWhenPositive, modifiedValue);
        }

        for (int i = 0; i < activeCalamities.Count; i++)
        {
            RunCalamityState state = activeCalamities[i];
            if (state == null || state.calamityData == null || state.calamityData.customEffectHandler == null)
                continue;

            modifiedValue = Mathf.Max(0, customModifier(modifiedValue, state));
            if (preservePositiveMinimum && baseValue > 0)
                modifiedValue = Mathf.Max(minimumWhenPositive, modifiedValue);
        }

        return modifiedValue;
    }

    private static List<BattleDamageFormulaModifierSource> GetPercentageModifierSources(
        IReadOnlyList<RunCalamityState> activeCalamities,
        CalamityBuiltInEffectType builtInType)
    {
        List<BattleDamageFormulaModifierSource> sources = new List<BattleDamageFormulaModifierSource>();
        if (activeCalamities == null || activeCalamities.Count == 0)
            return sources;

        for (int i = 0; i < activeCalamities.Count; i++)
        {
            RunCalamityState state = activeCalamities[i];
            if (state == null || state.calamityData == null)
                continue;

            IReadOnlyList<CalamityBuiltInEffectEntry> effects = state.calamityData.GetValidEffects();
            for (int j = 0; j < effects.Count; j++)
            {
                CalamityBuiltInEffectEntry effect = effects[j];
                if (effect == null || effect.effectType != builtInType || effect.effectAmount == 0)
                    continue;

                sources.Add(new BattleDamageFormulaModifierSource(effect.effectAmount, state.calamityData.DisplayName));
            }
        }

        return sources;
    }

    private static int ModifyEnemyMaxHpWithHandler(int currentValue, RunCalamityState calamityState)
    {
        return calamityState.calamityData.customEffectHandler.ModifyEnemyMaxHP(currentValue, calamityState);
    }

    private static int ModifyEnemyDamageWithHandler(int currentValue, RunCalamityState calamityState)
    {
        return calamityState.calamityData.customEffectHandler.ModifyEnemyDamage(currentValue, calamityState);
    }

    private static int ModifyBattleExperienceWithHandler(int currentValue, RunCalamityState calamityState)
    {
        return calamityState.calamityData.customEffectHandler.ModifyBattleExperienceReward(currentValue, calamityState);
    }

    private static int ModifyBattleMoneyWithHandler(int currentValue, RunCalamityState calamityState)
    {
        return calamityState.calamityData.customEffectHandler.ModifyBattleMoneyReward(currentValue, calamityState);
    }

    private static int ModifyMerchantPriceWithHandler(int currentValue, RunCalamityState calamityState)
    {
        return calamityState.calamityData.customEffectHandler.ModifyMerchantPrice(currentValue, calamityState);
    }

    private static int ModifyAllyDamageWithHandler(int currentValue, RunCalamityState calamityState)
    {
        return calamityState.calamityData.customEffectHandler.ModifyAllyDamage(currentValue, calamityState);
    }

    private static int ModifyAllyMaxHpWithHandler(int currentValue, RunCalamityState calamityState)
    {
        return calamityState.calamityData.customEffectHandler.ModifyAllyMaxHP(currentValue, calamityState);
    }

    private static int ModifyEnemyIncomingDamageWithHandler(int currentValue, RunCalamityState calamityState)
    {
        return calamityState.calamityData.customEffectHandler.ModifyEnemyIncomingDamage(currentValue, calamityState);
    }

    private static int SumFlatEffectAmounts(IReadOnlyList<RunCalamityState> activeCalamities, CalamityBuiltInEffectType builtInType)
    {
        if (activeCalamities == null || activeCalamities.Count == 0)
            return 0;

        int total = 0;
        for (int i = 0; i < activeCalamities.Count; i++)
        {
            RunCalamityState state = activeCalamities[i];
            if (state == null || state.calamityData == null)
                continue;

            IReadOnlyList<CalamityBuiltInEffectEntry> effects = state.calamityData.GetValidEffects();
            for (int j = 0; j < effects.Count; j++)
            {
                CalamityBuiltInEffectEntry effect = effects[j];
                if (effect != null && effect.effectType == builtInType)
                    total += effect.effectAmount;
            }
        }

        return total;
    }

    private static int ResolveMinimumPercentOverride(
        int defaultPercent,
        IReadOnlyList<RunCalamityState> activeCalamities,
        CalamityBuiltInEffectType builtInType)
    {
        int resolvedPercent = Mathf.Clamp(defaultPercent, 0, 100);
        if (activeCalamities == null || activeCalamities.Count == 0)
            return resolvedPercent;

        for (int i = 0; i < activeCalamities.Count; i++)
        {
            RunCalamityState state = activeCalamities[i];
            if (state == null || state.calamityData == null)
                continue;

            IReadOnlyList<CalamityBuiltInEffectEntry> effects = state.calamityData.GetValidEffects();
            for (int j = 0; j < effects.Count; j++)
            {
                CalamityBuiltInEffectEntry effect = effects[j];
                if (effect == null || effect.effectType != builtInType)
                    continue;

                resolvedPercent = Mathf.Min(resolvedPercent, Mathf.Clamp(effect.effectAmount, 0, 100));
            }
        }

        return resolvedPercent;
    }
}
