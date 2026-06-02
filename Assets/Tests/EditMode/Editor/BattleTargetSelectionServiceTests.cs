using System.Collections.Generic;
using NUnit.Framework;

public class BattleTargetSelectionServiceTests
{
    [Test]
    public void SelectTargets_CurrentTarget_ReturnsValidCurrentOnly()
    {
        List<string> result = BattleTargetSelectionService.SelectTargets(
            EnemySkill_SO.DamageTargetMode.CurrentTarget,
            "A",
            new List<string> { "B", "C" },
            randomTargetCount: 1,
            isValidTarget: value => value == "A",
            randomRange: (min, max) => min);

        CollectionAssert.AreEqual(new[] { "A" }, result);
    }

    [Test]
    public void SelectTargets_AllOpponents_CopiesCandidates()
    {
        List<string> candidates = new List<string> { "A", "B" };
        List<string> result = BattleTargetSelectionService.SelectTargets(
            EnemySkill_SO.DamageTargetMode.AllOpponents,
            null,
            candidates,
            randomTargetCount: 1,
            isValidTarget: _ => true,
            randomRange: (min, max) => min);

        candidates.Clear();
        CollectionAssert.AreEqual(new[] { "A", "B" }, result);
    }

    [Test]
    public void SelectTargets_RandomOpponents_ClampsCountAndRemovesSelectedTargets()
    {
        int[] picks = { 1, 0 };
        int pickIndex = 0;

        List<string> result = BattleTargetSelectionService.SelectTargets(
            EnemySkill_SO.DamageTargetMode.RandomOpponents,
            null,
            new List<string> { "A", "B" },
            randomTargetCount: 5,
            isValidTarget: _ => true,
            randomRange: (min, max) => picks[pickIndex++]);

        CollectionAssert.AreEqual(new[] { "B", "A" }, result);
    }

    [Test]
    public void Select_AllOpponents_FiltersInvalidTargets()
    {
        List<string> result = BattleTargetSelectionService.Select(
            EnemySkill_SO.DamageTargetMode.AllOpponents,
            currentTarget: null,
            candidates: new[] { "A", "Dead", "B" },
            randomTargetCount: 1,
            isValidTarget: target => target != "Dead",
            randomOrder: targets => targets);

        CollectionAssert.AreEqual(new[] { "A", "B" }, result);
    }

    [Test]
    public void Select_AllOpponentsExceptCurrent_FiltersCurrentTarget()
    {
        List<string> result = BattleTargetSelectionService.Select(
            (EnemySkill_SO.DamageTargetMode)8,
            currentTarget: "A",
            candidates: new[] { "A", "Dead", "B", "C" },
            randomTargetCount: 1,
            isValidTarget: target => target != "Dead",
            randomOrder: targets => targets);

        CollectionAssert.AreEqual(new[] { "B", "C" }, result);
    }

    [Test]
    public void Select_RandomOpponents_WhenCountExceedsCandidates_ReturnsAllValidCandidates()
    {
        List<string> result = BattleTargetSelectionService.Select(
            EnemySkill_SO.DamageTargetMode.RandomOpponents,
            currentTarget: "A",
            candidates: new[] { "A", "Dead", "B" },
            randomTargetCount: 99,
            isValidTarget: target => target != "Dead",
            randomOrder: targets => targets);

        CollectionAssert.AreEqual(new[] { "A", "B" }, result);
    }

    [Test]
    public void Select_AllFriendlies_UsesAllValidCandidates()
    {
        List<string> result = BattleTargetSelectionService.Select(
            EnemySkill_SO.DamageTargetMode.AllFriendlies,
            currentTarget: null,
            candidates: new[] { "A", "Dead", "B" },
            randomTargetCount: 1,
            isValidTarget: target => target != "Dead",
            randomOrder: targets => targets);

        CollectionAssert.AreEqual(new[] { "A", "B" }, result);
    }

    [Test]
    public void Select_LowestHpFriendly_UsesResolverAndReturnsOneValidCandidate()
    {
        List<string> result = BattleTargetSelectionService.Select(
            EnemySkill_SO.DamageTargetMode.LowestHpFriendly,
            currentTarget: null,
            candidates: new[] { "High", "Dead", "Low" },
            randomTargetCount: 1,
            isValidTarget: target => target != "Dead",
            randomOrder: targets => targets,
            comparePriority: (left, right) => GetHp(left).CompareTo(GetHp(right)));

        CollectionAssert.AreEqual(new[] { "Low" }, result);
    }

    [Test]
    public void Select_LowestHpOpponent_UsesResolverAndReturnsOneValidCandidate()
    {
        List<string> result = BattleTargetSelectionService.Select(
            EnemySkill_SO.DamageTargetMode.LowestHpOpponent,
            currentTarget: null,
            candidates: new[] { "High", "Dead", "Low" },
            randomTargetCount: 1,
            isValidTarget: target => target != "Dead",
            randomOrder: targets => targets,
            comparePriority: (left, right) => GetHp(left).CompareTo(GetHp(right)));

        CollectionAssert.AreEqual(new[] { "Low" }, result);
    }

    private static int GetHp(string target)
    {
        return target == "Low" ? 2 : 9;
    }
}
