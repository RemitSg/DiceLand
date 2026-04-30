using NUnit.Framework;

public class BattleClashPresentationPlanServiceTests
{
    [Test]
    public void Resolve_AllyAttackWin_UsesAllyFxAndAllyFollowUpLog()
    {
        BattleClashPresentationPlan plan = BattleClashPresentationPlanService.Resolve(
            new BattleClashResult { Outcome = BattleClashOutcome.AllyAttackWins });

        Assert.AreEqual(BattleClashHitFxSource.Ally, plan.HitFxSource);
        Assert.AreEqual(BattleClashPostLogType.AllyAttackWinFollowUp, plan.PostLogType);
        Assert.IsFalse(plan.AttackBreaksDefense);
    }

    [Test]
    public void Resolve_EnemyAttackWin_UsesEnemyFxAndEnemyFollowUpLog()
    {
        BattleClashPresentationPlan plan = BattleClashPresentationPlanService.Resolve(
            new BattleClashResult { Outcome = BattleClashOutcome.EnemyAttackWins });

        Assert.AreEqual(BattleClashHitFxSource.Enemy, plan.HitFxSource);
        Assert.AreEqual(BattleClashPostLogType.EnemyAttackWinFollowUp, plan.PostLogType);
    }

    [Test]
    public void Resolve_AllyBreaksEnemyDefense_UsesAllyFxAndMarksDefenseBreak()
    {
        BattleClashPresentationPlan plan = BattleClashPresentationPlanService.Resolve(
            new BattleClashResult { Outcome = BattleClashOutcome.AllyBreaksEnemyDefense });

        Assert.AreEqual(BattleClashHitFxSource.Ally, plan.HitFxSource);
        Assert.AreEqual(BattleClashPostLogType.AllyAttackEnemyDefense, plan.PostLogType);
        Assert.IsTrue(plan.AttackBreaksDefense);
    }

    [Test]
    public void Resolve_EnemyBreaksAllyDefense_UsesEnemyFxAndMarksDefenseBreak()
    {
        BattleClashPresentationPlan plan = BattleClashPresentationPlanService.Resolve(
            new BattleClashResult { Outcome = BattleClashOutcome.EnemyBreaksAllyDefense });

        Assert.AreEqual(BattleClashHitFxSource.Enemy, plan.HitFxSource);
        Assert.AreEqual(BattleClashPostLogType.AllyDefenseEnemyAttack, plan.PostLogType);
        Assert.IsTrue(plan.AttackBreaksDefense);
    }

    [Test]
    public void Resolve_DefenseWinsAndTies_UseSameLogFamilyWithoutDefenseBreak()
    {
        BattleClashPresentationPlan enemyDefensePlan = BattleClashPresentationPlanService.Resolve(
            new BattleClashResult { Outcome = BattleClashOutcome.EnemyDefenseWins });
        BattleClashPresentationPlan allyDefensePlan = BattleClashPresentationPlanService.Resolve(
            new BattleClashResult { Outcome = BattleClashOutcome.AllyDefenseWins });
        BattleClashPresentationPlan bothDefensePlan = BattleClashPresentationPlanService.Resolve(
            new BattleClashResult { Outcome = BattleClashOutcome.BothDefenseTie });

        Assert.AreEqual(BattleClashPostLogType.AllyAttackEnemyDefense, enemyDefensePlan.PostLogType);
        Assert.AreEqual(BattleClashHitFxSource.Ally, enemyDefensePlan.HitFxSource);
        Assert.IsFalse(enemyDefensePlan.AttackBreaksDefense);

        Assert.AreEqual(BattleClashPostLogType.AllyDefenseEnemyAttack, allyDefensePlan.PostLogType);
        Assert.AreEqual(BattleClashHitFxSource.Enemy, allyDefensePlan.HitFxSource);
        Assert.IsFalse(allyDefensePlan.AttackBreaksDefense);

        Assert.AreEqual(BattleClashPostLogType.BothDefense, bothDefensePlan.PostLogType);
        Assert.AreEqual(BattleClashHitFxSource.Ally, bothDefensePlan.HitFxSource);
        Assert.IsFalse(bothDefensePlan.AttackBreaksDefense);
    }
}
