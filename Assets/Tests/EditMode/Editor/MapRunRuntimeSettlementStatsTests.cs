using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class MapRunRuntimeSettlementStatsTests
{
    [Test]
    public void ApplyEncounterResult_IncrementsBattleBandCounterOnVictory()
    {
        MapRunRuntime.RestoreSnapshot(CreateSnapshot(new BoardTileData
        {
            coord = Vector2Int.one,
            tileType = MapTileType.Elite,
            dangerBand = MapDangerBand.Inner
        }));

        MapRunRuntime.ApplyEncounterResult(new BattleEncounterData
        {
            sourceCoord = Vector2Int.one,
            tileType = MapTileType.Elite,
            dangerBand = MapDangerBand.Inner,
            wasVictory = true
        });

        Assert.IsTrue(MapRunRuntime.TryGetSnapshot("Map", out MapRunSnapshot snapshot));
        Assert.AreEqual(1, snapshot.runState.settlementStats.eliteBattleInnerCleared);
    }

    [Test]
    public void ApplyEncounterResult_DoesNotIncrementBattleCountersOnDefeat()
    {
        MapRunRuntime.RestoreSnapshot(CreateSnapshot(new BoardTileData
        {
            coord = Vector2Int.one,
            tileType = MapTileType.Battle,
            dangerBand = MapDangerBand.Outer
        }));

        MapRunRuntime.ApplyEncounterResult(new BattleEncounterData
        {
            sourceCoord = Vector2Int.one,
            tileType = MapTileType.Battle,
            dangerBand = MapDangerBand.Outer,
            wasVictory = false
        });

        Assert.IsTrue(MapRunRuntime.TryGetSnapshot("Map", out MapRunSnapshot snapshot));
        Assert.AreEqual(0, snapshot.runState.settlementStats.normalBattleOuterCleared);
    }

    [Test]
    public void ApplyEncounterResult_IncrementsEventBattleAndBossCounters()
    {
        MapRunRuntime.RestoreSnapshot(CreateSnapshot(new BoardTileData
        {
            coord = Vector2Int.one,
            tileType = MapTileType.Boss,
            dangerBand = MapDangerBand.Core
        }));

        MapRunRuntime.ApplyEncounterResult(new BattleEncounterData
        {
            sourceCoord = Vector2Int.one,
            tileType = MapTileType.Boss,
            dangerBand = MapDangerBand.Core,
            isEventEncounter = true,
            wasVictory = true
        });

        Assert.IsTrue(MapRunRuntime.TryGetSnapshot("Map", out MapRunSnapshot snapshot));
        Assert.AreEqual(1, snapshot.runState.settlementStats.bossesDefeated);
        Assert.AreEqual(1, snapshot.runState.settlementStats.eventBattlesCompleted);
    }

    private static MapRunSnapshot CreateSnapshot(BoardTileData tile)
    {
        return new MapRunSnapshot
        {
            sceneName = "Map",
            runState = new RunState(),
            tiles = new List<BoardTileData> { tile }
        };
    }
}
