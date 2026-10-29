using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public sealed partial class AgentAdventureSession
{
    private AgentAdventureProfile profile;
    private readonly RunSettlementContext settlementContext = new RunSettlementContext();
    private readonly Func<long> utcNow;
    private string settlementPolicy;
    private int runIndex = 1;
    private BattleSettlementRelicChoiceData StartingRelic => map.StartingChoices.FirstOrDefault(c => c != null && c.HasChoices && !c.rewardApplied);

    private AgentAdventureSettlement DescribeSettlement() => new AgentAdventureSettlement
    {
        completed = phase == "finished", policy = settlementPolicy,
        // A caller observing the adapter cannot mutate the owned, once-awarded result.
        data = settlementContext.Current == null ? null : JsonUtility.FromJson<RunSettlementData>(JsonUtility.ToJson(settlementContext.Current))
    };

    private IEnumerable<AgentAdventureAction> SettlementActions()
    {
        if (phase == "victory_pending_settlement" || phase == "defeat_pending_settlement")
            yield return new AgentAdventureAction { op = "run_settle" };
        if (phase == "map" || phase == "battle") yield return new AgentAdventureAction { op = "run_abandon" };
        if (phase != "finished") yield break;
        yield return new AgentAdventureAction { op = "run_next" };
        AgentAdventureMeta meta = profile.Describe();
        foreach (var node in meta.nodes)
        {
            if (node.state == AbilityNodeState.Available.ToString() && meta.abilityPoints >= Mathf.Max(0, node.cost))
                yield return new AgentAdventureAction { op = "run_unlock_ability", assetId = node.id };
            if (node.state == AbilityNodeState.Unlocked.ToString() && node.toggleable)
                yield return new AgentAdventureAction { op = "run_set_ability", assetId = node.id, choiceIndex = node.enabled ? 0 : 1 };
        }
    }

    private void SettleRun()
    {
        RunSettlementFlowService.ExecuteSettlement(new RunSettlementFlowRequest
        {
            runState = map.Snapshot.runState, runCleared = outcome == "victory", settlementUnixSeconds = operationUtcSeconds,
            getAbilityPoints = profile.Wallet.GetCurrentPoints, addAbilityPoints = profile.AddPoints
        }, settlementContext);
        settlementPolicy = "shared_score";
        phase = "finished"; message = "冒险已结算，可管理隔离能力树或开始下一局。";
    }

    private void AbandonRun()
    {
        bool inBattle = phase == "battle";
        if (inBattle)
        {
            // Formal battle abandon carries only resolved party data, without claiming unconfirmed loot.
            RunSettlementFlowService.SyncResolvedPartyMembers(map.Snapshot.runState, bridge.Battle.Encounter);
            bridge.Dispose(); bridge = null;
        }
        outcome = "abandoned";
        if (inBattle) SettleRun();
        else
        {
            // RunGameManager's map abandon returns to menu without awarding ability points.
            settlementPolicy = "map_abandon_no_rewards"; phase = "finished";
            message = "已放弃冒险；沿用正式地图放弃规则，不发放能力点。";
        }
    }

    private void ExecuteMetaAction(AgentAdventureRequest request)
    {
        AbilityUnlockResult result = request.op == "run_unlock_ability" ? profile.Progression.TryUnlock(request.assetId)
            : profile.Progression.SetNodeEnabled(request.assetId, request.choiceIndex == 1);
        if (!result.success) throw new InvalidOperationException(result.message);
        if (profile.Wallet.GetCurrentPoints() != result.pointsAfter
            || profile.Progression.GetNodeState(request.assetId) != AbilityNodeState.Unlocked
            || (request.op == "run_set_ability" && profile.Progression.IsNodeEnabled(request.assetId) != (request.choiceIndex == 1)))
            throw new InvalidOperationException("Isolated ability progression could not be persisted.");
        message = result.message;
    }

    private void StartNextRun(int nextSeed, DifficultyConfig_SO nextDifficulty)
    {
        AgentAdventureConfiguration configuration = nextDifficulty != null && nextDifficulty != map.Configuration.starting.difficulty
            ? AgentAdventureConfiguration.Load(nextDifficulty) : map.Configuration;
        map.Dispose(); map = null;
        map = new AgentAdventureMapRuntime(configuration, nextSeed, profile.Progression.BuildRunBonus(), operationUtcSeconds);
        seed = nextSeed; runIndex++; outcome = message = settlementPolicy = null;
        settlementContext.Clear(); choices.Clear(); choiceTier = 0;
        ReleasePendingEventEncounter(); currentEvent = nextEvent = null; pendingEventDungeon = null;
        eventDepth = 0; eventResultContent = eventResultMessage = null; eventDiscardCache.Clear();
        eventOptions = Array.Empty<AgentAdventureEventOption>(); recruitCandidates.Clear(); ResetReforgeVisit();
        treasureOptions = Array.Empty<TreasureRewardChoice>(); treasureRelicChoice = null; treasureResult = null; treasureOptionIndex = -1;
        phase = StartingRelic != null ? "starting_relic" : "map";
    }

    private AgentAdventureStartingRelic DescribeStartingRelic()
    {
        if (phase != "starting_relic") return null;
        BattleSettlementRelicChoiceData bag = StartingRelic;
        return new AgentAdventureStartingRelic
        {
            bagsRemaining = map.StartingChoices.Count(c => c != null && c.HasChoices && !c.rewardApplied),
            refreshesRemaining = bag.refreshesRemaining, canRefresh = BattleRelicChoiceSelectionService.CanRefresh(bag),
            choices = bag.choices.Select((c, i) => new AgentBattleRewardItem
            { index = i, id = AssetId(c.relicData), name = c.displayName, description = c.description, amount = c.amount }).ToArray()
        };
    }

    private IEnumerable<AgentAdventureAction> StartingRelicActions()
    {
        BattleSettlementRelicChoiceData bag = StartingRelic;
        for (int i = 0; i < bag.choices.Count; i++)
            if (bag.choices[i]?.relicData != null) yield return new AgentAdventureAction { op = "run_choose_starting_relic", choiceIndex = i };
        if (BattleRelicChoiceSelectionService.CanRefresh(bag)) yield return new AgentAdventureAction { op = "run_refresh_starting_relics" };
    }

    private void ExecuteStartingRelic(AgentAdventureRequest request)
    {
        map.WithRandom(() =>
        {
            var bag = StartingRelic;
            if (request.op == "run_refresh_starting_relics")
            {
                if (!BattleRelicChoiceSelectionService.TryRefresh(bag, new System.Random(UnityEngine.Random.Range(int.MinValue, int.MaxValue))))
                    throw new InvalidOperationException("Starting relic choices could not be refreshed.");
            }
            else
            {
                bag.selectedChoice = bag.choices[request.choiceIndex];
                var result = TreasureRewardService.ApplyRelicChoice(map.Snapshot.runState, bag);
                if (!result.success) throw new InvalidOperationException(result.message);
                message = result.message;
            }
            return true;
        });
        if (StartingRelic == null) phase = "map";
    }
}
