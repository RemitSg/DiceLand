using NUnit.Framework;

public class BattleCombatPhaseGuardServiceTests
{
    [Test]
    public void ShouldBlockInteraction_WhenSettlementOverlayVisible_ReturnsTrue()
    {
        Assert.IsTrue(BattleCombatPhaseGuardService.ShouldBlockInteraction(
            settlementOverlayVisible: true,
            skillInfoOverlayVisible: false,
            tutorialGuideOverlayVisible: false));
    }

    [Test]
    public void ShouldBlockInteraction_WhenSkillInfoOverlayVisible_ReturnsTrue()
    {
        Assert.IsTrue(BattleCombatPhaseGuardService.ShouldBlockInteraction(
            settlementOverlayVisible: false,
            skillInfoOverlayVisible: true,
            tutorialGuideOverlayVisible: false));
    }

    [Test]
    public void ShouldBlockInteraction_WhenTutorialGuideOverlayVisible_ReturnsTrue()
    {
        Assert.IsTrue(BattleCombatPhaseGuardService.ShouldBlockInteraction(
            settlementOverlayVisible: false,
            skillInfoOverlayVisible: false,
            tutorialGuideOverlayVisible: true));
    }

    [Test]
    public void ShouldBlockInteraction_WhenNoBlockingOverlayVisible_ReturnsFalse()
    {
        Assert.IsFalse(BattleCombatPhaseGuardService.ShouldBlockInteraction(
            settlementOverlayVisible: false,
            skillInfoOverlayVisible: false,
            tutorialGuideOverlayVisible: false));
    }
}
