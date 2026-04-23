using UnityEngine;

public static class MainMenuRuntime
{
    public static DifficultyConfig_SO SelectedDifficultyConfig { get; private set; }
    public static bool ContinueRequested { get; private set; }
    public static bool TutorialRequested { get; private set; }
    public static bool TutorialActive { get; private set; }
    public static string TutorialReturnSceneName { get; private set; }

    public static void SetSelectedDifficulty(DifficultyConfig_SO difficultyConfig)
    {
        SelectedDifficultyConfig = difficultyConfig;
    }

    public static void ClearSelectedDifficulty()
    {
        SelectedDifficultyConfig = null;
    }

    public static void RequestContinueFromSave()
    {
        ContinueRequested = true;
    }

    public static bool ConsumeContinueRequest()
    {
        bool requested = ContinueRequested;
        ContinueRequested = false;
        return requested;
    }

    public static void ClearContinueRequest()
    {
        ContinueRequested = false;
    }

    public static void RequestTutorialStart(string returnSceneName = null)
    {
        TutorialRequested = true;
        TutorialActive = false;
        TutorialReturnSceneName = returnSceneName;
    }

    public static bool ConsumeTutorialRequest()
    {
        bool requested = TutorialRequested;
        TutorialRequested = false;
        if (requested)
            TutorialActive = true;
        return requested;
    }

    public static void ClearTutorialRequest()
    {
        TutorialRequested = false;
        TutorialActive = false;
        TutorialReturnSceneName = null;
    }
}
