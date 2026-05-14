using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

public class MapTreasureRewardServiceTests
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
    public void BuildChoices_PreRollsMoneyBlueprintAndRelicBagRarity()
    {
        CraftingBlueprint_SO blueprint = CreateBlueprint("Molten Scroll", LootRarity.Rare);
        Relic_SO relic = CreateRelic("Amber Core", LootRarity.Epic);
        TreasureRewardTable_SO rewardTable = CreateTreasureTable(
            CreateBlueprintTable(blueprint),
            CreateRarityWeightTable(new LootRarityWeightTable_SO.RarityWeightEntry { rarity = LootRarity.Rare, weight = 1f }),
            CreateRelicTable(relic),
            CreateRarityWeightTable(new LootRarityWeightTable_SO.RarityWeightEntry { rarity = LootRarity.Epic, weight = 1f }),
            30,
            50);

        IReadOnlyList<TreasureRewardChoice> choices = TreasureRewardService.BuildChoices(rewardTable, 9876);

        Assert.AreEqual(3, choices.Count);
        Assert.AreEqual(TreasureRewardType.Wealth, choices[0].rewardType);
        Assert.That(choices[0].moneyAmount, Is.InRange(30, 50));
        Assert.AreEqual(TreasureRewardType.Knowledge, choices[1].rewardType);
        Assert.AreSame(blueprint, choices[1].blueprint);
        Assert.AreEqual(TreasureRewardType.Power, choices[2].rewardType);
        Assert.AreEqual(LootRarity.Epic, choices[2].relicBagRarity);
        Assert.IsNull(choices[2].relic);
    }

    [Test]
    public void ApplyChoice_GrantsMoneyAndBlueprintButOpensRelicBagChoice()
    {
        CraftingBlueprint_SO blueprint = CreateBlueprint("Molten Scroll", LootRarity.Rare);
        Relic_SO relicA = CreateRelic("Amber Core", LootRarity.Epic);
        Relic_SO relicB = CreateRelic("Silver Fang", LootRarity.Epic);
        Relic_SO relicC = CreateRelic("Storm Eye", LootRarity.Epic);
        TreasureRewardTable_SO rewardTable = CreateTreasureTable(
            CreateBlueprintTable(blueprint),
            CreateRarityWeightTable(new LootRarityWeightTable_SO.RarityWeightEntry { rarity = LootRarity.Rare, weight = 1f }),
            CreateRelicTable(relicA, relicB, relicC),
            CreateRarityWeightTable(new LootRarityWeightTable_SO.RarityWeightEntry { rarity = LootRarity.Epic, weight = 1f }),
            37,
            37);
        IReadOnlyList<TreasureRewardChoice> choices = TreasureRewardService.BuildChoices(rewardTable, 2468);
        RunState runState = new RunState();

        TreasureRewardResult moneyResult = TreasureRewardService.ApplyChoice(runState, rewardTable, choices[0], 2468);
        TreasureRewardResult blueprintResult = TreasureRewardService.ApplyChoice(runState, rewardTable, choices[1], 2468);
        TreasureRewardResult relicResult = TreasureRewardService.ApplyChoice(runState, rewardTable, choices[2], 2468);

        Assert.IsTrue(moneyResult.success);
        Assert.AreEqual(37, runState.money);
        Assert.IsTrue(blueprintResult.success);
        Assert.AreEqual(1, runState.GetBlueprintAmount(blueprint));
        Assert.IsTrue(relicResult.success);
        Assert.NotNull(relicResult.relicChoiceData);
        Assert.AreEqual(3, relicResult.relicChoiceData.choices.Count);
        Assert.AreEqual(0, runState.GetRelicAmount(relicA));
        Assert.AreEqual(0, runState.GetRelicAmount(relicB));
        Assert.AreEqual(0, runState.GetRelicAmount(relicC));
        Assert.That(relicResult.message, Does.Contain("\u6253\u5f00<color=#A35CFF>\u53f2\u8bd7</color>\u9057\u7269\u888b"));
    }

    [Test]
    public void RarityLabels_UseTreasureChineseNamesAndColors()
    {
        Assert.AreEqual("\u5e38\u89c1", TreasureRewardService.GetRarityLabel(LootRarity.Common));
        Assert.AreEqual("\u7f55\u89c1", TreasureRewardService.GetRarityLabel(LootRarity.Uncommon));
        Assert.AreEqual("\u7a00\u6709", TreasureRewardService.GetRarityLabel(LootRarity.Rare));
        Assert.AreEqual("\u53f2\u8bd7", TreasureRewardService.GetRarityLabel(LootRarity.Epic));
        Assert.AreEqual("\u4f20\u5947", TreasureRewardService.GetRarityLabel(LootRarity.Legendary));

        Assert.AreEqual("<color=#808080>\u5e38\u89c1</color>", TreasureRewardService.GetColoredRarityLabel(LootRarity.Common));
        Assert.AreEqual("<color=#38B256>\u7f55\u89c1</color>", TreasureRewardService.GetColoredRarityLabel(LootRarity.Uncommon));
        Assert.AreEqual("<color=#407EFF>\u7a00\u6709</color>", TreasureRewardService.GetColoredRarityLabel(LootRarity.Rare));
        Assert.AreEqual("<color=#A35CFF>\u53f2\u8bd7</color>", TreasureRewardService.GetColoredRarityLabel(LootRarity.Epic));
        Assert.AreEqual("<color=#E6B42C>\u4f20\u5947</color>", TreasureRewardService.GetColoredRarityLabel(LootRarity.Legendary));
    }

    [Test]
    public void BuildChoices_ColorsRelicBagRarityInDescription()
    {
        Relic_SO relic = CreateRelic("Amber Core", LootRarity.Legendary);
        TreasureRewardTable_SO rewardTable = CreateTreasureTable(
            CreateBlueprintTable(CreateBlueprint("Molten Scroll", LootRarity.Rare)),
            CreateRarityWeightTable(new LootRarityWeightTable_SO.RarityWeightEntry { rarity = LootRarity.Rare, weight = 1f }),
            CreateRelicTable(relic),
            CreateRarityWeightTable(new LootRarityWeightTable_SO.RarityWeightEntry { rarity = LootRarity.Legendary, weight = 1f }),
            30,
            50);

        IReadOnlyList<TreasureRewardChoice> choices = TreasureRewardService.BuildChoices(rewardTable, 9876);

        Assert.That(choices[2].description, Does.Contain("\u83b7\u5f97 1 \u4e2a<color=#E6B42C>\u4f20\u5947</color>\u9057\u7269\u888b"));
    }

    [Test]
    public void ApplyRelicChoice_GrantsSelectedRelicAfterBagChoice()
    {
        Relic_SO selectedRelic = CreateRelic("Amber Core", LootRarity.Epic);
        RunState runState = new RunState();
        BattleSettlementLootEntry entry = new BattleSettlementLootEntry
        {
            relicData = selectedRelic,
            amount = 1,
            displayName = selectedRelic.DisplayName,
            rarity = selectedRelic.rarity,
            isRelicChoiceOption = true
        };
        BattleSettlementRelicChoiceData choiceData = new BattleSettlementRelicChoiceData
        {
            selectedChoice = entry,
            choices = new List<BattleSettlementLootEntry> { entry }
        };

        TreasureRewardResult result = TreasureRewardService.ApplyRelicChoice(runState, choiceData);

        Assert.IsTrue(result.success);
        Assert.AreEqual(1, runState.GetRelicAmount(selectedRelic));
        Assert.AreSame(selectedRelic, result.relic);
        Assert.IsTrue(choiceData.rewardApplied);
    }

    private CraftingBlueprint_SO CreateBlueprint(string name, LootRarity rarity)
    {
        CraftingBlueprint_SO blueprint = ScriptableObject.CreateInstance<CraftingBlueprint_SO>();
        blueprint.blueprintName = name;
        blueprint.rarity = rarity;
        createdObjects.Add(blueprint);
        return blueprint;
    }

    private Relic_SO CreateRelic(string name, LootRarity rarity)
    {
        Relic_SO relic = ScriptableObject.CreateInstance<Relic_SO>();
        relic.relicName = name;
        relic.rarity = rarity;
        createdObjects.Add(relic);
        return relic;
    }

    private BlueprintTable_SO CreateBlueprintTable(params CraftingBlueprint_SO[] blueprints)
    {
        BlueprintTable_SO table = ScriptableObject.CreateInstance<BlueprintTable_SO>();
        SetPrivateField(table, "blueprints", new List<CraftingBlueprint_SO>(blueprints));
        createdObjects.Add(table);
        return table;
    }

    private RelicTable_SO CreateRelicTable(params Relic_SO[] relics)
    {
        RelicTable_SO table = ScriptableObject.CreateInstance<RelicTable_SO>();
        SetPrivateField(table, "relics", new List<Relic_SO>(relics));
        createdObjects.Add(table);
        return table;
    }

    private LootRarityWeightTable_SO CreateRarityWeightTable(params LootRarityWeightTable_SO.RarityWeightEntry[] weights)
    {
        LootRarityWeightTable_SO table = ScriptableObject.CreateInstance<LootRarityWeightTable_SO>();
        SetPrivateField(table, "rarityWeights", new List<LootRarityWeightTable_SO.RarityWeightEntry>(weights));
        createdObjects.Add(table);
        return table;
    }

    private TreasureRewardTable_SO CreateTreasureTable(
        BlueprintTable_SO blueprintTable,
        LootRarityWeightTable_SO blueprintRarityWeightTable,
        RelicTable_SO relicTable,
        LootRarityWeightTable_SO relicBagRarityWeightTable,
        int minMoney,
        int maxMoney)
    {
        TreasureRewardTable_SO table = ScriptableObject.CreateInstance<TreasureRewardTable_SO>();
        SetPrivateField(table, "blueprintTable", blueprintTable);
        SetPrivateField(table, "blueprintRarityWeightTable", blueprintRarityWeightTable);
        SetPrivateField(table, "relicTable", relicTable);
        SetPrivateField(table, "relicBagRarityWeightTable", relicBagRarityWeightTable);
        SetPrivateField(table, "minMoney", minMoney);
        SetPrivateField(table, "maxMoney", maxMoney);
        createdObjects.Add(table);
        return table;
    }

    private static void SetPrivateField<T>(object target, string fieldName, T value)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field, $"Missing field {fieldName} on {target.GetType().Name}.");
        field.SetValue(target, value);
    }
}
