using System;
using System.Collections.Generic;
using UnityEngine;

public static class BattleTargetSelectionService
{
    public static List<T> Select<T>(
        EnemySkill_SO.DamageTargetMode targetMode,
        T currentTarget,
        IEnumerable<T> candidates,
        int randomTargetCount,
        Func<T, bool> isValidTarget,
        Func<List<T>, List<T>> randomOrder,
        Comparison<T> comparePriority = null)
    {
        switch (targetMode)
        {
            case EnemySkill_SO.DamageTargetMode.CurrentTarget:
                return currentTarget != null && (isValidTarget == null || isValidTarget(currentTarget))
                    ? new List<T> { currentTarget }
                    : new List<T>();
            case EnemySkill_SO.DamageTargetMode.AllOpponents:
            case EnemySkill_SO.DamageTargetMode.AllFriendlies:
            case EnemySkill_SO.DamageTargetMode.AllFriendliesExceptSelf:
                return BuildValidTargetList(candidates, isValidTarget);
            case EnemySkill_SO.DamageTargetMode.LowestHpFriendly:
            case EnemySkill_SO.DamageTargetMode.LowestHpOpponent:
                List<T> priorityPool = BuildValidTargetList(candidates, isValidTarget);
                if (priorityPool.Count <= 1)
                    return priorityPool;
                if (comparePriority == null)
                    return new List<T> { priorityPool[0] };
                priorityPool.Sort(comparePriority);
                return new List<T> { priorityPool[0] };
            case EnemySkill_SO.DamageTargetMode.RandomOpponents:
                List<T> orderedPool = BuildValidTargetList(candidates, isValidTarget);
                if (orderedPool.Count == 0)
                    return orderedPool;

                if (randomOrder != null)
                    orderedPool = randomOrder(orderedPool) ?? new List<T>();

                int targetCount = Mathf.Clamp(randomTargetCount, 1, orderedPool.Count);
                return orderedPool.GetRange(0, targetCount);
            default:
                return new List<T>();
        }
    }

    public static List<T> SelectTargets<T>(
        EnemySkill_SO.DamageTargetMode targetMode,
        T currentTarget,
        List<T> candidates,
        int randomTargetCount,
        Func<T, bool> isValidTarget,
        Func<int, int, int> randomRange)
    {
        return Select(
            targetMode,
            currentTarget,
            candidates,
            randomTargetCount,
            isValidTarget,
            pool => SelectRandomTargets(pool, randomTargetCount, randomRange));
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

    private static List<T> BuildValidTargetList<T>(IEnumerable<T> candidates, Func<T, bool> isValidTarget)
    {
        List<T> result = new List<T>();
        if (candidates == null)
            return result;

        foreach (T candidate in candidates)
        {
            if (isValidTarget == null || isValidTarget(candidate))
                result.Add(candidate);
        }

        return result;
    }
}
