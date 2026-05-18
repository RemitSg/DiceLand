using UnityEngine;

public readonly struct BattleSharedEnergySpendResult
{
    public BattleSharedEnergySpendResult(
        int currentSharedEnergy,
        int temporarySharedEnergy,
        int reservedSharedEnergy,
        int spentSharedEnergy,
        int spentTemporarySharedEnergy)
    {
        CurrentSharedEnergy = Mathf.Max(0, currentSharedEnergy);
        TemporarySharedEnergy = Mathf.Max(0, temporarySharedEnergy);
        ReservedSharedEnergy = Mathf.Max(0, reservedSharedEnergy);
        SpentSharedEnergy = Mathf.Max(0, spentSharedEnergy);
        SpentTemporarySharedEnergy = Mathf.Max(0, spentTemporarySharedEnergy);
    }

    public int CurrentSharedEnergy { get; }
    public int TemporarySharedEnergy { get; }
    public int ReservedSharedEnergy { get; }
    public int SpentSharedEnergy { get; }
    public int SpentTemporarySharedEnergy { get; }
}

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
        return CalculateAvailableEnergyForAlly(
            currentSharedEnergy,
            temporarySharedEnergy: 0,
            reservedSharedEnergy,
            allyReservedEnergy);
    }

    public static int CalculateAvailableEnergyForAlly(
        int currentSharedEnergy,
        int temporarySharedEnergy,
        int reservedSharedEnergy,
        int allyReservedEnergy)
    {
        int otherReservedEnergy = Mathf.Max(0, reservedSharedEnergy - Mathf.Max(0, allyReservedEnergy));
        return Mathf.Max(0, CalculateTotalAvailableEnergy(currentSharedEnergy, temporarySharedEnergy) - otherReservedEnergy);
    }

    public static int CalculateTotalAvailableEnergy(int currentSharedEnergy, int temporarySharedEnergy)
    {
        return Mathf.Max(0, currentSharedEnergy) + Mathf.Max(0, temporarySharedEnergy);
    }

    public static bool HasEnoughEnergyForSkill(
        int availableEnergyForAlly,
        int energyCost,
        int connectedButtonCount)
    {
        return CalculateSkillSelectionRequirement(connectedButtonCount, energyCost) <= Mathf.Max(0, availableEnergyForAlly);
    }

    public static BattleSharedEnergySpendResult SpendReservedEnergy(
        int currentSharedEnergy,
        int temporarySharedEnergy,
        int reservedSharedEnergy,
        int maxSharedEnergy)
    {
        int normal = Mathf.Clamp(currentSharedEnergy, 0, Mathf.Max(0, maxSharedEnergy));
        int temporary = Mathf.Max(0, temporarySharedEnergy);
        int reserved = Mathf.Clamp(Mathf.Max(0, reservedSharedEnergy), 0, normal + temporary);

        int spentTemporary = Mathf.Min(temporary, reserved);
        temporary -= spentTemporary;
        int spentNormal = reserved - spentTemporary;
        normal = Mathf.Clamp(normal - spentNormal, 0, Mathf.Max(0, maxSharedEnergy));

        return new BattleSharedEnergySpendResult(normal, temporary, 0, spentNormal, spentTemporary);
    }
}
