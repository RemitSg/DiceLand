using NUnit.Framework;

public class BattleSuccessfulHitSkillEffectPlanServiceTests
{
    [Test]
    public void Resolve_SingleAttack_IncludesSingleAttackBeforeClashWinAndHit()
    {
        BattleSuccessfulHitSkillEffectPlan plan = BattleSuccessfulHitSkillEffectPlanService.Resolve(isSingleAttack: true);

        CollectionAssert.AreEqual(
            new[]
            {
                EnemySkill_SO.DiceEffectTrigger.OnSingleAttack,
                EnemySkill_SO.DiceEffectTrigger.OnClashWin,
                EnemySkill_SO.DiceEffectTrigger.OnHit
            },
            plan.ImmediateTriggers);
        CollectionAssert.AreEqual(plan.ImmediateTriggers, plan.StatusEffectTriggers);
        Assert.IsTrue(plan.ApplyReactiveOnHitStatusEffects);
    }

    [Test]
    public void Resolve_ClashHit_UsesClashWinAndHitOnly()
    {
        BattleSuccessfulHitSkillEffectPlan plan = BattleSuccessfulHitSkillEffectPlanService.Resolve(isSingleAttack: false);

        CollectionAssert.AreEqual(
            new[]
            {
                EnemySkill_SO.DiceEffectTrigger.OnClashWin,
                EnemySkill_SO.DiceEffectTrigger.OnHit
            },
            plan.ImmediateTriggers);
        CollectionAssert.AreEqual(plan.ImmediateTriggers, plan.StatusEffectTriggers);
        Assert.IsTrue(plan.ApplyReactiveOnHitStatusEffects);
    }
}
