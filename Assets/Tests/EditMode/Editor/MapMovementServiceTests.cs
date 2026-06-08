using NUnit.Framework;

public class MapMovementServiceTests
{
    [Test]
    public void ShouldPresentPendingCalamityAfterArrival_ReturnsTrueForEmptyTile()
    {
        BoardTileData tile = new BoardTileData
        {
            tileType = MapTileType.Empty,
            isVisited = true,
            isResolved = true
        };

        Assert.IsTrue(MapMovementService.ShouldPresentPendingCalamityAfterArrival(tile));
    }

    [Test]
    public void ShouldPresentPendingCalamityAfterArrival_ReturnsFalseForOverlayTiles()
    {
        Assert.IsFalse(MapMovementService.ShouldPresentPendingCalamityAfterArrival(new BoardTileData { tileType = MapTileType.Event }));
        Assert.IsFalse(MapMovementService.ShouldPresentPendingCalamityAfterArrival(new BoardTileData { tileType = MapTileType.Merchant }));
        Assert.IsFalse(MapMovementService.ShouldPresentPendingCalamityAfterArrival(new BoardTileData { tileType = MapTileType.Forge }));
    }
}
