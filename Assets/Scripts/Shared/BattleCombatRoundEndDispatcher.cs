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
}
