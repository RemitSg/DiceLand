using System;
using System.Collections.Generic;
using UnityEngine;

public static class BattleTargetSelectionService
{
    public static List<T> SelectTargets<T>(
        EnemySkill_SO.DamageTargetMode targetMode,
        T currentTarget,
        List<T> candidates,
        int randomTargetCount,
        Func<T, bool> isValidTarget,
        Func<int, int, int> randomRange)
    {
        switch (targetMode)
        {
            case EnemySkill_SO.DamageTargetMode.CurrentTarget:
                return currentTarget != null && (isValidTarget == null || isValidTarget(currentTarget))
                    ? new List<T> { currentTarget }
                    : new List<T>();
            case EnemySkill_SO.DamageTargetMode.AllOpponents:
                return candidates != null ? new List<T>(candidates) : new List<T>();
            case EnemySkill_SO.DamageTargetMode.RandomOpponents:
                return SelectRandomTargets(candidates, randomTargetCount, randomRange);
            default:
                return new List<T>();
        }
    }

    public static List<T> SelectRandomTargets<T>(
        List<T> candidates,
        int count,
        Func<int, int, int> randomRange)
    {
        List<T> pool = candidates != null ? new List<T>(candidates) : new List<T>();
        List<T> result = new List<T>();
        if (pool.Count == 0)
            return result;

        int targetCount = Mathf.Clamp(count, 1, pool.Count);
        for (int i = 0; i < targetCount; i++)
        {
            if (pool.Count == 0)
                break;

            int randomIndex = randomRange != null ? randomRange(0, pool.Count) : UnityEngine.Random.Range(0, pool.Count);
            result.Add(pool[randomIndex]);
            pool.RemoveAt(randomIndex);
        }

        return result;
    }
}
