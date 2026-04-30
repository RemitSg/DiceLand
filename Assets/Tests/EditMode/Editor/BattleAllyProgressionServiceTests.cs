using NUnit.Framework;

public class BattleAllyProgressionServiceTests
{
    [Test]
    public void Evaluate_AddsExperienceWithoutLevelUp()
    {
        BattleAllyProgressionState result = BattleAllyProgressionService.Evaluate(
            initialLevel: 1,
            initialExperience: 10,
            initialCurrentHP: 5,
            initialMaxHP: 20,
            gainedExperience: 20,
            resolveExperienceToNextLevel: _ => 100,
            resolveMaxHPForLevel: (_, fallback) => fallback);

        Assert.AreEqual(1, result.FinalLevel);
        Assert.AreEqual(30, result.FinalExperience);
        Assert.AreEqual(20, result.FinalMaxHP);
        Assert.AreEqual(5, result.FinalCurrentHP);
    }

    [Test]
    public void Evaluate_LevelsUpAndHealsHalfOfUpgradedMaxHpWhenAlive()
    {
        BattleAllyProgressionState result = BattleAllyProgressionService.Evaluate(
            initialLevel: 1,
            initialExperience: 90,
            initialCurrentHP: 5,
            initialMaxHP: 20,
            gainedExperience: 10,
            resolveExperienceToNextLevel: _ => 100,
            resolveMaxHPForLevel: (level, _) => level == 2 ? 25 : 20);

        Assert.AreEqual(2, result.FinalLevel);
        Assert.AreEqual(0, result.FinalExperience);
        Assert.AreEqual(25, result.FinalMaxHP);
        Assert.AreEqual(18, result.FinalCurrentHP);
    }

    [Test]
    public void Evaluate_DoesNotHealWhenInitialHpIsZero()
    {
        BattleAllyProgressionState result = BattleAllyProgressionService.Evaluate(
            initialLevel: 1,
            initialExperience: 90,
            initialCurrentHP: 0,
            initialMaxHP: 20,
            gainedExperience: 10,
            resolveExperienceToNextLevel: _ => 100,
            resolveMaxHPForLevel: (level, _) => level == 2 ? 25 : 20);

        Assert.AreEqual(0, result.FinalCurrentHP);
    }
}
