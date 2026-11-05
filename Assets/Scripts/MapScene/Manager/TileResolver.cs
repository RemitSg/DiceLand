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
                message = GameText.Get("map.tile.unknown", "未知格子。");
                break;
        }

        TileResolved?.Invoke(tile, message);
        return message;
    }

    private static string ResolveEmptyTile(BoardTileData tile)
    {
        tile.isResolved = true;
        return GameText.Get("map.tile.enter_empty", "来到了空白格。");
    }

    private static string ResolveBattleTile(BoardTileData tile, RunState runState)
    {
        if (!tile.isResolved)
        {
            tile.isResolved = true;
            int gainedExp = tile.previewDangerRating * 5;
            runState.ApplyExperienceToPrimaryMember(gainedExp);
            return GameText.Format("map.tile.battle_resolution", "战斗格占位结算：危险度 {getRatingPhrase}，获得 {gainedExp} 经验。", ("getRatingPhrase", MapDangerRatingService.GetRatingPhrase(tile.previewDangerRating)), ("gainedExp", gainedExp));
        }

        return GameText.Get("map.tile.battle_cleared", "这个战斗格已经被清理过了。");
    }

    private static string ResolveEliteTile(BoardTileData tile, RunState runState)
    {
        if (!tile.isResolved)
        {
            tile.isResolved = true;
            int gainedExp = tile.previewDangerRating * 8;
            runState.ApplyExperienceToPrimaryMember(gainedExp);
            return GameText.Format("map.tile.elite_resolution", "精英战斗格占位结算：危险度 {getRatingPhrase}，获得 {gainedExp} 经验。", ("getRatingPhrase", MapDangerRatingService.GetRatingPhrase(tile.previewDangerRating)), ("gainedExp", gainedExp));
        }

        return GameText.Get("map.tile.elite_cleared", "这个精英格已经被清理过了。");
    }

    private string ResolveEventTile(BoardTileData tile, RunState runState)
    {
        if (!tile.isResolved)
            return GameText.Get("map.tile.enter_event", "进入事件格。");

        return GameText.Get("map.tile.event_resolved", "这个事件格已经解决过了。");
    }

    private static string ResolveMerchantTile(BoardTileData tile)
    {
        if (!tile.isResolved)
            tile.isResolved = true;

        return GameText.Get("map.tile.enter_shop", "进入商人格。可在此消耗金钱购买商品。");
    }

    private static string ResolveTreasureTile(BoardTileData tile)
    {
        if (!tile.isResolved)
            return GameText.Get("map.tile.enter_treasure", "进入宝藏格。");

        return GameText.Get("map.tile.treasure_claimed", "这个宝藏格已经领取过了。");
    }

    private static string ResolveForgeTile(BoardTileData tile)
    {
        if (!tile.isResolved)
            tile.isResolved = true;

        return GameText.Get("map.tile.enter_forge", "进入锻造格。可在此使用图纸和材料锻造武器。");
    }

    private static string ResolveRestTile(BoardTileData tile)
    {
        if (!tile.isResolved)
        {
            tile.isResolved = true;
            return GameText.Get("map.tile.enter_rest", "休息格占位结算：后续可接入治疗或净化效果。");
        }

        return GameText.Get("map.feedback.you_have_already_used_this_rest_tile", "这个休息格已经使用过了。");
    }

    private static string ResolveKeyTile(BoardTileData tile, RunState runState)
    {
        if (!tile.isResolved)
        {
            tile.isResolved = true;
            runState.currentKeys += 1;
            return runState.IsBossUnlocked
                ? GameText.Format("map.tile.key_boss_unlocked", "获得一把钥匙。当前钥匙 {currentKeys}/{requiredKeys}，Boss 已解锁。", ("currentKeys", runState.currentKeys), ("requiredKeys", runState.requiredKeys))
                : GameText.Format("map.tile.key_collected", "获得一把钥匙。当前钥匙 {currentKeys}/{requiredKeys}。", ("currentKeys", runState.currentKeys), ("requiredKeys", runState.requiredKeys));
        }

        return GameText.Get("map.tile.key_taken", "这个钥匙格已经领取过了。");
    }

    private static string ResolveBossTile(RunState runState)
    {
        return runState.IsBossUnlocked
            ? GameText.Get("map.tile.boss_ready", "Boss 格占位结算：这里将接入最终战。")
            : GameText.Format("map.tile.boss_locked", "需要更多钥匙才能挑战 Boss。当前钥匙 {currentKeys}/{requiredKeys}。", ("currentKeys", runState.currentKeys), ("requiredKeys", runState.requiredKeys));
    }

    private static string ResolveSpecialDungeonExitTile(BoardTileData tile)
    {
        tile.isVisited = true;
        return GameText.Get("map.tile.exit_dungeon", "离开特殊副本。");
    }
}
