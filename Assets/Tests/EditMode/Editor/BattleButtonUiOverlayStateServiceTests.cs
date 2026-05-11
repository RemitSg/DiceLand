using NUnit.Framework;

public class BattleButtonUiOverlayStateServiceTests
{
    [Test]
    public void ResolveClickBlock_WhenSettlementVisible_BlocksBattleButton()
    {
        BattleButtonClickBlockResult result = BattleButtonUiOverlayStateService.ResolveClickBlock(
            settlementVisible: true,
            tutorialBlocking: false,
            skillInfoVisible: false);

        Assert.IsTrue(result.ShouldBlock);
        Assert.AreEqual(BattleButtonClickBlockReason.SettlementVisible, result.Reason);
    }

    [Test]
    public void ResolveClickBlock_WhenTutorialAndSkillInfoVisible_PrioritizesTutorial()
    {
        BattleButtonClickBlockResult result = BattleButtonUiOverlayStateService.ResolveClickBlock(
            settlementVisible: false,
            tutorialBlocking: true,
            skillInfoVisible: true);

        Assert.IsTrue(result.ShouldBlock);
        Assert.AreEqual(BattleButtonClickBlockReason.TutorialBlocking, result.Reason);
    }

    [Test]
    public void ResolveClickBlock_WhenPauseMenuVisible_BlocksBattleButton()
    {
        BattleButtonClickBlockResult result = BattleButtonUiOverlayStateService.ResolveClickBlock(
            settlementVisible: false,
            tutorialBlocking: false,
            skillInfoVisible: false,
            pauseMenuVisible: true);

        Assert.IsTrue(result.ShouldBlock);
        Assert.AreEqual(BattleButtonClickBlockReason.PauseMenuVisible, result.Reason);
    }

    [Test]
    public void ResolveClickBlock_WhenNothingVisible_AllowsBattleButton()
    {
        BattleButtonClickBlockResult result = BattleButtonUiOverlayStateService.ResolveClickBlock(
            settlementVisible: false,
            tutorialBlocking: false,
            skillInfoVisible: false);

        Assert.IsFalse(result.ShouldBlock);
        Assert.AreEqual(BattleButtonClickBlockReason.None, result.Reason);
    }
}
