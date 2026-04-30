using System.Collections.Generic;

public static class BattleRunStateCloneService
{
    public static List<RunLootState> CloneLootStates(List<RunLootState> source)
    {
        List<RunLootState> clone = new List<RunLootState>();
        if (source == null)
            return clone;

        for (int i = 0; i < source.Count; i++)
        {
            RunLootState loot = source[i];
            if (loot != null)
                clone.Add(loot.Clone());
        }

        return clone;
    }

    public static List<RunRelicState> CloneRelicStates(List<RunRelicState> source)
    {
        List<RunRelicState> clone = new List<RunRelicState>();
        if (source == null)
            return clone;

        for (int i = 0; i < source.Count; i++)
        {
            RunRelicState relic = source[i];
            if (relic != null)
                clone.Add(relic.Clone());
        }

        return clone;
    }
}
