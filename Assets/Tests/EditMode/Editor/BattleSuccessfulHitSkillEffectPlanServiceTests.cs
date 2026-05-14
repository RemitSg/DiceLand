using NUnit.Framework;

public class BattleSuccessfulHitSkillEffectPlanServiceTests
{
    [Test]
    public void Resolve_SingleAttack_IncludesSingleAttackBeforeHit()
    {
        BattleSuccessfulHitSkillEffectPlan plan = BattleSuccessfulHitSkillEffectPlanService.Resolve(isSingleAttack: true);

        CollectionAssert.AreEqual(
            new[]
            {
                EnemySkill_SO.DiceEffectTrigger.OnSingleAttack,
                EnemySkill_SO.DiceEffectTrigger.OnHit
            },
            plan.ImmediateTriggers);
        CollectionAssert.AreEqual(plan.ImmediateTriggers, plan.StatusEffectTriggers);
        Assert.IsTrue(plan.ApplyReactiveOnHitStatusEffects);
    }

    [Test]
    public void Resolve_ClashHit_UsesHitOnly()
    {
        BattleSuccessfulHitSkillEffectPlan plan = BattleSuccessfulHitSkillEffectPlanService.Resolve(isSingleAttack: false);

        CollectionAssert.AreEqual(
            new[]
            {
                EnemySkill_SO.DiceEffectTrigger.OnHit
            },
            plan.ImmediateTriggers);
        CollectionAssert.AreEqual(plan.ImmediateTriggers, plan.StatusEffectTriggers);
        Assert.IsTrue(plan.ApplyReactiveOnHitStatusEffects);
    }

    [Test]
    public void Resolve_DoesNotTreatClashWinAsSuccessfulHit()
    {
        BattleSuccessfulHitSkillEffectPlan singleAttackPlan = BattleSuccessfulHitSkillEffectPlanService.Resolve(isSingleAttack: true);
        BattleSuccessfulHitSkillEffectPlan clashHitPlan = BattleSuccessfulHitSkillEffectPlanService.Resolve(isSingleAttack: false);

        CollectionAssert.DoesNotContain(singleAttackPlan.ImmediateTriggers, EnemySkill_SO.DiceEffectTrigger.OnClashWin);
        CollectionAssert.DoesNotContain(singleAttackPlan.StatusEffectTriggers, EnemySkill_SO.DiceEffectTrigger.OnClashWin);
        CollectionAssert.DoesNotContain(clashHitPlan.ImmediateTriggers, EnemySkill_SO.DiceEffectTrigger.OnClashWin);
        CollectionAssert.DoesNotContain(clashHitPlan.StatusEffectTriggers, EnemySkill_SO.DiceEffectTrigger.OnClashWin);
    }
}
