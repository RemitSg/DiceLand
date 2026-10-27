using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

public sealed partial class AgentAdventureSession
{
    private IReadOnlyList<TreasureRewardChoice> treasureOptions = Array.Empty<TreasureRewardChoice>();
    private BattleSettlementRelicChoiceData treasureRelicChoice;
    private TreasureRewardResult treasureResult;
    private int treasureOptionIndex = -1;

    private bool OpenTreasureLocation(BoardTileData tile)
    {
        if (tile.tileType != MapTileType.Treasure || tile.isResolved) return false;
        tile.isVisited = true;
        treasureOptions = map.WithRandom(() => TreasureRewardService.BuildTileChoices(map.Snapshot.runState, map.Configuration.treasure, tile));
        treasureRelicChoice = null; treasureResult = null; treasureOptionIndex = -1;
        phase = "treasure"; message = "发现宝藏，请选择一份奖励。";
        return true;
    }

    private IEnumerable<AgentAdventureAction> TreasureActions()
    {
        if (phase == "treasure")
        {
            for (int i = 0; i < treasureOptions.Count; i++)
                if (treasureOptions[i].isAvailable) yield return new AgentAdventureAction { op = "run_choose_treasure", choiceIndex = i };
        }
        else if (phase == "treasure_relic")
        {
            if (treasureRelicChoice != null && !treasureRelicChoice.rewardApplied)
            {
                for (int i = 0; i < treasureRelicChoice.choices.Count; i++)
                    if (treasureRelicChoice.choices[i]?.relicData != null) yield return new AgentAdventureAction { op = "run_choose_treasure_relic", choiceIndex = i };
                if (BattleRelicChoiceSelectionService.CanRefresh(treasureRelicChoice)) yield return new AgentAdventureAction { op = "run_refresh_treasure_relics" };
            }
        }
        else if (phase == "treasure_result") yield return new AgentAdventureAction { op = "run_continue_treasure" };
    }

    private AgentAdventureTreasure DescribeTreasure()
    {
        if (phase != "treasure" && phase != "treasure_relic" && phase != "treasure_result") return null;
        return new AgentAdventureTreasure
        {
            selectedOptionIndex = treasureOptionIndex,
            refreshesRemaining = treasureRelicChoice != null ? treasureRelicChoice.refreshesRemaining : 0,
            canRefresh = phase == "treasure_relic" && BattleRelicChoiceSelectionService.CanRefresh(treasureRelicChoice),
            options = phase == "treasure" ? treasureOptions.Select((o, i) => new AgentAdventureTreasureOption
            {
                index = i, kind = o.rewardType == TreasureRewardType.Wealth ? "Money" : o.rewardType == TreasureRewardType.Knowledge ? "Blueprint" : o.relic != null ? "Relic" : "RelicBag",
                title = PlainTreasureText(o.title), description = PlainTreasureText(o.description), money = o.moneyAmount,
                selectable = o.isAvailable, unavailableReason = PlainTreasureText(o.unavailableReason), blueprint = AssetId(o.blueprint), relic = AssetId(o.relic),
                rarity = o.rewardType == TreasureRewardType.Power ? (o.relic != null ? o.relic.rarity : o.relicBagRarity).ToString() : o.blueprint != null ? o.blueprint.rarity.ToString() : ""
            }).ToArray() : Array.Empty<AgentAdventureTreasureOption>(),
            relicChoices = phase == "treasure_relic" ? treasureRelicChoice.choices.Select((entry, i) => new AgentBattleRewardItem
            { index = i, id = AssetId(entry.relicData), name = PlainTreasureText(entry.displayName), description = PlainTreasureText(entry.description), amount = entry.amount }).ToArray() : Array.Empty<AgentBattleRewardItem>(),
            result = treasureResult == null ? null : new AgentAdventureTreasureResult { message = PlainTreasureText(treasureResult.message), money = treasureResult.moneyAmount,
                blueprint = treasureResult.blueprint == null ? null : Item(treasureResult.blueprint, treasureResult.blueprint.DisplayName, 1),
                relic = treasureResult.relic == null ? null : Item(treasureResult.relic, treasureResult.relic.DisplayName, 1) }
        };
    }

    private void ExecuteTreasureAction(AgentAdventureRequest request)
    {
        map.WithRandom(() =>
        {
            if (request.op == "run_choose_treasure")
            {
                treasureOptionIndex = request.choiceIndex;
                treasureResult = TreasureRewardService.ApplyTileChoice(map.Snapshot.runState, map.Configuration.treasure, CurrentTile, treasureOptions[request.choiceIndex]);
                if (!treasureResult.success) throw new InvalidOperationException(treasureResult.message);
                treasureRelicChoice = treasureResult.relicChoiceData;
                phase = treasureRelicChoice != null && treasureRelicChoice.HasChoices ? "treasure_relic" : "treasure_result";
                message = PlainTreasureText(treasureResult.message);
            }
            else if (request.op == "run_choose_treasure_relic")
            {
                treasureRelicChoice.selectedChoice = treasureRelicChoice.choices[request.choiceIndex];
                treasureResult = TreasureRewardService.ApplyTileRelicChoice(map.Snapshot.runState, CurrentTile, treasureRelicChoice);
                if (!treasureResult.success) throw new InvalidOperationException(treasureResult.message);
                phase = "treasure_result"; message = PlainTreasureText(treasureResult.message);
            }
            else if (request.op == "run_refresh_treasure_relics")
            {
                if (!BattleRelicChoiceSelectionService.TryRefresh(treasureRelicChoice, new System.Random(UnityEngine.Random.Range(int.MinValue, int.MaxValue))))
                    throw new InvalidOperationException("Relic choices could not be refreshed.");
                message = "遗物选项已刷新，请重新选择。";
            }
            return true;
        });
        if (request.op == "run_continue_treasure")
        {
            treasureOptions = Array.Empty<TreasureRewardChoice>(); treasureRelicChoice = null;
            phase = "map"; ClearDeferrals(); PresentCalamity();
        }
    }

    private static string PlainTreasureText(string value) => Regex.Replace(value ?? "", "<[^>]+>", "");
}
