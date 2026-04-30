using NUnit.Framework;

public class BattleDeathRelicContextFactoryTests
{
    [Test]
    public void CreatePlayerDeath_UsesPlayerDeathTrigger()
    {
        RelicExecutionContext context = BattleDeathRelicContextFactory.CreatePlayerDeath(null, null);

        Assert.AreEqual(Relic_SO.RelicTrigger.OnPlayerDeath, context.trigger);
        Assert.IsTrue(context.sourceIsPlayer);
    }

    [Test]
    public void CreateEnemyDeath_UsesEnemyDeathTrigger()
    {
        RelicExecutionContext context = BattleDeathRelicContextFactory.CreateEnemyDeath(null, null, null);

        Assert.AreEqual(Relic_SO.RelicTrigger.OnEnemyDeath, context.trigger);
        Assert.IsFalse(context.sourceIsPlayer);
    }
}
