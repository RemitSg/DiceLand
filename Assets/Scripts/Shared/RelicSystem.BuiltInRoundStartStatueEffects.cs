using UnityEngine;

public static partial class RelicSystem
{
    private static void HandleRestoreSharedEnergyOnSpecificRound(
        Relic_SO relicData,
        RunRelicState relicState,
        RelicExecutionContext context,
        Relic_SO.RelicBuiltInEffectEntry effect)
    {
        if (context.trigger != Relic_SO.RelicTrigger.OnBattleRoundStart)
            return;
        if (!IsCurrentRoundWithinRange(context, effect, out _, out _))
            return;

        int recovery = Mathf.Max(0, effect.amount * Mathf.Max(1, relicState.amount));
        if (recovery > 0)
            context.additionalSharedEnergyRecovery += recovery;
    }

    private static void HandleApplyStatusToFullHpAlliesOnBattleRoundStart(
        Relic_SO relicData,
        RunRelicState relicState,
        RelicExecutionContext context,
        Relic_SO.RelicBuiltInEffectEntry effect)
    {
        if (context.trigger != Relic_SO.RelicTrigger.OnBattleRoundStart || effect.statusData == null || context.allies == null)
            return;

        int stacks = CalculateScaledStatusStacks(relicState, effect);
        if (stacks <= 0)
            return;

        for (int i = 0; i < context.allies.Length; i++)
        {
            BattleAllyController ally = context.allies[i];
            if (ally == null || ally.IsDead || ally.MaxHP <= 0 || ally.CurrentHP < ally.MaxHP)
                continue;

            if (TryApplyStatus(ally, effect, stacks))
                ApplyImmediateBattleRoundStartSpeedBonus(context, ally, effect, stacks);
        }
    }

    private static void HandleApplyStatusToLowHpAlliesOnBattleRoundStart(
        Relic_SO relicData,
        RunRelicState relicState,
        RelicExecutionContext context,
        Relic_SO.RelicBuiltInEffectEntry effect)
    {
        if (context.trigger != Relic_SO.RelicTrigger.OnBattleRoundStart || effect.statusData == null || context.allies == null)
            return;

        int stacks = CalculateScaledStatusStacks(relicState, effect);
        if (stacks <= 0)
            return;

        float threshold = Mathf.Max(0, effect.requiredStatusStacks) / 100f;
        for (int i = 0; i < context.allies.Length; i++)
        {
            BattleAllyController ally = context.allies[i];
            if (ally == null || ally.IsDead || ally.MaxHP <= 0)
                continue;

            float hpRatio = ally.CurrentHP / (float)ally.MaxHP;
            if (hpRatio >= threshold)
                continue;

            if (TryApplyStatus(ally, effect, stacks))
                ApplyImmediateBattleRoundStartSpeedBonus(context, ally, effect, stacks);
        }
    }
}
