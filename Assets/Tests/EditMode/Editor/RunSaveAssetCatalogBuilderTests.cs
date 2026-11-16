using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public class RunSaveAssetCatalogBuilderTests
{
    private readonly List<Object> createdObjects = new List<Object>();

    [TearDown]
    public void TearDown()
    {
        for (int i = createdObjects.Count - 1; i >= 0; i--)
        {
            if (createdObjects[i] != null)
                Object.DestroyImmediate(createdObjects[i]);
        }

        createdObjects.Clear();
    }

    [Test]
    public void Build_AllowsCyclicEnemyDeathWatchReferences()
    {
        EnemyData_SO enemyA = CreateEnemy("enemy-a");
        EnemyData_SO enemyB = CreateEnemy("enemy-b");
        enemyA.onSpecificEnemyDeathStatusGains = new List<EnemyData_SO.OnSpecificEnemyDeathStatusGainEntry>
        {
            new EnemyData_SO.OnSpecificEnemyDeathStatusGainEntry { watchedEnemyData = enemyB, stackAmount = 1 }
        };
        enemyB.onSpecificEnemyDeathStatusGains = new List<EnemyData_SO.OnSpecificEnemyDeathStatusGainEntry>
        {
            new EnemyData_SO.OnSpecificEnemyDeathStatusGainEntry { watchedEnemyData = enemyA, stackAmount = 1 }
        };

        EnemyGroup_SO group = ScriptableObject.CreateInstance<EnemyGroup_SO>();
        group.groupName = "cyclic-watch-group";
        group.enemies.Add(enemyA);
        createdObjects.Add(group);

        MainMenuSaveAssetCatalog catalog = RunSaveAssetCatalogBuilder.Build(
            new RunSaveAssetCatalogBuildRequest
            {
                boardTiles = new List<BoardTileData>
                {
                    new BoardTileData
                    {
                        tileType = MapTileType.Battle,
                        assignedEnemyGroup = group
                    }
                }
            });

        Assert.AreSame(group, catalog.ResolveEnemyGroup("cyclic-watch-group"));
    }

    [Test]
    public void Build_ResolvesOncePerStockRelicFromMerchantCatalog()
    {
        MerchantCatalog_SO merchantCatalog = AssetDatabase.LoadAssetAtPath<MerchantCatalog_SO>(MerchantCatalogLiveData.CatalogPath);
        Relic_SO trashBag = AssetDatabase.LoadAssetAtPath<Relic_SO>("Assets/Data/Relics/108 垃圾袋/垃圾袋.asset");
        Assert.IsNotNull(trashBag);
        MainMenuSaveAssetCatalog catalog = RunSaveAssetCatalogBuilder.Build(
            new RunSaveAssetCatalogBuildRequest { merchantCatalog = merchantCatalog });

        Assert.AreSame(trashBag, catalog.ResolveRelic(trashBag.relicId));
    }

    private EnemyData_SO CreateEnemy(string name)
    {
        EnemyData_SO enemyData = ScriptableObject.CreateInstance<EnemyData_SO>();
        enemyData.enemyName = name;
        createdObjects.Add(enemyData);
        return enemyData;
    }
}
