using NUnit.Framework;

public class BattlePresentationContinueInputServiceTests
{
    [Test]
    public void ShouldContinue_WhenMousePressedAfterWaitStarts_ReturnsTrue()
    {
        BattlePresentationContinueInputState state = BattlePresentationContinueInputService.CreateState(false);

        bool shouldContinue = BattlePresentationContinueInputService.ShouldContinue(ref state, true, true, false);

        Assert.IsTrue(shouldContinue);
    }

    [Test]
    public void ShouldContinue_WhenWaitStartsWhileMouseHeld_DoesNotConsumeHeldClick()
    {
        BattlePresentationContinueInputState state = BattlePresentationContinueInputService.CreateState(true);

        bool shouldContinue = BattlePresentationContinueInputService.ShouldContinue(ref state, true, true, false);

        Assert.IsFalse(shouldContinue);
    }

    [Test]
    public void ShouldContinue_WhenWaitStartsWhileMouseHeld_RequiresReleaseThenNextClick()
    {
        BattlePresentationContinueInputState state = BattlePresentationContinueInputService.CreateState(true);

        Assert.IsFalse(BattlePresentationContinueInputService.ShouldContinue(ref state, true, false, false));
        Assert.IsFalse(BattlePresentationContinueInputService.ShouldContinue(ref state, false, false, false));

        bool shouldContinue = BattlePresentationContinueInputService.ShouldContinue(ref state, true, true, false);

        Assert.IsTrue(shouldContinue);
    }

    [Test]
    public void ShouldContinue_WhenClickIsBlockedByUi_ReturnsFalse()
    {
        BattlePresentationContinueInputState state = BattlePresentationContinueInputService.CreateState(false);

        bool shouldContinue = BattlePresentationContinueInputService.ShouldContinue(ref state, true, true, true);

        Assert.IsFalse(shouldContinue);
    }

    [Test]
    public void IsEnemyDetailInteractionBlocked_WhenPresentationInteractionLockIsActive_ReturnsTrue()
    {
        Assert.IsFalse(BattlePresentationContinueInputService.IsEnemyDetailInteractionBlocked);

        using (BattlePresentationContinueInputService.BeginPresentationInteractionLock())
        {
            Assert.IsTrue(BattlePresentationContinueInputService.IsEnemyDetailInteractionBlocked);
        }

        Assert.IsFalse(BattlePresentationContinueInputService.IsEnemyDetailInteractionBlocked);
    }
}
