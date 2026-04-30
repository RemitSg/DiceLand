using NUnit.Framework;

public class BattleSingleAttackPresentationPlanServiceTests
{
    [Test]
    public void Resolve_AttackDieInRemainingAllyDice_MovesOnFirstAndReturnsOnLast()
    {
        BattleSingleAttackPresentationPlan plan = BattleSingleAttackPresentationPlanService.Resolve(
            new BattleSingleAttackResult { IsDefenseOnly = false, ShouldDealDamage = true },
            diceIndex: 3,
            startIndex: 2,
            totalDiceCount: 4,
            returnOnLastDie: true,
            forceHitCallback: false);

        Assert.IsTrue(plan.ShouldPlayPresentation);
        Assert.IsTrue(plan.ShouldApplyHit);
        Assert.IsFalse(plan.MoveIntoPosition);
        Assert.IsTrue(plan.ReturnToOrigin);
        Assert.AreEqual(BattleSingleAttackPostLogType.Hit, plan.PostLogType);
    }

    [Test]
    public void Resolve_AttackDieInRemainingEnemyDice_DoesNotReturnOnLastWhenCallerReturnsManually()
    {
        BattleSingleAttackPresentationPlan plan = BattleSingleAttackPresentationPlanService.Resolve(
            new BattleSingleAttackResult { IsDefenseOnly = false, ShouldDealDamage = true },
            diceIndex: 1,
            startIndex: 1,
            totalDiceCount: 3,
            returnOnLastDie: false,
            forceHitCallback: false);

        Assert.IsTrue(plan.ShouldPlayPresentation);
        Assert.IsTrue(plan.ShouldApplyHit);
        Assert.IsTrue(plan.MoveIntoPosition);
        Assert.IsFalse(plan.ReturnToOrigin);
    }

    [Test]
    public void Resolve_DefenseOnlyDie_SkipsPresentationAndUsesDefenseLog()
    {
        BattleSingleAttackPresentationPlan plan = BattleSingleAttackPresentationPlanService.Resolve(
            new BattleSingleAttackResult { IsDefenseOnly = true, ShouldDealDamage = false },
            diceIndex: 0,
            startIndex: 0,
            totalDiceCount: 2,
            returnOnLastDie: true,
            forceHitCallback: false);

        Assert.IsFalse(plan.ShouldPlayPresentation);
        Assert.IsFalse(plan.ShouldApplyHit);
        Assert.IsTrue(plan.MoveIntoPosition);
        Assert.IsFalse(plan.ReturnToOrigin);
        Assert.AreEqual(BattleSingleAttackPostLogType.DefenseOnly, plan.PostLogType);
    }

    [Test]
    public void Resolve_ForcedHitCallback_PreservesFullSingleAttackLegacyCallback()
    {
        BattleSingleAttackPresentationPlan plan = BattleSingleAttackPresentationPlanService.Resolve(
            new BattleSingleAttackResult { IsDefenseOnly = true, ShouldDealDamage = false },
            diceIndex: 0,
            startIndex: 0,
            totalDiceCount: 1,
            returnOnLastDie: false,
            forceHitCallback: true);

        Assert.IsTrue(plan.ShouldPlayPresentation);
        Assert.IsTrue(plan.ShouldApplyHit);
        Assert.AreEqual(BattleSingleAttackPostLogType.Hit, plan.PostLogType);
    }
}
