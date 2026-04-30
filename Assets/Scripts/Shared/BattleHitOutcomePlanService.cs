public struct BattleHitOutcomePlan
{
    public bool ShowZeroDamageFeedback;
    public bool ApplySuccessfulHitEffects;
    public bool ApplyMissEffects;
}

public static class BattleHitOutcomePlanService
{
    public static BattleHitOutcomePlan Resolve(int dealtDamage)
    {
        bool successfulHit = dealtDamage > 0;

        return new BattleHitOutcomePlan
        {
            ShowZeroDamageFeedback = !successfulHit,
            ApplySuccessfulHitEffects = successfulHit,
            ApplyMissEffects = !successfulHit
        };
    }
}
