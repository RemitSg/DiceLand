using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class BattleStatusDrivenStageTransitionDispatcherTests
{
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
    public void Dispatch_WhenAnyAllyMeetsStatusRule_TransitionsEnemyAndLogsOnce()
    {
        BattleStatus_SO bleed = CreateStatus("bleed", "Bleed");
        BattleAllyController ally = CreateAlly("Bleeding Ally", 12);
        BattleEnemyController broodmother = CreateBroodmother(bleed);
        ally.AddStatus(bleed, bleed.StatusId, bleed.DisplayName, 3);

        int logCount = 0;
        int previousStage = -1;
        int currentStage = -1;

        int transitionCount = BattleStatusDrivenStageTransitionDispatcher.Dispatch(
            new[] { broodmother },
            new[] { ally },
            new[] { broodmother },
            (enemy, previous, current) =>
            {
                logCount++;
                previousStage = previous;
                currentStage = current;
            });

        Assert.AreEqual(1, transitionCount);
        Assert.AreEqual(1, logCount);
        Assert.AreEqual(0, previousStage);
        Assert.AreEqual(1, currentStage);
        Assert.AreEqual(1, broodmother.CurrentStageIndex);
    }

    [Test]
    public void BattleButtonStatusChanged_WhenAnyAllyMeetsStatusRule_TransitionsEnemy()
    {
        BattleStatus_SO bleed = CreateStatus("bleed", "Bleed");
        BattleAllyController ally = CreateAlly("Bleeding Ally", 12);
        BattleEnemyController broodmother = CreateBroodmother(bleed);
        BattleButton battleButton = CreateBattleButton(new[] { ally }, new[] { broodmother });
        ally.AddStatus(bleed, bleed.StatusId, bleed.DisplayName, 3);

        InvokePrivateMethod(battleButton, "HandleAllyStatusChanged", ally);

        Assert.AreEqual(1, broodmother.CurrentStageIndex);
    }

    private BattleAllyController CreateAlly(string name, int maxHp)
    {
        GameObject go = new GameObject(name);
        createdObjects.Add(go);

        BattleAllyController ally = go.AddComponent<BattleAllyController>();
        RunPartyMemberState runtimeState = new RunPartyMemberState
        {
            characterData = CreateCharacter(name, maxHp),
            level = 1,
            experience = 0,
            maxHP = maxHp,
            currentHP = maxHp
        };
        ally.ApplyRuntimeState(runtimeState);
        return ally;
    }

    private BattleEnemyController CreateBroodmother(BattleStatus_SO bleed)
    {
        GameObject go = new GameObject("Broodmother");
        createdObjects.Add(go);

        EnemyData_SO enemyData = ScriptableObject.CreateInstance<EnemyData_SO>();
        enemyData.enemyName = "Broodmother";
        enemyData.baseHP = 28;
        enemyData.minSpeed = 2;
        enemyData.maxSpeed = 5;
        enemyData.baseSpeedButtonCount = 1;
        enemyData.stageSprites = new List<EnemyData_SO.EnemyStageEntry>
        {
            new EnemyData_SO.EnemyStageEntry { stageName = "Stage 1", speedButtonCountOverride = 1 },
            new EnemyData_SO.EnemyStageEntry { stageName = "Stage 2", speedButtonCountOverride = 2 }
        };
        enemyData.statusStageTransitionRules = new List<EnemyData_SO.StatusStageTransitionRule>
        {
            new EnemyData_SO.StatusStageTransitionRule
            {
                ruleName = "Wake on blood",
                statusData = bleed,
                targetMode = EnemyData_SO.StageStatusTargetMode.AnyUnit,
                comparisonType = EnemyData_SO.StageStatusComparisonType.GreaterOrEqual,
                stackThreshold = 3,
                targetStageIndex = 1,
                priority = 1
            }
        };
        createdObjects.Add(enemyData);

        EnemySkill_SO skill = ScriptableObject.CreateInstance<EnemySkill_SO>();
        skill.skillName = "Idle";
        skill.skillCategory = EnemySkill_SO.SkillCategory.Attack;
        skill.diceCount = 1;
        skill.dicePowerRanges = new[]
        {
            new EnemySkill_SO.DicePowerRange { minPower = 1, maxPower = 1 }
        };
        enemyData.skillPool = new List<EnemySkill_SO> { skill };
        createdObjects.Add(skill);

        BattleEnemyController enemy = go.AddComponent<BattleEnemyController>();
        enemy.ApplyEnemyData(enemyData);
        return enemy;
    }

    private BattleButton CreateBattleButton(BattleAllyController[] allies, BattleEnemyController[] enemies)
    {
        GameObject go = new GameObject("BattleButton");
        createdObjects.Add(go);
        BattleButton battleButton = go.AddComponent<BattleButton>();
        SetPrivateField(battleButton, "activeRoundAllies", allies);
        SetPrivateField(battleButton, "activeRoundEnemies", enemies);
        return battleButton;
    }

    private CharacterData_SO CreateCharacter(string name, int baseHp)
    {
        CharacterData_SO character = ScriptableObject.CreateInstance<CharacterData_SO>();
        character.characterName = name;
        character.baseHP = baseHp;
        character.minSpeed = 1;
        character.maxSpeed = 3;
        createdObjects.Add(character);
        return character;
    }

    private BattleStatus_SO CreateStatus(string statusId, string displayName)
    {
        BattleStatus_SO status = ScriptableObject.CreateInstance<BattleStatus_SO>();
        SetPrivateField(status, "statusId", statusId);
        SetPrivateField(status, "displayName", displayName);
        createdObjects.Add(status);
        return status;
    }

    private static void SetPrivateField<TValue>(object target, string fieldName, TValue value)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field, $"Field {fieldName} was not found on {target.GetType().Name}.");
        field.SetValue(target, value);
    }

    private static void InvokePrivateMethod(object target, string methodName, params object[] args)
    {
        MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(method, $"Method {methodName} was not found on {target.GetType().Name}.");
        method.Invoke(target, args);
    }
}
