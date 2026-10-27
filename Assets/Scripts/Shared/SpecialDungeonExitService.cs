using System.Collections.Generic;
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
        {
            restored.runState.playerGridPosition = context.returnEventCoord;
            restored.runState.boardSize = restored.boardSize;
        }

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

    public static bool TryCompleteExit(SpecialDungeonRuntimeContext context, RunState latest, BoardTileData exit,
        IEnumerable<int> currentPendingCalamities, out MapRunSnapshot restored, out string message)
    {
        restored = null;
        message = "当前无法离开特殊副本。";
        if (context == null || !context.isActive || latest == null || exit == null
            || exit.tileType != MapTileType.SpecialDungeonExit || exit.coord != latest.playerGridPosition
            || exit.sourceSpecialDungeon != context.activeDungeon || exit.coord.y != Mathf.Max(1, latest.boardSize / 2)
            || (exit.coord.x != 0 && exit.coord.x != latest.boardSize - 1)) return false;
        restored = BuildReturnedMainMapSnapshot(context, latest);
        if (restored?.runState == null) return false;
        if (exit.coord.x == latest.boardSize - 1)
        {
            if (restored.runState.settlementStats == null) restored.runState.settlementStats = new RunSettlementStats();
            int score = context.activeDungeon != null ? context.activeDungeon.settlementScoreValue
                : RunScoreConfig_SO.CreateDefaultRuntimeConfig().defaultSpecialDungeonScore;
            restored.runState.settlementStats.AddSpecialDungeonScore(score);
        }
        var tiers = new HashSet<int>(restored.pendingCalamityTiers);
        if (context.deferredCalamityTiers != null)
            foreach (int tier in context.deferredCalamityTiers)
                if (tiers.Add(tier)) restored.pendingCalamityTiers.Add(tier);
        if (currentPendingCalamities != null)
            foreach (int tier in currentPendingCalamities)
                if (tiers.Add(tier)) restored.pendingCalamityTiers.Add(tier);
        context.isActive = false;
        message = "已离开特殊副本，回到主地图。";
        return true;
    }
}
