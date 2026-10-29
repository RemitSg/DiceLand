using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

public sealed class AgentAdventureBattleBridgeTests
{
    private AgentAdventureBattleBridge bridge;
    private readonly List<UnityEngine.Object> temporary = new List<UnityEngine.Object>();
    private MapRunSnapshot originalGlobal;
    private Random.State originalRandom;

    [SetUp] public void SetUp()
    {
        originalGlobal = (MapRunSnapshot)typeof(MapRunRuntime).GetField("currentSnapshot", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
        originalRandom = Random.state;
    }
    [TearDown] public void TearDown()
    {
        bridge?.Dispose(); bridge = null;
        foreach (UnityEngine.Object value in temporary) UnityEngine.Object.DestroyImmediate(value);
        temporary.Clear();
        MapRunRuntime.RestoreSnapshot(originalGlobal);
        Random.state = originalRandom;
    }
    private static T Find<T>(Func<T, bool> predicate) where T : UnityEngine.Object
    {
        return AssetDatabase.FindAssets("t:" + typeof(T).Name, new[] { "Assets/Data" })
            .Select(g => AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(g))).First(predicate);
    }
    private T Temporary<T>() where T : ScriptableObject { T value = ScriptableObject.CreateInstance<T>(); temporary.Add(value); return value; }
    private MapRunSnapshot Snapshot(MapTileType kind = MapTileType.Battle, int hp = 19)
    {
        CharacterData_SO character = Find<CharacterData_SO>(c => c.characterName == "Chara");
        Weapon_SO weapon = Find<Weapon_SO>(w => w.weaponName == "新手木剑");
        return new MapRunSnapshot
        {
            sceneName = "AgentAdventureBridgeTest", boardSize = 2,
            runState = new RunState { money = 17, requiredKeys = 3, partyMembers = new List<RunPartyMemberState>
            { new RunPartyMemberState { memberId = "persistent-member", characterData = character, weaponData = weapon, maxHP = character.baseHP, currentHP = hp, level = 1, experience = 17 } } },
            tiles = new List<BoardTileData> { new BoardTileData { coord = Vector2Int.one, tileType = kind, dangerBand = MapDangerBand.Outer, encounterSeed = 1234 } }
        };
    }
    private EnemyGroup_SO Group()
    {
        EnemyGroup_SO group = Temporary<EnemyGroup_SO>();
        group.enemies.Add(Find<EnemyData_SO>(e => e.enemyID == 1));
        return group;
    }
    private void Open(MapRunSnapshot snapshot, RelicTable_SO table = null)
    {
        bridge = new AgentAdventureBattleBridge(snapshot, snapshot.tiles[0], Group(), null, null, table, null, 30);
    }
    private static AgentBattleObservation Act(AgentBattleSession battle, string op, AgentBattleLegalAction action = null)
    {
        AgentBattleObservation state = battle.Observe();
        return battle.Act(new AgentBattleRequest { op = op, sessionId = state.sessionId, revision = state.revision, requestId = Guid.NewGuid().ToString("N"),
            actorId = action?.actorId, buttonIndex = action != null ? action.buttonIndex : 0, targetId = action?.targetId,
            targetButtonIndex = action != null ? action.targetButtonIndex : 0, choiceIndex = action != null ? action.choiceIndex : 0 }).state;
    }
    private static AgentBattleObservation Win(AgentBattleSession battle)
    {
        AgentBattleObservation state = battle.Observe();
        for (int i = 0; i < 30 && state.phase == "targeting"; i++)
        {
            AgentBattleLegalAction target = state.legalActions.FirstOrDefault(a => a.op == "target");
            if (target != null) Act(battle, "target", target);
            state = Act(battle, "commit");
        }
        Assert.AreEqual("victory", state.outcome);
        return state;
    }

    [Test] public void RealEncounter_PreservesHpExperienceAndIdentityAcrossTwoBattles()
    {
        MapRunSnapshot snapshot = Snapshot(); Open(snapshot);
        Assert.AreEqual(19, bridge.Battle.Observe().actors.Single(a => a.side == "ally").hp);
        Act(bridge.Battle, "commit"); // Accept an actual enemy hit before fighting back.
        AgentBattleObservation end = Win(bridge.Battle);
        Assert.Greater(end.rewards.experience, 0);
        int hp = end.actors.Single(a => a.side == "ally").hp;
        bridge.ApplyResult();
        Assert.AreEqual(hp, snapshot.runState.partyMembers[0].currentHP);
        Assert.Less(hp, 19);
        Assert.AreEqual("persistent-member", snapshot.runState.partyMembers[0].memberId);
        Assert.Greater(snapshot.runState.partyMembers[0].experience, 17);
        bridge.Dispose(); bridge = null;
        snapshot.tiles[0].tileType = MapTileType.Battle; snapshot.tiles[0].isResolved = false;
        Open(snapshot);
        Assert.AreEqual(hp, bridge.Battle.Observe().actors.Single(a => a.side == "ally").hp);
        Assert.AreEqual(snapshot.runState.partyMembers[0].experience, bridge.Battle.Encounter.partyMembers[0].experience);
    }

    [Test] public void ResultApplication_IsIdempotentAndDoesNotReplaceUiSnapshot()
    {
        MapRunRuntime.RestoreSnapshot(new MapRunSnapshot { sceneName = "UserSnapshot", runState = new RunState { money = 999 } });
        MapRunSnapshot snapshot = Snapshot(); Open(snapshot); Win(bridge.Battle);
        bridge.Battle.Encounter.moneyGained = 7; // Tests the writer's transfer independently of reward table RNG.
        bridge.ApplyResult();
        bridge.ApplyResult();
        Assert.AreEqual(24, snapshot.runState.money);
        Assert.IsTrue(snapshot.tiles[0].isResolved);
        Assert.AreEqual(MapTileType.Empty, snapshot.tiles[0].tileType);
        Assert.IsTrue(MapRunRuntime.TryGetSnapshot("UserSnapshot", out MapRunSnapshot untouched));
        Assert.AreEqual(999, untouched.runState.money);
    }

    [Test] public void ConfiguredLevelAndMoneyRewards_PersistIntoTheNextBattle()
    {
        MapRunSnapshot snapshot = Snapshot();
        LevelTable_SO levels = Find<LevelTable_SO>(t => t.GetExperienceToNextLevel(1) > 0);
        MoneyRewardTable_SO money = Find<MoneyRewardTable_SO>(t => t.RollReward(MapTileType.Battle, MapDangerBand.Outer, 1234) > 0);
        DifficultyConfig_SO difficulty = AssetDatabase.LoadAssetAtPath<DifficultyConfig_SO>("Assets/Data/Game/NormalConfig.asset");
        Assert.IsNotNull(difficulty);
        snapshot.runState.partyMembers[0].experience = levels.GetExperienceToNextLevel(1) - 1;
        bridge = new AgentAdventureBattleBridge(snapshot, snapshot.tiles[0], Group(), difficulty, money, null, levels, 30);
        Assert.Throws<InvalidOperationException>(() => bridge.Battle.Replay());
        AgentBattleObservation end = Win(bridge.Battle);
        bridge.ApplyResult();
        Assert.Greater(snapshot.runState.partyMembers[0].level, 1);
        Assert.AreEqual(17 + end.rewards.money, snapshot.runState.money);
        Assert.Greater(end.rewards.money, 0);
        int level = snapshot.runState.partyMembers[0].level;
        bridge.Dispose(); bridge = null;
        snapshot.tiles[0].tileType = MapTileType.Battle; snapshot.tiles[0].isResolved = false;
        bridge = new AgentAdventureBattleBridge(snapshot, snapshot.tiles[0], Group(), difficulty, money, null, levels, 30);
        Assert.AreEqual(level, bridge.Battle.Encounter.partyMembers[0].level);
        Assert.AreEqual(snapshot.runState.partyMembers[0].currentHP, bridge.Battle.Observe().actors.Single(a => a.side == "ally").hp);
    }

    [Test] public void EliteReward_MustBeChosenAndAcquisitionEffectIsAppliedOnceOnReturn()
    {
        MapRunSnapshot snapshot = Snapshot(MapTileType.Elite);
        RelicTable_SO table = Temporary<RelicTable_SO>();
        typeof(RelicTable_SO).GetField("relics", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(table,
            new List<Relic_SO> { Find<Relic_SO>(r => r.relicId == "82") });
        Open(snapshot, table);
        AgentBattleObservation end = Win(bridge.Battle);
        Assert.AreEqual(MapTileType.Elite, bridge.Battle.Encounter.tileType);
        Assert.AreEqual("reward", end.phase);
        Assert.Throws<InvalidOperationException>(() => bridge.ApplyResult());
        AgentBattleLegalAction choice = end.legalActions.Single(a => a.op == "choose_relic");
        int maxHp = snapshot.runState.partyMembers[0].maxHP;
        end = Act(bridge.Battle, "choose_relic", choice);
        Assert.AreEqual("terminal", end.phase);
        bridge.ApplyResult(); bridge.ApplyResult();
        Assert.AreEqual(1, snapshot.runState.GetRelicAmount(table.Relics[0]));
        Assert.AreEqual(maxHp + 3, snapshot.runState.partyMembers[0].maxHP);
    }

    [Test] public void BossWithoutKeys_IsRejectedBeforeStartingBattle()
    {
        MapRunSnapshot snapshot = Snapshot(MapTileType.Boss);
        Assert.Throws<ArgumentException>(() => Open(snapshot));
        Assert.IsNull(BattleEncounterRuntime.ActiveEncounter);
        Assert.AreEqual(19, snapshot.runState.partyMembers[0].currentHP);
    }

    [Test] public void UnlockedBoss_UsesBossContextAndClearsItsActualTile()
    {
        MapRunSnapshot snapshot = Snapshot(MapTileType.Boss);
        snapshot.runState.currentKeys = snapshot.runState.requiredKeys;
        Open(snapshot);
        Assert.AreEqual(MapTileType.Boss, bridge.Battle.Encounter.tileType);
        Win(bridge.Battle); bridge.ApplyResult();
        Assert.IsTrue(snapshot.tiles[0].isResolved);
        Assert.AreEqual(1, snapshot.runState.settlementStats.bossesDefeated);
    }

    [Test] public void CopiedTile_CannotSpoofTheAdventureBattleContext()
    {
        MapRunSnapshot snapshot = Snapshot(MapTileType.Boss);
        var fake = new BoardTileData { coord = snapshot.tiles[0].coord, tileType = MapTileType.Battle };
        Assert.Throws<ArgumentException>(() => bridge = new AgentAdventureBattleBridge(snapshot, fake, Group(), null, null, null, null));
        Assert.IsNull(BattleEncounterRuntime.ActiveEncounter);
    }

    [Test] public void RefreshReward_ExcludesCurrentChoicesConsumesOneRefreshAndIsIdempotent()
    {
        MapRunSnapshot snapshot = Snapshot(MapTileType.Elite);
        snapshot.runState.abilityRelicChoiceRefreshCount = 1;
        RelicTable_SO table = Temporary<RelicTable_SO>();
        typeof(RelicTable_SO).GetField("relics", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(table,
            new List<Relic_SO> { Find<Relic_SO>(r => r.relicId == "80"), Find<Relic_SO>(r => r.relicId == "82"),
                Find<Relic_SO>(r => r.relicId == "15"), Find<Relic_SO>(r => r.relicId == "92") });
        Open(snapshot, table);
        AgentBattleObservation end = Win(bridge.Battle);
        string[] old = end.rewards.choices.Select(c => c.id).ToArray();
        Assert.IsTrue(end.legalActions.Any(a => a.op == "refresh_rewards"));
        var command = new AgentBattleRequest { op = "refresh_rewards", sessionId = end.sessionId,
            revision = end.revision, requestId = "refresh-once" };
        AgentBattleResponse refreshed = bridge.Battle.Act(command);
        Assert.AreEqual(0, refreshed.state.rewards.refreshesRemaining);
        Assert.IsFalse(refreshed.state.rewards.choices.Any(c => old.Contains(c.id)));
        Assert.IsFalse(refreshed.state.legalActions.Any(a => a.op == "refresh_rewards"));
        Assert.AreEqual(JsonUtility.ToJson(refreshed), JsonUtility.ToJson(bridge.Battle.Act(command)));
        Assert.Throws<ArgumentException>(() => Act(bridge.Battle, "choose_relic", new AgentBattleLegalAction { choiceIndex = 999 }));
        Assert.AreEqual(refreshed.state.revision, bridge.Battle.Observe().revision);
        Act(bridge.Battle, "choose_relic", refreshed.state.legalActions.First(a => a.op == "choose_relic"));
        bridge.ApplyResult();
    }

    [Test] public void Defeat_ReturnsActualDeadPartyAndDoesNotClearTheTileOrGrantVictoryMoney()
    {
        MapRunSnapshot snapshot = Snapshot(hp: 1); Open(snapshot);
        AgentBattleObservation end = Act(bridge.Battle, "commit");
        Assert.AreEqual("defeat", end.outcome);
        bridge.ApplyResult();
        Assert.AreEqual(0, snapshot.runState.partyMembers[0].currentHP);
        Assert.AreEqual(17, snapshot.runState.money);
        Assert.IsFalse(snapshot.tiles[0].isResolved);
    }

    [Test] public void RewardSelection_RejectsForeignChoiceAndCannotApplyTwice()
    {
        Relic_SO relic = Find<Relic_SO>(r => r.relicId == "80");
        var valid = new BattleSettlementLootEntry { relicData = relic };
        var data = new BattleSettlementRelicChoiceData { choices = new List<BattleSettlementLootEntry> { valid }, selectedChoice = new BattleSettlementLootEntry { relicData = relic } };
        var encounter = new BattleEncounterData();
        Assert.IsFalse(BattleRelicChoiceSelectionService.TryApply(encounter, data, out _));
        data.selectedChoice = valid;
        Assert.IsTrue(BattleRelicChoiceSelectionService.TryApply(encounter, data, out _));
        Assert.IsTrue(BattleRelicChoiceSelectionService.TryApply(encounter, data, out _));
        Assert.AreEqual(1, encounter.resolvedRelics.Single().amount);
    }

    [Test] public void AcquiredPackage_UsesSeededRandomInsteadOfWallClock()
    {
        Relic_SO package = Find<Relic_SO>(r => r.relicId == "92");
        Random.InitState(7788); var first = new RunState(); first.AddRelic(package);
        Random.InitState(7788); var second = new RunState(); second.AddRelic(package);
        CollectionAssert.AreEqual(first.relicInventory.Select(r => r.relicData.relicId + ":" + r.amount).ToArray(), second.relicInventory.Select(r => r.relicData.relicId + ":" + r.amount).ToArray());
        Assert.Greater(first.relicInventory.Count, 1);
    }
}
