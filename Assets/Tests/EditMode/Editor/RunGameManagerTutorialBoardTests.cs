using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class RunGameManagerTutorialBoardTests
{
    [Test]
    public void BuildTutorialBoardTiles_CreatesExpectedLinearTutorialRoute()
    {
        MethodInfo method = typeof(RunGameManager).GetMethod("BuildTutorialBoardTiles", BindingFlags.Static | BindingFlags.NonPublic);

        Assert.IsNotNull(method);

        List<BoardTileData> tiles = method.Invoke(null, new object[] { 11 }) as List<BoardTileData>;
        Assert.IsNotNull(tiles);
        Assert.AreEqual(10, tiles.Count);

        MapTileType[] expectedSequence =
        {
            MapTileType.Empty,
            MapTileType.Empty,
            MapTileType.Battle,
            MapTileType.Event,
            MapTileType.Rest,
            MapTileType.Merchant,
            MapTileType.Forge,
            MapTileType.Elite,
            MapTileType.Key,
            MapTileType.Boss
        };

        int expectedRow = 5;
        for (int i = 0; i < expectedSequence.Length; i++)
        {
            Assert.AreEqual(expectedSequence[i], tiles[i].tileType, $"Tile type mismatch at index {i}.");
            Assert.AreEqual(new Vector2Int(i, expectedRow), tiles[i].coord, $"Coordinate mismatch at index {i}.");
        }

        Assert.IsTrue(tiles[0].isSpawnTile);
        Assert.IsTrue(tiles[0].isVisited);
        Assert.IsTrue(tiles[0].isResolved);
        Assert.AreEqual(2, tiles[2].previewEnemyCount);
        Assert.AreEqual(3, tiles[7].previewDangerRating);
        Assert.AreEqual(1, tiles[9].previewEnemyCount);
        Assert.IsFalse(string.IsNullOrEmpty(tiles[9].previewTag));
    }
}
