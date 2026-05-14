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

            choices.Add(BuildChoice(option, i));
        }

        return choices;
    }

    public static TreasureRewardResult ApplyChoice(RunState runState, SpecialDungeonTreasureOptionEntry option, int seed)
    {
        if (runState == null || option == null)
            return Fail("\u7279\u6b8a\u526f\u672c\u5b9d\u85cf\u9886\u53d6\u5931\u8d25\u3002");
        if (!option.IsConfigured)
            return Fail("\u8be5\u5b9d\u85cf\u9009\u9879\u914d\u7f6e\u4e0d\u5b8c\u6574\u3002");

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
                return Fail("\u672a\u77e5\u7684\u7279\u6b8a\u526f\u672c\u5b9d\u85cf\u5956\u52b1\u3002");
        }
    }

    public static SpecialDungeonTreasureOptionEntry ResolveOption(BoardTileData tile, TreasureRewardChoice choice)
    {
        int index = choice != null ? choice.specialDungeonOptionIndex : -1;
        if (tile == null || tile.specialDungeonTreasureOptions == null || index < 0 || index >= tile.specialDungeonTreasureOptions.Count)
            return null;

        return tile.specialDungeonTreasureOptions[index];
    }

    private static TreasureRewardChoice BuildChoice(SpecialDungeonTreasureOptionEntry option, int optionIndex)
    {
        TreasureRewardChoice choice = new TreasureRewardChoice
        {
            title = ResolveTitle(option),
            description = option.description,
            isAvailable = option.IsConfigured,
            unavailableReason = option.IsConfigured ? string.Empty : "\u8be5\u5b9d\u85cf\u9009\u9879\u914d\u7f6e\u4e0d\u5b8c\u6574\u3002",
            iconOverride = option.iconOverride,
            specialDungeonOptionIndex = optionIndex
        };

        switch (option.rewardType)
        {
            case SpecialDungeonTreasureRewardType.Money:
                choice.rewardType = TreasureRewardType.Wealth;
                choice.moneyAmount = Mathf.Max(0, option.moneyAmount);
                if (string.IsNullOrWhiteSpace(choice.description))
                    choice.description = $"\u83b7\u5f97 {choice.moneyAmount} \u91d1\u94b1";
                break;
            case SpecialDungeonTreasureRewardType.Blueprint:
                choice.rewardType = TreasureRewardType.Knowledge;
                choice.blueprint = option.blueprintData;
                if (string.IsNullOrWhiteSpace(choice.description))
                    choice.description = option.blueprintData != null
                        ? $"\u83b7\u5f97\u56fe\u7eb8\uff1a{GetBlueprintDisplayName(option.blueprintData)}"
                        : "\u83b7\u5f97\u56fe\u7eb8";
                break;
            case SpecialDungeonTreasureRewardType.Relic:
                choice.rewardType = TreasureRewardType.Power;
                choice.relic = option.relicData;
                if (string.IsNullOrWhiteSpace(choice.description))
                    choice.description = option.relicData != null
                        ? $"\u83b7\u5f97\u9057\u7269\uff1a{GetRelicDisplayName(option.relicData)}"
                        : "\u83b7\u5f97\u9057\u7269";
                break;
            case SpecialDungeonTreasureRewardType.RelicBag:
                choice.rewardType = TreasureRewardType.Power;
                choice.relicBagRarity = option.relicBagRarity;
                if (string.IsNullOrWhiteSpace(choice.description))
                    choice.description = $"\u6253\u5f00{TreasureRewardService.GetColoredRarityLabel(option.relicBagRarity)}\u9057\u7269\u888b";
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
            message = $"\u83b7\u5f97 {money} \u91d1\u94b1\u3002"
        };
    }

    private static TreasureRewardResult ApplyBlueprint(RunState runState, SpecialDungeonTreasureOptionEntry option)
    {
        runState.AddBlueprint(option.blueprintData, 1);
        return new TreasureRewardResult
        {
            success = true,
            blueprint = option.blueprintData,
            message = $"\u83b7\u5f97\u56fe\u7eb8\uff1a{GetBlueprintDisplayName(option.blueprintData)}\u3002"
        };
    }

    private static TreasureRewardResult ApplyRelic(RunState runState, SpecialDungeonTreasureOptionEntry option)
    {
        runState.AddRelic(option.relicData, 1);
        return new TreasureRewardResult
        {
            success = true,
            relic = option.relicData,
            message = $"\u83b7\u5f97\u9057\u7269\uff1a{GetRelicDisplayName(option.relicData)}\u3002"
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
            return Fail("\u672a\u627e\u5230\u53ef\u4ece\u9057\u7269\u888b\u4e2d\u83b7\u5f97\u7684\u9057\u7269\u3002");
        }

        choiceData.title = $"{TreasureRewardService.GetRarityLabel(option.relicBagRarity)}\u9057\u7269\u888b\uff1a\u9009\u62e9 1 \u4e2a\u9057\u7269";
        choiceData.requiredMessage = "\u8bf7\u5148\u4ece\u9057\u7269\u888b\u4e2d\u9009\u62e9\u4e00\u4e2a\u9057\u7269\u3002";
        return new TreasureRewardResult
        {
            success = true,
            relicChoiceData = choiceData,
            message = $"\u6253\u5f00{TreasureRewardService.GetColoredRarityLabel(option.relicBagRarity)}\u9057\u7269\u888b\uff0c\u8bf7\u9009\u62e9 1 \u4e2a\u9057\u7269\u3002"
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
                return "\u8d22\u5bcc";
            case SpecialDungeonTreasureRewardType.Blueprint:
                return "\u56fe\u7eb8";
            case SpecialDungeonTreasureRewardType.Relic:
                return "\u9057\u7269";
            case SpecialDungeonTreasureRewardType.RelicBag:
                return "\u9057\u7269\u888b";
            default:
                return "Reward";
        }
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
}
