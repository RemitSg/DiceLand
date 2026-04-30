using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class BattleZeroDamageFeedbackDispatcherTests
{
    [Test]
    public void Dispatch_ForwardsPlanToPopupCallback()
    {
        List<string> calls = new List<string>();
        BattleZeroDamageFeedbackPlan plan = BattleZeroDamageFeedbackPlanService.Resolve(
            new Vector3(1f, 2f, 3f),
            isPlayerAttack: false,
            modifierRatioPercent: 120);

        BattleZeroDamageFeedbackDispatcher.Dispatch(
            plan,
            (position, damage, isPlayerAttack, modifierRatioPercent) =>
                calls.Add($"{position}:{damage}:{isPlayerAttack}:{modifierRatioPercent}"));

        CollectionAssert.AreEqual(
            new[] { "(1.00, 3.40, 3.00):0:False:120" },
            calls);
    }
}
