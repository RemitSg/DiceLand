using System.Collections.Generic;

public static class BattleRelicInventorySetBuilder
{
    public static HashSet<Relic_SO> BuildOwnedRelicSet(IReadOnlyList<RunRelicState> relicInventory)
    {
        HashSet<Relic_SO> ownedRelics = new HashSet<Relic_SO>();
        if (relicInventory == null)
            return ownedRelics;

        for (int i = 0; i < relicInventory.Count; i++)
        {
            RunRelicState relicState = relicInventory[i];
            if (relicState == null || relicState.relicData == null || relicState.amount <= 0)
                continue;

            ownedRelics.Add(relicState.relicData);
        }

        return ownedRelics;
    }
}
