using UnityEngine;

public static class MainMenuRuntime
{
    public static DifficultyConfig_SO SelectedDifficultyConfig { get; private set; }
    public static bool ContinueRequested { get; private set; }

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
}
