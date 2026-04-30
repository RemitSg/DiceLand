using NUnit.Framework;

public class BattleDamageModifierCalculationServiceTests
{
    [Test]
    public void ApplyIncomingReduction_AppliesFlatThenPercentReduction()
    {
        int damage = BattleDamageModifierCalculationService.ApplyIncomingReduction(
            baseDamage: 10,
            flatModifier: 2,
            reductionPercent: 50);

        Assert.AreEqual(6, damage);
    }

    [Test]
    public void ApplyIncomingReduction_ClampsNegativeFlatAndOverReductionToZero()
    {
        int damage = BattleDamageModifierCalculationService.ApplyIncomingReduction(
            baseDamage: 4,
            flatModifier: -10,
            reductionPercent: 250);

        Assert.AreEqual(0, damage);
    }

    [Test]
    public void ApplyOutgoingBonus_AppliesFlatThenPercentBonus()
    {
        int damage = BattleDamageModifierCalculationService.ApplyOutgoingBonus(
            baseDamage: 10,
            flatModifier: 2,
            bonusPercent: 50);

        Assert.AreEqual(18, damage);
    }

    [Test]
    public void ApplyOutgoingBonus_IgnoresNegativeBonusPercentAndClampsDamage()
    {
        int damage = BattleDamageModifierCalculationService.ApplyOutgoingBonus(
            baseDamage: 3,
            flatModifier: -8,
            bonusPercent: -50);

        Assert.AreEqual(0, damage);
    }
}
