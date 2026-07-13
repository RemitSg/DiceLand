using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum MapBattleTilePreviewCommitResult
{
    NotCommitted,
    BattleHandoffStarted,
    MovementCommittedWithoutHandoff
}

[DisallowMultipleComponent]
public sealed class MapBattleTilePreviewController : MonoBehaviour
{
    private const float FirstPreviewDuration = 0.8f;
    private const float RepeatPreviewDuration = 0.3f;
    private const float PreviewExitDuration = 0.3f;
    private const float PreviewZoom = 1.75f;

    private readonly HashSet<BoardTileData> previewedTiles = new HashSet<BoardTileData>();
    private MapViewController mapViewController;
    private PlayerMapController playerMapController;
    private BoardManager boardManager;
    private MapBattleTilePreviewOverlay overlay;
    private Func<BoardTileData, int, MapBattleTilePreviewCommitResult> tryCommitMove;
    private Action<string> showStatus;
    private MapBattleTilePreviewContext currentContext;
    private Coroutine transitionRoutine;
    private int transitionGeneration;
    private bool commitInvoked;
    private float lastEnterDuration;

    public MapBattleTilePreviewState State { get; private set; } = MapBattleTilePreviewState.Hidden;
    public bool IsActive => State != MapBattleTilePreviewState.Hidden;

    public void Configure(
        MapViewController view,
        PlayerMapController player,
        BoardManager board,
        MapBattleTilePreviewOverlay previewOverlay,
        Func<BoardTileData, int, MapBattleTilePreviewCommitResult> commitMove,
        Action<string> statusPresenter)
    {
        if (overlay != null)
        {
            overlay.ConfirmRequested -= Confirm;
            overlay.CancelRequested -= HandleCancelRequested;
        }

        CancelImmediate();
        mapViewController = view;
        playerMapController = player;
        boardManager = board;
        overlay = previewOverlay;
        tryCommitMove = commitMove;
        showStatus = statusPresenter;

        if (overlay != null)
        {
            overlay.ConfirmRequested -= Confirm;
            overlay.ConfirmRequested += Confirm;
            overlay.CancelRequested -= HandleCancelRequested;
            overlay.CancelRequested += HandleCancelRequested;
            overlay.Hide();
        }
    }

    public bool BeginPreview(MapBattleTilePreviewContext context, bool immediate = false)
    {
        if (IsActive || context == null || !context.canPreview || context.tile == null ||
            mapViewController == null || playerMapController == null || boardManager == null || overlay == null)
        {
            return false;
        }

        currentContext = context;
        commitInvoked = false;
        State = MapBattleTilePreviewState.Entering;
        lastEnterDuration = previewedTiles.Contains(context.tile)
            ? RepeatPreviewDuration
            : FirstPreviewDuration;
        overlay.Hide();

        Vector3 origin = playerMapController.PawnWorldPosition;
        Vector3 target = boardManager.GridToWorld(context.tile.coord);
        int generation = ++transitionGeneration;
        if (immediate)
        {
            mapViewController.ApplyBattlePreviewImmediate(origin, target, PreviewZoom);
            if (!playerMapController.BeginBattleTilePreview(context.tile, true))
            {
                mapViewController.RestoreBattlePreviewImmediate();
                ResetHidden();
                showStatus?.Invoke("战斗格预览未能打开。");
                return false;
            }

            CompleteEntering(generation);
            return State == MapBattleTilePreviewState.Ready;
        }

        transitionRoutine = StartCoroutine(PlayEnterRoutine(origin, target, generation));
        return true;
    }

    public void Confirm()
    {
        if (State != MapBattleTilePreviewState.Ready || currentContext == null ||
            !currentContext.canConfirm || commitInvoked)
        {
            return;
        }

        commitInvoked = true;
        State = MapBattleTilePreviewState.Committing;
        overlay?.SetInteractionEnabled(false);

        MapBattleTilePreviewCommitResult result;
        try
        {
            result = tryCommitMove != null
                ? tryCommitMove(currentContext.tile, currentContext.moveCost)
                : MapBattleTilePreviewCommitResult.NotCommitted;
        }
        catch (Exception exception)
        {
            Debug.LogException(exception, this);
            result = MapBattleTilePreviewCommitResult.NotCommitted;
        }

        if (result != MapBattleTilePreviewCommitResult.NotCommitted)
        {
            CompleteCommittedPreview();
            if (result == MapBattleTilePreviewCommitResult.MovementCommittedWithoutHandoff)
                showStatus?.Invoke("已进入战斗格，但战斗未能启动，请再次点击重试");
            return;
        }

        commitInvoked = false;
        State = MapBattleTilePreviewState.Ready;
        overlay?.ShowFailure("本次移动未能完成，请重试或返回地图。");
        showStatus?.Invoke("本次移动未能完成。");
    }

    public void Cancel(bool immediate = false)
    {
        if (State != MapBattleTilePreviewState.Ready && State != MapBattleTilePreviewState.Entering)
            return;

        if (immediate)
        {
            CancelImmediate();
            return;
        }

        StopTransitionRoutine();
        int generation = ++transitionGeneration;
        State = MapBattleTilePreviewState.Exiting;
        overlay?.Hide();
        transitionRoutine = StartCoroutine(PlayExitRoutine(generation));
    }

    public void CancelImmediate()
    {
        StopTransitionRoutine();
        transitionGeneration++;
        overlay?.Hide();
        mapViewController?.RestoreBattlePreviewImmediate();
        playerMapController?.CancelBattleTilePreview(true);
        ResetHidden();
    }

    private IEnumerator PlayEnterRoutine(Vector3 origin, Vector3 target, int generation)
    {
        IEnumerator cameraRoutine = mapViewController.PlayBattlePreviewRoutine(
            origin,
            target,
            PreviewZoom,
            lastEnterDuration);
        IEnumerator pawnRoutine = playerMapController.PlayBattleTilePreviewRoutine(
            currentContext.tile,
            lastEnterDuration);

        IEnumerator combined = RunFrameOnlyParallel(cameraRoutine, pawnRoutine);
        while (true)
        {
            bool hasNext;
            try
            {
                hasNext = combined.MoveNext();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
                CancelImmediate();
                showStatus?.Invoke("战斗格预览未能打开。");
                yield break;
            }

            if (!hasNext)
                break;
            yield return combined.Current;
        }

        CompleteEntering(generation);
    }

    private IEnumerator PlayExitRoutine(int generation)
    {
        IEnumerator cameraRoutine = mapViewController != null
            ? mapViewController.RestoreBattlePreviewRoutine(PreviewExitDuration)
            : null;
        IEnumerator pawnRoutine = playerMapController != null
            ? playerMapController.RestoreBattleTilePreviewRoutine(PreviewExitDuration)
            : null;

        IEnumerator combined = RunFrameOnlyParallel(cameraRoutine, pawnRoutine);
        while (true)
        {
            bool hasNext;
            try
            {
                hasNext = combined.MoveNext();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
                CancelImmediate();
                showStatus?.Invoke("战斗格预览恢复失败，已立即返回地图。");
                yield break;
            }

            if (!hasNext)
                break;
            yield return combined.Current;
        }

        if (generation != transitionGeneration || State != MapBattleTilePreviewState.Exiting)
            yield break;

        ResetHidden();
    }

    private static IEnumerator RunFrameOnlyParallel(IEnumerator first, IEnumerator second)
    {
        IDisposable firstDisposable = first as IDisposable;
        IDisposable secondDisposable = second as IDisposable;
        try
        {
            bool firstActive = first != null;
            bool secondActive = second != null;
            while (firstActive || secondActive)
            {
                if (firstActive)
                {
                    firstActive = first.MoveNext();
                    if (firstActive && first.Current != null)
                        throw new InvalidOperationException("Battle preview frame routines may only yield null.");
                }

                if (secondActive)
                {
                    secondActive = second.MoveNext();
                    if (secondActive && second.Current != null)
                        throw new InvalidOperationException("Battle preview frame routines may only yield null.");
                }

                if (firstActive || secondActive)
                    yield return null;
            }
        }
        finally
        {
            firstDisposable?.Dispose();
            secondDisposable?.Dispose();
        }
    }

    private void CompleteEntering(int generation)
    {
        transitionRoutine = null;
        if (generation != transitionGeneration || State != MapBattleTilePreviewState.Entering)
            return;

        if (!mapViewController.IsBattlePreviewActive || !playerMapController.IsBattlePreviewActive)
        {
            CancelImmediate();
            showStatus?.Invoke("战斗格预览未能打开。");
            return;
        }

        previewedTiles.Add(currentContext.tile);
        State = MapBattleTilePreviewState.Ready;
        overlay.Show(currentContext);
    }

    private void CompleteCommittedPreview()
    {
        overlay?.Hide();
        mapViewController?.RestoreBattlePreviewImmediate();
        playerMapController?.CancelBattleTilePreview(true);
        ResetHidden();
    }

    private void HandleCancelRequested()
    {
        Cancel(false);
    }

    private void StopTransitionRoutine()
    {
        if (transitionRoutine == null)
            return;

        StopCoroutine(transitionRoutine);
        transitionRoutine = null;
    }

    private void ResetHidden()
    {
        transitionRoutine = null;
        currentContext = null;
        commitInvoked = false;
        State = MapBattleTilePreviewState.Hidden;
    }

    private void OnDisable()
    {
        CancelImmediate();
    }

    private void OnDestroy()
    {
        if (overlay == null)
            return;

        overlay.ConfirmRequested -= Confirm;
        overlay.CancelRequested -= HandleCancelRequested;
    }
}
