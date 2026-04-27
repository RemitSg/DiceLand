using NUnit.Framework;
using UnityEngine;

public class BattleStatusEffectConditionServiceTests
{
    [Test]
    public void IsConditionMet_TargetStatusStacksAtLeastUsesConditionStatusData()
    {
        BattleStatus_SO darkness = ScriptableObject.CreateInstance<BattleStatus_SO>();
        BattleStatus_SO misfortune = ScriptableObject.CreateInstance<BattleStatus_SO>();

        try
        {
            EnemySkill_SO.StatusEffectEntry effect = new EnemySkill_SO.StatusEffectEntry
            {
                useCondition = true,
                condition = EnemySkill_SO.PowerModifierCondition.TargetStatusStacksAtLeast,
                statusData = misfortune,
                conditionStatusData = darkness,
                requiredConditionStatusStacks = 3
            };

            bool enoughStacks = BattleStatusEffectConditionService.IsConditionMet(
                effect,
                selfSpeed: 2,
                targetSpeed: 4,
                getSelfStatusStacksByAsset: _ => 0,
                getSelfStatusStacksById: _ => 0,
                getTargetStatusStacksByAsset: checkedStatus => checkedStatus == darkness ? 3 : 0,
                getTargetStatusStacksById: _ => 0);
            bool notEnoughStacks = BattleStatusEffectConditionService.IsConditionMet(
                effect,
                selfSpeed: 2,
                targetSpeed: 4,
                getSelfStatusStacksByAsset: _ => 0,
                getSelfStatusStacksById: _ => 0,
                getTargetStatusStacksByAsset: checkedStatus => checkedStatus == darkness ? 2 : 0,
                getTargetStatusStacksById: _ => 0);

            Assert.IsTrue(enoughStacks);
            Assert.IsFalse(notEnoughStacks);
        }
        finally
        {
            Object.DestroyImmediate(darkness);
            Object.DestroyImmediate(misfortune);
        }
    }

    [Test]
    public void IsConditionMet_StatusStackOperationCanRequireTargetStatusStacks()
    {
        BattleStatus_SO darkness = ScriptableObject.CreateInstance<BattleStatus_SO>();

        try
        {
            EnemySkill_SO.StatusStackOperationEffectEntry effect = new EnemySkill_SO.StatusStackOperationEffectEntry
            {
                useCondition = true,
                condition = EnemySkill_SO.PowerModifierCondition.TargetStatusStacksAtLeast,
                conditionStatusData = darkness,
                requiredConditionStatusStacks = 3
            };

            bool enoughStacks = BattleStatusEffectConditionService.IsConditionMet(
                effect,
                selfSpeed: 2,
                targetSpeed: 4,
                getSelfStatusStacksByAsset: _ => 0,
                getSelfStatusStacksById: _ => 0,
                getTargetStatusStacksByAsset: checkedStatus => checkedStatus == darkness ? 3 : 0,
                getTargetStatusStacksById: _ => 0);
            bool notEnoughStacks = BattleStatusEffectConditionService.IsConditionMet(
                effect,
                selfSpeed: 2,
                targetSpeed: 4,
                getSelfStatusStacksByAsset: _ => 0,
                getSelfStatusStacksById: _ => 0,
                getTargetStatusStacksByAsset: checkedStatus => checkedStatus == darkness ? 2 : 0,
                getTargetStatusStacksById: _ => 0);

            Assert.IsTrue(enoughStacks);
            Assert.IsFalse(notEnoughStacks);
        }
        finally
        {
            Object.DestroyImmediate(darkness);
        }
    }
}
