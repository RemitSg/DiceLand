using System;
using System.Collections.Generic;
using UnityEngine;

public static class RunRestService
{
    public static List<CharacterData_SO> GetRecruitCandidates(RunState state, Vector2Int coord, string logicalSceneName, CharacterRoster_SO roster, DifficultyConfig_SO difficulty)
    {
        if (state == null || roster == null || difficulty == null || state.recruitedMemberCount >= Mathf.Max(1, difficulty.maxRecruitCount))
            return new List<CharacterData_SO>();
        int seed = coord.x * 73856093 ^ coord.y * 19349663 ^ (logicalSceneName != null ? logicalSceneName.GetHashCode() : 0);
        return roster.GetRandomUnownedCharacters(state, 0, 2, new System.Random(seed));
    }

    public static int RecruitCost(RunState state, DifficultyConfig_SO difficulty)
    {
        return state != null && difficulty != null ? difficulty.GetRecruitCostForCurrentRecruitCount(state.recruitedMemberCount) : 0;
    }
}
