using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public static partial class RelicSystem
{
    private static void HandleApplyStatusToPlayerWhenSpeedEquals(
        Relic_SO relicData,
        RunRelicState relicState,
        RelicExecutionContext context,
        Relic_SO.RelicBuiltInEffectEntry effect)
    {
        if (context.trigger != Relic_SO.RelicTrigger.OnPlayerRoundStart || !context.sourceIsPlayer)
            return;

        BattleAllyController roundStartAlly = context.source as BattleAllyController;
        if (roundStartAlly == null || effect.statusData == null)
            return;

        int speedMin = effect.requiredSpeedMin;
        int speedMax = effect.requiredSpeedMax;
        if (speedMax < speedMin || (speedMin == 1 && speedMax == 1 && effect.requiredSpeed != 1))
        {
            int fallbackSpeed = Mathf.Max(0, effect.requiredSpeed);
            speedMin = fallbackSpeed;
            speedMax = fallbackSpeed;
        }

        if (context.currentSpeed < Mathf.Max(0, speedMin) || context.currentSpeed > Mathf.Max(speedMin, speedMax))
            return;

        int stacksToApply = CalculateScaledStatusStacks(relicState, effect);
        if (stacksToApply <= 0)
            return;

        bool applied = TryApplyStatus(roundStartAlly, effect, stacksToApply);
        if (!applied)
            return;

        if (effect.statusApplyTiming == Relic_SO.RelicStatusApplyTiming.NextRoundStart)
        {
            int delayRounds = Mathf.Max(1, effect.applyAfterRoundStarts);
            AddRelicFeedback(context, relicData, $"{roundStartAlly.GetDebugDisplayName()} 将于下 {delayRounds} 个回合开始时获得 {stacksToApply} 层 {GetStatusDisplayName(effect.statusData)}。");
        }
        else
        {
            AddRelicFeedback(context, relicData, $"{roundStartAlly.GetDebugDisplayName()} 获得 {stacksToApply} 层 {GetStatusDisplayName(effect.statusData)}。");
        }
    }

    private static void HandleApplyStatusToAllEnemiesOnSpecificRound(
        Relic_SO relicData,
        RunRelicState relicState,
        RelicExecutionContext context,
        Relic_SO.RelicBuiltInEffectEntry effect)
    {
        if (context.trigger != Relic_SO.RelicTrigger.OnBattleRoundStart)
            return;
        if (effect.statusData == null || context.enemies == null || context.enemies.Length == 0)
            return;

        if (!IsCurrentRoundWithinRange(context, effect, out int requiredRoundMin, out int requiredRoundMax))
            return;

        int enemyStacksToApply = CalculateScaledStatusStacks(relicState, effect);
        if (enemyStacksToApply <= 0)
            return;

        int appliedEnemyCount = 0;
        for (int enemyIndex = 0; enemyIndex < context.enemies.Length; enemyIndex++)
        {
            BattleEnemyController enemy = context.enemies[enemyIndex];
            if (enemy == null || enemy.IsDead)
                continue;

            if (TryApplyStatus(enemy, effect, enemyStacksToApply))
            {
                ApplyImmediateBattleRoundStartSpeedBonus(context, enemy, effect, enemyStacksToApply);
                appliedEnemyCount += 1;
            }
        }

        if (appliedEnemyCount <= 0)
            return;

        string roundRangeText = BuildRoundRangeLabel(requiredRoundMin, requiredRoundMax);
        if (effect.statusApplyTiming == Relic_SO.RelicStatusApplyTiming.NextRoundStart)
        {
            int delayRounds = Mathf.Max(1, effect.applyAfterRoundStarts);
            AddRelicFeedback(context, relicData, $"{roundRangeText}将使 {appliedEnemyCount} 名敌人于下 {delayRounds} 个回合开始时获得 {enemyStacksToApply} 层 {GetStatusDisplayName(effect.statusData)}。");
        }
        else
        {
            AddRelicFeedback(context, relicData, $"{roundRangeText}使 {appliedEnemyCount} 名敌人获得 {enemyStacksToApply} 层 {GetStatusDisplayName(effect.statusData)}。");
        }
    }

    private static void HandleApplyStatusToAllAlliesOnSpecificRound(
        Relic_SO relicData,
        RunRelicState relicState,
        RelicExecutionContext context,
        Relic_SO.RelicBuiltInEffectEntry effect)
    {
        if (context.trigger != Relic_SO.RelicTrigger.OnBattleRoundStart)
            return;
        if (effect.statusData == null || context.allies == null || context.allies.Length == 0)
            return;

        if (!IsCurrentRoundWithinRange(context, effect, out int requiredRoundMin, out int requiredRoundMax))
            return;

        int allyStacksToApply = CalculateScaledStatusStacks(relicState, effect);
        if (allyStacksToApply <= 0)
            return;

        int appliedAllyCount = 0;
        for (int allyIndex = 0; allyIndex < context.allies.Length; allyIndex++)
        {
            BattleAllyController allyTarget = context.allies[allyIndex];
            if (allyTarget == null || allyTarget.IsDead)
                continue;

            if (TryApplyStatus(allyTarget, effect, allyStacksToApply))
            {
                ApplyImmediateBattleRoundStartSpeedBonus(context, allyTarget, effect, allyStacksToApply);
                appliedAllyCount += 1;
            }
        }

        if (appliedAllyCount <= 0)
            return;

        string roundRangeText = BuildRoundRangeLabel(requiredRoundMin, requiredRoundMax);
        if (effect.statusApplyTiming == Relic_SO.RelicStatusApplyTiming.NextRoundStart)
        {
            int delayRounds = Mathf.Max(1, effect.applyAfterRoundStarts);
            AddRelicFeedback(context, relicData, $"{roundRangeText}将使 {appliedAllyCount} 名友方于下 {delayRounds} 个回合开始时获得 {allyStacksToApply} 层 {GetStatusDisplayName(effect.statusData)}。");
        }
        else
        {
            AddRelicFeedback(context, relicData, $"{roundRangeText}使 {appliedAllyCount} 名友方获得 {allyStacksToApply} 层 {GetStatusDisplayName(effect.statusData)}。");
        }
    }

    private static void HandleApplyStatusToRandomAllyOnSpecificRound(
        Relic_SO relicData,
        RunRelicState relicState,
        RelicExecutionContext context,
        Relic_SO.RelicBuiltInEffectEntry effect)
    {
        if (context.trigger != Relic_SO.RelicTrigger.OnBattleRoundStart)
            return;
        if (effect.statusData == null || context.allies == null || context.allies.Length == 0)
            return;

        if (!IsCurrentRoundWithinRange(context, effect, out int requiredRoundMin, out int requiredRoundMax))
            return;

        int randomAllyStacksToApply = CalculateScaledStatusStacks(relicState, effect);
        if (randomAllyStacksToApply <= 0)
            return;

        List<BattleAllyController> aliveAllies = new List<BattleAllyController>();
        for (int allyIndex = 0; allyIndex < context.allies.Length; allyIndex++)
        {
            BattleAllyController candidate = context.allies[allyIndex];
            if (candidate != null && !candidate.IsDead)
                aliveAllies.Add(candidate);
        }

        if (aliveAllies.Count <= 0)
            return;

        BattleAllyController randomAlly = aliveAllies[Random.Range(0, aliveAllies.Count)];
        if (!TryApplyStatus(randomAlly, effect, randomAllyStacksToApply))
            return;

        ApplyImmediateBattleRoundStartSpeedBonus(context, randomAlly, effect, randomAllyStacksToApply);

        string roundLabel = BuildRoundRangeLabel(requiredRoundMin, requiredRoundMax);
        AddRelicFeedback(context, relicData, $"{roundLabel}时，{randomAlly.GetDebugDisplayName()} 获得 {randomAllyStacksToApply} 层 {GetStatusDisplayName(effect.statusData)}。");
    }

    private static void HandleApplyStatusToRandomEnemyOnSpecificRound(
        Relic_SO relicData,
        RunRelicState relicState,
        RelicExecutionContext context,
        Relic_SO.RelicBuiltInEffectEntry effect)
    {
        if (context.trigger != Relic_SO.RelicTrigger.OnBattleRoundStart)
            return;
        if (effect.statusData == null || context.enemies == null || context.enemies.Length == 0)
            return;

        if (!IsCurrentRoundWithinRange(context, effect, out int requiredRoundMin, out int requiredRoundMax))
            return;

        int randomEnemyStacksToApply = CalculateScaledStatusStacks(relicState, effect);
        if (randomEnemyStacksToApply <= 0)
            return;

        List<BattleEnemyController> aliveEnemies = new List<BattleEnemyController>();
        for (int enemyIndex = 0; enemyIndex < context.enemies.Length; enemyIndex++)
        {
            BattleEnemyController candidate = context.enemies[enemyIndex];
            if (candidate != null && !candidate.IsDead)
                aliveEnemies.Add(candidate);
        }

        if (aliveEnemies.Count <= 0)
            return;

        BattleEnemyController randomEnemy = aliveEnemies[Random.Range(0, aliveEnemies.Count)];
        if (!TryApplyStatus(randomEnemy, effect, randomEnemyStacksToApply))
            return;

        ApplyImmediateBattleRoundStartSpeedBonus(context, randomEnemy, effect, randomEnemyStacksToApply);

        string roundLabel = BuildRoundRangeLabel(requiredRoundMin, requiredRoundMax);
        AddRelicFeedback(context, relicData, $"{roundLabel}时，{randomEnemy.GetDebugDisplayName()} 获得 {randomEnemyStacksToApply} 层 {GetStatusDisplayName(effect.statusData)}。");
    }

    private static void HandleApplyStatusToAllAlliesOnOddRounds(
        Relic_SO relicData,
        RunRelicState relicState,
        RelicExecutionContext context,
        Relic_SO.RelicBuiltInEffectEntry effect)
    {
        if (context.trigger != Relic_SO.RelicTrigger.OnBattleRoundStart)
            return;
        if (context.currentRound % 2 == 0 || effect.statusData == null || context.allies == null || context.allies.Length == 0)
            return;

        int oddRoundStacks = CalculateScaledStatusStacks(relicState, effect);
        if (oddRoundStacks <= 0)
            return;

        int oddRoundAffectedCount = 0;
        for (int allyIndex = 0; allyIndex < context.allies.Length; allyIndex++)
        {
            BattleAllyController oddRoundTarget = context.allies[allyIndex];
            if (oddRoundTarget == null || oddRoundTarget.IsDead)
                continue;

            if (TryApplyStatus(oddRoundTarget, effect, oddRoundStacks))
            {
                ApplyImmediateBattleRoundStartSpeedBonus(context, oddRoundTarget, effect, oddRoundStacks);
                oddRoundAffectedCount += 1;
            }
        }

        if (oddRoundAffectedCount > 0)
            AddRelicFeedback(context, relicData, $"奇数回合开始时，{oddRoundAffectedCount} 名友方获得 {oddRoundStacks} 层 {GetStatusDisplayName(effect.statusData)}。");
    }
}
