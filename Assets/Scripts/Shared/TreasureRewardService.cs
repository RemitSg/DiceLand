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
            { choice.isAvailable = false; choice.unavailableReason = GameText.Get("ui.special_dungeon_treasure.no_relics_are_available_in_this_bag", "未找到可从遗物袋中获得的遗物。"); }
        }
        return choices;
    }

    public static TreasureRewardResult ApplyTileChoice(RunState state, TreasureRewardTable_SO table, BoardTileData tile, TreasureRewardChoice choice)
    {
        if (state == null || tile == null || tile.tileType != MapTileType.Treasure || tile.isResolved || choice == null || !choice.isAvailable)
            return Fail(GameText.Get("ui.treasure_reward.this_treasure_cannot_be_claimed", "宝藏奖励不可领取。"));
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
            return Fail(GameText.Get("ui.treasure_reward.this_relic_cannot_be_claimed", "遗物奖励不可领取。"));
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
            return Fail(GameText.Get("ui.treasure_reward.could_not_claim_the_treasure_reward", "宝藏奖励领取失败。"));
        if (!choice.isAvailable)
            return Fail(string.IsNullOrWhiteSpace(choice.unavailableReason) ? GameText.Get("ui.treasure_reward.this_treasure_reward_is_unavailable", "该宝藏选项暂不可用。") : choice.unavailableReason);

        switch (choice.rewardType)
        {
            case TreasureRewardType.Wealth:
                int gainedMoney = Mathf.Max(0, choice.moneyAmount);
                runState.AddMoney(gainedMoney);
                return new TreasureRewardResult
                {
                    success = true,
                    moneyAmount = gainedMoney,
                    message = GameText.Format("ui.treasure_reward.gained_gold", "获得 {gainedMoney} 金钱。", ("gainedMoney", gainedMoney))
                };
            case TreasureRewardType.Knowledge:
                if (choice.blueprint == null)
                    return Fail(GameText.Get("ui.treasure_reward.no_blueprint_is_available_to_claim", "未找到可领取的图纸。"));

                runState.AddPreviewedBlueprint(choice.blueprint, 1);
                return new TreasureRewardResult
                {
                    success = true,
                    blueprint = choice.blueprint,
                    message = GameText.Format("ui.special_dungeon_treasure.gained_blueprint", "获得图纸：{getBlueprintDisplayName}。", ("getBlueprintDisplayName", GetBlueprintDisplayName(choice.blueprint)))
                };
            case TreasureRewardType.Power:
                BattleSettlementRelicChoiceData relicChoiceData = BuildRelicChoiceData(runState, rewardTable, choice.relicBagRarity, seed);
                if (relicChoiceData == null || !relicChoiceData.HasChoices)
                    return Fail(GameText.Get("ui.special_dungeon_treasure.no_relics_are_available_in_this_bag", "未找到可从遗物袋中获得的遗物。"));

                return new TreasureRewardResult
                {
                    success = true,
                    relicChoiceData = relicChoiceData,
                    message = GameText.Format("ui.special_dungeon_treasure.open_a_relic_bag_and_choose_one_relic", "打开{getColoredRarityLabel}遗物袋，请选择 1 个遗物。", ("getColoredRarityLabel", GetColoredRarityLabel(choice.relicBagRarity)))
                };
            default:
                return Fail(GameText.Get("ui.treasure_reward.unknown_treasure_reward", "未知的宝藏奖励。"));
        }
    }

    public static TreasureRewardResult ApplyRelicChoice(RunState runState, BattleSettlementRelicChoiceData choiceData)
    {
        if (runState == null || choiceData == null)
            return Fail(GameText.Get("ui.treasure_reward.could_not_select_the_relic", "遗物选择失败。"));
        if (choiceData.rewardApplied)
            return Fail(GameText.Get("ui.treasure_reward.this_relic_reward_has_already_been_claimed", "该遗物奖励已经领取。"));

        BattleSettlementLootEntry selectedChoice = choiceData.selectedChoice;
        if (selectedChoice == null || selectedChoice.relicData == null || choiceData.choices == null || !choiceData.choices.Contains(selectedChoice))
            return Fail(GameText.Get("ui.map_treasure.choose_a_relic_first", "请先选择一个遗物。"));

        int amount = Mathf.Max(1, selectedChoice.amount);
        runState.AddPreviewedRelic(selectedChoice.relicData, amount);
        choiceData.rewardApplied = true;

        return new TreasureRewardResult
        {
            success = true,
            relic = selectedChoice.relicData,
            message = GameText.Format("ui.special_dungeon_treasure.gained_relic", "获得遗物：{getRelicDisplayName}。", ("getRelicDisplayName", GetRelicDisplayName(selectedChoice.relicData)))
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
            title = GameText.Get("ui.special_dungeon_treasure.wealth", "财富"),
            moneyAmount = money,
            description = GameText.Format("ui.treasure_reward.gain_gold", "获得 {money} 金钱", ("money", money))
        };
    }

    private static TreasureRewardChoice BuildKnowledgeChoice(TreasureRewardTable_SO rewardTable, int seed)
    {
        CraftingBlueprint_SO blueprint = DrawBlueprint(rewardTable, seed);
        bool available = blueprint != null;
        return new TreasureRewardChoice
        {
            rewardType = TreasureRewardType.Knowledge,
            title = GameText.Get("ui.treasure_reward.knowledge", "知识"),
            blueprint = blueprint,
            isAvailable = available,
            unavailableReason = available ? string.Empty : GameText.Get("ui.treasure_reward.no_blueprints_are_available_in_this_treasure_table", "宝藏图纸表未配置可用图纸。"),
            description = available
                ? GameText.Format("ui.treasure_reward.gain_one_blueprint", "获得 1 张图纸：{getBlueprintDisplayName}", ("getBlueprintDisplayName", GetBlueprintDisplayName(blueprint)))
                : GameText.Get("ui.map_treasure.no_blueprint_available", "暂无可用图纸")
        };
    }

    private static TreasureRewardChoice BuildPowerChoice(TreasureRewardTable_SO rewardTable, int seed)
    {
        bool hasRelics = rewardTable != null && rewardTable.RelicTable != null && rewardTable.RelicTable.IsValid;
        LootRarity rarity = SelectWeightedRarity(rewardTable != null ? rewardTable.RelicBagRarityWeightTable : null, CreateRandom(seed, RelicBagRaritySeedSalt));
        return new TreasureRewardChoice
        {
            rewardType = TreasureRewardType.Power,
            title = GameText.Get("ui.treasure_reward.power", "力量"),
            relicBagRarity = rarity,
            isAvailable = hasRelics,
            unavailableReason = hasRelics ? string.Empty : GameText.Get("ui.treasure_reward.no_relics_are_available_in_this_treasure_table", "宝藏遗物表未配置可用遗物。"),
            description = hasRelics
                ? GameText.Format("ui.treasure_reward.gain_one_relic_bag", "获得 1 个{getColoredRarityLabel}遗物袋", ("getColoredRarityLabel", GetColoredRarityLabel(rarity)))
                : GameText.Get("ui.treasure_reward.no_relics_available", "暂无可用遗物")
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

        choiceData.title = GameText.Format("ui.special_dungeon_treasure.relic_bag_choose_one_relic", "{getRarityLabel}遗物袋：选择 1 个遗物", ("getRarityLabel", GetRarityLabel(rarity)));
        choiceData.requiredMessage = GameText.Get("ui.special_dungeon_treasure.choose_a_relic_from_the_bag_first", "请先从遗物袋中选择一个遗物。");
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
            return GameText.Get("ui.map_hud.blueprints", "图纸");

        return !string.IsNullOrWhiteSpace(blueprint.DisplayName) ? blueprint.LocalizedName : blueprint.name;
    }

    private static string GetRelicDisplayName(Relic_SO relic)
    {
        if (relic == null)
            return GameText.Get("ui.map_hud.relics", "遗物");

        return !string.IsNullOrWhiteSpace(relic.DisplayName) ? relic.LocalizedName : relic.name;
    }

    public static string GetRarityLabel(LootRarity rarity)
    {
        switch (rarity)
        {
            case LootRarity.Trash:
                return "Trash";
            case LootRarity.Uncommon:
                return GameText.Get("ui.treasure_reward.uncommon", "罕见");
            case LootRarity.Rare:
                return GameText.Get("battle.settlement.rare", "稀有");
            case LootRarity.Epic:
                return GameText.Get("battle.settlement.epic", "史诗");
            case LootRarity.Legendary:
                return GameText.Get("ui.treasure_reward.legendary", "传奇");
            case LootRarity.Common:
            default:
                return GameText.Get("ui.treasure_reward.common", "常见");
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
