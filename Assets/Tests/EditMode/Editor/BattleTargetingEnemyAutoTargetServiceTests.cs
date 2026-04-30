using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class BattleTargetingEnemyAutoTargetServiceTests
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
    public void BuildLogEntries_ReportsOnlyConnectedEnemyButtons()
    {
        BattleEnemyController enemy = CreateEnemy("Scout", buttonCount: 2);
        BattleAllyController ally = CreateAlly("Guard", buttonCount: 1);
        Connect(enemy.SpeedButtons[1], ally.SpeedButtons[0]);

        List<string> logs = BattleTargetingEnemyAutoTargetService.BuildLogEntries(new[] { enemy });

        Assert.AreEqual(1, logs.Count);
        StringAssert.Contains("Scout", logs[0]);
        StringAssert.Contains("Guard", logs[0]);
        StringAssert.Contains("2", logs[0]);
    }

    [Test]
    public void BuildLogEntries_SkipsDeadAndNullEnemies()
    {
        BattleEnemyController enemy = CreateEnemy("Scout", buttonCount: 1);
        enemy.TakeDamage(99);

        List<string> logs = BattleTargetingEnemyAutoTargetService.BuildLogEntries(new BattleEnemyController[] { null, enemy });

        Assert.AreEqual(0, logs.Count);
    }

    private BattleAllyController CreateAlly(string name, int buttonCount)
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
