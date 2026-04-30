using NUnit.Framework;
using UnityEngine;

public class BattleDiceRangeModifierServiceTests
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
    public void ApplyRangeModifiers_AppliesUnconditionalModifiersAndClampsRange()
    {
        EnemySkill_SO skill = CreateSkill(new[]
        {
            new EnemySkill_SO.ConditionalDiceRangeModifierEntry
            {
                minPowerBonus = -5,
                maxPowerBonus = -10
            }
        });

        EnemySkill_SO.DicePowerRange result = BattleDiceRangeModifierService.ApplyRangeModifiers(
            skill,
            0,
            new EnemySkill_SO.DicePowerRange { minPower = 3, maxPower = 8 },
            selfSpeed: 2,
            targetSpeed: 2,
            getSelfStatusStacksByAsset: _ => 0,
            getSelfStatusStacksById: _ => 0,
            getTargetStatusStacksByAsset: _ => 0,
            getTargetStatusStacksById: _ => 0);

        Assert.AreEqual(0, result.minPower);
        Assert.AreEqual(0, result.maxPower);
    }

    [Test]
    public void ApplyRangeModifiers_UsesInclusiveSpeedDifferenceAndTreatsZeroAsOne()
    {
        EnemySkill_SO skill = CreateSkill(new[]
        {
            new EnemySkill_SO.ConditionalDiceRangeModifierEntry
            {
                useCondition = true,
                condition = EnemySkill_SO.PowerModifierCondition.SelfSpeedLowerThanTarget,
                requiredSpeedDifference = 0,
                minPowerBonus = 1,
                maxPowerBonus = 2
            }
        });

        EnemySkill_SO.DicePowerRange noBonus = BattleDiceRangeModifierService.ApplyRangeModifiers(
            skill,
            0,
            new EnemySkill_SO.DicePowerRange { minPower = 2, maxPower = 4 },
            selfSpeed: 4,
            targetSpeed: 4,
            getSelfStatusStacksByAsset: _ => 0,
            getSelfStatusStacksById: _ => 0,
            getTargetStatusStacksByAsset: _ => 0,
            getTargetStatusStacksById: _ => 0);

        EnemySkill_SO.DicePowerRange withBonus = BattleDiceRangeModifierService.ApplyRangeModifiers(
            skill,
            0,
            new EnemySkill_SO.DicePowerRange { minPower = 2, maxPower = 4 },
            selfSpeed: 3,
            targetSpeed: 4,
            getSelfStatusStacksByAsset: _ => 0,
            getSelfStatusStacksById: _ => 0,
            getTargetStatusStacksByAsset: _ => 0,
            getTargetStatusStacksById: _ => 0);

        Assert.AreEqual(2, noBonus.minPower);
        Assert.AreEqual(4, noBonus.maxPower);
        Assert.AreEqual(3, withBonus.minPower);
        Assert.AreEqual(6, withBonus.maxPower);
    }

    [Test]
    public void ApplyRangeModifiers_TargetStatusAtLeastConditionUsesTargetStacks()
    {
        BattleStatus_SO status = ScriptableObject.CreateInstance<BattleStatus_SO>();
        createdObjects.Add(status);

        EnemySkill_SO skill = CreateSkill(new[]
        {
            new EnemySkill_SO.ConditionalDiceRangeModifierEntry
            {
                useCondition = true,
                condition = EnemySkill_SO.PowerModifierCondition.TargetStatusStacksAtLeast,
                targetStatusData = status,
                requiredStatusStacks = 3,
                minPowerBonus = 2,
                maxPowerBonus = 2
            }
        });

        EnemySkill_SO.DicePowerRange result = BattleDiceRangeModifierService.ApplyRangeModifiers(
            skill,
            0,
            new EnemySkill_SO.DicePowerRange { minPower = 1, maxPower = 5 },
            selfSpeed: 1,
            targetSpeed: 1,
            getSelfStatusStacksByAsset: _ => 0,
            getSelfStatusStacksById: _ => 0,
            getTargetStatusStacksByAsset: checkedStatus => checkedStatus == status ? 3 : 0,
            getTargetStatusStacksById: _ => 0);

        Assert.AreEqual(3, result.minPower);
        Assert.AreEqual(7, result.maxPower);
    }

    [Test]
    public void ApplyRangeModifiers_TargetStatusPerThresholdScalesMinimumPowerAndCapsBonus()
    {
        BattleStatus_SO status = ScriptableObject.CreateInstance<BattleStatus_SO>();
        createdObjects.Add(status);

        EnemySkill_SO skill = CreateSkill(new[]
        {
            new EnemySkill_SO.ConditionalDiceRangeModifierEntry
            {
                useCondition = true,
                condition = EnemySkill_SO.PowerModifierCondition.TargetStatusStacksPerThreshold,
                targetStatusData = status,
                requiredStatusStacks = 1,
                minPowerBonus = 1,
                minPowerBonusCap = 5
            }
        });

        EnemySkill_SO.DicePowerRange threeStacks = BattleDiceRangeModifierService.ApplyRangeModifiers(
            skill,
            0,
            new EnemySkill_SO.DicePowerRange { minPower = 1, maxPower = 6 },
            selfSpeed: 1,
            targetSpeed: 1,
            getSelfStatusStacksByAsset: _ => 0,
            getSelfStatusStacksById: _ => 0,
            getTargetStatusStacksByAsset: checkedStatus => checkedStatus == status ? 3 : 0,
            getTargetStatusStacksById: _ => 0);

        EnemySkill_SO.DicePowerRange eightStacks = BattleDiceRangeModifierService.ApplyRangeModifiers(
            skill,
            0,
            new EnemySkill_SO.DicePowerRange { minPower = 1, maxPower = 6 },
            selfSpeed: 1,
            targetSpeed: 1,
            getSelfStatusStacksByAsset: _ => 0,
            getSelfStatusStacksById: _ => 0,
            getTargetStatusStacksByAsset: checkedStatus => checkedStatus == status ? 8 : 0,
            getTargetStatusStacksById: _ => 0);

        Assert.AreEqual(4, threeStacks.minPower);
        Assert.AreEqual(6, threeStacks.maxPower);
        Assert.AreEqual(6, eightStacks.minPower);
        Assert.AreEqual(6, eightStacks.maxPower);
    }

    [Test]
    public void ApplyRangeModifiers_TargetSkillConditionRequiresMatchingSkill()
    {
        EnemySkill_SO targetSkill = ScriptableObject.CreateInstance<EnemySkill_SO>();
        EnemySkill_SO otherSkill = ScriptableObject.CreateInstance<EnemySkill_SO>();
        createdObjects.Add(targetSkill);
        createdObjects.Add(otherSkill);

        EnemySkill_SO skill = CreateSkill(new[]
        {
            new EnemySkill_SO.ConditionalDiceRangeModifierEntry
            {
                useCondition = true,
                condition = EnemySkill_SO.PowerModifierCondition.TargetSkillIsSpecific,
                targetSkillData = targetSkill,
                minPowerBonus = 2,
                maxPowerBonus = 4
            }
        });

        EnemySkill_SO.DicePowerRange matching = BattleDiceRangeModifierService.ApplyRangeModifiers(
            skill,
            0,
            new EnemySkill_SO.DicePowerRange { minPower = 1, maxPower = 3 },
            selfSpeed: 1,
            targetSpeed: 1,
            getSelfStatusStacksByAsset: _ => 0,
            getSelfStatusStacksById: _ => 0,
            getTargetStatusStacksByAsset: _ => 0,
            getTargetStatusStacksById: _ => 0,
            targetSkill: targetSkill);

        EnemySkill_SO.DicePowerRange other = BattleDiceRangeModifierService.ApplyRangeModifiers(
            skill,
            0,
            new EnemySkill_SO.DicePowerRange { minPower = 1, maxPower = 3 },
            selfSpeed: 1,
            targetSpeed: 1,
            getSelfStatusStacksByAsset: _ => 0,
            getSelfStatusStacksById: _ => 0,
            getTargetStatusStacksByAsset: _ => 0,
            getTargetStatusStacksById: _ => 0,
            targetSkill: otherSkill);

        Assert.AreEqual(3, matching.minPower);
        Assert.AreEqual(7, matching.maxPower);
        Assert.AreEqual(1, other.minPower);
        Assert.AreEqual(3, other.maxPower);
    }

    [Test]
    public void ApplyRangeModifiers_NullSkillReturnsOriginalRange()
    {
        EnemySkill_SO.DicePowerRange result = BattleDiceRangeModifierService.ApplyRangeModifiers(
            null,
            0,
            new EnemySkill_SO.DicePowerRange { minPower = 2, maxPower = 6 },
            selfSpeed: 1,
            targetSpeed: 9,
            getSelfStatusStacksByAsset: _ => 99,
            getSelfStatusStacksById: _ => 99,
            getTargetStatusStacksByAsset: _ => 99,
            getTargetStatusStacksById: _ => 99);

        Assert.AreEqual(2, result.minPower);
        Assert.AreEqual(6, result.maxPower);
    }

    private EnemySkill_SO CreateSkill(EnemySkill_SO.ConditionalDiceRangeModifierEntry[] modifiers)
    {
        EnemySkill_SO skill = ScriptableObject.CreateInstance<EnemySkill_SO>();
        skill.diceEffectSettings = new[]
        {
            new EnemySkill_SO.DiceEffectSettings
            {
                rangeModifiers = modifiers
            }
        };
        createdObjects.Add(skill);
        return skill;
    }
}
