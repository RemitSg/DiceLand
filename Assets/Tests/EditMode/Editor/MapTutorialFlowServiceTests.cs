using NUnit.Framework;
using System.Reflection;
using UnityEngine;

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
        Assert.AreEqual("终战", elite.title);
        Assert.AreEqual("这里就是最后一站了，迎接最后一道考验吧！", elite.body);
        Assert.IsFalse(MapTutorialFlowService.TryConsumeArrivalGuide(state, MapTileType.Elite, out _));

        Assert.IsFalse(MapTutorialFlowService.TryConsumeArrivalGuide(state, MapTileType.Battle, out _));
        Assert.IsFalse(MapTutorialFlowService.TryConsumeArrivalGuide(state, MapTileType.Key, out _));
        Assert.IsFalse(MapTutorialFlowService.TryConsumeArrivalGuide(state, MapTileType.Boss, out _));
    }

    [Test]
    public void RevisedMapSequenceContinuesDirectlyPastRemovedLessons()
    {
        float previousTime = Time.timeScale;
        var managerHost = new GameObject("RevisedTutorialManager");
        managerHost.SetActive(false);
        var overlayHost = new GameObject("RevisedTutorialOverlay");
        try
        {
            var manager = managerHost.AddComponent<RunGameManager>();
            var guide = overlayHost.AddComponent<MapTutorialGuideOverlay>();
            const BindingFlags fields = BindingFlags.Instance | BindingFlags.NonPublic;
            typeof(RunGameManager).GetField("mapTutorialGuideOverlay", fields).SetValue(manager, guide);
            typeof(RunGameManager).GetMethod("ShowMapMoneyGuide", fields).Invoke(manager, null);
            Assert.AreEqual("金币", TutorialForestView.GetText(guide.Presentation.Title));
            guide.Presentation.Primary.onClick.Invoke();
            Assert.AreEqual("查看格子详情", TutorialForestView.GetText(guide.Presentation.Title));

            typeof(RunGameManager).GetMethod("ContinueAfterBattleTileInspected", fields).Invoke(manager, null);
            foreach (string title in new[] { "格子详情", "坐标与类型", "移动消耗", "敌人属性", "危险度与区域" })
            {
                Assert.AreEqual(title, TutorialForestView.GetText(guide.Presentation.Title));
                guide.Presentation.Primary.onClick.Invoke();
            }
            Assert.AreEqual("进入战斗", TutorialForestView.GetText(guide.Presentation.Title));
        }
        finally
        {
            Object.DestroyImmediate(overlayHost);
            Object.DestroyImmediate(managerHost);
            Time.timeScale = previousTime;
        }
    }
}
