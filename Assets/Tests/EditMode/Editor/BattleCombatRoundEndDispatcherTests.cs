using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class BattleCombatRoundEndDispatcherTests
{
    private readonly List<GameObject> createdObjects = new List<GameObject>();

    [TearDown]
    public void TearDown()
    {
        for (int i = 0; i < createdObjects.Count; i++)
        {
            if (createdObjects[i] != null)
                Object.DestroyImmediate(createdObjects[i]);
        }

        createdObjects.Clear();
    }

    [Test]
    public void Dispatch_NormalRoundEnd_FinalizesLivingArrayEntriesBeforeStatusAndRelics()
    {
        BattleAllyController firstAlly = CreateAlly("First Ally");
        BattleAllyController secondAlly = CreateAlly("Second Ally");
        BattleEnemyController enemy = CreateEnemy("Enemy");
        List<string> calls = new List<string>();

        BattleCombatRoundEndDispatcher.Dispatch(
            new[] { firstAlly, null, secondAlly },
            new[] { enemy },
            ally => calls.Add($"finalize:{ally.gameObject.name}"),
            () => calls.Add("summary"),
            (allies, enemies) => calls.Add($"status:{allies.Length}:{enemies.Length}"),
            (allies, enemies) => calls.Add($"relic:{allies.Length}:{enemies.Length}"));

        CollectionAssert.AreEqual(
            new[]
            {
                "finalize:First Ally",
                "finalize:Second Ally",
                "summary",
                "status:3:1",
                "relic:3:1"
            },
            calls);
    }

    [Test]
    public void Dispatch_AllowsMissingCallbacksAndNullArrays()
    {
        Assert.DoesNotThrow(() => BattleCombatRoundEndDispatcher.Dispatch(
            null,
            null,
            null,
            null,
            null,
            null));
    }

    [Test]
    public void DispatchRoundEndStatusEffects_SkipsNullCombatants()
    {
        BattleAllyController livingAlly = CreateAlly("Living Ally");
        BattleAllyController deadAlly = CreateAlly("Dead Ally");
        BattleEnemyController livingEnemy = CreateEnemy("Living Enemy");
        BattleEnemyController deadEnemy = CreateEnemy("Dead Enemy");
        List<string> calls = new List<string>();

        BattleCombatRoundEndDispatcher.DispatchRoundEndStatusEffects(
            new[] { livingAlly, null, deadAlly },
            new[] { livingEnemy, null, deadEnemy },
            ally => calls.Add($"ally:{ally.gameObject.name}"),
            enemy => calls.Add($"enemy:{enemy.gameObject.name}"));

        CollectionAssert.AreEqual(
            new[]
            {
                "ally:Living Ally",
                "ally:Dead Ally",
                "enemy:Living Enemy",
                "enemy:Dead Enemy"
            },
            calls);
    }

    private BattleAllyController CreateAlly(string name)
    {
        GameObject go = CreateGameObject(name);
        return go.AddComponent<BattleAllyController>();
    }

    private BattleEnemyController CreateEnemy(string name)
    {
        GameObject go = CreateGameObject(name);
        return go.AddComponent<BattleEnemyController>();
    }

    private GameObject CreateGameObject(string name)
    {
        GameObject go = new GameObject(name);
        createdObjects.Add(go);
        return go;
    }
}
