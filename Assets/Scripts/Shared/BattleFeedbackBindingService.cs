using System;

public static class BattleFeedbackBindingService
{
    public static bool BindIfAvailable(
        BattleFeedbackSystem feedbackSystem,
        BattleAllyController[] allies,
        BattleEnemyController[] enemies)
    {
        if (feedbackSystem == null)
            return false;

        feedbackSystem.BindCombatants(
            allies ?? Array.Empty<BattleAllyController>(),
            enemies ?? Array.Empty<BattleEnemyController>());
        return true;
    }
}
