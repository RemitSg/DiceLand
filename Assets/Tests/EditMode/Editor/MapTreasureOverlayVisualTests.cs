using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public class MapTreasureOverlayVisualTests
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
    public void Show_AppliesConfiguredPanelAndOptionBackgroundSprites()
    {
        MapTreasureOverlay overlay = CreateOverlay();
        Sprite panelBackground = CreateSprite("PanelBackground");
        Sprite optionBackground = CreateSprite("OptionBackground");

        overlay.ConfigureBackgroundSprites(panelBackground, optionBackground);
        overlay.Show(CreateTreasureTile(), new[]
        {
            new TreasureRewardChoice
            {
                rewardType = TreasureRewardType.Wealth,
                title = "Gold",
                description = "Gain gold.",
                isAvailable = true
            }
        }, AlwaysSelect, AlwaysConfirmRelicChoice);

        Assert.AreSame(panelBackground, FindChild(overlay.transform, "Panel").GetComponent<Image>().sprite);
        Assert.AreSame(optionBackground, FindChild(overlay.transform, "TreasureChoice_0").GetComponent<Image>().sprite);
    }

    [Test]
    public void Show_UsesConfiguredAndBlueprintRewardIcons()
    {
        MapTreasureOverlay overlay = CreateOverlay();
        Sprite moneyIcon = CreateSprite("MoneyIcon");
        Sprite blueprintIcon = CreateSprite("BlueprintIcon");
        Sprite relicBagIcon = CreateSprite("RelicBagIcon");
        CraftingBlueprint_SO blueprint = ScriptableObject.CreateInstance<CraftingBlueprint_SO>();
        blueprint.icon = blueprintIcon;
        blueprint.blueprintName = "Blade Scroll";
        createdObjects.Add(blueprint);

        overlay.ConfigureRewardIcons(moneyIcon, relicBagIcon);
        overlay.Show(CreateTreasureTile(), new[]
        {
            new TreasureRewardChoice
            {
                rewardType = TreasureRewardType.Wealth,
                title = "Gold",
                description = "Gain gold.",
                isAvailable = true
            },
            new TreasureRewardChoice
            {
                rewardType = TreasureRewardType.Knowledge,
                title = "Blueprint",
                description = "Gain a blueprint.",
                isAvailable = true,
                blueprint = blueprint
            },
            new TreasureRewardChoice
            {
                rewardType = TreasureRewardType.Power,
                title = "Relic Bag",
                description = "Gain a relic bag.",
                isAvailable = true
            }
        }, AlwaysSelect, AlwaysConfirmRelicChoice);

        Assert.AreSame(moneyIcon, FindChild(FindChild(overlay.transform, "TreasureChoice_0"), "RewardIcon").GetComponent<Image>().sprite);
        Assert.AreSame(blueprintIcon, FindChild(FindChild(overlay.transform, "TreasureChoice_1"), "RewardIcon").GetComponent<Image>().sprite);
        Assert.AreSame(relicBagIcon, FindChild(FindChild(overlay.transform, "TreasureChoice_2"), "RewardIcon").GetComponent<Image>().sprite);
    }

    [Test]
    public void Show_UsesRelicBagIconForChoiceRarity()
    {
        MapTreasureOverlay overlay = CreateOverlay();
        Sprite commonIcon = CreateSprite("CommonBagIcon");
        Sprite uncommonIcon = CreateSprite("UncommonBagIcon");
        Sprite rareIcon = CreateSprite("RareBagIcon");
        Sprite epicIcon = CreateSprite("EpicBagIcon");
        Sprite legendaryIcon = CreateSprite("LegendaryBagIcon");

        overlay.ConfigureRelicBagIcons(commonIcon, uncommonIcon, rareIcon, epicIcon, legendaryIcon);
        overlay.Show(CreateTreasureTile(), new[]
        {
            CreateRelicBagChoice(LootRarity.Common),
            CreateRelicBagChoice(LootRarity.Uncommon),
            CreateRelicBagChoice(LootRarity.Legendary)
        }, AlwaysSelect, AlwaysConfirmRelicChoice);

        Assert.AreSame(commonIcon, FindChild(FindChild(overlay.transform, "TreasureChoice_0"), "RewardIcon").GetComponent<Image>().sprite);
        Assert.AreSame(uncommonIcon, FindChild(FindChild(overlay.transform, "TreasureChoice_1"), "RewardIcon").GetComponent<Image>().sprite);
        Assert.AreSame(legendaryIcon, FindChild(FindChild(overlay.transform, "TreasureChoice_2"), "RewardIcon").GetComponent<Image>().sprite);

        overlay.Show(CreateTreasureTile(), new[]
        {
            CreateRelicBagChoice(LootRarity.Rare),
            CreateRelicBagChoice(LootRarity.Epic)
        }, AlwaysSelect, AlwaysConfirmRelicChoice);

        Assert.AreSame(rareIcon, FindChild(FindChild(overlay.transform, "TreasureChoice_0"), "RewardIcon").GetComponent<Image>().sprite);
        Assert.AreSame(epicIcon, FindChild(FindChild(overlay.transform, "TreasureChoice_1"), "RewardIcon").GetComponent<Image>().sprite);
    }

    [Test]
    public void Show_OpensRelicChoiceCardsAndConfirmsSelectedRelic()
    {
        MapTreasureOverlay overlay = CreateOverlay();
        Relic_SO firstRelic = CreateRelic("Amber Core", LootRarity.Epic);
        Relic_SO secondRelic = CreateRelic("Silver Fang", LootRarity.Epic);
        BattleSettlementRelicChoiceData choiceData = BattleSettlementRelicChoiceBuilder.Build(new[] { firstRelic, secondRelic });
        bool confirmed = false;

        overlay.Show(CreateTreasureTile(), new[] { CreateRelicBagChoice(LootRarity.Epic) },
            (BoardTileData tile, TreasureRewardChoice choice, out TreasureRewardResult result) =>
            {
                result = new TreasureRewardResult
                {
                    success = true,
                    message = "打开遗物袋。",
                    relicChoiceData = choiceData
                };
                return true;
            },
            (BoardTileData tile, BattleSettlementRelicChoiceData relicChoiceData, out string message) =>
            {
                confirmed = true;
                message = "获得遗物。";
                return true;
            });

        FindChild(FindChild(overlay.transform, "TreasureChoice_0"), "ChooseButton").GetComponent<Button>().onClick.Invoke();
        Assert.NotNull(FindChild(overlay.transform, "TreasureRelicChoice_0"));

        FindChild(overlay.transform, "ConfirmRelicChoiceButton").GetComponent<Button>().onClick.Invoke();
        Assert.IsFalse(confirmed);

        FindChild(overlay.transform, "TreasureRelicChoice_0").GetComponent<Button>().onClick.Invoke();
        FindChild(overlay.transform, "ConfirmRelicChoiceButton").GetComponent<Button>().onClick.Invoke();

        Assert.IsTrue(confirmed);
        Assert.AreSame(choiceData.choices[0], choiceData.selectedChoice);
    }

    private MapTreasureOverlay CreateOverlay()
    {
        GameObject go = new GameObject("TreasureOverlay");
        createdObjects.Add(go);
        return go.AddComponent<MapTreasureOverlay>();
    }

    [Test]
    public void RelicRefreshButtonUsesTheSharedPoolClearsSelectionAndRespectsTheLimit()
    {
        MapTreasureOverlay overlay = CreateOverlay();
        Relic_SO[] pool = { CreateRelic("A", LootRarity.Common), CreateRelic("B", LootRarity.Common),
            CreateRelic("C", LootRarity.Common), CreateRelic("D", LootRarity.Common) };
        Assert.IsTrue(BattleEliteRelicChoiceBuilderService.TryBuildFromRelicsWithRarity(out var choice,
            pool, LootRarity.Common, new System.Random(77), null, 2));
        choice.refreshesRemaining = 1;
        overlay.ShowRelicChoice(choice, (BattleSettlementRelicChoiceData data, out string message) => { message = "chosen"; return true; });
        Button refresh = FindChild(overlay.transform, "RefreshRelicChoiceButton").GetComponent<Button>();
        Button confirm = FindChild(overlay.transform, "ConfirmRelicChoiceButton").GetComponent<Button>();
        Assert.IsTrue(refresh.gameObject.activeSelf); Assert.IsTrue(refresh.interactable);
        Assert.Less(confirm.GetComponent<RectTransform>().anchoredPosition.x, refresh.GetComponent<RectTransform>().anchoredPosition.x);
        Relic_SO first = choice.choices[0].relicData, second = choice.choices[1].relicData;
        FindChild(overlay.transform, "TreasureRelicChoice_0").GetComponent<Button>().onClick.Invoke();
        Assert.NotNull(choice.selectedChoice);
        refresh.onClick.Invoke();
        Assert.IsNull(choice.selectedChoice); Assert.AreEqual(0, choice.refreshesRemaining);
        Assert.IsFalse(refresh.gameObject.activeSelf); Assert.AreEqual(0f, confirm.GetComponent<RectTransform>().anchoredPosition.x);
        foreach (var entry in choice.choices) { Assert.AreNotSame(first, entry.relicData); Assert.AreNotSame(second, entry.relicData); }
    }

    private BoardTileData CreateTreasureTile()
    {
        return new BoardTileData
        {
            coord = new Vector2Int(1, 1),
            tileType = MapTileType.Treasure,
            encounterSeed = 12345
        };
    }

    private Sprite CreateSprite(string name)
    {
        Texture2D texture = new Texture2D(2, 2);
        texture.name = $"{name}Texture";
        Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, 2f, 2f), new Vector2(0.5f, 0.5f));
        sprite.name = name;
        createdObjects.Add(sprite);
        createdObjects.Add(texture);
        return sprite;
    }

    private Relic_SO CreateRelic(string name, LootRarity rarity)
    {
        Relic_SO relic = ScriptableObject.CreateInstance<Relic_SO>();
        relic.relicName = name;
        relic.rarity = rarity;
        createdObjects.Add(relic);
        return relic;
    }

    private static TreasureRewardChoice CreateRelicBagChoice(LootRarity rarity)
    {
        return new TreasureRewardChoice
        {
            rewardType = TreasureRewardType.Power,
            title = $"{rarity} Relic Bag",
            description = "Gain a relic bag.",
            isAvailable = true,
            relicBagRarity = rarity
        };
    }

    private static bool AlwaysSelect(BoardTileData tile, TreasureRewardChoice choice, out TreasureRewardResult result)
    {
        result = new TreasureRewardResult
        {
            success = true,
            message = string.Empty
        };
        return true;
    }

    private static bool AlwaysConfirmRelicChoice(BoardTileData tile, BattleSettlementRelicChoiceData choiceData, out string message)
    {
        message = string.Empty;
        return true;
    }

    private static Transform FindChild(Transform root, string name)
    {
        Transform[] children = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i].name == name)
                return children[i];
        }

        Assert.Fail($"Missing child named {name}.");
        return null;
    }
}
