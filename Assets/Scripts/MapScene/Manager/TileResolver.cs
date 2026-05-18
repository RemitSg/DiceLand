using System;
using UnityEngine;

[DisallowMultipleComponent]
public class TileResolver : MonoBehaviour
{
    [SerializeField] private MoneyRewardTable_SO moneyRewardTable;

    public event Action<BoardTileData, string> TileResolved;
    public event Action<BoardTileData> KeyCollected;

    public void ConfigureMoneyRewardTable(MoneyRewardTable_SO rewardTable)
    {
        moneyRewardTable = rewardTable;
    }

    public string ResolveTile(BoardTileData tile, RunState runState)
    {
        if (tile == null || runState == null)
            return string.Empty;

        tile.isVisited = true;

        string message;
        switch (tile.tileType)
        {
            case MapTileType.Empty:
                message = ResolveEmptyTile(tile);
                break;
            case MapTileType.Battle:
                message = ResolveBattleTile(tile, runState);
                break;
            case MapTileType.Elite:
                message = ResolveEliteTile(tile, runState);
                break;
            case MapTileType.Event:
                message = ResolveEventTile(tile, runState);
                break;
            case MapTileType.Merchant:
                message = ResolveMerchantTile(tile);
                break;
            case MapTileType.Treasure:
                message = ResolveTreasureTile(tile);
                break;
            case MapTileType.Forge:
                message = ResolveForgeTile(tile);
                break;
            case MapTileType.Rest:
                message = ResolveRestTile(tile);
                break;
            case MapTileType.Key:
                bool wasResolvedBeforeKey = tile.isResolved;
                message = ResolveKeyTile(tile, runState);
                if (!wasResolvedBeforeKey && tile.isResolved)
                    KeyCollected?.Invoke(tile);
                break;
            case MapTileType.Boss:
                message = ResolveBossTile(runState);
                break;
            case MapTileType.SpecialDungeonExit:
                message = ResolveSpecialDungeonExitTile(tile);
                break;
            default:
                message = "\u672a\u77e5\u683c\u5b50\u3002";
                break;
        }

        TileResolved?.Invoke(tile, message);
        return message;
    }

    private static string ResolveEmptyTile(BoardTileData tile)
    {
        tile.isResolved = true;
        return "\u6765\u5230\u4e86\u7a7a\u767d\u683c\u3002";
    }

    private static string ResolveBattleTile(BoardTileData tile, RunState runState)
    {
        if (!tile.isResolved)
        {
            tile.isResolved = true;
            int gainedExp = tile.previewDangerRating * 5;
            runState.ApplyExperienceToPrimaryMember(gainedExp);
            return $"\u6218\u6597\u683c\u5360\u4f4d\u7ed3\u7b97\uff1a\u5371\u9669\u5ea6 {tile.previewDangerRating}\uff0c\u83b7\u5f97 {gainedExp} \u7ecf\u9a8c\u3002";
        }

        return "\u8fd9\u4e2a\u6218\u6597\u683c\u5df2\u7ecf\u88ab\u6e05\u7406\u8fc7\u4e86\u3002";
    }

    private static string ResolveEliteTile(BoardTileData tile, RunState runState)
    {
        if (!tile.isResolved)
        {
            tile.isResolved = true;
            int gainedExp = tile.previewDangerRating * 8;
            runState.ApplyExperienceToPrimaryMember(gainedExp);
            return $"\u7cbe\u82f1\u6218\u6597\u683c\u5360\u4f4d\u7ed3\u7b97\uff1a\u5371\u9669\u5ea6 {tile.previewDangerRating}\uff0c\u83b7\u5f97 {gainedExp} \u7ecf\u9a8c\u3002";
        }

        return "\u8fd9\u4e2a\u7cbe\u82f1\u683c\u5df2\u7ecf\u88ab\u6e05\u7406\u8fc7\u4e86\u3002";
    }

    private string ResolveEventTile(BoardTileData tile, RunState runState)
    {
        if (!tile.isResolved)
            return "\u8fdb\u5165\u4e8b\u4ef6\u683c\u3002";

        return "\u8fd9\u4e2a\u4e8b\u4ef6\u683c\u5df2\u7ecf\u89e3\u51b3\u8fc7\u4e86\u3002";
    }

    private static string ResolveMerchantTile(BoardTileData tile)
    {
        if (!tile.isResolved)
            tile.isResolved = true;

        return "\u8fdb\u5165\u5546\u4eba\u683c\u3002\u53ef\u5728\u6b64\u6d88\u8017\u91d1\u94b1\u8d2d\u4e70\u5546\u54c1\u3002";
    }

    private static string ResolveTreasureTile(BoardTileData tile)
    {
        if (!tile.isResolved)
            return "\u8fdb\u5165\u5b9d\u85cf\u683c\u3002";

        return "\u8fd9\u4e2a\u5b9d\u85cf\u683c\u5df2\u7ecf\u9886\u53d6\u8fc7\u4e86\u3002";
    }

    private static string ResolveForgeTile(BoardTileData tile)
    {
        if (!tile.isResolved)
            tile.isResolved = true;

        return "\u8fdb\u5165\u953b\u9020\u683c\u3002\u53ef\u5728\u6b64\u4f7f\u7528\u56fe\u7eb8\u548c\u6750\u6599\u953b\u9020\u6b66\u5668\u3002";
    }

    private static string ResolveRestTile(BoardTileData tile)
    {
        if (!tile.isResolved)
        {
            tile.isResolved = true;
            return "\u4f11\u606f\u683c\u5360\u4f4d\u7ed3\u7b97\uff1a\u540e\u7eed\u53ef\u63a5\u5165\u6cbb\u7597\u6216\u51c0\u5316\u6548\u679c\u3002";
        }

        return "\u8fd9\u4e2a\u4f11\u606f\u683c\u5df2\u7ecf\u4f7f\u7528\u8fc7\u4e86\u3002";
    }

    private static string ResolveKeyTile(BoardTileData tile, RunState runState)
    {
        if (!tile.isResolved)
        {
            tile.isResolved = true;
            runState.currentKeys += 1;
            return runState.IsBossUnlocked
                ? $"\u83b7\u5f97\u4e00\u628a\u94a5\u5319\u3002\u5f53\u524d\u94a5\u5319 {runState.currentKeys}/{runState.requiredKeys}\uff0cBoss \u5df2\u89e3\u9501\u3002"
                : $"\u83b7\u5f97\u4e00\u628a\u94a5\u5319\u3002\u5f53\u524d\u94a5\u5319 {runState.currentKeys}/{runState.requiredKeys}\u3002";
        }

        return "\u8fd9\u4e2a\u94a5\u5319\u683c\u5df2\u7ecf\u9886\u53d6\u8fc7\u4e86\u3002";
    }

    private static string ResolveBossTile(RunState runState)
    {
        return runState.IsBossUnlocked
            ? "Boss \u683c\u5360\u4f4d\u7ed3\u7b97\uff1a\u8fd9\u91cc\u5c06\u63a5\u5165\u6700\u7ec8\u6218\u3002"
            : $"\u9700\u8981\u66f4\u591a\u94a5\u5319\u624d\u80fd\u6311\u6218 Boss\u3002\u5f53\u524d\u94a5\u5319 {runState.currentKeys}/{runState.requiredKeys}\u3002";
    }

    private static string ResolveSpecialDungeonExitTile(BoardTileData tile)
    {
        tile.isVisited = true;
        return "离开特殊副本。";
    }
}
