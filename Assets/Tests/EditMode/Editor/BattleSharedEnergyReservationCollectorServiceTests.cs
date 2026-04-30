using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class BattleSharedEnergyReservationCollectorServiceTests
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
    public void CalculateReservedEnergyForAlly_MultipliesConnectedButtonsBySelectedSkillCost()
    {
        BattleAllyController ally = CreateAlly("Ally", buttonCount: 2, energyCost: 2);
        BattleEnemyController enemy = CreateEnemy("Enemy", buttonCount: 2);
        Connect(ally.SpeedButtons[0], enemy.SpeedButtons[0]);
        Connect(ally.SpeedButtons[1], enemy.SpeedButtons[1]);

        int reserved = BattleSharedEnergyReservationCollectorService.CalculateReservedEnergyForAlly(ally);

        Assert.AreEqual(4, reserved);
    }

    [Test]
    public void CalculateTotalReservedEnergy_ClampsToCurrentSharedEnergy()
    {
        BattleAllyController ally = CreateAlly("Ally", buttonCount: 2, energyCost: 4);
        BattleEnemyController enemy = CreateEnemy("Enemy", buttonCount: 2);
        Connect(ally.SpeedButtons[0], enemy.SpeedButtons[0]);
        Connect(ally.SpeedButtons[1], enemy.SpeedButtons[1]);

        int total = BattleSharedEnergyReservationCollectorService.CalculateTotalReservedEnergy(
            new[] { ally },
            currentSharedEnergy: 5);

        Assert.AreEqual(5, total);
    }

    private BattleAllyController CreateAlly(string name, int buttonCount, int energyCost)
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
        ally.GrantTemporarySkill(CreateSkill($"{name}_Skill", energyCost), replaceWeaponSkills: true);
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
