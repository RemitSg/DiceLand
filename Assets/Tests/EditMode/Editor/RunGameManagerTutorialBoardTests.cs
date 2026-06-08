using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class RunGameManagerTutorialBoardTests
{
    [Test]
    public void BuildTutorialBoardTiles_CreatesShortMerchantForgeEliteRoute()
    {
        MethodInfo method = typeof(RunGameManager).GetMethod("BuildTutorialBoardTiles", BindingFlags.Static | BindingFlags.NonPublic);

        Assert.IsNotNull(method);

        List<BoardTileData> tiles = method.Invoke(null, new object[] { 6 }) as List<BoardTileData>;
        Assert.IsNotNull(tiles);
        Assert.AreEqual(6, tiles.Count);

        MapTileType[] expectedSequence =
        {
            MapTileType.Empty,
            MapTileType.Battle,
            MapTileType.Battle,
            MapTileType.Merchant,
            MapTileType.Forge,
            MapTileType.Elite
        };

        int expectedRow = 3;
        for (int i = 0; i < expectedSequence.Length; i++)
        {
            Assert.AreEqual(expectedSequence[i], tiles[i].tileType, $"Tile type mismatch at index {i}.");
            Assert.AreEqual(new Vector2Int(i, expectedRow), tiles[i].coord, $"Coordinate mismatch at index {i}.");
        }

        Assert.IsTrue(tiles[0].isSpawnTile);
        Assert.IsTrue(tiles[0].isVisited);
        Assert.IsTrue(tiles[0].isResolved);

        Assert.AreEqual(1, tiles[1].previewEnemyCount);
        Assert.AreEqual(1, tiles[1].previewDangerRating);
        Assert.AreEqual("教程战斗", tiles[1].previewTag);

        Assert.AreEqual(0, tiles[3].previewEnemyCount);
        Assert.AreEqual("教程商店", tiles[3].previewTag);

        Assert.AreEqual(1, tiles[5].previewEnemyCount);
        Assert.AreEqual(2, tiles[5].previewDangerRating);
        Assert.AreEqual("教程终战", tiles[5].previewTag);
    }

    [Test]
    public void RunGameManager_ExposesDedicatedTutorialStartingPartyConfig()
    {
        FieldInfo field = typeof(RunGameManager).GetField("tutorialStartingPartyMembers", BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.IsNotNull(field);
        Assert.AreEqual(typeof(List<>), field.FieldType.GetGenericTypeDefinition());
    }
}
