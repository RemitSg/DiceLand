using System;
using UnityEngine;

public sealed class BattleExitFlowPlan
{
    public string targetSceneName;
    public bool clearBattleEncounterRuntime;
    public bool clearMapRunRuntime;
    public bool clearTutorialRuntime;
    public bool unlockCombatInput;
}

public static class BattleExitFlowService
{
    public const string DefaultMainMenuSceneName = "Menu Selection";

    public static BattleExitFlowPlan BuildAbandonToMainMenuPlan(string mainMenuSceneName)
    {
        return new BattleExitFlowPlan
        {
            targetSceneName = ResolveMainMenuSceneName(mainMenuSceneName),
            clearBattleEncounterRuntime = true,
            clearMapRunRuntime = true,
            clearTutorialRuntime = true,
            unlockCombatInput = true
        };
    }

    public static string ResolveMainMenuSceneName(string mainMenuSceneName)
    {
        return string.IsNullOrWhiteSpace(mainMenuSceneName)
            ? DefaultMainMenuSceneName
            : mainMenuSceneName;
    }

    public static void ExecuteAbandonToMainMenu(
        string mainMenuSceneName,
        Action<string> transitionToScene,
        Action beforeTransition = null)
    {
        BattleExitFlowPlan plan = BuildAbandonToMainMenuPlan(mainMenuSceneName);
        ApplyPlan(plan, BattleEncounterRuntime.ActiveEncounter, beforeTransition, transitionToScene);
    }

    private static void ApplyPlan(
        BattleExitFlowPlan plan,
        BattleEncounterData activeEncounter,
        Action beforeTransition,
        Action<string> transitionToScene)
    {
        if (plan == null)
            return;

        if (plan.clearMapRunRuntime)
            MapRunRuntime.Clear(activeEncounter != null ? activeEncounter.sourceSceneName : null);

        if (plan.clearTutorialRuntime)
        {
            MainMenuRuntime.ClearTutorialRequest();
            TutorialBattleRuntime.Clear();
        }

        if (plan.clearBattleEncounterRuntime)
            BattleEncounterRuntime.Clear();

        if (plan.unlockCombatInput)
        {
            SpeedButton.CombatLocked = false;
            SpeedButton.InputSuppressed = false;
        }

        beforeTransition?.Invoke();

        if (transitionToScene != null)
            transitionToScene(plan.targetSceneName);
        else
            Debug.LogWarning("[BattleExitFlowService] Missing transition callback for abandoning battle.");
    }
}
