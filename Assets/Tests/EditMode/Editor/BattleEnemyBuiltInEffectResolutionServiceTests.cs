using System.Linq;
using NUnit.Framework;

public class BattleEnemyBuiltInEffectResolutionServiceTests
{
    [Test]
    public void FilterByTrigger_OnlyIncludesEffectsMatchingTrigger()
    {
        TestBuiltInEffect[] effects =
        {
            new TestBuiltInEffect("on-use", EnemySkill_SO.DiceEffectTrigger.OnSkillUse),
            new TestBuiltInEffect("on-hit", EnemySkill_SO.DiceEffectTrigger.OnHit)
        };

        TestBuiltInEffect[] result = BattleEnemyBuiltInEffectResolutionService
            .FilterByTrigger(effects, EnemySkill_SO.DiceEffectTrigger.OnHit, effect => effect.Trigger)
            .ToArray();

        CollectionAssert.AreEqual(new[] { "on-hit" }, result.Select(effect => effect.Id));
    }

    [Test]
    public void BuildEligibleEffects_PreservesIndexesAndAppliesCondition()
    {
        EnemySkill_SO.BuiltInEffectEntry[] effects =
        {
            new EnemySkill_SO.BuiltInEffectEntry { trigger = EnemySkill_SO.DiceEffectTrigger.OnHit, effectType = EnemySkill_SO.BuiltInEffectType.Heal, amount = 1 },
            new EnemySkill_SO.BuiltInEffectEntry { trigger = EnemySkill_SO.DiceEffectTrigger.OnHit, effectType = EnemySkill_SO.BuiltInEffectType.Damage, amount = 1 },
            new EnemySkill_SO.BuiltInEffectEntry { trigger = EnemySkill_SO.DiceEffectTrigger.OnMiss, effectType = EnemySkill_SO.BuiltInEffectType.ApplyStatus, statusStackAmount = 1 }
        };

        BattleIndexedEffect<EnemySkill_SO.BuiltInEffectEntry>[] result = BattleEnemyBuiltInEffectResolutionService
            .BuildEligibleEffects(
                effects,
                EnemySkill_SO.DiceEffectTrigger.OnHit,
                effect => effect.effectType == EnemySkill_SO.BuiltInEffectType.Damage)
            .ToArray();

        Assert.AreEqual(1, result.Length);
        Assert.AreEqual(1, result[0].Index);
        Assert.AreEqual(EnemySkill_SO.BuiltInEffectType.Damage, result[0].Effect.effectType);
    }

    [Test]
    public void Dispatch_RoutesHealDamageStatusAndTransferWithSelectedTargets()
    {
        BattleIndexedEffect<EnemySkill_SO.BuiltInEffectEntry>[] effects =
        {
            new BattleIndexedEffect<EnemySkill_SO.BuiltInEffectEntry>(
                0,
                new EnemySkill_SO.BuiltInEffectEntry
                {
                    effectType = EnemySkill_SO.BuiltInEffectType.Heal,
                    targetMode = EnemySkill_SO.DamageTargetMode.Self,
                    amount = 2
                }),
            new BattleIndexedEffect<EnemySkill_SO.BuiltInEffectEntry>(
                1,
                new EnemySkill_SO.BuiltInEffectEntry
                {
                    effectType = EnemySkill_SO.BuiltInEffectType.Damage,
                    targetMode = EnemySkill_SO.DamageTargetMode.AllOpponents,
                    amount = 3
                }),
            new BattleIndexedEffect<EnemySkill_SO.BuiltInEffectEntry>(
                2,
                new EnemySkill_SO.BuiltInEffectEntry
                {
                    effectType = EnemySkill_SO.BuiltInEffectType.ApplyStatus,
                    targetMode = EnemySkill_SO.DamageTargetMode.CurrentTarget,
                    statusStackAmount = 1
                }),
            new BattleIndexedEffect<EnemySkill_SO.BuiltInEffectEntry>(
                3,
                new EnemySkill_SO.BuiltInEffectEntry
                {
                    effectType = EnemySkill_SO.BuiltInEffectType.TransferRandomNegativeStatus,
                    targetMode = EnemySkill_SO.DamageTargetMode.RandomOpponents,
                    randomTargetCount = 1,
                    amount = 1
                })
        };
        System.Collections.Generic.List<string> calls = new System.Collections.Generic.List<string>();

        BattleEnemyBuiltInEffectDispatcher.Dispatch(
            effects,
            "A",
            new System.Collections.Generic.List<string> { "A", "B" },
            (effect, index) => $"E{index}",
            (mode, current, candidates, count) =>
            {
                if (mode == EnemySkill_SO.DamageTargetMode.CurrentTarget)
                    return new System.Collections.Generic.List<string> { current };
                if (mode == EnemySkill_SO.DamageTargetMode.RandomOpponents)
                    return new System.Collections.Generic.List<string> { candidates[1] };
                return new System.Collections.Generic.List<string>(candidates);
            },
            target => !string.IsNullOrEmpty(target),
            (effect, index, label) => calls.Add($"self-heal:{label}:{effect.amount}"),
            (target, effect, index, label) => calls.Add($"target-heal:{target}:{label}"),
            (effect, index, label) => calls.Add($"self-damage:{label}"),
            (target, effect, index, label) => calls.Add($"target-damage:{target}:{label}:{effect.amount}"),
            (effect, index, label) => calls.Add($"self-status:{label}"),
            (target, effect, index, label) => calls.Add($"target-status:{target}:{label}:{effect.statusStackAmount}"),
            (targets, effect, index, label) => calls.Add($"transfer:{string.Join(",", targets)}:{label}"));

        CollectionAssert.AreEqual(
            new[]
            {
                "self-heal:E0:2",
                "target-damage:A:E1:3",
                "target-damage:B:E1:3",
                "target-status:A:E2:1",
                "transfer:B:E3"
            },
            calls);
    }

    private readonly struct TestBuiltInEffect
    {
        public TestBuiltInEffect(string id, EnemySkill_SO.DiceEffectTrigger trigger)
        {
            Id = id;
            Trigger = trigger;
        }

        public string Id { get; }
        public EnemySkill_SO.DiceEffectTrigger Trigger { get; }
    }
}
