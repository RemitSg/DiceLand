using System;

public static class BattleDamageModifierResolutionDispatcher
{
    public static int ResolveIncomingAndDispatchFeedback(
        int originalDamage,
        RelicExecutionContext context,
        Action<RelicExecutionContext> flushFeedback)
    {
        BattleDamageModifierResolution resolution = BattleDamageModifierResolutionService.ResolveIncoming(originalDamage, context);
        if (resolution.FeedbackPlan.FlushRelicFeedback)
            flushFeedback?.Invoke(context);

        return resolution.AdjustedDamage;
    }

    public static int ResolveOutgoingAndDispatchFeedback(
        int originalDamage,
        RelicExecutionContext context,
        Action<RelicExecutionContext> flushFeedback)
    {
        BattleDamageModifierResolution resolution = BattleDamageModifierResolutionService.ResolveOutgoing(originalDamage, context);
        if (resolution.FeedbackPlan.FlushRelicFeedback)
            flushFeedback?.Invoke(context);

        return resolution.AdjustedDamage;
    }
}
