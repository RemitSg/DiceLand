using NUnit.Framework;

public class MapTilePresentationServiceTests
{
    [Test]
    public void GetTileTypeLabel_ReturnsLocalizedMapTileNames()
    {
        Assert.AreEqual("空白", MapTilePresentationService.GetTileTypeLabel(MapTileType.Empty));
        Assert.AreEqual("战斗", MapTilePresentationService.GetTileTypeLabel(MapTileType.Battle));
        Assert.AreEqual("精英", MapTilePresentationService.GetTileTypeLabel(MapTileType.Elite));
        Assert.AreEqual("事件", MapTilePresentationService.GetTileTypeLabel(MapTileType.Event));
        Assert.AreEqual("商人", MapTilePresentationService.GetTileTypeLabel(MapTileType.Merchant));
        Assert.AreEqual("锻造", MapTilePresentationService.GetTileTypeLabel(MapTileType.Forge));
        Assert.AreEqual("休息", MapTilePresentationService.GetTileTypeLabel(MapTileType.Rest));
        Assert.AreEqual("钥匙", MapTilePresentationService.GetTileTypeLabel(MapTileType.Key));
        Assert.AreEqual("Boss", MapTilePresentationService.GetTileTypeLabel(MapTileType.Boss));
    }

    [Test]
    public void BuildUnavailableEnemyGroupMessage_CombinesTileAndDangerLabels()
    {
        string message = MapTilePresentationService.BuildUnavailableEnemyGroupMessage(MapTileType.Elite, MapDangerBand.Inner);

        Assert.AreEqual("精英 / 内层 没有可用敌人组。", message);
    }
}
