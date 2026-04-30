using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;

public class BattleCombatActionExecutionDispatcherTests
{
    [Test]
    public void Dispatch_ExecutesEachActionThenChecksCompletion()
    {
        List<string> calls = new List<string>();
        List<BattleCombatAction> actions = new List<BattleCombatAction>
        {
            new BattleCombatAction { Type = BattleCombatActionType.Clash },
            new BattleCombatAction { Type = BattleCombatActionType.AllySingleAttack },
            new BattleCombatAction { Type = BattleCombatActionType.EnemySingleAttack }
        };

        IEnumerator routine = BattleCombatActionExecutionDispatcher.Dispatch(
            actions,
            action => RecordAction(calls, "clash"),
            action => RecordAction(calls, "ally"),
            action => RecordAction(calls, "enemy"),
            () =>
            {
                calls.Add("check");
                return false;
            },
            () => calls.Add("completed"));

        Drain(routine);

        CollectionAssert.AreEqual(
            new[]
            {
                "clash:start",
                "clash:end",
                "check",
                "ally:start",
                "ally:end",
                "check",
                "enemy:start",
                "enemy:end",
                "check"
            },
            calls);
    }

    [Test]
    public void Dispatch_WhenCompletionCheckSucceeds_StopsBeforeRemainingActions()
    {
        List<string> calls = new List<string>();
        int checks = 0;
        BattleCombatActionExecutionResult result = new BattleCombatActionExecutionResult();
        List<BattleCombatAction> actions = new List<BattleCombatAction>
        {
            new BattleCombatAction { Type = BattleCombatActionType.AllySingleAttack },
            new BattleCombatAction { Type = BattleCombatActionType.EnemySingleAttack },
            new BattleCombatAction { Type = BattleCombatActionType.Clash }
        };

        IEnumerator routine = BattleCombatActionExecutionDispatcher.Dispatch(
            actions,
            action => RecordAction(calls, "clash"),
            action => RecordAction(calls, "ally"),
            action => RecordAction(calls, "enemy"),
            () =>
            {
                checks++;
                calls.Add($"check:{checks}");
                return checks == 2;
            },
            () => calls.Add("completed"),
            result);

        Drain(routine);

        CollectionAssert.AreEqual(
            new[]
            {
                "ally:start",
                "ally:end",
                "check:1",
                "enemy:start",
                "enemy:end",
                "check:2",
                "completed"
            },
            calls);
        Assert.IsTrue(result.BattleCompletedDuringAction);
    }

    [Test]
    public void Dispatch_SkipsNullActionsAndAllowsMissingCallbacks()
    {
        Assert.DoesNotThrow(() => Drain(BattleCombatActionExecutionDispatcher.Dispatch(
            new List<BattleCombatAction> { null, new BattleCombatAction { Type = BattleCombatActionType.Clash } },
            null,
            null,
            null,
            null,
            null)));
    }

    [Test]
    public void Dispatch_WithExecutionContext_ExposesCompletionResult()
    {
        List<string> calls = new List<string>();
        BattleCombatActionExecutionContext context = new BattleCombatActionExecutionContext(
            resolveClash: action => RecordAction(calls, "clash"),
            resolveAllySingleAttack: action => RecordAction(calls, "ally"),
            resolveEnemySingleAttack: action => RecordAction(calls, "enemy"),
            isBattleCompleted: () => true,
            onBattleCompleted: () => calls.Add("completed"));

        Drain(BattleCombatActionExecutionDispatcher.Dispatch(
            new List<BattleCombatAction> { new BattleCombatAction { Type = BattleCombatActionType.Clash } },
            context));

        CollectionAssert.AreEqual(
            new[]
            {
                "clash:start",
                "clash:end",
                "completed"
            },
            calls);
        Assert.IsTrue(context.Result.BattleCompletedDuringAction);
    }

    private static IEnumerator RecordAction(List<string> calls, string label)
    {
        calls.Add($"{label}:start");
        yield return null;
        calls.Add($"{label}:end");
    }

    private static void Drain(IEnumerator routine)
    {
        while (routine != null && routine.MoveNext())
        {
            if (routine.Current is IEnumerator nested)
                Drain(nested);
        }
    }
}
