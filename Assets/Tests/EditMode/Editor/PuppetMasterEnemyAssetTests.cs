using NUnit.Framework;
using UnityEditor;

public class PuppetMasterEnemyAssetTests
{
    private const string MasterPath = "Assets/Data/Enemies/Enemies/035 PuppetMaster/PuppetMaster_Data.asset";
    private const string PuppetPath = "Assets/Data/Enemies/Enemies/3501 SoullessPuppet/SoullessPuppet_Data.asset";
    private const string GroupPath = "Assets/Data/Enemies/EnemyGroup/Events/PuppetMaster.asset";

    [Test]
    public void PuppetMasterEncounter_HasExpectedEnemyGroupComposition()
    {
        EnemyData_SO master = Load<EnemyData_SO>(MasterPath);
        EnemyData_SO puppet = Load<EnemyData_SO>(PuppetPath);
        EnemyGroup_SO group = Load<EnemyGroup_SO>(GroupPath);

        Assert.AreEqual(35, group.groupID);
        Assert.AreEqual(3, group.enemies.Count);
        Assert.AreSame(puppet, group.enemies[0]);
        Assert.AreSame(puppet, group.enemies[1]);
        Assert.AreSame(master, group.enemies[2]);
        Assert.AreEqual(1, group.attributes.Count);
        Assert.AreEqual("暗", group.attributes[0].attributeName);
    }

    [Test]
    public void PuppetMasterAndPuppet_HaveExpectedCoreStatsAndPassives()
    {
        EnemyData_SO master = Load<EnemyData_SO>(MasterPath);
        EnemyData_SO puppet = Load<EnemyData_SO>(PuppetPath);

        AssertEnemy(master, 35, "牵丝傀儡师", 35, 1, 5, 2);
        AssertEnemy(puppet, 3501, "无神傀儡", 20, 3, 6, 1);
        Assert.AreEqual("牵丝", master.passiveDescriptions[0].passiveName);
        Assert.AreEqual("无神", puppet.passiveDescriptions[0].passiveName);
    }

    [Test]
    public void PuppetSkills_HaveExpectedStatusAndRoundRules()
    {
        EnemyData_SO master = Load<EnemyData_SO>(MasterPath);
        EnemyData_SO puppet = Load<EnemyData_SO>(PuppetPath);

        Assert.AreEqual(1, master.skillPool.Count);
        Assert.AreEqual("奉献指示", master.skillPool[0].skillName);
        Assert.AreEqual(1, master.conditionalSkillRules.Count);
        Assert.AreEqual(1, master.conditionalSkillRules[0].repeatEveryNRounds);
        Assert.AreEqual(1, master.conditionalSkillRules[0].maxSlotsPerRound);

        EnemySkill_SO manipulate = master.conditionalSkillRules[0].skill;
        Assert.AreEqual("操纵", manipulate.skillName);
        Assert.AreEqual(2, manipulate.diceCount);
        Assert.AreEqual(4, manipulate.GetDiceEffectSettings(0).statusEffects.Length);
        Assert.AreEqual(3, manipulate.GetDiceEffectSettings(1).statusEffects.Length);
        Assert.AreEqual("target", manipulate.GetDiceEffectSettings(0).statusEffects[3].statusData.StatusId);

        EnemySkill_SO offeringOrder = master.skillPool[0];
        Assert.AreEqual("奉献指示", offeringOrder.skillName);
        Assert.AreEqual(3, offeringOrder.GetDiceEffectSettings(0).damageEffects[0].damageAmount);
        Assert.AreEqual("strength", offeringOrder.GetDiceEffectSettings(0).statusEffects[0].statusData.StatusId);

        Assert.AreEqual(1, puppet.conditionalSkillRules.Count);
        EnemyData_SO.ConditionalSkillRule sacrificeRule = puppet.conditionalSkillRules[0];
        Assert.AreEqual(EnemyData_SO.ConditionalSkillConditionType.RoundSequence, sacrificeRule.conditionType);
        Assert.AreEqual(3, sacrificeRule.firstTriggerRound);
        Assert.AreEqual(2, sacrificeRule.repeatEveryNRounds);
        Assert.AreEqual("牺牲", sacrificeRule.skill.skillName);
    }

    private static void AssertEnemy(EnemyData_SO enemy, int id, string name, int hp, int minSpeed, int maxSpeed, int speedButtons)
    {
        Assert.AreEqual(id, enemy.enemyID);
        Assert.AreEqual(name, enemy.enemyName);
        Assert.AreEqual(hp, enemy.baseHP);
        Assert.AreEqual(minSpeed, enemy.minSpeed);
        Assert.AreEqual(maxSpeed, enemy.maxSpeed);
        Assert.AreEqual(speedButtons, enemy.baseSpeedButtonCount);
    }

    private static T Load<T>(string path) where T : UnityEngine.Object
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        Assert.IsNotNull(asset, path);
        return asset;
    }
}
