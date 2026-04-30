using System.Collections.Generic;
using NUnit.Framework;

public class BattleEnemyBuiltInTransferServiceTests
{
    [Test]
    public void IsTransferableNegativeStatus_RequiresPositiveStacksAndKnownNegativeId()
    {
        Assert.IsTrue(BattleEnemyBuiltInTransferService.IsTransferableNegativeStatus(new BattleStatusRuntimeEntry
        {
            fallbackStatusId = "burn",
            stacks = 2
        }));
        Assert.IsFalse(BattleEnemyBuiltInTransferService.IsTransferableNegativeStatus(new BattleStatusRuntimeEntry
        {
            fallbackStatusId = "burn",
            stacks = 0
        }));
        Assert.IsFalse(BattleEnemyBuiltInTransferService.IsTransferableNegativeStatus(new BattleStatusRuntimeEntry
        {
            fallbackStatusId = "strength",
            stacks = 2
        }));
    }

    [Test]
    public void BuildTransferableStatuses_FiltersNullAndNonTransferableStatuses()
    {
        List<BattleStatusRuntimeEntry> result = BattleEnemyBuiltInTransferService.BuildTransferableStatuses(
            new BattleStatusRuntimeEntry[]
            {
                null,
                new BattleStatusRuntimeEntry { fallbackStatusId = "poison", stacks = 1 },
                new BattleStatusRuntimeEntry { fallbackStatusId = "strength", stacks = 1 }
            });

        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("poison", result[0].Key);
    }

    [Test]
    public void ResolveTransferPlan_UsesInjectedRandomSelectionAndClampsTransferStacks()
    {
        List<int> randomInputs = new List<int>();
        int[] randomOutputs = { 1, 2 };
        int randomCallIndex = 0;

        BattleEnemyBuiltInTransferPlan plan = BattleEnemyBuiltInTransferService.ResolveTransferPlan(
            new List<BattleStatusRuntimeEntry>
            {
                new BattleStatusRuntimeEntry { fallbackStatusId = "poison", stacks = 3 },
                new BattleStatusRuntimeEntry { fallbackStatusId = "burn", stacks = 2 }
            },
            targetCount: 3,
            transferAmount: 0,
            randomRange: maxExclusive =>
            {
                randomInputs.Add(maxExclusive);
                return randomOutputs[randomCallIndex++];
            });

        Assert.IsTrue(plan.ShouldTransfer);
        Assert.AreEqual("burn", plan.SelectedStatus.Key);
        Assert.AreEqual(2, plan.TargetIndex);
        Assert.AreEqual(1, plan.TransferStacks);
        CollectionAssert.AreEqual(new[] { 2, 3 }, randomInputs);
    }

    [Test]
    public void ResolveTransferPlan_ReturnsNoTransferWhenNoTargetExists()
    {
        bool randomCalled = false;

        BattleEnemyBuiltInTransferPlan plan = BattleEnemyBuiltInTransferService.ResolveTransferPlan(
            new List<BattleStatusRuntimeEntry>
            {
                new BattleStatusRuntimeEntry { fallbackStatusId = "poison", stacks = 3 }
            },
            targetCount: 0,
            transferAmount: 2,
            randomRange: _ =>
            {
                randomCalled = true;
                return 0;
            });

        Assert.IsFalse(plan.ShouldTransfer);
        Assert.IsNull(plan.SelectedStatus);
        Assert.AreEqual(-1, plan.TargetIndex);
        Assert.AreEqual(0, plan.TransferStacks);
        Assert.IsFalse(randomCalled);
    }

    [Test]
    public void TransferDispatcher_RemovesStacksAddsRemovedStacksThenLogsSuccess()
    {
        BattleStatusRuntimeEntry status = new BattleStatusRuntimeEntry { fallbackStatusId = "burn", stacks = 3 };
        BattleEnemyBuiltInTransferPlan plan = new BattleEnemyBuiltInTransferPlan(true, status, 0, 2);
        List<string> events = new List<string>();

        bool transferred = BattleEnemyBuiltInTransferDispatcher.TryDispatch(
            plan,
            (selectedStatus, requestedStacks) =>
            {
                events.Add($"remove:{selectedStatus.Key}:{requestedStacks}");
                return 1;
            },
            (selectedStatus, removedStacks) =>
            {
                events.Add($"add:{selectedStatus.Key}:{removedStacks}");
                return true;
            },
            (selectedStatus, removedStacks) => events.Add($"log:{selectedStatus.Key}:{removedStacks}"));

        Assert.IsTrue(transferred);
        CollectionAssert.AreEqual(new[] { "remove:burn:2", "add:burn:1", "log:burn:1" }, events);
    }

    [Test]
    public void TransferDispatcher_ReturnsFalseWithoutAddingWhenNothingWasRemoved()
    {
        BattleStatusRuntimeEntry status = new BattleStatusRuntimeEntry { fallbackStatusId = "burn", stacks = 3 };
        BattleEnemyBuiltInTransferPlan plan = new BattleEnemyBuiltInTransferPlan(true, status, 0, 2);
        List<string> events = new List<string>();

        bool transferred = BattleEnemyBuiltInTransferDispatcher.TryDispatch(
            plan,
            (selectedStatus, requestedStacks) =>
            {
                events.Add($"remove:{selectedStatus.Key}:{requestedStacks}");
                return 0;
            },
            (selectedStatus, removedStacks) =>
            {
                events.Add($"add:{selectedStatus.Key}:{removedStacks}");
                return true;
            },
            (selectedStatus, removedStacks) => events.Add($"log:{selectedStatus.Key}:{removedStacks}"));

        Assert.IsFalse(transferred);
        CollectionAssert.AreEqual(new[] { "remove:burn:2" }, events);
    }
}
