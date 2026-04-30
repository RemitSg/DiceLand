using NUnit.Framework;

public class BattleStatusApplicationDispatcherTests
{
    [Test]
    public void Execute_UsesDelayedCallbackForDelayedPlan()
    {
        bool queued = false;
        bool added = false;

        bool result = BattleStatusApplicationDispatcher.Execute(
            new BattleStatusApplicationPlan(true, true, 2, 3, 4),
            queueDelayed: (stacks, delay, repeat) =>
            {
                queued = stacks == 2 && delay == 3 && repeat == 4;
                return true;
            },
            addImmediate: stacks =>
            {
                added = true;
                return true;
            },
            onBlocked: null);

        Assert.IsTrue(result);
        Assert.IsTrue(queued);
        Assert.IsFalse(added);
    }

    [Test]
    public void Execute_UsesImmediateCallbackForImmediatePlan()
    {
        bool added = false;

        bool result = BattleStatusApplicationDispatcher.Execute(
            new BattleStatusApplicationPlan(true, false, 5, 1, 1),
            queueDelayed: (stacks, delay, repeat) => true,
            addImmediate: stacks =>
            {
                added = stacks == 5;
                return true;
            },
            onBlocked: null);

        Assert.IsTrue(result);
        Assert.IsTrue(added);
    }

    [Test]
    public void Execute_CallsBlockedCallbackWhenApplicationFails()
    {
        bool blocked = false;

        bool result = BattleStatusApplicationDispatcher.Execute(
            new BattleStatusApplicationPlan(true, false, 1, 1, 1),
            queueDelayed: null,
            addImmediate: _ => false,
            onBlocked: () => blocked = true);

        Assert.IsFalse(result);
        Assert.IsTrue(blocked);
    }

    [Test]
    public void Execute_ReturnsFalseForInactivePlan()
    {
        bool result = BattleStatusApplicationDispatcher.Execute(
            new BattleStatusApplicationPlan(false, false, 0, 1, 1),
            queueDelayed: (stacks, delay, repeat) => true,
            addImmediate: stacks => true,
            onBlocked: null);

        Assert.IsFalse(result);
    }
}
