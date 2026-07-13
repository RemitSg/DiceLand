using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class MapBattleTilePreviewPawnTests
{
    private GameObject root;
    private BoardManager board;
    private ActionPointSystem points;
    private PlayerMapController controller;
    private RunState runState;
    private BoardTileData origin;
    private BoardTileData target;
    private BoardTileData otherTarget;
    private Transform pawn;
    private Transform visualRoot;

    [SetUp]
    public void SetUp()
    {
        root = new GameObject("BattlePreviewPawnTest");
        board = root.AddComponent<BoardManager>();
        points = root.AddComponent<ActionPointSystem>();
        controller = root.AddComponent<PlayerMapController>();

        origin = new BoardTileData { coord = new Vector2Int(0, 0), tileType = MapTileType.Empty };
        target = new BoardTileData { coord = new Vector2Int(1, 0), tileType = MapTileType.Battle };
        otherTarget = new BoardTileData { coord = new Vector2Int(0, 1), tileType = MapTileType.Battle };
        board.LoadBoard(2, origin.coord, target.coord, origin.coord, new[] { origin, target, otherTarget });
        points.Initialize(5);
        runState = new RunState { playerGridPosition = origin.coord };
        controller.Initialize(root.transform, board, points, null, runState);
        controller.SetPosition(origin);

        pawn = GetPrivateField<Transform>(controller, "playerPawn");
        visualRoot = GetPrivateField<Transform>(controller, "visualRoot");
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(root);
    }

    [Test]
    public void ImmediatePreview_MovesPresentationWithoutChangingCoordinateOrActionPoints()
    {
        controller.BeginBattleTilePreview(target, true);

        Assert.IsTrue(controller.IsBattlePreviewActive);
        Assert.AreEqual(World(target), controller.PawnWorldPosition);
        Assert.AreEqual(origin.coord, runState.playerGridPosition);
        Assert.AreEqual(5, points.CurrentPoints);
    }

    [Test]
    public void Cancel_RestoresExactPawnAndVisualRootTransforms()
    {
        Vector3 pawnPosition = new Vector3(2.25f, -3.5f, -0.37f);
        Quaternion pawnRotation = Quaternion.Euler(11f, 23f, 37f);
        Vector3 pawnScale = new Vector3(1.2f, 0.8f, 1.1f);
        Vector3 visualPosition = new Vector3(0.1f, 0.2f, 0.3f);
        Quaternion visualRotation = Quaternion.Euler(3f, 5f, 7f);
        Vector3 visualScale = new Vector3(0.7f, 1.3f, 0.9f);
        pawn.SetPositionAndRotation(pawnPosition, pawnRotation);
        pawn.localScale = pawnScale;
        visualRoot.SetLocalPositionAndRotation(visualPosition, visualRotation);
        visualRoot.localScale = visualScale;

        controller.BeginBattleTilePreview(target, true);
        controller.CancelBattleTilePreview(true);

        Assert.IsFalse(controller.IsBattlePreviewActive);
        Assert.AreEqual(pawnPosition, pawn.position);
        Assert.AreEqual(pawnRotation, pawn.rotation);
        Assert.AreEqual(pawnScale, pawn.localScale);
        Assert.AreEqual(visualPosition, visualRoot.localPosition);
        Assert.AreEqual(visualRotation, visualRoot.localRotation);
        Assert.AreEqual(visualScale, visualRoot.localScale);
    }

    [Test]
    public void RepeatedPreview_PreservesFirstOriginalPose()
    {
        Vector3 original = pawn.position;

        controller.BeginBattleTilePreview(target, true);
        controller.BeginBattleTilePreview(otherTarget, true);
        controller.CancelBattleTilePreview(true);

        Assert.AreEqual(original, pawn.position);
    }

    [Test]
    public void MatchingCommit_SpendsOnceAndDefersCallbackWithoutLandingReplay()
    {
        int callbackCount = 0;
        GameObject landingPrefab = new GameObject("LandingEffectPrefab");
        landingPrefab.transform.SetParent(root.transform, false);
        SetPrivateField(controller, "landingEffectPrefab", landingPrefab);
        int childCountBeforeCommit = root.transform.childCount;
        controller.PlayerMovedToTile += (_, __) => callbackCount++;
        controller.BeginBattleTilePreview(target, true);

        bool moved = controller.TryMoveTo(target, 2);

        Assert.IsTrue(moved);
        Assert.AreEqual(target.coord, runState.playerGridPosition);
        Assert.AreEqual(3, points.CurrentPoints);
        Assert.AreEqual(World(target), pawn.position);
        Assert.IsFalse(controller.IsBattlePreviewActive);
        Assert.AreEqual(childCountBeforeCommit, root.transform.childCount);
        Assert.AreEqual(0, callbackCount, "Preview commit callback must not run before TryMoveTo returns.");
    }

    [Test]
    public void FailedSpend_KeepsPreviewAtTarget()
    {
        points.RestoreState(1, 0, 1);
        controller.BeginBattleTilePreview(target, true);

        Assert.IsFalse(controller.TryMoveTo(target, 2));

        Assert.IsTrue(controller.IsBattlePreviewActive);
        Assert.AreEqual(World(target), pawn.position);
        Assert.AreEqual(origin.coord, runState.playerGridPosition);
        Assert.AreEqual(1, points.CurrentPoints);
    }

    [Test]
    public void MismatchedMove_RestoresPreviewOriginBeforeNormalMove()
    {
        controller.BeginBattleTilePreview(target, true);

        Assert.IsTrue(controller.TryMoveTo(otherTarget, 0));

        Assert.IsFalse(controller.IsBattlePreviewActive);
        Assert.AreEqual(otherTarget.coord, runState.playerGridPosition);
        Assert.AreEqual(World(origin), pawn.position, "Normal hop must begin from the logical origin.");
    }

    [Test]
    public void PreviewDuringNormalMovement_IsRejectedWithoutCapturingOrMutatingPresentation()
    {
        Assert.IsTrue(controller.TryMoveTo(otherTarget, 0));
        Vector3 movementPosition = pawn.position;

        bool beganPreview = controller.BeginBattleTilePreview(target, true);

        Assert.IsFalse(beganPreview);
        Assert.IsFalse(controller.IsBattlePreviewActive);
        Assert.IsFalse(GetPrivateField<bool>(controller, "hasBattlePreviewSnapshot"));
        Assert.AreEqual(movementPosition, pawn.position);
    }

    [Test]
    public void CanMoveTo_ValidatesWithoutChangingPreviewPresentationOrState()
    {
        controller.BeginBattleTilePreview(target, true);
        Vector3 previewPosition = pawn.position;

        Assert.IsTrue(controller.CanMoveTo(target));

        Assert.IsTrue(controller.IsBattlePreviewActive);
        Assert.IsFalse(controller.IsPreviewCommitPending);
        Assert.AreEqual(origin.coord, runState.playerGridPosition);
        Assert.AreEqual(previewPosition, pawn.position);
        Assert.AreEqual(5, points.CurrentPoints);
    }

    [Test]
    public void InvalidMismatchedMove_KeepsPreviewAtTarget()
    {
        BoardTileData nonAdjacent = new BoardTileData { coord = new Vector2Int(2, 0), tileType = MapTileType.Empty };
        controller.BeginBattleTilePreview(target, true);

        Assert.IsFalse(controller.TryMoveTo(nonAdjacent, 0));
        Assert.IsTrue(controller.IsBattlePreviewActive);
        Assert.AreEqual(World(target), pawn.position);

        points.RestoreState(1, 0, 1);
        Assert.IsFalse(controller.TryMoveTo(otherTarget, 2));
        Assert.IsTrue(controller.IsBattlePreviewActive);
        Assert.AreEqual(World(target), pawn.position);
        Assert.AreEqual(1, points.CurrentPoints);
    }

    [Test]
    public void PendingArrival_ExplicitCompletionEmitsExactlyOnceAndRejectsOtherOwnership()
    {
        int callbackCount = 0;
        controller.PlayerMovedToTile += (_, __) => callbackCount++;
        controller.BeginBattleTilePreview(target, true);
        Assert.IsTrue(controller.TryMoveTo(target, 0));

        Assert.IsTrue(controller.IsPreviewCommitPending);
        Assert.IsFalse(controller.TryMoveTo(otherTarget, 0));
        Assert.IsFalse(controller.BeginBattleTilePreview(otherTarget, true));
        Assert.AreEqual(0, callbackCount);

        Assert.IsTrue(controller.CompletePendingBattlePreviewArrival());
        Assert.AreEqual(1, callbackCount);
        Assert.IsFalse(controller.IsPreviewCommitPending);

        Assert.IsFalse(controller.CompletePendingBattlePreviewArrival());
        Assert.AreEqual(1, callbackCount);
    }

    [Test]
    public void ReinitializeSetPositionSetSpawnAndDisable_CompletePendingArrivalBeforeReset()
    {
        AssertPendingArrivalCompleted(() => controller.Initialize(root.transform, board, points, null, runState));
        controller.SetPosition(origin);
        AssertPendingArrivalCompleted(() => controller.SetPosition(origin));
        AssertPendingArrivalCompleted(() => controller.SetSpawn(origin));
        AssertPendingArrivalCompleted(() => controller.enabled = false);
    }

    [Test]
    public void PendingCommitWithMismatchedLogicalState_DoesNotEmitOldTileEvent()
    {
        int callbackCount = 0;
        controller.PlayerMovedToTile += (_, __) => callbackCount++;
        controller.BeginBattleTilePreview(target, true);
        Assert.IsTrue(controller.TryMoveTo(target, 0));

        runState.playerGridPosition = otherTarget.coord;
        Assert.IsFalse(controller.CompletePendingBattlePreviewArrival());

        Assert.AreEqual(0, callbackCount);
        Assert.IsFalse(controller.IsPreviewCommitPending);
    }

    [Test]
    public void InitializeSetPositionAndDisable_InvalidatePreviewAndRestoreSafely()
    {
        Vector3 original = pawn.position;
        controller.BeginBattleTilePreview(target, true);
        controller.Initialize(root.transform, board, points, null, runState);
        Assert.IsFalse(controller.IsBattlePreviewActive);
        Assert.AreEqual(original, pawn.position);

        controller.BeginBattleTilePreview(target, true);
        controller.SetPosition(otherTarget);
        Assert.IsFalse(controller.IsBattlePreviewActive);
        Assert.AreEqual(World(otherTarget), pawn.position);

        controller.BeginBattleTilePreview(target, true);
        controller.enabled = false;
        Assert.IsFalse(controller.IsBattlePreviewActive);
        Assert.AreEqual(World(otherTarget), pawn.position);
    }

    [Test]
    public void StalePreviewIterator_CannotWriteAfterCancelOrCommit()
    {
        IEnumerator staleCancel = controller.PlayBattleTilePreviewRoutine(target, 1f);
        Assert.IsTrue(staleCancel.MoveNext());
        controller.CancelBattleTilePreview(true);
        Vector3 afterCancel = pawn.position;
        Drain(staleCancel);
        Assert.AreEqual(afterCancel, pawn.position);

        IEnumerator staleCommit = controller.PlayBattleTilePreviewRoutine(target, 1f);
        Assert.IsTrue(staleCommit.MoveNext());
        controller.BeginBattleTilePreview(target, true);
        Assert.IsTrue(controller.TryMoveTo(target, 0));
        Vector3 afterCommit = pawn.position;
        Drain(staleCommit);
        Assert.AreEqual(afterCommit, pawn.position);
    }

    [TestCase(float.NaN)]
    [TestCase(float.PositiveInfinity)]
    [TestCase(float.NegativeInfinity)]
    public void NonFiniteDurations_CompleteImmediately(float duration)
    {
        IEnumerator approach = controller.PlayBattleTilePreviewRoutine(target, duration);
        Assert.IsFalse(approach.MoveNext());
        Assert.AreEqual(World(target), pawn.position);

        IEnumerator restore = controller.RestoreBattleTilePreviewRoutine(duration);
        Assert.IsFalse(restore.MoveNext());
        Assert.AreEqual(World(origin), pawn.position);
    }

    private Vector3 World(BoardTileData tile)
    {
        return board.GridToWorld(tile.coord) + new Vector3(0f, 0f, -0.2f);
    }

    private static T GetPrivateField<T>(object instance, string name)
    {
        return (T)instance.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(instance);
    }

    private static void SetPrivateField(object instance, string name, object value)
    {
        instance.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(instance, value);
    }

    private void AssertPendingArrivalCompleted(System.Action lifecycleAction)
    {
        if (!controller.enabled)
            controller.enabled = true;
        controller.SetPosition(origin);
        int callbackCount = 0;
        System.Action<BoardTileData, string> handler = (_, __) => callbackCount++;
        controller.PlayerMovedToTile += handler;
        controller.BeginBattleTilePreview(target, true);
        Assert.IsTrue(controller.TryMoveTo(target, 0));

        lifecycleAction();

        controller.PlayerMovedToTile -= handler;
        Assert.AreEqual(1, callbackCount);
        Assert.IsFalse(controller.IsPreviewCommitPending);
    }

    private static void Drain(IEnumerator routine)
    {
        int guard = 0;
        while (routine.MoveNext() && guard++ < 100)
        {
        }

        Assert.Less(guard, 100, "Routine did not terminate.");
    }
}
