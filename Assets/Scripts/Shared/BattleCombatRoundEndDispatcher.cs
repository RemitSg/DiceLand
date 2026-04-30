using System;

public static class BattleCombatRoundEndDispatcher
{
    public static void Dispatch(
        BattleAllyController[] allies,
        BattleEnemyController[] enemies,
        Action<BattleAllyController> finalizeAllySkillHistory,
        Action logRoundSummary,
        Action<BattleAllyController[], BattleEnemyController[]> applyRoundEndStatuses,
        Action<BattleAllyController[], BattleEnemyController[]> applyRoundEndRelics)
    {
        if (allies != null && finalizeAllySkillHistory != null)
        {
            foreach (BattleAllyController ally in allies)
            {
                if (ally != null)
                    finalizeAllySkillHistory(ally);
            }
        }

        logRoundSummary?.Invoke();
        applyRoundEndStatuses?.Invoke(allies, enemies);
        applyRoundEndRelics?.Invoke(allies, enemies);
    }

    public static void DispatchRoundEndStatusEffects(
        BattleAllyController[] allies,
        BattleEnemyController[] enemies,
        Action<BattleAllyController> consumeAllyRoundEndStatuses,
        Action<BattleEnemyController> consumeEnemyRoundEndStatuses)
    {
        if (allies != null)
        {
            for (int i = 0; i < allies.Length; i++)
            {
                BattleAllyController ally = allies[i];
                if (ally == null || ally.IsDead)
                    continue;

                consumeAllyRoundEndStatuses?.Invoke(ally);
            }
        }

        if (enemies != null)
        {
            for (int i = 0; i < enemies.Length; i++)
            {
                BattleEnemyController enemy = enemies[i];
                if (enemy == null || enemy.IsDead)
                    continue;

                consumeEnemyRoundEndStatuses?.Invoke(enemy);
            }
        }
    }
}
