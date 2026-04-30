public struct BattleDamageModifierResolution
{
    public int AdjustedDamage;
    public BattleDamageModifierFeedbackPlan FeedbackPlan;
}

public static class BattleDamageModifierResolutionService
{
    public static BattleDamageModifierResolution ResolveIncoming(int originalDamage, RelicExecutionContext context)
    {
        int adjustedDamage = BattleDamageModifierCalculationService.ApplyIncomingReduction(
            originalDamage,
            context != null ? context.incomingDamageFlatModifier : 0,
            context != null ? context.incomingDamagePercentModifier : 0);

        return new BattleDamageModifierResolution
        {
            AdjustedDamage = adjustedDamage,
            FeedbackPlan = BattleDamageModifierFeedbackPlanService.ResolveIncoming(originalDamage, adjustedDamage)
        };
    }

    public static BattleDamageModifierResolution ResolveOutgoing(int originalDamage, RelicExecutionContext context)
    {
        int adjustedDamage = BattleDamageModifierCalculationService.ApplyOutgoingBonus(
            originalDamage,
            context != null ? context.outgoingDamageFlatModifier : 0,
            context != null ? context.outgoingDamagePercentModifier : 0);

        return new BattleDamageModifierResolution
        {
            AdjustedDamage = adjustedDamage,
            FeedbackPlan = BattleDamageModifierFeedbackPlanService.ResolveOutgoing(originalDamage, adjustedDamage)
        };
    }
}
