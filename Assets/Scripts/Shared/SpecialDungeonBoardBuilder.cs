using System.Collections.Generic;
using UnityEngine;

public sealed class SpecialDungeonBoardBuildResult
{
    public int boardSize;
    public Vector2Int leftExitCoord;
    public Vector2Int spawnCoord;
    public Vector2Int rightExitCoord;
    public List<BoardTileData> tiles = new List<BoardTileData>();
}

public static class SpecialDungeonBoardBuilder
{
    private const int SpawnIndex = 1;

    public static SpecialDungeonBoardBuildResult Build(SpecialDungeon_SO dungeon, int seed)
    {
        IReadOnlyList<SpecialDungeonTileEntry> entries = dungeon != null
            ? dungeon.GetValidEntries()
            : System.Array.Empty<SpecialDungeonTileEntry>();
        int totalTiles = entries.Count + 3;
        int row = Mathf.Max(1, totalTiles / 2);

        SpecialDungeonBoardBuildResult result = new SpecialDungeonBoardBuildResult
        {
            boardSize = totalTiles,
            leftExitCoord = new Vector2Int(0, row),
            spawnCoord = new Vector2Int(SpawnIndex, row),
            rightExitCoord = new Vector2Int(totalTiles - 1, row)
        };

        result.tiles.Add(CreateExit(result.leftExitCoord, dungeon, seed));
        result.tiles.Add(CreateSpawn(result.spawnCoord, dungeon, seed ^ 0x51A7));

        for (int i = 0; i < entries.Count; i++)
            result.tiles.Add(CreateConfiguredTile(entries[i], dungeon, i, new Vector2Int(i + 2, row), seed));

        result.tiles.Add(CreateExit(result.rightExitCoord, dungeon, seed ^ 0x7E17));
        return result;
    }

    private static BoardTileData CreateExit(Vector2Int coord, SpecialDungeon_SO dungeon, int seed)
    {
        return new BoardTileData
        {
            coord = coord,
            tileType = MapTileType.SpecialDungeonExit,
            dangerBand = MapDangerBand.Outer,
            isResolved = true,
            encounterSeed = seed,
            previewTag = "副本出口",
            sourceSpecialDungeon = dungeon,
            specialDungeonEntryIndex = -1
        };
    }

    private static BoardTileData CreateSpawn(Vector2Int coord, SpecialDungeon_SO dungeon, int seed)
    {
        return new BoardTileData
        {
            coord = coord,
            tileType = MapTileType.Empty,
            dangerBand = MapDangerBand.Outer,
            isVisited = true,
            isResolved = true,
            isSpawnTile = true,
            encounterSeed = seed,
            previewTag = "副本起点",
            sourceSpecialDungeon = dungeon,
            specialDungeonEntryIndex = -1
        };
    }

    private static BoardTileData CreateConfiguredTile(
        SpecialDungeonTileEntry entry,
        SpecialDungeon_SO dungeon,
        int entryIndex,
        Vector2Int coord,
        int seed)
    {
        BoardTileData tile = new BoardTileData
        {
            coord = coord,
            tileType = entry.tileType,
            dangerBand = ResolveDangerBand(entryIndex),
            encounterSeed = seed ^ ((entryIndex + 1) * 397),
            previewDangerRating = ResolvePreviewDangerRating(entry, ResolveDangerBand(entryIndex)),
            previewTag = entry.previewTag,
            assignedEnemyGroup = entry.enemyGroup,
            assignedEvent = entry.eventData,
            sourceSpecialDungeon = dungeon,
            specialDungeonEntryIndex = entryIndex
        };

        CopyMerchantOffers(entry, tile);
        CopyTreasureOptions(entry, tile);
        return tile;
    }

    private static int ResolvePreviewDangerRating(SpecialDungeonTileEntry entry, MapDangerBand dangerBand)
    {
        if (entry == null)
            return 0;

        if (entry.tileType != MapTileType.Battle && entry.tileType != MapTileType.Elite)
            return 0;

        return MapDangerRatingService.ResolveRating(
            entry.tileType,
            dangerBand,
            entry.enemyGroup,
            entry.previewDangerRating);
    }

    private static void CopyMerchantOffers(SpecialDungeonTileEntry entry, BoardTileData tile)
    {
        if (entry == null || entry.merchantOffers == null || tile == null)
            return;

        for (int i = 0; i < entry.merchantOffers.Count; i++)
        {
            SpecialDungeonMerchantOfferEntry offer = entry.merchantOffers[i];
            if (offer == null)
                continue;

            tile.merchantOffers.Add(new MerchantOfferState
            {
                itemType = offer.itemType,
                lootItem = offer.lootItem,
                weaponData = offer.weaponData,
                blueprintData = offer.blueprintData,
                relicData = offer.relicData,
                price = Mathf.Max(0, offer.price)
            });
        }

        if (tile.merchantOffers.Count > 0)
            tile.merchantStockGenerated = true;
    }

    private static void CopyTreasureOptions(SpecialDungeonTileEntry entry, BoardTileData tile)
    {
        if (entry == null || entry.treasureOptions == null || tile == null)
            return;

        for (int i = 0; i < entry.treasureOptions.Count; i++)
        {
            SpecialDungeonTreasureOptionEntry option = entry.treasureOptions[i];
            if (option != null)
                tile.specialDungeonTreasureOptions.Add(option);
        }
    }

    private static MapDangerBand ResolveDangerBand(int entryIndex)
    {
        if (entryIndex <= 1)
            return MapDangerBand.Outer;
        if (entryIndex <= 3)
            return MapDangerBand.Mid;
        if (entryIndex <= 5)
            return MapDangerBand.Inner;
        return MapDangerBand.Core;
    }
}
