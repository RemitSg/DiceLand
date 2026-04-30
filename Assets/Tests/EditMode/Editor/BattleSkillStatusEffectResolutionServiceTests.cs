using System.Linq;
using NUnit.Framework;

public class BattleSkillStatusEffectResolutionServiceTests
{
    [Test]
    public void FilterEffects_WhenConditionalOnly_SkipsNonConditionalEffects()
    {
        TestEffect[] effects =
        {
            new TestEffect("normal", conditional: false),
            new TestEffect("conditional", conditional: true)
        };

        TestEffect[] result = BattleSkillStatusEffectResolutionService
            .FilterEffects(effects, conditionalOnly: true, isConditional: effect => effect.Conditional)
            .ToArray();

        CollectionAssert.AreEqual(new[] { "conditional" }, result.Select(effect => effect.Id));
    }

    [Test]
    public void BuildEligibleStatusEffects_PreservesOriginalIndexes()
    {
        EnemySkill_SO.StatusEffectEntry[] effects =
        {
            new EnemySkill_SO.StatusEffectEntry { trigger = EnemySkill_SO.DiceEffectTrigger.OnMiss, stackAmount = 1 },
            new EnemySkill_SO.StatusEffectEntry { trigger = EnemySkill_SO.DiceEffectTrigger.OnHit, stackAmount = 1 },
            new EnemySkill_SO.StatusEffectEntry { trigger = EnemySkill_SO.DiceEffectTrigger.OnHit, stackAmount = 1 }
        };

        BattleIndexedEffect<EnemySkill_SO.StatusEffectEntry>[] result = BattleSkillStatusEffectResolutionService
            .BuildEligibleStatusEffects(
                effects,
                EnemySkill_SO.DiceEffectTrigger.OnHit,
                conditionalOnly: false)
            .ToArray();

        CollectionAssert.AreEqual(new[] { 1, 2 }, result.Select(effect => effect.Index));
    }

    private readonly struct TestEffect
    {
        public TestEffect(string id, bool conditional)
        {
            Id = id;
            Conditional = conditional;
        }

        public string Id { get; }
        public bool Conditional { get; }
    }
}
