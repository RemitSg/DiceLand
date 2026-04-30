using UnityEngine;

public static class BattleEncounterDropContextService
{
    public static bool ShouldUseEliteRelicChoice(BattleEncounterData encounter)
    {
        return encounter != null && encounter.tileType == MapTileType.Elite;
    }

    public static int ResolveRelicChoiceCount(int configuredChoiceCount)
    {
        return Mathf.Max(1, configuredChoiceCount);
    }
}
