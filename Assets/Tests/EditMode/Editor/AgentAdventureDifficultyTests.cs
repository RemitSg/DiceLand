using System;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class AgentAdventureDifficultyTests
{
    private AgentAdventureSession session;
    private UnityEngine.Random.State outsideRandom;
    private bool outsideInput, outsideCombat;
    private string slot;
    [SetUp] public void SetUp()
    {
        outsideRandom = UnityEngine.Random.state; outsideInput = SpeedButton.InputSuppressed; outsideCombat = SpeedButton.CombatLocked;
    }
    [TearDown] public void TearDown()
    {
        session?.Dispose(); session = null; UnityEngine.Random.state = outsideRandom;
        SpeedButton.InputSuppressed = outsideInput; SpeedButton.CombatLocked = outsideCombat;
        if (slot != null && File.Exists(AgentAdventureSaveService.SlotPath(slot))) File.Delete(AgentAdventureSaveService.SlotPath(slot));
        slot = null;
    }
    private static string Id(UnityEngine.Object asset) => AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(asset));
    private AgentAdventureRequest Request(string op) => new AgentAdventureRequest { op = op, sessionId = session.Id,
        requestId = Guid.NewGuid().ToString("N"), revision = session.Observe().revision };
    private AgentAdventureObservation Act(string op) => session.Act(Request(op)).state;

    [Test] public void CatalogueMatchesSerializedFormalMenuReferencesAndSharedActionPointRules()
    {
        var open = typeof(EditorSceneManager).GetMethod("OpenPreviewScene", BindingFlags.NonPublic | BindingFlags.Static,
            null, new[] { typeof(string) }, null);
        Scene preview = (Scene)open.Invoke(null, new object[] { "Assets/Scenes/Menu Selection.unity" });
        string[] expected;
        try
        {
            var controller = preview.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<MainMenuSceneController>(true)).Single();
            var source = new SerializedObject(controller).FindProperty("difficultyConfigs");
            expected = Enumerable.Range(0, source.arraySize).Select(i => source.GetArrayElementAtIndex(i).objectReferenceValue)
                .Where(a => a != null).Select(Id).Distinct().ToArray();
        }
        finally { EditorSceneManager.ClosePreviewScene(preview); }
        var offered = AgentAdventureConfiguration.DescribeDifficulties();
        Assert.AreEqual(4, expected.Length); CollectionAssert.AreEquivalent(expected, offered.Select(d => d.id));
        Assert.AreEqual(1, offered.Count(d => d.isDefault));
        foreach (var d in offered)
        {
            var config = AgentAdventureConfiguration.ResolveDifficulty(d.id);
            Assert.AreEqual(config.BoardSize, d.boardSize); Assert.AreEqual(config.difficultyName, d.name);
            CollectionAssert.AreEqual(Enumerable.Range(0, 11).Select(t => RunBootstrapService.ResolveActionPointLimitForTier(config, t)), d.actionPointLimits);
        }
    }

    [TestCase(0)][TestCase(1)][TestCase(2)][TestCase(3)]
    public void EachMenuDifficultyCreatesItsRealBoardAndOmittedNextChoiceKeepsIt(int index)
    {
        var d = AgentAdventureConfiguration.SelectableDifficulties()[index];
        session = new AgentAdventureSession(new AgentAdventureRequest { difficultyId = Id(d), seed = 1234 }, () => 10000);
        var state = session.Observe(); Assert.AreEqual(Id(d), state.difficultyId); Assert.AreEqual(d.difficultyName, state.difficulty);
        Assert.AreEqual(d.BoardSize * d.BoardSize, state.tiles.Length); Assert.AreEqual(d.requiredKeys, state.requiredKeys);
        Assert.AreEqual(RunBootstrapService.ResolveActionPointLimitForTier(d, 0), state.maxActionPoints);
        Assert.AreEqual(Id(d), session.Replay().create.difficultyId);
        Act("run_abandon"); state = Act("run_next");
        Assert.AreEqual(Id(d), state.difficultyId); Assert.AreEqual(d.BoardSize, state.boardSize);
    }

    [Test] public void DefaultRemainsNormalAndModeSwitchSurvivesSaveRecoveryAndFullReplay()
    {
        session = new AgentAdventureSession(new AgentAdventureRequest { seed = 7 }, () => 10000);
        string normal = Id(AssetDatabase.LoadAssetAtPath<DifficultyConfig_SO>("Assets/Data/Game/NormalConfig.asset"));
        Assert.AreEqual(normal, session.Observe().difficultyId); Assert.AreEqual("Medium", session.Observe().difficulty);
        var different = AgentAdventureConfiguration.DescribeDifficulties().First(d => d.id != normal);
        Act("run_abandon"); var next = Request("run_next"); next.difficultyId = different.id;
        var state = session.Act(next).state; Assert.AreEqual(different.id, state.difficultyId);
        slot = "difficulty-test-" + Guid.NewGuid().ToString("N"); session.Save(slot); string hash = AgentAdventureReplayService.StateHash(state);
        session.Dispose(); session = AgentAdventureSaveService.Load(slot, () => 10000);
        Assert.AreEqual(hash, AgentAdventureReplayService.StateHash(session.Observe()));
        next.sessionId = session.Id; Assert.AreEqual(hash, AgentAdventureReplayService.StateHash(session.Act(next).state));
        Act("run_abandon"); var back = Request("run_next"); back.difficultyId = normal;
        state = session.Act(back).state; Assert.AreEqual(normal, state.difficultyId); Assert.AreEqual(3, state.runIndex);
        var replay = session.Replay(); session.Dispose(); session = null;
        var verified = AgentAdventureReplayService.Verify(replay); Assert.IsTrue(verified.verified);
        Assert.AreEqual(normal, verified.finalState.difficultyId); Assert.AreEqual(3, verified.finalState.runIndex);
    }

    [Test] public void InvalidNextModeRejectsBeforeMutatingOrFaultingAndRequestIdCanBeRetried()
    {
        session = new AgentAdventureSession(new AgentAdventureRequest { seed = 7 }); Act("run_abandon");
        string before = JsonUtility.ToJson(session.Observe()); var next = Request("run_next"); next.difficultyId = "Assets/Data/Game/NormalConfig.asset";
        Assert.Throws<ArgumentException>(() => session.Act(next)); Assert.AreEqual(before, JsonUtility.ToJson(session.Observe()));
        next.difficultyId = ""; Assert.AreEqual(2, session.Act(next).state.runIndex);
    }

    [Test] public void UnlistedDifficultyAssetCannotBeSelectedOrCreateAnIsolatedProfile()
    {
        var allowed = AgentAdventureConfiguration.SelectableDifficulties();
        string unlisted = AssetDatabase.FindAssets("t:DifficultyConfig_SO", new[] { "Assets/Data/Game" })
            .First(g => !allowed.Any(d => Id(d) == g));
        string profileId = "invalid-difficulty-" + Guid.NewGuid().ToString("N");
        Assert.Throws<ArgumentException>(() => new AgentAdventureSession(new AgentAdventureRequest { difficultyId = unlisted, profileId = profileId }));
        Assert.IsFalse(Directory.Exists(Path.Combine("Library", "AgentAdventure", "Profiles", profileId)));
    }

    [Test] public void DifficultyParameterOnMovementRejectsBeforeSpendingActionPoints()
    {
        session = new AgentAdventureSession(new AgentAdventureRequest { seed = 7 });
        string before = JsonUtility.ToJson(session.Observe()); var move = session.Observe().legalActions.First(a => a.op == "run_move");
        var request = Request(move.op); request.x = move.x; request.y = move.y;
        request.difficultyId = AgentAdventureConfiguration.DescribeDifficulties().First(d => !d.isDefault).id;
        Assert.Throws<ArgumentException>(() => session.Act(request)); Assert.AreEqual(before, JsonUtility.ToJson(session.Observe()));
    }

    [Test] public void CatalogueDuringAnAdventurePreservesObservationSceneRandomAndInput()
    {
        session = new AgentAdventureSession(new AgentAdventureRequest { seed = 7 });
        var active = SceneManager.GetActiveScene(); bool dirty = active.isDirty; int scenes = SceneManager.sceneCount;
        string before = JsonUtility.ToJson(session.Observe()), rng = JsonUtility.ToJson(UnityEngine.Random.state);
        SpeedButton.InputSuppressed = true; SpeedButton.CombatLocked = true;
        var catalogue = AgentBattleSession.Catalogue(); Assert.AreEqual(4, catalogue.difficulties.Length);
        Assert.AreEqual(before, JsonUtility.ToJson(session.Observe())); Assert.AreEqual(rng, JsonUtility.ToJson(UnityEngine.Random.state));
        Assert.AreEqual(active, SceneManager.GetActiveScene()); Assert.AreEqual(dirty, active.isDirty); Assert.AreEqual(scenes, SceneManager.sceneCount);
        Assert.IsTrue(SpeedButton.InputSuppressed); Assert.IsTrue(SpeedButton.CombatLocked);
    }
}
