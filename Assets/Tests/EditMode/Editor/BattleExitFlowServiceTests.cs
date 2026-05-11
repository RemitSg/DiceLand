using NUnit.Framework;

public class BattleExitFlowServiceTests
{
    [Test]
    public void BuildAbandonToMainMenuPlan_UsesDefaultMainMenuForBlankSceneName()
    {
        BattleExitFlowPlan plan = BattleExitFlowService.BuildAbandonToMainMenuPlan(" ");

        Assert.AreEqual("Menu Selection", plan.targetSceneName);
        Assert.IsTrue(plan.clearBattleEncounterRuntime);
        Assert.IsTrue(plan.clearMapRunRuntime);
        Assert.IsTrue(plan.clearTutorialRuntime);
        Assert.IsTrue(plan.unlockCombatInput);
    }

    [Test]
    public void BuildAbandonToMainMenuPlan_UsesConfiguredMainMenuScene()
    {
        BattleExitFlowPlan plan = BattleExitFlowService.BuildAbandonToMainMenuPlan("Custom Menu");

        Assert.AreEqual("Custom Menu", plan.targetSceneName);
    }
}
