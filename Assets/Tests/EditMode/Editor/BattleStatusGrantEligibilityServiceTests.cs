using System.Collections.Generic;
using NUnit.Framework;

public class BattleStatusGrantEligibilityServiceTests
{
    [Test]
    public void ShouldApplyGrant_RequiresStatusDataAndPositiveStacks()
    {
        BattleStatus_SO status = UnityEngine.ScriptableObject.CreateInstance<BattleStatus_SO>();

        Assert.IsFalse(BattleStatusGrantEligibilityService.ShouldApplyGrant(null, 1));
        Assert.IsFalse(BattleStatusGrantEligibilityService.ShouldApplyGrant(status, 0));
        Assert.IsTrue(BattleStatusGrantEligibilityService.ShouldApplyGrant(status, 1));
    }

    [Test]
    public void DispatchGrants_SkipsInvalidGrantsAndUnavailableTargetsAndLogsSuccessfulAdds()
    {
        BattleStatus_SO status = UnityEngine.ScriptableObject.CreateInstance<BattleStatus_SO>();
        PendingBattleStatusGrant validGrant = new PendingBattleStatusGrant { statusData = status, stacks = 2 };
        List<string> events = new List<string>();

        BattleStatusGrantDispatcher.DispatchGrants(
            new[]
            {
                null,
                new PendingBattleStatusGrant { statusData = null, stacks = 2 },
                new PendingBattleStatusGrant { statusData = status, stacks = 0 },
                validGrant
            },
            new[] { "dead", "alive" },
            target => target == "dead",
            (target, grant) =>
            {
                events.Add($"add:{target}:{grant.stacks}");
                return target == "alive";
            },
            (target, grant) => events.Add($"log:{target}:{grant.stacks}"));

        CollectionAssert.AreEqual(new[] { "add:alive:2", "log:alive:2" }, events);
    }

    [Test]
    public void DispatchEnemyStatusGains_ReturnsWhetherAnyEntryAppliedAndLogsCurrentStacks()
    {
        BattleStatus_SO status = UnityEngine.ScriptableObject.CreateInstance<BattleStatus_SO>();
        List<string> events = new List<string>();

        bool appliedAny = BattleEnemyStatusGainDispatcher.Dispatch(
            new[]
            {
                new EnemyData_SO.StatusGainEntry { statusData = null, stackAmount = 2 },
                new EnemyData_SO.StatusGainEntry { statusData = status, stackAmount = 0 },
                new EnemyData_SO.StatusGainEntry { statusData = status, stackAmount = 2 }
            },
            entry =>
            {
                events.Add($"add:{entry.stackAmount}");
                return true;
            },
            entry => entry.stackAmount + 3,
            (entry, currentStacks) => events.Add($"log:{entry.stackAmount}:{currentStacks}"));

        Assert.IsTrue(appliedAny);
        CollectionAssert.AreEqual(new[] { "add:2", "log:2:5" }, events);
    }
}
