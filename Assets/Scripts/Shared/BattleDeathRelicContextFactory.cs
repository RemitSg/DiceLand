public static class BattleDeathRelicContextFactory
{
    public static RelicExecutionContext CreatePlayerDeath(
        BattleAllyController defeatedAlly,
        BattleAllyController[] allies)
    {
        return CreatePlayerDeath(defeatedAlly, allies, null);
    }

    public static RelicExecutionContext CreatePlayerDeath(
        BattleAllyController defeatedAlly,
        BattleAllyController[] allies,
        BattleEnemyController[] enemies)
    {
        return RelicExecutionContext.CreatePlayerDeath(
            null,
            defeatedAlly,
            allies,
            enemies);
    }

    public static RelicExecutionContext CreateEnemyDeath(
        BattleEnemyController defeatedEnemy,
        BattleAllyController[] allies,
        BattleEnemyController[] enemies)
    {
        return RelicExecutionContext.CreateEnemyDeath(
            null,
            defeatedEnemy,
            allies,
            enemies);
    }
}
