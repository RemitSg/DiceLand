using NUnit.Framework;
using UnityEngine;

public class SpecialDungeonEventEffectTests
{
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
