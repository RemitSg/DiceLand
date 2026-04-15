using System;
using System.Collections.Generic;
using UnityEngine;

public enum EnemyPoolKind
{
    Standard,
    Elite
}

[CreateAssetMenu(fileName = "New EnemyRoster", menuName = "Enemies/EnemyRoster")]
public class EnemyRoster_SO : ScriptableObject
{
    [Serializable]
    public class DangerPool
    {
        public MapDangerBand dangerBand;
        public List<EnemyGroup_SO> standardGroups = new List<EnemyGroup_SO>();
        public List<EnemyGroup_SO> eliteGroups = new List<EnemyGroup_SO>();

        public List<EnemyGroup_SO> GetPool(EnemyPoolKind poolKind)
        {
            switch (poolKind)
            {
                case EnemyPoolKind.Elite:
                    return eliteGroups;
                default:
                    return standardGroups;
            }
        }
    }

    [Header("Pools By Danger Band")]
    [SerializeField] private List<DangerPool> pools = new List<DangerPool>();

    [Header("Boss Pool")]
    [SerializeField] private List<EnemyGroup_SO> bossGroups = new List<EnemyGroup_SO>();

    public IReadOnlyList<DangerPool> Pools => pools;
    public IReadOnlyList<EnemyGroup_SO> BossGroups => bossGroups;

    public DangerPool GetDangerPool(MapDangerBand dangerBand)
    {
        for (int i = 0; i < pools.Count; i++)
        {
            if (pools[i] != null && pools[i].dangerBand == dangerBand)
                return pools[i];
        }

        return null;
    }

    public IReadOnlyList<EnemyGroup_SO> GetGroups(MapDangerBand dangerBand, EnemyPoolKind poolKind)
    {
        DangerPool pool = GetDangerPool(dangerBand);
        if (pool == null)
            return Array.Empty<EnemyGroup_SO>();

        List<EnemyGroup_SO> groupPool = pool.GetPool(poolKind);
        return groupPool ?? (IReadOnlyList<EnemyGroup_SO>)Array.Empty<EnemyGroup_SO>();
    }

    public IReadOnlyList<EnemyGroup_SO> GetBossGroups()
    {
        return bossGroups ?? (IReadOnlyList<EnemyGroup_SO>)Array.Empty<EnemyGroup_SO>();
    }

    public EnemyGroup_SO GetRandomGroup(MapDangerBand dangerBand, EnemyPoolKind poolKind, System.Random rng = null)
    {
        IReadOnlyList<EnemyGroup_SO> candidates = GetGroups(dangerBand, poolKind);
        if (candidates == null || candidates.Count == 0)
            return null;

        System.Random effectiveRng = rng ?? new System.Random();
        return candidates[effectiveRng.Next(0, candidates.Count)];
    }

    public EnemyGroup_SO GetRandomBossGroup(System.Random rng = null)
    {
        IReadOnlyList<EnemyGroup_SO> candidates = GetBossGroups();
        if (candidates == null || candidates.Count == 0)
            return null;

        System.Random effectiveRng = rng ?? new System.Random();
        return candidates[effectiveRng.Next(0, candidates.Count)];
    }

    public bool HasEnemies(MapDangerBand dangerBand, EnemyPoolKind poolKind)
    {
        return GetGroups(dangerBand, poolKind).Count > 0;
    }

    public bool HasBossEnemies()
    {
        return GetBossGroups().Count > 0;
    }

    private void OnValidate()
    {
        EnsureAllDangerPoolsExist();
        CleanupPools();
    }

    private void EnsureAllDangerPoolsExist()
    {
        foreach (MapDangerBand dangerBand in Enum.GetValues(typeof(MapDangerBand)))
        {
            if (GetDangerPool(dangerBand) != null)
                continue;

            pools.Add(new DangerPool { dangerBand = dangerBand });
        }
    }

    private void CleanupPools()
    {
        HashSet<MapDangerBand> seen = new HashSet<MapDangerBand>();
        for (int i = pools.Count - 1; i >= 0; i--)
        {
            DangerPool pool = pools[i];
            if (pool == null)
            {
                pools.RemoveAt(i);
                continue;
            }

            if (!seen.Add(pool.dangerBand))
            {
                pools.RemoveAt(i);
                continue;
            }

            EnsurePoolList(pool.standardGroups);
            EnsurePoolList(pool.eliteGroups);
        }

        EnsurePoolList(bossGroups);
        pools.Sort((a, b) => a.dangerBand.CompareTo(b.dangerBand));
    }

    private static void EnsurePoolList(List<EnemyGroup_SO> list)
    {
        if (list == null)
            return;
    }
}
