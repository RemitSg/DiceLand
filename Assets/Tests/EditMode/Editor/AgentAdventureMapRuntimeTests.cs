using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

public sealed class AgentAdventureMapRuntimeTests
{
    private AgentAdventureMapRuntime map;
    private AgentAdventureConfiguration configuration;
    private Random.State outside;

    [SetUp] public void SetUp() { outside = Random.state; configuration = AgentAdventureConfiguration.Load(); }
    [TearDown] public void TearDown() { map?.Dispose(); map = null; Random.state = outside; }
    private void Open(int seed = 1234) { map = new AgentAdventureMapRuntime(configuration, seed); }
    private static string BoardSignature(AgentAdventureMapRuntime value)
    {
        return string.Join("|", value.Snapshot.tiles.OrderBy(t => t.coord.x).ThenBy(t => t.coord.y)
            .Select(t => t.coord + ":" + t.tileType + ":" + t.encounterSeed + ":" + (t.assignedEnemyGroup != null ? t.assignedEnemyGroup.name : "")));
    }

    [Test] public void NormalConfiguration_ReadsSavedPartyAndGeneratesActualBoardWithoutViews()
    {
        Scene active = SceneManager.GetActiveScene(); int count = SceneManager.sceneCount;
        Open();
        Assert.AreSame(AssetDatabase.LoadAssetAtPath<DifficultyConfig_SO>("Assets/Data/Game/NormalConfig.asset"), configuration.starting.difficulty);
        Assert.AreEqual("Medium", map.Snapshot.runState.difficultyName);
        Assert.AreEqual(81, map.Snapshot.tiles.Count);
        Assert.AreEqual(3, map.Snapshot.tiles.Count(t => t.tileType == MapTileType.Key));
        Assert.AreEqual(1, map.Snapshot.tiles.Count(t => t.tileType == MapTileType.Boss));
        Assert.AreEqual(configuration.starting.party[0].memberId, map.Snapshot.runState.partyMembers[0].memberId);
        Assert.AreEqual(configuration.starting.party[0].startingMaxHP, map.Snapshot.runState.partyMembers[0].currentHP);
        foreach (BoardTileData tile in map.Snapshot.tiles.Where(t => t.IsHostileTile))
            Assert.AreSame(MapBattleEncounterService.SelectEnemyGroupForTile(configuration.enemies, tile), tile.assignedEnemyGroup);
        // The shipped roster has empty Inner/Core pools. Keep the content gap visible, never synthesize substitutes.
        Assert.AreEqual(map.Snapshot.tiles.Count(t => t.IsHostileTile && (t.assignedEnemyGroup == null || !t.assignedEnemyGroup.IsValid)), map.ConfigurationIssues.Length);
        Assert.Greater(map.ConfigurationIssues.Length, 0);
        Assert.IsEmpty(map.Board.GetComponentsInChildren<Canvas>(true));
        Assert.IsEmpty(map.Board.GetComponentsInChildren<SpriteRenderer>(true));
        Assert.IsEmpty(map.Board.GetComponentsInChildren<BoardTileView>(true));
        Assert.AreEqual(active.handle, SceneManager.GetActiveScene().handle);
        map.Dispose(); map = null;
        Assert.AreEqual(count, SceneManager.sceneCount);
    }

    [Test] public void SameSeed_GeneratesSameActualMapAndPreservesOutsideRandom()
    {
        string random = JsonUtility.ToJson(Random.state);
        Open(); string first = BoardSignature(map);
        Assert.AreEqual(random, JsonUtility.ToJson(Random.state));
        map.Dispose(); map = null;
        Open(); Assert.AreEqual(first, BoardSignature(map));
        Assert.IsTrue(map.TryMove(map.LegalDestinations().First().coord));
        Assert.AreEqual(random, JsonUtility.ToJson(Random.state));
    }

    [Test] public void InvalidMovement_IsRejectedBeforeSpendingOrChangingRandom()
    {
        Open(); string before = JsonUtility.ToJson(map.Snapshot); string random = JsonUtility.ToJson(Random.state);
        Assert.IsFalse(map.TryMove(map.Snapshot.runState.playerGridPosition));
        Assert.IsFalse(map.TryMove(new Vector2Int(-100, 40)));
        Assert.AreEqual(before, JsonUtility.ToJson(map.Snapshot));
        Assert.AreEqual(random, JsonUtility.ToJson(Random.state));
    }

    [Test] public void NewKeyWithOnePoint_UsesSharedOverflowCycleAndCollectsOnlyOnce()
    {
        Open(); Vector2Int spawn = map.Snapshot.runState.playerGridPosition;
        BoardTileData target = map.LegalDestinations().First();
        // Directed test of the real resolver and movement code; this is not a normal-run route.
        target.tileType = MapTileType.Key; target.isVisited = false; target.isResolved = false;
        map.Points.RestoreState(6, 0, 1);
        Assert.IsTrue(map.TryMove(target.coord));
        Assert.AreEqual(2, map.Snapshot.runState.currentCycle);
        Assert.AreEqual(1, map.Snapshot.runState.calamityTier);
        Assert.AreEqual(RunBootstrapService.ResolveActionPointLimitForTier(configuration.starting.difficulty, 1) - 1, map.Points.CurrentPoints);
        Assert.AreEqual(1, map.Snapshot.runState.currentKeys);
        Assert.AreEqual(1, map.Snapshot.runState.settlementStats.keysCollected);
        CollectionAssert.AreEqual(new[] { 1 }, map.PendingCalamities);
        Assert.IsTrue(map.TryMove(spawn)); Assert.IsTrue(map.TryMove(target.coord));
        Assert.AreEqual(1, map.Snapshot.runState.currentKeys);
        Assert.AreEqual(1, map.Snapshot.runState.settlementStats.keysCollected);
    }

    [Test] public void HostileArrival_QueuesCalamityForAfterBattleAndDoesNotAwardPlaceholderExperience()
    {
        Open(); BoardTileData target = map.LegalDestinations().First(t => t.IsHostileTile);
        map.Points.RestoreState(6, 0, 2);
        int experience = map.Snapshot.runState.partyMembers[0].experience;
        Assert.IsTrue(map.TryMove(target.coord));
        Assert.IsTrue(map.CycleFlags.deferPendingCalamityUntilMapResume);
        Assert.AreEqual(1, map.PendingCalamities.Count);
        Assert.IsFalse(target.isResolved);
        Assert.AreEqual(experience, map.Snapshot.runState.partyMembers[0].experience);
        Assert.IsNull(BattleEncounterRuntime.ActiveEncounter);
        using (var battle = new AgentAdventureBattleBridge(map.Snapshot, target, target.assignedEnemyGroup,
            configuration.starting.difficulty, configuration.money, configuration.eliteRelics, configuration.levels))
        {
            Assert.AreEqual(map.Snapshot.runState.partyMembers[0].currentHP, battle.Battle.Observe().actors.Single(a => a.side == "ally").hp);
            Assert.AreSame(target.assignedEnemyGroup, battle.Battle.Encounter.enemyGroup);
        }
    }

    [Test] public void SharedBootstrap_AppliesStartingBonusesEquipmentInventoryAndClock()
    {
        var bonus = new AbilityRunBonus { startingMoneyFlat = 7, battleStartMaxHealthFlat = 3,
            startingPartySizeFlat = 1, extraStartingCompanion = configuration.starting.recruitRoster.Characters.First(c => c != null && c != configuration.starting.party[0].characterData),
            startingWeaponReplacement = AssetDatabase.FindAssets("t:Weapon_SO").Select(g => AssetDatabase.LoadAssetAtPath<Weapon_SO>(AssetDatabase.GUIDToAssetPath(g)))
                .First(w => w != null && w != configuration.starting.party[0].equippedWeapon) };
        RunState state = RunBootstrapService.CreateNormalRun(configuration.starting, bonus, 123456, out var choices);
        Assert.AreEqual(2, state.partyMembers.Count);
        Assert.AreEqual(configuration.starting.startingMoney + 7, state.money);
        Assert.AreEqual(123456, state.settlementStats.runStartedUnixSeconds);
        Assert.AreEqual(configuration.starting.party[0].startingMaxHP + 3, state.partyMembers[0].maxHP);
        Assert.AreSame(bonus.startingWeaponReplacement, state.partyMembers[0].weaponData);
        Assert.IsTrue(state.weaponInventory.Any(w => w.weaponData == state.partyMembers[0].weaponData && w.amount > 0));
        Assert.IsEmpty(choices);
    }

    [Test] public void SecondMap_IsRejectedWithoutDestroyingTheFirst()
    {
        Open(); string before = BoardSignature(map);
        Assert.Throws<InvalidOperationException>(() => new AgentAdventureMapRuntime(configuration, 9));
        Assert.AreEqual(before, BoardSignature(map));
        Assert.AreEqual(81, map.Snapshot.tiles.Count);
    }
}
