using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class BattleRoundSummaryLogServiceTests
{
    private readonly List<GameObject> createdObjects = new List<GameObject>();
    private readonly List<ScriptableObject> createdAssets = new List<ScriptableObject>();

    [TearDown]
    public void TearDown()
    {
        for (int i = 0; i < createdObjects.Count; i++)
        {
            if (createdObjects[i] != null)
                Object.DestroyImmediate(createdObjects[i]);
        }

        for (int i = 0; i < createdAssets.Count; i++)
        {
            if (createdAssets[i] != null)
                Object.DestroyImmediate(createdAssets[i]);
        }

        createdObjects.Clear();
        createdAssets.Clear();
    }

    [Test]
    public void BuildSummaryLines_IncludesOnlyLivingAlliesAndEnemiesInOriginalOrder()
    {
        BattleAllyController livingAlly = CreateAlly("Living Ally", currentHp: 7, maxHp: 10);
        BattleAllyController deadAlly = CreateAlly("Dead Ally", currentHp: 0, maxHp: 10);
        BattleEnemyController livingEnemy = CreateEnemy("Living Enemy", enemyId: 2, hp: 5);
        BattleEnemyController deadEnemy = CreateEnemy("Dead Enemy", enemyId: 3, hp: 1);
        deadEnemy.TakeDamage(1);

        IReadOnlyList<string> lines = BattleRoundSummaryLogService.BuildSummaryLines(
            new[] { livingAlly, null, deadAlly },
            new[] { null, livingEnemy, deadEnemy });

        Assert.AreEqual(3, lines.Count);
        Assert.AreEqual("\u2500\u2500 \u56de\u5408\u7ed3\u7b97\u6c47\u603b \u2500\u2500", lines[0]);
        Assert.AreEqual("\u53cb\u65b9 Living Ally HP 7/10", lines[1]);
        Assert.AreEqual("\u654c\u65b9 Living Enemy(ID:2) HP 5/5", lines[2]);
    }

    private BattleAllyController CreateAlly(string name, int currentHp, int maxHp)
    {
        GameObject go = CreateGameObject(name);
        BattleAllyController ally = go.AddComponent<BattleAllyController>();
        ally.ApplyRuntimeState(new RunPartyMemberState
        {
            currentHP = currentHp,
            maxHP = maxHp,
            level = 1,
            experience = 0
        });
        return ally;
    }

    private BattleEnemyController CreateEnemy(string name, int enemyId, int hp)
    {
        EnemyData_SO enemyData = ScriptableObject.CreateInstance<EnemyData_SO>();
        createdAssets.Add(enemyData);
        enemyData.enemyID = enemyId;
        enemyData.enemyName = name;
        enemyData.baseHP = hp;

        GameObject go = CreateGameObject(name);
        BattleEnemyController enemy = go.AddComponent<BattleEnemyController>();
        enemy.ApplyEnemyData(enemyData);
        return enemy;
    }

    private GameObject CreateGameObject(string name)
    {
        GameObject go = new GameObject(name);
        createdObjects.Add(go);
        return go;
    }
}
