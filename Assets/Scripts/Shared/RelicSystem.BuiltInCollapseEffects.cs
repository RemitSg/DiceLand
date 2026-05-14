using UnityEngine;

public static partial class RelicSystem
{
    private static void HandleQueueStatusToAllEnemiesOnPlayerDeathNextRound(
        Relic_SO relicData,
        RunRelicState relicState,
        RelicExecutionContext context,
        Relic_SO.RelicBuiltInEffectEntry effect)
    {
        if (context.trigger != Relic_SO.RelicTrigger.OnPlayerDeath || effect.statusData == null || context.enemies == null)
            return;

        int stacks = CalculateScaledStatusStacks(relicState, effect);
        if (stacks <= 0)
            return;

        Relic_SO.RelicStatusApplyTiming originalTiming = effect.statusApplyTiming;
        int originalDelay = effect.applyAfterRoundStarts;
        effect.statusApplyTiming = Relic_SO.RelicStatusApplyTiming.NextRoundStart;
        effect.applyAfterRoundStarts = Mathf.Max(1, originalDelay);

        for (int i = 0; i < context.enemies.Length; i++)
        {
            BattleEnemyController enemy = context.enemies[i];
            if (enemy == null || enemy.IsDead)
                continue;

            TryApplyStatus(enemy, effect, stacks);
        }

        effect.statusApplyTiming = originalTiming;
        effect.applyAfterRoundStarts = originalDelay;
    }
}
