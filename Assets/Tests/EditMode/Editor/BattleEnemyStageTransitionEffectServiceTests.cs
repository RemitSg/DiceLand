using NUnit.Framework;

public class BattleEnemyStageTransitionEffectServiceTests
{
    [Test]
    public void ShouldProcess_RequiresMatchingTriggerAndSelfTarget()
    {
        EnemySkill_SO.EnemyStageTransitionEffectEntry effect = new EnemySkill_SO.EnemyStageTransitionEffectEntry
        {
            trigger = EnemySkill_SO.DiceEffectTrigger.OnSkillUse,
            targetMode = EnemySkill_SO.EnemyStageTransitionTargetMode.Self
        };

        Assert.IsTrue(BattleEnemyStageTransitionEffectService.ShouldProcess(effect, EnemySkill_SO.DiceEffectTrigger.OnSkillUse));
        Assert.IsFalse(BattleEnemyStageTransitionEffectService.ShouldProcess(effect, EnemySkill_SO.DiceEffectTrigger.OnClash));

        effect.targetMode = (EnemySkill_SO.EnemyStageTransitionTargetMode)999;
        Assert.IsFalse(BattleEnemyStageTransitionEffectService.ShouldProcess(effect, EnemySkill_SO.DiceEffectTrigger.OnSkillUse));
    }

    [Test]
    public void ShouldApplyNextRoundStart_DetectsDelayedTiming()
    {
        EnemySkill_SO.EnemyStageTransitionEffectEntry effect = new EnemySkill_SO.EnemyStageTransitionEffectEntry
        {
            applyTiming = EnemySkill_SO.EnemyStageTransitionApplyTiming.NextRoundStart
        };

        Assert.IsTrue(BattleEnemyStageTransitionEffectService.ShouldApplyNextRoundStart(effect));

        effect.applyTiming = EnemySkill_SO.EnemyStageTransitionApplyTiming.Immediate;
        Assert.IsFalse(BattleEnemyStageTransitionEffectService.ShouldApplyNextRoundStart(effect));
    }
}
