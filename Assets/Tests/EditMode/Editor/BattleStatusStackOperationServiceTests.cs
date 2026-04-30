using NUnit.Framework;

public class BattleStatusStackOperationServiceTests
{
    [Test]
    public void BuildActionLabel_DescribesClearStacks()
    {
        Assert.AreEqual(
            "清空 燃烧",
            BattleStatusStackOperationService.BuildActionLabel(EnemySkill_SO.StatusStackOperationType.ClearStacks, "燃烧", 3));
    }

    [Test]
    public void BuildActionLabel_DescribesReducedStacks()
    {
        Assert.AreEqual(
            "使 燃烧 减少 2 层",
            BattleStatusStackOperationService.BuildActionLabel(EnemySkill_SO.StatusStackOperationType.ReduceStacks, "燃烧", 2));
    }

    [Test]
    public void NormalizeReduceAmount_ClampsToAtLeastOne()
    {
        Assert.AreEqual(1, BattleStatusStackOperationService.NormalizeReduceAmount(0));
    }

    [Test]
    public void DispatchOperation_ClearStacks_ClearsAndLogsAffectedStacks()
    {
        bool cleared = false;
        int loggedStacks = 0;

        int affectedStacks = BattleStatusStackOperationDispatcher.DispatchOperation(
            EnemySkill_SO.StatusStackOperationType.ClearStacks,
            stackAmount: 3,
            getCurrentStacks: () => 4,
            removeStacks: _ => 0,
            clearStacks: () => cleared = true,
            logApplied: stacks => loggedStacks = stacks);

        Assert.AreEqual(4, affectedStacks);
        Assert.IsTrue(cleared);
        Assert.AreEqual(4, loggedStacks);
    }

    [Test]
    public void DispatchOperation_ReduceStacks_RemovesNormalizedAmountAndLogsOnlyWhenPositive()
    {
        int removedAmount = 0;
        int logCount = 0;

        int affectedStacks = BattleStatusStackOperationDispatcher.DispatchOperation(
            EnemySkill_SO.StatusStackOperationType.ReduceStacks,
            stackAmount: 0,
            getCurrentStacks: () => 99,
            removeStacks: amount =>
            {
                removedAmount = amount;
                return 0;
            },
            clearStacks: () => Assert.Fail("Reduce should not clear all stacks."),
            logApplied: _ => logCount++);

        Assert.AreEqual(0, affectedStacks);
        Assert.AreEqual(1, removedAmount);
        Assert.AreEqual(0, logCount);
    }

    [Test]
    public void DispatchEffects_AppliesSelfAndFilteredTargets()
    {
        EnemySkill_SO.StatusStackOperationEffectEntry[] effects =
        {
            new EnemySkill_SO.StatusStackOperationEffectEntry
            {
                trigger = EnemySkill_SO.DiceEffectTrigger.OnHit,
                targetMode = EnemySkill_SO.DamageTargetMode.Self
            },
            new EnemySkill_SO.StatusStackOperationEffectEntry
            {
                trigger = EnemySkill_SO.DiceEffectTrigger.OnHit,
                targetMode = EnemySkill_SO.DamageTargetMode.AllOpponents
            },
            new EnemySkill_SO.StatusStackOperationEffectEntry
            {
                trigger = EnemySkill_SO.DiceEffectTrigger.OnMiss,
                targetMode = EnemySkill_SO.DamageTargetMode.AllOpponents
            }
        };
        System.Collections.Generic.List<string> calls = new System.Collections.Generic.List<string>();

        BattleStatusStackOperationDispatcher.DispatchEffects(
            effects,
            EnemySkill_SO.DiceEffectTrigger.OnHit,
            "current",
            new System.Collections.Generic.List<string> { "A", "B" },
            _ => true,
            (effect, target) => target != "B",
            (mode, current, candidates, count) => new System.Collections.Generic.List<string>(candidates),
            target => target != null,
            (effect, index) => calls.Add($"self:{index}"),
            (target, effect, index) => calls.Add($"target:{target}:{index}"));

        CollectionAssert.AreEqual(
            new[]
            {
                "self:0",
                "target:A:1"
            },
            calls);
    }
}
