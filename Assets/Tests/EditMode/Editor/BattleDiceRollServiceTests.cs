using NUnit.Framework;

public class BattleDiceRollServiceTests
{
    [Test]
    public void ApplyRangeBonus_ClampsMinimumToOneAndMaximumAtLeastMinimum()
    {
        EnemySkill_SO.DicePowerRange result = BattleDiceRollService.ApplyRangeBonus(
            new EnemySkill_SO.DicePowerRange { minPower = 2, maxPower = 4 },
            minPowerBonus: -5,
            maxPowerBonus: -10);

        Assert.AreEqual(1, result.minPower);
        Assert.AreEqual(1, result.maxPower);
    }

    [Test]
    public void NormalizeRange_ClampsInvalidRanges()
    {
        EnemySkill_SO.DicePowerRange result = BattleDiceRollService.NormalizeRange(
            new EnemySkill_SO.DicePowerRange { minPower = -3, maxPower = -1 });

        Assert.AreEqual(1, result.minPower);
        Assert.AreEqual(1, result.maxPower);
    }

    [Test]
    public void RollPower_UsesNormalizedInclusiveRangeWithExclusiveRandomUpperBound()
    {
        int observedMin = -1;
        int observedExclusiveMax = -1;

        int rolled = BattleDiceRollService.RollPower(
            new EnemySkill_SO.DicePowerRange { minPower = 2, maxPower = 5 },
            (minInclusive, maxExclusive) =>
            {
                observedMin = minInclusive;
                observedExclusiveMax = maxExclusive;
                return 4;
            });

        Assert.AreEqual(4, rolled);
        Assert.AreEqual(2, observedMin);
        Assert.AreEqual(6, observedExclusiveMax);
    }

    [Test]
    public void RollPower_WhenForcedToMax_ReturnsNormalizedMaximumWithoutRandomRoll()
    {
        bool randomCalled = false;

        int rolled = BattleDiceRollService.RollPower(
            new EnemySkill_SO.DicePowerRange { minPower = 2, maxPower = 5 },
            BattleDiceRollService.ForcedRollMode.Max,
            (minInclusive, maxExclusive) =>
            {
                randomCalled = true;
                return minInclusive;
            });

        Assert.AreEqual(5, rolled);
        Assert.IsFalse(randomCalled);
    }

    [Test]
    public void RollPower_WhenForcedToMin_ReturnsNormalizedMinimumWithoutRandomRoll()
    {
        bool randomCalled = false;

        int rolled = BattleDiceRollService.RollPower(
            new EnemySkill_SO.DicePowerRange { minPower = -3, maxPower = -1 },
            BattleDiceRollService.ForcedRollMode.Min,
            (minInclusive, maxExclusive) =>
            {
                randomCalled = true;
                return maxExclusive;
            });

        Assert.AreEqual(1, rolled);
        Assert.IsFalse(randomCalled);
    }
}
