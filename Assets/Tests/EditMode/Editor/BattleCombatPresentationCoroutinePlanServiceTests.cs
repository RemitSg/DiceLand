using NUnit.Framework;

public class BattleCombatPresentationCoroutinePlanServiceTests
{
    [Test]
    public void BuildClashPresentation_ReturnsMoveRollImpactReturnOrder()
    {
        BattlePresentationStep[] steps = BattleCombatPresentationCoroutinePlanService.BuildClashPresentation(
            moveIntoPosition: true,
            returnToOrigin: true);

        CollectionAssert.AreEqual(
            new[]
            {
                BattlePresentationStep.MoveToTarget,
                BattlePresentationStep.RollDice,
                BattlePresentationStep.ApplyImpact,
                BattlePresentationStep.ReturnToOrigin
            },
            steps);
    }

    [Test]
    public void BuildSingleAttackPresentation_WhenNoReturn_SkipsReturnStep()
    {
        BattlePresentationStep[] steps = BattleCombatPresentationCoroutinePlanService.BuildSingleAttackPresentation(
            moveIntoPosition: true,
            shouldApplyImpact: true,
            returnToOrigin: false);

        CollectionAssert.AreEqual(
            new[]
            {
                BattlePresentationStep.MoveToTarget,
                BattlePresentationStep.RollDice,
                BattlePresentationStep.ApplyImpact
            },
            steps);
    }
}
