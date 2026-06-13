using UnityEngine;

public static class Enemy037To039EffectService
{
    public const int TideEnemyId = 38;
    public const int ThunderWolfEnemyId = 39;

    public const string ErodeStatusId = "erode";
    public const string VulnerableStatusId = "vulnerable";
    public const string CurrentStatusId = "current";
    public const string TemporaryStrengthStatusId = "strength";

    public static bool IsEveryThirdRoundFromThree(int round)
    {
        return round >= 3 && (round - 3) % 3 == 0;
    }

    public static bool ShouldGrantElectrophileStrength(int currentStacks)
    {
        return currentStacks >= 10;
    }

    public static bool ShouldApplyUndercurrent(float roll)
    {
        return roll < 0.5f;
    }

    public static bool ShouldApplyUndercurrent()
    {
        return ShouldApplyUndercurrent(Random.value);
    }
}
