using NUnit.Framework;

public class BattleDamageModifierContextFactoryTests
{
    [Test]
    public void CreateIncomingAttack_UsesIncomingAttackTriggerAndClampsSpeeds()
    {
        RelicExecutionContext context = BattleDamageModifierContextFactory.CreateIncomingAttack(
            skillData: null,
            diceIndex: 2,
            incomingDamage: 9,
            sourceSpeed: -3,
            targetSpeed: 4,
            source: null,
            target: null);

        Assert.AreEqual(Relic_SO.RelicTrigger.OnPlayerAboutToTakeAttackDamage, context.trigger);
        Assert.IsFalse(context.sourceIsPlayer);
        Assert.AreEqual(2, context.diceIndex);
        Assert.AreEqual(9, context.incomingDamage);
        Assert.AreEqual(0, context.incomingSourceSpeed);
        Assert.AreEqual(4, context.incomingTargetSpeed);
    }

    [Test]
    public void CreateIncomingSingleAttack_UsesSingleAttackTrigger()
    {
        RelicExecutionContext context = BattleDamageModifierContextFactory.CreateIncomingSingleAttack(
            skillData: null,
            diceIndex: 1,
            incomingDamage: 7,
            sourceSpeed: 5,
            targetSpeed: 6,
            source: null,
            target: null);

        Assert.AreEqual(Relic_SO.RelicTrigger.OnPlayerAboutToTakeSingleAttackDamage, context.trigger);
        Assert.AreEqual(7, context.incomingDamage);
        Assert.AreEqual(5, context.incomingSourceSpeed);
        Assert.AreEqual(6, context.incomingTargetSpeed);
    }

    [Test]
    public void CreateOutgoing_UsesOutgoingTrigger()
    {
        RelicExecutionContext context = BattleDamageModifierContextFactory.CreateOutgoing(
            skillData: null,
            diceIndex: 3,
            outgoingDamage: 11,
            source: null,
            target: null);

        Assert.AreEqual(Relic_SO.RelicTrigger.OnPlayerAboutToDealDamage, context.trigger);
        Assert.IsTrue(context.sourceIsPlayer);
        Assert.AreEqual(3, context.diceIndex);
        Assert.AreEqual(11, context.outgoingDamage);
    }
}
