using System;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public class DiagonalMapMovementPersistenceTests
{
    private Relic_SO boots;
    private MainMenuSaveAssetCatalog catalog;
    private MapRunSnapshot previousSnapshot;
    private static readonly FieldInfo SnapshotField = typeof(MapRunRuntime).GetField("currentSnapshot", BindingFlags.Static | BindingFlags.NonPublic);

    [SetUp]
    public void SetUp()
    {
        previousSnapshot = (MapRunSnapshot)SnapshotField.GetValue(null);
        boots = AssetDatabase.LoadAssetAtPath<Relic_SO>("Assets/Data/Relics/152 神行靴/神行靴.asset");
        Assert.NotNull(boots);
        catalog = new MainMenuSaveAssetCatalog();
        catalog.Add(boots);
    }

    [TearDown]
    public void TearDown()
    {
        SnapshotField.SetValue(null, previousSnapshot);
    }

    [TestCase(true)]
    [TestCase(false)]
    public void BattleReturn_PreservesOrRecoversDiagonalMovement(bool hadUnlock)
    {
        var run = new RunState();
        run.AddRelic(boots);
        run.allowDiagonalMapMovement = hadUnlock;
        MapRunRuntime.RestoreSnapshot(new MapRunSnapshot { sceneName = "Map", runState = run });
        MapRunRuntime.ApplyEncounterResult(new BattleEncounterData { wasVictory = true });

        Assert.IsTrue(MapRunRuntime.TryGetSnapshot("Map", out MapRunSnapshot restored));
        Assert.IsTrue(restored.runState.allowDiagonalMapMovement);
        Assert.AreEqual(1, restored.runState.GetRelicAmount(boots));
        AssertDiagonalAdjacency(restored.runState);
    }

    [Test]
    public void SaveRoundTrip_PreservesUnlockAfterBootsAreConsumed()
    {
        var run = new RunState();
        run.AddRelic(boots);
        Assert.IsTrue(run.TryConsumeRelic(boots, 1));

        RunState restored = Deserialize(Serialize(run));

        Assert.IsTrue(restored.allowDiagonalMapMovement);
        Assert.AreEqual(0, restored.GetRelicAmount(boots));
    }

    [Test]
    public void LegacySave_WithBootsRecoversUnlockWithoutReplayingAcquisitionRewards()
    {
        var run = new RunState { money = 23 };
        run.AddRelic(boots);
        string legacy = Serialize(run).Replace("\"allowDiagonalMapMovement\":true,", "");
        Assert.IsFalse(legacy.Contains("allowDiagonalMapMovement"));

        RunState restored = Deserialize(legacy);
        RelicSystem.RestoreDiagonalMapMovement(restored);

        Assert.IsTrue(restored.allowDiagonalMapMovement);
        Assert.AreEqual(23, restored.money);
        Assert.AreEqual(1, restored.GetRelicAmount(boots));
        Assert.AreEqual(0, restored.ItemAcquisitions.PendingCount);
    }

    [TestCase(0)]
    [TestCase(1)]
    public void SnapshotAndSave_WithoutAnEnabledBootsEffectKeepCardinalMovement(int owned)
    {
        var relic = UnityEngine.Object.Instantiate(boots);
        try
        {
            relic.builtInEffects[0].chance = 0f;
            var run = new RunState();
            run.relicInventory.Add(new RunRelicState { relicData = relic, amount = owned });
            RelicSystem.RestoreDiagonalMapMovement(run);
            Assert.IsFalse(run.allowDiagonalMapMovement);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(relic);
        }

        Assert.IsFalse(Deserialize(Serialize(new RunState())).allowDiagonalMapMovement);
    }

    [Test]
    public void LegacySave_ZeroBootsDoesNotUnlockDiagonalMovement()
    {
        RunState restored = Deserialize("{\"relicInventory\":[{\"relicId\":\"152\",\"amount\":0}]}");
        Assert.IsFalse(restored.allowDiagonalMapMovement);
    }

    private static string Serialize(RunState state)
    {
        MethodInfo method = typeof(MainMenuRunSaveSystem).GetMethod("SerializeRunState", BindingFlags.Static | BindingFlags.NonPublic);
        return JsonUtility.ToJson(method.Invoke(null, new object[] { state }));
    }

    private RunState Deserialize(string json)
    {
        Type type = typeof(MainMenuRunSaveSystem).Assembly.GetType("SavedRunState");
        object saved = JsonUtility.FromJson(json, type);
        MethodInfo method = typeof(MainMenuRunSaveSystem).GetMethod("DeserializeRunState", BindingFlags.Static | BindingFlags.NonPublic);
        return (RunState)method.Invoke(null, new[] { saved, catalog });
    }

    private static void AssertDiagonalAdjacency(RunState state)
    {
        var root = new GameObject("RestoredBootsBoard");
        try
        {
            var board = root.AddComponent<BoardManager>();
            Assert.IsTrue(board.IsAdjacent(Vector2Int.zero, Vector2Int.one, state.allowDiagonalMapMovement));
            Assert.IsFalse(board.IsAdjacent(Vector2Int.zero, Vector2Int.one * 2, state.allowDiagonalMapMovement));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }
}
