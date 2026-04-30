using UnityEngine;

public static class BattleSharedEnergyReservationService
{
    public static int CalculateSkillReservation(int connectedButtonCount, int energyCost)
    {
        return Mathf.Max(0, connectedButtonCount) * Mathf.Max(0, energyCost);
    }

    public static int CalculateSkillSelectionRequirement(int connectedButtonCount, int energyCost)
    {
        return Mathf.Max(1, connectedButtonCount) * Mathf.Max(0, energyCost);
    }

    public static int CalculateAvailableEnergyForAlly(
        int currentSharedEnergy,
        int reservedSharedEnergy,
        int allyReservedEnergy)
    {
        int otherReservedEnergy = Mathf.Max(0, reservedSharedEnergy - Mathf.Max(0, allyReservedEnergy));
        return Mathf.Max(0, currentSharedEnergy - otherReservedEnergy);
    }

    public static bool HasEnoughEnergyForSkill(
        int availableEnergyForAlly,
        int energyCost,
        int connectedButtonCount)
    {
        return CalculateSkillSelectionRequirement(connectedButtonCount, energyCost) <= Mathf.Max(0, availableEnergyForAlly);
    }
}
