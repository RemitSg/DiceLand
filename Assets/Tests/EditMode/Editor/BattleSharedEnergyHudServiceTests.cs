using NUnit.Framework;

public class BattleSharedEnergyHudServiceTests
{
    [Test]
    public void BuildEnergyText_UsesAvailableAndMaxEnergy()
    {
        Assert.AreEqual("能量 7 / 10", BattleSharedEnergyHudService.BuildEnergyText(10, 3, 10));
    }

    [Test]
    public void BuildRoundText_UsesCurrentRound()
    {
        Assert.AreEqual("第 3 回合", BattleSharedEnergyHudService.BuildRoundText(3));
    }

    [Test]
    public void CalculateFillRatio_ClampsAvailableEnergy()
    {
        Assert.AreEqual(0.7f, BattleSharedEnergyHudService.CalculateFillRatio(10, 3, 10));
        Assert.AreEqual(0f, BattleSharedEnergyHudService.CalculateFillRatio(0, 0, 0));
    }
}
