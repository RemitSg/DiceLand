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
}
