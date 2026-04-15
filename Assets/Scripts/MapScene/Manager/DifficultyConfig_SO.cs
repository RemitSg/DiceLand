using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DifficultyConfig", menuName = "Map/DifficultyConfig")]
public class DifficultyConfig_SO : ScriptableObject
{
    [Serializable]
    public class TileWeightEntry
    {
        public MapTileType tileType;
        [Min(0)] public int weight = 1;
    }

    [Serializable]
    public class TileMinimumEntry
    {
        public MapTileType tileType;
        [Min(0)] public int minimumCount = 0;
    }

    [Serializable]
    public class ActionPointTierEntry
    {
        [Min(0)] public int calamityTier = 0;
        [Min(1)] public int actionPointLimit = 6;
    }

    public string difficultyName = "Normal";
    [Range(7, 13)] public int boardSize = 9;
    [Min(1)] public int startingActionPoints = 6;
    public List<ActionPointTierEntry> actionPointTiers = new List<ActionPointTierEntry>
    {
        new ActionPointTierEntry { calamityTier = 0, actionPointLimit = 6 }
    };
    [Min(1)] public int requiredKeys = 2;
    [Min(1)] public int generatedKeyTileCount = 3;
    [Min(0)] public int spawnMinRingDistance = 2;
    public List<TileWeightEntry> tileWeights = new List<TileWeightEntry>();
    public List<TileMinimumEntry> tileMinimums = new List<TileMinimumEntry>();
    [Min(1)] public int maxRecruitCount = 4;
    public List<int> recruitCostTiers = new List<int> { 50, 80, 120, 180 };

    public int BoardSize
    {
        get
        {
            int size = Mathf.Clamp(boardSize, 7, 13);
            return size % 2 == 0 ? size + 1 : size;
        }
    }

    public int GetWeight(MapTileType tileType)
    {
        if (tileWeights == null)
            return 0;

        foreach (TileWeightEntry entry in tileWeights)
        {
            if (entry != null && entry.tileType == tileType)
                return Mathf.Max(0, entry.weight);
        }

        return 0;
    }

    public int GetRecruitCostForCurrentRecruitCount(int recruitedCount)
    {
        if (recruitCostTiers == null || recruitCostTiers.Count == 0)
            return 0;

        int tierIndex = Mathf.Clamp(Mathf.Max(0, recruitedCount), 0, recruitCostTiers.Count - 1);
        return Mathf.Max(0, recruitCostTiers[tierIndex]);
    }

    public int GetMinimumCount(MapTileType tileType)
    {
        if (tileMinimums == null)
            return 0;

        foreach (TileMinimumEntry entry in tileMinimums)
        {
            if (entry != null && entry.tileType == tileType)
                return Mathf.Max(0, entry.minimumCount);
        }

        return 0;
    }

    public int GetActionPointLimitForCalamityTier(int calamityTier)
    {
        int requestedTier = Mathf.Max(0, calamityTier);
        int resolvedLimit = Mathf.Max(1, startingActionPoints);
        int bestTier = -1;

        if (actionPointTiers != null)
        {
            for (int i = 0; i < actionPointTiers.Count; i++)
            {
                ActionPointTierEntry entry = actionPointTiers[i];
                if (entry == null)
                    continue;

                int entryTier = Mathf.Max(0, entry.calamityTier);
                if (entryTier > requestedTier || entryTier < bestTier)
                    continue;

                bestTier = entryTier;
                resolvedLimit = Mathf.Max(1, entry.actionPointLimit);
            }
        }

        return resolvedLimit;
    }

    private void OnValidate()
    {
        boardSize = Mathf.Clamp(boardSize, 7, 13);
        if (boardSize % 2 == 0)
            boardSize += 1;

        startingActionPoints = Mathf.Max(1, startingActionPoints);
        if (actionPointTiers == null || actionPointTiers.Count == 0)
        {
            actionPointTiers = new List<ActionPointTierEntry>
            {
                new ActionPointTierEntry { calamityTier = 0, actionPointLimit = startingActionPoints }
            };
        }
        for (int i = 0; i < actionPointTiers.Count; i++)
        {
            if (actionPointTiers[i] == null)
            {
                actionPointTiers[i] = new ActionPointTierEntry
                {
                    calamityTier = 0,
                    actionPointLimit = startingActionPoints
                };
            }

            actionPointTiers[i].calamityTier = Mathf.Max(0, actionPointTiers[i].calamityTier);
            actionPointTiers[i].actionPointLimit = Mathf.Max(1, actionPointTiers[i].actionPointLimit);
        }

        bool hasTierZero = false;
        for (int i = 0; i < actionPointTiers.Count; i++)
        {
            if (actionPointTiers[i] != null && Mathf.Max(0, actionPointTiers[i].calamityTier) == 0)
            {
                hasTierZero = true;
                break;
            }
        }

        if (!hasTierZero)
        {
            actionPointTiers.Insert(0, new ActionPointTierEntry
            {
                calamityTier = 0,
                actionPointLimit = startingActionPoints
            });
        }

        for (int i = 0; i < actionPointTiers.Count; i++)
        {
            if (actionPointTiers[i] != null && Mathf.Max(0, actionPointTiers[i].calamityTier) == 0)
            {
                startingActionPoints = Mathf.Max(1, actionPointTiers[i].actionPointLimit);
                break;
            }
        }

        requiredKeys = Mathf.Max(1, requiredKeys);
        generatedKeyTileCount = Mathf.Max(requiredKeys, generatedKeyTileCount);
        spawnMinRingDistance = Mathf.Max(0, spawnMinRingDistance);
        maxRecruitCount = Mathf.Max(1, maxRecruitCount);

        if (recruitCostTiers == null || recruitCostTiers.Count == 0)
            recruitCostTiers = new List<int> { 50, 80, 120, 180 };

        while (recruitCostTiers.Count < 4)
            recruitCostTiers.Add(recruitCostTiers.Count > 0 ? recruitCostTiers[recruitCostTiers.Count - 1] : 50);

        for (int i = 0; i < recruitCostTiers.Count; i++)
            recruitCostTiers[i] = Mathf.Max(0, recruitCostTiers[i]);

        if (tileWeights == null || tileWeights.Count == 0)
        {
            tileWeights = new List<TileWeightEntry>
            {
                new TileWeightEntry { tileType = MapTileType.Battle, weight = 4 },
                new TileWeightEntry { tileType = MapTileType.Elite, weight = 1 },
                new TileWeightEntry { tileType = MapTileType.Event, weight = 4 },
                new TileWeightEntry { tileType = MapTileType.Merchant, weight = 1 },
                new TileWeightEntry { tileType = MapTileType.Rest, weight = 1 }
            };
        }

        EnsureWeightEntry(MapTileType.Battle, 4);
        EnsureWeightEntry(MapTileType.Elite, 1);
        EnsureWeightEntry(MapTileType.Event, 4);
        EnsureWeightEntry(MapTileType.Merchant, 1);
        EnsureWeightEntry(MapTileType.Forge, 1);
        EnsureWeightEntry(MapTileType.Rest, 1);

        if (tileMinimums == null)
            tileMinimums = new List<TileMinimumEntry>();

        EnsureMinimumEntry(MapTileType.Battle, 0);
        EnsureMinimumEntry(MapTileType.Elite, 0);
        EnsureMinimumEntry(MapTileType.Event, 0);
        EnsureMinimumEntry(MapTileType.Merchant, 0);
        EnsureMinimumEntry(MapTileType.Forge, 0);
        EnsureMinimumEntry(MapTileType.Rest, 0);

        for (int i = 0; i < tileMinimums.Count; i++)
        {
            if (tileMinimums[i] == null)
            {
                tileMinimums[i] = new TileMinimumEntry
                {
                    tileType = MapTileType.Battle,
                    minimumCount = 0
                };
            }

            tileMinimums[i].minimumCount = Mathf.Max(0, tileMinimums[i].minimumCount);
        }
    }

    private void EnsureWeightEntry(MapTileType tileType, int defaultWeight)
    {
        foreach (TileWeightEntry entry in tileWeights)
        {
            if (entry != null && entry.tileType == tileType)
                return;
        }

        tileWeights.Add(new TileWeightEntry
        {
            tileType = tileType,
            weight = defaultWeight
        });
    }

    private void EnsureMinimumEntry(MapTileType tileType, int defaultMinimumCount)
    {
        foreach (TileMinimumEntry entry in tileMinimums)
        {
            if (entry != null && entry.tileType == tileType)
                return;
        }

        tileMinimums.Add(new TileMinimumEntry
        {
            tileType = tileType,
            minimumCount = Mathf.Max(0, defaultMinimumCount)
        });
    }
}
