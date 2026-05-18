using System.Collections.Generic;

public sealed partial class RelicExecutionContext
{
    public static RelicExecutionContext CreateEnemyDeath(
        RunState runState,
        BattleEnemyController defeatedEnemy,
        BattleAllyController[] allies,
        BattleEnemyController[] enemies)
    {
        return new RelicExecutionContext
        {
            trigger = Relic_SO.RelicTrigger.OnEnemyDeath,
            runState = runState,
            sourceIsPlayer = false,
            target = defeatedEnemy,
            allies = allies,
            enemies = enemies,
            feedbackMessages = new List<string>()
        };
    }

    public static RelicExecutionContext CreatePlayerDeath(
        RunState runState,
        BattleAllyController defeatedAlly,
        BattleAllyController[] allies)
    {
        return CreatePlayerDeath(runState, defeatedAlly, allies, null);
    }

    public static RelicExecutionContext CreatePlayerDeath(
        RunState runState,
        BattleAllyController defeatedAlly,
        BattleAllyController[] allies,
        BattleEnemyController[] enemies)
    {
        return new RelicExecutionContext
        {
            trigger = Relic_SO.RelicTrigger.OnPlayerDeath,
            runState = runState,
            sourceIsPlayer = true,
            target = defeatedAlly,
            allies = allies,
            enemies = enemies,
            feedbackMessages = new List<string>()
        };
    }
}
