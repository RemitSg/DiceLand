using NUnit.Framework;

public class BattleStatusEffectEligibilityServiceTests
{
    [Test]
    public void ShouldProcess_ReturnsFalseWhenTriggerDoesNotMatch()
    {
        EnemySkill_SO.StatusEffectEntry effect = new EnemySkill_SO.StatusEffectEntry
        {
            trigger = EnemySkill_SO.DiceEffectTrigger.OnHit,
            stackAmount = 1,
            useCondition = false
        };

        Assert.IsFalse(BattleStatusEffectEligibilityService.ShouldProcess(
            effect,
            EnemySkill_SO.DiceEffectTrigger.OnMiss,
            conditionalOnly: false));
    }

    [Test]
    public void ShouldProcess_ReturnsFalseWhenStackAmountIsNotPositive()
    {
        EnemySkill_SO.StatusEffectEntry effect = new EnemySkill_SO.StatusEffectEntry
        {
            trigger = EnemySkill_SO.DiceEffectTrigger.OnHit,
            stackAmount = 0,
            useCondition = false
        };

        Assert.IsFalse(BattleStatusEffectEligibilityService.ShouldProcess(
            effect,
            EnemySkill_SO.DiceEffectTrigger.OnHit,
            conditionalOnly: false));
    }

    [Test]
    public void ShouldProcess_RequiresMatchingConditionalBucket()
    {
        EnemySkill_SO.StatusEffectEntry effect = new EnemySkill_SO.StatusEffectEntry
        {
            trigger = EnemySkill_SO.DiceEffectTrigger.OnHit,
            stackAmount = 1,
            useCondition = true
        };

        Assert.IsFalse(BattleStatusEffectEligibilityService.ShouldProcess(effect, EnemySkill_SO.DiceEffectTrigger.OnHit, false));
        Assert.IsTrue(BattleStatusEffectEligibilityService.ShouldProcess(effect, EnemySkill_SO.DiceEffectTrigger.OnHit, true));
    }
}
