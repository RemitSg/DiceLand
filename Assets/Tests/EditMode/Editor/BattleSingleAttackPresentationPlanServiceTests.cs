using NUnit.Framework;
using UnityEngine;

public class BattleSingleAttackPresentationPlanServiceTests
{
    private EnemySkill_SO skill;

    [TearDown]
    public void TearDown()
    {
        if (skill != null)
            Object.DestroyImmediate(skill);

        skill = null;
    }

    [Test]
    public void Resolve_AttackDieInRemainingAllyDice_MovesOnFirstAndReturnsOnLast()
    {
        BattleSingleAttackPresentationPlan plan = BattleSingleAttackPresentationPlanService.Resolve(
            new BattleSingleAttackResult { IsDefenseOnly = false, ShouldDealDamage = true },
            diceIndex: 3,
            startIndex: 2,
            totalDiceCount: 4,
            returnOnLastDie: true,
            forceHitCallback: false);

        Assert.IsTrue(plan.ShouldPlayPresentation);
        Assert.IsTrue(plan.ShouldApplyHit);
        Assert.IsFalse(plan.MoveIntoPosition);
        Assert.IsTrue(plan.ReturnToOrigin);
        Assert.AreEqual(BattleSingleAttackPostLogType.Hit, plan.PostLogType);
    }

    [Test]
    public void Resolve_AttackDieInRemainingEnemyDice_DoesNotReturnOnLastWhenCallerReturnsManually()
    {
        BattleSingleAttackPresentationPlan plan = BattleSingleAttackPresentationPlanService.Resolve(
            new BattleSingleAttackResult { IsDefenseOnly = false, ShouldDealDamage = true },
            diceIndex: 1,
            startIndex: 1,
            totalDiceCount: 3,
            returnOnLastDie: false,
            forceHitCallback: false);

        Assert.IsTrue(plan.ShouldPlayPresentation);
        Assert.IsTrue(plan.ShouldApplyHit);
        Assert.IsTrue(plan.MoveIntoPosition);
        Assert.IsFalse(plan.ReturnToOrigin);
    }

    [Test]
    public void Resolve_DefenseOnlyDie_SkipsPresentationAndUsesDefenseLog()
    {
        BattleSingleAttackPresentationPlan plan = BattleSingleAttackPresentationPlanService.Resolve(
            new BattleSingleAttackResult { IsDefenseOnly = true, ShouldDealDamage = false },
            diceIndex: 0,
            startIndex: 0,
            totalDiceCount: 2,
            returnOnLastDie: true,
            forceHitCallback: false);

        Assert.IsFalse(plan.ShouldPlayPresentation);
        Assert.IsFalse(plan.ShouldApplyHit);
        Assert.IsTrue(plan.MoveIntoPosition);
        Assert.IsFalse(plan.ReturnToOrigin);
        Assert.AreEqual(BattleSingleAttackPostLogType.DefenseOnly, plan.PostLogType);
    }

    [Test]
    public void Resolve_ForcedHitCallback_PreservesFullSingleAttackLegacyCallback()
    {
        BattleSingleAttackPresentationPlan plan = BattleSingleAttackPresentationPlanService.Resolve(
            new BattleSingleAttackResult { IsDefenseOnly = true, ShouldDealDamage = false },
            diceIndex: 0,
            startIndex: 0,
            totalDiceCount: 1,
            returnOnLastDie: false,
            forceHitCallback: true);

        Assert.IsTrue(plan.ShouldPlayPresentation);
        Assert.IsTrue(plan.ShouldApplyHit);
        Assert.AreEqual(BattleSingleAttackPostLogType.Hit, plan.PostLogType);
    }

    [Test]
    public void FindFirstAttackDiceIndex_SkipsLeadingDefenseDiceInMixedSkill()
    {
        skill = ScriptableObject.CreateInstance<EnemySkill_SO>();
        skill.skillCategory = EnemySkill_SO.SkillCategory.Attack;
        skill.diceSkillCategories = new[]
        {
            EnemySkill_SO.SkillCategory.Defense,
            EnemySkill_SO.SkillCategory.Attack,
            EnemySkill_SO.SkillCategory.Attack
        };

        int firstAttackDiceIndex = BattleSingleAttackPresentationStartService.FindFirstAttackDiceIndex(skill, 3);

        Assert.AreEqual(1, firstAttackDiceIndex);
    }

    [Test]
    public void FindFirstAttackDiceIndex_ReturnsMinusOneWhenAllDiceAreDefense()
    {
        skill = ScriptableObject.CreateInstance<EnemySkill_SO>();
        skill.skillCategory = EnemySkill_SO.SkillCategory.Defense;
        skill.diceSkillCategories = new[]
        {
            EnemySkill_SO.SkillCategory.Defense,
            EnemySkill_SO.SkillCategory.Defense
        };

        int firstAttackDiceIndex = BattleSingleAttackPresentationStartService.FindFirstAttackDiceIndex(skill, 2);

        Assert.AreEqual(-1, firstAttackDiceIndex);
    }
}
