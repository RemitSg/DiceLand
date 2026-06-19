using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class RelicSystem143To158Tests
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
    public void BuiltInEffects143To158_AreRegisteredAndHaveMetadata()
    {
        for (int value = 124; value <= 135; value++)
        {
            Relic_SO.RelicBuiltInEffectType effectType = (Relic_SO.RelicBuiltInEffectType)value;
            Assert.IsTrue(RelicSystem.IsBuiltInEffectRegistered(effectType), $"{effectType} should be registered.");
            Assert.IsTrue(RelicBuiltInEffectMetadataCatalog.TryGet(effectType, out _), $"{effectType} should have metadata.");
        }
    }

    [Test]
    public void EnergyInsurance_RefundsOncePerSkillPerRound()
    {
        EnemySkill_SO skill = CreateObject<EnemySkill_SO>("EnergySkill");
        skill.energyCost = 2;
        Relic_SO relic = CreateRelic(
            Relic_SO.RelicTrigger.OnPlayerClashLost,
            Relic_SO.RelicBuiltInEffectType.RefundEnergyOnClashLostOncePerSkillPerRound,
            effect => effect.amount = 1);
        BattleEncounterData encounter = new BattleEncounterData
        {
            relicInventory = new List<RunRelicState> { new RunRelicState { relicData = relic, amount = 1 } }
        };
        BattleEncounterRuntime.SetEncounter(encounter);
        BattleEncounterRuntime.ConsumeEncounter();

        RelicExecutionContext first = RelicTriggerDispatcher.ProcessPlayerClashLost(encounter, null, skill, 0, null, null, 1);
        RelicExecutionContext second = RelicTriggerDispatcher.ProcessPlayerClashLost(encounter, null, skill, 1, null, null, 1);
        RelicExecutionContext nextRound = RelicTriggerDispatcher.ProcessPlayerClashLost(encounter, null, skill, 0, null, null, 2);

        Assert.AreEqual(1, first.additionalSharedEnergyRecovery);
        Assert.AreEqual(0, second.additionalSharedEnergyRecovery);
        Assert.AreEqual(1, nextRound.additionalSharedEnergyRecovery);
    }

    [Test]
    public void CrownLegacy_GrantsMoneyRelicsAndBlueprintsFromConfiguredTables()
    {
        Relic_SO source = CreateObject<Relic_SO>("CrownLegacy");
        Relic_SO firstRelic = CreateObject<Relic_SO>("FirstRelic");
        Relic_SO secondRelic = CreateObject<Relic_SO>("SecondRelic");
        CraftingBlueprint_SO firstBlueprint = CreateObject<CraftingBlueprint_SO>("FirstBlueprint");
        CraftingBlueprint_SO secondBlueprint = CreateObject<CraftingBlueprint_SO>("SecondBlueprint");
        RelicTable_SO relicTable = CreateRelicTable(firstRelic, secondRelic);
        BlueprintTable_SO blueprintTable = CreateBlueprintTable(firstBlueprint, secondBlueprint);
        LootRarityWeightTable_SO blueprintWeights = CreateRarityWeightTable(LootRarity.Common);
        source.builtInEffects = new List<Relic_SO.RelicBuiltInEffectEntry>
        {
            new Relic_SO.RelicBuiltInEffectEntry
            {
                trigger = Relic_SO.RelicTrigger.OnRelicAcquired,
                effectType = Relic_SO.RelicBuiltInEffectType.GrantMoneyOnAcquire,
                amount = 200,
                chance = 1f
            },
            new Relic_SO.RelicBuiltInEffectEntry
            {
                trigger = Relic_SO.RelicTrigger.OnRelicAcquired,
                effectType = Relic_SO.RelicBuiltInEffectType.GrantRandomRelicsAndBlueprintsOnAcquire,
                amount = 2,
                maxStacks = 2,
                relicTable = relicTable,
                blueprintTable = blueprintTable,
                blueprintRarityWeightTable = blueprintWeights,
                chance = 1f
            }
        };

        RunState runState = new RunState();
        runState.AddRelic(source, 1);

        Assert.AreEqual(200, runState.money);
        Assert.AreEqual(2, runState.GetRelicAmount(firstRelic) + runState.GetRelicAmount(secondRelic));
        Assert.AreEqual(2, runState.GetBlueprintAmount(firstBlueprint) + runState.GetBlueprintAmount(secondBlueprint));
    }

    [Test]
    public void Farewell_KeepsOnlyInitialPartyMemberAndDisablesRecruit()
    {
        Relic_SO relic = CreateRelic(
            Relic_SO.RelicTrigger.OnRelicAcquired,
            Relic_SO.RelicBuiltInEffectType.KeepOnlyInitialPartyMemberAndDisableRecruitOnAcquire,
            effect => effect.amount = 20);
        RunPartyMemberState initial = new RunPartyMemberState { memberId = "initial", maxHP = 10, currentHP = 10, preCalamityMaxHP = 10 };
        RunPartyMemberState other = new RunPartyMemberState { memberId = "other", maxHP = 10, currentHP = 10, preCalamityMaxHP = 10 };
        RunState runState = new RunState { partyMembers = new List<RunPartyMemberState> { initial, other } };

        runState.AddRelic(relic, 1);

        Assert.AreEqual(1, runState.partyMembers.Count);
        Assert.AreSame(initial, runState.partyMembers[0]);
        Assert.IsTrue(runState.recruitmentDisabled);
        Assert.AreEqual(30, runState.partyMembers[0].maxHP);
    }

    [Test]
    public void DiagonalMovement_UsesRunStateFlag()
    {
        GameObject go = new GameObject("Board");
        createdObjects.Add(go);
        BoardManager board = go.AddComponent<BoardManager>();

        Assert.IsFalse(board.IsAdjacent(Vector2Int.zero, Vector2Int.one, false));
        Assert.IsTrue(board.IsAdjacent(Vector2Int.zero, Vector2Int.one, true));
    }

    [Test]
    public void FixedFishReforge_ReturnsConfiguredCombinedRelicOnlyForPair()
    {
        Relic_SO yin = CreateObject<Relic_SO>("Yin");
        Relic_SO yang = CreateObject<Relic_SO>("Yang");
        Relic_SO combined = CreateObject<Relic_SO>("Combined");
        Relic_SO filler = CreateObject<Relic_SO>("Filler");
        yin.builtInEffects = CreateFixedReforgeEffects(combined);
        yang.builtInEffects = CreateFixedReforgeEffects(combined);

        Assert.IsTrue(RelicFixedReforgeService.TryResolveFixedResult(null, yin, yang, out Relic_SO result));
        Assert.AreSame(combined, result);
        Assert.IsTrue(RelicFixedReforgeService.IsFixedResult(null, yin, yang, combined));
        Assert.IsFalse(RelicFixedReforgeService.TryResolveFixedResult(null, yin, filler, out _));
    }

    private static List<Relic_SO.RelicBuiltInEffectEntry> CreateFixedReforgeEffects(Relic_SO resultRelic)
    {
        return new List<Relic_SO.RelicBuiltInEffectEntry>
        {
            new Relic_SO.RelicBuiltInEffectEntry
            {
                trigger = Relic_SO.RelicTrigger.OnRelicAcquired,
                effectType = Relic_SO.RelicBuiltInEffectType.ConfigureFixedRelicReforgePair,
                relicData = resultRelic,
                chance = 1f
            }
        };
    }

    private Relic_SO CreateRelic(
        Relic_SO.RelicTrigger trigger,
        Relic_SO.RelicBuiltInEffectType effectType,
        System.Action<Relic_SO.RelicBuiltInEffectEntry> configure)
    {
        Relic_SO relic = CreateObject<Relic_SO>($"Relic_{effectType}");
        relic.builtInEffects = new List<Relic_SO.RelicBuiltInEffectEntry>();
        Relic_SO.RelicBuiltInEffectEntry effect = new Relic_SO.RelicBuiltInEffectEntry
        {
            trigger = trigger,
            effectType = effectType,
            chance = 1f
        };
        configure?.Invoke(effect);
        relic.builtInEffects.Add(effect);
        return relic;
    }

    private T CreateObject<T>(string name) where T : ScriptableObject
    {
        T instance = ScriptableObject.CreateInstance<T>();
        instance.name = name;
        createdObjects.Add(instance);
        return instance;
    }

    private RelicTable_SO CreateRelicTable(params Relic_SO[] relics)
    {
        RelicTable_SO table = CreateObject<RelicTable_SO>("RelicTable");
        SetPrivateField(table, "relics", new List<Relic_SO>(relics));
        SetPrivateField(table, "rarityWeights", new List<RelicTable_SO.RarityWeightEntry>
        {
            new RelicTable_SO.RarityWeightEntry { rarity = LootRarity.Common, weight = 1f },
            new RelicTable_SO.RarityWeightEntry { rarity = LootRarity.Uncommon, weight = 1f },
            new RelicTable_SO.RarityWeightEntry { rarity = LootRarity.Rare, weight = 1f },
            new RelicTable_SO.RarityWeightEntry { rarity = LootRarity.Epic, weight = 1f },
            new RelicTable_SO.RarityWeightEntry { rarity = LootRarity.Legendary, weight = 1f }
        });
        return table;
    }

    private BlueprintTable_SO CreateBlueprintTable(params CraftingBlueprint_SO[] blueprints)
    {
        BlueprintTable_SO table = CreateObject<BlueprintTable_SO>("BlueprintTable");
        SetPrivateField(table, "blueprints", new List<CraftingBlueprint_SO>(blueprints));
        return table;
    }

    private LootRarityWeightTable_SO CreateRarityWeightTable(LootRarity rarity)
    {
        LootRarityWeightTable_SO table = CreateObject<LootRarityWeightTable_SO>("RarityWeights");
        SetPrivateField(table, "rarityWeights", new List<LootRarityWeightTable_SO.RarityWeightEntry>
        {
            new LootRarityWeightTable_SO.RarityWeightEntry { rarity = rarity, weight = 1f }
        });
        return table;
    }

    private static void SetPrivateField<T>(object target, string fieldName, T value)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field, $"{fieldName} should exist on {target.GetType().Name}.");
        field.SetValue(target, value);
    }
}
