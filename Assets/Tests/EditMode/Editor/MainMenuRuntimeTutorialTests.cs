using NUnit.Framework;

public class MainMenuRuntimeTutorialTests
{
    [SetUp]
    public void SetUp()
    {
        MainMenuRuntime.ClearSelectedDifficulty();
        MainMenuRuntime.ClearContinueRequest();
        MainMenuRuntime.ClearTutorialRequest();
    }

    [Test]
    public void RequestTutorialStart_SetsTutorialRequestedFlag()
    {
        MainMenuRuntime.RequestTutorialStart();

        Assert.IsTrue(MainMenuRuntime.TutorialRequested);
        Assert.IsFalse(MainMenuRuntime.TutorialActive);
    }

    [Test]
    public void DefaultState_HasNoPendingTutorialRequest()
    {
        Assert.IsFalse(MainMenuRuntime.TutorialRequested);
        Assert.IsFalse(MainMenuRuntime.ConsumeTutorialRequest());
    }

    [Test]
    public void ConsumeTutorialRequest_ReturnsTrueOnceAndClearsFlag()
    {
        MainMenuRuntime.RequestTutorialStart();

        Assert.IsTrue(MainMenuRuntime.ConsumeTutorialRequest());
        Assert.IsFalse(MainMenuRuntime.ConsumeTutorialRequest());
        Assert.IsFalse(MainMenuRuntime.TutorialRequested);
        Assert.IsTrue(MainMenuRuntime.TutorialActive);
    }

    [Test]
    public void ClearTutorialRequest_ResetsTutorialFlagWithoutTouchingContinue()
    {
        MainMenuRuntime.RequestContinueFromSave();
        MainMenuRuntime.RequestTutorialStart();

        MainMenuRuntime.ClearTutorialRequest();

        Assert.IsFalse(MainMenuRuntime.TutorialRequested);
        Assert.IsFalse(MainMenuRuntime.TutorialActive);
        Assert.IsTrue(MainMenuRuntime.ConsumeContinueRequest());
    }
}
