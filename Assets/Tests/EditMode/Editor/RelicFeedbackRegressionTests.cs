using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public class RelicFeedbackRegressionTests
{
    private readonly List<Object> createdObjects = new List<Object>();
    private Random.State originalRandomState;

    [SetUp]
    public void SetUp()
    {
        originalRandomState = Random.state;
    }

    [TearDown]
    public void TearDown()
    {
        BattleEncounterRuntime.Clear();
        Random.state = originalRandomState;
        for (int i = createdObjects.Count - 1; i >= 0; i--)
            if (createdObjects[i] != null)
                Object.DestroyImmediate(createdObjects[i]);
        createdObjects.Clear();
    }

    [Test]
    public void Phoenix_RepeatedLethalHitsConsumeBurnUntilThresholdIsNoLongerMet()
    {
        Relic_SO relic = LoadRelic("059 凰/凰.asset");
        BattleStatus_SO burn = relic.builtInEffects[0].conditionStatusData;
        Assert.NotNull(burn);
        BattleAllyController ally = CreateAlly("Leader", 10);
        BattleEnemyController enemy = CreateEnemy();
        enemy.AddStatus(burn, burn.StatusId, burn.DisplayName, 25);
        EnterBattle(relic);

        ally.TakeDamage(10);
        Assert.AreEqual(1, ally.CurrentHP);
        Assert.AreEqual(15, enemy.GetStatusStacks(burn));
        ally.TakeDamage(4);
        Assert.AreEqual(1, ally.CurrentHP, "A second lethal hit must still be prevented.");
        Assert.AreEqual(11, enemy.GetStatusStacks(burn));
        ally.TakeDamage(6);
        Assert.AreEqual(1, ally.CurrentHP);
        Assert.AreEqual(5, enemy.GetStatusStacks(burn));
        ally.TakeDamage(1);
        Assert.IsTrue(ally.IsDead, "Five burn stacks do not satisfy the strictly-above-five condition.");
        Assert.AreEqual(5, enemy.GetStatusStacks(burn));
    }

    [Test]
    public void CounterattackBlade_LimitsAllDefenseWinsPerActorAndResetsNextRoundAndBattle()
    {
        Relic_SO relic = LoadRelic("146 反击刺刃/反击刺刃.asset");
        Assert.AreEqual(Relic_SO.RelicBuiltInEffectType.ApplyStatusToSelfOnDefenseClashWinOncePerActorPerRound,
            relic.builtInEffects[0].effectType);
        BattleStatus_SO strength = CreateStatus();
        relic.builtInEffects[0].statusData = strength;
        BattleAllyController first = CreateAlly("First", 10);
        BattleAllyController second = CreateAlly("Second", 10);
        EnemySkill_SO skillA = CreateObject<EnemySkill_SO>();
        EnemySkill_SO skillB = CreateObject<EnemySkill_SO>();
        BattleEncounterData encounter = EnterBattle(relic);

        DefenseWin(encounter, first, skillA, 1);
        DefenseWin(encounter, first, skillB, 1);
        DefenseWin(encounter, second, skillA, 1);
        Assert.AreEqual(0, first.GetStatusStacks(strength), "The reward must wait until next round.");
        first.ConsumeRoundStartStatusEffects();
        second.ConsumeRoundStartStatusEffects();
        Assert.AreEqual(1, first.GetStatusStacks(strength));
        Assert.AreEqual(1, second.GetStatusStacks(strength));

        DefenseWin(encounter, first, skillA, 2);
        DefenseWin(encounter, first, skillA, 2);
        first.ConsumeRoundStartStatusEffects();
        Assert.AreEqual(2, first.GetStatusStacks(strength));
        encounter = EnterBattle(relic);
        DefenseWin(encounter, first, skillA, 2);
        first.ConsumeRoundStartStatusEffects();
        Assert.AreEqual(3, first.GetStatusStacks(strength));
    }

    [Test]
    public void UnrestrictedDefenseWinRelic_StillRewardsEveryWin()
    {
        Relic_SO relic = LoadRelic("040 冥龟甲/冥龟甲.asset");
        Assert.AreEqual(Relic_SO.RelicBuiltInEffectType.ApplyStatusToSelfOnDefenseClashWin, relic.builtInEffects[0].effectType);
        BattleStatus_SO status = CreateStatus();
        relic.builtInEffects[0].statusData = status;
        BattleAllyController ally = CreateAlly("Guard", 10);
        BattleEncounterData encounter = EnterBattle(relic);
        DefenseWin(encounter, ally, null, 1);
        DefenseWin(encounter, ally, null, 1);
        ally.ConsumeRoundStartStatusEffects();
        Assert.AreEqual(2, ally.GetStatusStacks(status));
    }

    [Test]
    public void FortunePotion_RollsIndependentlyForEachLivingAlly()
    {
        Relic_SO relic = LoadRelic("154 福灵剂/福灵剂.asset");
        Assert.AreEqual(0.25f, relic.builtInEffects[0].chance);
        BattleStatus_SO luck = CreateStatus();
        relic.builtInEffects[0].statusData = luck;
        BattleAllyController first = CreateAlly("First", 10);
        BattleAllyController second = CreateAlly("Second", 10);
        BattleAllyController third = CreateAlly("Third", 10);
        BattleAllyController dead = CreateAlly("Dead", 0);
        bool[] expected = null;
        int chosenSeed = -1;
        for (int seed = 0; seed < 100; seed++)
        {
            Random.InitState(seed);
            bool[] rolls = { Random.value < 0.25f, Random.value < 0.25f, Random.value < 0.25f };
            if (!rolls[0] && rolls[1])
            {
                expected = rolls;
                chosenSeed = seed;
                break;
            }
        }
        Assert.NotNull(expected, "Use a deterministic seed where allies have different outcomes.");
        Random.InitState(chosenSeed);
        BattleEncounterData encounter = EnterBattle(relic);
        RelicSystem.Process(encounter.relicInventory, RelicExecutionContext.CreateBattleRoundStart(
            null, 1, new[] { first, null, dead, second, third }, new BattleEnemyController[0]));
        Assert.AreEqual(expected[0] ? 1 : 0, first.GetStatusStacks(luck));
        Assert.AreEqual(expected[1] ? 1 : 0, second.GetStatusStacks(luck));
        Assert.AreEqual(expected[2] ? 1 : 0, third.GetStatusStacks(luck));
        Assert.AreEqual(0, dead.GetStatusStacks(luck));
    }

    [TestCase(0f, 0)]
    [TestCase(1f, 1)]
    public void FortunePotion_RespectsProbabilityBoundaries(float chance, int expectedStacks)
    {
        Relic_SO relic = LoadRelic("154 福灵剂/福灵剂.asset");
        BattleStatus_SO status = CreateStatus();
        relic.builtInEffects[0].statusData = status;
        relic.builtInEffects[0].chance = chance;
        BattleAllyController first = CreateAlly("First", 10);
        BattleAllyController second = CreateAlly("Second", 10);
        BattleEncounterData encounter = EnterBattle(relic);
        RelicSystem.Process(encounter.relicInventory, RelicExecutionContext.CreateBattleRoundStart(
            null, 1, new[] { first, second }, new BattleEnemyController[0]));
        Assert.AreEqual(expectedStacks, first.GetStatusStacks(status));
        Assert.AreEqual(expectedStacks, second.GetStatusStacks(status));
    }

    [Test]
    public void EnergyInsurance_SharedSkillAssetOnDifferentActorsHasSeparateAllowance()
    {
        Relic_SO relic = LoadRelic("144 能量保险/能量保险.asset");
        EnemySkill_SO skill = CreateObject<EnemySkill_SO>();
        skill.energyCost = 2;
        BattleAllyController first = CreateAlly("First", 10);
        BattleAllyController second = CreateAlly("Second", 10);
        BattleEncounterData encounter = EnterBattle(relic);
        Assert.AreEqual(1, RelicTriggerDispatcher.ProcessPlayerClashLost(encounter, null, skill, 0, first, null, 1).additionalSharedEnergyRecovery);
        Assert.AreEqual(0, RelicTriggerDispatcher.ProcessPlayerClashLost(encounter, null, skill, 1, first, null, 1).additionalSharedEnergyRecovery);
        Assert.AreEqual(1, RelicTriggerDispatcher.ProcessPlayerClashLost(encounter, null, skill, 0, second, null, 1).additionalSharedEnergyRecovery);
    }

    [Test]
    public void LaterBattleSettlement_PreservesDeadMembersAndOriginalOrderAndUpdatesParticipants()
    {
        RunPartyMemberState leader = Member("Leader", 0);
        leader.level = 5;
        leader.experience = 7;
        RunPartyMemberState first = Member("First", 10);
        RunPartyMemberState second = Member("Second", 10);
        MapRunSnapshot snapshot = new MapRunSnapshot
        {
            runState = new RunState { partyMembers = new List<RunPartyMemberState> { leader, first, second } }
        };
        RunPartyMemberState guest = Member("Guest", 10);
        guest.isTemporaryBattleGuest = true;
        RunPartyMemberState updatedSecond = Member("Second", 8);
        updatedSecond.level = 2;
        MapRunRuntime.ApplyEncounterResult(snapshot, new BattleEncounterData
        {
            wasVictory = true,
            resolvedPartyMembers = new List<RunPartyMemberState> { updatedSecond, guest, Member("First", 0) }
        });
        CollectionAssert.AreEqual(new[] { "Leader", "First", "Second" }, snapshot.runState.partyMembers.Select(m => m.memberId));
        Assert.AreEqual(0, snapshot.runState.partyMembers[0].currentHP);
        Assert.AreEqual(0, snapshot.runState.partyMembers[1].currentHP);
        Assert.AreEqual(8, snapshot.runState.partyMembers[2].currentHP);
        Assert.AreEqual(2, snapshot.runState.partyMembers[2].level);
        Assert.AreNotSame(leader, snapshot.runState.partyMembers[0]);

        MapRunRuntime.ApplyEncounterResult(snapshot, new BattleEncounterData
        {
            wasVictory = true,
            resolvedPartyMembers = new List<RunPartyMemberState> { Member("Second", 6) }
        });
        CollectionAssert.AreEqual(new[] { "Leader", "First", "Second" }, snapshot.runState.partyMembers.Select(m => m.memberId));
        Assert.AreEqual(5, snapshot.runState.partyMembers[0].level);
        Assert.AreEqual(7, snapshot.runState.partyMembers[0].experience);
        Assert.AreEqual(0, snapshot.runState.partyMembers[1].currentHP);
        Assert.AreEqual(6, snapshot.runState.partyMembers[2].currentHP);

        snapshot.runState.AddRelic(LoadRelic("153 永别/永别.asset"), 1);
        Assert.AreEqual(1, snapshot.runState.partyMembers.Count);
        Assert.AreEqual("Leader", snapshot.runState.partyMembers[0].memberId);
    }

    [Test]
    public void Settlement_MatchesDistinctMemberIdsEvenWhenCharacterAssetsAreShared()
    {
        CharacterData_SO character = CreateObject<CharacterData_SO>();
        RunPartyMemberState first = Member("First", 0);
        RunPartyMemberState second = Member("Second", 10);
        first.characterData = second.characterData = character;
        RunPartyMemberState updated = second.Clone();
        updated.currentHP = 3;
        MapRunSnapshot snapshot = new MapRunSnapshot
        {
            runState = new RunState { partyMembers = new List<RunPartyMemberState> { first, second } }
        };
        MapRunRuntime.ApplyEncounterResult(snapshot, new BattleEncounterData
        {
            resolvedPartyMembers = new List<RunPartyMemberState> { updated }
        });
        Assert.AreEqual(2, snapshot.runState.partyMembers.Count);
        Assert.AreEqual(0, snapshot.runState.partyMembers[0].currentHP);
        Assert.AreEqual(3, snapshot.runState.partyMembers[1].currentHP);
    }

    private static RunPartyMemberState Member(string id, int hp)
    {
        return new RunPartyMemberState { memberId = id, maxHP = 10, preCalamityMaxHP = 10, currentHP = hp };
    }

    private BattleEncounterData EnterBattle(Relic_SO relic)
    {
        BattleEncounterData encounter = new BattleEncounterData
        {
            relicInventory = new List<RunRelicState> { new RunRelicState { relicData = relic, amount = 1 } }
        };
        BattleEncounterRuntime.SetEncounter(encounter);
        BattleEncounterRuntime.ConsumeEncounter();
        return encounter;
    }

    private static void DefenseWin(BattleEncounterData encounter, BattleAllyController ally, EnemySkill_SO skill, int round)
    {
        RelicSystem.Process(encounter.relicInventory,
            RelicExecutionContext.CreatePlayerDefenseClashWin(null, skill, 0, round, ally, null));
    }

    private Relic_SO LoadRelic(string path)
    {
        Relic_SO asset = AssetDatabase.LoadAssetAtPath<Relic_SO>("Assets/Data/Relics/" + path);
        Assert.NotNull(asset, path);
        Relic_SO clone = Object.Instantiate(asset);
        createdObjects.Add(clone);
        return clone;
    }

    private BattleAllyController CreateAlly(string id, int hp)
    {
        GameObject go = new GameObject(id);
        createdObjects.Add(go);
        BattleAllyController ally = go.AddComponent<BattleAllyController>();
        ally.ApplyRuntimeState(Member(id, hp));
        return ally;
    }

    private BattleEnemyController CreateEnemy()
    {
        EnemyData_SO data = CreateObject<EnemyData_SO>();
        data.baseHP = 100;
        data.enemyName = "BurnTarget";
        GameObject go = new GameObject("BurnTarget");
        createdObjects.Add(go);
        BattleEnemyController enemy = go.AddComponent<BattleEnemyController>();
        enemy.ApplyEnemyData(data);
        return enemy;
    }

    private BattleStatus_SO CreateStatus()
    {
        BattleStatus_SO status = CreateObject<BattleStatus_SO>();
        typeof(BattleStatus_SO).GetField("statusId", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(status, "regression-status");
        typeof(BattleStatus_SO).GetField("behaviorType", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(status, BattleStatus_SO.StatusBehaviorType.None);
        return status;
    }

    private T CreateObject<T>() where T : ScriptableObject
    {
        T asset = ScriptableObject.CreateInstance<T>();
        createdObjects.Add(asset);
        return asset;
    }
}
