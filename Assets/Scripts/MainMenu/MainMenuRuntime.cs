public static class MainMenuRuntime
{
    private static DifficultyConfig_SO selectedDifficultyConfig;

    public static DifficultyConfig_SO SelectedDifficultyConfig => selectedDifficultyConfig;

    public static void SetSelectedDifficulty(DifficultyConfig_SO config)
    {
        selectedDifficultyConfig = config;
    }

    public static void ClearSelectedDifficulty()
    {
        selectedDifficultyConfig = null;
    }
}