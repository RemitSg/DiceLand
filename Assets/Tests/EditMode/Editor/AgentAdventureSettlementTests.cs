using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class AgentAdventureSettlementTests
{
    private AgentAdventureSession session;
    private long now;
    private UnityEngine.Random.State outsideRandom;
    private RunSettlementData outsideSettlement;
    private readonly List<UnityEngine.Object> temporary = new List<UnityEngine.Object>();
    private AgentAdventureMapRuntime Map => (AgentAdventureMapRuntime)Field("map").GetValue(session);
    private AgentAdventureProfile Profile => (AgentAdventureProfile)Field("profile").GetValue(session);
    private static FieldInfo Field(string name) => typeof(AgentAdventureSession).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
    [SetUp] public void SetUp() { now = 10000; outsideRandom = UnityEngine.Random.state; outsideSettlement = RunSettlementRuntime.Current; }
    [TearDown] public void TearDown()
    {
        session?.Dispose(); session = null;
        foreach (var o in temporary) if (o != null) UnityEngine.Object.DestroyImmediate(o);
        temporary.Clear(); UnityEngine.Random.state = outsideRandom; RunSettlementRuntime.Set(outsideSettlement);
    }
    private void Open(string profileId = null) { session = new AgentAdventureSession(new AgentAdventureRequest { seed = 1234, profileId = profileId }, () => now); }
    private AgentAdventureRequest Command(string op, string asset = null, int seed = 42, int choice = 0)
    {
        var s = session.Observe();
        return new AgentAdventureRequest { op = op, assetId = asset, seed = seed, choiceIndex = choice,
            sessionId = s.sessionId, revision = s.revision, requestId = Guid.NewGuid().ToString("N") };
    }
    private AgentAdventureObservation Act(string op, string asset = null, int seed = 42, int choice = 0) => session.Act(Command(op, asset, seed, choice)).state;
    private AgentAdventureObservation Move(BoardTileData tile)
    {
        var c = Command("run_move"); c.x = tile.coord.x; c.y = tile.coord.y; return session.Act(c).state;
    }
    private AgentAdventureObservation BattleAct(AgentAdventureAction action)
    {
        var c = Command(action.op); c.battleAction = action.battleAction; return session.Act(c).state;
    }
    private AgentAdventureObservation RealFirstBattle() => Move(Map.LegalDestinations().Single(t => t.coord == new Vector2Int(1, 1)));

    [Test] public void MapAbandonAwardsNothingAndRetryDoesNotAbandonTheNextRun()
    {
        Open(); Map.Snapshot.runState.settlementStats.bossesDefeated = 10; now += 3601;
        var request = Command("run_abandon"); var first = session.Act(request);
        Assert.AreEqual("finished", first.state.phase); Assert.AreEqual("abandoned", first.state.outcome);
        Assert.AreEqual("map_abandon_no_rewards", first.state.settlement.policy); Assert.IsNull(first.state.settlement.data);
        Assert.AreEqual(0, first.state.meta.abilityPoints);
        var next = Act("run_next", seed: 79);
        Assert.AreEqual(79, next.seed); Assert.AreEqual(2, next.runIndex); Assert.AreEqual("map", next.phase);
        Assert.AreEqual(JsonUtility.ToJson(first), JsonUtility.ToJson(session.Act(request)));
        Assert.AreEqual("map", session.Observe().phase); Assert.AreEqual(0, next.progress.bossesDefeated);
        Assert.IsFalse(next.settlement.completed); Assert.AreEqual(now, next.progress.runStartedUnixSeconds);
    }

    [Test] public void BattleAbandonUsesFormalNonClearScoreOnceAndDoesNotClaimBattleRewards()
    {
        Open(); var state = RealFirstBattle(); Assert.AreEqual("battle", state.phase);
        Map.Snapshot.runState.settlementStats.bossesDefeated = 2; now += 3601;
        var expected = RunScoreService.Calculate(Map.Snapshot.runState, null, false, now);
        int money = state.money; var command = Command("run_abandon"); var result = session.Act(command);
        Assert.AreEqual("finished", result.state.phase); Assert.AreEqual("abandoned", result.state.outcome);
        Assert.AreEqual(expected.finalScore, result.state.settlement.data.scoreResult.finalScore);
        Assert.AreEqual(expected.gainedAbilityPoints, result.state.meta.abilityPoints);
        Assert.AreEqual(money, result.state.money); Assert.IsNull(BattleEncounterRuntime.ActiveEncounter);
        Assert.AreEqual(JsonUtility.ToJson(result), JsonUtility.ToJson(session.Act(command)));
        result.state.settlement.data.scoreResult.finalScore = -100;
        Assert.AreEqual(expected.finalScore, session.Observe().settlement.data.scoreResult.finalScore);
        Assert.Throws<ArgumentException>(() => session.Act(Command("run_settle")));
    }

    [Test] public void RealNormalBattleDefeatReturnsSettlesAndStartsFresh()
    {
        Open(); var state = RealFirstBattle();
        for (int i = 0; i < 80 && state.battle.phase == "targeting"; i++)
            state = BattleAct(state.legalActions.Single(a => a.op == "run_battle" && a.battleAction.op == "commit"));
        Assert.AreEqual("defeat", state.battle.outcome);
        state = Act("run_return"); Assert.AreEqual("defeat_pending_settlement", state.phase);
        Assert.IsTrue(state.legalActions.All(a => a.op == "run_settle"));
        now += 3601;
        var expected = RunScoreService.Calculate(Map.Snapshot.runState, null, false, now);
        state = Act("run_settle"); Assert.AreEqual("defeat", state.outcome); Assert.AreEqual("finished", state.phase);
        Assert.AreEqual(expected.finalScore, state.settlement.data.scoreResult.finalScore);
        Assert.AreEqual(3601, state.settlement.data.displayStats.durationSeconds);
        state = Act("run_next", seed: 127); Assert.AreEqual(16, state.party.Single().hp); Assert.AreEqual(127, state.seed);
    }

    [Test] public void DirectedBossWinRequiresRealCombatAndUsesTheClearMultiplierRules()
    {
        Open(); var group = ScriptableObject.CreateInstance<EnemyGroup_SO>(); temporary.Add(group);
        group.enemies.Add(AssetDatabase.FindAssets("t:EnemyData_SO", new[] { "Assets/Data" }).Select(g => AssetDatabase.LoadAssetAtPath<EnemyData_SO>(AssetDatabase.GUIDToAssetPath(g))).First(e => e.enemyID == 1));
        var tile = Map.LegalDestinations().First(); tile.tileType = MapTileType.Boss; tile.isResolved = false; tile.assignedEnemyGroup = group;
        Map.Snapshot.runState.currentKeys = Map.Snapshot.runState.requiredKeys;
        var state = Move(tile);
        for (int i = 0; i < 50 && state.battle.phase == "targeting"; i++)
        {
            var target = state.legalActions.FirstOrDefault(a => a.op == "run_battle" && a.battleAction.op == "target");
            if (target != null) state = BattleAct(target);
            state = BattleAct(state.legalActions.Single(a => a.op == "run_battle" && a.battleAction.op == "commit"));
        }
        Assert.AreEqual("victory", state.battle.outcome);
        state = Act("run_return"); Assert.AreEqual("victory_pending_settlement", state.phase);
        now += 3601; var expected = RunScoreService.Calculate(Map.Snapshot.runState, null, true, now);
        state = Act("run_settle"); Assert.AreEqual("victory", state.outcome);
        Assert.IsTrue(state.settlement.data.runCleared); Assert.IsTrue(state.settlement.data.displayStats.calamityScoreCounted);
        Assert.AreEqual(expected.finalScore, state.settlement.data.scoreResult.finalScore);
    }

    [Test] public void SharedAbilityUnlockAffectsOnlyTheNextRunAndKeepsTheUserSettlement()
    {
        var player = new RunSettlementData { abilityPointsAfter = 987 }; RunSettlementRuntime.Set(player);
        Open(); Act("run_abandon");
        Act("run_unlock_ability", "journey-start");
        Profile.Wallet.SetPoints(2); // Directed meta resources, not evidence of a normal earned-point route.
        Act("run_unlock_ability", "start-money-1"); Assert.AreEqual(1, session.Observe().meta.abilityPoints);
        Assert.AreEqual(0, session.Observe().money);
        var request = Command("run_next", seed: 37); var next = session.Act(request);
        Assert.AreEqual(10, next.state.money); Assert.AreEqual(1, next.state.meta.abilityPoints);
        Assert.AreEqual(JsonUtility.ToJson(next), JsonUtility.ToJson(session.Act(request)));
        Assert.AreSame(player, RunSettlementRuntime.Current);
        Assert.Throws<ArgumentException>(() => session.Act(Command("run_unlock_ability", "start-money-2")));
    }

    [Test] public void PersistentProfileSurvivesCloseAndDefaultProfileDoesNotShareIt()
    {
        string profileId = "test-" + Guid.NewGuid().ToString("N");
        string wallet = null, progression = null;
        try
        {
            Open(profileId); wallet = Profile.Wallet.FilePath; progression = Profile.Progression.FilePath;
            Act("run_abandon"); Act("run_unlock_ability", "journey-start");
            session.Dispose(); session = null; Open(profileId);
            Assert.AreEqual("Unlocked", session.Observe().meta.nodes.Single(n => n.id == "journey-start").state);
            session.Dispose(); session = null; Open();
            Assert.IsFalse(session.Observe().meta.persistent);
            Assert.AreEqual("Available", session.Observe().meta.nodes.Single(n => n.id == "journey-start").state);
        }
        finally
        {
            if (wallet != null && File.Exists(wallet)) File.Delete(wallet);
            if (progression != null && File.Exists(progression)) File.Delete(progression);
            foreach (string file in new[] { "committed.json", "lease.lock" })
                if (wallet != null && File.Exists(Path.Combine(Path.GetDirectoryName(wallet), file))) File.Delete(Path.Combine(Path.GetDirectoryName(wallet), file));
            if (wallet != null && Directory.Exists(Path.GetDirectoryName(wallet))) Directory.Delete(Path.GetDirectoryName(wallet), false);
        }
    }

    [TestCase("../player")][TestCase("C:/player")][TestCase("CON")][TestCase("x.y")][TestCase("a b")]
    public void ProfileRejectsPathsAndDeviceNamesBeforeCreatingAnAdventure(string id)
    {
        Assert.Throws<ArgumentException>(() => new AgentAdventureSession(new AgentAdventureRequest { profileId = id }));
        Assert.IsNull(BattleEncounterRuntime.ActiveEncounter);
    }

    [Test] public void StartingBagUsesFormalRelicAcquisitionAndRetryDoesNotApplyTwice()
    {
        Open();
        var bonus = new AbilityRunBonus();
        bonus.startingRelicBags.Add(new AbilityStartingRelicBagBonus { relicTable = Resources.Load<TreasureRewardTable_SO>("DefaultTreasureRewardTable").RelicTable, bagCount = 1, choiceCount = 3 });
        var bag = AbilityRunBonusRuntimeService.BuildStartingRelicBagChoices(Map.Snapshot.runState, bonus, 3).Single();
        Assert.IsNotNull(bag); Assert.IsTrue(bag.HasChoices);
        Map.StartingChoices.Add(bag); Field("phase").SetValue(session, "starting_relic");
        var before = session.Observe(); Assert.IsNotEmpty(before.startingRelic.choices);
        var request = Command("run_choose_starting_relic", choice: 0); var result = session.Act(request);
        Assert.AreEqual("map", result.state.phase); Assert.IsTrue(bag.rewardApplied);
        Assert.AreEqual(JsonUtility.ToJson(result), JsonUtility.ToJson(session.Act(request)));
        Assert.IsTrue(result.state.relics.Any(r => r.id == before.startingRelic.choices[0].id));
    }

    [Test] public void IllegalTerminalCommandsAndObservationCannotAdvanceTimeOrAwardPoints()
    {
        Open(); string observed = JsonUtility.ToJson(session.Observe());
        now += 5000;
        Assert.Throws<ArgumentException>(() => session.Act(Command("run_settle")));
        Assert.Throws<ArgumentException>(() => session.Act(Command("run_next")));
        Assert.AreEqual(observed, JsonUtility.ToJson(session.Observe()));
        Assert.AreEqual(0, Profile.Wallet.GetCurrentPoints());
    }

    [Test] public void SharedResolvedPartySyncClonesPersistentMembersAndExcludesGuests()
    {
        var member = new RunPartyMemberState { memberId = "persistent", currentHP = 3 };
        var encounter = new BattleEncounterData { resolvedPartyMembers = new List<RunPartyMemberState>
            { member, new RunPartyMemberState { isTemporaryBattleGuest = true } } };
        var state = new RunState(); RunSettlementFlowService.SyncResolvedPartyMembers(state, encounter);
        Assert.AreEqual(1, state.partyMembers.Count); Assert.AreNotSame(member, state.partyMembers[0]);
        Assert.AreEqual(3, state.partyMembers[0].currentHP);
    }
}
