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
