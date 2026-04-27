using NUnit.Framework;
using UnityEngine;

public class BattleSingleAttackDiceResolutionServiceTests
{
    private readonly System.Collections.Generic.List<UnityEngine.Object> createdObjects = new System.Collections.Generic.List<UnityEngine.Object>();

    [TearDown]
    public void TearDown()
    {
        foreach (UnityEngine.Object createdObject in createdObjects)
        {
            if (createdObject != null)
                Object.DestroyImmediate(createdObject);
        }

        createdObjects.Clear();
    }

    [Test]
    public void Resolve_CombinesSkillUseAndActionBonusesBeforeSingleAttackResult()
    {
        BattleSingleAttackDiceResolution resolution = BattleSingleAttackDiceResolutionService.Resolve(
            basePower: 4,
            statusPowerModifier: 1,
            skillUseBonus: 2,
            actionBonus: 3,
            isDefenseSkill: false);

        Assert.AreEqual(4, resolution.BasePower);
        Assert.AreEqual(1, resolution.StatusPowerModifier);
        Assert.AreEqual(5, resolution.BonusPower);
        Assert.AreEqual(9, resolution.PowerInput.FinalPower);
        Assert.AreEqual(9, resolution.AttackResult.FinalPower);
        Assert.AreEqual("4+5=9", resolution.AttackResult.FormattedPower);
    }

    [Test]
    public void Resolve_DefenseSkillKeepsDisplayPowerButSuppressesHitPower()
    {
        BattleSingleAttackDiceResolution resolution = BattleSingleAttackDiceResolutionService.Resolve(
            basePower: 3,
            statusPowerModifier: 2,
            skillUseBonus: 1,
            actionBonus: 1,
            isDefenseSkill: true);

        Assert.IsTrue(resolution.AttackResult.IsDefenseOnly);
        Assert.AreEqual(5, resolution.AttackResult.FinalPower);
        Assert.AreEqual(0, resolution.AttackResult.HitPower);
        Assert.AreEqual(4, resolution.AttackResult.DisplayPowerModifier);
        Assert.AreEqual("3+2=5", resolution.AttackResult.FormattedPower);
    }

    [Test]
    public void ResolveWithModifiers_CollectsSkillUseRelicAndSingleAttackBonuses()
    {
        EnemySkill_SO skill = CreateSkill(new[]
        {
            Modifier(EnemySkill_SO.DiceEffectTrigger.OnSkillUse, 2),
            Modifier(EnemySkill_SO.DiceEffectTrigger.OnSingleAttack, 3),
            Modifier(EnemySkill_SO.DiceEffectTrigger.OnClashWin, 99)
        });

        BattleSingleAttackDiceResolution resolution = BattleSingleAttackDiceResolutionService.ResolveWithModifiers(
            skill,
            diceIndex: 0,
            basePower: 5,
            statusPowerModifier: 1,
            isDefenseSkill: false,
            selfSpeed: 4,
            targetSpeed: 2,
            getSelfStatusStacksByAsset: _ => 0,
            getSelfStatusStacksById: _ => 0,
            getTargetStatusStacksByAsset: _ => 0,
            getTargetStatusStacksById: _ => 0,
            relicPowerBonus: 4);

        Assert.AreEqual(6, resolution.SkillUseBonus);
        Assert.AreEqual(3, resolution.ActionBonus);
        Assert.AreEqual(9, resolution.BonusPower);
        Assert.AreEqual(14, resolution.AttackResult.FinalPower);
        Assert.AreEqual(10, resolution.AttackResult.DisplayPowerModifier);
        Assert.AreEqual("5+9=14", resolution.AttackResult.FormattedPower);
    }

    private EnemySkill_SO CreateSkill(EnemySkill_SO.ConditionalPowerModifierEntry[] modifiers)
    {
        EnemySkill_SO skill = ScriptableObject.CreateInstance<EnemySkill_SO>();
        createdObjects.Add(skill);
        skill.diceEffectSettings = new[]
        {
            new EnemySkill_SO.DiceEffectSettings
            {
                powerModifiers = modifiers
            }
        };
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
