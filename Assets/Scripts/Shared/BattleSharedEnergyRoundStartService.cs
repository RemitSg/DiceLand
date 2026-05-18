using UnityEngine;

public readonly struct BattleSharedEnergyRoundStartState
{
    public BattleSharedEnergyRoundStartState(int currentSharedEnergy, int temporarySharedEnergy, int reservedSharedEnergy)
    {
        CurrentSharedEnergy = currentSharedEnergy;
        TemporarySharedEnergy = temporarySharedEnergy;
        ReservedSharedEnergy = reservedSharedEnergy;
    }

    public int CurrentSharedEnergy { get; }
    public int TemporarySharedEnergy { get; }
    public int ReservedSharedEnergy { get; }
}

public static class BattleSharedEnergyRoundStartService
{
    public static BattleSharedEnergyRoundStartState Resolve(int currentSharedEnergy, int currentSharedEnergyMax)
    {
        return Resolve(currentSharedEnergy, temporarySharedEnergy: 0, currentSharedEnergyMax);
    }

    public static BattleSharedEnergyRoundStartState Resolve(int currentSharedEnergy, int temporarySharedEnergy, int currentSharedEnergyMax)
    {
        int maxEnergy = Mathf.Max(0, currentSharedEnergyMax);
        int recoveredEnergy = Mathf.Clamp(currentSharedEnergy + 1, 0, maxEnergy);
        return new BattleSharedEnergyRoundStartState(recoveredEnergy, 0, 0);
    }
}
