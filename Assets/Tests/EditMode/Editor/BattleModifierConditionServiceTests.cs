using NUnit.Framework;

public class BattleModifierConditionServiceTests
{
    [Test]
    public void IsConditionMet_SpeedDifferenceConditionsAreInclusiveAndZeroFallsBackToOne()
    {
        Assert.IsFalse(BattleModifierConditionService.IsConditionMet(
            EnemySkill_SO.PowerModifierCondition.SelfSpeedHigherThanTarget,
            selfSpeed: 5,
            targetSpeed: 5,
            requiredSpeedDifference: 0));

        Assert.IsTrue(BattleModifierConditionService.IsConditionMet(
            EnemySkill_SO.PowerModifierCondition.SelfSpeedHigherThanTarget,
            selfSpeed: 6,
            targetSpeed: 5,
            requiredSpeedDifference: 0));

        Assert.IsTrue(BattleModifierConditionService.IsConditionMet(
            EnemySkill_SO.PowerModifierCondition.SelfSpeedLowerThanTarget,
            selfSpeed: 4,
            targetSpeed: 6,
            requiredSpeedDifference: 2));
    }

    [Test]
    public void IsConditionMet_ExactSpeedAndFixedValueConditionsUseLegacyInclusiveRules()
    {
        Assert.IsTrue(BattleModifierConditionService.IsConditionMet(
            EnemySkill_SO.PowerModifierCondition.SelfSpeedEqualToTarget,
            selfSpeed: 4,
            targetSpeed: 4));

        Assert.IsTrue(BattleModifierConditionService.IsConditionMet(
            EnemySkill_SO.PowerModifierCondition.SelfSpeedHigherThanValue,
            selfSpeed: 5,
            targetSpeed: 1,
            requiredSpeedValue: 5));

        Assert.IsTrue(BattleModifierConditionService.IsConditionMet(
            EnemySkill_SO.PowerModifierCondition.SelfSpeedLowerThanValue,
            selfSpeed: 3,
            targetSpeed: 8,
            requiredSpeedValue: 3));
    }

    [Test]
    public void IsConditionMet_NonSpeedConditionReturnsFalse()
    {
        Assert.IsFalse(BattleModifierConditionService.IsConditionMet(
            EnemySkill_SO.PowerModifierCondition.TargetHasStatus,
            selfSpeed: 99,
            targetSpeed: 1));
    }
}
