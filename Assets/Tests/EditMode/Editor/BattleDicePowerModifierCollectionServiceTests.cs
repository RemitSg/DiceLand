using NUnit.Framework;
using UnityEngine;

public class BattleDicePowerModifierCollectionServiceTests
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
    public void GetSkillUseBonus_AddsRelicBonusToSkillUseModifier()
    {
        EnemySkill_SO skill = CreateSkill(new[]
        {
            Modifier(EnemySkill_SO.DiceEffectTrigger.OnSkillUse, 3),
            Modifier(EnemySkill_SO.DiceEffectTrigger.OnClash, 7)
        });

        int bonus = BattleDicePowerModifierCollectionService.GetSkillUseBonus(
            skill,
            0,
            selfSpeed: 2,
            targetSpeed: 1,
            getSelfStatusStacksByAsset: _ => 0,
            getSelfStatusStacksById: _ => 0,
            getTargetStatusStacksByAsset: _ => 0,
            getTargetStatusStacksById: _ => 0,
            targetSkill: null,
            relicPowerBonus: 5);

        Assert.AreEqual(8, bonus);
    }

    [Test]
    public void GetActionBonus_UsesRequestedTriggerOnly()
    {
        EnemySkill_SO skill = CreateSkill(new[]
        {
            Modifier(EnemySkill_SO.DiceEffectTrigger.OnSingleAttack, 4),
            Modifier(EnemySkill_SO.DiceEffectTrigger.OnClash, 9)
        });

        int bonus = BattleDicePowerModifierCollectionService.GetActionBonus(
            skill,
            0,
            EnemySkill_SO.DiceEffectTrigger.OnSingleAttack,
            selfSpeed: 2,
            targetSpeed: 1,
            getSelfStatusStacksByAsset: _ => 0,
            getSelfStatusStacksById: _ => 0,
            getTargetStatusStacksByAsset: _ => 0,
            getTargetStatusStacksById: _ => 0);

        Assert.AreEqual(4, bonus);
    }

    [Test]
    public void GetClashWinBonus_ReturnsZeroWhenNotWinner()
    {
        EnemySkill_SO skill = CreateSkill(new[]
        {
            Modifier(EnemySkill_SO.DiceEffectTrigger.OnClashWin, 6)
        });

        int bonus = BattleDicePowerModifierCollectionService.GetClashWinBonus(
            skill,
            0,
            shouldApply: false,
            selfSpeed: 5,
            targetSpeed: 1,
            getSelfStatusStacksByAsset: _ => 0,
            getSelfStatusStacksById: _ => 0,
            getTargetStatusStacksByAsset: _ => 0,
            getTargetStatusStacksById: _ => 0);

        Assert.AreEqual(0, bonus);
    }

    [Test]
    public void GetClashWinBonus_EvaluatesModifierWhenWinner()
    {
        EnemySkill_SO skill = CreateSkill(new[]
        {
            Modifier(EnemySkill_SO.DiceEffectTrigger.OnClashWin, 6)
        });

        int bonus = BattleDicePowerModifierCollectionService.GetClashWinBonus(
            skill,
            0,
            shouldApply: true,
            selfSpeed: 5,
            targetSpeed: 1,
            getSelfStatusStacksByAsset: _ => 0,
            getSelfStatusStacksById: _ => 0,
            getTargetStatusStacksByAsset: _ => 0,
            getTargetStatusStacksById: _ => 0);

        Assert.AreEqual(6, bonus);
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

    private static EnemySkill_SO.ConditionalPowerModifierEntry Modifier(EnemySkill_SO.DiceEffectTrigger trigger, int bonus)
    {
        return new EnemySkill_SO.ConditionalPowerModifierEntry
        {
            trigger = trigger,
            powerBonus = bonus
        };
    }
}
