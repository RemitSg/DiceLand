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
            preCalamityMaxHP = 20,
            maxHP = 20,
            currentHP = 20
        };

        RunPartyMemberMaxHpService.Recalculate(member, calamities);

        Assert.AreEqual(20, member.preCalamityMaxHP);
        Assert.AreEqual(14, member.maxHP);
        Assert.AreEqual(14, member.currentHP);

        member.preCalamityMaxHP = 24;
        member.currentHP = 24;
        RunPartyMemberMaxHpService.Recalculate(member, calamities);

        Assert.AreEqual(24, member.preCalamityMaxHP);
        Assert.AreEqual(17, member.maxHP);
        Assert.AreEqual(17, member.currentHP);
    }

    [Test]
    public void PartyMaxHpRecalculation_AdjustsCurrentHpByMaxHpIncreaseOnly()
    {
        List<RunCalamityState> calamities = BuildCalamities(
            CreateCalamity(CalamityBuiltInEffectType.AllyMaxHpPercentModifier, -30));
        RunPartyMemberState member = new RunPartyMemberState
        {
            memberId = "Strawberry Ally",
            preCalamityMaxHP = 16,
            maxHP = 11,
            currentHP = 8
        };

        RunPartyMemberMaxHpService.AddPreCalamityMaxHp(member, 4, calamities);

        Assert.AreEqual(20, member.preCalamityMaxHP);
        Assert.AreEqual(14, member.maxHP);
        Assert.AreEqual(11, member.currentHP);

        RunPartyMemberMaxHpService.AddPreCalamityMaxHp(member, -6, calamities);

        Assert.AreEqual(14, member.preCalamityMaxHP);
        Assert.AreEqual(10, member.maxHP);
        Assert.AreEqual(10, member.currentHP);

        member.currentHP = 6;
        RunPartyMemberMaxHpService.AddPreCalamityMaxHp(member, -4, calamities);

        Assert.AreEqual(10, member.preCalamityMaxHP);
        Assert.AreEqual(7, member.maxHP);
        Assert.AreEqual(6, member.currentHP);
    }

    [Test]
    public void ApplyChosenCalamity_RewritesRunPartyMaxHpForMapAndFutureBattles()
    {
        RunState runState = new RunState
        {
            partyMembers = new List<RunPartyMemberState>
            {
                new RunPartyMemberState
                {
                    memberId = "Molly",
                    preCalamityMaxHP = 20,
                    maxHP = 20,
                    currentHP = 20
                }
            }
        };
        Calamity_SO calamity = CreateCalamity(CalamityBuiltInEffectType.AllyMaxHpPercentModifier, -30);

        CalamitySelectionService.ApplyChosenCalamity(runState, calamity, 1);

        Assert.AreEqual(20, runState.partyMembers[0].preCalamityMaxHP);
        Assert.AreEqual(14, runState.partyMembers[0].maxHP);
        Assert.AreEqual(14, runState.partyMembers[0].currentHP);
    }

    [Test]
    public void AllyMaxHpCalamity_SettlementLevelUpHealsHalfOfEffectiveMaxHp()
    {
        BattleEncounterData encounter = new BattleEncounterData
        {
            activeCalamities = BuildCalamities(CreateCalamity(CalamityBuiltInEffectType.AllyMaxHpPercentModifier, -30))
        };
        BattleEncounterRuntime.SetEncounter(encounter);
        BattleEncounterRuntime.ConsumeEncounter();
        BattleAllyController ally = CreateAlly("Growing Ally", 20, 8);

        List<BattleSettlementPartyProgression> progressions = BattleSettlementPartyMemberFactory.BuildProgressions(
            new[] { ally },
            10,
            _ => 10,
            (_, level, _) => level == 2 ? 24 : 20);
        List<RunPartyMemberState> runStates = BattleSettlementPartyMemberFactory.BuildRunStates(progressions);

        Assert.AreEqual(24, runStates[0].preCalamityMaxHP);
        Assert.AreEqual(17, runStates[0].maxHP);
        Assert.AreEqual(17, runStates[0].currentHP);
    }

    [Test]
    public void AllyMaxHpCalamity_SettlementLevelUpKeepsFlatMaxHpBonus()
    {
        BattleEncounterData encounter = new BattleEncounterData
        {
            activeCalamities = BuildCalamities(CreateCalamity(CalamityBuiltInEffectType.AllyMaxHpPercentModifier, -30))
        };
        BattleEncounterRuntime.SetEncounter(encounter);
        BattleEncounterRuntime.ConsumeEncounter();
        BattleAllyController ally = CreateAlly("Strawberry Ally", 20, 10, characterBaseHp: 16, maxHpFlatBonus: 4);

        List<BattleSettlementPartyProgression> progressions = BattleSettlementPartyMemberFactory.BuildProgressions(
            new[] { ally },
            10,
            _ => 10,
            (_, level, _) => level == 2 ? 24 : 16);
        List<RunPartyMemberState> runStates = BattleSettlementPartyMemberFactory.BuildRunStates(progressions);

        Assert.AreEqual(4, runStates[0].maxHPFlatBonus);
        Assert.AreEqual(28, runStates[0].preCalamityMaxHP);
        Assert.AreEqual(20, runStates[0].maxHP);
        Assert.AreEqual(20, runStates[0].currentHP);
    }

    [Test]
    public void AllyMaxHpCalamity_SettlementCarriesEffectiveAndPreCalamityMaxHp()
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
        Assert.AreEqual(100, runStates[0].preCalamityMaxHP);
        Assert.AreEqual(70, runStates[0].maxHP);
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

    private BattleAllyController CreateAlly(string name, int maxHp, int currentHp, int characterBaseHp = 0, int maxHpFlatBonus = 0)
    {
        GameObject go = new GameObject(name);
        createdObjects.Add(go);

        BattleAllyController ally = go.AddComponent<BattleAllyController>();
        CharacterData_SO character = ScriptableObject.CreateInstance<CharacterData_SO>();
        character.characterName = name;
        character.baseHP = characterBaseHp > 0 ? characterBaseHp : maxHp;
        createdObjects.Add(character);

        ally.ApplyRuntimeState(new RunPartyMemberState
        {
            memberId = name,
            characterData = character,
            level = 1,
            experience = 0,
            maxHPFlatBonus = maxHpFlatBonus,
            preCalamityMaxHP = maxHp,
            maxHP = maxHp,
            currentHP = currentHp
        });
        return ally;
    }
}
