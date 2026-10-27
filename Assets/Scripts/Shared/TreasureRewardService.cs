using System;
using System.Collections.Generic;
using UnityEngine;

public enum TreasureRewardType
{
    Wealth = 0,
    Knowledge = 1,
    Power = 2
}

public sealed class TreasureRewardChoice
{
    public TreasureRewardType rewardType;
    public string title;
    public string description;
    public bool isAvailable = true;
    public string unavailableReason;
    public int moneyAmount;
    public CraftingBlueprint_SO blueprint;
    public LootRarity relicBagRarity;
    public Relic_SO relic;
    public Sprite iconOverride;
    public int specialDungeonOptionIndex = -1;
}

public sealed class TreasureRewardResult
{
    public bool success;
    public string message;
    public Relic_SO relic;
    public CraftingBlueprint_SO blueprint;
    public int moneyAmount;
    public BattleSettlementRelicChoiceData relicChoiceData;
}

public static class TreasureRewardService
{
    private const int BlueprintSeedSalt = unchecked((int)0xB10EAA01);
    private const int RelicBagRaritySeedSalt = unchecked((int)0x7E1CBA6);
    private const int RelicContentSeedSalt = unchecked((int)0x5A17C0DE);

    public static IReadOnlyList<TreasureRewardChoice> BuildChoices(TreasureRewardTable_SO rewardTable, int seed)
    {
        List<TreasureRewardChoice> choices = new List<TreasureRewardChoice>(3);

        choices.Add(BuildWealthChoice(rewardTable, seed));
        choices.Add(BuildKnowledgeChoice(rewardTable, seed));
        choices.Add(BuildPowerChoice(rewardTable, seed));

        return choices;
    }

    public static bool IsSpecialDungeonTreasure(BoardTileData tile) => tile != null && tile.sourceSpecialDungeon != null
        && tile.specialDungeonTreasureOptions != null && tile.specialDungeonTreasureOptions.Count > 0;

    public static IReadOnlyList<TreasureRewardChoice> BuildTileChoices(RunState state, TreasureRewardTable_SO table, BoardTileData tile)
    {
        if (state == null || tile == null || tile.tileType != MapTileType.Treasure || tile.isResolved)
            return Array.Empty<TreasureRewardChoice>();
        table = AbilityRunBonusRuntimeService.ResolveTreasureRewardTable(state, table);
        bool special = IsSpecialDungeonTreasure(tile);
        IReadOnlyList<TreasureRewardChoice> choices = special ? SpecialDungeonTreasureService.BuildChoices(tile) : BuildChoices(table, tile.encounterSeed);
        foreach (TreasureRewardChoice choice in choices)
        {
            if (!choice.isAvailable || choice.rewardType != TreasureRewardType.Power || choice.relic != null) continue;
            // Preview bag availability without granting anything or revealing the rolled contents.
            BattleSettlementRelicChoiceData bag = special
                ? SpecialDungeonTreasureService.ApplyChoice(state, SpecialDungeonTreasureService.ResolveOption(tile, choice), tile.encounterSeed).relicChoiceData
                : BuildRelicChoiceData(state, table, choice.relicBagRarity, tile.encounterSeed);
            if (bag == null || !bag.HasChoices)
            { choice.isAvailable = false; choice.unavailableReason = "未找到可从遗物袋中获得的遗物。"; }
        }
        return choices;
    }

    public static TreasureRewardResult ApplyTileChoice(RunState state, TreasureRewardTable_SO table, BoardTileData tile, TreasureRewardChoice choice)
    {
        if (state == null || tile == null || tile.tileType != MapTileType.Treasure || tile.isResolved || choice == null || !choice.isAvailable)
            return Fail("宝藏奖励不可领取。");
        TreasureRewardResult result = IsSpecialDungeonTreasure(tile)
            ? SpecialDungeonTreasureService.ApplyChoice(state, SpecialDungeonTreasureService.ResolveOption(tile, choice), tile.encounterSeed)
            : ApplyChoice(state, AbilityRunBonusRuntimeService.ResolveTreasureRewardTable(state, table), choice, tile.encounterSeed);
        if (!result.success) return result;
        if (result.relicChoiceData != null && result.relicChoiceData.HasChoices)
            AbilityRunBonusRuntimeService.ApplyRelicChoiceRefreshBonus(state, result.relicChoiceData);
        else { tile.isVisited = true; tile.isResolved = true; }
        return result;
    }

    public static TreasureRewardResult ApplyTileRelicChoice(RunState state, BoardTileData tile, BattleSettlementRelicChoiceData choice)
    {
        if (state == null || tile == null || tile.tileType != MapTileType.Treasure || tile.isResolved)
            return Fail("遗物奖励不可领取。");
        TreasureRewardResult result = ApplyRelicChoice(state, choice);
        if (result.success) { tile.isVisited = true; tile.isResolved = true; }
        return result;
    }

    public static TreasureRewardResult ApplyChoice(
        RunState runState,
        TreasureRewardTable_SO rewardTable,
        TreasureRewardChoice choice,
        int seed)
    {
        if (runState == null || choice == null)
            return Fail("\u5b9d\u85cf\u5956\u52b1\u9886\u53d6\u5931\u8d25\u3002");
        if (!choice.isAvailable)
            return Fail(string.IsNullOrWhiteSpace(choice.unavailableReason) ? "\u8be5\u5b9d\u85cf\u9009\u9879\u6682\u4e0d\u53ef\u7528\u3002" : choice.unavailableReason);

        switch (choice.rewardType)
        {
            case TreasureRewardType.Wealth:
                int gainedMoney = Mathf.Max(0, choice.moneyAmount);
                runState.AddMoney(gainedMoney);
                return new TreasureRewardResult
                {
                    success = true,
                    moneyAmount = gainedMoney,
                    message = $"\u83b7\u5f97 {gainedMoney} \u91d1\u94b1\u3002"
                };
            case TreasureRewardType.Knowledge:
                if (choice.blueprint == null)
                    return Fail("\u672a\u627e\u5230\u53ef\u9886\u53d6\u7684\u56fe\u7eb8\u3002");

                runState.AddBlueprint(choice.blueprint, 1);
                return new TreasureRewardResult
                {
                    success = true,
                    blueprint = choice.blueprint,
                    message = $"\u83b7\u5f97\u56fe\u7eb8\uff1a{GetBlueprintDisplayName(choice.blueprint)}\u3002"
                };
            case TreasureRewardType.Power:
                BattleSettlementRelicChoiceData relicChoiceData = BuildRelicChoiceData(runState, rewardTable, choice.relicBagRarity, seed);
                if (relicChoiceData == null || !relicChoiceData.HasChoices)
                    return Fail("\u672a\u627e\u5230\u53ef\u4ece\u9057\u7269\u888b\u4e2d\u83b7\u5f97\u7684\u9057\u7269\u3002");

                return new TreasureRewardResult
                {
                    success = true,
                    relicChoiceData = relicChoiceData,
                    message = $"\u6253\u5f00{GetColoredRarityLabel(choice.relicBagRarity)}\u9057\u7269\u888b\uff0c\u8bf7\u9009\u62e9 1 \u4e2a\u9057\u7269\u3002"
                };
            default:
                return Fail("\u672a\u77e5\u7684\u5b9d\u85cf\u5956\u52b1\u3002");
        }
    }

    public static TreasureRewardResult ApplyRelicChoice(RunState runState, BattleSettlementRelicChoiceData choiceData)
    {
        if (runState == null || choiceData == null)
            return Fail("\u9057\u7269\u9009\u62e9\u5931\u8d25\u3002");
        if (choiceData.rewardApplied)
            return Fail("\u8be5\u9057\u7269\u5956\u52b1\u5df2\u7ecf\u9886\u53d6\u3002");

        BattleSettlementLootEntry selectedChoice = choiceData.selectedChoice;
        if (selectedChoice == null || selectedChoice.relicData == null || choiceData.choices == null || !choiceData.choices.Contains(selectedChoice))
            return Fail("\u8bf7\u5148\u9009\u62e9\u4e00\u4e2a\u9057\u7269\u3002");

        int amount = Mathf.Max(1, selectedChoice.amount);
        runState.AddRelic(selectedChoice.relicData, amount);
        choiceData.rewardApplied = true;

        return new TreasureRewardResult
        {
            success = true,
            relic = selectedChoice.relicData,
            message = $"\u83b7\u5f97\u9057\u7269\uff1a{GetRelicDisplayName(selectedChoice.relicData)}\u3002"
        };
    }

    private static TreasureRewardChoice BuildWealthChoice(TreasureRewardTable_SO rewardTable, int seed)
    {
        int minMoney = rewardTable != null ? rewardTable.MinMoney : 30;
        int maxMoney = rewardTable != null ? rewardTable.MaxMoney : 50;
        System.Random rng = CreateRandom(seed, 0x51A7);
        int money = maxMoney > minMoney ? rng.Next(minMoney, maxMoney + 1) : minMoney;

        return new TreasureRewardChoice
        {
            rewardType = TreasureRewardType.Wealth,
            title = "\u8d22\u5bcc",
            moneyAmount = money,
            description = $"\u83b7\u5f97 {money} \u91d1\u94b1"
        };
    }

    private static TreasureRewardChoice BuildKnowledgeChoice(TreasureRewardTable_SO rewardTable, int seed)
    {
        CraftingBlueprint_SO blueprint = DrawBlueprint(rewardTable, seed);
        bool available = blueprint != null;
        return new TreasureRewardChoice
        {
            rewardType = TreasureRewardType.Knowledge,
            title = "\u77e5\u8bc6",
            blueprint = blueprint,
            isAvailable = available,
            unavailableReason = available ? string.Empty : "\u5b9d\u85cf\u56fe\u7eb8\u8868\u672a\u914d\u7f6e\u53ef\u7528\u56fe\u7eb8\u3002",
            description = available
                ? $"\u83b7\u5f97 1 \u5f20\u56fe\u7eb8\uff1a{GetBlueprintDisplayName(blueprint)}"
                : "\u6682\u65e0\u53ef\u7528\u56fe\u7eb8"
        };
    }

    private static TreasureRewardChoice BuildPowerChoice(TreasureRewardTable_SO rewardTable, int seed)
    {
        bool hasRelics = rewardTable != null && rewardTable.RelicTable != null && rewardTable.RelicTable.IsValid;
        LootRarity rarity = SelectWeightedRarity(rewardTable != null ? rewardTable.RelicBagRarityWeightTable : null, CreateRandom(seed, RelicBagRaritySeedSalt));
        return new TreasureRewardChoice
        {
            rewardType = TreasureRewardType.Power,
            title = "\u529b\u91cf",
            relicBagRarity = rarity,
            isAvailable = hasRelics,
            unavailableReason = hasRelics ? string.Empty : "\u5b9d\u85cf\u9057\u7269\u8868\u672a\u914d\u7f6e\u53ef\u7528\u9057\u7269\u3002",
            description = hasRelics
                ? $"\u83b7\u5f97 1 \u4e2a{GetColoredRarityLabel(rarity)}\u9057\u7269\u888b"
                : "\u6682\u65e0\u53ef\u7528\u9057\u7269"
        };
    }

    private static CraftingBlueprint_SO DrawBlueprint(TreasureRewardTable_SO rewardTable, int seed)
    {
        if (rewardTable == null || rewardTable.BlueprintTable == null)
            return null;

        IReadOnlyList<CraftingBlueprint_SO> blueprints = rewardTable.BlueprintTable.GetValidBlueprints();
        if (blueprints == null || blueprints.Count == 0)
            return null;

        List<CraftingBlueprint_SO> candidates = new List<CraftingBlueprint_SO>(blueprints);
        Dictionary<LootRarity, List<CraftingBlueprint_SO>> buckets = BuildBlueprintBuckets(candidates);
        System.Random rng = CreateRandom(seed, BlueprintSeedSalt);
        if (rewardTable.BlueprintRarityWeightTable != null && rewardTable.BlueprintRarityWeightTable.HasPositiveWeights)
        {
            LootRarity selectedRarity = SelectWeightedRarity(rewardTable.BlueprintRarityWeightTable, rng);
            List<CraftingBlueprint_SO> bucket = ResolveBlueprintBucketWithFallback(buckets, selectedRarity);
            if (bucket != null && bucket.Count > 0)
                return bucket[rng.Next(bucket.Count)];
        }

        return candidates[rng.Next(candidates.Count)];
    }

    private static BattleSettlementRelicChoiceData BuildRelicChoiceData(
        RunState runState,
        TreasureRewardTable_SO rewardTable,
        LootRarity rarity,
        int seed)
    {
        if (rewardTable == null || rewardTable.RelicTable == null || rewardTable.RelicTable.Relics == null)
            return null;

        HashSet<Relic_SO> ownedRelics = BattleRelicInventorySetBuilder.BuildOwnedRelicSet(runState != null ? runState.relicInventory : null);
        System.Random rng = CreateRandom(seed, RelicContentSeedSalt ^ (int)rarity);
        if (!BattleEliteRelicChoiceBuilderService.TryBuildFromRelicsWithRarity(
                out BattleSettlementRelicChoiceData choiceData,
                rewardTable.RelicTable.Relics,
                rarity,
                rng,
                ownedRelics,
                3))
            return null;

        choiceData.title = $"{GetRarityLabel(rarity)}\u9057\u7269\u888b\uff1a\u9009\u62e9 1 \u4e2a\u9057\u7269";
        choiceData.requiredMessage = "\u8bf7\u5148\u4ece\u9057\u7269\u888b\u4e2d\u9009\u62e9\u4e00\u4e2a\u9057\u7269\u3002";
        return choiceData;
    }

    private static LootRarity SelectWeightedRarity(LootRarityWeightTable_SO weightTable, System.Random rng)
    {
        if (rng == null)
            rng = new System.Random();

        if (weightTable == null || !weightTable.HasPositiveWeights)
            return LootRarity.Common;

        float totalWeight = 0f;
        List<LootRarity> rarities = new List<LootRarity>();
        List<float> cumulative = new List<float>();
        for (int i = (int)LootRarity.Common; i <= (int)LootRarity.Legendary; i++)
        {
            LootRarity rarity = (LootRarity)i;
            float weight = Mathf.Max(0f, weightTable.GetWeight(rarity));
            if (weight <= 0f)
                continue;

            totalWeight += weight;
            rarities.Add(rarity);
            cumulative.Add(totalWeight);
        }

        if (totalWeight <= 0f || rarities.Count == 0)
            return LootRarity.Common;

        double roll = rng.NextDouble() * totalWeight;
        for (int i = 0; i < cumulative.Count; i++)
        {
            if (roll <= cumulative[i])
                return rarities[i];
        }

        return rarities[rarities.Count - 1];
    }

    private static Dictionary<LootRarity, List<CraftingBlueprint_SO>> BuildBlueprintBuckets(List<CraftingBlueprint_SO> blueprints)
    {
        Dictionary<LootRarity, List<CraftingBlueprint_SO>> buckets = new Dictionary<LootRarity, List<CraftingBlueprint_SO>>();
        for (int i = 0; i < blueprints.Count; i++)
        {
            CraftingBlueprint_SO blueprint = blueprints[i];
            if (blueprint == null)
                continue;

            if (!buckets.TryGetValue(blueprint.rarity, out List<CraftingBlueprint_SO> bucket))
            {
                bucket = new List<CraftingBlueprint_SO>();
                buckets.Add(blueprint.rarity, bucket);
            }

            bucket.Add(blueprint);
        }

        return buckets;
    }

    private static Dictionary<LootRarity, List<Relic_SO>> BuildRelicBuckets(List<Relic_SO> relics)
    {
        Dictionary<LootRarity, List<Relic_SO>> buckets = new Dictionary<LootRarity, List<Relic_SO>>();
        for (int i = 0; i < relics.Count; i++)
        {
            Relic_SO relic = relics[i];
            if (relic == null)
                continue;

            if (!buckets.TryGetValue(relic.rarity, out List<Relic_SO> bucket))
            {
                bucket = new List<Relic_SO>();
                buckets.Add(relic.rarity, bucket);
            }

            bucket.Add(relic);
        }

        return buckets;
    }

    private static List<CraftingBlueprint_SO> ResolveBlueprintBucketWithFallback(
        Dictionary<LootRarity, List<CraftingBlueprint_SO>> buckets,
        LootRarity selectedRarity)
    {
        if (TryGetNonEmptyBucket(buckets, selectedRarity, out List<CraftingBlueprint_SO> directBucket))
            return directBucket;

        for (int i = (int)selectedRarity - 1; i >= (int)LootRarity.Common; i--)
        {
            if (TryGetNonEmptyBucket(buckets, (LootRarity)i, out List<CraftingBlueprint_SO> lowerBucket))
                return lowerBucket;
        }

        for (int i = (int)selectedRarity + 1; i <= (int)LootRarity.Legendary; i++)
        {
            if (TryGetNonEmptyBucket(buckets, (LootRarity)i, out List<CraftingBlueprint_SO> higherBucket))
                return higherBucket;
        }

        return null;
    }

    private static List<Relic_SO> ResolveRelicBucketWithFallback(
        Dictionary<LootRarity, List<Relic_SO>> buckets,
        LootRarity selectedRarity)
    {
        if (TryGetNonEmptyBucket(buckets, selectedRarity, out List<Relic_SO> directBucket))
            return directBucket;

        for (int i = (int)selectedRarity - 1; i >= (int)LootRarity.Common; i--)
        {
            if (TryGetNonEmptyBucket(buckets, (LootRarity)i, out List<Relic_SO> lowerBucket))
                return lowerBucket;
        }

        for (int i = (int)selectedRarity + 1; i <= (int)LootRarity.Legendary; i++)
        {
            if (TryGetNonEmptyBucket(buckets, (LootRarity)i, out List<Relic_SO> higherBucket))
                return higherBucket;
        }

        return null;
    }

    private static bool TryGetNonEmptyBucket<T>(
        Dictionary<LootRarity, List<T>> buckets,
        LootRarity rarity,
        out List<T> bucket)
    {
        bucket = null;
        if (buckets == null)
            return false;

        if (!buckets.TryGetValue(rarity, out List<T> foundBucket) || foundBucket == null || foundBucket.Count == 0)
            return false;

        bucket = foundBucket;
        return true;
    }

    private static System.Random CreateRandom(int seed, int salt)
    {
        unchecked
        {
            return new System.Random((seed * 397) ^ salt);
        }
    }

    private static TreasureRewardResult Fail(string message)
    {
        return new TreasureRewardResult
        {
            success = false,
            message = message
        };
    }

    private static string GetBlueprintDisplayName(CraftingBlueprint_SO blueprint)
    {
        if (blueprint == null)
            return "\u56fe\u7eb8";

        return !string.IsNullOrWhiteSpace(blueprint.DisplayName) ? blueprint.DisplayName : blueprint.name;
    }

    private static string GetRelicDisplayName(Relic_SO relic)
    {
        if (relic == null)
            return "\u9057\u7269";

        return !string.IsNullOrWhiteSpace(relic.DisplayName) ? relic.DisplayName : relic.name;
    }

    public static string GetRarityLabel(LootRarity rarity)
    {
        switch (rarity)
        {
            case LootRarity.Trash:
                return "Trash";
            case LootRarity.Uncommon:
                return "\u7f55\u89c1";
            case LootRarity.Rare:
                return "\u7a00\u6709";
            case LootRarity.Epic:
                return "\u53f2\u8bd7";
            case LootRarity.Legendary:
                return "\u4f20\u5947";
            case LootRarity.Common:
            default:
                return "\u5e38\u89c1";
        }
    }

    public static string GetColoredRarityLabel(LootRarity rarity)
    {
        return $"<color=#{ColorUtility.ToHtmlStringRGB(GetRarityColor(rarity))}>{GetRarityLabel(rarity)}</color>";
    }

    public static Color GetRarityColor(LootRarity rarity)
    {
        switch (rarity)
        {
            case LootRarity.Trash:
                return new Color32(126, 83, 48, 255);
            case LootRarity.Uncommon:
                return new Color32(56, 178, 86, 255);
            case LootRarity.Rare:
                return new Color32(64, 126, 255, 255);
            case LootRarity.Epic:
                return new Color32(163, 92, 255, 255);
            case LootRarity.Legendary:
                return new Color32(230, 180, 44, 255);
            case LootRarity.Common:
            default:
                return new Color32(128, 128, 128, 255);
        }
    }
}
