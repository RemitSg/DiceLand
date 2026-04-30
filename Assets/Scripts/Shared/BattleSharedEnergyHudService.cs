using UnityEngine;

public static class BattleSharedEnergyHudService
{
    public static string BuildEnergyText(int currentSharedEnergy, int reservedSharedEnergy, int maxSharedEnergy)
    {
        int availableEnergy = Mathf.Max(0, currentSharedEnergy - reservedSharedEnergy);
        return $"能量 {availableEnergy} / {Mathf.Max(0, maxSharedEnergy)}";
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
}
