using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

public sealed class AgentAdventureRestTests
{
    private AgentAdventureSession session;
    private Random.State outside;
    private MapRunSnapshot originalGlobal;
    private readonly List<ScriptableObject> temporary = new List<ScriptableObject>();
    private AgentAdventureMapRuntime Map => (AgentAdventureMapRuntime)typeof(AgentAdventureSession).GetField("map", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(session);
    private static FieldInfo Global => typeof(MapRunRuntime).GetField("currentSnapshot", BindingFlags.Static | BindingFlags.NonPublic);
    [SetUp] public void SetUp()
    {
        outside = Random.state; originalGlobal = (MapRunSnapshot)Global.GetValue(null);
        session = new AgentAdventureSession(new AgentAdventureRequest { seed = 1234 });
    }
    [TearDown] public void TearDown()
    {
        session?.Dispose(); foreach (var o in temporary) UnityEngine.Object.DestroyImmediate(o); temporary.Clear();
        Global.SetValue(null, originalGlobal); Random.state = outside;
    }
    private AgentAdventureRequest Command(string op, int choice = 0, string asset = null)
    {
        var state = session.Observe(); return new AgentAdventureRequest { op = op, choiceIndex = choice, assetId = asset,
            sessionId = state.sessionId, revision = state.revision, requestId = Guid.NewGuid().ToString("N") };
    }
    private AgentAdventureObservation Act(string op, int choice = 0, string asset = null) => session.Act(Command(op, choice, asset)).state;
    private AgentAdventureObservation Enter(int points = 6)
    {
        // Directed resource/cycle tests. These mutations are never exposed by the public game API.
        var tile = Map.LegalDestinations().First(); tile.tileType = MapTileType.Rest; tile.isVisited = false; tile.isResolved = false;
        Map.Points.RestoreState(6, 0, points);
        var command = Command("run_move"); command.x = tile.coord.x; command.y = tile.coord.y;
        return session.Act(command).state;
    }

    [Test] public void Healing_RespectsCalamityConsumesVisitAndRetriesDoNotHealAgain()
    {
        Map.Snapshot.runState.partyMembers[0].currentHP = 1;
        var calamity = ScriptableObject.CreateInstance<Calamity_SO>(); temporary.Add(calamity);
        calamity.builtInEffects.Add(new CalamityBuiltInEffectEntry { effectType = CalamityBuiltInEffectType.RestHealPercentOverride, effectAmount = 50 });
        Map.Snapshot.runState.AddCalamity(calamity, 1);
        var state = Enter(); Assert.AreEqual("rest", state.phase); Assert.AreEqual(50, state.rest.healPercent);
        var request = Command("run_rest_heal"); var healed = session.Act(request);
        Assert.AreEqual(9, healed.state.party[0].hp); Assert.AreEqual("map", healed.state.phase);
        Assert.IsTrue(healed.state.tiles.Single(t => t.x == healed.state.x && t.y == healed.state.y).resolved);
        Assert.AreEqual(JsonUtility.ToJson(healed), JsonUtility.ToJson(session.Act(request)));
        Assert.Throws<ArgumentException>(() => Act("run_rest_heal"));
        Assert.IsFalse(healed.state.legalActions.Any(a => a.op == "run_interact"));
    }

    [Test] public void ActionPointRestore_AfterExactExhaustionCancelsOneDeferredCalamity()
    {
        var state = Enter(2); Assert.AreEqual("rest", state.phase); Assert.AreEqual(1, Map.PendingCalamities.Count);
        Assert.IsTrue(Map.CycleFlags.suppressPendingCalamityOnNextRestActionPointRestore);
        state = Act("run_rest_restore_ap");
        Assert.AreEqual("map", state.phase); Assert.AreEqual(state.maxActionPoints, state.actionPoints);
        Assert.AreEqual(state.actionPoints, Map.Points.CurrentPoints); Assert.IsEmpty(Map.PendingCalamities);
        Assert.IsFalse(Map.CycleFlags.suppressPendingCalamityOnNextRestActionPointRestore);
    }

    [Test] public void ActionPointRestore_AfterOverflowKeepsTheCalamityChoice()
    {
        var state = Enter(1); Assert.IsFalse(Map.CycleFlags.suppressPendingCalamityOnNextRestActionPointRestore);
        state = Act("run_rest_restore_ap");
        Assert.AreEqual("calamity", state.phase); Assert.IsNotEmpty(state.calamityChoices);
        Assert.AreEqual(state.maxActionPoints, state.actionPoints); Assert.IsEmpty(Map.PendingCalamities);
    }

    [Test] public void Recruit_SpendsConfiguredCostAndCarriesTheNewPartyIntoCombat()
    {
        Map.Snapshot.runState.money = 1000; var state = Enter();
        var action = state.legalActions.First(a => a.op == "run_recruit");
        var candidate = state.rest.candidates.Single(c => c.index == action.choiceIndex);
        int cost = state.rest.recruitCost; state = Act("run_recruit", action.choiceIndex);
        Assert.AreEqual(1000 - cost, state.money); Assert.AreEqual(2, state.party.Length);
        Assert.AreEqual(1, Map.Snapshot.runState.recruitedMemberCount);
        Assert.IsTrue(state.party.Any(p => p.character == candidate.id && p.hp == candidate.hp));
        var hostile = Map.Snapshot.tiles.First(t => t.IsHostileTile && t.tileType != MapTileType.Boss && t.assignedEnemyGroup != null && t.assignedEnemyGroup.IsValid);
        using (var bridge = new AgentAdventureBattleBridge(Map.Snapshot, hostile, hostile.assignedEnemyGroup,
            Map.Configuration.starting.difficulty, Map.Configuration.money, Map.Configuration.eliteRelics, Map.Configuration.levels))
            Assert.AreEqual(2, bridge.Battle.Observe().actors.Count(a => a.side == "ally"));
    }

    [Test] public void RecruitCandidates_AreStableAndUnaffordableOrLimitReachedChoicesAreRejected()
    {
        var state = Enter(); string[] candidates = state.rest.candidates.Select(c => c.id).ToArray();
        Assert.IsNotEmpty(candidates); Assert.IsFalse(state.legalActions.Any(a => a.op == "run_recruit"));
        Assert.Throws<ArgumentException>(() => Act("run_recruit"));
        Map.Snapshot.runState.money = 1000; Map.Snapshot.runState.recruitedMemberCount = state.rest.maxRecruitCount;
        state = session.Observe(); CollectionAssert.AreEqual(candidates, state.rest.candidates.Select(c => c.id));
        Assert.IsFalse(state.legalActions.Any(a => a.op == "run_recruit"));
    }

    [Test] public void Equip_UsesTheFormalCompatibleMemberSelectionAndNextBattleLoadout()
    {
        var member = Map.Snapshot.runState.partyMembers[0];
        Weapon_SO weapon = AssetDatabase.FindAssets("t:Weapon_SO").Select(g => AssetDatabase.LoadAssetAtPath<Weapon_SO>(AssetDatabase.GUIDToAssetPath(g)))
            .First(w => w != null && w != member.weaponData && w.weaponType == member.characterData.compatibleWeaponType
                && w.skillPool != null && w.skillPool.Length > 0 && w.skillPool.All(s => s != null));
        Map.Snapshot.runState.AddWeapon(weapon);
        var action = session.Observe().legalActions.Single(a => a.op == "run_equip" && a.assetId == AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(weapon)));
        int ap = Map.Points.CurrentPoints; var state = Act("run_equip", asset: action.assetId);
        Assert.AreSame(weapon, member.weaponData); Assert.AreEqual(ap, state.actionPoints);
        Assert.AreEqual(action.assetId, state.party[0].weapon);
        Assert.Throws<ArgumentException>(() => Act("run_equip", asset: action.assetId));
        var hostile = Map.Snapshot.tiles.First(t => t.IsHostileTile && t.tileType != MapTileType.Boss && t.assignedEnemyGroup != null && t.assignedEnemyGroup.IsValid);
        using (var bridge = new AgentAdventureBattleBridge(Map.Snapshot, hostile, hostile.assignedEnemyGroup,
            Map.Configuration.starting.difficulty, Map.Configuration.money, Map.Configuration.eliteRelics, Map.Configuration.levels))
        {
            Assert.AreSame(weapon, bridge.Battle.Encounter.partyMembers[0].weaponData);
            Assert.AreEqual(AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(weapon.skillPool[0])),
                bridge.Battle.Observe().actors.Single(a => a.side == "ally").skills[0].id);
        }
    }

    [Test] public void RestAndRecruitQuotes_DoNotReplaceTheUiSnapshotOrRestVisit()
    {
        MapRunRuntime.RestoreSnapshot(new MapRunSnapshot { sceneName = "UserSentinel", runState = new RunState { money = 777 } });
        bool pending = RestSceneRuntime.HasPendingVisit; string before = JsonUtility.ToJson(Global.GetValue(null));
        Enter(); for (int i = 0; i < 5; i++) session.Observe(); Act("run_rest_heal");
        Assert.AreEqual(before, JsonUtility.ToJson(Global.GetValue(null))); Assert.AreEqual(pending, RestSceneRuntime.HasPendingVisit);
    }
}
