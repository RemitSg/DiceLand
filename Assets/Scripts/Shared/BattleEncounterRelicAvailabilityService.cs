public static class BattleEncounterRelicAvailabilityService
{
    public static bool HasBattleRelics(BattleEncounterData encounter)
    {
        return encounter != null
            && encounter.relicInventory != null
            && encounter.relicInventory.Count > 0;
    }
}
