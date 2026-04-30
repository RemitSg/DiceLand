using NUnit.Framework;

public class BattleSuccessfulHitRelicPlanServiceTests
{
    [Test]
    public void ResolveAllyHit_AlwaysAppliesPlayerHitRelics()
    {
        BattleSuccessfulHitRelicPlan plan = BattleSuccessfulHitRelicPlanService.ResolveAllyHit(targetIsDead: false);

        Assert.IsTrue(plan.ApplyPlayerHitRelics);
        Assert.IsFalse(plan.ApplyEnemyKilledByPlayerRelics);
        Assert.IsFalse(plan.ApplyPlayerTookSingleAttackRelics);
    }

    [Test]
    public void ResolveAllyHit_DeadTargetAlsoAppliesKillRelics()
    {
        BattleSuccessfulHitRelicPlan plan = BattleSuccessfulHitRelicPlanService.ResolveAllyHit(targetIsDead: true);

        Assert.IsTrue(plan.ApplyPlayerHitRelics);
        Assert.IsTrue(plan.ApplyEnemyKilledByPlayerRelics);
        Assert.IsFalse(plan.ApplyPlayerTookSingleAttackRelics);
    }

    [Test]
    public void ResolveEnemyHit_SingleAttackAppliesPlayerTookSingleAttackRelics()
    {
        BattleSuccessfulHitRelicPlan plan = BattleSuccessfulHitRelicPlanService.ResolveEnemyHit(isSingleAttack: true);

        Assert.IsFalse(plan.ApplyPlayerHitRelics);
        Assert.IsFalse(plan.ApplyEnemyKilledByPlayerRelics);
        Assert.IsTrue(plan.ApplyPlayerTookSingleAttackRelics);
    }

    [Test]
    public void ResolveEnemyHit_ClashHitDoesNotApplySingleAttackRelics()
    {
        BattleSuccessfulHitRelicPlan plan = BattleSuccessfulHitRelicPlanService.ResolveEnemyHit(isSingleAttack: false);

        Assert.IsFalse(plan.ApplyPlayerHitRelics);
        Assert.IsFalse(plan.ApplyEnemyKilledByPlayerRelics);
        Assert.IsFalse(plan.ApplyPlayerTookSingleAttackRelics);
    }
}
