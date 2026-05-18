using NUnit.Framework;
using UnityEngine;

public class SpecialDungeonRuntimeContextTests
{
    [Test]
    public void Clone_CopiesMainMapSnapshotReturnCoordAndDelayedCalamities()
    {
        SpecialDungeonRuntimeContext context = new SpecialDungeonRuntimeContext
        {
            isActive = true,
            activeDungeonId = "d1",
            returnEventCoord = new Vector2Int(3, 4),
            mainMapSnapshot = new MapRunSnapshot { sceneName = "Map" }
        };
        context.deferredCalamityTiers.Add(2);

        SpecialDungeonRuntimeContext clone = context.Clone();

        Assert.IsTrue(clone.isActive);
        Assert.AreEqual("d1", clone.activeDungeonId);
        Assert.AreEqual(new Vector2Int(3, 4), clone.returnEventCoord);
        Assert.AreEqual(1, clone.deferredCalamityTiers.Count);
        Assert.AreNotSame(context.mainMapSnapshot, clone.mainMapSnapshot);
    }

    [Test]
    public void Clone_StripsNestedSpecialDungeonContextFromMainMapSnapshot()
    {
        SpecialDungeonRuntimeContext context = new SpecialDungeonRuntimeContext
        {
            isActive = true,
            activeDungeonId = "d1",
            mainMapSnapshot = new MapRunSnapshot
            {
                sceneName = "Map",
                specialDungeonContext = new SpecialDungeonRuntimeContext
                {
                    isActive = true,
                    activeDungeonId = "nested"
                }
            }
        };

        SpecialDungeonRuntimeContext clone = context.Clone();

        Assert.IsNotNull(clone.mainMapSnapshot);
        Assert.IsNull(clone.mainMapSnapshot.specialDungeonContext);
    }

    [Test]
    public void ExitDungeon_RestoresMainMapEventTileAsResolved()
    {
        MapRunSnapshot main = new MapRunSnapshot
        {
            sceneName = "Map",
            runState = new RunState { playerGridPosition = new Vector2Int(4, 4) },
            boardSize = 6,
            spawnCoord = Vector2Int.zero,
            bossCoord = new Vector2Int(5, 5),
            selectedCoord = new Vector2Int(4, 4),
            actionPointBase = 6,
            actionPointCurrent = 3
        };
        main.tiles.Add(new BoardTileData { coord = new Vector2Int(4, 4), tileType = MapTileType.Event });
        SpecialDungeonRuntimeContext context = new SpecialDungeonRuntimeContext
        {
            isActive = true,
            returnEventCoord = new Vector2Int(4, 4),
            mainMapSnapshot = main
        };

        MapRunSnapshot restored = SpecialDungeonExitService.BuildReturnedMainMapSnapshot(context, new RunState());

        Assert.AreEqual(new Vector2Int(4, 4), restored.runState.playerGridPosition);
        Assert.IsTrue(restored.tiles[0].isResolved);
        Assert.IsNull(restored.tiles[0].assignedEvent);
    }
}
