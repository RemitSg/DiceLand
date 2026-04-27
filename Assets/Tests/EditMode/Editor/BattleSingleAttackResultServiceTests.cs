using NUnit.Framework;

public class BattleSingleAttackResultServiceTests
{
    [Test]
    public void Resolve_AttackSkillBuildsFinalPowerAndDisplayModifier()
    {
        BattleSingleAttackResult result = BattleSingleAttackResultService.Resolve(
            basePower: 4,
            bonusPower: 3,
            statusPowerModifier: 2,
            isDefenseSkill: false);

        Assert.IsFalse(result.IsDefenseOnly);
        Assert.IsTrue(result.ShouldDealDamage);
        Assert.AreEqual(7, result.FinalPower);
        Assert.AreEqual(7, result.HitPower);
        Assert.AreEqual(3, result.HitPowerModifier);
        Assert.AreEqual(5, result.DisplayPowerModifier);
        Assert.AreEqual("4+3=7", result.FormattedPower);
    }

    [Test]
    public void Resolve_NegativeBonusClampsFinalPowerToOne()
    {
        BattleSingleAttackResult result = BattleSingleAttackResultService.Resolve(
            basePower: 2,
            bonusPower: -10,
            statusPowerModifier: -1,
            isDefenseSkill: false);

        Assert.IsTrue(result.ShouldDealDamage);
        Assert.AreEqual(1, result.FinalPower);
        Assert.AreEqual(1, result.HitPower);
        Assert.AreEqual(-11, result.DisplayPowerModifier);
        Assert.AreEqual("2-10=1", result.FormattedPower);
    }

    [Test]
    public void Resolve_DefenseSkillDoesNotDealDamageButKeepsPreviewPower()
    {
        BattleSingleAttackResult result = BattleSingleAttackResultService.Resolve(
            basePower: 5,
            bonusPower: 1,
            statusPowerModifier: 4,
            isDefenseSkill: true);

        Assert.IsTrue(result.IsDefenseOnly);
        Assert.IsFalse(result.ShouldDealDamage);
        Assert.AreEqual(6, result.FinalPower);
        Assert.AreEqual(0, result.HitPower);
        Assert.AreEqual(0, result.HitPowerModifier);
        Assert.AreEqual(5, result.DisplayPowerModifier);
        Assert.AreEqual("5+1=6", result.FormattedPower);
    }
}
