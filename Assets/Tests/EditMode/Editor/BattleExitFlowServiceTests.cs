using NUnit.Framework;
using System.Reflection;

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

    [Test]
    public void SettlementDefeatReturnDecision_WhenNormalMapDefeatDoesNotRetry_DoesNotReturnToMap()
    {
        BattleEncounterRuntime.Clear();
        BattleEncounterRuntime.SetEncounter(new BattleEncounterData
        {
            wasVictory = false,
            restartBattleOnDefeat = false,
            sourceSceneName = "MapScene"
        });
        BattleEncounterRuntime.ConsumeEncounter();

        MethodInfo method = typeof(BattleSettlementOverlay).GetMethod(
            "ShouldReturnToMapAfterDefeat",
            BindingFlags.NonPublic | BindingFlags.Static);

        Assert.IsNotNull(method);
        Assert.IsFalse((bool)method.Invoke(null, null));

        BattleEncounterRuntime.Clear();
    }

    [Test]
    public void SettlementDefeatReturnDecision_WhenEncounterExplicitlyReturnsToMap_ReturnsToMap()
    {
        BattleEncounterRuntime.Clear();
        BattleEncounterRuntime.SetEncounter(new BattleEncounterData
        {
            wasVictory = false,
            restartBattleOnDefeat = false,
            returnToMapOnDefeat = true,
            sourceSceneName = "MapScene"
        });
        BattleEncounterRuntime.ConsumeEncounter();

        MethodInfo method = typeof(BattleSettlementOverlay).GetMethod(
            "ShouldReturnToMapAfterDefeat",
            BindingFlags.NonPublic | BindingFlags.Static);

        Assert.IsNotNull(method);
        Assert.IsTrue((bool)method.Invoke(null, null));

        BattleEncounterRuntime.Clear();
    }
}
