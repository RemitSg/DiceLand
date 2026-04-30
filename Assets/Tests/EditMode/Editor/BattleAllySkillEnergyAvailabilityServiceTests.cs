using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class BattleAllySkillEnergyAvailabilityServiceTests
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
    public void HasEnoughEnergyForSkill_RequiresOneUseWhenAllyHasNoConnections()
    {
        BattleAllyController ally = CreateAlly("Ally", buttonCount: 1, selectedSkillCost: 0);
        EnemySkill_SO skill = CreateSkill("Costly", energyCost: 2);

        bool canUse = BattleAllySkillEnergyAvailabilityService.HasEnoughEnergyForSkill(
            ally,
            skill,
            currentSharedEnergy: 1,
            reservedSharedEnergy: 0);

        Assert.IsFalse(canUse);
    }

    [Test]
    public void HasEnoughEnergyForSkill_IgnoresCurrentAllyReservationWhenCheckingSkillSwitch()
    {
        BattleAllyController ally = CreateAlly("Ally", buttonCount: 1, selectedSkillCost: 2);
        BattleEnemyController enemy = CreateEnemy("Enemy", buttonCount: 1);
        Connect(ally.SpeedButtons[0], enemy.SpeedButtons[0]);

        bool canUse = BattleAllySkillEnergyAvailabilityService.HasEnoughEnergyForSkill(
            ally,
            CreateSkill("Replacement", energyCost: 3),
            currentSharedEnergy: 3,
            reservedSharedEnergy: 2);

        Assert.IsTrue(canUse);
    }

    [Test]
    public void HasEnoughEnergyForSkill_AccountsForOtherReservedEnergy()
    {
        BattleAllyController ally = CreateAlly("Ally", buttonCount: 1, selectedSkillCost: 0);
        EnemySkill_SO skill = CreateSkill("Replacement", energyCost: 2);

        bool canUse = BattleAllySkillEnergyAvailabilityService.HasEnoughEnergyForSkill(
            ally,
            skill,
            currentSharedEnergy: 3,
            reservedSharedEnergy: 2);

        Assert.IsFalse(canUse);
    }

    private BattleAllyController CreateAlly(string name, int buttonCount, int selectedSkillCost)
    {
        GameObject go = CreateGameObject(name);
        BattleAllyController ally = go.AddComponent<BattleAllyController>();
        ally.ApplyRuntimeState(new RunPartyMemberState
        {
            characterData = CreateCharacter(name),
            level = 1,
            maxHP = 10,
            currentHP = 10
        });
        ally.RegisterSpeedButtons(CreateSpeedButtons(go.transform, buttonCount, SpeedButton.ButtonType.Ally));
        ally.SetTemporaryExtraSpeedButtons(Mathf.Max(0, buttonCount - 1));
        ally.GrantTemporarySkill(CreateSkill($"{name}_Selected", selectedSkillCost), replaceWeaponSkills: true);
        ally.SelectDefaultSkill(1);
        return ally;
    }

    private BattleEnemyController CreateEnemy(string name, int buttonCount)
    {
        GameObject go = CreateGameObject(name);
        BattleEnemyController enemy = go.AddComponent<BattleEnemyController>();
        EnemyData_SO enemyData = ScriptableObject.CreateInstance<EnemyData_SO>();
        enemyData.enemyName = name;
        enemyData.baseHP = 10;
        enemyData.baseSpeedButtonCount = buttonCount;
        createdObjects.Add(enemyData);
        enemy.ApplyEnemyData(enemyData);
        enemy.RegisterSpeedButtons(CreateSpeedButtons(go.transform, buttonCount, SpeedButton.ButtonType.Enemy));
        return enemy;
    }

    private CharacterData_SO CreateCharacter(string name)
    {
        CharacterData_SO character = ScriptableObject.CreateInstance<CharacterData_SO>();
        character.characterName = name;
        character.baseHP = 10;
        createdObjects.Add(character);
        return character;
    }

    private EnemySkill_SO CreateSkill(string name, int energyCost)
    {
        EnemySkill_SO skill = ScriptableObject.CreateInstance<EnemySkill_SO>();
        skill.skillName = name;
        skill.energyCost = energyCost;
        createdObjects.Add(skill);
        return skill;
    }

    private List<SpeedButton> CreateSpeedButtons(Transform parent, int count, SpeedButton.ButtonType buttonType)
    {
        List<SpeedButton> buttons = new List<SpeedButton>();
        for (int i = 0; i < count; i++)
        {
            GameObject buttonObject = CreateGameObject($"SpeedButton_{i + 1}");
            buttonObject.transform.SetParent(parent, false);
            SpeedButton button = buttonObject.AddComponent<SpeedButton>();
            button.SetButtonType(buttonType);
            buttons.Add(button);
        }

        return buttons;
    }

    private GameObject CreateGameObject(string name)
    {
        GameObject go = new GameObject(name);
        createdObjects.Add(go);
        return go;
    }

    private static void Connect(SpeedButton source, SpeedButton target)
    {
        SetPrivateField(source, "isConnected", true);
        SetPrivateField(source, "connectedButton", target);
    }

    private static void SetPrivateField<TValue>(object target, string fieldName, TValue value)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field, $"Field {fieldName} was not found on {target.GetType().Name}.");
        field.SetValue(target, value);
    }
}
