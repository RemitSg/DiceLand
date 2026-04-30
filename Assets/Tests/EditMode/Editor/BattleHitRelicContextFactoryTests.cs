using NUnit.Framework;

public class BattleHitRelicContextFactoryTests
{
    [Test]
    public void CreatePlayerHitEnemy_UsesHitTriggerAndEventKey()
    {
        RelicExecutionContext context = BattleHitRelicContextFactory.CreatePlayerHitEnemy(
            skillData: null,
            diceIndex: 2,
            dealtDamage: 7,
            sourceSpeed: 3,
            targetSpeed: 4,
            currentRound: 5,
            eventInstanceKey: "hit-key",
            source: null,
            target: null);

        Assert.AreEqual(Relic_SO.RelicTrigger.OnPlayerHitEnemy, context.trigger);
        Assert.IsTrue(context.sourceIsPlayer);
        Assert.AreEqual(2, context.diceIndex);
        Assert.AreEqual(7, context.resolvedDamage);
        Assert.AreEqual(3, context.incomingSourceSpeed);
        Assert.AreEqual(4, context.incomingTargetSpeed);
        Assert.AreEqual(5, context.currentRound);
        Assert.AreEqual("hit-key", context.eventInstanceKey);
    }

    [Test]
    public void CreatePlayerTookSingleAttack_UsesSingleAttackDamageTrigger()
    {
        RelicExecutionContext context = BattleHitRelicContextFactory.CreatePlayerTookSingleAttack(
            skillData: null,
            diceIndex: 1,
            dealtDamage: 6,
            sourceSpeed: 8,
            targetSpeed: 2,
            currentRound: 3,
            eventInstanceKey: "take-key",
            source: null,
            target: null);

        Assert.AreEqual(Relic_SO.RelicTrigger.OnPlayerTookSingleAttackDamage, context.trigger);
        Assert.IsFalse(context.sourceIsPlayer);
        Assert.AreEqual(6, context.resolvedDamage);
        Assert.AreEqual("take-key", context.eventInstanceKey);
    }

    [Test]
    public void CreateEnemyKilledByPlayer_UsesKillTrigger()
    {
        RelicExecutionContext context = BattleHitRelicContextFactory.CreateEnemyKilledByPlayer(
            skillData: null,
            diceIndex: 0,
            dealtDamage: 12,
            source: null,
            target: null);

        Assert.AreEqual(Relic_SO.RelicTrigger.OnEnemyKilledByPlayer, context.trigger);
        Assert.IsTrue(context.sourceIsPlayer);
        Assert.AreEqual(12, context.resolvedDamage);
    }

    [Test]
    public void CreatePlayerDefenseClashWin_UsesDefenseClashWinTrigger()
    {
        RelicExecutionContext context = BattleHitRelicContextFactory.CreatePlayerDefenseClashWin(
            skillData: null,
            diceIndex: 4,
            currentRound: 2,
            source: null,
            target: null);

        Assert.AreEqual(Relic_SO.RelicTrigger.OnPlayerDefenseClashWin, context.trigger);
        Assert.IsTrue(context.sourceIsPlayer);
        Assert.AreEqual(4, context.diceIndex);
        Assert.AreEqual(2, context.currentRound);
    }
}
