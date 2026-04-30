using System.Collections.Generic;
using NUnit.Framework;

public class BattleHealingStatusStackSourceServiceTests
{
    [Test]
    public void ResolveStatusStackSource_Self_UsesSelfStacks()
    {
        int result = BattleHealingStatusStackSourceService.ResolveStatusStackSource(
            EnemySkill_SO.DamageTargetMode.Self,
            self: "self",
            currentTarget: "target",
            candidates: new List<string> { "target" },
            randomTargetCount: 1,
            resolveSelfStacks: value => value == "self" ? 3 : 0,
            resolveTargetStacks: _ => 9,
            selectTargets: (_, _, _, _) => new List<string> { "target" });

        Assert.AreEqual(3, result);
    }

    [Test]
    public void ResolveStatusStackSource_TargetMode_UsesFirstSelectedTarget()
    {
        int result = BattleHealingStatusStackSourceService.ResolveStatusStackSource(
            EnemySkill_SO.DamageTargetMode.AllOpponents,
            self: "self",
            currentTarget: "target",
            candidates: new List<string> { "A", "B" },
            randomTargetCount: 1,
            resolveSelfStacks: _ => 0,
            resolveTargetStacks: value => value == "B" ? 4 : 1,
            selectTargets: (_, _, _, _) => new List<string> { "B", "A" });

        Assert.AreEqual(4, result);
    }

    [Test]
    public void ResolveStatusStackSource_NoSelectedTarget_ReturnsZero()
    {
        int result = BattleHealingStatusStackSourceService.ResolveStatusStackSource(
            EnemySkill_SO.DamageTargetMode.CurrentTarget,
            self: "self",
            currentTarget: null,
            candidates: new List<string>(),
            randomTargetCount: 1,
            resolveSelfStacks: _ => 3,
            resolveTargetStacks: _ => 4,
            selectTargets: (_, _, _, _) => new List<string>());

        Assert.AreEqual(0, result);
    }
}
