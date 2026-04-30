using NUnit.Framework;
using UnityEngine;

public class BattleSummonEffectServiceTests
{
    [Test]
    public void ShouldProcess_RequiresMatchingTriggerAndEnemyData()
    {
        EnemyData_SO enemyData = ScriptableObject.CreateInstance<EnemyData_SO>();
        EnemySkill_SO.SummonEnemyEffectEntry effect = new EnemySkill_SO.SummonEnemyEffectEntry
        {
            trigger = EnemySkill_SO.DiceEffectTrigger.OnSkillUse,
            enemyData = enemyData
        };

        Assert.IsTrue(BattleSummonEffectService.ShouldProcess(effect, EnemySkill_SO.DiceEffectTrigger.OnSkillUse));
        Assert.IsFalse(BattleSummonEffectService.ShouldProcess(effect, EnemySkill_SO.DiceEffectTrigger.OnClash));

        Object.DestroyImmediate(enemyData);
        effect.enemyData = null;
        Assert.IsFalse(BattleSummonEffectService.ShouldProcess(effect, EnemySkill_SO.DiceEffectTrigger.OnSkillUse));
    }

    [Test]
    public void NormalizeSummonCount_ClampsToAtLeastOne()
    {
        Assert.AreEqual(1, BattleSummonEffectService.NormalizeSummonCount(0));
        Assert.AreEqual(3, BattleSummonEffectService.NormalizeSummonCount(3));
    }

    [Test]
    public void BuildLabel_UsesEffectNameEnemyNameThenFallback()
    {
        EnemyData_SO enemyData = ScriptableObject.CreateInstance<EnemyData_SO>();
        enemyData.enemyName = "Slime";

        Assert.AreEqual(
            "Call",
            BattleSummonEffectService.BuildLabel(
                new EnemySkill_SO.SummonEnemyEffectEntry { effectName = "Call", enemyData = enemyData },
                0));

        Assert.AreEqual(
            "Slime",
            BattleSummonEffectService.BuildLabel(
                new EnemySkill_SO.SummonEnemyEffectEntry { enemyData = enemyData },
                0));

        Assert.AreEqual(
            "鍙敜鏁堟灉#2",
            BattleSummonEffectService.BuildLabel(default, 1));

        Object.DestroyImmediate(enemyData);
    }
}
