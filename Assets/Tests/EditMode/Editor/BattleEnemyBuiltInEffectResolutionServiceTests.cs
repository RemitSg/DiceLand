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
