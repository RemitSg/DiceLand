using UnityEngine;

public static class BattleCombatantContextService
{
    public static bool TryCollectCombatants(
        GameObject alliesParent,
        GameObject enemiesParent,
        out BattleAllyController[] allies,
        out BattleEnemyController[] enemies,
        out string warningMessage)
    {
        allies = null;
        enemies = null;
        warningMessage = null;

        if (alliesParent == null || enemiesParent == null)
        {
            warningMessage = "[BattleButton] alliesParent 或 enemiesParent 未配置。";
            return false;
        }

        allies = alliesParent.GetComponentsInChildren<BattleAllyController>(false);
        enemies = enemiesParent.GetComponentsInChildren<BattleEnemyController>(false);
        return true;
    }
}
