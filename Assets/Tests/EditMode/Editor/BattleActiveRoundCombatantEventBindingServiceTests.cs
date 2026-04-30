using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class BattleActiveRoundCombatantEventBindingServiceTests
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
    public void BindDeathHandlers_RebindingDoesNotDuplicateCallbacks()
    {
        BattleAllyController ally = CreateAlly("Ally");
        BattleEnemyController enemy = CreateEnemy("Enemy");
        int allyDeaths = 0;
        int enemyDeaths = 0;
        Action<BattleAllyController> allyHandler = _ => allyDeaths++;
        Action<BattleEnemyController> enemyHandler = _ => enemyDeaths++;

        BattleActiveRoundCombatantEventBindingService.BindDeathHandlers(
            new[] { ally },
            new[] { enemy },
            allyHandler,
            enemyHandler);
        BattleActiveRoundCombatantEventBindingService.BindDeathHandlers(
            new[] { ally },
            new[] { enemy },
            allyHandler,
            enemyHandler);

        InvokeEvent(ally, "Died", ally);
        InvokeEvent(enemy, "Died", enemy);

        Assert.AreEqual(1, allyDeaths);
        Assert.AreEqual(1, enemyDeaths);
    }

    [Test]
    public void UnbindDeathHandlers_RemovesCallbacks()
    {
        BattleAllyController ally = CreateAlly("Ally");
        BattleEnemyController enemy = CreateEnemy("Enemy");
        int allyDeaths = 0;
        int enemyDeaths = 0;
        Action<BattleAllyController> allyHandler = _ => allyDeaths++;
        Action<BattleEnemyController> enemyHandler = _ => enemyDeaths++;

        BattleActiveRoundCombatantEventBindingService.BindDeathHandlers(
            new[] { ally },
            new[] { enemy },
            allyHandler,
            enemyHandler);
        BattleActiveRoundCombatantEventBindingService.UnbindDeathHandlers(
            new[] { ally },
            new[] { enemy },
            allyHandler,
            enemyHandler);

        InvokeEvent(ally, "Died", ally);
        InvokeEvent(enemy, "Died", enemy);

        Assert.AreEqual(0, allyDeaths);
        Assert.AreEqual(0, enemyDeaths);
    }

    private BattleAllyController CreateAlly(string name)
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
