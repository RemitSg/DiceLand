using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

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

    [Serializable]
    public class MerchantRelicPriceEntry
    {
        public LootRarity rarity = LootRarity.Common;
        [FormerlySerializedAs("price")]
        [Min(0)] public int minPrice = 18;
        [Min(0)] public int maxPrice = 18;
    }

    [Serializable]
    public class BattleExperienceRewardEntry
    {
        public MapDangerBand dangerBand = MapDangerBand.Outer;
        public MapTileType tileType = MapTileType.Battle;
        [Min(0)] public int experience = 12;
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
    [FormerlySerializedAs("merchantRefreshCost")]
    [SerializeField, HideInInspector] private int legacyMerchantRefreshCost = 30;
    public List<int> merchantRefreshCostTiers = new List<int> { 30, 45, 60 };
    public List<MerchantRelicPriceEntry> merchantRelicPrices = new List<MerchantRelicPriceEntry>
    {
        new MerchantRelicPriceEntry { rarity = LootRarity.Common, minPrice = 14, maxPrice = 20 },
        new MerchantRelicPriceEntry { rarity = LootRarity.Uncommon, minPrice = 18, maxPrice = 26 },
        new MerchantRelicPriceEntry { rarity = LootRarity.Rare, minPrice = 26, maxPrice = 34 },
        new MerchantRelicPriceEntry { rarity = LootRarity.Epic, minPrice = 34, maxPrice = 42 },
        new MerchantRelicPriceEntry { rarity = LootRarity.Legendary, minPrice = 42, maxPrice = 52 }
    };
    public List<BattleExperienceRewardEntry> battleExperienceRewards = new List<BattleExperienceRewardEntry>
    {
        new BattleExperienceRewardEntry { dangerBand = MapDangerBand.Outer, tileType = MapTileType.Battle, experience = 12 },
        new BattleExperienceRewardEntry { dangerBand = MapDangerBand.Mid, tileType = MapTileType.Battle, experience = 16 },
        new BattleExperienceRewardEntry { dangerBand = MapDangerBand.Inner, tileType = MapTileType.Battle, experience = 20 },
        new BattleExperienceRewardEntry { dangerBand = MapDangerBand.Core, tileType = MapTileType.Battle, experience = 24 },
        new BattleExperienceRewardEntry { dangerBand = MapDangerBand.Outer, tileType = MapTileType.Elite, experience = 24 },
        new BattleExperienceRewardEntry { dangerBand = MapDangerBand.Mid, tileType = MapTileType.Elite, experience = 32 },
        new BattleExperienceRewardEntry { dangerBand = MapDangerBand.Inner, tileType = MapTileType.Elite, experience = 40 },
        new BattleExperienceRewardEntry { dangerBand = MapDangerBand.Core, tileType = MapTileType.Elite, experience = 48 }
    };

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

    public int GetMerchantRefreshCost(int refreshCount)
    {
        if (merchantRefreshCostTiers == null || merchantRefreshCostTiers.Count == 0)
            return Mathf.Max(0, legacyMerchantRefreshCost);

        int clampedIndex = Mathf.Clamp(Mathf.Max(0, refreshCount), 0, merchantRefreshCostTiers.Count - 1);
        return Mathf.Max(0, merchantRefreshCostTiers[clampedIndex]);
    }

    public int GetMerchantRelicPrice(LootRarity rarity, System.Random rng)
    {
        GetMerchantRelicPriceRange(rarity, out int minPrice, out int maxPrice);
        if (rng == null || maxPrice <= minPrice)
            return minPrice;

        return rng.Next(minPrice, maxPrice + 1);
    }

    public bool TryGetBattleExperienceReward(MapDangerBand dangerBand, MapTileType tileType, out int experience)
    {
        experience = 0;
        if (battleExperienceRewards == null)
            return false;

        for (int i = 0; i < battleExperienceRewards.Count; i++)
        {
            BattleExperienceRewardEntry entry = battleExperienceRewards[i];
            if (entry == null || entry.dangerBand != dangerBand || entry.tileType != tileType)
                continue;

            experience = Mathf.Max(0, entry.experience);
            return true;
        }

        return false;
    }

    public void GetMerchantRelicPriceRange(LootRarity rarity, out int minPrice, out int maxPrice)
    {
        if (merchantRelicPrices != null)
        {
            for (int i = 0; i < merchantRelicPrices.Count; i++)
            {
                MerchantRelicPriceEntry entry = merchantRelicPrices[i];
                if (entry == null || entry.rarity != rarity)
                    continue;

                minPrice = Mathf.Max(0, entry.minPrice);
                maxPrice = Mathf.Max(minPrice, entry.maxPrice);
                return;
            }
        }

        GetDefaultMerchantRelicPriceRange(rarity, out minPrice, out maxPrice);
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
        legacyMerchantRefreshCost = Mathf.Max(0, legacyMerchantRefreshCost);
        EnsureMerchantRefreshCosts();
        EnsureMerchantRelicPrices();
        EnsureBattleExperienceRewards();

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
                new TileWeightEntry { tileType = MapTileType.Treasure, weight = 1 },
                new TileWeightEntry { tileType = MapTileType.Rest, weight = 1 }
            };
        }

        EnsureWeightEntry(MapTileType.Battle, 4);
        EnsureWeightEntry(MapTileType.Elite, 1);
        EnsureWeightEntry(MapTileType.Event, 4);
        EnsureWeightEntry(MapTileType.Merchant, 1);
        EnsureWeightEntry(MapTileType.Treasure, 1);
        EnsureWeightEntry(MapTileType.Forge, 1);
        EnsureWeightEntry(MapTileType.Rest, 1);

        if (tileMinimums == null)
            tileMinimums = new List<TileMinimumEntry>();

        EnsureMinimumEntry(MapTileType.Battle, 0);
        EnsureMinimumEntry(MapTileType.Elite, 0);
        EnsureMinimumEntry(MapTileType.Event, 0);
        EnsureMinimumEntry(MapTileType.Merchant, 0);
        EnsureMinimumEntry(MapTileType.Treasure, 0);
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

    private void EnsureMerchantRefreshCosts()
    {
        if (merchantRefreshCostTiers == null)
            merchantRefreshCostTiers = new List<int>();

        if (merchantRefreshCostTiers.Count == 0)
        {
            int fallbackCost = Mathf.Max(0, legacyMerchantRefreshCost);
            if (fallbackCost <= 0)
                fallbackCost = 30;

            merchantRefreshCostTiers.Add(fallbackCost);
        }

        for (int i = 0; i < merchantRefreshCostTiers.Count; i++)
            merchantRefreshCostTiers[i] = Mathf.Max(0, merchantRefreshCostTiers[i]);
    }

    private void EnsureMerchantRelicPrices()
    {
        if (merchantRelicPrices == null)
            merchantRelicPrices = new List<MerchantRelicPriceEntry>();

        Array enumValues = Enum.GetValues(typeof(LootRarity));
        HashSet<LootRarity> seenRarities = new HashSet<LootRarity>();
        List<MerchantRelicPriceEntry> sanitizedEntries = new List<MerchantRelicPriceEntry>();

        for (int i = 0; i < merchantRelicPrices.Count; i++)
        {
            MerchantRelicPriceEntry entry = merchantRelicPrices[i];
            if (entry == null)
                continue;

            entry.minPrice = Mathf.Max(0, entry.minPrice);
            entry.maxPrice = Mathf.Max(entry.minPrice, entry.maxPrice);
            if (!seenRarities.Add(entry.rarity))
                continue;

            sanitizedEntries.Add(entry);
        }

        merchantRelicPrices = sanitizedEntries;

        for (int i = 0; i < enumValues.Length; i++)
        {
            LootRarity rarity = (LootRarity)enumValues.GetValue(i);
            if (seenRarities.Contains(rarity))
                continue;

            merchantRelicPrices.Add(new MerchantRelicPriceEntry
            {
                rarity = rarity,
                minPrice = GetDefaultMerchantRelicMinPrice(rarity),
                maxPrice = GetDefaultMerchantRelicMaxPrice(rarity)
            });
        }

        merchantRelicPrices.Sort((left, right) => left.rarity.CompareTo(right.rarity));
    }

    private void EnsureBattleExperienceRewards()
    {
        if (battleExperienceRewards == null)
            battleExperienceRewards = new List<BattleExperienceRewardEntry>();

        EnsureBattleExperienceRewardEntry(MapDangerBand.Outer, MapTileType.Battle, 12);
        EnsureBattleExperienceRewardEntry(MapDangerBand.Mid, MapTileType.Battle, 16);
        EnsureBattleExperienceRewardEntry(MapDangerBand.Inner, MapTileType.Battle, 20);
        EnsureBattleExperienceRewardEntry(MapDangerBand.Core, MapTileType.Battle, 24);
        EnsureBattleExperienceRewardEntry(MapDangerBand.Outer, MapTileType.Elite, 24);
        EnsureBattleExperienceRewardEntry(MapDangerBand.Mid, MapTileType.Elite, 32);
        EnsureBattleExperienceRewardEntry(MapDangerBand.Inner, MapTileType.Elite, 40);
        EnsureBattleExperienceRewardEntry(MapDangerBand.Core, MapTileType.Elite, 48);

        for (int i = 0; i < battleExperienceRewards.Count; i++)
        {
            if (battleExperienceRewards[i] == null)
            {
                battleExperienceRewards[i] = new BattleExperienceRewardEntry
                {
                    dangerBand = MapDangerBand.Outer,
                    tileType = MapTileType.Battle,
                    experience = 12
                };
            }

            battleExperienceRewards[i].experience = Mathf.Max(0, battleExperienceRewards[i].experience);
        }
    }

    private static void GetDefaultMerchantRelicPriceRange(LootRarity rarity, out int minPrice, out int maxPrice)
    {
        minPrice = GetDefaultMerchantRelicMinPrice(rarity);
        maxPrice = GetDefaultMerchantRelicMaxPrice(rarity);
    }

    private static int GetDefaultMerchantRelicMinPrice(LootRarity rarity)
    {
        switch (rarity)
        {
            case LootRarity.Trash:
                return 1;
            case LootRarity.Uncommon:
                return 18;
            case LootRarity.Rare:
                return 26;
            case LootRarity.Epic:
                return 34;
            case LootRarity.Legendary:
                return 42;
            case LootRarity.Common:
            default:
                return 14;
        }
    }

    private static int GetDefaultMerchantRelicMaxPrice(LootRarity rarity)
    {
        switch (rarity)
        {
            case LootRarity.Trash:
                return 1;
            case LootRarity.Uncommon:
                return 26;
            case LootRarity.Rare:
                return 34;
            case LootRarity.Epic:
                return 42;
            case LootRarity.Legendary:
                return 52;
            case LootRarity.Common:
            default:
                return 20;
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

    private void EnsureBattleExperienceRewardEntry(MapDangerBand dangerBand, MapTileType tileType, int defaultExperience)
    {
        for (int i = 0; i < battleExperienceRewards.Count; i++)
        {
            BattleExperienceRewardEntry entry = battleExperienceRewards[i];
            if (entry != null && entry.dangerBand == dangerBand && entry.tileType == tileType)
                return;
        }

        battleExperienceRewards.Add(new BattleExperienceRewardEntry
        {
            dangerBand = dangerBand,
            tileType = tileType,
            experience = Mathf.Max(0, defaultExperience)
        });
    }
}
