using System.Collections.Generic;

public static class BattleEncounterLootUtility
{
    private const int EncounterLootSeedSalt = 0x51A7;

    public static int ResolveRngSeed(int encounterSeed)
    {
        return encounterSeed ^ EncounterLootSeedSalt;
    }

    public static bool HasOverrideLootTables(IReadOnlyList<LootTable_SO> overrideLootTables)
    {
        return overrideLootTables != null && overrideLootTables.Count > 0;
    }
}
