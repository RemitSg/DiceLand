using UnityEngine;

public readonly struct BattleSharedEnergyHudSegments
{
    public BattleSharedEnergyHudSegments(float normalFillRatio, float temporaryFillRatio)
    {
        NormalFillRatio = Mathf.Clamp01(normalFillRatio);
        TemporaryFillRatio = Mathf.Clamp01(temporaryFillRatio);
    }

    public float NormalFillRatio { get; }
    public float TemporaryFillRatio { get; }
}

public static class BattleSharedEnergyHudService
{
    public static string BuildEnergyText(int currentSharedEnergy, int reservedSharedEnergy, int maxSharedEnergy)
    {
        int availableEnergy = Mathf.Max(0, currentSharedEnergy - reservedSharedEnergy);
        return $"能量 {availableEnergy} / {Mathf.Max(0, maxSharedEnergy)}";
    }

    public static string BuildEnergyText(
        int currentSharedEnergy,
        int temporarySharedEnergy,
        int reservedSharedEnergy,
        int maxSharedEnergy,
        string temporaryEnergyColor)
    {
        int availableEnergy = CalculateAvailableEnergy(currentSharedEnergy, temporarySharedEnergy, reservedSharedEnergy);
        int maxEnergy = Mathf.Max(0, maxSharedEnergy);
        if (temporarySharedEnergy > 0)
            return $"能量 <color={temporaryEnergyColor}>{availableEnergy}</color> / {maxEnergy}";

        return $"能量 {availableEnergy} / {maxEnergy}";
    }

    public static string BuildRoundText(int currentRound)
    {
        return $"第 {Mathf.Max(1, currentRound)} 回合";
    }

    public static float CalculateFillRatio(int currentSharedEnergy, int reservedSharedEnergy, int maxSharedEnergy)
    {
        int availableEnergy = Mathf.Max(0, currentSharedEnergy - reservedSharedEnergy);
        return maxSharedEnergy > 0
            ? Mathf.Clamp01((float)availableEnergy / maxSharedEnergy)
            : 0f;
    }

    public static BattleSharedEnergyHudSegments CalculateSegmentRatios(
        int currentSharedEnergy,
        int temporarySharedEnergy,
        int reservedSharedEnergy,
        int maxSharedEnergy)
    {
        CalculateAvailableEnergySegments(
            currentSharedEnergy,
            temporarySharedEnergy,
            reservedSharedEnergy,
            out int normalEnergy,
            out int temporaryEnergy);

        int denominator = Mathf.Max(Mathf.Max(0, maxSharedEnergy), normalEnergy + temporaryEnergy);
        if (denominator <= 0)
            return new BattleSharedEnergyHudSegments(0f, 0f);

        return new BattleSharedEnergyHudSegments(
            (float)normalEnergy / denominator,
            (float)temporaryEnergy / denominator);
    }

    private static int CalculateAvailableEnergy(int currentSharedEnergy, int temporarySharedEnergy, int reservedSharedEnergy)
    {
        CalculateAvailableEnergySegments(
            currentSharedEnergy,
            temporarySharedEnergy,
            reservedSharedEnergy,
            out int normalEnergy,
            out int temporaryEnergy);
        return normalEnergy + temporaryEnergy;
    }

    private static void CalculateAvailableEnergySegments(
        int currentSharedEnergy,
        int temporarySharedEnergy,
        int reservedSharedEnergy,
        out int normalEnergy,
        out int temporaryEnergy)
    {
        int normal = Mathf.Max(0, currentSharedEnergy);
        int temporary = Mathf.Max(0, temporarySharedEnergy);
        int reserved = Mathf.Clamp(Mathf.Max(0, reservedSharedEnergy), 0, normal + temporary);

        int reservedFromTemporary = Mathf.Min(temporary, reserved);
        temporaryEnergy = temporary - reservedFromTemporary;
        normalEnergy = Mathf.Max(0, normal - (reserved - reservedFromTemporary));
    }
}
