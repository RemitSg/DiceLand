using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class RunServiceRefactorTests
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
    public void BuildTutorialBoardTiles_CreatesLinearTutorialRoute()
    {
        List<BoardTileData> tiles = RunBootstrapService.BuildTutorialBoardTiles(12);

        Assert.AreEqual(12, tiles.Count);
        Assert.IsTrue(tiles[0].isSpawnTile);
        Assert.AreEqual(MapTileType.Battle, tiles[2].tileType);
        Assert.AreEqual(MapTileType.Boss, tiles[11].tileType);
        Assert.AreEqual(TutorialBattleStageId.Battle07, RunBootstrapService.ResolveTutorialStageId(11));
    }

    [Test]
    public void GetMoveCost_UsesCurrentVisitedAndUnvisitedRules()
    {
        RunState runState = new RunState { playerGridPosition = Vector2Int.zero };
        BoardTileData currentTile = new BoardTileData { coord = Vector2Int.zero };
        BoardTileData visitedTile = new BoardTileData { coord = Vector2Int.right, isVisited = true };
        BoardTileData unvisitedTile = new BoardTileData { coord = Vector2Int.up, isVisited = false };

        Assert.AreEqual(0, MapMovementService.GetMoveCost(runState, currentTile));
        Assert.AreEqual(1, MapMovementService.GetMoveCost(runState, visitedTile));
        Assert.AreEqual(2, MapMovementService.GetMoveCost(runState, unvisitedTile));
    }

    [Test]
    public void ExecuteBuiltInEventEffects_AppliesMoneyAndActionPoints()
    {
        GameObject actionPointObject = new GameObject("ActionPointSystem");
        createdObjects.Add(actionPointObject);
        ActionPointSystem actionPointSystem = actionPointObject.AddComponent<ActionPointSystem>();
        actionPointSystem.Initialize(3);

        RunState runState = new RunState { money = 5 };
        MapEventOptionData optionData = new MapEventOptionData
        {
            optionId = "gain",
            optionLabel = "Gain",
            builtInEffectType = MapEventOptionBuiltInEffectType.GainMoney,
            effectAmount = 7,
            additionalBuiltInEffects = new List<MapEventBuiltInEffectData>
            {
                new MapEventBuiltInEffectData
                {
                    effectType = MapEventOptionBuiltInEffectType.LoseActionPoints,
                    effectAmount = 2
                }
            }
        };

        bool executed = MapEventBuiltInEffectExecutorService.TryExecute(
            new MapEventBuiltInEffectExecutionRequest
            {
                runState = runState,
                actionPointSystem = actionPointSystem,
                optionData = optionData
            },
            out MapEventBuiltInEffectExecutionResult result);

        Assert.IsTrue(executed);
        Assert.IsTrue(result.success);
        Assert.AreEqual(12, runState.money);
        Assert.AreEqual(1, actionPointSystem.CurrentPoints);
    }

    [Test]
    public void BuildMapBattleEncounter_ClonesRelicsCalamitiesAndLivingParty()
    {
        EnemyData_SO enemy = ScriptableObject.CreateInstance<EnemyData_SO>();
        EnemyGroup_SO group = ScriptableObject.CreateInstance<EnemyGroup_SO>();
        Relic_SO relic = ScriptableObject.CreateInstance<Relic_SO>();
        Calamity_SO calamity = ScriptableObject.CreateInstance<Calamity_SO>();
        createdObjects.Add(enemy);
        createdObjects.Add(group);
        createdObjects.Add(relic);
        createdObjects.Add(calamity);

        group.groupName = "Test Group";
        group.enemies = new List<EnemyData_SO> { enemy };
        RunState runState = new RunState
        {
            partyMembers = new List<RunPartyMemberState>
            {
                new RunPartyMemberState { memberId = "Alive", maxHP = 10, currentHP = 5 },
                new RunPartyMemberState { memberId = "Dead", maxHP = 10, currentHP = 0 }
            },
            relicInventory = new List<RunRelicState> { new RunRelicState { relicData = relic, amount = 1 } },
            activeCalamities = new List<RunCalamityState> { new RunCalamityState { calamityData = calamity, acquiredAtTier = 1 } }
        };

        BattleEncounterData encounter = MapBattleEncounterService.BuildEncounter(
            new MapBattleEncounterBuildRequest
            {
                runState = runState,
                tile = new BoardTileData { coord = new Vector2Int(2, 3), tileType = MapTileType.Battle, encounterSeed = 99 },
                encounterGroup = group,
                battleSceneName = "Battle"
            });

        Assert.IsNotNull(encounter);
        Assert.AreEqual(1, encounter.partyMembers.Count);
        Assert.AreEqual(1, encounter.relicInventory.Count);
        Assert.AreEqual(1, encounter.activeCalamities.Count);
        Assert.AreEqual(99, encounter.seed);
    }
}
