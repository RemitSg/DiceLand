using NUnit.Framework;

public class BattleSingleAttackDiceResolutionServiceTests
{
    [Test]
    public void Resolve_CombinesSkillUseAndActionBonusesBeforeSingleAttackResult()
    {
        BattleSingleAttackDiceResolution resolution = BattleSingleAttackDiceResolutionService.Resolve(
            basePower: 4,
            statusPowerModifier: 1,
            skillUseBonus: 2,
            actionBonus: 3,
            isDefenseSkill: false);

        Assert.AreEqual(4, resolution.BasePower);
        Assert.AreEqual(1, resolution.StatusPowerModifier);
        Assert.AreEqual(5, resolution.BonusPower);
        Assert.AreEqual(9, resolution.PowerInput.FinalPower);
        Assert.AreEqual(9, resolution.AttackResult.FinalPower);
        Assert.AreEqual("4+5=9", resolution.AttackResult.FormattedPower);
    }

    [Test]
    public void Resolve_DefenseSkillKeepsDisplayPowerButSuppressesHitPower()
    {
        BattleSingleAttackDiceResolution resolution = BattleSingleAttackDiceResolutionService.Resolve(
            basePower: 3,
            statusPowerModifier: 2,
            skillUseBonus: 1,
            actionBonus: 1,
            isDefenseSkill: true);

        Assert.IsTrue(resolution.AttackResult.IsDefenseOnly);
        Assert.AreEqual(5, resolution.AttackResult.FinalPower);
        Assert.AreEqual(0, resolution.AttackResult.HitPower);
        Assert.AreEqual(4, resolution.AttackResult.DisplayPowerModifier);
        Assert.AreEqual("3+2=5", resolution.AttackResult.FormattedPower);
    }
}
