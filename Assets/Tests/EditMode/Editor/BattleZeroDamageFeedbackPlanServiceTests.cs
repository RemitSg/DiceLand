using NUnit.Framework;
using UnityEngine;

public class BattleZeroDamageFeedbackPlanServiceTests
{
    [Test]
    public void Resolve_OffsetsPopupAboveActorCenterAndKeepsDirection()
    {
        BattleZeroDamageFeedbackPlan plan = BattleZeroDamageFeedbackPlanService.Resolve(
            actorCenterWorld: new Vector3(1f, 2f, 3f),
            isPlayerAttack: true,
            modifierRatioPercent: 75);

        Assert.AreEqual(new Vector3(1f, 3.4f, 3f), plan.PopupPosition);
        Assert.AreEqual(0, plan.Damage);
        Assert.IsTrue(plan.IsPlayerAttack);
        Assert.AreEqual(75, plan.ModifierRatioPercent);
    }
}
