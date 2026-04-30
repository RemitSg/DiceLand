using System.Collections.Generic;
using UnityEngine;

public static class BattleExperienceRewardService
{
    public static int CalculateBaseReward(IReadOnlyList<int> enemyBaseHpValues)
    {
        if (enemyBaseHpValues == null || enemyBaseHpValues.Count == 0)
            return 0;

        int baseReward = 0;
        for (int i = 0; i < enemyBaseHpValues.Count; i++)
            baseReward += Mathf.Max(6, enemyBaseHpValues[i] / 2);

        return baseReward;
    }

    public static int ApplyTileMultiplier(int baseReward, MapTileType tileType)
    {
        switch (tileType)
        {
            case MapTileType.Elite:
                return Mathf.RoundToInt(baseReward * 1.5f);
            case MapTileType.Boss:
                return Mathf.RoundToInt(baseReward * 2.5f);
            default:
                return baseReward;
        }
    }
}
