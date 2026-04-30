using UnityEngine;

public struct BattleZeroDamageFeedbackPlan
{
    public Vector3 PopupPosition;
    public int Damage;
    public bool IsPlayerAttack;
    public int ModifierRatioPercent;
}

public static class BattleZeroDamageFeedbackPlanService
{
    private static readonly Vector3 PopupOffset = new Vector3(0f, 1.4f, 0f);

    public static BattleZeroDamageFeedbackPlan Resolve(Vector3 actorCenterWorld, bool isPlayerAttack, int modifierRatioPercent)
    {
        return new BattleZeroDamageFeedbackPlan
        {
            PopupPosition = actorCenterWorld + PopupOffset,
            Damage = 0,
            IsPlayerAttack = isPlayerAttack,
            ModifierRatioPercent = modifierRatioPercent
        };
    }
}
