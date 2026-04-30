using UnityEngine;

public readonly struct BattleSharedEnergyRoundStartState
{
    public BattleSharedEnergyRoundStartState(int currentSharedEnergy, int reservedSharedEnergy)
    {
        CurrentSharedEnergy = currentSharedEnergy;
        ReservedSharedEnergy = reservedSharedEnergy;
    }

    public int CurrentSharedEnergy { get; }
    public int ReservedSharedEnergy { get; }
}

public static class BattleSharedEnergyRoundStartService
{
    public static BattleSharedEnergyRoundStartState Resolve(int currentSharedEnergy, int currentSharedEnergyMax)
    {
        int maxEnergy = Mathf.Max(0, currentSharedEnergyMax);
        int recoveredEnergy = Mathf.Clamp(currentSharedEnergy + 1, 0, maxEnergy);
        return new BattleSharedEnergyRoundStartState(recoveredEnergy, 0);
    }
}
