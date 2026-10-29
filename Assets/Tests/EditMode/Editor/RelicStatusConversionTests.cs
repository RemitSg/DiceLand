using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public class RelicStatusConversionTests
{
    private readonly List<Object> created = new List<Object>();
    private BattleStatus_SO erode;
    private BattleStatus_SO vulnerable;
    private Relic_SO salt;

    [SetUp]
    public void SetUp()
    {
        erode = AssetDatabase.LoadAssetAtPath<BattleStatus_SO>("Assets/Data/Battle/Erode/Erode.asset");
        vulnerable = AssetDatabase.LoadAssetAtPath<BattleStatus_SO>("Assets/Data/Battle/Vulnerable/Vulnerable.asset");
        salt = AssetDatabase.LoadAssetAtPath<Relic_SO>("Assets/Data/Relics/160 碎潮盐瓶/碎潮盐瓶.asset");
        Assert.NotNull(erode);
        Assert.NotNull(vulnerable);
        Assert.NotNull(salt);
    }

    [TearDown]
    public void TearDown()
    {
        BattleEncounterRuntime.Clear();
        for (int i = created.Count - 1; i >= 0; i--)
            if (created[i] != null)
                Object.DestroyImmediate(created[i]);
        created.Clear();
    }

    [Test]
    public void RelicAsset_IsRareValidAndIncludedInTheRelicPool()
    {
        Assert.AreEqual("160", salt.relicId);
        Assert.AreEqual(LootRarity.Rare, salt.rarity);
        Assert.NotNull(salt.icon);
        Assert.IsEmpty(RelicValidationUtility.ValidateRelic(salt));
        CollectionAssert.Contains(AssetDatabase.LoadAssetAtPath<RelicTable_SO>("Assets/Data/Relics/OuterEliteRelicLoot.asset").Relics, salt);
    }

    [TestCase(1)]
    [TestCase(7)]
    [TestCase(30)]
    public void DirectEnemyApplication_ConvertsEachApplicationToFixedFifteen(int stacks)
    {
        Activate(salt);
        BattleEnemyController enemy = Enemy();
        Assert.IsTrue(enemy.AddStatus(erode, erode.StatusId, erode.DisplayName, stacks));
        Assert.IsTrue(enemy.AddStatus(null, erode.StatusId, erode.DisplayName, stacks));
        Assert.AreEqual(0, enemy.GetStatusStacks(erode));
        Assert.AreEqual(30, enemy.GetStatusStacks(vulnerable));
    }

    [Test]
    public void NoRelicOrZeroOwnedCopies_DoesNotConvert()
    {
        Activate();
        BattleEnemyController enemy = Enemy();
        enemy.AddStatus(erode, erode.StatusId, erode.DisplayName, 3);
        Assert.AreEqual(3, enemy.GetStatusStacks(erode));
        Assert.AreEqual(0, enemy.GetStatusStacks(vulnerable));
        Activate(salt).relicInventory[0].amount = 0;
        enemy.AddStatus(erode, erode.StatusId, erode.DisplayName, 2);
        Assert.AreEqual(5, enemy.GetStatusStacks(erode));
    }

    [Test]
    public void DuplicateOwnedCopies_DoNotMultiplyTheReplacement()
    {
        Activate(salt).relicInventory[0].amount = 3;
        BattleEnemyController enemy = Enemy();
        enemy.AddStatus(erode, erode.StatusId, erode.DisplayName, 10);
        Assert.AreEqual(15, enemy.GetStatusStacks(vulnerable));
    }

    [Test]
    public void AlliesAndOtherStatuses_AreUnaffected()
    {
        Activate(salt);
        GameObject go = CreateGo("Ally");
        BattleAllyController ally = go.AddComponent<BattleAllyController>();
        ally.ApplyRuntimeState(new RunPartyMemberState { memberId = "Ally", maxHP = 100, currentHP = 100 });
        ally.AddStatus(erode, erode.StatusId, erode.DisplayName, 4);
        Assert.AreEqual(4, ally.GetStatusStacks(erode));
        Assert.AreEqual(0, ally.GetStatusStacks(vulnerable));
        BattleEnemyController enemy = Enemy();
        enemy.AddStatus(vulnerable, vulnerable.StatusId, vulnerable.DisplayName, 2);
        Assert.AreEqual(2, enemy.GetStatusStacks(vulnerable));
    }

    [Test]
    public void QueuedApplications_KeepTheirIndividualConversionAndRepeatTiming()
    {
        Activate(salt);
        BattleEnemyController enemy = Enemy();
        enemy.QueueStatusForRoundStart(erode, erode.StatusId, erode.DisplayName, 2, 1, 2);
        enemy.QueueStatusForRoundStart(erode, erode.StatusId, erode.DisplayName, 5, 1, 2);
        Assert.AreEqual(0, enemy.GetStatusStacks(vulnerable));
        enemy.ConsumeRoundStartStatusEffects();
        Assert.AreEqual(0, enemy.GetStatusStacks(erode));
        Assert.AreEqual(30, enemy.GetStatusStacks(vulnerable));
        enemy.ConsumeRoundStartStatusEffects();
        Assert.AreEqual(60, enemy.GetStatusStacks(vulnerable));
    }

    [Test]
    public void Replacement_UsesVulnerabilityImmunityAndConsumesResistanceOnlyOnce()
    {
        Activate(salt);
        BattleEnemyController erodeImmune = Enemy(erode);
        Assert.IsTrue(erodeImmune.AddStatus(erode, erode.StatusId, erode.DisplayName, 4));
        Assert.AreEqual(15, erodeImmune.GetStatusStacks(vulnerable));
        BattleEnemyController vulnerableImmune = Enemy(vulnerable);
        Assert.IsFalse(vulnerableImmune.AddStatus(erode, erode.StatusId, erode.DisplayName, 4));
        Assert.AreEqual(0, vulnerableImmune.GetStatusStacks(erode));
        Assert.AreEqual(0, vulnerableImmune.GetStatusStacks(vulnerable));
        BattleEnemyController resistant = Enemy();
        resistant.AddStatus(null, "resistance", "抵抗", 2);
        Assert.IsFalse(resistant.AddStatus(erode, erode.StatusId, erode.DisplayName, 4));
        Assert.AreEqual(1, resistant.GetStatusStacks("resistance"));
        Assert.AreEqual(0, resistant.GetStatusStacks(erode));
        Assert.AreEqual(0, resistant.GetStatusStacks(vulnerable));
    }

    [Test]
    public void ZeroNegativeOrDeadTarget_DoesNotApplyReplacement()
    {
        Activate(salt);
        BattleEnemyController enemy = Enemy();
        Assert.IsFalse(enemy.AddStatus(erode, erode.StatusId, erode.DisplayName, 0));
        Assert.IsFalse(enemy.AddStatus(erode, erode.StatusId, erode.DisplayName, -1));
        enemy.TakeDamage(10000);
        Assert.IsFalse(enemy.AddStatus(erode, erode.StatusId, erode.DisplayName, 1));
        Assert.AreEqual(0, enemy.GetStatusStacks(vulnerable));
    }

    [Test]
    public void PlayerSkillPipeline_UsesConvertedStatusForBonusesAndPreservesSkillConfiguration()
    {
        Relic_SO bonus = ScriptableObject.CreateInstance<Relic_SO>();
        created.Add(bonus);
        bonus.builtInEffects = new List<Relic_SO.RelicBuiltInEffectEntry>
        {
            new Relic_SO.RelicBuiltInEffectEntry { trigger = Relic_SO.RelicTrigger.OnStatusApplied,
                effectType = Relic_SO.RelicBuiltInEffectType.AddExtraStatusStacksOnApply, statusData = vulnerable, amount = 2 }
        };
        Activate(salt, bonus);
        BattleEnemyController enemy = Enemy();
        BattleAllyController ally = CreateGo("Ally").AddComponent<BattleAllyController>();
        ally.ApplyRuntimeState(new RunPartyMemberState { memberId = "Ally", maxHP = 100, currentHP = 100 });
        BattleButton button = CreateGo("Battle").AddComponent<BattleButton>();
        EnemySkill_SO.StatusEffectEntry effect = new EnemySkill_SO.StatusEffectEntry { statusData = erode, stackAmount = 7 };
        MethodInfo method = typeof(BattleButton).GetMethod("ApplyResolvedStatusEffect", BindingFlags.Instance | BindingFlags.NonPublic,
            null, new[] { typeof(BattleAllyController), typeof(BattleEnemyController), typeof(EnemySkill_SO.StatusEffectEntry), typeof(int) }, null);
        Assert.NotNull(method);
        method.Invoke(button, new object[] { ally, enemy, effect, 0 });
        Assert.AreEqual(0, enemy.GetStatusStacks(erode));
        Assert.AreEqual(17, enemy.GetStatusStacks(vulnerable));
        Assert.AreSame(erode, effect.statusData);
        Assert.AreEqual(7, effect.stackAmount);
    }

    [Test]
    public void WaveCry_IsFivePercentPerErodeStack_AndCannotUseConvertedStacks()
    {
        Relic_SO wave = AssetDatabase.LoadAssetAtPath<Relic_SO>("Assets/Data/Relics/079 浪鸣/浪鸣.asset");
        Assert.AreEqual(5, wave.builtInEffects[0].amount);
        BattleEncounterData encounter = Activate(wave);
        BattleEnemyController enemy = Enemy();
        enemy.AddStatus(erode, erode.StatusId, erode.DisplayName, 2);
        RelicExecutionContext damage = new RelicExecutionContext { trigger = Relic_SO.RelicTrigger.OnPlayerAboutToDealDamage, target = enemy, outgoingDamage = 100 };
        RelicSystem.Process(encounter.relicInventory, damage);
        Assert.AreEqual(10, damage.outgoingDamagePercentModifier);
        encounter = Activate(wave, salt);
        BattleEnemyController converted = Enemy();
        converted.AddStatus(erode, erode.StatusId, erode.DisplayName, 2);
        damage = new RelicExecutionContext { trigger = Relic_SO.RelicTrigger.OnPlayerAboutToDealDamage, target = converted, outgoingDamage = 100 };
        RelicSystem.Process(encounter.relicInventory, damage);
        Assert.AreEqual(0, damage.outgoingDamagePercentModifier);
        Assert.AreEqual(15, converted.GetStatusStacks(vulnerable));
    }

    private BattleEncounterData Activate(params Relic_SO[] relics)
    {
        BattleEncounterData encounter = new BattleEncounterData();
        foreach (Relic_SO relic in relics)
            encounter.relicInventory.Add(new RunRelicState { relicData = relic, amount = 1 });
        BattleEncounterRuntime.SetEncounter(encounter);
        BattleEncounterRuntime.ConsumeEncounter();
        return encounter;
    }

    private BattleEnemyController Enemy(BattleStatus_SO immune = null)
    {
        EnemyData_SO data = ScriptableObject.CreateInstance<EnemyData_SO>();
        created.Add(data);
        data.baseHP = 1000;
        data.statusImmunityRules = new List<EnemyData_SO.StatusImmunityRule>();
        if (immune != null)
            data.statusImmunityRules.Add(new EnemyData_SO.StatusImmunityRule { statusData = immune, allowApplicationWhileImmune = false });
        BattleEnemyController enemy = CreateGo("Target").AddComponent<BattleEnemyController>();
        enemy.ApplyEnemyData(data);
        return enemy;
    }

    private GameObject CreateGo(string name)
    {
        GameObject go = new GameObject(name);
        created.Add(go);
        return go;
    }
}
