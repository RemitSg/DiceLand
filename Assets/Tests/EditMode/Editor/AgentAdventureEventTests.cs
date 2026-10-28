using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

public sealed class AgentAdventureEventTests
{
    private AgentAdventureSession session;
    private readonly List<UnityEngine.Object> temporary = new List<UnityEngine.Object>();
    private Random.State outside;
    private MapRunSnapshot global;
    [SetUp] public void SetUp()
    {
        outside = Random.state;
        global = (MapRunSnapshot)typeof(MapRunRuntime).GetField("currentSnapshot", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
    }
    [TearDown] public void TearDown()
    {
        session?.Dispose(); session = null;
        foreach (var obj in temporary) if (obj != null) UnityEngine.Object.DestroyImmediate(obj);
        temporary.Clear(); Random.state = outside; MapRunRuntime.RestoreSnapshot(global);
    }
    private AgentAdventureMapRuntime Map => (AgentAdventureMapRuntime)typeof(AgentAdventureSession).GetField("map", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(session);
    private T Temp<T>() where T : ScriptableObject { var obj = ScriptableObject.CreateInstance<T>(); temporary.Add(obj); return obj; }
    private static T Find<T>(Func<T, bool> filter) where T : UnityEngine.Object => AssetDatabase.FindAssets("t:" + typeof(T).Name, new[] { "Assets/Data" })
        .Select(g => AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(g))).First(filter);
    private MapEvent_SO Event(params MapEventOptionData[] options)
    { var ev = Temp<MapEvent_SO>(); ev.eventName = "Directed event"; ev.options = options.ToList(); return ev; }
    private MapEventOptionData Option(MapEventOptionBuiltInEffectType type = MapEventOptionBuiltInEffectType.None, int amount = 0)
        => new MapEventOptionData { optionId = Guid.NewGuid().ToString("N"), builtInEffectType = type, effectAmount = amount };
    private void Open() { session = new AgentAdventureSession(new AgentAdventureRequest { seed = 1234 }); }
    private AgentAdventureRequest Command(AgentAdventureAction action)
    {
        var state = session.Observe();
        return new AgentAdventureRequest { sessionId = state.sessionId, revision = state.revision, requestId = Guid.NewGuid().ToString("N"),
            op = action.op, choiceIndex = action.choiceIndex, x = action.x, y = action.y, battleAction = action.battleAction };
    }
    private AgentAdventureObservation Act(string op, int choice = 0) => session.Act(Command(new AgentAdventureAction { op = op, choiceIndex = choice })).state;
    private AgentAdventureObservation Enter(MapEvent_SO ev)
    {
        BoardTileData tile = Map.LegalDestinations().First(); tile.tileType = MapTileType.Event; tile.isResolved = false; tile.isVisited = false; tile.assignedEvent = ev;
        return session.Act(Command(new AgentAdventureAction { op = "run_move", x = tile.coord.x, y = tile.coord.y })).state;
    }
    private BoardTileData Tile => Map.Snapshot.tiles.Single(t => t.coord == Map.Snapshot.runState.playerGridPosition);

    [Test] public void MoneyCostsUsePreSelectionBalanceAndRetriesDoNotPayTwice()
    {
        Open(); Map.Snapshot.runState.money = 101;
        var option = Option(); option.requiredMoney = 5; option.requiredMoneyPercent = 50;
        option.additionalBuiltInEffects.Add(new MapEventBuiltInEffectData { effectType = MapEventOptionBuiltInEffectType.GainMoneyPercentOfPreSelectionMoney, moneyPercent = 20 });
        var state = Enter(Event(option));
        Assert.AreEqual(55, state.mapEvent.options.Single().requiredMoney);
        var command = Command(state.legalActions.Single());
        var response = session.Act(command);
        Assert.AreEqual(66, response.state.money); Assert.AreEqual("event_result", response.state.phase);
        Assert.AreEqual(JsonUtility.ToJson(response), JsonUtility.ToJson(session.Act(command)));
        Assert.IsTrue(Tile.isResolved); Assert.AreEqual(1, Map.Snapshot.runState.settlementStats.eventTilesCompleted);
        Assert.AreEqual("map", Act("run_continue_event").phase);
        Assert.IsFalse(session.Observe().legalActions.Any(a => a.op == "run_interact"));
    }

    [Test] public void InvalidAndUnavailableOptionsCannotSpendOrAdvanceRevision()
    {
        Open(); var paid = Option(); paid.requiredMoney = 1; var free = Option();
        Map.Snapshot.runState.money = -17;
        var state = Enter(Event(paid, free));
        Assert.IsFalse(state.mapEvent.options[0].selectable); Assert.IsTrue(state.mapEvent.options[1].selectable);
        Assert.Throws<ArgumentException>(() => Act("run_choose_event", 0));
        Assert.Throws<ArgumentException>(() => Act("run_choose_event", 900));
        Assert.AreEqual(state.revision, session.Observe().revision); Assert.AreEqual(-17, session.Observe().money);
        Act("run_choose_event", 1); Assert.AreEqual(-17, session.Observe().money);
    }

    [Test] public void DiscardPreviewIsFrozenAndObservationDoesNotChangeEitherRandomStreamOrUiSnapshot()
    {
        MapRunRuntime.RestoreSnapshot(new MapRunSnapshot { sceneName = "UserEventSnapshot", runState = new RunState { money = 777 } });
        Open(); var lootA = Find<LootItem_SO>(l => true); var lootB = Find<LootItem_SO>(l => l != lootA);
        Map.Snapshot.runState.AddLoot(lootA, 3); Map.Snapshot.runState.AddLoot(lootB, 3);
        var option = Option(MapEventOptionBuiltInEffectType.LoseOwnedRandomLoot); option.randomOwnedLootAmountMin = 1; option.randomOwnedLootAmountMax = 2;
        var state = Enter(Event(option));
        var cache = (Dictionary<string, MapEventLootDiscardPreview>)typeof(AgentAdventureSession).GetField("eventDiscardCache", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(session);
        var selected = cache.Values.Single();
        string rng = JsonUtility.ToJson(Random.state), mapRng = JsonUtility.ToJson(Map.PreviewWithRandom(() => Random.state));
        string observation = JsonUtility.ToJson(state);
        for (int i = 0; i < 5; i++) Assert.AreEqual(observation, JsonUtility.ToJson(session.Observe()));
        Assert.AreEqual(rng, JsonUtility.ToJson(Random.state)); Assert.AreEqual(mapRng, JsonUtility.ToJson(Map.PreviewWithRandom(() => Random.state)));
        Act("run_choose_event"); Assert.AreEqual(3 - selected.amount, Map.Snapshot.runState.GetLootAmount(selected.lootItem));
        Assert.AreEqual(777, MapRunRuntime.TryGetSnapshot("UserEventSnapshot", out var untouched) ? untouched.runState.money : -1);
    }

    [Test] public void WeightedFollowUpDefersCompletionUntilTheLastChoiceAndKeepsResultText()
    {
        Open(); var childOption = Option(MapEventOptionBuiltInEffectType.GainMoney, 8);
        var child = Event(childOption); child.eventName = "Child";
        var root = Option(MapEventOptionBuiltInEffectType.GainMoney, 1000);
        root.possibleResults.Add(new MapEventPossibleResultData { weight = 0, primaryEffect = new MapEventBuiltInEffectData { effectType = MapEventOptionBuiltInEffectType.GainMoney, effectAmount = 999 } });
        root.possibleResults.Add(new MapEventPossibleResultData { weight = 1, resultContent = "Rolled child", nextEvent = child });
        Enter(Event(root)); var state = Act("run_choose_event");
        Assert.AreEqual("Rolled child", state.mapEvent.resultContent); Assert.IsFalse(Tile.isResolved); Assert.AreEqual(0, state.money);
        state = Act("run_continue_event"); Assert.AreEqual("Child", state.mapEvent.name); Assert.AreEqual(1, state.mapEvent.depth);
        state = Act("run_choose_event"); Assert.AreEqual(8, state.money); Assert.IsTrue(Tile.isResolved);
        Assert.AreEqual(1, Map.Snapshot.runState.settlementStats.eventTilesCompleted);
        Act("run_continue_event");
    }

    [Test] public void EventActionPointLossQueuesCalamityUntilResultIsAcknowledged()
    {
        Open(); Enter(Event(Option(MapEventOptionBuiltInEffectType.LoseActionPoints, 4)));
        var state = Act("run_choose_event");
        Assert.AreEqual(2, state.cycle); Assert.AreEqual("event_result", state.phase); Assert.IsEmpty(state.calamityChoices);
        Assert.AreEqual("calamity", Act("run_continue_event").phase);
    }

    [Test] public void NonResolvingChoiceAllowsAnotherVisitButCannotRepeatWithinTheResultStep()
    {
        Open(); var option = Option(MapEventOptionBuiltInEffectType.GainMoney, 3); option.resolveEvent = false;
        Enter(Event(option)); Act("run_choose_event");
        Assert.Throws<ArgumentException>(() => Act("run_choose_event"));
        Assert.IsFalse(Tile.isResolved); Act("run_continue_event");
        Assert.AreEqual("event", Act("run_interact").phase); Assert.AreEqual(3, session.Observe().money);
    }

    [Test] public void OldGamblingTable_DeclineThenLeaveCompletesTileAndDoesNotReopenOnReturn()
    {
        Open();
        var root = AssetDatabase.LoadAssetAtPath<MapEvent_SO>("Assets/Data/Events/025 旧赌桌/025 旧赌桌.asset");
        Assert.IsNotNull(root);
        var state = Enter(root);
        BoardTileData origin = Tile;
        int moneyBefore = state.money;
        Act("run_choose_event", state.mapEvent.options.Single(o => o.label == "不上桌").index);
        Assert.IsFalse(origin.isResolved, "The follow-up choice must still be available.");
        state = Act("run_continue_event");
        Assert.AreEqual("025-1", origin.assignedEvent.options.Single(o => o.optionLabel == "不上桌").nextEvent.eventId);
        state = Act("run_choose_event", state.mapEvent.options.Single(o => o.label == "离开").index);
        Assert.IsTrue(origin.isResolved, "Leaving the actual follow-up event must complete the tile.");
        Assert.IsNull(origin.assignedEvent);
        Assert.AreEqual(1, Map.Snapshot.runState.settlementStats.eventTilesCompleted);
        Assert.AreEqual(moneyBefore, state.money);
        Assert.AreEqual("map", Act("run_continue_event").phase);
        Assert.IsFalse(session.Observe().legalActions.Any(a => a.op == "run_interact"));

        BoardTileData away = Map.LegalDestinations().First();
        away.tileType = MapTileType.Empty; away.isResolved = true; away.isVisited = true;
        session.Act(Command(new AgentAdventureAction { op = "run_move", x = away.coord.x, y = away.coord.y }));
        state = session.Act(Command(new AgentAdventureAction { op = "run_move", x = origin.coord.x, y = origin.coord.y })).state;
        Assert.AreEqual("map", state.phase, "Re-entering the completed gambling tile must not reopen its event.");
        Assert.AreEqual(1, Map.Snapshot.runState.settlementStats.eventTilesCompleted);
    }

    [TestCase(false)] [TestCase(true)] public void EventEncounterUsesActualBattleAndGrantsQueuedLootOnlyOnVictory(bool defeat)
    {
        Open(); var state = Map.Snapshot.runState;
        if (defeat) state.partyMembers[0].currentHP = 1;
        var loot = Find<LootItem_SO>(l => true); var option = Option(MapEventOptionBuiltInEffectType.StartEncounter);
        option.encounterSelectionMode = MapEventEncounterSelectionMode.RandomEnemies;
        option.encounterEnemies.Add(Find<EnemyData_SO>(e => e.enemyID == 1));
        option.additionalBuiltInEffects.Add(new MapEventBuiltInEffectData { effectType = MapEventOptionBuiltInEffectType.GainLoot,
            lootRewards = new List<MapEventLootRewardEntry> { new MapEventLootRewardEntry { lootItem = loot, amountMin = 2, amountMax = 2 } } });
        var relic = Find<Relic_SO>(r => r.relicId == "82");
        var blueprint = Find<CraftingBlueprint_SO>(b => true);
        var blueprints = Temp<BlueprintTable_SO>();
        typeof(BlueprintTable_SO).GetField("blueprints", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(blueprints, new List<CraftingBlueprint_SO> { blueprint });
        option.additionalBuiltInEffects.Add(new MapEventBuiltInEffectData { effectType = MapEventOptionBuiltInEffectType.GainRelics,
            relicRewards = new List<MapEventRelicRewardEntry> { new MapEventRelicRewardEntry { relicData = relic } } });
        option.additionalBuiltInEffects.Add(new MapEventBuiltInEffectData { effectType = MapEventOptionBuiltInEffectType.GainBlueprints,
            blueprintTable = blueprints });
        Enter(Event(option)); var observation = Act("run_choose_event");
        Assert.IsTrue(observation.mapEvent.hasPendingBattle); Assert.AreEqual(0, state.GetLootAmount(loot));
        observation = Act("run_continue_event"); Assert.AreEqual("battle", observation.phase);
        var bridge = (AgentAdventureBattleBridge)typeof(AgentAdventureSession).GetField("bridge", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(session);
        EnemyGroup_SO runtimeGroup = bridge.Battle.Encounter.enemyGroup;
        Assert.IsTrue(bridge.Battle.Encounter.isEventEncounter); Assert.AreEqual(2, bridge.Battle.Encounter.pendingVictoryLootRewards.Single().amount);
        for (int i = 0; i < 80 && observation.battle.phase == "targeting"; i++)
        {
            if (!defeat)
            {
                var target = observation.legalActions.FirstOrDefault(a => a.op == "run_battle" && a.battleAction.op == "target");
                if (target != null) observation = session.Act(Command(target)).state;
            }
            observation = session.Act(Command(observation.legalActions.Single(a => a.op == "run_battle" && a.battleAction.op == "commit"))).state;
        }
        Assert.AreEqual(defeat ? "defeat" : "victory", observation.battle.outcome);
        int visible = observation.battle.rewards.loot.Where(l => l.id == AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(loot))).Sum(l => l.amount);
        if (defeat) Assert.AreEqual(0, visible); else Assert.GreaterOrEqual(visible, 2);
        Assert.AreEqual(defeat ? 0 : 1, observation.battle.rewards.relics.Length);
        Assert.AreEqual(defeat ? 0 : 1, observation.battle.rewards.blueprints.Length);
        int settledMaxHp = observation.battle.rewards.party.Single().maxHp;
        var returned = Act("run_return"); Assert.IsTrue(runtimeGroup == null, "Only the owned generated event group is destroyed.");
        Assert.AreEqual(defeat ? "defeat_pending_settlement" : "map", returned.phase);
        if (defeat) Assert.AreEqual(0, state.GetLootAmount(loot)); else Assert.GreaterOrEqual(state.GetLootAmount(loot), 2);
        Assert.AreEqual(defeat ? 0 : 1, state.settlementStats.eventBattlesCompleted);
        Assert.AreEqual(defeat ? 0 : 1, state.GetRelicAmount(relic));
        Assert.AreEqual(defeat ? 0 : 1, state.GetBlueprintAmount(blueprint));
        Assert.AreEqual(settledMaxHp + (defeat ? 0 : 3), state.partyMembers.Single().maxHP);
    }

    [Test] public void NestedLimitIsVisibleAndDoesNotContinueUnbounded()
    {
        Open(); var option = Option(); var ev = Event(option); option.nextEvent = ev;
        Enter(ev);
        for (int i = 0; i < MapEventOptionService.MaxNestedEventDepth; i++) { Act("run_choose_event"); Act("run_continue_event"); }
        var state = Act("run_choose_event"); Assert.IsEmpty(state.legalActions); Assert.IsNotEmpty(state.mapEvent.blockedReason);
        Assert.Throws<ArgumentException>(() => Act("run_continue_event"));
    }

    [Test] public void BuiltInFailureRefundsSelectionCostWithoutCompletingTheEvent()
    {
        var ev = Event(); var tile = new BoardTileData { tileType = MapTileType.Event, assignedEvent = ev };
        var option = Option(MapEventOptionBuiltInEffectType.StartEncounter); option.requiredMoney = 5;
        var state = new RunState { money = 8 };
        Assert.IsFalse(MapEventOptionService.TryExecute(new MapEventBuiltInEffectExecutionRequest { runState = state, currentEventTile = tile, optionData = option }, ev, null, out var result));
        Assert.AreEqual(8, state.money); Assert.IsFalse(result.success); Assert.IsFalse(tile.isResolved);
    }

    [Test] public void ClosingBeforeEventBattleDestroysGeneratedGroupButPreservesAssetGroups()
    {
        Open(); var option = Option(MapEventOptionBuiltInEffectType.StartEncounter);
        option.encounterSelectionMode = MapEventEncounterSelectionMode.RandomEnemies;
        option.encounterEnemies.Add(Find<EnemyData_SO>(e => e.enemyID == 1));
        Enter(Event(option)); Act("run_choose_event");
        var field = typeof(AgentAdventureSession).GetField("pendingEventEncounter", BindingFlags.Instance | BindingFlags.NonPublic);
        var generated = ((BattleEncounterData)field.GetValue(session)).enemyGroup;
        session.Dispose(); session = null; Assert.IsTrue(generated == null);
        Open(); var asset = Find<EnemyGroup_SO>(g => g.IsValid);
        option = Option(MapEventOptionBuiltInEffectType.StartEncounter); option.encounterEnemyGroups.Add(asset);
        Enter(Event(option)); Act("run_choose_event"); session.Dispose(); session = null;
        Assert.IsTrue(asset != null); Assert.IsTrue(asset.IsValid);
    }

    [Test] public void EventRelicRollUsesTheScopedRandomStreamInsteadOfTheWallClock()
    {
        var table = Temp<RelicTable_SO>();
        typeof(RelicTable_SO).GetField("relics", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(table,
            AssetDatabase.FindAssets("t:Relic_SO", new[] { "Assets/Data" }).Take(12).Select(g => AssetDatabase.LoadAssetAtPath<Relic_SO>(AssetDatabase.GUIDToAssetPath(g))).ToList());
        var effect = new MapEventBuiltInEffectData { effectType = MapEventOptionBuiltInEffectType.GainRelics, relicTable = table };
        Random.InitState(7856); var replayStart = Random.state;
        var first = new RunState(); Assert.IsTrue(MapEventRewardService.TryExecuteRelicReward(first, effect, out _));
        var end = JsonUtility.ToJson(Random.state);
        Random.InitState(555); Random.state = replayStart;
        var second = new RunState(); Assert.IsTrue(MapEventRewardService.TryExecuteRelicReward(second, effect, out _));
        Assert.AreSame(first.relicInventory.Single().relicData, second.relicInventory.Single().relicData);
        Assert.AreEqual(end, JsonUtility.ToJson(Random.state));
    }

    [Test] public void UnaffordableMoneyEffectCannotPartiallyDamageTheParty()
    {
        Open(); var option = Option(MapEventOptionBuiltInEffectType.AdjustAllPartyMembersHpByMaxHpPercent, -20);
        option.additionalBuiltInEffects.Add(new MapEventBuiltInEffectData { effectType = MapEventOptionBuiltInEffectType.LoseMoney, effectAmount = 20 });
        var ev = Event(option); var state = Enter(ev); int hp = state.party.Single().hp;
        Assert.IsFalse(state.mapEvent.options.Single().selectable); Assert.IsEmpty(state.legalActions);
        Assert.That(state.mapEvent.options.Single().unavailableReason, Does.Contain("20"));
        Assert.Throws<ArgumentException>(() => Act("run_choose_event"));
        Assert.IsFalse(MapEventOptionService.TryExecute(new MapEventBuiltInEffectExecutionRequest { runState = Map.Snapshot.runState,
            currentEventTile = Tile, optionData = option }, ev, null, out var result));
        Assert.That(result.message, Does.Contain("20")); Assert.AreEqual(hp, Map.Snapshot.runState.partyMembers.Single().currentHP);
        Assert.AreEqual(0, Map.Snapshot.runState.money); Assert.IsFalse(Tile.isResolved);
    }

    [Test] public void MoneyPreflightUsesTheSameOrderedIncomeAndSelectionFeeRules()
    {
        var option = Option(MapEventOptionBuiltInEffectType.GainMoney, 10); option.requiredMoney = 5;
        option.additionalBuiltInEffects.Add(new MapEventBuiltInEffectData { effectType = MapEventOptionBuiltInEffectType.LoseMoney, effectAmount = 20 });
        var state = new RunState { money = 15 }; var ev = Event(option); var tile = new BoardTileData { tileType = MapTileType.Event };
        Assert.IsTrue(MapEventOptionService.BuildPreview(state, option, null).isSelectable);
        Assert.IsTrue(MapEventOptionService.TryExecute(new MapEventBuiltInEffectExecutionRequest { runState = state, currentEventTile = tile, optionData = option }, ev, null, out _));
        Assert.AreEqual(0, state.money); Assert.IsTrue(tile.isResolved);
    }

    [Test] public void WeightedMoneyPreflightChecksEveryBranchWithoutDrawingOrAdvancingRandomness()
    {
        var option = Option(); option.possibleResults.Add(new MapEventPossibleResultData { weight = 1,
            primaryEffect = new MapEventBuiltInEffectData { effectType = MapEventOptionBuiltInEffectType.LoseMoney, effectAmount = 20 } });
        option.possibleResults.Add(new MapEventPossibleResultData { weight = 1,
            primaryEffect = new MapEventBuiltInEffectData { effectType = MapEventOptionBuiltInEffectType.GainRandomMoney, randomMoneyMin = 20, randomMoneyMax = 50 } });
        var state = new RunState { money = 19 }; string rng = JsonUtility.ToJson(Random.state);
        Assert.IsFalse(MapEventOptionService.BuildPreview(state, option, null).isSelectable);
        state.money = 20; Assert.IsTrue(MapEventOptionService.BuildPreview(state, option, null).isSelectable);
        Assert.AreEqual(rng, JsonUtility.ToJson(Random.state)); Assert.AreEqual(20, state.money);
    }

    [Test] public void BuiltInFailurePreservesTheActualEffectErrorMessage()
    {
        var option = Option(MapEventOptionBuiltInEffectType.LoseMoney, 20);
        Assert.IsFalse(MapEventBuiltInEffectExecutorService.TryExecute(new MapEventBuiltInEffectExecutionRequest { runState = new RunState(), optionData = option }, out var result));
        Assert.That(result.message, Does.Contain("20"));
    }

    [Test] public void RandomIncomeCannotHideAnUnaffordableHighPercentageLoss()
    {
        var option = Option(MapEventOptionBuiltInEffectType.GainRandomMoney); option.randomMoneyMin = 0; option.randomMoneyMax = 10;
        option.additionalBuiltInEffects.Add(new MapEventBuiltInEffectData { effectType = MapEventOptionBuiltInEffectType.LoseMoneyPercent, moneyPercent = 150 });
        string rng = JsonUtility.ToJson(Random.state);
        Assert.IsFalse(MapEventOptionService.BuildPreview(new RunState(), option, null).isSelectable);
        Assert.AreEqual(rng, JsonUtility.ToJson(Random.state));
    }
}
