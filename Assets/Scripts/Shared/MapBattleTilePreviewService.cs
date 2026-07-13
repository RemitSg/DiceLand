using System;
using System.Collections.Generic;

public enum MapBattleTilePreviewState
{
    Hidden,
    Entering,
    Ready,
    Committing,
    Exiting
}

public sealed class MapBattleTilePreviewContext
{
    public BoardTileData tile;
    public int moveCost;
    public int dangerRating;
    public string[] attributeNames = Array.Empty<string>();
    public bool canPreview;
    public bool canConfirm;
    public string blockReason = string.Empty;
    public string confirmLabel = string.Empty;
}

public static class MapBattleTilePreviewService
{
    public static MapBattleTilePreviewContext BuildContext(
        RunState runState,
        BoardTileData tile,
        int moveCost,
        bool canAttemptMove)
    {
        int safeMoveCost = Math.Max(0, moveCost);
        bool canPreview = tile != null && tile.IsHostileTile && !tile.isResolved;
        string[] attributeNames = CollectAttributeNames(tile != null ? tile.assignedEnemyGroup : null);

        MapBattleTilePreviewContext context = new MapBattleTilePreviewContext
        {
            tile = tile,
            moveCost = safeMoveCost,
            dangerRating = MapDangerRatingService.ClampRating(tile != null ? tile.previewDangerRating : 1),
            attributeNames = attributeNames.Length > 0 ? attributeNames : new[] { "无明显倾向" },
            canPreview = canPreview,
            canConfirm = canPreview && canAttemptMove,
            confirmLabel = $"进入战斗 · 预计 {safeMoveCost} AP"
        };

        if (tile != null && tile.tileType == MapTileType.Boss &&
            (runState == null || !runState.IsBossUnlocked))
        {
            int currentKeys = runState != null ? runState.currentKeys : 0;
            int requiredKeys = runState != null ? runState.requiredKeys : 0;
            context.canConfirm = false;
            context.blockReason = $"Boss 未解锁：{currentKeys}/{requiredKeys} 钥匙";
        }
        else if (!canAttemptMove)
        {
            context.canConfirm = false;
            context.blockReason = $"行动点不足，需要 {safeMoveCost} 点";
        }

        return context;
    }

    public static bool CanTransition(MapBattleTilePreviewState from, MapBattleTilePreviewState to)
    {
        return (from == MapBattleTilePreviewState.Hidden && to == MapBattleTilePreviewState.Entering) ||
               (from == MapBattleTilePreviewState.Entering && to == MapBattleTilePreviewState.Ready) ||
               (from == MapBattleTilePreviewState.Ready && to == MapBattleTilePreviewState.Committing) ||
               (from == MapBattleTilePreviewState.Ready && to == MapBattleTilePreviewState.Exiting) ||
               (from == MapBattleTilePreviewState.Exiting && to == MapBattleTilePreviewState.Hidden);
    }

    private static string[] CollectAttributeNames(EnemyGroup_SO enemyGroup)
    {
        IReadOnlyList<LootAttribute_SO> source = enemyGroup != null ? enemyGroup.GetAttributes() : null;
        List<string> names = new List<string>(3);
        HashSet<string> seenKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; source != null && i < source.Count && names.Count < 3; i++)
        {
            LootAttribute_SO attribute = source[i];
            if (attribute == null || string.IsNullOrWhiteSpace(attribute.attributeName))
                continue;

            string name = attribute.attributeName.Trim();
            string key = !string.IsNullOrWhiteSpace(attribute.attributeId)
                ? attribute.attributeId.Trim()
                : name;

            if (seenKeys.Add(key))
                names.Add(name);
        }

        return names.ToArray();
    }
}
