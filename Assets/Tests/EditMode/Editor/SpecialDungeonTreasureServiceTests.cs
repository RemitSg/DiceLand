using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

public class SpecialDungeonTreasureServiceTests
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
    public void BuildChoices_ConvertsConfiguredOptionsAndPreservesOptionIndex()
    {
        Sprite icon = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f), Vector2.zero);
        createdObjects.Add(icon);
        CraftingBlueprint_SO blueprint = CreateBlueprint("Molten Scroll", LootRarity.Rare);
        Relic_SO relic = CreateRelic("Amber Core", LootRarity.Epic);
        BoardTileData tile = new BoardTileData { tileType = MapTileType.Treasure };
        tile.specialDungeonTreasureOptions.Add(new SpecialDungeonTreasureOptionEntry
        {
            title = "Coins",
            description = "A fixed pouch.",
            rewardType = SpecialDungeonTreasureRewardType.Money,
            moneyAmount = 12,
            iconOverride = icon
        });
        tile.specialDungeonTreasureOptions.Add(new SpecialDungeonTreasureOptionEntry
        {
            title = "Formula",
            rewardType = SpecialDungeonTreasureRewardType.Blueprint,
            blueprintData = blueprint
        });
        tile.specialDungeonTreasureOptions.Add(new SpecialDungeonTreasureOptionEntry
        {
            title = "Relic",
            rewardType = SpecialDungeonTreasureRewardType.Relic,
            relicData = relic
        });

        IReadOnlyList<TreasureRewardChoice> choices = SpecialDungeonTreasureService.BuildChoices(tile);

        Assert.That(choices, Has.Count.EqualTo(3));
        Assert.AreEqual(TreasureRewardType.Wealth, choices[0].rewardType);
        Assert.AreEqual("Coins", choices[0].title);
        Assert.AreEqual("A fixed pouch.", choices[0].description);
        Assert.AreEqual(12, choices[0].moneyAmount);
        Assert.AreEqual(0, choices[0].specialDungeonOptionIndex);
        Assert.AreSame(icon, choices[0].iconOverride);
        Assert.AreEqual(TreasureRewardType.Knowledge, choices[1].rewardType);
        Assert.AreSame(blueprint, choices[1].blueprint);
        Assert.AreEqual(1, choices[1].specialDungeonOptionIndex);
        Assert.AreEqual(TreasureRewardType.Power, choices[2].rewardType);
        Assert.AreSame(relic, choices[2].relic);
        Assert.AreEqual(2, choices[2].specialDungeonOptionIndex);
    }

    [Test]
    public void ResolveOption_UsesOriginalTileOptionIndex()
    {
        SpecialDungeonTreasureOptionEntry first = new SpecialDungeonTreasureOptionEntry
        {
            rewardType = SpecialDungeonTreasureRewardType.Money,
            moneyAmount = 3
        };
        SpecialDungeonTreasureOptionEntry second = new SpecialDungeonTreasureOptionEntry
        {
            rewardType = SpecialDungeonTreasureRewardType.Money,
            moneyAmount = 7
        };
        BoardTileData tile = new BoardTileData { tileType = MapTileType.Treasure };
        tile.specialDungeonTreasureOptions.Add(first);
        tile.specialDungeonTreasureOptions.Add(second);

        SpecialDungeonTreasureOptionEntry resolved = SpecialDungeonTreasureService.ResolveOption(
            tile,
            new TreasureRewardChoice { specialDungeonOptionIndex = 1 });

        Assert.AreSame(second, resolved);
    }

    [Test]
    public void ApplyChoice_AppliesMoneyBlueprintAndFixedRelicRewards()
    {
        CraftingBlueprint_SO blueprint = CreateBlueprint("Molten Scroll", LootRarity.Rare);
        Relic_SO relic = CreateRelic("Amber Core", LootRarity.Epic);
        RunState runState = new RunState();

        TreasureRewardResult moneyResult = SpecialDungeonTreasureService.ApplyChoice(
            runState,
            new SpecialDungeonTreasureOptionEntry
            {
                rewardType = SpecialDungeonTreasureRewardType.Money,
                moneyAmount = 25
            },
            10);
        TreasureRewardResult blueprintResult = SpecialDungeonTreasureService.ApplyChoice(
            runState,
            new SpecialDungeonTreasureOptionEntry
            {
                rewardType = SpecialDungeonTreasureRewardType.Blueprint,
                blueprintData = blueprint
            },
            10);
        TreasureRewardResult relicResult = SpecialDungeonTreasureService.ApplyChoice(
            runState,
            new SpecialDungeonTreasureOptionEntry
            {
                rewardType = SpecialDungeonTreasureRewardType.Relic,
                relicData = relic
            },
            10);

        Assert.IsTrue(moneyResult.success);
        Assert.AreEqual(25, runState.money);
        Assert.IsTrue(blueprintResult.success);
        Assert.AreEqual(1, runState.GetBlueprintAmount(blueprint));
        Assert.IsTrue(relicResult.success);
        Assert.AreEqual(1, runState.GetRelicAmount(relic));
        Assert.AreSame(relic, relicResult.relic);
    }

    [Test]
    public void ApplyChoice_RelicBagBuildsChoiceDataWithoutGrantingRelicImmediately()
    {
        Relic_SO common = CreateRelic("Common Relic", LootRarity.Common);
        Relic_SO firstRare = CreateRelic("Rare A", LootRarity.Rare);
        Relic_SO secondRare = CreateRelic("Rare B", LootRarity.Rare);
        Relic_SO thirdRare = CreateRelic("Rare C", LootRarity.Rare);
        RelicTable_SO relicTable = CreateRelicTable(common, firstRare, secondRare, thirdRare);
        RunState runState = new RunState();
        SpecialDungeonTreasureOptionEntry option = new SpecialDungeonTreasureOptionEntry
        {
            rewardType = SpecialDungeonTreasureRewardType.RelicBag,
            relicTable = relicTable,
            relicBagRarity = LootRarity.Rare,
            relicBagChoiceCount = 3
        };

        TreasureRewardResult result = SpecialDungeonTreasureService.ApplyChoice(runState, option, 1234);

        Assert.IsTrue(result.success);
        Assert.NotNull(result.relicChoiceData);
        Assert.AreEqual(3, result.relicChoiceData.choices.Count);
        Assert.IsTrue(result.relicChoiceData.choices.TrueForAll(choice => choice.rarity == LootRarity.Rare));
        Assert.AreEqual(0, runState.GetRelicAmount(firstRare));
        Assert.That(result.message, Does.Contain("\u9057\u7269\u888b"));
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

    private RelicTable_SO CreateRelicTable(params Relic_SO[] relics)
    {
        RelicTable_SO table = ScriptableObject.CreateInstance<RelicTable_SO>();
        SetPrivateField(table, "relics", new List<Relic_SO>(relics));
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
