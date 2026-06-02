using NUnit.Framework;
using UnityEngine;

public class SpecialDungeonBoardBuilderTests
{
    [Test]
    public void BuildBoard_AddsLeftExitSpawnConfiguredTilesAndRightExit()
    {
        SpecialDungeon_SO dungeon = ScriptableObject.CreateInstance<SpecialDungeon_SO>();
        dungeon.entries.Add(new SpecialDungeonTileEntry { tileType = MapTileType.Battle, previewTag = "Guard" });
        dungeon.entries.Add(new SpecialDungeonTileEntry { tileType = MapTileType.Event });

        SpecialDungeonBoardBuildResult result = SpecialDungeonBoardBuilder.Build(dungeon, 123);

        Assert.AreEqual(5, result.boardSize);
        Assert.AreEqual(new Vector2Int(1, 2), result.spawnCoord);
        Assert.AreEqual(new Vector2Int(4, 2), result.rightExitCoord);
        Assert.AreEqual(MapTileType.SpecialDungeonExit, result.tiles[0].tileType);
        Assert.AreEqual(MapTileType.Empty, result.tiles[1].tileType);
        Assert.IsTrue(result.tiles[1].isSpawnTile);
        Assert.AreEqual(MapTileType.Battle, result.tiles[2].tileType);
        Assert.AreEqual("Guard", result.tiles[2].previewTag);
        Assert.AreEqual(MapTileType.Event, result.tiles[3].tileType);
        Assert.AreEqual(MapTileType.SpecialDungeonExit, result.tiles[4].tileType);

        Object.DestroyImmediate(dungeon);
    }

    [Test]
    public void BuildBoard_MapsMerchantOffersAndTreasureOptionsToTileData()
    {
        SpecialDungeon_SO dungeon = ScriptableObject.CreateInstance<SpecialDungeon_SO>();
        dungeon.entries.Add(new SpecialDungeonTileEntry
        {
            tileType = MapTileType.Merchant,
            merchantOffers =
            {
                new SpecialDungeonMerchantOfferEntry { itemType = MerchantItemType.Loot, price = 7 }
            }
        });
        dungeon.entries.Add(new SpecialDungeonTileEntry
        {
            tileType = MapTileType.Treasure,
            treasureOptions =
            {
                new SpecialDungeonTreasureOptionEntry
                {
                    rewardType = SpecialDungeonTreasureRewardType.Money,
                    moneyAmount = 20
                }
            }
        });

        SpecialDungeonBoardBuildResult result = SpecialDungeonBoardBuilder.Build(dungeon, 123);

        Assert.That(result.tiles[2].merchantOffers, Has.Count.EqualTo(1));
        Assert.AreEqual(7, result.tiles[2].merchantOffers[0].price);
        Assert.That(result.tiles[3].specialDungeonTreasureOptions, Has.Count.EqualTo(1));
        Assert.AreEqual(20, result.tiles[3].specialDungeonTreasureOptions[0].moneyAmount);

        Object.DestroyImmediate(dungeon);
    }

    [Test]
    public void BuildBoard_AppliesConfiguredBattleDangerRatingBeforeEnemyGroupAndDefaults()
    {
        SpecialDungeon_SO dungeon = ScriptableObject.CreateInstance<SpecialDungeon_SO>();
        EnemyGroup_SO enemyGroup = ScriptableObject.CreateInstance<EnemyGroup_SO>();
        enemyGroup.dangerRatingOverride = 6;
        dungeon.entries.Add(new SpecialDungeonTileEntry
        {
            tileType = MapTileType.Battle,
            previewDangerRating = 7
        });
        dungeon.entries.Add(new SpecialDungeonTileEntry
        {
            tileType = MapTileType.Elite,
            previewDangerRating = 0,
            enemyGroup = enemyGroup
        });
        dungeon.entries.Add(new SpecialDungeonTileEntry
        {
            tileType = MapTileType.Battle,
            previewDangerRating = 0
        });

        SpecialDungeonBoardBuildResult result = SpecialDungeonBoardBuilder.Build(dungeon, 123);

        Assert.AreEqual(7, result.tiles[2].previewDangerRating);
        Assert.AreEqual(6, result.tiles[3].previewDangerRating);
        Assert.AreEqual(2, result.tiles[4].previewDangerRating);

        Object.DestroyImmediate(enemyGroup);
        Object.DestroyImmediate(dungeon);
    }
}
