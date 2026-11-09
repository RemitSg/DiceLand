using UnityEngine;

public partial class PlayerMapController
{
    private MapTeleportEffect teleportEffect;
    public bool IsTeleporting => teleportEffect != null && !teleportEffect.IsComplete;

    /// <summary>Repositions without spending AP, exploring, resolving the tile, or firing normal arrival rewards.</summary>
    public MapTeleportEffect TeleportTo(BoardTileData tile, MapViewController view = null, bool animate = true)
    {
        if (tile == null || boardManager == null || runState == null) return null;
        StopTeleportPresentation();
        CompletePendingBattlePreviewArrival();
        InvalidateBattlePreview(true);
        if (moveRoutine != null) StopCoroutine(moveRoutine);
        moveRoutine = null;
        pendingHeadlessArrival = null;
        isMoving = false;
        EnsurePawn();
        runState.playerGridPosition = tile.coord;
        Vector3 destination = boardManager.GridToWorld(tile.coord) + new Vector3(0, 0, -.2f);
        if (!animate || headlessExecution || !isActiveAndEnabled || Vector3.Distance(playerPawn.position, destination) < .001f)
        {
            SetPosition(tile);
            view?.FocusOnPosition(destination, true);
            return null;
        }

        teleportEffect = MapTeleportEffect.Spawn(unitsRoot, playerPawn, visualRoot, destination, boardManager.TileSpacing, view);
        teleportEffect.Finished += HandleTeleportFinished;
        isMoving = true;
        return teleportEffect;
    }

    private void HandleTeleportFinished()
    {
        teleportEffect = null;
        isMoving = false;
    }

    private void StopTeleportPresentation()
    {
        if (teleportEffect == null) return;
        MapTeleportEffect previous = teleportEffect;
        teleportEffect = null;
        previous.Finished -= HandleTeleportFinished;
        previous.CompleteImmediately();
        isMoving = false;
        if (Application.isPlaying) Destroy(previous.gameObject);
        else DestroyImmediate(previous.gameObject);
    }
}
