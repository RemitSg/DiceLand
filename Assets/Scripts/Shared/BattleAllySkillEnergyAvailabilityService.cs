using UnityEngine;

public static class BattleAllySkillEnergyAvailabilityService
{
    public static bool HasEnoughEnergyForSkill(
        BattleAllyController ally,
        EnemySkill_SO skill,
        int currentSharedEnergy,
        int reservedSharedEnergy)
    {
        return HasEnoughEnergyForSkill(
            ally,
            skill,
            currentSharedEnergy,
            temporarySharedEnergy: 0,
            reservedSharedEnergy);
    }

    public static bool HasEnoughEnergyForSkill(
        BattleAllyController ally,
        EnemySkill_SO skill,
        int currentSharedEnergy,
        int temporarySharedEnergy,
        int reservedSharedEnergy)
    {
        if (ally == null || skill == null)
            return false;

        int connectedButtonCount = BattleSharedEnergyReservationCollectorService.CalculateConnectedAllyButtonCount(ally);
        int availableEnergyForAlly = BattleSharedEnergyReservationService.CalculateAvailableEnergyForAlly(
            currentSharedEnergy,
            temporarySharedEnergy,
            reservedSharedEnergy,
            BattleSharedEnergyReservationCollectorService.CalculateReservedEnergyForAlly(ally));

        return BattleSharedEnergyReservationService.HasEnoughEnergyForSkill(
            availableEnergyForAlly,
            skill.energyCost,
            connectedButtonCount);
    }

    public static bool HasEnoughEnergyForSkillOnButton(
        BattleAllyController ally,
        SpeedButton button,
        EnemySkill_SO skill,
        int currentSharedEnergy,
        int reservedSharedEnergy)
    {
        return HasEnoughEnergyForSkillOnButton(
            ally,
            button,
            skill,
            currentSharedEnergy,
            temporarySharedEnergy: 0,
            reservedSharedEnergy);
    }

    public static bool HasEnoughEnergyForSkillOnButton(
        BattleAllyController ally,
        SpeedButton button,
        EnemySkill_SO skill,
        int currentSharedEnergy,
        int temporarySharedEnergy,
        int reservedSharedEnergy)
    {
        if (ally == null || skill == null)
            return false;

        int allyReservedEnergy = BattleSharedEnergyReservationCollectorService.CalculateReservedEnergyForAlly(ally);
        int currentButtonReservedEnergy = BattleSharedEnergyReservationCollectorService.CalculateReservedEnergyForAllyButton(ally, button);
        int availableEnergyForAlly = BattleSharedEnergyReservationService.CalculateAvailableEnergyForAlly(
            currentSharedEnergy,
            temporarySharedEnergy,
            reservedSharedEnergy,
            allyReservedEnergy);

        return Mathf.Max(0, availableEnergyForAlly - Mathf.Max(0, allyReservedEnergy - currentButtonReservedEnergy)) >= Mathf.Max(0, skill.energyCost);
    }
}
