using UnityEngine;

public static class BattleDamageModifierCalculationService
{
    public static int ApplyIncomingReduction(int baseDamage, int flatModifier, int reductionPercent)
    {
        int adjustedDamage = Mathf.Max(0, baseDamage + flatModifier);
        int clampedReductionPercent = Mathf.Max(0, reductionPercent);
        if (clampedReductionPercent <= 0)
            return adjustedDamage;

        float multiplier = Mathf.Max(0f, 1f - (clampedReductionPercent / 100f));
        return Mathf.Max(0, Mathf.FloorToInt((adjustedDamage * multiplier) + 0.5f));
    }

    public static int ApplyOutgoingBonus(int baseDamage, int flatModifier, int bonusPercent)
    {
        int adjustedDamage = Mathf.Max(0, baseDamage + flatModifier);
        int clampedBonusPercent = Mathf.Max(0, bonusPercent);
        if (clampedBonusPercent <= 0)
            return adjustedDamage;

        float multiplier = 1f + (clampedBonusPercent / 100f);
        return Mathf.Max(0, Mathf.FloorToInt((adjustedDamage * multiplier) + 0.5f));
    }
}
