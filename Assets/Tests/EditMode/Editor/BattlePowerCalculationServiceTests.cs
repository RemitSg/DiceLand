using NUnit.Framework;

public class BattlePowerCalculationServiceTests
{
    [Test]
    public void ClampFinalPower_AddsBonusAndNeverDropsBelowOne()
    {
        Assert.AreEqual(7, BattlePowerCalculationService.ClampFinalPower(5, 2));
        Assert.AreEqual(3, BattlePowerCalculationService.ClampFinalPower(5, -2));
        Assert.AreEqual(1, BattlePowerCalculationService.ClampFinalPower(2, -99));
    }

    [Test]
    public void FormatPowerWithBonus_ReturnsBaseOnlyWhenBonusIsZero()
    {
        Assert.AreEqual("5", BattlePowerCalculationService.FormatPowerWithBonus(5, 0));
    }

    [Test]
    public void FormatPowerWithBonus_ShowsSignedBonusAndClampedFinalPower()
    {
        Assert.AreEqual("5+2=7", BattlePowerCalculationService.FormatPowerWithBonus(5, 2));
        Assert.AreEqual("5-2=3", BattlePowerCalculationService.FormatPowerWithBonus(5, -2));
        Assert.AreEqual("2-99=1", BattlePowerCalculationService.FormatPowerWithBonus(2, -99));
    }
}
