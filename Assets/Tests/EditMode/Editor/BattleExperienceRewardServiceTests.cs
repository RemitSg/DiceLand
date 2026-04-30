using NUnit.Framework;

public class BattleExperienceRewardServiceTests
{
    [Test]
    public void CalculateBaseReward_UsesHalfHpWithMinimumSixPerEnemy()
    {
        Assert.AreEqual(16, BattleExperienceRewardService.CalculateBaseReward(new[] { 4, 20 }));
    }

    [Test]
    public void ApplyTileMultiplier_EliteAndBossUseCurrentMultipliers()
    {
        Assert.AreEqual(15, BattleExperienceRewardService.ApplyTileMultiplier(10, MapTileType.Elite));
        Assert.AreEqual(25, BattleExperienceRewardService.ApplyTileMultiplier(10, MapTileType.Boss));
        Assert.AreEqual(10, BattleExperienceRewardService.ApplyTileMultiplier(10, MapTileType.Battle));
    }
}
