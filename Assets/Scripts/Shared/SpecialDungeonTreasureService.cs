using System.Collections.Generic;
using UnityEngine;

public static class SpecialDungeonTreasureService
{
    private const int RelicBagSeedSalt = unchecked((int)0x5D6E7A);

    public static IReadOnlyList<TreasureRewardChoice> BuildChoices(BoardTileData tile)
    {
        List<TreasureRewardChoice> choices = new List<TreasureRewardChoice>();
        if (tile == null || tile.specialDungeonTreasureOptions == null)
            return choices;

        for (int i = 0; i < tile.specialDungeonTreasureOptions.Count; i++)
        {
            SpecialDungeonTreasureOptionEntry option = tile.specialDungeonTreasureOptions[i];
            if (option == null)
                continue;

            choices.Add(BuildChoice(tile, option, i));
        }

        return choices;
    }

    public static TreasureRewardResult ApplyChoice(RunState runState, SpecialDungeonTreasureOptionEntry option, int seed)
    {
        if (runState == null || option == null)
            return Fail(GameText.Get("ui.special_dungeon_treasure.could_not_claim_the_dungeon_treasure", "特殊副本宝藏领取失败。"));
        if (!option.IsConfigured)
            return Fail(GameText.Get("ui.special_dungeon_treasure.this_treasure_reward_is_incomplete", "该宝藏选项配置不完整。"));

        switch (option.rewardType)
        {
            case SpecialDungeonTreasureRewardType.Money:
                return ApplyMoney(runState, option);
            case SpecialDungeonTreasureRewardType.Blueprint:
                return ApplyBlueprint(runState, option);
            case SpecialDungeonTreasureRewardType.Relic:
                return ApplyRelic(runState, option);
            case SpecialDungeonTreasureRewardType.RelicBag:
                return BuildRelicBagChoice(runState, option, seed);
            default:
                return Fail(GameText.Get("ui.special_dungeon_treasure.unknown_dungeon_treasure_reward", "未知的特殊副本宝藏奖励。"));
        }
    }

    public static SpecialDungeonTreasureOptionEntry ResolveOption(BoardTileData tile, TreasureRewardChoice choice)
    {
        int index = choice != null ? choice.specialDungeonOptionIndex : -1;
        if (tile == null || tile.specialDungeonTreasureOptions == null || index < 0 || index >= tile.specialDungeonTreasureOptions.Count)
            return null;

        return tile.specialDungeonTreasureOptions[index];
    }

    private static TreasureRewardChoice BuildChoice(BoardTileData tile, SpecialDungeonTreasureOptionEntry option, int optionIndex)
    {
        string title = ResolveTitle(option);
        string key = $"treasure.{optionIndex}";
        var dungeon = tile.sourceSpecialDungeon;
        TreasureRewardChoice choice = new TreasureRewardChoice
        {
            title = dungeon != null ? dungeon.LocalizeTile(tile.specialDungeonEntryIndex, key + ".name", title) : title,
            description = dungeon != null ? dungeon.LocalizeTile(tile.specialDungeonEntryIndex, key + ".description", option.description) : option.description,
            isAvailable = option.IsConfigured,
            unavailableReason = option.IsConfigured ? string.Empty : GameText.Get("ui.special_dungeon_treasure.this_treasure_reward_is_incomplete", "该宝藏选项配置不完整。"),
            iconOverride = option.iconOverride,
            specialDungeonOptionIndex = optionIndex
        };

        switch (option.rewardType)
        {
            case SpecialDungeonTreasureRewardType.Money:
                choice.rewardType = TreasureRewardType.Wealth;
                choice.moneyAmount = Mathf.Max(0, option.moneyAmount);
                if (string.IsNullOrWhiteSpace(choice.description))
                    choice.description = GameText.Format("ui.special_dungeon_treasure.gain_gold", "获得 {moneyAmount} 金钱", ("moneyAmount", choice.moneyAmount));
                break;
            case SpecialDungeonTreasureRewardType.Blueprint:
                choice.rewardType = TreasureRewardType.Knowledge;
                choice.blueprint = option.blueprintData;
                if (string.IsNullOrWhiteSpace(choice.description))
                    choice.description = option.blueprintData != null
                        ? GameText.Format("ui.special_dungeon_treasure.gain_blueprint", "获得图纸：{getBlueprintDisplayName}", ("getBlueprintDisplayName", GetBlueprintDisplayName(option.blueprintData)))
                        : GameText.Get("ui.map_inventory_detail_view.blueprint_acquired", "获得图纸");
                break;
            case SpecialDungeonTreasureRewardType.Relic:
                choice.rewardType = TreasureRewardType.Power;
                choice.relic = option.relicData;
                if (string.IsNullOrWhiteSpace(choice.description))
                    choice.description = option.relicData != null
                        ? GameText.Format("ui.special_dungeon_treasure.gain_relic", "获得遗物：{getRelicDisplayName}", ("getRelicDisplayName", GetRelicDisplayName(option.relicData)))
                        : GameText.Get("ui.map_inventory_detail_view.relic_acquired", "获得遗物");
                break;
            case SpecialDungeonTreasureRewardType.RelicBag:
                choice.rewardType = TreasureRewardType.Power;
                choice.relicBagRarity = option.relicBagRarity;
                if (string.IsNullOrWhiteSpace(choice.description))
                    choice.description = GameText.Format("ui.special_dungeon_treasure.open_a_relic_bag", "打开{getColoredRarityLabel}遗物袋", ("getColoredRarityLabel", TreasureRewardService.GetColoredRarityLabel(option.relicBagRarity)));
                break;
        }

        return choice;
    }

    private static TreasureRewardResult ApplyMoney(RunState runState, SpecialDungeonTreasureOptionEntry option)
    {
        int money = Mathf.Max(0, option.moneyAmount);
        runState.AddMoney(money);
        return new TreasureRewardResult
        {
            success = true,
            moneyAmount = money,
            message = GameText.Format("ui.special_dungeon_treasure.gained_gold", "获得 {money} 金钱。", ("money", money))
        };
    }

    private static TreasureRewardResult ApplyBlueprint(RunState runState, SpecialDungeonTreasureOptionEntry option)
    {
        runState.AddPreviewedBlueprint(option.blueprintData, 1);
        return new TreasureRewardResult
        {
            success = true,
            blueprint = option.blueprintData,
            message = GameText.Format("ui.special_dungeon_treasure.gained_blueprint", "获得图纸：{getBlueprintDisplayName}。", ("getBlueprintDisplayName", GetBlueprintDisplayName(option.blueprintData)))
        };
    }

    private static TreasureRewardResult ApplyRelic(RunState runState, SpecialDungeonTreasureOptionEntry option)
    {
        runState.AddPreviewedRelic(option.relicData, 1);
        return new TreasureRewardResult
        {
            success = true,
            relic = option.relicData,
            message = GameText.Format("ui.special_dungeon_treasure.gained_relic", "获得遗物：{getRelicDisplayName}。", ("getRelicDisplayName", GetRelicDisplayName(option.relicData)))
        };
    }

    private static TreasureRewardResult BuildRelicBagChoice(RunState runState, SpecialDungeonTreasureOptionEntry option, int seed)
    {
        HashSet<Relic_SO> ownedRelics = BattleRelicInventorySetBuilder.BuildOwnedRelicSet(runState.relicInventory);
        System.Random rng = CreateRandom(seed, option.relicBagRarity);
        if (!BattleEliteRelicChoiceBuilderService.TryBuildFromRelicsWithRarity(
                out BattleSettlementRelicChoiceData choiceData,
                option.relicTable.Relics,
                option.relicBagRarity,
                rng,
                ownedRelics,
                Mathf.Max(1, option.relicBagChoiceCount)))
        {
            return Fail(GameText.Get("ui.special_dungeon_treasure.no_relics_are_available_in_this_bag", "未找到可从遗物袋中获得的遗物。"));
        }

        choiceData.title = GameText.Format("ui.special_dungeon_treasure.relic_bag_choose_one_relic", "{getRarityLabel}遗物袋：选择 1 个遗物", ("getRarityLabel", TreasureRewardService.GetRarityLabel(option.relicBagRarity)));
        choiceData.requiredMessage = GameText.Get("ui.special_dungeon_treasure.choose_a_relic_from_the_bag_first", "请先从遗物袋中选择一个遗物。");
        return new TreasureRewardResult
        {
            success = true,
            relicChoiceData = choiceData,
            message = GameText.Format("ui.special_dungeon_treasure.open_a_relic_bag_and_choose_one_relic", "打开{getColoredRarityLabel}遗物袋，请选择 1 个遗物。", ("getColoredRarityLabel", TreasureRewardService.GetColoredRarityLabel(option.relicBagRarity)))
        };
    }

    private static System.Random CreateRandom(int seed, LootRarity rarity)
    {
        unchecked
        {
            return new System.Random((seed * 397) ^ RelicBagSeedSalt ^ (int)rarity);
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

    private static string ResolveTitle(SpecialDungeonTreasureOptionEntry option)
    {
        if (option == null)
            return "Reward";
        if (!string.IsNullOrWhiteSpace(option.title))
            return option.title;

        switch (option.rewardType)
        {
            case SpecialDungeonTreasureRewardType.Money:
                return GameText.Get("ui.special_dungeon_treasure.wealth", "财富");
            case SpecialDungeonTreasureRewardType.Blueprint:
                return GameText.Get("ui.map_hud.blueprints", "图纸");
            case SpecialDungeonTreasureRewardType.Relic:
                return GameText.Get("ui.map_hud.relics", "遗物");
            case SpecialDungeonTreasureRewardType.RelicBag:
                return GameText.Get("ui.map_treasure.relic_bag", "遗物袋");
            default:
                return "Reward";
        }
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
}
