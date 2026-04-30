public struct BattleDamageModifierFeedbackPlan
{
    public bool FlushRelicFeedback;
}

public static class BattleDamageModifierFeedbackPlanService
{
    public static BattleDamageModifierFeedbackPlan ResolveIncoming(int originalDamage, int adjustedDamage)
    {
        return new BattleDamageModifierFeedbackPlan
        {
            FlushRelicFeedback = adjustedDamage < originalDamage
        };
    }

    public static BattleDamageModifierFeedbackPlan ResolveOutgoing(int originalDamage, int adjustedDamage)
    {
        return new BattleDamageModifierFeedbackPlan
        {
            FlushRelicFeedback = adjustedDamage > originalDamage
        };
    }
}
