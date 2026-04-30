public static class BattleDeathRelicContextFactory
{
    public static RelicExecutionContext CreatePlayerDeath(
        BattleAllyController defeatedAlly,
        BattleAllyController[] allies)
    {
        return RelicExecutionContext.CreatePlayerDeath(
            null,
            defeatedAlly,
            allies);
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
