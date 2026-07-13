using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class MapBattleTilePreviewControllerTests
{
    private GameObject root;
    private BoardManager board;
    private ActionPointSystem actionPoints;
    private PlayerMapController player;
    private MapViewController view;
    private MapBattleTilePreviewOverlay overlay;
    private MapBattleTilePreviewController controller;
    private RunState runState;
    private BoardTileData origin;
    private BoardTileData target;
    private Camera mapCamera;
    private int commitCount;

    [SetUp]
    public void SetUp()
    {
        root = new GameObject("BattleTilePreviewControllerTest");
        board = root.AddComponent<BoardManager>();
        actionPoints = root.AddComponent<ActionPointSystem>();
        player = root.AddComponent<PlayerMapController>();
        view = root.AddComponent<MapViewController>();
        overlay = new GameObject("PreviewOverlay").AddComponent<MapBattleTilePreviewOverlay>();
        overlay.transform.SetParent(root.transform, false);
        controller = root.AddComponent<MapBattleTilePreviewController>();
        mapCamera = new GameObject("MapCamera").AddComponent<Camera>();
        mapCamera.transform.SetParent(root.transform, false);

        origin = new BoardTileData
        {
            coord = Vector2Int.zero,
            tileType = MapTileType.Empty,
            isVisited = true
        };
        target = new BoardTileData
        {
            coord = Vector2Int.right,
            tileType = MapTileType.Battle,
            previewDangerRating = 4
        };
        board.LoadBoard(2, origin.coord, target.coord, origin.coord, new[] { origin, target });
        actionPoints.Initialize(5);
        runState = new RunState { playerGridPosition = origin.coord };
        player.Initialize(root.transform, board, actionPoints, null, runState);
        player.SetPosition(origin);
        view.Initialize(mapCamera, board, 2f);
        controller.Configure(view, player, board, overlay, CommitMove, _ => { });
    }

    [TearDown]
    public void TearDown()
    {
        UnityEngine.Object.DestroyImmediate(root);
    }

    [Test]
    public void BeginPreviewImmediate_ReachesReadyAfterPresentationArrives()
    {
        controller.BeginPreview(Context(), true);

        Assert.AreEqual(MapBattleTilePreviewState.Ready, controller.State);
        Assert.IsTrue(controller.IsActive);
        Assert.IsTrue(overlay.IsVisible);
        Assert.IsTrue(view.IsBattlePreviewActive);
        Assert.IsTrue(player.IsBattlePreviewActive);
        Assert.AreEqual(origin.coord, runState.playerGridPosition);
        Assert.AreEqual(5, actionPoints.CurrentPoints);
    }

    [Test]
    public void ConfirmTwice_CommitsExactlyOnce()
    {
        controller.BeginPreview(Context(), true);

        controller.Confirm();
        controller.Confirm();

        Assert.AreEqual(1, commitCount);
        Assert.AreEqual(MapBattleTilePreviewState.Committing, controller.State);
        Assert.IsTrue(controller.IsActive);
    }

    [Test]
    public void CancelImmediate_RestoresPresentationAndNeverCommits()
    {
        MapCameraPreviewState cameraBefore = view.CaptureCameraState();
        Vector3 pawnBefore = player.PawnWorldPosition;
        controller.BeginPreview(Context(), true);

        controller.CancelImmediate();

        Assert.AreEqual(0, commitCount);
        Assert.AreEqual(MapBattleTilePreviewState.Hidden, controller.State);
        Assert.IsFalse(controller.IsActive);
        Assert.IsFalse(overlay.IsVisible);
        Assert.IsFalse(view.IsBattlePreviewActive);
        Assert.IsFalse(player.IsBattlePreviewActive);
        Assert.AreEqual(pawnBefore, player.PawnWorldPosition);
        MapCameraPreviewState cameraAfter = view.CaptureCameraState();
        Assert.AreEqual(cameraBefore.orthographic, cameraAfter.orthographic);
        Assert.AreEqual(cameraBefore.position, cameraAfter.position);
        Assert.AreEqual(cameraBefore.rotation, cameraAfter.rotation);
    }

    [Test]
    public void FailedCommit_ReturnsToReadyAndShowsFailure()
    {
        controller.Configure(view, player, board, overlay, (_, __) => false, _ => { });
        controller.BeginPreview(Context(), true);

        controller.Confirm();

        Assert.AreEqual(MapBattleTilePreviewState.Ready, controller.State);
        Assert.IsTrue(controller.IsActive);
        Assert.IsTrue(overlay.IsVisible);
        Assert.That(AllOverlayText(), Does.Contain("未能完成"));
    }

    [TestCase(false, "行动点不足")]
    [TestCase(true, "Boss 未解锁")]
    public void DisabledContext_NeverInvokesCommit(bool boss, string reason)
    {
        MapBattleTilePreviewContext context = Context();
        context.tile.tileType = boss ? MapTileType.Boss : MapTileType.Battle;
        context.canConfirm = false;
        context.blockReason = reason;
        controller.BeginPreview(context, true);

        controller.Confirm();

        Assert.AreEqual(0, commitCount);
        Assert.AreEqual(MapBattleTilePreviewState.Ready, controller.State);
        Assert.That(AllOverlayText(), Does.Contain(reason));
    }

    [Test]
    public void CancelAnimated_RemainsActiveWhileExitingSoPauseCannotOpenSameFrame()
    {
        controller.BeginPreview(Context(), true);

        controller.Cancel(false);

        Assert.AreEqual(MapBattleTilePreviewState.Exiting, controller.State);
        Assert.IsTrue(controller.IsActive);
        Assert.IsFalse(overlay.IsVisible);
    }

    [Test]
    public void CancelDuringEntering_IsAcceptedAndRemainsActiveUntilRestoreFinishes()
    {
        controller.BeginPreview(Context(), false);
        Assert.AreEqual(MapBattleTilePreviewState.Entering, controller.State);

        controller.Cancel(false);

        Assert.AreEqual(MapBattleTilePreviewState.Exiting, controller.State);
        Assert.IsTrue(controller.IsActive);
    }

    [Test]
    public void RepeatedCoordinate_UsesShorterEntryDuration()
    {
        controller.BeginPreview(Context(), true);
        Assert.AreEqual(0.8f, GetPrivateField<float>(controller, "lastEnterDuration"), 0.001f);
        controller.CancelImmediate();

        controller.BeginPreview(Context(), true);

        Assert.AreEqual(0.3f, GetPrivateField<float>(controller, "lastEnterDuration"), 0.001f);
    }

    [Test]
    public void RunInParallel_CompletesInLongestRoutineDurationInsteadOfTheirSum()
    {
        System.Collections.IEnumerator combined = MapBattleTilePreviewController.RunInParallel(
            Frames(2),
            Frames(4));

        int yieldedFrames = 0;
        while (combined.MoveNext())
            yieldedFrames++;

        Assert.AreEqual(4, yieldedFrames);
    }

    [Test]
    public void RunGameManagerEscape_CancelsPreviewAndKeepsPauseBlockedDuringExit()
    {
        controller.BeginPreview(Context(), true);
        RunGameManager manager = CreateInactiveRunGameManager();

        bool handled = manager.TryHandleBattlePreviewEscape(true);

        Assert.IsTrue(handled);
        Assert.AreEqual(MapBattleTilePreviewState.Exiting, controller.State);
        Assert.IsFalse(InvokePrivate<bool>(manager, "CanOpenMapPauseMenu"));
    }

    [Test]
    public void RunGameManagerPause_CancelsPreviewImmediatelyBeforePersistence()
    {
        controller.BeginPreview(Context(), true);
        RunGameManager manager = CreateInactiveRunGameManager();

        InvokePrivate<object>(manager, "OnApplicationPause", true);

        Assert.AreEqual(MapBattleTilePreviewState.Hidden, controller.State);
        Assert.IsFalse(view.IsBattlePreviewActive);
        Assert.IsFalse(player.IsBattlePreviewActive);
    }

    private bool CommitMove(BoardTileData tile, int cost)
    {
        commitCount++;
        return true;
    }

    private MapBattleTilePreviewContext Context()
    {
        return new MapBattleTilePreviewContext
        {
            tile = target,
            moveCost = 2,
            dangerRating = 4,
            attributeNames = new[] { "Fire", "Dark" },
            canPreview = true,
            canConfirm = true,
            confirmLabel = "进入战斗 · 预计 2 AP"
        };
    }

    private string AllOverlayText()
    {
        TMPro.TextMeshProUGUI[] labels = root.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true);
        return string.Join("|", Array.ConvertAll(labels, label => label.text));
    }

    private static T GetPrivateField<T>(object instance, string name)
    {
        FieldInfo field = instance.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field, $"Missing private field {name}.");
        return (T)field.GetValue(instance);
    }

    private RunGameManager CreateInactiveRunGameManager()
    {
        GameObject managerObject = new GameObject("InactiveRunGameManager");
        managerObject.SetActive(false);
        managerObject.transform.SetParent(root.transform, false);
        RunGameManager manager = managerObject.AddComponent<RunGameManager>();
        SetPrivateField(manager, "mapBattleTilePreviewController", controller);
        SetPrivateField(manager, "mapBattleTilePreviewOverlay", overlay);
        SetPrivateField(manager, "runState", runState);
        return manager;
    }

    private static void SetPrivateField(object instance, string name, object value)
    {
        FieldInfo field = instance.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field, $"Missing private field {name}.");
        field.SetValue(instance, value);
    }

    private static T InvokePrivate<T>(object instance, string name, params object[] arguments)
    {
        MethodInfo method = instance.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(method, $"Missing private method {name}.");
        object result = method.Invoke(instance, arguments);
        return result == null ? default : (T)result;
    }

    private static System.Collections.IEnumerator Frames(int count)
    {
        for (int i = 0; i < count; i++)
            yield return null;
    }
}
