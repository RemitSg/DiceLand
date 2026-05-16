public struct BattleHitOutcomePlan
{
    public bool ShowZeroDamageFeedback;
    public bool ApplySuccessfulHitEffects;
    public bool ApplyMissEffects;
}

public static class BattleHitOutcomePlanService
{
    public static BattleHitOutcomePlan Resolve(int dealtDamage, bool hitConfirmed)
    {
        return new BattleHitOutcomePlan
        {
            ShowZeroDamageFeedback = dealtDamage <= 0,
            ApplySuccessfulHitEffects = hitConfirmed,
            ApplyMissEffects = !hitConfirmed
        };
    }
}
