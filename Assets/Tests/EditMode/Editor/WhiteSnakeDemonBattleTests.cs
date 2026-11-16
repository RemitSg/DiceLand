using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public sealed class WhiteSnakeDemonBattleTests
{
    private const string GroupPath = "Assets/Data/Enemies/EnemyGroup/Events/WhiteSnakeDemon.asset";
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private AgentBattleSession session;
    private EnemySkill_SO testSkill;
    private BattleButton battle;
    private BattleEnemyController snake;
    private BattleAllyController ally;

    [SetUp]
    public void SetUp()
    {
        var group = AssetDatabase.LoadAssetAtPath<EnemyGroup_SO>(GroupPath);
        var catalogue = AgentBattleSession.Catalogue();
        var character = Load<CharacterData_SO>(catalogue.characters.Single(item => item.name == "Chara").id);
        var weapon = Load<Weapon_SO>(catalogue.weapons.Single(item => item.name == "新手木剑").id);
        var encounter = new BattleEncounterData
        {
            enemyGroup = group,
            tileType = MapTileType.Event,
            partyMembers = new List<RunPartyMemberState>
            {
                new RunPartyMemberState { characterData = character, weaponData = weapon, maxHP = 100, currentHP = 100 }
            }
        };
        session = new AgentBattleSession(new AgentBattleRequest { seed = 45 }, encounter, null);
        battle = Object.FindObjectOfType<BattleButton>();
        snake = Object.FindObjectOfType<BattleEnemyController>();
        ally = Object.FindObjectOfType<BattleAllyController>();
    }

    [TearDown]
    public void TearDown()
    {
        session?.Dispose();
        if (testSkill != null) Object.DestroyImmediate(testSkill);
    }

    [Test]
    public void OddRoundsAddHasteToBothSlotsAndOnlyEvenRoundsAssignConstrict()
    {
        AgentBattleObservation state = session.Observe();
        for (int round = 1; round <= 5; round++)
        {
            Assert.AreEqual(round, state.round);
            var enemy = state.actors.Single(actor => actor.side == "enemy");
            Assert.AreEqual(2, enemy.slots.Length);
            var skills = enemy.slots.Select(slot => slot.selectedSkill.name).ToArray();
            int haste = round % 2;
            Assert.AreEqual(haste, enemy.statuses.SingleOrDefault(status => status.id == "swiftness")?.stacks ?? 0);
            Assert.AreEqual(haste, snake.PendingRoundStartSpeedBonus);
            Assert.IsTrue(enemy.slots.All(slot => slot.speed >= 2 + haste && slot.speed <= 6 + haste));
            Assert.AreEqual(2, skills.Distinct().Count());
            Assert.AreEqual(1 - haste, skills.Count(skill => skill == "绞缠"));
            Assert.IsTrue(skills.All(skill => skill == "绞缠" || skill == "毒牙" || skill == "盘身扫尾"));
            if (round == 5) break;
            state = session.Act(new AgentBattleRequest
            {
                op = "commit", sessionId = state.sessionId, revision = state.revision, requestId = Guid.NewGuid().ToString("N")
            }).state;
        }
    }

    [Test]
    public void AttackDiceStackBeforeDamageAndRoundEndClearsTheCappedBonus()
    {
        Assert.AreEqual(1, snake.TakeDamage(1));
        Assert.AreEqual(0f, snake.DamageReductionPercent);
        int[] actualDamage = new int[6];
        for (int i = 0; i < actualDamage.Length; i++)
        {
            actualDamage[i] = (int)InvokeBattle("ApplyAllyHit", ally, snake, 4, 0, true, 0);
            Assert.AreEqual(Mathf.Min((i + 1) * 10, 50), snake.DamageReductionPercent);
        }
        CollectionAssert.AreEqual(new[] { 3, 3, 2, 2, 2, 2 }, actualDamage);
        Drain(battle.ExecuteHeadlessRound());
        Assert.AreEqual(0f, snake.DamageReductionPercent);
        Assert.AreEqual(4, snake.TakeDamage(4));
    }

    [Test]
    public void VenomFangAppliesPoisonAtTheNextRoundStartOnly()
    {
        var source = snake.SpeedButtons.Single(button => snake.GetSelectedSkillForButton(button).skillName == "毒牙");
        var poison = AssetDatabase.LoadAssetAtPath<BattleStatus_SO>("Assets/Data/Battle/Poison/Poison.asset");
        Drain((IEnumerator)InvokeBattle("ResolveEnemySingleAttackAction", snake, ally, source, ally.SpeedButtons[0]));
        Assert.AreEqual(0, ally.GetStatusStacks(poison));
        ally.ConsumeRoundEndStatusEffects();
        ally.ConsumeRoundStartStatusEffects();
        Assert.AreEqual(1, ally.GetStatusStacks(poison));
        ally.ConsumeRoundEndStatusEffects();
        ally.ConsumeRoundStartStatusEffects();
        Assert.AreEqual(0, ally.GetStatusStacks(poison));
    }

    [TestCase(true, 4, 3, 4)]
    [TestCase(true, 4, 4, 3)]
    [TestCase(false, 4, 3, 4)]
    [TestCase(false, 4, 5, 3)]
    public void ConstrictComparesTheParticipatingSlots(bool clash, int sourceSpeed, int targetSpeed, int expectedDamage)
    {
        snake.SelectRandomSkills(2);
        var source = snake.SpeedButtons.Single(button => snake.GetSelectedSkillForButton(button).skillName == "绞缠");
        snake.GenerateRandomSpeed(2, 2);
        ally.SetRoundSpeed(9);
        var target = ally.SpeedButtons[0];
        source.Speed = sourceSpeed;
        target.Speed = targetSpeed;
        var misfortune = AssetDatabase.LoadAssetAtPath<BattleStatus_SO>("Assets/Data/Battle/Misfortune/Misfortune.asset");
        Assert.IsTrue(snake.AddStatus(misfortune, misfortune.StatusId, misfortune.DisplayName, 1));
        testSkill = ScriptableObject.CreateInstance<EnemySkill_SO>();
        testSkill.minPower = testSkill.maxPower = testSkill.diceCount = 1;
        ally.GrantTemporarySkill(testSkill, true);

        int before = ally.CurrentHP;
        IEnumerator action = clash
            ? (IEnumerator)InvokeBattle("ResolveClashAction", ally, snake, target, source)
            : (IEnumerator)InvokeBattle("ResolveEnemySingleAttackAction", snake, ally, source, target);
        Drain(action);
        Assert.AreEqual(expectedDamage, before - ally.CurrentHP);
    }

    private object InvokeBattle(string method, params object[] args) => typeof(BattleButton).GetMethod(method, Private).Invoke(battle, args);

    private static T Load<T>(string guid) where T : Object => AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));

    private static void Drain(IEnumerator routine)
    {
        while (routine.MoveNext())
        {
            if (routine.Current is IEnumerator nested) Drain(nested);
        }
    }
}
