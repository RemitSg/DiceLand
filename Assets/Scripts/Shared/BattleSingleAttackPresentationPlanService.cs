public enum BattleSingleAttackPostLogType
{
    DefenseOnly,
    Hit
}

public struct BattleSingleAttackPresentationPlan
{
    public bool ShouldPlayPresentation;
    public bool ShouldApplyHit;
    public bool MoveIntoPosition;
    public bool ReturnToOrigin;
    public BattleSingleAttackPostLogType PostLogType;
}

public static class BattleSingleAttackPresentationPlanService
{
    public static BattleSingleAttackPresentationPlan Resolve(
        BattleSingleAttackResult attackResult,
        int diceIndex,
        int startIndex,
        int totalDiceCount,
        bool returnOnLastDie,
        bool forceHitCallback)
    {
        bool defenseOnly = attackResult.IsDefenseOnly && !forceHitCallback;
        bool shouldPlayPresentation = !defenseOnly;
        bool shouldApplyHit = shouldPlayPresentation && (forceHitCallback || attackResult.ShouldDealDamage);

        return new BattleSingleAttackPresentationPlan
        {
            ShouldPlayPresentation = shouldPlayPresentation,
            ShouldApplyHit = shouldApplyHit,
            MoveIntoPosition = diceIndex == startIndex,
            ReturnToOrigin = shouldPlayPresentation && returnOnLastDie && diceIndex == totalDiceCount - 1,
            PostLogType = defenseOnly ? BattleSingleAttackPostLogType.DefenseOnly : BattleSingleAttackPostLogType.Hit
        };
    }
}
