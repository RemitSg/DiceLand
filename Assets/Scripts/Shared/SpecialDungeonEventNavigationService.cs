using System.Collections.Generic;
using UnityEngine;

public static class SpecialDungeonEventNavigationService
{
    public const int NoDestination = -1;
    public const int ExitDestination = -2;

    public static bool CanPlayerMove(SpecialDungeonRuntimeContext context, Vector2Int position, BoardTileData destination)
    {
        if (context == null || !context.isActive || context.activeDungeon == null || !context.activeDungeon.eventDrivenNavigation)
            return true;
        int size = context.activeDungeon.GetValidEntries().Count + 3;
        return !context.eventNavigationStarted
            && position == new Vector2Int(1, Mathf.Max(1, size / 2))
            && destination != null && destination.sourceSpecialDungeon == context.activeDungeon
            && destination.specialDungeonEntryIndex == 0 && destination.tileType == MapTileType.Event;
    }

    public static void MarkEventStarted(SpecialDungeonRuntimeContext context, BoardTileData tile)
    {
        if (context != null && context.isActive && context.activeDungeon != null && context.activeDungeon.eventDrivenNavigation
            && tile != null && tile.sourceSpecialDungeon == context.activeDungeon)
            context.eventNavigationStarted = true;
    }

    public static bool IsValidDestination(BoardTileData tile, int entryIndex)
    {
        if (entryIndex == NoDestination) return true;
        SpecialDungeon_SO dungeon = tile != null ? tile.sourceSpecialDungeon : null;
        return dungeon != null && dungeon.eventDrivenNavigation
            && (entryIndex == ExitDestination || (entryIndex >= 0 && entryIndex < dungeon.GetValidEntries().Count));
    }

    public static bool TryNavigate(SpecialDungeonRuntimeContext context, int entryIndex, RunState state,
        BoardManager board, PlayerMapController mover, out BoardTileData destination)
    {
        destination = null;
        if (context == null || !context.isActive || context.activeDungeon == null || !context.activeDungeon.eventDrivenNavigation
            || entryIndex == NoDestination || state == null || board == null || mover == null)
            return false;
        foreach (BoardTileData tile in board.GetAllTiles())
        {
            if (tile.sourceSpecialDungeon != context.activeDungeon) continue;
            if (entryIndex == ExitDestination ? tile.tileType == MapTileType.SpecialDungeonExit && tile.coord.x == board.BoardSize - 1
                : tile.specialDungeonEntryIndex == entryIndex && tile.tileType == MapTileType.Event && tile.assignedEvent != null)
            { destination = tile; break; }
        }
        if (destination == null) return false;
        context.eventNavigationStarted = true;
        destination.isVisited = true;
        mover.SetPosition(destination, true);
        board.SetSelectedTile(destination.coord);
        board.RefreshAllViews();
        return true;
    }
}
