using NUnit.Framework;

public class BattleDicePowerPreparationServiceTests
{
    [Test]
    public void Resolve_CombinesRelicSkillUseAndActionBonuses()
    {
        BattleDicePowerPreparation result = BattleDicePowerPreparationService.Resolve(
            basePower: 4,
            statusPowerModifier: 2,
            relicPowerBonus: 1,
            skillUsePowerBonus: 3,
            actionPowerBonus: 5);

        Assert.AreEqual(4, result.SkillUseBonus);
        Assert.AreEqual(5, result.ActionBonus);
        Assert.AreEqual(9, result.BonusPower);
        Assert.AreEqual(13, result.FinalPower);
        Assert.AreEqual(8, result.FollowUpBasePower);
        Assert.AreEqual(11, result.DisplayPowerModifier);
        Assert.AreEqual("4+9=13", result.FormattedPower);
    }

    [Test]
    public void Resolve_ClampsFinalAndFollowUpPowerIndependently()
    {
        BattleDicePowerPreparation result = BattleDicePowerPreparationService.Resolve(
            basePower: 2,
            statusPowerModifier: 0,
            relicPowerBonus: -5,
            skillUsePowerBonus: 1,
            actionPowerBonus: -10);

        Assert.AreEqual(-4, result.SkillUseBonus);
        Assert.AreEqual(-14, result.BonusPower);
        Assert.AreEqual(1, result.FinalPower);
        Assert.AreEqual(1, result.FollowUpBasePower);
        Assert.AreEqual("2-14=1", result.FormattedPower);
    }

    [Test]
    public void Resolve_UsesActionBonusOnlyForFinalPowerNotFollowUpBasePower()
    {
        BattleDicePowerPreparation result = BattleDicePowerPreparationService.Resolve(
            basePower: 5,
            statusPowerModifier: 0,
            relicPowerBonus: 0,
            skillUsePowerBonus: 2,
            actionPowerBonus: 7);

        Assert.AreEqual(14, result.FinalPower);
        Assert.AreEqual(7, result.FollowUpBasePower);
    }
}
