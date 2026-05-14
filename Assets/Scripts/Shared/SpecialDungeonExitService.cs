using UnityEngine;

public static class SpecialDungeonExitService
{
    public static MapRunSnapshot BuildReturnedMainMapSnapshot(
        SpecialDungeonRuntimeContext context,
        RunState latestRunState)
    {
        if (context == null || context.mainMapSnapshot == null)
            return null;

        MapRunSnapshot restored = context.mainMapSnapshot.Clone();
        if (latestRunState != null)
            restored.runState = latestRunState.Clone();
        if (restored.runState != null)
            restored.runState.playerGridPosition = context.returnEventCoord;

        for (int i = 0; i < restored.tiles.Count; i++)
        {
            BoardTileData tile = restored.tiles[i];
            if (tile == null || tile.coord != context.returnEventCoord)
                continue;

            tile.isVisited = true;
            tile.isResolved = true;
            tile.assignedEvent = null;
            break;
        }

        restored.selectedCoord = context.returnEventCoord;
        restored.specialDungeonContext = null;
        return restored;
    }
}
