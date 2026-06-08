using NUnit.Framework;

public class MapTutorialFlowServiceTests
{
    [Test]
    public void TryConsumeArrivalGuide_ShowsMerchantForgeAndEliteFinalOnce()
    {
        MapTutorialFlowState state = new MapTutorialFlowState();

        Assert.IsTrue(MapTutorialFlowService.TryConsumeArrivalGuide(state, MapTileType.Merchant, out MapTutorialGuideContent merchant));
        Assert.That(merchant.title, Does.Contain("商店"));
        Assert.That(merchant.body, Does.Contain("图纸"));
        Assert.That(merchant.body, Does.Contain("战利品"));

        Assert.IsFalse(MapTutorialFlowService.TryConsumeArrivalGuide(state, MapTileType.Merchant, out _));

        Assert.IsTrue(MapTutorialFlowService.TryConsumeArrivalGuide(state, MapTileType.Forge, out MapTutorialGuideContent forge));
        Assert.That(forge.title, Does.Contain("锻造"));
        Assert.That(forge.body, Does.Contain("图纸"));
        Assert.That(forge.body, Does.Contain("装备"));

        Assert.IsTrue(MapTutorialFlowService.TryConsumeArrivalGuide(state, MapTileType.Elite, out MapTutorialGuideContent elite));
        Assert.That(elite.body, Does.Contain("最后"));
        Assert.That(elite.body, Does.Contain("教程"));
        Assert.That(elite.body, Does.Contain("精英"));
        Assert.IsFalse(MapTutorialFlowService.TryConsumeArrivalGuide(state, MapTileType.Elite, out _));

        Assert.IsFalse(MapTutorialFlowService.TryConsumeArrivalGuide(state, MapTileType.Battle, out _));
        Assert.IsFalse(MapTutorialFlowService.TryConsumeArrivalGuide(state, MapTileType.Key, out _));
        Assert.IsFalse(MapTutorialFlowService.TryConsumeArrivalGuide(state, MapTileType.Boss, out _));
    }
}
