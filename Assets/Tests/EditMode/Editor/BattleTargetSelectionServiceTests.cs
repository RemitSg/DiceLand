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
}
