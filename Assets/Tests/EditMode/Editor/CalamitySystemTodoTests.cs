using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class CalamitySystemTodoTests
{
    private readonly List<Object> createdObjects = new List<Object>();

    [TearDown]
    public void TearDown()
    {
        BattleEncounterRuntime.Clear();

        for (int i = createdObjects.Count - 1; i >= 0; i--)
        {
            if (createdObjects[i] != null)
                Object.DestroyImmediate(createdObjects[i]);
        }

        createdObjects.Clear();
    }

    [Test]
    public void MerchantPriceModifier_IncreasesPriceByConfiguredPercent()
    {
        List<RunCalamityState> calamities = BuildCalamities(
            CreateCalamity(CalamityBuiltInEffectType.MerchantPricePercentModifier, 15));

        Assert.AreEqual(115, CalamitySystem.ApplyMerchantPriceModifiers(100, calamities));
    }

    [Test]
    public void RestPercentOverrides_UseLowestConfiguredPercent()
    {
        List<RunCalamityState> calamities = BuildCalamities(
            CreateCalamity(CalamityBuiltInEffectType.RestHealPercentOverride, 70),
            CreateCalamity(CalamityBuiltInEffectType.RestHealPercentOverride, 85),
            CreateCalamity(CalamityBuiltInEffectType.RestActionPointRestorePercentOverride, 70));

        Assert.AreEqual(70, CalamitySystem.ResolveRestHealPercent(100, calamities));
        Assert.AreEqual(70, CalamitySystem.ResolveRestActionPointRestorePercent(100, calamities));
    }

    [Test]
    public void EnemyBattleStartStatusEffects_ReturnOnlyConfiguredStatusEntries()
    {
        BattleStatus_SO shield = ScriptableObject.CreateInstance<BattleStatus_SO>();
        createdObjects.Add(shield);
        Calamity_SO calamity = CreateCalamity(CalamityBuiltInEffectType.EnemyBattleStartStatus, 10, shield);

        List<CalamityBuiltInEffectEntry> effects = CalamitySystem.GetEnemyBattleStartStatusEffects(BuildCalamities(calamity));

        Assert.AreEqual(1, effects.Count);
        Assert.AreSame(shield, effects[0].statusData);
        Assert.AreEqual(10, effects[0].effectAmount);
    }

    [Test]
    public void DamageAndStatModifiers_ResolveNewTodoCalamityEffects()
    {
        List<RunCalamityState> calamities = BuildCalamities(
            CreateCalamity(CalamityBuiltInEffectType.AllyDamagePercentModifier, -30),
            CreateCalamity(CalamityBuiltInEffectType.AllyMaxHpPercentModifier, -30),
            CreateCalamity(CalamityBuiltInEffectType.EnemyIncomingDamagePercentModifier, -30),
            CreateCalamity(CalamityBuiltInEffectType.EnemySpeedFlatModifier, 1),
            CreateCalamity(CalamityBuiltInEffectType.ForgeCostFlatModifier, 40),
            CreateCalamity(CalamityBuiltInEffectType.BattleStartSharedEnergyFlatModifier, -2));

        Assert.AreEqual(7, CalamitySystem.ApplyAllyDamageModifiers(10, calamities));
        Assert.AreEqual(7, CalamitySystem.ApplyAllyMaxHpModifiers(10, calamities));
        Assert.AreEqual(7, CalamitySystem.ApplyEnemyIncomingDamageModifiers(10, calamities));
        Assert.AreEqual(4, CalamitySystem.ApplyEnemySpeedModifiers(3, calamities));
        Assert.AreEqual(40, CalamitySystem.ApplyForgeCostModifiers(0, calamities));
        Assert.AreEqual(-2, CalamitySystem.ResolveBattleStartSharedEnergyDelta(calamities));
    }

    [Test]
    public void AllyMaxHpCalamity_RecalculatesFromPersistentMaxHpWithRounding()
    {
        List<RunCalamityState> calamities = BuildCalamities(
            CreateCalamity(CalamityBuiltInEffectType.AllyMaxHpPercentModifier, -30));

        Assert.AreEqual(14, CalamitySystem.ApplyAllyMaxHpModifiers(20, calamities));
        Assert.AreEqual(17, CalamitySystem.ApplyAllyMaxHpModifiers(24, calamities));
    }

    [Test]
    public void PartyEffectiveStats_ExposeCalamityAdjustedHpWithoutRewritingPersistentMaxHp()
    {
        List<RunCalamityState> calamities = BuildCalamities(
            CreateCalamity(CalamityBuiltInEffectType.AllyMaxHpPercentModifier, -30));
        RunPartyMemberState member = new RunPartyMemberState
        {
            memberId = "Strawberry Ally",
            maxHP = 20,
            currentHP = 20
        };

        RunPartyMemberEffectiveStats stats = RunPartyMemberEffectiveStatsService.Resolve(member, calamities);

        Assert.AreEqual(20, member.maxHP);
        Assert.AreEqual(14, stats.maxHP);
        Assert.AreEqual(14, stats.currentHP);

        member.maxHP = 24;
        member.currentHP = 24;
        stats = RunPartyMemberEffectiveStatsService.Resolve(member, calamities);

        Assert.AreEqual(24, member.maxHP);
        Assert.AreEqual(17, stats.maxHP);
        Assert.AreEqual(17, stats.currentHP);
    }

    [Test]
    public void AllyMaxHpCalamity_DoesNotRewritePersistentPartyMaxHpAfterSettlement()
    {
        BattleEncounterData encounter = new BattleEncounterData
        {
            activeCalamities = BuildCalamities(CreateCalamity(CalamityBuiltInEffectType.AllyMaxHpPercentModifier, -30))
        };
        BattleEncounterRuntime.SetEncounter(encounter);
        BattleEncounterRuntime.ConsumeEncounter();
        BattleAllyController ally = CreateAlly("Sick Ally", 100, 100);

        List<BattleSettlementPartyProgression> progressions = BattleSettlementPartyMemberFactory.BuildProgressions(
            new[] { ally },
            0,
            _ => 100,
            (_, _, fallbackMaxHP) => fallbackMaxHP);
        List<RunPartyMemberState> runStates = BattleSettlementPartyMemberFactory.BuildRunStates(progressions);

        Assert.AreEqual(70, ally.MaxHP);
        Assert.AreEqual(100, ally.PersistentMaxHP);
        Assert.AreEqual(1, runStates.Count);
        Assert.AreEqual(100, runStates[0].maxHP);
        Assert.AreEqual(70, runStates[0].currentHP);
    }

    private List<RunCalamityState> BuildCalamities(params Calamity_SO[] calamities)
    {
        List<RunCalamityState> states = new List<RunCalamityState>();
        for (int i = 0; i < calamities.Length; i++)
        {
            states.Add(new RunCalamityState
            {
                calamityData = calamities[i]
            });
        }

        return states;
    }

    private Calamity_SO CreateCalamity(CalamityBuiltInEffectType effectType, int amount, BattleStatus_SO statusData = null)
    {
        Calamity_SO calamity = ScriptableObject.CreateInstance<Calamity_SO>();
        calamity.calamityId = $"{effectType}_{amount}_{createdObjects.Count}";
        calamity.builtInEffects = new List<CalamityBuiltInEffectEntry>
        {
            new CalamityBuiltInEffectEntry
            {
                effectType = effectType,
                effectAmount = amount,
                statusData = statusData
            }
        };
        createdObjects.Add(calamity);
        return calamity;
    }

    private BattleAllyController CreateAlly(string name, int maxHp, int currentHp)
    {
        GameObject go = new GameObject(name);
        createdObjects.Add(go);

        BattleAllyController ally = go.AddComponent<BattleAllyController>();
        CharacterData_SO character = ScriptableObject.CreateInstance<CharacterData_SO>();
        character.characterName = name;
        character.baseHP = maxHp;
        createdObjects.Add(character);

        ally.ApplyRuntimeState(new RunPartyMemberState
        {
            memberId = name,
            characterData = character,
            level = 1,
            experience = 0,
            maxHP = maxHp,
            currentHP = currentHp
        });
        return ally;
    }
}
