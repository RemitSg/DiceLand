using System.Collections.Generic;

public enum BattlePresentationStep
{
    MoveToTarget = 0,
    RollDice = 1,
    ApplyImpact = 2,
    ReturnToOrigin = 3
}

public static class BattleCombatPresentationCoroutinePlanService
{
    public static BattlePresentationStep[] BuildClashPresentation(bool moveIntoPosition = true, bool returnToOrigin = true)
    {
        return BuildSteps(moveIntoPosition, shouldApplyImpact: true, returnToOrigin);
    }

    public static BattlePresentationStep[] BuildSingleAttackPresentation(
        bool moveIntoPosition,
        bool shouldApplyImpact,
        bool returnToOrigin)
    {
        return BuildSteps(moveIntoPosition, shouldApplyImpact, returnToOrigin);
    }

    public static bool ContainsStep(BattlePresentationStep[] steps, BattlePresentationStep step)
    {
        if (steps == null)
            return false;

        for (int i = 0; i < steps.Length; i++)
        {
            if (steps[i] == step)
                return true;
        }

        return false;
    }

    private static BattlePresentationStep[] BuildSteps(
        bool moveIntoPosition,
        bool shouldApplyImpact,
        bool returnToOrigin)
    {
        List<BattlePresentationStep> steps = new List<BattlePresentationStep>();
        if (moveIntoPosition)
            steps.Add(BattlePresentationStep.MoveToTarget);

        steps.Add(BattlePresentationStep.RollDice);

        if (shouldApplyImpact)
            steps.Add(BattlePresentationStep.ApplyImpact);

        if (returnToOrigin)
            steps.Add(BattlePresentationStep.ReturnToOrigin);

        return steps.ToArray();
    }
}
