using NUnit.Framework;
using UnityEngine;

public class BattleStatusEffectApplicationServiceTests
{
    [Test]
    public void TryBuildPlan_ResolvesStatusAndUsesAdjustedStacks()
    {
        BattleStatus_SO status = ScriptableObject.CreateInstance<BattleStatus_SO>();

        EnemySkill_SO.StatusEffectEntry effect = new EnemySkill_SO.StatusEffectEntry
        {
            statusData = status,
            stackAmount = 2,
            applyTiming = EnemySkill_SO.StatusApplyTiming.Immediate
        };

        bool built = BattleStatusEffectApplicationService.TryBuildPlan(
            effect,
            adjustedStacks: 5,
            out BattleStatusEffectApplicationPlan plan);

        Assert.IsTrue(built);
        Assert.AreSame(status, plan.StatusData);
        Assert.AreEqual(status.StatusId, plan.StatusId);
        Assert.AreEqual(status.DisplayName, plan.StatusName);
        Assert.AreEqual(5, plan.ApplicationPlan.StackAmount);
        Assert.IsTrue(plan.ApplicationPlan.ShouldApply);

        Object.DestroyImmediate(status);
    }

    [Test]
    public void TryBuildPlan_ReturnsFalseWhenStatusCannotResolve()
    {
        EnemySkill_SO.StatusEffectEntry effect = new EnemySkill_SO.StatusEffectEntry
        {
            stackAmount = 2,
            statusType = (EnemySkill_SO.DiceStatusType)999
        };

        Assert.IsFalse(BattleStatusEffectApplicationService.TryBuildPlan(
            effect,
            adjustedStacks: 2,
            out BattleStatusEffectApplicationPlan plan));
        Assert.IsFalse(plan.ApplicationPlan.ShouldApply);
    }

    [Test]
    public void BuildImmediateAppliedLog_UsesProvidedTargetAndCurrentStacks()
    {
        string log = BattleStatusEffectApplicationService.BuildImmediateAppliedLog(
            "Attacker",
            "Target",
            "StatusEffect#1",
            "Burn",
            appliedStacks: 2,
            currentStacks: 4);

        StringAssert.Contains("Attacker", log);
        StringAssert.Contains("Target", log);
        StringAssert.Contains("StatusEffect#1", log);
        StringAssert.Contains("Burn", log);
        StringAssert.Contains("2", log);
        StringAssert.Contains("4", log);
    }
}
