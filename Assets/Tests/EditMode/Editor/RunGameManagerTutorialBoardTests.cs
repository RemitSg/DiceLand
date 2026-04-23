using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class RunGameManagerTutorialBoardTests
{
    [Test]
    public void BuildTutorialBoardTiles_CreatesBattleHeavySevenLessonRoute()
    {
        MethodInfo method = typeof(RunGameManager).GetMethod("BuildTutorialBoardTiles", BindingFlags.Static | BindingFlags.NonPublic);

        Assert.IsNotNull(method);

        List<BoardTileData> tiles = method.Invoke(null, new object[] { 12 }) as List<BoardTileData>;
        Assert.IsNotNull(tiles);
        Assert.AreEqual(12, tiles.Count);

        MapTileType[] expectedSequence =
        {
            MapTileType.Empty,
            MapTileType.Empty,
            MapTileType.Battle,
            MapTileType.Battle,
            MapTileType.Rest,
            MapTileType.Battle,
            MapTileType.Merchant,
            MapTileType.Battle,
            MapTileType.Forge,
            MapTileType.Battle,
            MapTileType.Key,
            MapTileType.Boss
        };

        int expectedRow = 6;
        for (int i = 0; i < expectedSequence.Length; i++)
        {
            Assert.AreEqual(expectedSequence[i], tiles[i].tileType, $"Tile type mismatch at index {i}.");
            Assert.AreEqual(new Vector2Int(i, expectedRow), tiles[i].coord, $"Coordinate mismatch at index {i}.");
        }

        Assert.IsTrue(tiles[0].isSpawnTile);
        Assert.IsTrue(tiles[0].isVisited);
        Assert.IsTrue(tiles[0].isResolved);

        Assert.AreEqual(1, tiles[2].previewEnemyCount);
        Assert.AreEqual(1, tiles[2].previewDangerRating);
        Assert.AreEqual("教程战斗", tiles[2].previewTag);

        Assert.AreEqual(1, tiles[9].previewEnemyCount);
        Assert.AreEqual(4, tiles[9].previewDangerRating);
        Assert.AreEqual("教程战斗", tiles[9].previewTag);

        Assert.AreEqual(1, tiles[11].previewEnemyCount);
        Assert.AreEqual(5, tiles[11].previewDangerRating);
        Assert.AreEqual("教程终战", tiles[11].previewTag);
    }

    [Test]
    public void RunGameManager_ExposesDedicatedTutorialStartingPartyConfig()
    {
        FieldInfo field = typeof(RunGameManager).GetField("tutorialStartingPartyMembers", BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.IsNotNull(field);
        Assert.AreEqual(typeof(List<>), field.FieldType.GetGenericTypeDefinition());
    }
}
