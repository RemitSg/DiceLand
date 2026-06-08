using System;
using System.Collections.Generic;

public static class BattleStatusDrivenStageTransitionDispatcher
{
    public static int Dispatch(
        IReadOnlyList<BattleEnemyController> transitionCandidates,
        BattleAllyController[] allies,
        BattleEnemyController[] enemies,
        Action<BattleEnemyController, int, int> logTransition)
    {
        if (transitionCandidates == null)
            return 0;

        int transitionCount = 0;
        for (int i = 0; i < transitionCandidates.Count; i++)
        {
            BattleEnemyController enemy = transitionCandidates[i];
            if (!BattleStatusFlowTargetService.ShouldAttemptStatusDrivenStageTransition(
                    enemy,
                    target => target.IsDead,
                    enemy != null && enemy.AppliedPendingStageTransitionThisRoundStart))
            {
                continue;
            }

            int previousStage = enemy.CurrentStageIndex;
            if (!enemy.TryApplyStatusDrivenStageTransition(allies, enemies))
                continue;

            transitionCount++;
            logTransition?.Invoke(enemy, previousStage, enemy.CurrentStageIndex);
        }

        return transitionCount;
    }
}
