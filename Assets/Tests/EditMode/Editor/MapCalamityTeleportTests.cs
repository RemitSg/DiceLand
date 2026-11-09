using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

public sealed class MapCalamityTeleportTests
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private GameObject root;
    private BoardManager board;
    private ActionPointSystem points;
    private PlayerMapController player;
    private MapViewController view;
    private RunGameManager manager;
    private Camera camera;
    private RunState state;
    private BoardTileData origin, destination, neighbor;
    private Calamity_SO calamity;
    private Random.State outsideRandom;

    [SetUp]
    public void SetUp()
    {
        outsideRandom = Random.state;
        root = new GameObject("Calamity teleport regression");
        board = root.AddComponent<BoardManager>(); board.ConfigureHeadlessExecution();
        points = root.AddComponent<ActionPointSystem>(); points.Initialize(6);
        origin = new BoardTileData { coord = Vector2Int.zero, isSpawnTile = true, isVisited = true, tileType = MapTileType.Empty };
        destination = new BoardTileData { coord = new Vector2Int(4, 3), dangerBand = MapDangerBand.Outer, tileType = MapTileType.Event };
        neighbor = new BoardTileData { coord = new Vector2Int(3, 3), isVisited = true, tileType = MapTileType.Empty };
        board.LoadBoard(5, origin.coord, new Vector2Int(2, 2), origin.coord, new[] { origin, destination, neighbor });
        board.TryGetTile(origin.coord, out origin);
        board.TryGetTile(destination.coord, out destination);
        board.TryGetTile(neighbor.coord, out neighbor);
        state = new RunState { playerGridPosition = origin.coord };
        player = root.AddComponent<PlayerMapController>();
        player.Initialize(root.transform, board, points, null, state); player.SetPosition(origin);
        camera = new GameObject("Map camera", typeof(Camera)).GetComponent<Camera>(); camera.transform.SetParent(root.transform);
        camera.aspect = 16f / 9;
        view = root.AddComponent<MapViewController>(); view.Initialize(camera, board, 1);
        manager = root.AddComponent<RunGameManager>();
        Set(manager, "runState", state); Set(manager, "boardManager", board);
        Set(manager, "playerMapController", player); Set(manager, "mapViewController", view);
        calamity = AssetDatabase.LoadAssetAtPath<Calamity_SO>("Assets/Data/Calamity/N1/DimensionalDisorder.asset");
        Assert.IsNotNull(calamity);
    }

    [TearDown]
    public void TearDown() { Object.DestroyImmediate(root); Random.state = outsideRandom; }

    [Test]
    public void ProductionCalamityCallbackMovesPawnToTheLogicalDestinationWithoutArrivalRewards()
    {
        int arrivals = 0; player.PlayerMovedToTile += (_, __) => arrivals++;
        var feedback = (List<string>)typeof(RunGameManager).GetMethod("ApplyChosenCalamityBuiltInMapEffects", Private).Invoke(manager, new object[] { calamity });
        Assert.AreEqual(destination.coord, state.playerGridPosition);
        Assert.AreEqual(destination.coord, board.SelectedCoord);
        Assert.That(Vector3.Distance(World(destination), player.PawnWorldPosition), Is.LessThan(.001f));
        Assert.AreEqual(6, points.CurrentPoints); Assert.AreEqual(0, arrivals);
        Assert.IsFalse(destination.isVisited); Assert.IsFalse(destination.isResolved);
        StringAssert.Contains("[4,3]", feedback[0]);
    }

    [Test]
    public void CurrentUnexploredTileIsNotATeleportDestinationAndNoCandidateLeavesPositionUnchanged()
    {
        origin.isSpawnTile = false; origin.isVisited = false;
        BoardTileData selected = null;
        CalamitySelectionService.ApplyBuiltInMapEffects(calamity, state, board, null, tile => selected = tile);
        Assert.AreSame(destination, selected);
        destination.isVisited = true;
        state.playerGridPosition = origin.coord;
        selected = null;
        List<string> feedback = CalamitySelectionService.ApplyBuiltInMapEffects(calamity, state, board, null, tile => selected = tile);
        Assert.IsNull(selected); Assert.AreEqual(origin.coord, state.playerGridPosition);
        Assert.That(feedback, Has.Count.EqualTo(1));
        StringAssert.Contains("没有可传送", feedback[0]);
    }

    [Test]
    public void AnimationDisappearsThenRelocatesAndRestoresTheExactPoseBeforeUnblockingInput()
    {
        Transform visual = (Transform)Get(player, "visualRoot");
        visual.localPosition = new Vector3(.03f, .07f, 0); visual.localScale = new Vector3(.9f, 1.1f, 1);
        visual.localRotation = Quaternion.Euler(0, 0, 4);
        Vector3 position = visual.localPosition, scale = visual.localScale;
        Quaternion rotation = visual.localRotation;
        int arrivals = 0, completions = 0;
        player.PlayerMovedToTile += (_, __) => arrivals++;
        MapTeleportEffect effect = player.TeleportTo(destination, view);
        Assert.IsNotNull(effect); Assert.IsTrue(player.IsTeleporting);
        Assert.IsTrue((bool)typeof(RunGameManager).GetMethod("IsMapViewInputBlocked", Private).Invoke(manager, null));
        Assert.IsFalse(player.CanMoveTo(neighbor));
        effect.Advance(.38f);
        Assert.IsFalse(effect.HasRelocated); Assert.AreEqual(World(origin), player.PawnWorldPosition);
        Assert.IsTrue(effect.HasVisibleGeometry); Assert.Greater(effect.ParticleCount, 0);
        effect.Advance(.3f);
        Assert.IsTrue(effect.HasRelocated); Assert.AreEqual(World(destination), player.PawnWorldPosition);
        effect.Completed += () => { completions++; Assert.IsFalse(player.IsTeleporting); Assert.IsTrue(player.CanMoveTo(neighbor)); };
        effect.Advance(MapTeleportEffect.Duration);
        effect.Advance(1);
        Assert.IsTrue(effect.IsComplete); Assert.AreEqual(1, completions); Assert.AreEqual(0, arrivals);
        Assert.AreEqual(position, visual.localPosition); Assert.AreEqual(scale, visual.localScale); Assert.AreEqual(rotation, visual.localRotation);
        Assert.IsFalse(effect.HasVisibleGeometry); Assert.AreEqual(6, points.CurrentPoints);
        Assert.IsFalse(destination.isVisited); Assert.IsFalse(destination.isResolved);
        foreach (SpriteRenderer sprite in visual.GetComponentsInChildren<SpriteRenderer>()) Assert.Greater(sprite.color.a, .01f);
    }

    [Test]
    public void RepositioningDuringTeleportCancelsStalePresentationAndRestoresVisibility()
    {
        MapTeleportEffect old = player.TeleportTo(destination, view);
        old.Advance(.4f);
        player.SetPosition(origin);
        old.Advance(5);
        Assert.IsFalse(player.IsTeleporting); Assert.AreEqual(origin.coord, state.playerGridPosition);
        Assert.AreEqual(World(origin), player.PawnWorldPosition);
        Assert.AreEqual(Vector3.one, ((Transform)Get(player, "visualRoot")).localScale);
    }

    [Test]
    public void HeadlessTeleportSynchronizesPositionImmediatelyWithoutCreatingVisualEffects()
    {
        player.ConfigureHeadlessExecution();
        Assert.IsNull(player.TeleportTo(destination, view));
        Assert.AreEqual(World(destination), player.PawnWorldPosition); Assert.AreEqual(destination.coord, state.playerGridPosition);
        Assert.IsFalse(player.IsTeleporting); Assert.IsEmpty(root.GetComponentsInChildren<MapTeleportEffect>(true));
        Assert.AreEqual(6, points.CurrentPoints);
    }

    [Test]
    public void EdgeDestinationCanReachCameraCenterWithoutChangingZoomAndUsesExplicitFrameTime()
    {
        camera.orthographicSize = 2.4f; Set(view, "targetZoomSize", 2.4f);
        view.FocusOnPosition(World(destination));
        for (int i = 0; i < 90; i++) view.AdvanceCameraSmoothing(1f / 60);
        Vector3 viewport = camera.WorldToViewportPoint(World(destination));
        Assert.AreEqual(.5f, viewport.x, .005f); Assert.AreEqual(.5f, viewport.y, .005f);
        Assert.AreEqual(2.4f, camera.orthographicSize, .001f);
    }

    [Test]
    public void SavingDuringTheVisualTransitionPreservesTheDestinationWhenRestored()
    {
        player.TeleportTo(destination, view).Advance(.2f);
        Assert.AreEqual(World(origin), player.PawnWorldPosition);
        MethodInfo serialize = typeof(MainMenuRunSaveSystem).GetMethod("SerializeRunState", BindingFlags.Static | BindingFlags.NonPublic);
        string json = JsonUtility.ToJson(serialize.Invoke(null, new object[] { state }));
        object saved = JsonUtility.FromJson(json, typeof(MainMenuRunSaveSystem).Assembly.GetType("SavedRunState"));
        MethodInfo deserialize = typeof(MainMenuRunSaveSystem).GetMethod("DeserializeRunState", BindingFlags.Static | BindingFlags.NonPublic);
        RunState restored = (RunState)deserialize.Invoke(null, new object[] { saved, new MainMenuSaveAssetCatalog() });
        Assert.AreEqual(destination.coord, restored.playerGridPosition);
        player.Initialize(root.transform, board, points, null, restored); player.SetPosition(destination);
        Assert.AreEqual(World(destination), player.PawnWorldPosition); Assert.IsFalse(player.IsTeleporting);
    }

    [Test]
    public void PresentationDoesNotConsumeGameRandomnessAndCompletesWhileTimeScaleIsZero()
    {
        Random.InitState(25179); Random.State before = Random.state;
        float previousScale = Time.timeScale;
        try
        {
            Time.timeScale = 0;
            MapTeleportEffect effect = player.TeleportTo(destination, view);
            for (int i = 0; i < 120; i++) effect.Advance(1f / 60);
            Assert.IsTrue(effect.IsComplete); Assert.AreEqual(World(destination), player.PawnWorldPosition);
            Assert.AreEqual(before, Random.state);
        }
        finally { Time.timeScale = previousScale; }
    }

    private Vector3 World(BoardTileData tile) => board.GridToWorld(tile.coord) + new Vector3(0, 0, -.2f);
    private static object Get(object owner, string field) => owner.GetType().GetField(field, Private).GetValue(owner);
    private static void Set(object owner, string field, object value) => owner.GetType().GetField(field, Private).SetValue(owner, value);
}
