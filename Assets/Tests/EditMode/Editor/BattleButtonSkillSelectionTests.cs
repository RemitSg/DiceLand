using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class BattleButtonSkillSelectionTests
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
    public void GetAllySkillUnavailableReason_ReturnsEnergyInsufficientWhenSkillCannotBeAffordedEvenWithoutConnections()
    {
        BattleButton battleButton = CreateBattleButton();
        BattleAllyController ally = CreateAlly("Tester");
        EnemySkill_SO costlySkill = CreateSkill("Costly Skill", 2);

        SetPrivateField(battleButton, "currentSharedEnergy", 1);
        SetPrivateField(battleButton, "reservedSharedEnergy", 0);

        string unavailableReason = battleButton.GetAllySkillUnavailableReason(ally, costlySkill);

        Assert.IsFalse(string.IsNullOrEmpty(unavailableReason));
        Assert.IsFalse(battleButton.CanSelectSkillForAlly(ally, costlySkill));
    }

    [Test]
    public void GetAllySkillUnavailableReason_AllowsSelectionWhenAtLeastOneUseCanBeAfforded()
    {
        BattleButton battleButton = CreateBattleButton();
        BattleAllyController ally = CreateAlly("Tester");
        EnemySkill_SO skill = CreateSkill("Affordable Skill", 2);

        SetPrivateField(battleButton, "currentSharedEnergy", 2);
        SetPrivateField(battleButton, "reservedSharedEnergy", 0);

        string unavailableReason = battleButton.GetAllySkillUnavailableReason(ally, skill);

        Assert.IsTrue(string.IsNullOrEmpty(unavailableReason));
        Assert.IsTrue(battleButton.CanSelectSkillForAlly(ally, skill));
    }

    [Test]
    public void ResolveAllyContextualDiceRange_AppliesTargetStatusScalingRangeModifier()
    {
        BattleButton battleButton = CreateBattleButton();
        BattleStatus_SO current = CreateStatus("current", "电流");
        EnemySkill_SO skill = CreateSkill("Range Modifier Skill", 0);
        skill.dicePowerRanges = new[]
        {
            new EnemySkill_SO.DicePowerRange
            {
                minPower = 1,
                maxPower = 1
            }
        };
        skill.diceEffectSettings = new[]
        {
            new EnemySkill_SO.DiceEffectSettings
            {
                rangeModifiers = new[]
                {
                    new EnemySkill_SO.ConditionalDiceRangeModifierEntry
                    {
                        useCondition = true,
                        condition = EnemySkill_SO.PowerModifierCondition.TargetStatusStacksPerThreshold,
                        targetStatusData = current,
                        requiredStatusStacks = 1,
                        minPowerBonus = 1,
                        minPowerBonusCap = 3
                    }
                }
            }
        };
        BattleAllyController ally = CreateAlly("Tester");
        BattleEnemyController enemy = CreateEnemy("Target", skill);
        ally.GrantTemporarySkill(skill, true);
        Assert.IsTrue(ally.SetSelectedSkillIndex(0));
        enemy.AddStatus(current, current.StatusId, current.DisplayName, 3);

        EnemySkill_SO.DicePowerRange range = InvokeResolveAllyContextualDiceRange(
            battleButton,
            ally,
            enemy,
            0,
            new EnemySkill_SO.DicePowerRange { minPower = 1, maxPower = 1 });

        Assert.AreEqual(4, range.minPower);
        Assert.AreEqual(4, range.maxPower);
    }

    [Test]
    public void AllySpeedButtons_CanKeepIndependentSelectedSkills()
    {
        BattleAllyController ally = CreateAlly("Multi Button Ally");
        SpeedButton firstButton = CreateSpeedButton("First Button", ally.transform);
        SpeedButton secondButton = CreateSpeedButton("Second Button", ally.transform);
        EnemySkill_SO firstSkill = CreateSkill("First Skill", 0);
        EnemySkill_SO secondSkill = CreateSkill("Second Skill", 0);

        ally.RegisterSpeedButtons(new[] { firstButton, secondButton });
        ally.GrantTemporarySkill(firstSkill, true);
        ally.GrantTemporarySkill(secondSkill, true);
        ally.SelectDefaultSkill();

        Assert.IsTrue(ally.SetSelectedSkillIndexForButton(firstButton, 0));
        Assert.IsTrue(ally.SetSelectedSkillIndexForButton(secondButton, 1));

        Assert.AreSame(firstSkill, ally.GetSelectedSkillForButton(firstButton));
        Assert.AreSame(secondSkill, ally.GetSelectedSkillForButton(secondButton));
        Assert.AreEqual(0, ally.GetSelectedSkillIndexForButton(firstButton));
        Assert.AreEqual(1, ally.GetSelectedSkillIndexForButton(secondButton));

        ally.ActivateSelectedSkillForButton(secondButton);
        Assert.AreSame(secondSkill, ally.SelectedSkill);
        ally.ActivateSelectedSkillForButton(firstButton);
        Assert.AreSame(firstSkill, ally.SelectedSkill);
    }

    private BattleButton CreateBattleButton()
    {
        GameObject go = new GameObject("BattleButton");
        createdObjects.Add(go);
        return go.AddComponent<BattleButton>();
    }

    private BattleAllyController CreateAlly(string name)
    {
        GameObject go = new GameObject(name);
        createdObjects.Add(go);

        BattleAllyController ally = go.AddComponent<BattleAllyController>();
        CharacterData_SO character = ScriptableObject.CreateInstance<CharacterData_SO>();
        character.characterName = name;
        character.baseHP = 10;
        character.minSpeed = 3;
        character.maxSpeed = 3;
        createdObjects.Add(character);

        ally.ApplyRuntimeState(new RunPartyMemberState
        {
            characterData = character,
            level = 1,
            maxHP = 10,
            currentHP = 10
        });
        ally.RegisterSpeedButtons(new SpeedButton[0]);
        return ally;
    }

    private EnemySkill_SO CreateSkill(string name, int energyCost)
    {
        EnemySkill_SO skill = ScriptableObject.CreateInstance<EnemySkill_SO>();
        skill.skillName = name;
        skill.energyCost = energyCost;
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

    private SpeedButton CreateSpeedButton(string name, Transform parent)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        createdObjects.Add(go);
        return go.AddComponent<SpeedButton>();
    }

    private BattleEnemyController CreateEnemy(string name, EnemySkill_SO skill)
    {
        GameObject go = new GameObject(name);
        createdObjects.Add(go);

        BattleEnemyController enemy = go.AddComponent<BattleEnemyController>();
        EnemyData_SO enemyData = ScriptableObject.CreateInstance<EnemyData_SO>();
        enemyData.enemyName = name;
        enemyData.baseHP = 10;
        enemyData.skillPool = new List<EnemySkill_SO> { skill };
        createdObjects.Add(enemyData);

        enemy.ApplyEnemyData(enemyData);
        enemy.SelectRandomSkill(1);
        return enemy;
    }

    private BattleStatus_SO CreateStatus(string statusId, string displayName)
    {
        BattleStatus_SO status = ScriptableObject.CreateInstance<BattleStatus_SO>();
        SetPrivateField(status, "statusId", statusId);
        SetPrivateField(status, "displayName", displayName);
        createdObjects.Add(status);
        return status;
    }

    private static EnemySkill_SO.DicePowerRange InvokeResolveAllyContextualDiceRange(
        BattleButton battleButton,
        BattleAllyController ally,
        BattleEnemyController target,
        int diceIndex,
        EnemySkill_SO.DicePowerRange range)
    {
        MethodInfo method = typeof(BattleButton).GetMethod(
            "ApplyAllyContextualDiceRangeModifiers",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(method);
        return (EnemySkill_SO.DicePowerRange)method.Invoke(battleButton, new object[] { ally, target, diceIndex, range });
    }

    private static void SetPrivateField<TValue>(object target, string fieldName, TValue value)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field, $"Field {fieldName} was not found on {target.GetType().Name}.");
        field.SetValue(target, value);
    }
}
