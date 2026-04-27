using NUnit.Framework;
using UnityEngine;

public class BattlePowerModifierServiceTests
{
    private readonly System.Collections.Generic.List<Object> createdObjects = new System.Collections.Generic.List<Object>();

    [TearDown]
    public void TearDown()
    {
        for (int i = createdObjects.Count - 1; i >= 0; i--)
        {
            Object created = createdObjects[i];
            if (created != null)
                Object.DestroyImmediate(created);
        }

        createdObjects.Clear();
    }

    [Test]
    public void EvaluatePowerModifier_AppliesUnconditionalModifierOnlyForMatchingTrigger()
    {
        EnemySkill_SO skill = CreateSkill(new[]
        {
            new EnemySkill_SO.ConditionalPowerModifierEntry
            {
                trigger = EnemySkill_SO.DiceEffectTrigger.OnSkillUse,
                powerBonus = 2
            },
            new EnemySkill_SO.ConditionalPowerModifierEntry
            {
                trigger = EnemySkill_SO.DiceEffectTrigger.OnClash,
                powerBonus = 7
            }
        });

        int bonus = BattlePowerModifierService.EvaluatePowerModifier(
            skill,
            0,
            EnemySkill_SO.DiceEffectTrigger.OnSkillUse,
            selfSpeed: 3,
            targetSpeed: 3,
            getSelfStatusStacksByAsset: _ => 0,
            getSelfStatusStacksById: _ => 0,
            getTargetStatusStacksByAsset: _ => 0,
            getTargetStatusStacksById: _ => 0);

        Assert.AreEqual(2, bonus);
    }

    [Test]
    public void EvaluatePowerModifier_UsesInclusiveSpeedDifferenceAndTreatsZeroAsOne()
    {
        EnemySkill_SO skill = CreateSkill(new[]
        {
            new EnemySkill_SO.ConditionalPowerModifierEntry
            {
                trigger = EnemySkill_SO.DiceEffectTrigger.OnClash,
                useCondition = true,
                condition = EnemySkill_SO.PowerModifierCondition.SelfSpeedHigherThanTarget,
                requiredSpeedDifference = 0,
                powerBonus = 3
            }
        });

        int equalSpeedBonus = BattlePowerModifierService.EvaluatePowerModifier(
            skill,
            0,
            EnemySkill_SO.DiceEffectTrigger.OnClash,
            selfSpeed: 5,
            targetSpeed: 5,
            getSelfStatusStacksByAsset: _ => 0,
            getSelfStatusStacksById: _ => 0,
            getTargetStatusStacksByAsset: _ => 0,
            getTargetStatusStacksById: _ => 0);

        int oneHigherBonus = BattlePowerModifierService.EvaluatePowerModifier(
            skill,
            0,
            EnemySkill_SO.DiceEffectTrigger.OnClash,
            selfSpeed: 6,
            targetSpeed: 5,
            getSelfStatusStacksByAsset: _ => 0,
            getSelfStatusStacksById: _ => 0,
            getTargetStatusStacksByAsset: _ => 0,
            getTargetStatusStacksById: _ => 0);

        Assert.AreEqual(0, equalSpeedBonus);
        Assert.AreEqual(3, oneHigherBonus);
    }

    [Test]
    public void EvaluatePowerModifier_AddsTargetStatusBonusPerThreshold()
    {
        BattleStatus_SO status = ScriptableObject.CreateInstance<BattleStatus_SO>();
        createdObjects.Add(status);

        EnemySkill_SO skill = CreateSkill(new[]
        {
            new EnemySkill_SO.ConditionalPowerModifierEntry
            {
                trigger = EnemySkill_SO.DiceEffectTrigger.OnSingleAttack,
                useCondition = true,
                condition = EnemySkill_SO.PowerModifierCondition.TargetStatusStacksPerThreshold,
                targetStatusData = status,
                requiredStatusStacks = 2,
                powerBonusPerStep = 4
            }
        });

        int bonus = BattlePowerModifierService.EvaluatePowerModifier(
            skill,
            0,
            EnemySkill_SO.DiceEffectTrigger.OnSingleAttack,
            selfSpeed: 2,
            targetSpeed: 4,
            getSelfStatusStacksByAsset: _ => 0,
            getSelfStatusStacksById: _ => 0,
            getTargetStatusStacksByAsset: checkedStatus => checkedStatus == status ? 5 : 0,
            getTargetStatusStacksById: _ => 0);

        Assert.AreEqual(8, bonus);
    }

    [Test]
    public void EvaluatePowerModifier_CapsTargetStatusBonusPerThresholdWhenConfigured()
    {
        BattleStatus_SO status = ScriptableObject.CreateInstance<BattleStatus_SO>();
        createdObjects.Add(status);

        EnemySkill_SO skill = CreateSkill(new[]
        {
            new EnemySkill_SO.ConditionalPowerModifierEntry
            {
                trigger = EnemySkill_SO.DiceEffectTrigger.OnSkillUse,
                useCondition = true,
                condition = EnemySkill_SO.PowerModifierCondition.TargetStatusStacksPerThreshold,
                targetStatusData = status,
                requiredStatusStacks = 1,
                powerBonusPerStep = 1,
                maxPowerBonus = 2
            }
        });

        int bonus = BattlePowerModifierService.EvaluatePowerModifier(
            skill,
            0,
            EnemySkill_SO.DiceEffectTrigger.OnSkillUse,
            selfSpeed: 2,
            targetSpeed: 4,
            getSelfStatusStacksByAsset: _ => 0,
            getSelfStatusStacksById: _ => 0,
            getTargetStatusStacksByAsset: checkedStatus => checkedStatus == status ? 5 : 0,
            getTargetStatusStacksById: _ => 0);

        Assert.AreEqual(2, bonus);
    }

    [Test]
    public void EvaluatePowerModifier_TargetSkillConditionRequiresMatchingSkill()
    {
        EnemySkill_SO targetSkill = ScriptableObject.CreateInstance<EnemySkill_SO>();
        EnemySkill_SO otherSkill = ScriptableObject.CreateInstance<EnemySkill_SO>();
        createdObjects.Add(targetSkill);
        createdObjects.Add(otherSkill);

        EnemySkill_SO skill = CreateSkill(new[]
        {
            new EnemySkill_SO.ConditionalPowerModifierEntry
            {
                trigger = EnemySkill_SO.DiceEffectTrigger.OnClash,
                useCondition = true,
                condition = EnemySkill_SO.PowerModifierCondition.TargetSkillIsSpecific,
                targetSkillData = targetSkill,
                powerBonus = 6
            }
        });

        int matchingBonus = BattlePowerModifierService.EvaluatePowerModifier(
            skill,
            0,
            EnemySkill_SO.DiceEffectTrigger.OnClash,
            selfSpeed: 3,
            targetSpeed: 3,
            getSelfStatusStacksByAsset: _ => 0,
            getSelfStatusStacksById: _ => 0,
            getTargetStatusStacksByAsset: _ => 0,
            getTargetStatusStacksById: _ => 0,
            targetSkill: targetSkill);

        int otherBonus = BattlePowerModifierService.EvaluatePowerModifier(
            skill,
            0,
            EnemySkill_SO.DiceEffectTrigger.OnClash,
            selfSpeed: 3,
            targetSpeed: 3,
            getSelfStatusStacksByAsset: _ => 0,
            getSelfStatusStacksById: _ => 0,
            getTargetStatusStacksByAsset: _ => 0,
            getTargetStatusStacksById: _ => 0,
            targetSkill: otherSkill);

        Assert.AreEqual(6, matchingBonus);
        Assert.AreEqual(0, otherBonus);
    }

    [Test]
    public void EvaluatePowerModifier_NullSkillReturnsZero()
    {
        int bonus = BattlePowerModifierService.EvaluatePowerModifier(
            null,
            0,
            EnemySkill_SO.DiceEffectTrigger.OnClash,
            selfSpeed: 10,
            targetSpeed: 1,
            getSelfStatusStacksByAsset: _ => 99,
            getSelfStatusStacksById: _ => 99,
            getTargetStatusStacksByAsset: _ => 99,
            getTargetStatusStacksById: _ => 99);

        Assert.AreEqual(0, bonus);
    }

    private EnemySkill_SO CreateSkill(EnemySkill_SO.ConditionalPowerModifierEntry[] modifiers)
    {
        EnemySkill_SO skill = ScriptableObject.CreateInstance<EnemySkill_SO>();
        skill.diceEffectSettings = new[]
        {
            new EnemySkill_SO.DiceEffectSettings
            {
                powerModifiers = modifiers
            }
        };
        createdObjects.Add(skill);
        return skill;
    }
}
