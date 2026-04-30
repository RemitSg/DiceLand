using NUnit.Framework;
using UnityEngine;

public class BattleCombatantContextServiceTests
{
    [Test]
    public void TryCollectCombatants_WhenParentMissing_ReturnsFalseAndMessage()
    {
        bool result = BattleCombatantContextService.TryCollectCombatants(
            null,
            new GameObject("Enemies"),
            out BattleAllyController[] allies,
            out BattleEnemyController[] enemies,
            out string message);

        Assert.IsFalse(result);
        Assert.IsNull(allies);
        Assert.IsNull(enemies);
        StringAssert.Contains("alliesParent", message);
    }

    [Test]
    public void TryCollectCombatants_CollectsInactiveExcludedChildren()
    {
        GameObject alliesParent = new GameObject("Allies");
        GameObject enemiesParent = new GameObject("Enemies");
        GameObject allyObject = new GameObject("Ally");
        GameObject enemyObject = new GameObject("Enemy");
        GameObject inactiveAllyObject = new GameObject("Inactive Ally");

        allyObject.transform.SetParent(alliesParent.transform, false);
        enemyObject.transform.SetParent(enemiesParent.transform, false);
        inactiveAllyObject.transform.SetParent(alliesParent.transform, false);
        inactiveAllyObject.SetActive(false);

        BattleAllyController ally = allyObject.AddComponent<BattleAllyController>();
        BattleEnemyController enemy = enemyObject.AddComponent<BattleEnemyController>();
        inactiveAllyObject.AddComponent<BattleAllyController>();

        bool result = BattleCombatantContextService.TryCollectCombatants(
            alliesParent,
            enemiesParent,
            out BattleAllyController[] allies,
            out BattleEnemyController[] enemies,
            out string message);

        Assert.IsTrue(result);
        Assert.AreEqual(1, allies.Length);
        Assert.AreSame(ally, allies[0]);
        Assert.AreEqual(1, enemies.Length);
        Assert.AreSame(enemy, enemies[0]);
        Assert.IsNull(message);

        Object.DestroyImmediate(alliesParent);
        Object.DestroyImmediate(enemiesParent);
    }
}
