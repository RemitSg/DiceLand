using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class EnemyTodoTests
{
    private const EnemyData_SO.ConditionalSkillConditionType AliveEnemyCountAtMostConditionType = (EnemyData_SO.ConditionalSkillConditionType)2;

    private readonly List<Object> createdObjects = new List<Object>();

    [TearDown]
    public void TearDown()
    {
        for (int i = createdObjects.Count - 1; i >= 0; i--)
        {
            Object created = createdObjects[i];
            if (created != null)
                Object.DestroyImmediate(created);
        }

        createdObjects.Clear();
    }

    [Test]
    public void ConditionalSkillRule_OnlyTriggersWhenAliveEnemyCountIsAtMostThreshold()
    {
        EnemySkill_SO normalSkill = CreateSkill("Normal");
        EnemySkill_SO splitSkill = CreateSkill("Split");

        EnemyData_SO.ConditionalSkillRule splitRule = new EnemyData_SO.ConditionalSkillRule
        {
            ruleName = "Split When Alone",
            skill = splitSkill,
            conditionType = AliveEnemyCountAtMostConditionType,
            firstTriggerRound = 1,
            repeatEveryNRounds = 1,
            priority = 10,
            maxSlotsPerRound = 1,
            useOncePerBattle = false
        };
        SetStructField(ref splitRule, "requiredAliveEnemyCount", 1);

        EnemyData_SO enemyData = CreateEnemyData("Splitter", 16, normalSkill, splitRule);

        BattleEnemyController loneEnemy = CreateEnemyController("Lone Enemy", enemyData);
        loneEnemy.SelectRandomSkill(1);
        Assert.AreEqual(splitSkill, loneEnemy.SelectedSkill);

        BattleEnemyController pairEnemyA = CreateEnemyController("Pair Enemy A", enemyData);
        BattleEnemyController pairEnemyB = CreateEnemyController("Pair Enemy B", enemyData);
        pairEnemyA.SelectRandomSkill(1);
        pairEnemyB.SelectRandomSkill(1);

        Assert.AreEqual(normalSkill, pairEnemyA.SelectedSkill);
        Assert.AreEqual(normalSkill, pairEnemyB.SelectedSkill);
    }

    [Test]
    public void NextAttackSkillPowerBonus_StacksAndConsumesAfterAttackSkillIsUsed()
    {
        EnemySkill_SO attackSkill = CreateSkill("Attack", 2, 3);
        EnemySkill_SO defenseSkill = CreateSkill("Defense", 4, 5, EnemySkill_SO.SkillCategory.Defense);
        attackSkill.allowedEnemyStages = new List<int> { 1, 2 };

        EnemyData_SO enemyData = CreateEnemyData("Gatekeeper", 40, attackSkill, default);
        enemyData.stageSprites = new List<EnemyData_SO.EnemyStageEntry>
        {
            new EnemyData_SO.EnemyStageEntry { stageName = "Stage 1" },
            new EnemyData_SO.EnemyStageEntry { stageName = "Stage 2" }
        };
        createdObjects.Add(enemyData);

        BattleEnemyController enemy = CreateEnemyController("Gatekeeper", enemyData);
        SpeedButton speedButton = CreateSpeedButton(enemy.gameObject, "Speed Button");
        enemy.RegisterSpeedButtons(new[] { speedButton });
        enemy.ApplyEnemyData(enemyData);
        enemy.TryTransitionToStage(1);

        enemy.QueueNextAttackSkillPowerBonus(1);
        enemy.QueueNextAttackSkillPowerBonus(1);

        EnemySkill_SO.DicePowerRange previewAttackRange = enemy.GetPreviewDicePowerRange(attackSkill, 0);
        EnemySkill_SO.DicePowerRange previewDefenseRange = enemy.GetPreviewDicePowerRange(defenseSkill, 0);

        Assert.AreEqual(4, previewAttackRange.minPower);
        Assert.AreEqual(5, previewAttackRange.maxPower);
        Assert.AreEqual(4, previewDefenseRange.minPower);
        Assert.AreEqual(5, previewDefenseRange.maxPower);

        enemy.SelectRandomSkill();
        SetSelectedSkill(enemy, attackSkill);
        enemy.ActivateSelectedSkillForButton(speedButton);

        EnemySkill_SO.DicePowerRange activeAttackRange = enemy.GetDicePowerRange(0);
        Assert.AreEqual(4, activeAttackRange.minPower);
        Assert.AreEqual(5, activeAttackRange.maxPower);

        EnemySkill_SO.DicePowerRange stillQueuedPreviewRange = enemy.GetPreviewDicePowerRange(attackSkill, 0);
        Assert.AreEqual(4, stillQueuedPreviewRange.minPower);
        Assert.AreEqual(5, stillQueuedPreviewRange.maxPower);

        enemy.ConsumeQueuedAttackSkillPowerBonusAfterAttackSkillUse();

        EnemySkill_SO.DicePowerRange consumedPreviewRange = enemy.GetPreviewDicePowerRange(attackSkill, 0);
        Assert.AreEqual(2, consumedPreviewRange.minPower);
        Assert.AreEqual(3, consumedPreviewRange.maxPower);
    }

    [Test]
    public void TakeDamage_CollapsesAllLinkedLastStandEnemiesWhenEveryLivingAllyReachesOneHp()
    {
        EnemySkill_SO skill = CreateSkill("Scratch");
        EnemyData_SO enemyData = CreateEnemyData("Collector", 10, skill, default);
        enemyData.surviveAtOneHpUntilAllLivingAlliesAtOrBelowOneHp = true;

        BattleEnemyController enemyA = CreateEnemyController("Collector A", enemyData);
        BattleEnemyController enemyB = CreateEnemyController("Collector B", enemyData);
        BattleEnemyController enemyC = CreateEnemyController("Collector C", enemyData);

        int firstDamage = enemyA.TakeDamage(99);
        Assert.AreEqual(9, firstDamage);
        Assert.AreEqual(1, enemyA.CurrentHP);
        Assert.IsFalse(enemyA.IsDead);

        int secondDamage = enemyB.TakeDamage(99);
        Assert.AreEqual(9, secondDamage);
        Assert.AreEqual(1, enemyB.CurrentHP);
        Assert.IsFalse(enemyB.IsDead);

        int finalDamage = enemyC.TakeDamage(99);
        Assert.Greater(finalDamage, 0);
        Assert.AreEqual(0, enemyA.CurrentHP);
        Assert.AreEqual(0, enemyB.CurrentHP);
        Assert.AreEqual(0, enemyC.CurrentHP);
        Assert.IsTrue(enemyA.IsDead);
        Assert.IsTrue(enemyB.IsDead);
        Assert.IsTrue(enemyC.IsDead);
    }

    private BattleEnemyController CreateEnemyController(string name, EnemyData_SO enemyData)
    {
        GameObject go = new GameObject(name);
        createdObjects.Add(go);

        BattleEnemyController controller = go.AddComponent<BattleEnemyController>();
        controller.ApplyEnemyData(enemyData);
        return controller;
    }

    private EnemyData_SO CreateEnemyData(string name, int hp, EnemySkill_SO normalSkill, EnemyData_SO.ConditionalSkillRule splitRule)
    {
        EnemyData_SO enemyData = ScriptableObject.CreateInstance<EnemyData_SO>();
        enemyData.enemyName = name;
        enemyData.baseHP = hp;
        enemyData.skillPool = new List<EnemySkill_SO> { normalSkill };
        enemyData.conditionalSkillRules = splitRule.skill != null
            ? new List<EnemyData_SO.ConditionalSkillRule> { splitRule }
            : new List<EnemyData_SO.ConditionalSkillRule>();
        createdObjects.Add(enemyData);
        return enemyData;
    }

    private EnemySkill_SO CreateSkill(string name, int minPower = 1, int maxPower = 2, EnemySkill_SO.SkillCategory category = EnemySkill_SO.SkillCategory.Attack)
    {
        EnemySkill_SO skill = ScriptableObject.CreateInstance<EnemySkill_SO>();
        skill.skillName = name;
        skill.skillCategory = category;
        skill.diceCount = 1;
        skill.dicePowerRanges = new[]
        {
            new EnemySkill_SO.DicePowerRange
            {
                minPower = minPower,
                maxPower = maxPower
            }
        };
        createdObjects.Add(skill);
        return skill;
    }

    private SpeedButton CreateSpeedButton(GameObject parent, string name)
    {
        GameObject speedButtonObject = new GameObject(name);
        speedButtonObject.transform.SetParent(parent.transform, false);
        speedButtonObject.AddComponent<LineRenderer>();
        SpeedButton speedButton = speedButtonObject.AddComponent<SpeedButton>();
        createdObjects.Add(speedButtonObject);
        return speedButton;
    }

    private static void SetSelectedSkill(BattleEnemyController enemy, EnemySkill_SO skill)
    {
        PropertyInfo selectedSkillProperty = typeof(BattleEnemyController).GetProperty("SelectedSkill", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.IsNotNull(selectedSkillProperty);
        selectedSkillProperty.SetValue(enemy, skill);
    }

    private static void SetStructField<TStruct, TValue>(ref TStruct target, string fieldName, TValue value) where TStruct : struct
    {
        object boxed = target;
        FieldInfo field = boxed.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.IsNotNull(field, $"Field {fieldName} was not found on {boxed.GetType().Name}.");
        field.SetValue(boxed, value);
        target = (TStruct)boxed;
    }
}
