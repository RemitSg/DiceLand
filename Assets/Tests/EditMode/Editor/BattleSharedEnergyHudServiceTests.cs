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

    [Test]
    public void BuildEnergyText_ColorsCurrentEnergyWhenTemporaryEnergyExists()
    {
        string text = BattleSharedEnergyHudService.BuildEnergyText(
            currentSharedEnergy: 8,
            temporarySharedEnergy: 3,
            reservedSharedEnergy: 0,
            maxSharedEnergy: 10,
            temporaryEnergyColor: "#A45CFF");

        Assert.AreEqual("能量 <color=#A45CFF>11</color> / 10", text);
    }

    [Test]
    public void CalculateSegmentRatios_UsesMaxEnergyWhenTotalDoesNotOverflow()
    {
        BattleSharedEnergyHudSegments segments = BattleSharedEnergyHudService.CalculateSegmentRatios(
            currentSharedEnergy: 5,
            temporarySharedEnergy: 3,
            reservedSharedEnergy: 0,
            maxSharedEnergy: 10);

        Assert.AreEqual(0.5f, segments.NormalFillRatio);
        Assert.AreEqual(0.3f, segments.TemporaryFillRatio);
    }

    [Test]
    public void CalculateSegmentRatios_CompressesSegmentsWhenTemporaryEnergyOverflowsMax()
    {
        BattleSharedEnergyHudSegments segments = BattleSharedEnergyHudService.CalculateSegmentRatios(
            currentSharedEnergy: 8,
            temporarySharedEnergy: 5,
            reservedSharedEnergy: 0,
            maxSharedEnergy: 10);

        Assert.AreEqual(8f / 13f, segments.NormalFillRatio);
        Assert.AreEqual(5f / 13f, segments.TemporaryFillRatio);
    }
}
