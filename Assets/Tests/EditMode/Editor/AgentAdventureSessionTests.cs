using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Random = UnityEngine.Random;

public sealed class AgentAdventureSessionTests
{
    private AgentAdventureSession session;
    private readonly List<UnityEngine.Object> temporary = new List<UnityEngine.Object>();
    private Random.State outside;
    [SetUp] public void SetUp() { outside = Random.state; }
    [TearDown] public void TearDown()
    {
        session?.Dispose(); session = null;
        foreach (var value in temporary) UnityEngine.Object.DestroyImmediate(value);
        temporary.Clear(); Random.state = outside;
    }
    private void Open() { session = new AgentAdventureSession(new AgentAdventureRequest { op = "create_run", seed = 1234 }); }
    private AgentAdventureMapRuntime Map => (AgentAdventureMapRuntime)typeof(AgentAdventureSession).GetField("map", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(session);
    private AgentAdventureRequest Command(AgentAdventureAction action)
    {
        var state = session.Observe();
        return new AgentAdventureRequest { op = action.op, x = action.x, y = action.y, choiceIndex = action.choiceIndex, battleAction = action.battleAction,
            sessionId = state.sessionId, revision = state.revision, requestId = Guid.NewGuid().ToString("N") };
    }
    private AgentAdventureObservation Act(AgentAdventureAction action) { return session.Act(Command(action)).state; }
    private Calamity_SO Calamity(params CalamityBuiltInEffectEntry[] effects)
    {
        var value = ScriptableObject.CreateInstance<Calamity_SO>(); temporary.Add(value);
        value.builtInEffects = effects.ToList(); return value;
    }

    [Test] public void CreateRun_UsesNormalContentAndShowsRealMovesAndConfigurationGaps()
    {
        Open(); var state = session.Observe();
        Assert.AreEqual("map", state.phase); Assert.AreEqual("Medium", state.difficulty);
        Assert.AreEqual(81, state.tiles.Length); Assert.AreEqual(16, state.party.Single().hp);
        Assert.IsTrue(state.legalActions.Any(a => a.op == "run_move")); Assert.Greater(state.configurationIssues.Length, 0);
        foreach (var action in state.legalActions.Where(a => a.op == "run_move"))
            Assert.AreEqual(1, Math.Abs(action.x - state.x) + Math.Abs(action.y - state.y));
    }

    [Test] public void RetriedMove_DoesNotSpendTwiceOrOpenAnotherBattle()
    {
        Open(); var command = Command(session.Observe().legalActions.First(a => a.op == "run_move" && a.x == 1 && a.y == 1));
        var first = session.Act(command); Assert.AreEqual("battle", first.state.phase);
        Assert.AreEqual(JsonUtility.ToJson(first), JsonUtility.ToJson(session.Act(command)));
        Assert.AreEqual(4, first.state.actionPoints); Assert.AreEqual(1, first.state.revision);
        Assert.AreEqual(1, Map.Snapshot.runState.settlementStats.unexploredTilesVisited);
        var stale = Command(new AgentAdventureAction { op = "run_return" }); stale.revision = 0;
        Assert.Throws<ArgumentException>(() => session.Act(stale));
        Assert.AreEqual(1, session.Observe().revision);
    }

    [Test] public void RealMapBattle_ReturnsCarriedStateThroughTheSameRewardWriter()
    {
        Open(); var state = Act(session.Observe().legalActions.First(a => a.op == "run_move" && a.x == 1 && a.y == 1));
        for (int round = 0; round < 100 && state.battle.phase == "targeting"; round++)
        {
            foreach (var actor in state.battle.actors.Where(a => a.side == "ally" && !a.dead))
                foreach (var slot in actor.slots)
                {
                    var target = state.legalActions.FirstOrDefault(a => a.op == "run_battle" && a.battleAction.op == "target"
                        && a.battleAction.actorId == actor.id && a.battleAction.buttonIndex == slot.index);
                    if (target != null) state = Act(target);
                }
            state = Act(state.legalActions.Single(a => a.op == "run_battle" && a.battleAction.op == "commit"));
        }
        var returned = state.legalActions.Single(a => a.op == "run_return");
        int settledHp = state.battle.rewards.party.Single().hp; // Includes the formal level-up healing.
        string outcome = state.battle.outcome;
        state = Act(returned);
        Assert.AreEqual(settledHp, state.party[0].hp);
        Assert.AreEqual("0", state.party[0].id);
        Assert.AreEqual(outcome == "victory" ? "map" : "defeat_pending_settlement", state.phase);
        Assert.IsNull(BattleEncounterRuntime.ActiveEncounter);
        if (outcome == "victory") Assert.Greater(state.legalActions.Length, 0);
    }

    [Test] public void ExhaustedCycle_RequiresAnActualCalamityChoiceAndRetriesDoNotApplyTwice()
    {
        Open(); var destination = Map.LegalDestinations().First();
        destination.tileType = MapTileType.Key; destination.isVisited = false; destination.isResolved = false;
        Map.Points.RestoreState(6, 0, 1);
        int keysWhenOverflowSpent = -1;
        Map.Points.ActionPointsChanged += (current, max) => { if (current == max - 1) keysWhenOverflowSpent = Map.Snapshot.runState.currentKeys; };
        var state = Act(new AgentAdventureAction { op = "run_move", x = destination.coord.x, y = destination.coord.y });
        Assert.AreEqual(0, keysWhenOverflowSpent, "Arrival must occur after the entire overflow cost is paid.");
        Assert.AreEqual("calamity", state.phase); Assert.Greater(state.calamityChoices.Length, 0);
        Assert.AreEqual(1, state.keys); Assert.AreEqual(2, state.cycle);
        var command = Command(state.legalActions.First(a => a.op == "run_choose_calamity"));
        var selected = session.Act(command);
        Assert.AreEqual(1, selected.state.activeCalamities.Length);
        Assert.AreEqual("map", selected.state.phase);
        Assert.AreEqual(JsonUtility.ToJson(selected), JsonUtility.ToJson(session.Act(command)));
    }

    [Test] public void CalamityMapEffects_UseSharedTeleportRelicLossAndRestConversion()
    {
        Open(); BoardTileData only = Map.Snapshot.tiles.First(t => t.dangerBand == MapDangerBand.Outer && !t.isSpawnTile && t.tileType != MapTileType.Boss);
        foreach (BoardTileData tile in Map.Snapshot.tiles) tile.isVisited = true;
        only.isVisited = false; only.tileType = MapTileType.Rest;
        var relic = UnityEditor.AssetDatabase.FindAssets("t:Relic_SO").Select(g => UnityEditor.AssetDatabase.LoadAssetAtPath<Relic_SO>(UnityEditor.AssetDatabase.GUIDToAssetPath(g))).First(r => r.relicId == "15");
        Map.Snapshot.runState.AddRelic(relic, 2);
        var calamity = Calamity(new CalamityBuiltInEffectEntry { effectType = CalamityBuiltInEffectType.TeleportToRandomOuterUnvisitedTile },
            new CalamityBuiltInEffectEntry { effectType = CalamityBuiltInEffectType.LoseRandomRelics, effectAmount = 2 },
            new CalamityBuiltInEffectEntry { effectType = CalamityBuiltInEffectType.ConvertUnvisitedRestTilesToBattles, effectAmount = 1 });
        var feedback = Map.WithRandom(() => CalamitySelectionService.ApplyBuiltInMapEffects(calamity, Map.Snapshot.runState, Map.Board, Map.Configuration.enemies));
        Assert.AreEqual(only.coord, Map.Snapshot.runState.playerGridPosition);
        Assert.AreEqual(0, Map.Snapshot.runState.GetRelicAmount(relic));
        Assert.AreEqual(MapTileType.Battle, only.tileType); Assert.IsNotNull(only.assignedEnemyGroup);
        Assert.IsFalse(only.isResolved); Assert.AreEqual(3, feedback.Count);
    }

    [Test] public void VisitedEmptyAmbush_UsesActualCalamityAndEnemyRoster()
    {
        Open(); var tile = Map.Snapshot.tiles.Single(t => t.isSpawnTile);
        var calamity = Calamity(new CalamityBuiltInEffectEntry { effectType = CalamityBuiltInEffectType.VisitedEmptyTileAmbushChance, effectAmount = 100 });
        Map.Snapshot.runState.AddCalamity(calamity, 1);
        Assert.IsTrue(Map.WithRandom(() => MapMovementService.TryTriggerVisitedEmptyTileAmbush(Map.Snapshot.runState, Map.Board, Map.Configuration.enemies, tile)));
        Assert.AreEqual(MapTileType.Battle, tile.tileType); Assert.IsFalse(tile.isResolved); Assert.IsTrue(tile.assignedEnemyGroup.IsValid);
    }

    [Test] public void MapLocation_IsNotReplacedWithPlaceholderRewards()
    {
        Open(); var action = session.Observe().legalActions.First(a => a.op == "run_move" && a.x == 2 && a.y == 2);
        var state = Act(action);
        Assert.AreEqual("event", state.phase);
        Assert.IsEmpty(state.legalActions);
        Assert.AreEqual(0, state.money); Assert.AreEqual(0, state.party[0].experience);
        Assert.IsFalse(state.tiles.Single(t => t.x == 2 && t.y == 2).resolved);
    }
}
