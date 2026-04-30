using System.Collections.Generic;
using NUnit.Framework;

public class BattleStatusTickPlanServiceTests
{
    [Test]
    public void ShouldProcessTick_RequiresAnyVisibleEffect()
    {
        Assert.IsFalse(BattleStatusTickPlanService.ShouldProcessTick(new BattleStatusTickResult()));
        Assert.IsTrue(BattleStatusTickPlanService.ShouldProcessTick(new BattleStatusTickResult { damage = 1 }));
        Assert.IsTrue(BattleStatusTickPlanService.ShouldProcessTick(new BattleStatusTickResult { speedBonus = -1 }));
        Assert.IsTrue(BattleStatusTickPlanService.ShouldProcessTick(new BattleStatusTickResult { appliedStacks = 1 }));
    }

    [Test]
    public void ShouldPlayTickEffect_RequiresStatusDataAndDamage()
    {
        BattleStatus_SO status = UnityEngine.ScriptableObject.CreateInstance<BattleStatus_SO>();

        Assert.IsFalse(BattleStatusTickPlanService.ShouldPlayTickEffect(new BattleStatusTickResult { statusData = status, damage = 0 }));
        Assert.IsFalse(BattleStatusTickPlanService.ShouldPlayTickEffect(new BattleStatusTickResult { statusData = null, damage = 1 }));
        Assert.IsTrue(BattleStatusTickPlanService.ShouldPlayTickEffect(new BattleStatusTickResult { statusData = status, damage = 1 }));
    }

    [Test]
    public void DispatchRoundStartTicks_SkipsInvisibleTicksAndPreservesCallbackOrder()
    {
        BattleStatus_SO status = UnityEngine.ScriptableObject.CreateInstance<BattleStatus_SO>();
        List<string> events = new List<string>();

        BattleStatusTickDispatcher.DispatchRoundStartTicks(
            new[]
            {
                new BattleStatusTickResult(),
                new BattleStatusTickResult { statusData = status, displayName = "Haste", speedBonus = 1 },
                new BattleStatusTickResult { statusData = status, displayName = "Burn", damage = 2 },
                new BattleStatusTickResult { appliedStatusName = "Poison", appliedStacks = 1 }
            },
            tick => events.Add($"resolve:{tick.displayName}"),
            tick => events.Add($"play:{tick.displayName}"),
            tick => events.Add($"damage:{tick.displayName}:{tick.damage}"),
            tick => events.Add($"speed:{tick.displayName}:{tick.speedBonus}"),
            tick => events.Add($"applied:{tick.appliedStatusName}:{tick.appliedStacks}"));

        CollectionAssert.AreEqual(
            new[]
            {
                "resolve:Haste",
                "speed:Haste:1",
                "resolve:Burn",
                "play:Burn",
                "damage:Burn:2",
                "applied:Poison:1"
            },
            events);
    }
}
