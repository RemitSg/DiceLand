using NUnit.Framework;
using UnityEngine;

public class SpecialDungeonEventEffectTests
{
    [TestCase(true)]
    [TestCase(false)]
    public void EventEncounterCarriesTheDungeonContext(bool inDungeon)
    {
        var dungeon = ScriptableObject.CreateInstance<SpecialDungeon_SO>();
        var group = ScriptableObject.CreateInstance<EnemyGroup_SO>();
        var enemy = ScriptableObject.CreateInstance<EnemyData_SO>();
        try
        {
            group.enemies.Add(enemy);
            var effect = new MapEventBuiltInEffectData(); effect.encounterEnemyGroups.Add(group);
            var tile = new BoardTileData { tileType = MapTileType.Event, sourceSpecialDungeon = inDungeon ? dungeon : null };
            Assert.IsTrue(MapEventEncounterService.TryPrepareEncounter(new RunState(), tile, effect, "Map", "Battle", 99, out var encounter, out _));
            Assert.AreEqual(inDungeon, encounter.isSpecialDungeonEncounter);
        }
        finally { Object.DestroyImmediate(enemy); Object.DestroyImmediate(group); Object.DestroyImmediate(dungeon); }
    }

    [Test]
    public void TryExecute_StartSpecialDungeonReturnsPendingDungeon()
    {
        SpecialDungeon_SO dungeon = ScriptableObject.CreateInstance<SpecialDungeon_SO>();
        RunState runState = new RunState();
        MapEventOptionData option = new MapEventOptionData
        {
            builtInEffectType = MapEventOptionBuiltInEffectType.StartSpecialDungeon,
            specialDungeon = dungeon
        };

        bool success = MapEventBuiltInEffectExecutorService.TryExecute(
            new MapEventBuiltInEffectExecutionRequest
            {
                runState = runState,
                optionData = option,
                currentEventTile = new BoardTileData { coord = new Vector2Int(2, 2), tileType = MapTileType.Event }
            },
            out MapEventBuiltInEffectExecutionResult result);

        Assert.IsTrue(success);
        Assert.AreSame(dungeon, result.pendingSpecialDungeon);

        Object.DestroyImmediate(dungeon);
    }
}
