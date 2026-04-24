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
        enemyData.conditionalSkillRules = new List<EnemyData_SO.ConditionalSkillRule> { splitRule };
        createdObjects.Add(enemyData);
        return enemyData;
    }

    private EnemySkill_SO CreateSkill(string name)
    {
        EnemySkill_SO skill = ScriptableObject.CreateInstance<EnemySkill_SO>();
        skill.skillName = name;
        skill.diceCount = 1;
        skill.dicePowerRanges = new[]
        {
            new EnemySkill_SO.DicePowerRange
            {
                minPower = 1,
                maxPower = 2
            }
        };
        createdObjects.Add(skill);
        return skill;
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
