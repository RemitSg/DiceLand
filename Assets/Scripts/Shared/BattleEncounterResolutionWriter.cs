using System.Collections.Generic;

public static class BattleEncounterResolutionWriter
{
    public static void ApplyCompletionResolution(
        BattleEncounterData encounter,
        bool victory,
        int experienceGained,
        int moneyGained,
        List<RunPartyMemberState> resolvedPartyMembers,
        List<RunLootState> lootRewards,
        List<RunRelicState> relicRewards)
    {
        if (encounter == null)
            return;

        encounter.wasVictory = victory;
        encounter.experienceGained = experienceGained;
        encounter.moneyGained = moneyGained;
        encounter.resolvedPartyMembers = resolvedPartyMembers;
        encounter.resolvedLoot = BattleRunStateCloneService.CloneLootStates(lootRewards);
        encounter.resolvedRelics = BattleRunStateCloneService.CloneRelicStates(relicRewards);
    }

    public static void ApplyForcedRetreatResolution(
        BattleEncounterData encounter,
        int experienceGained,
        int moneyGained,
        List<RunPartyMemberState> resolvedPartyMembers,
        List<RunLootState> lootRewards,
        List<RunRelicState> relicRewards,
        string subtitle,
        IReadOnlyList<LootTable_SO> settlementLootTables)
    {
        if (encounter == null)
            return;

        ApplyCompletionResolution(
            encounter,
            true,
            experienceGained,
            moneyGained,
            resolvedPartyMembers,
            lootRewards,
            relicRewards);
        encounter.resolutionOverride = new BattleEncounterResolutionOverride
        {
            forceVictory = true,
            subtitleOverride = subtitle,
            lootTables = BattleCollectionCloneService.CloneNonNullReferences(settlementLootTables)
        };
    }
}
