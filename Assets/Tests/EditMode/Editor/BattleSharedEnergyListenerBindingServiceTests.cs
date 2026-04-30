using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class BattleSharedEnergyListenerBindingServiceTests
{
    private readonly List<UnityEngine.Object> createdObjects = new List<UnityEngine.Object>();

    [TearDown]
    public void TearDown()
    {
        for (int i = createdObjects.Count - 1; i >= 0; i--)
        {
            if (createdObjects[i] != null)
                UnityEngine.Object.DestroyImmediate(createdObjects[i]);
        }

        createdObjects.Clear();
    }

    [Test]
    public void Bind_RebindingDoesNotDuplicateCallbacks()
    {
        BattleAllyController ally = CreateAlly("Ally", buttonCount: 2);
        BattleEnemyController enemy = CreateEnemy("Enemy");
        int selectedSkillChanges = 0;
        int allyStatusChanges = 0;
        int connectionChanges = 0;
        int enemyStatusChanges = 0;

        Action<BattleAllyController> selectedSkillHandler = _ => selectedSkillChanges++;
        Action<BattleAllyController> allyStatusHandler = _ => allyStatusChanges++;
        Action<SpeedButton> connectionHandler = _ => connectionChanges++;
        Action<BattleEnemyController> enemyStatusHandler = _ => enemyStatusChanges++;

        BattleSharedEnergyListenerBindingService.Bind(
            new[] { ally },
            new[] { enemy },
            selectedSkillHandler,
            allyStatusHandler,
            connectionHandler,
            enemyStatusHandler);
        BattleSharedEnergyListenerBindingService.Bind(
            new[] { ally },
            new[] { enemy },
            selectedSkillHandler,
            allyStatusHandler,
            connectionHandler,
            enemyStatusHandler);

        InvokeEvent(ally, "SelectedSkillChanged", ally);
        InvokeEvent(ally, "StatusChanged", ally);
        InvokeEvent(ally.SpeedButtons[0], "ConnectionChanged", ally.SpeedButtons[0]);
        InvokeEvent(ally.SpeedButtons[1], "ConnectionChanged", ally.SpeedButtons[1]);
        InvokeEvent(enemy, "StatusChanged", enemy);

        Assert.AreEqual(1, selectedSkillChanges);
        Assert.AreEqual(1, allyStatusChanges);
        Assert.AreEqual(2, connectionChanges);
        Assert.AreEqual(1, enemyStatusChanges);
    }

    [Test]
    public void Unbind_RemovesCallbacks()
    {
        BattleAllyController ally = CreateAlly("Ally", buttonCount: 1);
        BattleEnemyController enemy = CreateEnemy("Enemy");
        int callbackCount = 0;
        Action<BattleAllyController> selectedSkillHandler = _ => callbackCount++;
        Action<BattleAllyController> allyStatusHandler = _ => callbackCount++;
        Action<SpeedButton> connectionHandler = _ => callbackCount++;
        Action<BattleEnemyController> enemyStatusHandler = _ => callbackCount++;

        BattleSharedEnergyListenerBindingService.Bind(
            new[] { ally },
            new[] { enemy },
            selectedSkillHandler,
            allyStatusHandler,
            connectionHandler,
            enemyStatusHandler);
        BattleSharedEnergyListenerBindingService.Unbind(
            new[] { ally },
            new[] { enemy },
            selectedSkillHandler,
            allyStatusHandler,
            connectionHandler,
            enemyStatusHandler);

        InvokeEvent(ally, "SelectedSkillChanged", ally);
        InvokeEvent(ally, "StatusChanged", ally);
        InvokeEvent(ally.SpeedButtons[0], "ConnectionChanged", ally.SpeedButtons[0]);
        InvokeEvent(enemy, "StatusChanged", enemy);

        Assert.AreEqual(0, callbackCount);
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
        ally.RegisterSpeedButtons(CreateSpeedButtons(go.transform, buttonCount));
        ally.SetTemporaryExtraSpeedButtons(Mathf.Max(0, buttonCount - 1));
        return ally;
    }

    private BattleEnemyController CreateEnemy(string name)
    {
        GameObject go = CreateGameObject(name);
        BattleEnemyController enemy = go.AddComponent<BattleEnemyController>();
        EnemyData_SO enemyData = ScriptableObject.CreateInstance<EnemyData_SO>();
        enemyData.enemyName = name;
        enemyData.baseHP = 10;
        createdObjects.Add(enemyData);
        enemy.ApplyEnemyData(enemyData);
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

    private List<SpeedButton> CreateSpeedButtons(Transform parent, int count)
    {
        List<SpeedButton> buttons = new List<SpeedButton>();
        for (int i = 0; i < count; i++)
        {
            GameObject buttonObject = CreateGameObject($"SpeedButton_{i + 1}");
            buttonObject.transform.SetParent(parent, false);
            SpeedButton button = buttonObject.AddComponent<SpeedButton>();
            button.SetButtonType(SpeedButton.ButtonType.Ally);
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

    private static void InvokeEvent(object target, string eventName, params object[] args)
    {
        FieldInfo field = target.GetType().GetField(eventName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field, $"Event backing field {eventName} was not found.");
        Delegate callback = field.GetValue(target) as Delegate;
        callback?.DynamicInvoke(args);
    }
}
