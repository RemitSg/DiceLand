using NUnit.Framework;

public class BattleEnemyBuiltInEffectEligibilityServiceTests
{
    [Test]
    public void ShouldProcess_ReturnsFalseWhenTriggerDoesNotMatch()
    {
        EnemySkill_SO.BuiltInEffectEntry effect = new EnemySkill_SO.BuiltInEffectEntry
        {
            trigger = EnemySkill_SO.DiceEffectTrigger.OnHit,
            effectType = EnemySkill_SO.BuiltInEffectType.Heal,
            amount = 1
        };

        Assert.IsFalse(BattleEnemyBuiltInEffectEligibilityService.ShouldProcess(effect, EnemySkill_SO.DiceEffectTrigger.OnMiss));
    }

    [Test]
    public void ShouldProcess_RequiresPositiveAmountForHealAndDamage()
    {
        EnemySkill_SO.BuiltInEffectEntry heal = new EnemySkill_SO.BuiltInEffectEntry
        {
            trigger = EnemySkill_SO.DiceEffectTrigger.OnHit,
            effectType = EnemySkill_SO.BuiltInEffectType.Heal,
            amount = 0
        };
        EnemySkill_SO.BuiltInEffectEntry damage = new EnemySkill_SO.BuiltInEffectEntry
        {
            trigger = EnemySkill_SO.DiceEffectTrigger.OnHit,
            effectType = EnemySkill_SO.BuiltInEffectType.Damage,
            amount = 2
        };

        Assert.IsFalse(BattleEnemyBuiltInEffectEligibilityService.ShouldProcess(heal, EnemySkill_SO.DiceEffectTrigger.OnHit));
        Assert.IsTrue(BattleEnemyBuiltInEffectEligibilityService.ShouldProcess(damage, EnemySkill_SO.DiceEffectTrigger.OnHit));
    }

    [Test]
    public void ShouldProcess_RequiresPositiveStatusStacksForApplyStatus()
    {
        EnemySkill_SO.BuiltInEffectEntry effect = new EnemySkill_SO.BuiltInEffectEntry
        {
            trigger = EnemySkill_SO.DiceEffectTrigger.OnHit,
            effectType = EnemySkill_SO.BuiltInEffectType.ApplyStatus,
            statusStackAmount = 0
        };

        Assert.IsFalse(BattleEnemyBuiltInEffectEligibilityService.ShouldProcess(effect, EnemySkill_SO.DiceEffectTrigger.OnHit));

        effect.statusStackAmount = 1;
        Assert.IsTrue(BattleEnemyBuiltInEffectEligibilityService.ShouldProcess(effect, EnemySkill_SO.DiceEffectTrigger.OnHit));
    }

    [Test]
    public void ShouldProcess_AllowsTransferWhenAmountOrFallbackIsPositive()
    {
        EnemySkill_SO.BuiltInEffectEntry effect = new EnemySkill_SO.BuiltInEffectEntry
        {
            trigger = EnemySkill_SO.DiceEffectTrigger.OnHit,
            effectType = EnemySkill_SO.BuiltInEffectType.TransferRandomNegativeStatus,
            amount = 0,
            fallbackAmount = 1
        };

        Assert.IsTrue(BattleEnemyBuiltInEffectEligibilityService.ShouldProcess(effect, EnemySkill_SO.DiceEffectTrigger.OnHit));
    }
}
