using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

public sealed class AgentAdventureSaveTests
{
    private AgentAdventureSession session;
    private readonly List<string> profiles = new List<string>(), slots = new List<string>();
    private UnityEngine.Random.State random;
    private long now;
    [SetUp] public void SetUp() { random = UnityEngine.Random.state; now = 10000; }
    [TearDown] public void TearDown()
    {
        AgentAdventureAtomicFile.FaultInjection = null;
        session?.Dispose(); session = null; AgentBattleServer.Stop();
        foreach (var id in profiles)
        {
            string directory = Path.GetFullPath(Path.Combine("Library", "AgentAdventure", "Profiles", id));
            foreach (string file in new[] { "ability_points.json", "ability_progression.json", "committed.json", "lease.lock" })
                if (File.Exists(Path.Combine(directory, file))) File.Delete(Path.Combine(directory, file));
            if (Directory.Exists(directory)) Directory.Delete(directory, false);
        }
        foreach (string slot in slots) if (File.Exists(AgentAdventureSaveService.SlotPath(slot))) File.Delete(AgentAdventureSaveService.SlotPath(slot));
        profiles.Clear(); slots.Clear(); UnityEngine.Random.state = random;
    }
    private string ProfileId() { string id = "save-test-" + Guid.NewGuid().ToString("N"); profiles.Add(id); return id; }
    private string Slot() { string id = "save-test-" + Guid.NewGuid().ToString("N"); slots.Add(id); return id; }
    private AgentAdventureProfile Profile(string id) => new AgentAdventureProfile(AgentAdventureConfiguration.Load().abilities, id);
    private void Open(string id = null, int seed = 1234) => session = new AgentAdventureSession(new AgentAdventureRequest { seed = seed, profileId = id }, () => now);
    private void Close() { session.Dispose(); session = null; }
    private AgentAdventureRequest Request(string op) => new AgentAdventureRequest { op = op, sessionId = session.Id,
        requestId = Guid.NewGuid().ToString("N"), revision = session.Observe().revision };
    private AgentAdventureObservation Act(string op) => session.Act(Request(op)).state;
    private void Battle()
    {
        var move = Request("run_move"); move.x = move.y = 1;
        Assert.AreEqual("battle", session.Act(move).state.phase);
    }
    private static void Fail(string fileName, string boundary)
    {
        AgentAdventureAtomicFile.FaultInjection = (path, stage) =>
        { if (Path.GetFileName(path) == fileName && stage == boundary) throw new IOException("Simulated process interruption at " + boundary); };
    }

    [Test] public void MidBattleSaveRestoresTargetsAndRetryCacheThenContinuesAtLiveTime()
    {
        Open(); Battle(); var state = session.Observe();
        var request = Request("run_battle"); request.battleAction = state.legalActions.First(a => a.op == "run_battle" && a.battleAction.op == "target").battleAction;
        state = session.Act(request).state; string hash = AgentAdventureReplayService.StateHash(state), oldId = session.Id;
        string slot = Slot(); session.Save(slot); Close(); now += 3601;
        session = AgentAdventureSaveService.Load(slot, () => now);
        Assert.AreNotEqual(oldId, session.Id); Assert.AreEqual(hash, AgentAdventureReplayService.StateHash(session.Observe()));
        request.sessionId = session.Id;
        Assert.AreEqual(hash, AgentAdventureReplayService.StateHash(session.Act(request).state));
        Assert.AreEqual(2, session.Observe().revision);
        state = Act("run_abandon"); Assert.AreEqual(3601, state.settlement.data.displayStats.durationSeconds);
        now += 13; state = Act("run_next"); Assert.AreEqual(now, state.progress.runStartedUnixSeconds);
        var replay = session.Replay(); Close(); Assert.IsTrue(AgentAdventureReplayService.Verify(replay).verified);
    }

    [TestCase("treasure")][TestCase("treasure_result")][TestCase("map")]
    public void NormalTreasureDecisionRestoresAndPaysOnlyOnce(string phase)
    {
        Open(seed:127); var move = Request("run_move"); move.x = 3; move.y = 1; session.Act(move);
        if (phase != "treasure") Act("run_choose_treasure");
        if (phase == "map") Act("run_continue_treasure");
        var state = session.Observe(); Assert.AreEqual(phase, state.phase);
        string slot = Slot(); session.Save(slot); Close(); session = AgentAdventureSaveService.Load(slot, () => now);
        Assert.AreEqual(AgentAdventureReplayService.StateHash(state), AgentAdventureReplayService.StateHash(session.Observe()));
        if (phase == "treasure") Act("run_choose_treasure");
        if (phase != "map") Act("run_continue_treasure");
        Assert.AreEqual(37, session.Observe().money); Assert.AreEqual("map", session.Observe().phase);
    }

    [Test] public void PersistentRecoveryUsesLastAcceptedCommandWithoutAnExplicitSave()
    {
        string id = ProfileId(); Open(id); Battle(); string hash = AgentAdventureReplayService.StateHash(session.Observe()); Close();
        session = AgentAdventureSaveService.Recover(id, () => now);
        Assert.AreEqual(hash, AgentAdventureReplayService.StateHash(session.Observe()));
        Act("run_abandon"); var unlock = Request("run_unlock_ability"); unlock.assetId = "journey-start"; session.Act(unlock);
        Act("run_next"); hash = AgentAdventureReplayService.StateHash(session.Observe()); Close();
        session = AgentAdventureSaveService.Recover(id, () => now);
        Assert.AreEqual(hash, AgentAdventureReplayService.StateHash(session.Observe()));
        Assert.AreEqual(2, session.Observe().runIndex);
    }

    [Test] public void ZeroRewardSettlementInvalidatesOlderProgressionGeneration()
    {
        string id = ProfileId(), slot = Slot(); Open(id); session.Save(slot);
        Assert.AreEqual(0, Act("run_abandon").meta.abilityPoints); Close();
        StringAssert.Contains("Stale save", Assert.Throws<ArgumentException>(() => AgentAdventureSaveService.Load(slot)).Message);
        session = AgentAdventureSaveService.Recover(id, () => now);
        Assert.AreEqual("finished", session.Observe().phase); Assert.AreEqual(0, session.Observe().meta.abilityPoints);
    }

    [Test] public void NewAdventureInvalidatesPreviousOwnerEvenWithIdenticalWallet()
    {
        string id = ProfileId(), slot = Slot(); Open(id); session.Save(slot); Close(); Open(id); Close();
        StringAssert.Contains("Stale save", Assert.Throws<ArgumentException>(() => AgentAdventureSaveService.Load(slot)).Message);
    }

    [TestCase("before_replace")][TestCase("after_replace")]
    public void InterruptedAbilityPurchaseRecoversBothFilesAndRetriesExactlyOnce(string boundary)
    {
        string id = ProfileId();
        // Directed initial points, not a normal earned-point victory claim.
        using (var setup = Profile(id)) { setup.AddPoints(1000); Assert.IsTrue(setup.Progression.TryUnlock("journey-start").success); }
        Open(id); Act("run_abandon"); var request = Request("run_unlock_ability"); request.assetId = "start-money-1";
        Fail("committed.json", boundary);
        Assert.Throws<IOException>(() => session.Act(request));
        Assert.Throws<InvalidOperationException>(() => session.Act(request));
        AgentAdventureAtomicFile.FaultInjection = null; Close();
        session = AgentAdventureSaveService.Recover(id, () => now);
        bool committed = boundary == "after_replace";
        Assert.AreEqual(committed ? 999 : 1000, session.Observe().meta.abilityPoints);
        Assert.AreEqual(committed ? "Unlocked" : "Available", session.Observe().meta.nodes.Single(n => n.id == "start-money-1").state);
        request.sessionId = session.Id;
        var response = session.Act(request);
        Assert.AreEqual(999, response.state.meta.abilityPoints); Assert.AreEqual(2, response.state.revision);
        Assert.IsTrue(response.state.meta.persistent); Assert.AreEqual(id, response.state.meta.profileId);
        var retry = session.Act(request); Assert.AreEqual(JsonUtility.ToJson(response), JsonUtility.ToJson(retry));
        Close(); session = AgentAdventureSaveService.Recover(id, () => now);
        Assert.AreEqual(999, session.Observe().meta.abilityPoints); Assert.AreEqual(2, session.Observe().revision);
    }

    [TestCase("before_replace")][TestCase("after_replace")]
    public void PositiveWalletAwardUsesOneAtomicCommitPoint(string boundary)
    {
        string id = ProfileId();
        using (var profile = Profile(id))
        {
            profile.AddPoints(100);
            using (var store = new AgentAdventureProfileStore(profile))
            {
                profile.AddPoints(10); Fail("committed.json", boundary);
                Assert.Throws<IOException>(() => store.Commit("transaction-test", null, true));
                AgentAdventureAtomicFile.FaultInjection = null;
            }
        }
        using (var profile = Profile(id))
        using (var store = new AgentAdventureProfileStore(profile))
        {
            Assert.AreEqual(boundary == "after_replace" ? 110 : 100, profile.Wallet.GetCurrentPoints());
            if (boundary == "before_replace") { profile.AddPoints(10); store.Commit("transaction-test", null, true); }
        }
        using (var profile = Profile(id))
        using (var store = new AgentAdventureProfileStore(profile)) Assert.AreEqual(110, profile.Wallet.GetCurrentPoints());
    }

    [Test] public void ProfileLeasePreventsASecondHostFromMutatingTheSameProfile()
    {
        string id = ProfileId();
        using (var first = Profile(id))
        using (var owner = new AgentAdventureProfileStore(first))
        using (var second = Profile(id)) Assert.Throws<IOException>(() => new AgentAdventureProfileStore(second));
    }

    [Test] public void FailedSaveReplacementKeepsThePriorSlotAndLiveSessionUsable()
    {
        Open(); string slot = Slot(); session.Save(slot); string old = File.ReadAllText(AgentAdventureSaveService.SlotPath(slot));
        Act("run_abandon"); Fail(slot + ".json", "before_replace"); Assert.Throws<IOException>(() => session.Save(slot));
        AgentAdventureAtomicFile.FaultInjection = null;
        Assert.AreEqual(old, File.ReadAllText(AgentAdventureSaveService.SlotPath(slot))); Assert.AreEqual("finished", session.Observe().phase);
        Act("run_next");
    }

    [Test] public void TransientWindowsSharingFailureRetriesWithoutDeletingTheCommittedSave()
    {
        Open(); string slot = Slot(); session.Save(slot); string path = AgentAdventureSaveService.SlotPath(slot);
        Act("run_abandon"); int attempts = 0;
        AgentAdventureAtomicFile.FaultInjection = (file, stage) =>
        {
            if (file == path && stage == "replace_attempt" && attempts++ < 2)
            {
                Assert.IsTrue(File.Exists(path));
                throw new IOException("Simulated sharing violation", unchecked((int)0x80070020));
            }
        };
        session.Save(slot); AgentAdventureAtomicFile.FaultInjection = null;
        Assert.AreEqual(3, attempts); Close(); session = AgentAdventureSaveService.Load(slot, () => now);
        Assert.AreEqual("finished", session.Observe().phase);
    }

    [Test] public void SavedConfigurationDirectoryChangesInvalidateTheJournalVersion()
    {
        Open(); string file = Path.Combine("Assets", "Configs", "AgentSaveTest-" + Guid.NewGuid().ToString("N") + ".json");
        try
        {
            File.WriteAllText(file, "{\"version\":1}");
            Assert.Throws<InvalidOperationException>(() => session.Save(Slot()));
        }
        finally
        {
            if (File.Exists(file)) File.Delete(file);
            if (File.Exists(file + ".meta")) File.Delete(file + ".meta");
        }
    }

    [Test] public void AuthoredStartingRelicAbilityRestoresPendingBagAndDoesNotAcquireTwice()
    {
        string id = ProfileId(); var config = AgentAdventureConfiguration.Load().abilities;
        using (var setup = new AgentAdventureProfile(config, id))
        {
            setup.AddPoints(1000); // Directed initial meta resources, not a normal earned-point route.
            void Unlock(string nodeId)
            {
                if (setup.Progression.GetNodeState(nodeId) == AbilityNodeState.Unlocked) return;
                foreach (string prerequisite in config.FindNode(nodeId).prerequisiteNodeIds) Unlock(prerequisite);
                var result = setup.Progression.TryUnlock(nodeId); Assert.IsTrue(result.success, result.message);
            }
            Unlock("start-random-relic");
        }
        Open(id); Assert.AreEqual("starting_relic", session.Observe().phase);
        string slot = Slot(), before = AgentAdventureReplayService.StateHash(session.Observe()); session.Save(slot); Close();
        session = AgentAdventureSaveService.Load(slot, () => now);
        Assert.AreEqual(before, AgentAdventureReplayService.StateHash(session.Observe()));
        var request = Request("run_choose_starting_relic"); var response = session.Act(request);
        Assert.AreEqual("map", response.state.phase); Assert.Greater(response.state.relics.Sum(r => r.amount), 0);
        Assert.AreEqual(JsonUtility.ToJson(response), JsonUtility.ToJson(session.Act(request)));
        string after = AgentAdventureReplayService.StateHash(session.Observe()); Close();
        session = AgentAdventureSaveService.Recover(id, () => now);
        Assert.AreEqual(after, AgentAdventureReplayService.StateHash(session.Observe()));
        var replay = session.Replay(); Close(); Assert.IsTrue(AgentAdventureReplayService.Verify(replay).verified);
    }

    [Test] public void CorruptCanonicalProfileRejectsWithoutZeroingWorkingWallet()
    {
        string id = ProfileId();
        using (var profile = Profile(id))
        {
            profile.AddPoints(100);
            using (var store = new AgentAdventureProfileStore(profile)) { }
            File.WriteAllText(Path.Combine(profile.StorageDirectory, "committed.json"), "{broken");
            Assert.Throws<ArgumentException>(() => new AgentAdventureProfileStore(profile));
            Assert.AreEqual(100, profile.Wallet.GetCurrentPoints());
        }
    }

    [Test] public void ValidJsonWithChangedCanonicalPointsFailsItsIntegrityCheck()
    {
        string id = ProfileId();
        using (var profile = Profile(id))
        {
            profile.AddPoints(100);
            using (var store = new AgentAdventureProfileStore(profile)) { }
            string path = Path.Combine(profile.StorageDirectory, "committed.json");
            var head = JsonUtility.FromJson<AgentAdventureProfileHead>(File.ReadAllText(path)); head.profile.points = 999;
            File.WriteAllText(path, JsonUtility.ToJson(head));
            Assert.Throws<ArgumentException>(() => new AgentAdventureProfileStore(profile));
            Assert.AreEqual(100, profile.Wallet.GetCurrentPoints());
        }
    }

    [TestCase("ability_points.json")][TestCase("ability_progression.json")]
    public void CorruptLegacyProfileIsNotMigratedAsZeroProgress(string file)
    {
        string id = ProfileId();
        using (var profile = Profile(id))
        {
            Directory.CreateDirectory(profile.StorageDirectory);
            string path = Path.Combine(profile.StorageDirectory, file); File.WriteAllText(path, "{broken");
            Assert.Throws<IOException>(() => new AgentAdventureProfileStore(profile));
            Assert.AreEqual("{broken", File.ReadAllText(path));
            Assert.IsFalse(File.Exists(Path.Combine(profile.StorageDirectory, "committed.json")));
        }
    }

    [Test] public void UnknownRecoveryDoesNotCreateAnEmptyProfile()
    {
        string id = ProfileId(); Assert.Throws<ArgumentException>(() => AgentAdventureSaveService.Recover(id));
        Assert.IsFalse(Directory.Exists(Path.Combine("Library", "AgentAdventure", "Profiles", id)));
    }

    [Test] public void PublicSaveLoadAndRecoveryRespectTheSingleSessionBoundary()
    {
        string slot = Slot(), id = ProfileId();
        AgentAdventureResponse Call(AgentAdventureRequest request) => JsonUtility.FromJson<AgentAdventureResponse>(AgentBattleServer.Dispatch(JsonUtility.ToJson(request)));
        var created = Call(new AgentAdventureRequest { op = "create_run", seed = 127, profileId = id }); Assert.IsTrue(created.success, created.message);
        string oldId = created.state.sessionId;
        var saved = Call(new AgentAdventureRequest { op = "run_save", sessionId = oldId, slotId = slot }); Assert.IsTrue(saved.success, saved.message);
        Assert.AreEqual(0, saved.save.revision);
        Assert.IsFalse(Call(new AgentAdventureRequest { op = "run_load", slotId = slot }).success);
        Call(new AgentAdventureRequest { op = "run_close", sessionId = oldId });
        var loaded = Call(new AgentAdventureRequest { op = "run_load", slotId = slot }); Assert.IsTrue(loaded.success, loaded.message);
        Assert.AreNotEqual(oldId, loaded.state.sessionId);
        Call(new AgentAdventureRequest { op = "run_close", sessionId = loaded.state.sessionId });
        var recovered = Call(new AgentAdventureRequest { op = "run_recover", profileId = id }); Assert.IsTrue(recovered.success, recovered.message);
        Assert.AreEqual(AgentAdventureReplayService.StateHash(created.state), AgentAdventureReplayService.StateHash(recovered.state));
    }
}
