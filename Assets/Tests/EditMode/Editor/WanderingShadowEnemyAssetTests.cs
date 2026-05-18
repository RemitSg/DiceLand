using NUnit.Framework;
using UnityEditor;

public class WanderingShadowEnemyAssetTests
{
    private const string RemnantPath = "Assets/Data/Enemies/Enemies/032 WanderingRemnant/WanderingRemnant_Data.asset";
    private const string PhantomPath = "Assets/Data/Enemies/Enemies/033 WanderingPhantom/WanderingPhantom_Data.asset";
    private const string EchoPath = "Assets/Data/Enemies/Enemies/034 Echo/Echo_Data.asset";
    private const string GroupPath = "Assets/Data/Enemies/EnemyGroup/Events/Echo.asset";

    [Test]
    public void WanderingShadowEnemies_HaveExpectedCoreStatsAndBattleStartImmunity()
    {
        EnemyData_SO remnant = Load<EnemyData_SO>(RemnantPath);
        EnemyData_SO phantom = Load<EnemyData_SO>(PhantomPath);
        EnemyData_SO echo = Load<EnemyData_SO>(EchoPath);

        AssertEnemy(remnant, 32, "陌路残影", 3, 3, 5, 1, 1);
        AssertEnemy(phantom, 33, "陌路幻影", 10, 3, 5, 1, 1);
        AssertEnemy(echo, 34, "回响", 20, 1, 5, 2, 3);
    }

    [Test]
    public void WanderingPhantom_UsesThreeRoundSkillCycleAndSummonsRemnants()
    {
        EnemyData_SO remnant = Load<EnemyData_SO>(RemnantPath);
        EnemyData_SO phantom = Load<EnemyData_SO>(PhantomPath);

        Assert.AreEqual(3, phantom.conditionalSkillRules.Count);
        AssertRoundRule(phantom.conditionalSkillRules[0], "幻化", 1);
        AssertRoundRule(phantom.conditionalSkillRules[1], "融入暗影", 2);
        AssertRoundRule(phantom.conditionalSkillRules[2], "暗影", 3);

        EnemySkill_SO morph = phantom.conditionalSkillRules[0].skill;
        EnemySkill_SO.SummonEnemyEffectEntry summon = morph.GetDiceEffectSettings(0).summonEffects[0];
        Assert.AreSame(remnant, summon.enemyData);
        Assert.AreEqual(2, summon.summonCount);
    }

    [Test]
    public void Echo_UsesConfiguredEffectsAndEventGroup()
    {
        EnemyData_SO remnant = Load<EnemyData_SO>(RemnantPath);
        EnemyData_SO echo = Load<EnemyData_SO>(EchoPath);
        EnemyGroup_SO group = Load<EnemyGroup_SO>(GroupPath);

        Assert.AreEqual(1, echo.skillPool.Count);
        Assert.AreEqual(2, echo.conditionalSkillRules.Count);
        AssertRoundRule(echo.conditionalSkillRules[0], "幻化", 1);
        AssertRoundRule(echo.conditionalSkillRules[1], "模糊", 3);

        EnemySkill_SO silence = echo.skillPool[0];
        EnemySkill_SO.BuiltInEffectEntry stun = silence.GetDiceEffectSettings(0).builtInEffects[0];
        Assert.AreEqual(EnemySkill_SO.DiceEffectTrigger.OnHit, stun.trigger);
        Assert.AreEqual(EnemySkill_SO.StatusApplyTiming.NextRoundStart, stun.applyTiming);
        Assert.AreEqual("stun", stun.statusData.StatusId);

        EnemySkill_SO morph = echo.conditionalSkillRules[0].skill;
        Assert.AreSame(remnant, morph.GetDiceEffectSettings(0).summonEffects[0].enemyData);

        EnemySkill_SO blur = echo.conditionalSkillRules[1].skill;
        EnemySkill_SO.BuiltInEffectEntry strength = blur.GetDiceEffectSettings(0).builtInEffects[0];
        Assert.AreEqual(EnemySkill_SO.DamageTargetMode.AllFriendliesExceptSelf, strength.targetMode);
        Assert.AreEqual("strength", strength.statusData.StatusId);

        Assert.AreEqual(34, group.groupID);
        Assert.AreEqual(1, group.enemies.Count);
        Assert.AreSame(echo, group.enemies[0]);
    }

    private static void AssertEnemy(
        EnemyData_SO enemy,
        int id,
        string name,
        int hp,
        int minSpeed,
        int maxSpeed,
        int speedButtons,
        int immuneStacks)
    {
        Assert.AreEqual(id, enemy.enemyID);
        Assert.AreEqual(name, enemy.enemyName);
        Assert.AreEqual(hp, enemy.baseHP);
        Assert.AreEqual(minSpeed, enemy.minSpeed);
        Assert.AreEqual(maxSpeed, enemy.maxSpeed);
        Assert.AreEqual(speedButtons, enemy.baseSpeedButtonCount);
        Assert.AreEqual(immuneStacks, enemy.battleStartStatusGains[0].stackAmount);
        Assert.AreEqual("immune", enemy.battleStartStatusGains[0].statusData.StatusId);
    }

    private static void AssertRoundRule(EnemyData_SO.ConditionalSkillRule rule, string skillName, int firstRound)
    {
        Assert.AreEqual(EnemyData_SO.ConditionalSkillConditionType.RoundSequence, rule.conditionType);
        Assert.AreEqual(firstRound, rule.firstTriggerRound);
        Assert.AreEqual(3, rule.repeatEveryNRounds);
        Assert.AreEqual(skillName, rule.skill.skillName);
    }

    private static T Load<T>(string path) where T : UnityEngine.Object
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        Assert.IsNotNull(asset, path);
        return asset;
    }
}
