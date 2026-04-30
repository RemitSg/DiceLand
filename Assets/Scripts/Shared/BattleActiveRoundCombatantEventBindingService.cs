using System;
using System.Collections.Generic;

public static class BattleActiveRoundCombatantEventBindingService
{
    public static void BindDeathHandlers(
        IReadOnlyList<BattleAllyController> allies,
        IReadOnlyList<BattleEnemyController> enemies,
        Action<BattleAllyController> allyDied,
        Action<BattleEnemyController> enemyDied)
    {
        if (allies != null && allyDied != null)
        {
            for (int i = 0; i < allies.Count; i++)
            {
                BattleAllyController ally = allies[i];
                if (ally == null)
                    continue;

                ally.Died -= allyDied;
                ally.Died += allyDied;
            }
        }

        if (enemies != null && enemyDied != null)
        {
            for (int i = 0; i < enemies.Count; i++)
            {
                BattleEnemyController enemy = enemies[i];
                if (enemy == null)
                    continue;

                enemy.Died -= enemyDied;
                enemy.Died += enemyDied;
            }
        }
    }

    public static void UnbindDeathHandlers(
        IReadOnlyList<BattleAllyController> allies,
        IReadOnlyList<BattleEnemyController> enemies,
        Action<BattleAllyController> allyDied,
        Action<BattleEnemyController> enemyDied)
    {
        if (allies != null && allyDied != null)
        {
            for (int i = 0; i < allies.Count; i++)
            {
                BattleAllyController ally = allies[i];
                if (ally != null)
                    ally.Died -= allyDied;
            }
        }

        if (enemies != null && enemyDied != null)
        {
            for (int i = 0; i < enemies.Count; i++)
            {
                BattleEnemyController enemy = enemies[i];
                if (enemy != null)
                    enemy.Died -= enemyDied;
            }
        }
    }
}
