using System;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class AgentAdventureReplayTests
{
    private AgentAdventureSession session;
    private UnityEngine.Random.State outside;
    private long now = 10000;
    [SetUp] public void SetUp() { outside = UnityEngine.Random.state; now = 10000; }
    [TearDown] public void TearDown() { session?.Dispose(); session = null; UnityEngine.Random.state = outside; }
    private void Open(string profile = null) => session = new AgentAdventureSession(new AgentAdventureRequest { seed = 1234, profileId = profile }, () => now);
    private AgentAdventureRequest Request(string op) => new AgentAdventureRequest { op = op, sessionId = session.Id,
        requestId = Guid.NewGuid().ToString("N"), revision = session.Observe().revision };
    private AgentAdventureObservation Act(string op)
    {
        now += 7; return session.Act(Request(op)).state;
    }
    private void FirstBattle()
    {
        var move = Request("run_move"); move.x = move.y = 1;
        Assert.AreEqual("battle", session.Act(move).state.phase);
    }
    private AgentAdventureReplay CloseJournal()
    {
        var journal = session.Replay(); session.Dispose(); session = null; return journal;
    }

    [Test] public void JournalClonesRequestsAndIncludesOnlyAcceptedFirstExecutions()
    {
        Open(); string initial = AgentAdventureReplayService.StateHash(session.Observe());
        var request = Request("run_abandon"); var first = session.Act(request);
        now += 100; session.Act(request);
        var stale = Request("run_next"); stale.revision = 0;
        Assert.Throws<ArgumentException>(() => session.Act(stale));
        request.seed = 987;
        var replay = session.Replay();
        Assert.AreEqual(initial, replay.initialStateHash); Assert.AreEqual(1, replay.commands.Length);
        Assert.AreEqual(10000, replay.commands[0].utcSeconds); Assert.AreEqual(1, replay.commands[0].request.seed);
        Assert.IsTrue(string.IsNullOrEmpty(replay.create.profileId)); Assert.IsTrue(string.IsNullOrEmpty(replay.commands[0].request.sessionId));
        replay.commands[0].request.op = "corrupt"; replay.initialProfile.points = 999;
        Assert.AreEqual("run_abandon", session.Replay().commands[0].request.op);
        Assert.AreEqual(0, session.Replay().initialProfile.points);
        Assert.AreEqual(JsonUtility.ToJson(first.state), JsonUtility.ToJson(session.Observe()));
        Assert.IsTrue(AgentAdventureReplayService.Verify(CloseJournal()).verified);
    }

    [Test] public void NormalDefeatSettlementFreeUnlockAndNextRunReplayWithRecordedWallClock()
    {
        Open(); FirstBattle(); var state = session.Observe();
        for (int i = 0; i < 100 && state.battle.phase == "targeting"; i++)
        {
            var command = Request("run_battle"); command.battleAction = state.legalActions.Single(a => a.op == "run_battle" && a.battleAction.op == "commit").battleAction;
            now += 17; state = session.Act(command).state;
        }
        Assert.AreEqual("defeat", state.battle.outcome); Act("run_return"); now += 3601;
        state = Act("run_settle"); Assert.AreEqual("finished", state.phase);
        Assert.Greater(state.settlement.data.scoreResult.displayStats.durationSeconds, 3600);
        var unlock = Request("run_unlock_ability"); unlock.assetId = "journey-start"; session.Act(unlock);
        var next = Request("run_next"); next.seed = 79; now += 88; state = session.Act(next).state;
        Assert.AreEqual(2, state.runIndex); Assert.AreEqual(now, state.progress.runStartedUnixSeconds);
        var replay = CloseJournal(); var result = AgentAdventureReplayService.Verify(replay);
        Assert.AreEqual(replay.commands.Length, result.commandsChecked);
        Assert.AreEqual(replay.finalStateHash, result.finalStateHash);
        Assert.AreEqual(79, result.finalState.seed); Assert.AreEqual(now, result.finalState.progress.runStartedUnixSeconds);
        Assert.AreEqual("Unlocked", result.finalState.meta.nodes.Single(n => n.id == "journey-start").state);
    }

    [Test] public void MidBattleReplaysSameTargetsDiceEventsAndCallerRandomState()
    {
        Open(); FirstBattle(); var state = session.Observe();
        var target = state.legalActions.First(a => a.op == "run_battle" && a.battleAction.op == "target");
        var command = Request("run_battle"); command.battleAction = target.battleAction; session.Act(command);
        var replay = CloseJournal(); string rng = JsonUtility.ToJson(UnityEngine.Random.state);
        var result = AgentAdventureReplayService.Verify(replay);
        Assert.AreEqual("battle", result.finalState.phase); Assert.AreEqual(replay.finalStateHash, result.finalStateHash);
        Assert.AreEqual(rng, JsonUtility.ToJson(UnityEngine.Random.state));
        Assert.IsNull(BattleEncounterRuntime.ActiveEncounter);
    }

    [Test] public void Disconnect_UsesOuterRevisionAndSurvivesFullAdventureReplay()
    {
        Open(); FirstBattle();
        var state = session.Observe();
        var target = Request("run_battle");
        target.battleAction = state.legalActions.First(a => a.op == "run_battle" && a.battleAction.op == "target").battleAction;
        state = session.Act(target).state;
        var disconnect = Request("run_battle");
        disconnect.battleAction = state.legalActions.First(a => a.op == "run_battle" && a.battleAction.op == "disconnect").battleAction;
        int revision = state.revision;
        state = session.Act(disconnect).state;
        Assert.AreEqual(revision + 1, state.revision);
        Assert.IsTrue(string.IsNullOrEmpty(state.battle.actors.Single(a => a.id == disconnect.battleAction.actorId).slots[disconnect.battleAction.buttonIndex].targetId));
        var replay = CloseJournal();
        var verification = AgentAdventureReplayService.Verify(replay);
        Assert.IsTrue(verification.verified);
        Assert.AreEqual(replay.commands.Length, verification.commandsChecked);
    }

    [Test] public void PersistentInitialProfileIsReplayedInTemporaryStorageWithoutSpendingAgain()
    {
        string id = "replay-test-" + Guid.NewGuid().ToString("N"), wallet = null, progression = null;
        try
        {
            using (var setup = new AgentAdventureProfile(AgentAdventureConfiguration.Load().abilities, id))
            {
                setup.AddPoints(1000); Assert.IsTrue(setup.Progression.TryUnlock("journey-start").success);
                wallet = setup.Wallet.FilePath; progression = setup.Progression.FilePath;
            }
            Open(id); Act("run_abandon");
            var paid = session.Observe().legalActions.First(a => a.op == "run_unlock_ability"
                && session.Observe().meta.nodes.Single(n => n.id == a.assetId).cost > 0);
            var request = Request(paid.op); request.assetId = paid.assetId; session.Act(request); Act("run_next");
            var replay = CloseJournal(); string beforeWallet = File.ReadAllText(wallet), beforeProgression = File.ReadAllText(progression);
            Assert.AreEqual(1000, replay.initialProfile.points);
            var verified = AgentAdventureReplayService.Verify(replay);
            Assert.IsTrue(verified.verified); Assert.Less(verified.finalState.meta.abilityPoints, 1000);
            Assert.AreEqual(beforeWallet, File.ReadAllText(wallet)); Assert.AreEqual(beforeProgression, File.ReadAllText(progression));
        }
        finally
        {
            session?.Dispose(); session = null;
            if (wallet != null && File.Exists(wallet)) File.Delete(wallet);
            if (progression != null && File.Exists(progression)) File.Delete(progression);
            foreach (string file in new[] { "committed.json", "lease.lock" })
                if (wallet != null && File.Exists(Path.Combine(Path.GetDirectoryName(wallet), file))) File.Delete(Path.Combine(Path.GetDirectoryName(wallet), file));
            if (wallet != null && Directory.Exists(Path.GetDirectoryName(wallet))) Directory.Delete(Path.GetDirectoryName(wallet), false);
        }
    }

    [TestCase("format")][TestCase("api")][TestCase("engine")][TestCase("content")]
    public void VersionMismatchRejectsBeforeCreatingAnyRuntime(string kind)
    {
        Open(); var replay = CloseJournal();
        if (kind == "format") replay.formatVersion++;
        if (kind == "api") replay.apiVersion = "future";
        if (kind == "engine") replay.engineVersion = "future";
        if (kind == "content") replay.contentHash = "future";
        Assert.Throws<ArgumentException>(() => AgentAdventureReplayService.Verify(replay));
        Assert.IsNull(BattleEncounterRuntime.ActiveEncounter); Open();
    }

    [Test] public void TamperedActionOrStateHashFailsAtItsRevisionAndReleasesRuntime()
    {
        Open(); FirstBattle(); var replay = CloseJournal();
        replay.commands[0].stateHash = "tampered";
        StringAssert.Contains("revision 1", Assert.Throws<ArgumentException>(() => AgentAdventureReplayService.Verify(replay)).Message);
        replay.commands[0].request.op = "run_next";
        StringAssert.Contains("revision 1", Assert.Throws<ArgumentException>(() => AgentAdventureReplayService.Verify(replay)).Message);
        Assert.IsNull(BattleEncounterRuntime.ActiveEncounter); Open();
    }

    [Test] public void SlotRoundTripIsAtomicOnReplacementAndDoesNotShareMutableObjects()
    {
        string slot = "test-" + Guid.NewGuid().ToString("N"), path = AgentAdventureReplayService.SlotPath(slot);
        try
        {
            Open(); AgentAdventureReplayService.Save(slot, session.Replay());
            Assert.AreEqual(0, AgentAdventureReplayService.Load(slot).commands.Length);
            Act("run_abandon"); AgentAdventureReplayService.Save(slot, CloseJournal());
            var replay = AgentAdventureReplayService.Load(slot);
            Assert.AreEqual(1, replay.commands.Length); Assert.IsTrue(AgentAdventureReplayService.Verify(replay).verified);
            replay.contentHash = "mutated"; Assert.AreNotEqual("mutated", AgentAdventureReplayService.Load(slot).contentHash);
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    [TestCase(null)][TestCase("")][TestCase("../player")][TestCase("C:/player")][TestCase("NUL")]
    public void SlotRejectsPathsAndDeviceNames(string slot) => Assert.Throws<ArgumentException>(() => AgentAdventureReplayService.SlotPath(slot));

    [Test] public void PublicVerificationRequiresClosingTheActiveSessionAndExportIsReadOnly()
    {
        string slot = "http-test-" + Guid.NewGuid().ToString("N"), path = AgentAdventureReplayService.SlotPath(slot);
        try
        {
            var created = JsonUtility.FromJson<AgentAdventureResponse>(AgentBattleServer.Dispatch("{\"op\":\"create_run\",\"seed\":1234}"));
            Assert.IsTrue(created.success); string id = created.state.sessionId;
            string before = AgentAdventureReplayService.StateHash(created.state);
            var exported = JsonUtility.FromJson<AgentAdventureResponse>(AgentBattleServer.Dispatch(JsonUtility.ToJson(new AgentAdventureRequest { op = "run_export", sessionId = id, slotId = slot })));
            Assert.IsTrue(exported.success); Assert.AreEqual(before, exported.replay.finalStateHash);
            Assert.IsFalse(JsonUtility.FromJson<AgentAdventureResponse>(AgentBattleServer.Dispatch(JsonUtility.ToJson(new AgentAdventureRequest { op = "run_verify", slotId = slot }))).success);
            Assert.IsTrue(JsonUtility.FromJson<AgentAdventureResponse>(AgentBattleServer.Dispatch(JsonUtility.ToJson(new AgentAdventureRequest { op = "run_close", sessionId = id }))).success);
            var result = JsonUtility.FromJson<AgentAdventureResponse>(AgentBattleServer.Dispatch(JsonUtility.ToJson(new AgentAdventureRequest { op = "run_verify", slotId = slot })));
            Assert.IsTrue(result.success, result.message); Assert.IsTrue(result.verification.verified);
        }
        finally
        {
            AgentBattleServer.Stop(); if (File.Exists(path)) File.Delete(path);
        }
    }
}
