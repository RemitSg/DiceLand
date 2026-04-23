using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class MapRunRuntimeTutorialTests
{
    private FieldInfo currentSnapshotField;

    [SetUp]
    public void SetUp()
    {
        currentSnapshotField = typeof(MapRunRuntime).GetField("currentSnapshot", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.IsNotNull(currentSnapshotField);
        currentSnapshotField.SetValue(null, null);
    }

    [TearDown]
    public void TearDown()
    {
        currentSnapshotField?.SetValue(null, null);
    }

    [Test]
    public void ApplyEncounterResult_TutorialVictoryRestoresAllPartyHpToFull()
    {
        MapRunSnapshot snapshot = new MapRunSnapshot
        {
            sceneName = "MapScene",
            runState = new RunState
            {
                partyMembers = new List<RunPartyMemberState>
                {
                    new RunPartyMemberState
                    {
                        memberId = "A",
                        maxHP = 10,
                        currentHP = 3
                    },
                    new RunPartyMemberState
                    {
                        memberId = "B",
                        maxHP = 14,
                        currentHP = 4
                    }
                }
            },
            tiles = new List<BoardTileData>
            {
                new BoardTileData
                {
                    coord = new Vector2Int(2, 7),
                    tileType = MapTileType.Battle
                }
            }
        };

        currentSnapshotField.SetValue(null, snapshot);

        BattleEncounterData encounter = new BattleEncounterData
        {
            wasVictory = true,
            isTutorialEncounter = true,
            sourceCoord = new Vector2Int(2, 7),
            resolvedPartyMembers = new List<RunPartyMemberState>
            {
                new RunPartyMemberState
                {
                    memberId = "A",
                    maxHP = 10,
                    currentHP = 1
                },
                new RunPartyMemberState
                {
                    memberId = "B",
                    maxHP = 14,
                    currentHP = 6
                }
            }
        };

        MapRunRuntime.ApplyEncounterResult(encounter);

        MapRunSnapshot updated = (MapRunSnapshot)currentSnapshotField.GetValue(null);
        Assert.IsNotNull(updated);
        Assert.IsNotNull(updated.runState);
        Assert.AreEqual(10, updated.runState.partyMembers[0].currentHP);
        Assert.AreEqual(14, updated.runState.partyMembers[1].currentHP);
    }
}
