using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public class WitheredCourtTests
{
    private const string Root = "Assets/Data/Enemies/Enemies/044 WitheredCourt/";
    private readonly List<Object> created = new List<Object>();
    private EnemyData_SO court;
    private EnemyData_SO bud;
    private UnityEngine.Random.State random;
    private BattleEncounterData previousEncounter;
    private BattleEncounterData previousPendingEncounter;

    [SetUp]
    public void SetUp()
    {
        random = UnityEngine.Random.state;
        previousEncounter = BattleEncounterRuntime.ActiveEncounter;
        previousPendingEncounter = BattleEncounterRuntime.CurrentEncounter;
        BattleEncounterRuntime.Clear();
        court = AssetDatabase.LoadAssetAtPath<EnemyData_SO>(Root + "WitheredCourt.asset");
        bud = AssetDatabase.LoadAssetAtPath<EnemyData_SO>(Root + "4401 NewBud/NewBud.asset");
        Assert.NotNull(court);
        Assert.NotNull(bud);
    }

    [TearDown]
    public void TearDown()
    {
        for (int i = created.Count - 1; i >= 0; i--)
            if (created[i] != null) Object.DestroyImmediate(created[i]);
        created.Clear();
        BattleEncounterRuntime.Clear();
        BattleEncounterRuntime.SetEncounter(previousEncounter);
        BattleEncounterRuntime.ConsumeEncounter();
        BattleEncounterRuntime.SetEncounter(previousPendingEncounter);
        UnityEngine.Random.state = random;
    }

    [Test]
    public void Assets_MatchIdentityStatsPassivesAndBossPool()
    {
        Assert.AreEqual(44, court.enemyID);
        Assert.AreEqual(60, court.baseHP);
        Assert.AreEqual(4401, bud.enemyID);
        Assert.AreEqual(10, bud.baseHP);
        Assert.AreEqual(2, court.baseSpeedButtonCount);
        Assert.AreEqual(1, bud.baseSpeedButtonCount);
        Assert.AreEqual(2, court.minSpeed);
        Assert.AreEqual(6, court.maxSpeed);
        Assert.AreEqual(1, bud.minSpeed);
        Assert.AreEqual(3, bud.maxSpeed);
        CollectionAssert.AreEqual(new[] { "落红无情", "落地生根", "枯萎" }, court.passiveDescriptions.Select(p => p.passiveName));
        Assert.AreEqual(EnemyData_SO.PassiveDisplayColorType.Red, court.passiveDescriptions[0].displayColorType);
        Assert.AreEqual(EnemyData_SO.PassiveDisplayColorType.Orange, court.passiveDescriptions[1].displayColorType);
        Assert.AreEqual(EnemyData_SO.PassiveDisplayColorType.Red, bud.passiveDescriptions[0].displayColorType);
        var roster = AssetDatabase.LoadAssetAtPath<EnemyRoster_SO>("Assets/Data/Enemies/EnemyGroup/EnemyRoster.asset");
        Assert.IsTrue(roster.BossGroups.Any(group => group.enemies.Contains(court)));
    }

    [TestCase(1)]
    [TestCase(4)]
    [TestCase(7)]
    public void Growth_IsForcedOnceAtTheConfiguredGlobalRounds(int round)
    {
        var enemy = Enemy(court);
        enemy.SelectRandomSkills(round);
        var selected = Selected(enemy);
        Assert.AreEqual(1, selected.Count(skill => skill == Skill("Growth")));
    }

    [TestCase(2)]
    [TestCase(3)]
    [TestCase(5)]
    public void NormalRounds_ExcludeGrowthAndLimitFlourishToOneSlot(int round)
    {
        var enemy = Enemy(court);
        for (int i = 0; i < 30; i++)
        {
            enemy.SelectRandomSkills(round);
            var selected = Selected(enemy);
            Assert.IsFalse(selected.Contains(Skill("Growth")));
            Assert.LessOrEqual(selected.Count(skill => skill == Skill("Flourish")), 1);
        }
    }

    [Test]
    public void Stage_RequiresStrictlyBelowHalfAndChangesOnlyAtNextRoundStart()
    {
        var enemy = Enemy(court);
        enemy.TakeDamage(30);
        enemy.ConsumeRoundStartStatusEffects();
        Assert.AreEqual(0, enemy.CurrentStageIndex);
        enemy.TakeDamage(1);
        Assert.AreEqual(0, enemy.CurrentStageIndex);
        enemy.ConsumeRoundStartStatusEffects();
        Assert.AreEqual(1, enemy.CurrentStageIndex);
        enemy.SelectRandomSkills(4);
        Assert.Contains(Skill("WildGrowth"), Selected(enemy));
        Assert.IsFalse(Selected(enemy).Contains(Skill("Growth")));
    }

    [Test]
    public void Summons_RespectFourLivingCopiesAndExcludeDeadOrUnrelatedEnemies()
    {
        var buds = new[] { Enemy(bud), Enemy(bud), Enemy(bud), Enemy(bud) };
        var all = buds.Concat(new[] { Enemy(court) }).ToArray();
        var growth = Skill("Growth").GetDiceEffectSettings(0).summonEffects[0];
        var wild = Skill("WildGrowth").GetDiceEffectSettings(0).summonEffects[0];
        Assert.AreEqual(0, BattleSummonEffectService.ResolveSummonCount(growth, all));
        buds[0].KillInstantly();
        Assert.AreEqual(1, BattleSummonEffectService.ResolveSummonCount(wild, all));
        buds[1].KillInstantly();
        Assert.AreEqual(2, BattleSummonEffectService.ResolveSummonCount(wild, all));
    }

    [TestCase("Flourish", "Strength_Temp")]
    [TestCase("Lush", "Strength")]
    public void Flourish_QueuesOnlyTheMatchingBudsStatusForNextRound(string skill, string statusName)
    {
        var boss = Enemy(court);
        var first = Enemy(bud);
        var dead = Enemy(bud);
        dead.KillInstantly();
        var all = new[] { boss, first, dead };
        var status = AssetDatabase.LoadAssetAtPath<BattleStatus_SO>("Assets/Data/Battle/Strength/" + statusName + ".asset");
        Apply(boss, Skill(skill), EnemySkill_SO.DiceEffectTrigger.OnSkillUse, 0, all);
        Assert.AreEqual(0, first.GetStatusStacks(status));
        first.ConsumeRoundStartStatusEffects();
        Assert.AreEqual(1, first.GetStatusStacks(status));
        Assert.AreEqual(0, boss.GetStatusStacks(status));
        Assert.AreEqual(0, dead.GetStatusStacks(status));
    }

    [Test]
    public void Decomposition_HealsEveryBudByEachActualHitAndNeverHealsBossOrDeadBud()
    {
        var boss = Enemy(court);
        var first = Enemy(bud);
        var second = Enemy(bud);
        var dead = Enemy(bud);
        boss.TakeDamage(10);
        first.TakeDamage(8);
        second.TakeDamage(4);
        dead.KillInstantly();
        var all = new[] { boss, first, second, dead };
        Apply(boss, Skill("Decomposition"), EnemySkill_SO.DiceEffectTrigger.OnHit, 3, all);
        Assert.AreEqual(5, first.CurrentHP);
        Assert.AreEqual(9, second.CurrentHP);
        Apply(boss, Skill("Decomposition"), EnemySkill_SO.DiceEffectTrigger.OnHit, 2, all);
        Assert.AreEqual(7, first.CurrentHP);
        Assert.AreEqual(10, second.CurrentHP);
        Assert.AreEqual(50, boss.CurrentHP);
        Assert.IsTrue(dead.IsDead);
    }

    [Test]
    public void Bud_UsesSeverBranchAtItsFourthPersonalRoundAfterLateSummon()
    {
        var seed = Enemy(bud);
        for (int round = 8; round < 11; round++)
        {
            seed.SelectRandomSkills(round);
            Assert.AreEqual("成长", seed.SelectedSkill.skillName);
        }
        seed.SelectRandomSkills(11);
        Assert.AreEqual("断枝", seed.SelectedSkill.skillName);
        seed.SelectRandomSkills(12);
        Assert.AreEqual("成长", seed.SelectedSkill.skillName);
    }

    [Test]
    public void BranchSacrifice_UsesHealthAtNextRoundDiesOnceAndGrantsTemporaryStrength()
    {
        var boss = Enemy(court);
        var seed = Enemy(bud);
        boss.TakeDamage(12);
        Apply(seed, BudSkill("SeverBranch"), EnemySkill_SO.DiceEffectTrigger.OnSkillUse, 0, new[] { boss, seed });
        Assert.IsFalse(seed.IsDead);
        seed.TakeDamage(4);
        seed.ConsumeRoundStartStatusEffects();
        Assert.IsTrue(seed.IsDead);
        Assert.AreEqual(54, boss.CurrentHP);
        var status = AssetDatabase.LoadAssetAtPath<BattleStatus_SO>("Assets/Data/Battle/Strength/Strength_Temp.asset");
        Assert.AreEqual(1, boss.GetStatusStacks(status));
        seed.ConsumeRoundStartStatusEffects();
        Assert.AreEqual(54, boss.CurrentHP);
        Assert.AreEqual(1, boss.GetStatusStacks(status));
    }

    [Test]
    public void BranchSacrifice_KilledBudCannotHealAndDeadBossCannotBeRevived()
    {
        var boss = Enemy(court);
        var seed = Enemy(bud);
        boss.TakeDamage(10);
        Apply(seed, BudSkill("SeverBranch"), EnemySkill_SO.DiceEffectTrigger.OnSkillUse, 0, new[] { boss, seed });
        seed.KillInstantly();
        seed.ConsumeRoundStartStatusEffects();
        Assert.AreEqual(50, boss.CurrentHP);
        var survivor = Enemy(bud);
        Apply(survivor, BudSkill("SeverBranch"), EnemySkill_SO.DiceEffectTrigger.OnSkillUse, 0, new[] { boss, survivor });
        boss.KillInstantly();
        survivor.ConsumeRoundStartStatusEffects();
        Assert.IsTrue(survivor.IsDead);
        Assert.IsTrue(boss.IsDead);
    }

    [Test]
    public void PoisonImmunity_BlocksApplication()
    {
        var boss = Enemy(court);
        var poison = AssetDatabase.LoadAssetAtPath<BattleStatus_SO>("Assets/Data/Battle/Poison/Poison.asset");
        Assert.IsFalse(boss.AddStatus(poison, poison.StatusId, poison.DisplayName, 3));
        Assert.AreEqual(0, boss.GetStatusStacks(poison));
    }

    private BattleEnemyController Enemy(EnemyData_SO data)
    {
        var go = new GameObject(data.enemyName);
        created.Add(go);
        var enemy = go.AddComponent<BattleEnemyController>();
        enemy.ApplyEnemyData(data);
        var buttons = new List<SpeedButton>();
        for (int i = 0; i < data.baseSpeedButtonCount; i++)
        {
            var button = new GameObject("SpeedButton_" + i).AddComponent<SpeedButton>();
            button.transform.SetParent(go.transform);
            buttons.Add(button);
        }
        enemy.RegisterSpeedButtons(buttons);
        return enemy;
    }

    private static List<EnemySkill_SO> Selected(BattleEnemyController enemy) =>
        (List<EnemySkill_SO>)typeof(BattleEnemyController).GetField("selectedSkillsPerButton", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(enemy);
    private static EnemySkill_SO Skill(string name) => AssetDatabase.LoadAssetAtPath<EnemySkill_SO>(Root + name + ".asset");
    private static EnemySkill_SO BudSkill(string name) => AssetDatabase.LoadAssetAtPath<EnemySkill_SO>(Root + "4401 NewBud/" + name + ".asset");
    private static void Apply(BattleEnemyController source, EnemySkill_SO skill, EnemySkill_SO.DiceEffectTrigger trigger,
        int damage, BattleEnemyController[] targets) => BattleWitheredCourtSkillService.Apply(source, skill, trigger, damage, targets);
}
