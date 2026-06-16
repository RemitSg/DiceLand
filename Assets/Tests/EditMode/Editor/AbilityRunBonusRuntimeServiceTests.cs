using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class AbilityRunBonusRuntimeServiceTests
{
    private readonly List<Object> createdObjects = new List<Object>();

    [TearDown]
    public void TearDown()
    {
        for (int i = createdObjects.Count - 1; i >= 0; i--)
        {
            if (createdObjects[i] != null)
                Object.DestroyImmediate(createdObjects[i]);
        }

        createdObjects.Clear();
    }

    [Test]
    public void ResolveMerchantRefreshCost_AppliesFreeRefreshBeforeBaseCost()
    {
        RunState runState = new RunState { abilityMerchantFreeRefreshPerShop = 1 };
        BoardTileData tile = new BoardTileData { merchantRefreshCount = 0 };

        Assert.AreEqual(0, AbilityRunBonusRuntimeService.ResolveMerchantRefreshCost(runState, tile, 30));

        tile.merchantRefreshCount = 1;
        Assert.AreEqual(30, AbilityRunBonusRuntimeService.ResolveMerchantRefreshCost(runState, tile, 30));
    }

    [Test]
    public void ResolveMerchantPurchasePrice_ConsumesFirstHalfDiscountOnce()
    {
        RunState runState = new RunState { abilityFirstMerchantPurchaseHalfDiscountAvailable = true };

        int firstPrice = AbilityRunBonusRuntimeService.ResolveMerchantPurchasePrice(runState, 9);
        AbilityRunBonusRuntimeService.NotifyMerchantPurchaseCompleted(runState, firstPrice);
        int secondPrice = AbilityRunBonusRuntimeService.ResolveMerchantPurchasePrice(runState, 9);

        Assert.AreEqual(5, firstPrice);
        Assert.AreEqual(9, secondPrice);
    }

    [Test]
    public void ApplyStartingLootTableRolls_AddsLootFromConfiguredTables()
    {
        LootItem_SO loot = ScriptableObject.CreateInstance<LootItem_SO>();
        LootTable_SO table = ScriptableObject.CreateInstance<LootTable_SO>();
        SetPrivateField(table, "drops", new List<LootTable_SO.LootDropEntry>
        {
            new LootTable_SO.LootDropEntry
            {
                lootItem = loot,
                dropChance = 1f,
                minAmount = 2,
                maxAmount = 2
            }
        });
        RunState runState = new RunState();
        AbilityRunBonus bonus = new AbilityRunBonus();
        bonus.startingLootTableRolls.Add(new AbilityStartingLootTableRollBonus { lootTable = table, rollCount = 2 });

        AbilityRunBonusRuntimeService.ApplyStartingLootTableRolls(runState, bonus, 123);

        Assert.AreEqual(4, runState.GetLootAmount(loot));
        Object.DestroyImmediate(loot);
        Object.DestroyImmediate(table);
    }

    [Test]
    public void ApplyStartingBlueprints_RollsBlueprintsFromConfiguredTable()
    {
        CraftingBlueprint_SO first = ScriptableObject.CreateInstance<CraftingBlueprint_SO>();
        CraftingBlueprint_SO second = ScriptableObject.CreateInstance<CraftingBlueprint_SO>();
        BlueprintTable_SO table = ScriptableObject.CreateInstance<BlueprintTable_SO>();
        SetPrivateField(table, "blueprints", new List<CraftingBlueprint_SO> { first, second });
        RunState runState = new RunState();
        AbilityRunBonus bonus = new AbilityRunBonus();
        bonus.startingBlueprints.Add(new AbilityStartingBlueprintBonus { blueprintTable = table, rollCount = 3 });

        AbilityRunBonusRuntimeService.ApplyStartingBlueprints(runState, bonus, 123);

        int totalBlueprints = runState.GetBlueprintAmount(first) + runState.GetBlueprintAmount(second);
        Assert.AreEqual(3, totalBlueprints);
        Object.DestroyImmediate(first);
        Object.DestroyImmediate(second);
        Object.DestroyImmediate(table);
    }

    [Test]
    public void ApplyPersistentRunBonuses_AppliesMaxHealthBonusThroughRunMaxHpService()
    {
        Calamity_SO calamity = ScriptableObject.CreateInstance<Calamity_SO>();
        calamity.builtInEffects = new List<CalamityBuiltInEffectEntry>
        {
            new CalamityBuiltInEffectEntry
            {
                effectType = CalamityBuiltInEffectType.AllyMaxHpPercentModifier,
                effectAmount = -30
            }
        };
        RunState runState = new RunState
        {
            partyMembers = new List<RunPartyMemberState>
            {
                new RunPartyMemberState
                {
                    memberId = "a",
                    preCalamityMaxHP = 16,
                    maxHP = 11,
                    currentHP = 8
                }
            },
            activeCalamities = new List<RunCalamityState>
            {
                new RunCalamityState { calamityData = calamity }
            }
        };
        AbilityRunBonus bonus = new AbilityRunBonus { battleStartMaxHealthFlat = 4 };

        AbilityRunBonusRuntimeService.ApplyPersistentRunBonuses(runState, bonus);

        Assert.AreEqual(4, runState.partyMembers[0].maxHPFlatBonus);
        Assert.AreEqual(20, runState.partyMembers[0].preCalamityMaxHP);
        Assert.AreEqual(14, runState.partyMembers[0].maxHP);
        Assert.AreEqual(11, runState.partyMembers[0].currentHP);
        Assert.AreEqual(0, runState.abilityBattleStartMaxHealthFlat);
    }

    [Test]
    public void BuildStartingRelicBagChoices_CreatesPendingRelicChoiceAndAppliesRefreshBonus()
    {
        Relic_SO first = CreateRelic("First", LootRarity.Rare);
        Relic_SO second = CreateRelic("Second", LootRarity.Rare);
        RelicTable_SO table = CreateRelicTable(first, second);
        RunState runState = new RunState { abilityRelicChoiceRefreshCount = 1 };
        AbilityRunBonus bonus = new AbilityRunBonus();
        bonus.startingRelicBags.Add(new AbilityStartingRelicBagBonus
        {
            relicTable = table,
            bagCount = 1,
            choiceCount = 2
        });

        List<BattleSettlementRelicChoiceData> choices = AbilityRunBonusRuntimeService.BuildStartingRelicBagChoices(runState, bonus, 123);

        Assert.AreEqual(1, choices.Count);
        Assert.IsTrue(choices[0].HasChoices);
        Assert.AreEqual(2, choices[0].choices.Count);
        Assert.AreEqual(1, choices[0].refreshesRemaining);
        Assert.AreEqual("初始遗物袋：选择 1 个遗物", choices[0].title);
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        target.GetType()
            .GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            .SetValue(target, value);
    }

    private Relic_SO CreateRelic(string name, LootRarity rarity)
    {
        Relic_SO relic = ScriptableObject.CreateInstance<Relic_SO>();
        relic.relicName = name;
        relic.rarity = rarity;
        createdObjects.Add(relic);
        return relic;
    }

    private RelicTable_SO CreateRelicTable(params Relic_SO[] relics)
    {
        RelicTable_SO table = ScriptableObject.CreateInstance<RelicTable_SO>();
        SetPrivateField(table, "relics", new List<Relic_SO>(relics));
        createdObjects.Add(table);
        return table;
    }
}
