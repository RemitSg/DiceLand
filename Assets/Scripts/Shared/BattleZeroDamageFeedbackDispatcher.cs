using System;
using UnityEngine;

public static class BattleZeroDamageFeedbackDispatcher
{
    public static void Dispatch(
        BattleZeroDamageFeedbackPlan plan,
        Action<Vector3, int, bool, int> showDamageResultPopup)
    {
        showDamageResultPopup?.Invoke(
            plan.PopupPosition,
            plan.Damage,
            plan.IsPlayerAttack,
            plan.ModifierRatioPercent);
    }
}
