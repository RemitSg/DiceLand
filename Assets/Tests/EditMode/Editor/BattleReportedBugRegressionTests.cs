using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

public sealed class BattleReportedBugRegressionTests
{
    private AgentBattleSession session;
    private readonly List<Object> temporary = new List<Object>();

    [TearDown]
    public void TearDown()
    {
        session?.Dispose();
        session = null;
        foreach (Object value in temporary) Object.DestroyImmediate(value);
        temporary.Clear();
    }

    private T Create<T>() where T : ScriptableObject
    {
        T value = ScriptableObject.CreateInstance<T>();
        temporary.Add(value);
        return value;
    }

    private EnemySkill_SO Skill(string name, int power, bool defense)
    {
        EnemySkill_SO value = Create<EnemySkill_SO>();
        value.skillName = name;
        value.energyCost = 0;
        value.skillCategory = defense ? EnemySkill_SO.SkillCategory.Defense : EnemySkill_SO.SkillCategory.Attack;
        value.diceCount = 1;
        value.dicePowerRanges = new[] { new EnemySkill_SO.DicePowerRange { minPower = power, maxPower = power } };
        return value;
    }

    private void Open(int allyPower, bool allyDefense, int enemyPower, bool enemyDefense)
    {
        AgentBattleCatalogue catalogue = AgentBattleSession.Catalogue();
        session = new AgentBattleSession(new AgentBattleRequest
        {
            op = "create", seed = 1234, maxRounds = 5,
            party = new[] { new AgentBattlePartyMember
            {
                character = catalogue.characters.Single(c => c.name == "Chara").id,
                weapon = catalogue.weapons.Single(w => w.name == "新手木剑").id
            } },
            enemies = new[] { catalogue.enemies.Single(e => e.name == "绿色史莱姆").id }
        });
        BattleAllyController ally = Object.FindObjectOfType<BattleAllyController>();
        ally.GrantTemporarySkill(Skill("Fixed ally die", allyPower, allyDefense), true);
        Assert.IsTrue(ally.SetSelectedSkillIndexForButton(ally.SpeedButtons[0], 0));
        Object.FindObjectOfType<BattleEnemyController>().SetRuntimeCopiedSkills(new[] { Skill("Fixed enemy die", enemyPower, enemyDefense) });
    }

    private AgentBattleObservation Act(string op, AgentBattleLegalAction action = null)
    {
        AgentBattleObservation before = session.Observe();
        return session.Act(new AgentBattleRequest
        {
            op = op, sessionId = before.sessionId, revision = before.revision, requestId = Guid.NewGuid().ToString("N"),
            actorId = action?.actorId, targetId = action?.targetId,
            buttonIndex = action != null ? action.buttonIndex : 0,
            targetButtonIndex = action != null ? action.targetButtonIndex : 0
        }).state;
    }

    private AgentBattleObservation Clash()
    {
        Act("target", session.Observe().legalActions.First(a => a.op == "target"));
        return Act("commit");
    }

    [TestCase(3, false, 1, false, "enemy_0", "友方胜")]
    [TestCase(1, false, 3, false, "ally_0", "敌方胜")]
    [TestCase(3, false, 1, true, "enemy_0", "敌方防御骰拼点落败")]
    [TestCase(1, true, 3, false, "ally_0", "友方防御骰拼点落败")]
    public void ClashDamageLog_ReportsHpAfterTheRealImpact(
        int allyPower, bool allyDefense, int enemyPower, bool enemyDefense, string damagedActor, string resultText)
    {
        Open(allyPower, allyDefense, enemyPower, enemyDefense);
        int hpBefore = session.Observe().actors.Single(a => a.id == damagedActor).hp;
        AgentBattleObservation after = Clash();
        AgentBattleActor victim = after.actors.Single(a => a.id == damagedActor);
        AgentBattleEvent damage = after.events.Single(e => e.kind == "damage" && e.actorId == damagedActor);
        string message = after.events.Single(e => e.kind == "log" && e.message.Contains(resultText)).message;
        Assert.AreEqual(hpBefore - damage.amount, victim.hp, "No other effect changes HP in this fixture.");
        Assert.Less(victim.hp, hpBefore);
        StringAssert.Contains($"剩余HP {victim.hp}/{victim.maxHp}", message);
    }

    [TestCase(0)]
    [TestCase(2)]
    [TestCase(20)]
    public void Unyielding_LethalHitDiesNextRoundEvenAfterRoundEndHealing(int healAmount)
    {
        Open(10, false, 1, false);
        AddCalamity(CalamityBuiltInEffectType.EnemyDelayDeathUntilNextRoundStart, 1);
        if (healAmount > 0) AddCalamity(CalamityBuiltInEffectType.EnemyRoundEndHealFlat, healAmount);
        AgentBattleObservation after = Clash();
        AgentBattleActor enemy = after.actors.Single(a => a.id == "enemy_0");
        Assert.AreEqual(0, enemy.hp);
        Assert.IsTrue(enemy.dead);
        Assert.AreEqual("victory", after.outcome);
        Assert.AreEqual(1, after.events.Count(e => e.kind == "death" && e.actorId == enemy.id));
        if (healAmount > 0)
            Assert.IsTrue(after.events.Any(e => e.kind == "log" && e.message.Contains("回合结束时") && e.message.Contains("恢复")), "Exercise actual calamity healing before the next round.");
        Assert.IsTrue(after.events.Any(e => e.kind == "log" && e.message.Contains("不屈到期")));
    }

    [Test]
    public void Unyielding_NonlethalDamageAndHealingDoNotScheduleDeath()
    {
        Open(3, false, 1, false);
        AddCalamity(CalamityBuiltInEffectType.EnemyDelayDeathUntilNextRoundStart, 1);
        AddCalamity(CalamityBuiltInEffectType.EnemyRoundEndHealFlat, 2);
        AgentBattleObservation after = Clash();
        AgentBattleActor enemy = after.actors.Single(a => a.id == "enemy_0");
        Assert.AreEqual(4, enemy.hp);
        Assert.IsFalse(enemy.dead);
        Assert.IsFalse(after.events.Any(e => e.kind == "death"));
    }

    private void AddCalamity(CalamityBuiltInEffectType type, int amount)
    {
        Calamity_SO calamity = Create<Calamity_SO>();
        calamity.calamityId = type.ToString();
        calamity.builtInEffects = new List<CalamityBuiltInEffectEntry>
        {
            new CalamityBuiltInEffectEntry { effectType = type, effectAmount = amount }
        };
        session.Encounter.activeCalamities.Add(new RunCalamityState { calamityData = calamity, acquiredAtTier = 1 });
    }

    [Test]
    public void Disconnect_ReleasesReservationRestoresEnemyIntentAndRetriesWithoutMutation()
    {
        Open(3, false, 1, false);
        var ally = Object.FindObjectOfType<BattleAllyController>();
        ally.SelectedSkill.energyCost = 2;
        Act("target", session.Observe().legalActions.First(a => a.op == "target"));
        var before = session.Observe();
        Assert.AreEqual(2, before.reservedEnergy);
        var offered = before.legalActions.Single(a => a.op == "disconnect");
        var request = new AgentBattleRequest { op = "disconnect", sessionId = before.sessionId, revision = before.revision,
            requestId = Guid.NewGuid().ToString("N"), actorId = offered.actorId, buttonIndex = offered.buttonIndex };
        var response = session.Act(request);
        Assert.IsTrue(string.IsNullOrEmpty(response.state.actors.Single(a => a.id == "ally_0").slots[0].targetId));
        Assert.AreEqual("ally_0", response.state.actors.Single(a => a.id == "enemy_0").slots[0].targetId);
        Assert.AreEqual(0, response.state.reservedEnergy);
        Assert.AreEqual(JsonUtility.ToJson(response), JsonUtility.ToJson(session.Act(request)));
        Assert.IsFalse(response.state.legalActions.Any(a => a.op == "disconnect"));
        Assert.Throws<ArgumentException>(() => Act("disconnect", offered));
    }

    [Test]
    public void Observe_ExposesActualPendingDeathWithoutClearingIt()
    {
        Open(3, false, 1, false);
        AddCalamity(CalamityBuiltInEffectType.EnemyDelayDeathUntilNextRoundStart, 1);
        var enemy = Object.FindObjectOfType<BattleEnemyController>();
        enemy.TakeDamage(99); enemy.Heal(2);
        Assert.IsTrue(session.Observe().actors.Single(a => a.id == "enemy_0").pendingDelayedDeath);
        Assert.IsTrue(session.Observe().actors.Single(a => a.id == "enemy_0").pendingDelayedDeath);
        Assert.IsTrue(enemy.TryApplyPendingCalamityDelayedDeath());
        Assert.IsFalse(session.Observe().actors.Single(a => a.id == "enemy_0").pendingDelayedDeath);
    }

    [Test]
    public void Observe_ReportsCurrentStageAndAuthoredPassives()
    {
        Open(3, false, 1, false);
        var enemy = Object.FindObjectOfType<BattleEnemyController>();
        var data = Create<EnemyData_SO>();
        data.enemyName = "Stage test"; data.baseHP = 10;
        data.stageSprites = new List<EnemyData_SO.EnemyStageEntry> { new EnemyData_SO.EnemyStageEntry { stageName = "Opening" }, new EnemyData_SO.EnemyStageEntry { stageName = "Enraged" } };
        data.passiveDescriptions = new List<EnemyData_SO.PassiveDescriptionEntry> { new EnemyData_SO.PassiveDescriptionEntry { passiveName = "Visible passive", description = "Actual configured text" } };
        enemy.ApplyEnemyData(data); enemy.TryTransitionToStage(1);
        var actor = session.Observe().actors.Single(a => a.id == "enemy_0");
        Assert.AreEqual("Enraged", actor.stageName);
        CollectionAssert.AreEqual(new[] { "Visible passive：Actual configured text" }, actor.passives);
    }
}
