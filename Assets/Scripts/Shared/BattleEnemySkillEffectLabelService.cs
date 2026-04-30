public static class BattleEnemySkillEffectLabelService
{
    public static string BuildLabel(string effectName, string fallbackPrefix, int fallbackIndex)
    {
        return string.IsNullOrWhiteSpace(effectName)
            ? $"{fallbackPrefix}#{fallbackIndex + 1}"
            : effectName;
    }
}
