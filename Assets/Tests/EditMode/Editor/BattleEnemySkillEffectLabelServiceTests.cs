using NUnit.Framework;

public class BattleEnemySkillEffectLabelServiceTests
{
    [Test]
    public void BuildLabel_UsesCustomNameWhenPresent()
    {
        Assert.AreEqual("Custom", BattleEnemySkillEffectLabelService.BuildLabel("Custom", "Fallback", 0));
    }

    [Test]
    public void BuildLabel_UsesFallbackPrefixAndOneBasedIndex()
    {
        Assert.AreEqual("内建效果#3", BattleEnemySkillEffectLabelService.BuildLabel("", "内建效果", 2));
    }
}
