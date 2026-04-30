public static class BattleAllySkillEnergyAvailabilityService
{
    public static bool HasEnoughEnergyForSkill(
        BattleAllyController ally,
        EnemySkill_SO skill,
        int currentSharedEnergy,
        int reservedSharedEnergy)
    {
        if (ally == null || skill == null)
            return false;

        int connectedButtonCount = BattleSharedEnergyReservationCollectorService.CalculateConnectedAllyButtonCount(ally);
        int availableEnergyForAlly = BattleSharedEnergyReservationService.CalculateAvailableEnergyForAlly(
            currentSharedEnergy,
            reservedSharedEnergy,
            BattleSharedEnergyReservationCollectorService.CalculateReservedEnergyForAlly(ally));

        return BattleSharedEnergyReservationService.HasEnoughEnergyForSkill(
            availableEnergyForAlly,
            skill.energyCost,
            connectedButtonCount);
    }
}
