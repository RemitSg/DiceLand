using NUnit.Framework;

public class Enemy037To039EffectServiceTests
{
    [Test]
    public void IsEveryThirdRoundFromThree_MatchesClosureSchedule()
    {
        Assert.IsFalse(Enemy037To039EffectService.IsEveryThirdRoundFromThree(1));
        Assert.IsTrue(Enemy037To039EffectService.IsEveryThirdRoundFromThree(3));
        Assert.IsFalse(Enemy037To039EffectService.IsEveryThirdRoundFromThree(4));
        Assert.IsTrue(Enemy037To039EffectService.IsEveryThirdRoundFromThree(6));
    }

    [Test]
    public void ShouldGrantElectrophileStrength_RequiresTenCurrentStacks()
    {
        Assert.IsFalse(Enemy037To039EffectService.ShouldGrantElectrophileStrength(9));
        Assert.IsTrue(Enemy037To039EffectService.ShouldGrantElectrophileStrength(10));
    }

    [Test]
    public void ShouldApplyUndercurrent_UsesHalfChance()
    {
        Assert.IsTrue(Enemy037To039EffectService.ShouldApplyUndercurrent(0.49f));
        Assert.IsFalse(Enemy037To039EffectService.ShouldApplyUndercurrent(0.5f));
    }
}
