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
        List<BoardTileData> tiles = RunBootstrapService.BuildTutorialBoardTiles(6);

        Assert.AreEqual(6, tiles.Count);
        Assert.IsTrue(tiles[0].isSpawnTile);
        Assert.AreEqual(MapTileType.Battle, tiles[1].tileType);
        Assert.AreEqual(MapTileType.Battle, tiles[2].tileType);
        Assert.AreEqual(MapTileType.Merchant, tiles[3].tileType);
        Assert.AreEqual(MapTileType.Forge, tiles[4].tileType);
        Assert.AreEqual(MapTileType.Elite, tiles[5].tileType);
        Assert.AreEqual(TutorialBattleStageId.Battle03, RunBootstrapService.ResolveTutorialStageId(5));
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
    public void ExecuteGainActionPoints_MessageUsesActualRestoredAmount()
    {
        GameObject actionPointObject = new GameObject("ActionPointSystem");
        createdObjects.Add(actionPointObject);
        ActionPointSystem actionPointSystem = actionPointObject.AddComponent<ActionPointSystem>();
        actionPointSystem.Initialize(3);
        actionPointSystem.TrySpend(1);

        RunState runState = new RunState();
        MapEventOptionData optionData = new MapEventOptionData
        {
            optionId = "restore",
            optionLabel = "Restore",
            builtInEffectType = MapEventOptionBuiltInEffectType.GainActionPoints,
            effectAmount = 999
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
        Assert.AreEqual(3, actionPointSystem.CurrentPoints);
        Assert.That(result.message, Does.Contain("1"));
        Assert.That(result.message, Does.Not.Contain("999"));
    }

    [Test]
    public void BuildMapBattleEncounter_ClonesRelicsCalamitiesAndLivingParty()
    {
        EnemyData_SO enemy = ScriptableObject.CreateInstance<EnemyData_SO>();
        EnemyGroup_SO group = ScriptableObject.CreateInstance<EnemyGroup_SO>();
        DifficultyConfig_SO difficulty = ScriptableObject.CreateInstance<DifficultyConfig_SO>();
        Relic_SO relic = ScriptableObject.CreateInstance<Relic_SO>();
        Calamity_SO calamity = ScriptableObject.CreateInstance<Calamity_SO>();
        createdObjects.Add(enemy);
        createdObjects.Add(group);
        createdObjects.Add(difficulty);
        createdObjects.Add(relic);
        createdObjects.Add(calamity);

        group.groupName = "Test Group";
        group.enemies = new List<EnemyData_SO> { enemy };
        difficulty.battleExperienceRewards = new List<DifficultyConfig_SO.BattleExperienceRewardEntry>
        {
            new DifficultyConfig_SO.BattleExperienceRewardEntry
            {
                dangerBand = MapDangerBand.Mid,
                tileType = MapTileType.Battle,
                experience = 37
            }
        };
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
                tile = new BoardTileData { coord = new Vector2Int(2, 3), tileType = MapTileType.Battle, dangerBand = MapDangerBand.Mid, encounterSeed = 99 },
                encounterGroup = group,
                difficultyConfig = difficulty,
                battleSceneName = "Battle"
            });

        Assert.IsNotNull(encounter);
        Assert.AreEqual(1, encounter.partyMembers.Count);
        Assert.AreEqual(1, encounter.relicInventory.Count);
        Assert.AreEqual(1, encounter.activeCalamities.Count);
        Assert.AreEqual(37, encounter.configuredExperienceReward);
        Assert.AreEqual(99, encounter.seed);
    }

    [Test]
    public void BuildMapBattleEncounter_TutorialBattle02AddsTemporaryMollyAlly()
    {
        EnemyData_SO enemy = ScriptableObject.CreateInstance<EnemyData_SO>();
        EnemyGroup_SO group = ScriptableObject.CreateInstance<EnemyGroup_SO>();
        createdObjects.Add(enemy);
        createdObjects.Add(group);

        group.groupName = "Tutorial Group";
        group.enemies = new List<EnemyData_SO> { enemy };

        CharacterData_SO chara = ScriptableObject.CreateInstance<CharacterData_SO>();
        chara.characterName = "Chara";
        chara.baseHP = 24;
        Weapon_SO charaWeapon = ScriptableObject.CreateInstance<Weapon_SO>();
        charaWeapon.weaponName = "Chara Weapon";
        createdObjects.Add(chara);
        createdObjects.Add(charaWeapon);

        RunState runState = new RunState
        {
            partyMembers = new List<RunPartyMemberState>
            {
                new RunPartyMemberState
                {
                    memberId = "Chara",
                    characterData = chara,
                    weaponData = charaWeapon,
                    maxHP = 24,
                    currentHP = 24
                }
            }
        };

        BattleEncounterData encounter = MapBattleEncounterService.BuildEncounter(
            new MapBattleEncounterBuildRequest
            {
                runState = runState,
                tile = new BoardTileData { coord = new Vector2Int(2, 3), tileType = MapTileType.Battle, encounterSeed = 26002 },
                encounterGroup = group,
                battleSceneName = "Battle",
                isTutorialEncounter = true,
                tutorialBattleStageId = TutorialBattleStageId.Battle02
            });

        Assert.IsNotNull(encounter);
        Assert.AreEqual(2, encounter.partyMembers.Count);
        Assert.AreEqual("Molly", encounter.partyMembers[0].DisplayName);
        Assert.AreEqual("Chara", encounter.partyMembers[1].DisplayName);
        Assert.AreEqual(1, encounter.partyMembers[0].currentHP);
        Assert.IsTrue(encounter.partyMembers[0].isTemporaryBattleGuest);
        Assert.AreEqual(1, encounter.partyMembers[0].weaponData.speedButtonCount);
        Assert.AreEqual("轻击", encounter.partyMembers[0].weaponData.skillPool[0].skillName);
    }
}
