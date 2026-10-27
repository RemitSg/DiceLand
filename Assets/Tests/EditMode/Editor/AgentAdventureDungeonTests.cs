using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

public sealed class AgentAdventureDungeonTests
{
    private AgentAdventureSession session;
    private readonly List<UnityEngine.Object> temporary = new List<UnityEngine.Object>();
    private Random.State outside;
    private MapRunSnapshot global;
    private AgentAdventureMapRuntime Map => (AgentAdventureMapRuntime)typeof(AgentAdventureSession).GetField("map", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(session);
    [SetUp] public void SetUp()
    {
        outside = Random.state;
        global = (MapRunSnapshot)typeof(MapRunRuntime).GetField("currentSnapshot", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
    }
    [TearDown] public void TearDown()
    {
        session?.Dispose(); session = null;
        foreach (var value in temporary) if (value != null) UnityEngine.Object.DestroyImmediate(value);
        temporary.Clear(); Random.state = outside; MapRunRuntime.RestoreSnapshot(global);
    }
    private T Temp<T>() where T : ScriptableObject { var value = ScriptableObject.CreateInstance<T>(); temporary.Add(value); return value; }
    private static T Find<T>(Func<T, bool> test) where T : UnityEngine.Object => AssetDatabase.FindAssets("t:" + typeof(T).Name, new[] { "Assets/Data" })
        .Select(g => AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(g))).First(test);
    private void Open() { session = new AgentAdventureSession(new AgentAdventureRequest { seed = 1234 }); }
    private AgentAdventureRequest Command(AgentAdventureAction action)
    {
        var state = session.Observe();
        return new AgentAdventureRequest { sessionId = state.sessionId, revision = state.revision, requestId = Guid.NewGuid().ToString("N"),
            op = action.op, choiceIndex = action.choiceIndex, x = action.x, y = action.y, battleAction = action.battleAction };
    }
    private AgentAdventureObservation Act(string op, int choice = 0) => session.Act(Command(new AgentAdventureAction { op = op, choiceIndex = choice })).state;
    private AgentAdventureObservation Move(int x)
    {
        var state = session.Observe(); return session.Act(Command(state.legalActions.Single(a => a.op == "run_move" && a.x == x))).state;
    }
    private SpecialDungeon_SO Dungeon(params SpecialDungeonTileEntry[] entries)
    { var dungeon = Temp<SpecialDungeon_SO>(); dungeon.dungeonName = "Directed dungeon"; dungeon.entries = entries.ToList(); dungeon.settlementScoreValue = 321; return dungeon; }
    private MapEvent_SO EntryEvent(SpecialDungeon_SO dungeon)
    {
        var ev = Temp<MapEvent_SO>(); ev.options.Add(new MapEventOptionData { builtInEffectType = MapEventOptionBuiltInEffectType.StartSpecialDungeon, specialDungeon = dungeon }); return ev;
    }
    private AgentAdventureObservation Enter(SpecialDungeon_SO dungeon)
    {
        BoardTileData tile = Map.LegalDestinations().First(); tile.tileType = MapTileType.Event; tile.isResolved = false; tile.isVisited = false; tile.assignedEvent = EntryEvent(dungeon);
        session.Act(Command(new AgentAdventureAction { op = "run_move", x = tile.coord.x, y = tile.coord.y }));
        Act("run_choose_event"); return Act("run_continue_event");
    }
    private EnemyGroup_SO SlimeGroup()
    { var group = Temp<EnemyGroup_SO>(); group.enemies.Add(Find<EnemyData_SO>(e => e.enemyID == 1)); return group; }

    [Test] public void EntryLoadsTheSharedLinearBoardAndEveryDungeonMoveCostsZero()
    {
        Open(); var ev = Temp<MapEvent_SO>(); ev.options.Add(new MapEventOptionData());
        var dungeon = Dungeon(new SpecialDungeonTileEntry { tileType = MapTileType.Event, eventData = ev });
        var state = Enter(dungeon);
        Assert.AreEqual("map", state.phase); Assert.NotNull(state.dungeon); Assert.AreEqual(4, state.boardSize); Assert.AreEqual(4, state.tiles.Length);
        Assert.AreEqual(1, state.x); Assert.AreEqual(1, state.dungeon.entriesTotal);
        Assert.AreEqual(0, state.dungeon.leftExitX); Assert.AreEqual(3, state.dungeon.rightExitX);
        Assert.IsTrue(state.tiles.All(t => t.moveCost == 0));
        int points = state.actionPoints; state = Move(2); Assert.AreEqual("event", state.phase); Assert.AreEqual(points, state.actionPoints);
        Act("run_choose_event"); state = Act("run_continue_event"); Assert.AreEqual(1, state.dungeon.entriesResolved);
    }

    [Test] public void LeftExitRestoresTheOuterBoardPositionAndActionPointsWithoutCompletionScore()
    {
        Open(); var original = Map.Snapshot.tiles.Select(t => t.coord).ToArray();
        var state = Enter(Dungeon()); int points = state.actionPoints; int x = state.dungeon.returnX, y = state.dungeon.returnY;
        state = Move(0);
        Assert.IsNull(state.dungeon); Assert.AreEqual(9, state.boardSize); Assert.AreEqual(9, Map.Snapshot.runState.boardSize);
        Assert.AreEqual(x, state.x); Assert.AreEqual(y, state.y); Assert.AreEqual(points, state.actionPoints);
        Assert.AreEqual(0, state.progress.specialDungeonCleared); Assert.AreEqual(0, state.progress.specialDungeonScoreTotal);
        CollectionAssert.AreEquivalent(original, Map.Snapshot.tiles.Select(t => t.coord));
        Assert.IsTrue(state.tiles.Single(t => t.x == x && t.y == y).resolved);
        Assert.IsFalse(state.legalActions.Any(a => a.op == "run_interact"));
        Assert.IsTrue(Map.LegalDestinations().All(t => Map.Snapshot.tiles.Contains(t)), "Return must retain live board references.");
    }

    [Test] public void RightExitCarriesRealTreasureAndScoresOnceEvenWhenTheMoveIsRetried()
    {
        Open(); var dungeon = Dungeon(new SpecialDungeonTileEntry { tileType = MapTileType.Treasure,
            treasureOptions = new List<SpecialDungeonTreasureOptionEntry> { new SpecialDungeonTreasureOptionEntry { moneyAmount = 47 } } });
        var state = Enter(dungeon); int points = state.actionPoints;
        state = Move(2); Assert.AreEqual("treasure", state.phase); Assert.AreEqual(47, state.treasure.options.Single().money);
        Act("run_choose_treasure"); Act("run_continue_treasure");
        var command = Command(session.Observe().legalActions.Single(a => a.op == "run_move" && a.x == 3));
        var first = session.Act(command); state = first.state;
        Assert.IsNull(state.dungeon); Assert.AreEqual(47, state.money); Assert.AreEqual(points, state.actionPoints);
        Assert.AreEqual(1, state.progress.specialDungeonCleared); Assert.AreEqual(321, state.progress.specialDungeonScoreTotal);
        Assert.AreEqual(JsonUtility.ToJson(first), JsonUtility.ToJson(session.Act(command)));
        Assert.AreEqual(1, session.Observe().progress.specialDungeonCleared);
    }

    [Test] public void PendingEntryCalamityReturnsExactlyOnceAfterLeavingTheDungeon()
    {
        Open(); Map.Points.RestoreState(6, 0, 2);
        var state = Enter(Dungeon()); Assert.AreEqual(2, state.cycle); Assert.AreEqual("map", state.phase);
        Assert.IsEmpty(state.calamityChoices); Assert.IsEmpty(Map.PendingCalamities);
        Assert.AreEqual(1, Map.Snapshot.specialDungeonContext.deferredCalamityTiers.Count);
        Assert.IsEmpty(Map.Snapshot.specialDungeonContext.mainMapSnapshot.pendingCalamityTiers);
        state = Move(0); Assert.AreEqual("calamity", state.phase);
        state = Act("run_choose_calamity"); Assert.AreEqual("map", state.phase); Assert.IsEmpty(Map.PendingCalamities);
        Assert.AreEqual(1, state.activeCalamities.Length);
    }

    [Test] public void FixedDungeonMerchantCannotRefreshAndPurchasedItemsCarryBack()
    {
        Open(); Map.Snapshot.runState.money = 10; var loot = Find<LootItem_SO>(l => true);
        Enter(Dungeon(new SpecialDungeonTileEntry { tileType = MapTileType.Merchant,
            merchantOffers = new List<SpecialDungeonMerchantOfferEntry> { new SpecialDungeonMerchantOfferEntry { itemType = MerchantItemType.Loot, lootItem = loot, price = 7 } } }));
        var state = Move(2); Assert.AreEqual("merchant", state.phase); Assert.AreEqual(7, state.merchantOffers.Single().price);
        Assert.IsFalse(state.legalActions.Any(a => a.op == "run_refresh_shop"));
        Act("run_buy"); Act("run_leave_location"); state = Move(3);
        Assert.AreEqual(3, state.money); Assert.AreEqual(1, Map.Snapshot.runState.GetLootAmount(loot));
    }

    [Test] public void DungeonBattleUsesSharedCombatAndSettledPartySurvivesBoardRestoration()
    {
        Open(); var state = Enter(Dungeon(new SpecialDungeonTileEntry { tileType = MapTileType.Battle, enemyGroup = SlimeGroup(), previewDangerRating = 97 }));
        state = Move(2); Assert.AreEqual("battle", state.phase); Assert.AreEqual(8, state.tiles.Single(t => t.x == 2).danger, "Configured danger is clamped by the shared rating service.");
        var bridge = (AgentAdventureBattleBridge)typeof(AgentAdventureSession).GetField("bridge", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(session);
        Assert.IsTrue(bridge.Battle.Encounter.isSpecialDungeonEncounter);
        for (int i = 0; i < 40 && state.battle.phase == "targeting"; i++)
        {
            var target = state.legalActions.FirstOrDefault(a => a.op == "run_battle" && a.battleAction.op == "target");
            if (target != null) state = session.Act(Command(target)).state;
            state = session.Act(Command(state.legalActions.Single(a => a.op == "run_battle" && a.battleAction.op == "commit"))).state;
        }
        Assert.AreEqual("victory", state.battle.outcome); var carried = state.battle.rewards.party.Single();
        state = Act("run_return"); Assert.NotNull(state.dungeon); Assert.IsTrue(state.tiles.Single(t => t.x == 2).resolved);
        state = Move(3); Assert.AreEqual(carried.hp, state.party.Single().hp); Assert.AreEqual(carried.level, state.party.Single().level);
        Assert.AreEqual(1, state.progress.specialDungeonCleared); Assert.IsNull(BattleEncounterRuntime.ActiveEncounter);
    }

    [Test] public void ASecondDungeonEntryInsideTheDungeonDoesNotReplaceTheOuterContext()
    {
        Open(); var inner = Dungeon(); var outer = Dungeon(new SpecialDungeonTileEntry { tileType = MapTileType.Event, eventData = EntryEvent(inner) });
        Enter(outer); var saved = Map.Snapshot.specialDungeonContext;
        Move(2); Act("run_choose_event"); var state = Act("run_continue_event");
        Assert.AreSame(saved, Map.Snapshot.specialDungeonContext); Assert.AreSame(outer, saved.activeDungeon);
        Assert.AreEqual("map", state.phase); Assert.That(state.message, Does.Contain("无法重复进入"));
        state = Move(3); Assert.AreEqual(321, state.progress.specialDungeonScoreTotal);
    }

    [Test] public void ObservationAndEntryDoNotReplaceTheUiSnapshotOrTheExternalRandomStream()
    {
        MapRunRuntime.RestoreSnapshot(new MapRunSnapshot { sceneName = "UserDungeonSnapshot", runState = new RunState { money = 800 } });
        Open(); var state = Enter(Dungeon()); string observed = JsonUtility.ToJson(state);
        string rng = JsonUtility.ToJson(Random.state), mapRng = JsonUtility.ToJson(Map.PreviewWithRandom(() => Random.state));
        for (int i = 0; i < 4; i++) Assert.AreEqual(observed, JsonUtility.ToJson(session.Observe()));
        Assert.AreEqual(rng, JsonUtility.ToJson(Random.state)); Assert.AreEqual(mapRng, JsonUtility.ToJson(Map.PreviewWithRandom(() => Random.state)));
        Assert.IsTrue(MapRunRuntime.TryGetSnapshot("UserDungeonSnapshot", out var untouched)); Assert.AreEqual(800, untouched.runState.money);
    }

    [Test] public void InvalidDungeonMoveAndStaleExitDoNotChangeBoardOrAp()
    {
        Open(); var state = Enter(Dungeon());
        Assert.Throws<ArgumentException>(() => session.Act(Command(new AgentAdventureAction { op = "run_move", x = 99, y = 99 })));
        var stale = Command(state.legalActions.First(a => a.op == "run_move")); stale.revision--;
        Assert.Throws<ArgumentException>(() => session.Act(stale)); Assert.AreEqual(JsonUtility.ToJson(state), JsonUtility.ToJson(session.Observe()));
    }

    [Test] public void DungeonActivityFlagSurvivesWireSerializationAtEntryAndExit()
    {
        Open();
        var outside = JsonUtility.FromJson<AgentAdventureObservation>(JsonUtility.ToJson(session.Observe()));
        Assert.IsTrue(outside.dungeon == null || !outside.dungeon.active);
        Enter(Dungeon());
        var inside = JsonUtility.FromJson<AgentAdventureObservation>(JsonUtility.ToJson(session.Observe()));
        Assert.IsTrue(inside.dungeon.active);
        Move(0);
        var returned = JsonUtility.FromJson<AgentAdventureObservation>(JsonUtility.ToJson(session.Observe()));
        Assert.IsTrue(returned.dungeon == null || !returned.dungeon.active);
    }
}
