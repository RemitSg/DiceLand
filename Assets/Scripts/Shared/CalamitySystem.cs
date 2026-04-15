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

    public static int ApplyBattleExperienceModifiers(int baseValue, IReadOnlyList<RunCalamityState> activeCalamities)
    {
        return ApplyPercentageModifier(baseValue, activeCalamities, CalamityBuiltInEffectType.BattleExperiencePercentModifier, 0, false, ModifyBattleExperienceWithHandler);
    }

    public static int ApplyBattleMoneyModifiers(int baseValue, IReadOnlyList<RunCalamityState> activeCalamities)
    {
        return ApplyPercentageModifier(baseValue, activeCalamities, CalamityBuiltInEffectType.BattleMoneyPercentModifier, 0, false, ModifyBattleMoneyWithHandler);
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
}
