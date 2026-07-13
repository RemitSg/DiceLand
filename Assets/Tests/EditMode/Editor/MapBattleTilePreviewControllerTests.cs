using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;

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
    private int[] preExistingEventSystemIds;

    [SetUp]
    public void SetUp()
    {
        commitCount = 0;
        preExistingEventSystemIds = UnityEngine.Object
            .FindObjectsByType<EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Select(system => system.GetInstanceID())
            .ToArray();
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
        EventSystem[] eventSystems = UnityEngine.Object.FindObjectsByType<EventSystem>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
        for (int i = 0; i < eventSystems.Length; i++)
        {
            EventSystem system = eventSystems[i];
            if (system == null || preExistingEventSystemIds.Contains(system.GetInstanceID()))
                continue;
            UnityEngine.Object.DestroyImmediate(system.gameObject);
        }
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
        MapBattleTilePreviewState stateDuringCommit = MapBattleTilePreviewState.Hidden;
        controller.Configure(view, player, board, overlay, (_, __) =>
        {
            stateDuringCommit = controller.State;
            commitCount++;
            return MapBattleTilePreviewCommitResult.BattleHandoffStarted;
        }, _ => { });
        controller.BeginPreview(Context(), true);

        controller.Confirm();
        controller.Confirm();

        Assert.AreEqual(1, commitCount);
        Assert.AreEqual(MapBattleTilePreviewState.Committing, stateDuringCommit);
        Assert.AreEqual(MapBattleTilePreviewState.Hidden, controller.State);
        Assert.IsFalse(controller.IsActive);
        Assert.IsFalse(overlay.IsVisible);
        Assert.IsTrue(view.IsBattlePreviewActive);
    }

    [TestCase(false, "行动点不足")]
    [TestCase(true, "Boss 未解锁")]
    public void Confirm_RevalidatesDynamicBlockBeforeCommitting(bool boss, string reason)
    {
        MapBattleTilePreviewContext refreshed = Context();
        refreshed.tile.tileType = boss ? MapTileType.Boss : MapTileType.Battle;
        refreshed.canConfirm = false;
        refreshed.blockReason = reason;
        controller.Configure(
            view,
            player,
            board,
            overlay,
            CommitMove,
            _ => { },
            _ => refreshed);
        controller.BeginPreview(Context(refreshed.tile), true);

        controller.Confirm();

        Assert.AreEqual(0, commitCount);
        Assert.AreEqual(MapBattleTilePreviewState.Ready, controller.State);
        Assert.IsTrue(overlay.IsVisible);
        Assert.That(AllOverlayText(), Does.Contain(reason));
        Assert.IsFalse(FindConfirmButton().interactable);
    }

    [Test]
    public void Confirm_InvalidatedTileCancelsPreviewWithoutCommitting()
    {
        string status = string.Empty;
        controller.Configure(
            view,
            player,
            board,
            overlay,
            CommitMove,
            message => status = message,
            _ => null);
        controller.BeginPreview(Context(), true);

        controller.Confirm();

        Assert.AreEqual(0, commitCount);
        Assert.AreEqual(MapBattleTilePreviewState.Hidden, controller.State);
        Assert.IsFalse(overlay.IsVisible);
        Assert.IsFalse(view.IsBattlePreviewActive);
        Assert.IsFalse(player.IsBattlePreviewActive);
        Assert.That(status, Does.Contain("失效"));
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
        controller.Configure(
            view,
            player,
            board,
            overlay,
            (_, __) => MapBattleTilePreviewCommitResult.NotCommitted,
            _ => { });
        controller.BeginPreview(Context(), true);

        controller.Confirm();

        Assert.AreEqual(MapBattleTilePreviewState.Ready, controller.State);
        Assert.IsTrue(controller.IsActive);
        Assert.IsTrue(overlay.IsVisible);
        Assert.That(AllOverlayText(), Does.Contain("未能完成"));
    }

    [Test]
    public void MovementCommittedWithoutHandoff_CleansPreviewAndCannotCommitAgain()
    {
        string status = string.Empty;
        controller.Configure(view, player, board, overlay, (_, __) =>
        {
            commitCount++;
            return MapBattleTilePreviewCommitResult.MovementCommittedWithoutHandoff;
        }, message => status = message);
        controller.BeginPreview(Context(), true);

        controller.Confirm();
        controller.Confirm();

        Assert.AreEqual(1, commitCount);
        Assert.AreEqual(MapBattleTilePreviewState.Hidden, controller.State);
        Assert.IsFalse(controller.IsActive);
        Assert.IsFalse(view.IsBattlePreviewActive);
        Assert.IsFalse(player.IsBattlePreviewActive);
        Assert.AreEqual("已进入战斗格，但战斗未能启动，请再次点击重试", status);
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
    public void EveryPreview_UsesUnifiedFastEntryDuration()
    {
        controller.BeginPreview(Context(), true);
        Assert.AreEqual(0.18f, GetPrivateField<float>(controller, "lastEnterDuration"), 0.001f);
        controller.CancelImmediate();

        controller.BeginPreview(Context(), true);

        Assert.AreEqual(0.18f, GetPrivateField<float>(controller, "lastEnterDuration"), 0.001f);
    }

    [Test]
    public void FailedEntry_AllowsNextPreviewAttempt()
    {
        SetPrivateField(player, "isMoving", true);
        Assert.IsFalse(controller.BeginPreview(Context(), true));
        SetPrivateField(player, "isMoving", false);

        Assert.IsTrue(controller.BeginPreview(Context(), true));

        Assert.AreEqual(MapBattleTilePreviewState.Ready, controller.State);
    }

    [Test]
    public void RunFrameOnlyParallel_CompletesInLongestRoutineDurationInsteadOfTheirSum()
    {
        System.Collections.IEnumerator combined = InvokePrivateStatic<System.Collections.IEnumerator>(
            typeof(MapBattleTilePreviewController),
            "RunFrameOnlyParallel",
            Frames(2),
            Frames(4));

        int yieldedFrames = 0;
        while (combined.MoveNext())
            yieldedFrames++;

        Assert.AreEqual(4, yieldedFrames);
    }

    [Test]
    public void RunFrameOnlyParallel_NonNullYieldFailsLoudlyAndDisposesBothRoutines()
    {
        TrackedEnumerator first = new TrackedEnumerator(new WaitForSeconds(0.1f));
        TrackedEnumerator second = new TrackedEnumerator(null);
        System.Collections.IEnumerator combined = InvokePrivateStatic<System.Collections.IEnumerator>(
            typeof(MapBattleTilePreviewController),
            "RunFrameOnlyParallel",
            first,
            second);

        Assert.Throws<InvalidOperationException>(() => combined.MoveNext());
        Assert.IsTrue(first.disposed);
        Assert.IsTrue(second.disposed);
    }

    [Test]
    public void RunGameManagerCommitWrapper_ClassifiesMovementAndHandoffOutcomes()
    {
        RunGameManager manager = CreateInactiveRunGameManager();
        SetPrivateField(manager, "boardManager", board);
        SetPrivateField(manager, "actionPointSystem", actionPoints);
        SetPrivateField(manager, "playerMapController", player);
        Assert.IsTrue(player.BeginBattleTilePreview(target, true));

        MapBattleTilePreviewCommitResult movedWithoutHandoff = InvokePrivate<MapBattleTilePreviewCommitResult>(
            manager,
            "TryCommitBattlePreviewMove",
            target,
            2);
        Assert.AreEqual(MapBattleTilePreviewCommitResult.MovementCommittedWithoutHandoff, movedWithoutHandoff);
        Assert.AreEqual(3, actionPoints.CurrentPoints);

        player.SetPosition(origin);
        Assert.IsTrue(player.BeginBattleTilePreview(target, true));
        player.PlayerMovedToTile += (_, __) =>
            SetPrivateField(manager, "battlePreviewBattleEntryAccepted", true);
        MapBattleTilePreviewCommitResult handoffStarted = InvokePrivate<MapBattleTilePreviewCommitResult>(
            manager,
            "TryCommitBattlePreviewMove",
            target,
            0);
        Assert.AreEqual(MapBattleTilePreviewCommitResult.BattleHandoffStarted, handoffStarted);
        Assert.AreEqual(3, actionPoints.CurrentPoints);

        BoardTileData nonAdjacent = new BoardTileData
        {
            coord = new Vector2Int(9, 9),
            tileType = MapTileType.Battle
        };
        MapBattleTilePreviewCommitResult notCommitted = InvokePrivate<MapBattleTilePreviewCommitResult>(
            manager,
            "TryCommitBattlePreviewMove",
            nonAdjacent,
            0);
        Assert.AreEqual(MapBattleTilePreviewCommitResult.NotCommitted, notCommitted);
    }

    [Test]
    public void RunGameManagerRefreshPreviewContext_UsesCurrentActionPointsAndBossUnlockState()
    {
        RunGameManager manager = CreateInactiveRunGameManager();
        SetPrivateField(manager, "boardManager", board);
        SetPrivateField(manager, "actionPointSystem", actionPoints);
        Assert.IsTrue(board.TryGetTile(target.coord, out BoardTileData currentTarget));

        Assert.IsTrue(actionPoints.TrySpend(5));
        MapBattleTilePreviewContext noActionPoints = InvokePrivate<MapBattleTilePreviewContext>(
            manager,
            "RefreshBattlePreviewContext",
            Context(currentTarget));
        Assert.IsFalse(noActionPoints.canConfirm);
        Assert.That(noActionPoints.blockReason, Does.Contain("行动点"));

        actionPoints.RestoreState(5, 0, 5);
        currentTarget.tileType = MapTileType.Boss;
        runState.currentKeys = 1;
        runState.requiredKeys = 3;
        MapBattleTilePreviewContext lockedBoss = InvokePrivate<MapBattleTilePreviewContext>(
            manager,
            "RefreshBattlePreviewContext",
            Context(currentTarget));
        Assert.IsFalse(lockedBoss.canConfirm);
        Assert.That(lockedBoss.blockReason, Does.Contain("1/3"));
    }

    [Test]
    public void RunGameManagerRefreshPreviewContext_RejectsStaleTileInstance()
    {
        RunGameManager manager = CreateInactiveRunGameManager();
        SetPrivateField(manager, "boardManager", board);
        SetPrivateField(manager, "actionPointSystem", actionPoints);
        Assert.IsTrue(board.TryGetTile(target.coord, out BoardTileData currentTarget));
        MapBattleTilePreviewContext staleContext = Context(currentTarget);
        BoardTileData replacement = new BoardTileData
        {
            coord = target.coord,
            tileType = MapTileType.Battle,
            previewDangerRating = target.previewDangerRating
        };
        Dictionary<Vector2Int, BoardTileData> tiles = GetPrivateField<Dictionary<Vector2Int, BoardTileData>>(board, "tiles");
        tiles[currentTarget.coord] = replacement;

        MapBattleTilePreviewContext refreshed = InvokePrivate<MapBattleTilePreviewContext>(
            manager,
            "RefreshBattlePreviewContext",
            staleContext);

        Assert.IsNull(refreshed);
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

    [Test]
    public void SceneTransitionInProgress_BlocksMapInputAndPauseAfterPreviewHandsOff()
    {
        RunGameManager manager = CreateInactiveRunGameManager();
        GameObject transitionObject = new GameObject("TransitionInProgress");
        transitionObject.SetActive(false);
        transitionObject.transform.SetParent(root.transform, false);
        SceneCurtainTransitionController transition = transitionObject.AddComponent<SceneCurtainTransitionController>();
        SetPrivateField(transition, "isTransitioning", true);
        SetPrivateField(manager, "sceneTransitionController", transition);

        Assert.IsTrue(InvokePrivate<bool>(manager, "IsMapViewInputBlocked"));
        Assert.IsFalse(InvokePrivate<bool>(manager, "CanOpenMapPauseMenu"));
    }

    private MapBattleTilePreviewCommitResult CommitMove(BoardTileData tile, int cost)
    {
        commitCount++;
        return MapBattleTilePreviewCommitResult.BattleHandoffStarted;
    }

    private MapBattleTilePreviewContext Context(BoardTileData tile = null)
    {
        return new MapBattleTilePreviewContext
        {
            tile = tile ?? target,
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

    private UnityEngine.UI.Button FindConfirmButton()
    {
        return root.GetComponentsInChildren<UnityEngine.UI.Button>(true)
            .Single(button => button.name == "ConfirmButton");
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

    private static T InvokePrivateStatic<T>(Type type, string name, params object[] arguments)
    {
        MethodInfo method = type.GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic);
        Assert.IsNotNull(method, $"Missing private static method {name}.");
        object result = method.Invoke(null, arguments);
        return result == null ? default : (T)result;
    }

    private static System.Collections.IEnumerator Frames(int count)
    {
        for (int i = 0; i < count; i++)
            yield return null;
    }

    private sealed class TrackedEnumerator : System.Collections.IEnumerator, IDisposable
    {
        private readonly object value;
        private bool moved;
        public bool disposed;

        public TrackedEnumerator(object value)
        {
            this.value = value;
        }

        public object Current => value;

        public bool MoveNext()
        {
            if (moved)
                return false;
            moved = true;
            return true;
        }

        public void Reset()
        {
            moved = false;
        }

        public void Dispose()
        {
            disposed = true;
        }
    }
}
