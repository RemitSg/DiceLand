using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public sealed class MapTreasureRemasterTests
{
    private readonly List<Object> owned = new List<Object>();
    private MapTreasureOverlay overlay;
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

    [SetUp]
    public void SetUp()
    {
        GameObject host = new GameObject("TreasureRemasterTest"); owned.Add(host);
        overlay = host.AddComponent<MapTreasureOverlay>(); overlay.ConfigureForestPresentation();
    }

    [TearDown]
    public void TearDown()
    {
        for (int i = owned.Count - 1; i >= 0; i--) if (owned[i] != null) Object.DestroyImmediate(owned[i]);
        owned.Clear();
    }

    [Test]
    public void GeneratedOfferNamesAmountsAndBlueprintRemainDynamic()
    {
        var choices = TreasureRewardService.BuildChoices(Table(), 37);
        Show(choices);
        Assert.AreEqual(choices[0].moneyAmount + " 金币", Text("TreasureChoice_0", "ChoiceTitle"));
        Assert.AreEqual(choices[1].blueprint.DisplayName, Text("TreasureChoice_1", "ChoiceTitle"));
        Assert.AreSame(choices[1].blueprint.DisplayIcon, Find("TreasureChoice_1").Find("RewardIcon").GetComponent<Image>().sprite);
        Assert.AreEqual("数量 ×1", Text("TreasureChoice_1", "ChoiceDetail"));
        Assert.AreEqual("仅可领取一项奖励", Find("ChoiceRule").GetComponent<TMP_Text>().text);
    }

    [Test]
    public void WealthUsesExistingServiceOnceResolvesTileAndHiddenFiresOnce()
    {
        RunState state = new RunState(); state.money = 20;
        BoardTileData tile = Tile(); var choices = TreasureRewardService.BuildTileChoices(state, Table(), tile);
        int calls = 0, hidden = 0;
        overlay.Hidden += () => hidden++;
        overlay.Show(tile, choices, (BoardTileData t, TreasureRewardChoice choice, out TreasureRewardResult result) =>
        { calls++; result = TreasureRewardService.ApplyTileChoice(state, Table(), t, choice); return result.success; }, NoRelic);
        Button claim = Find("TreasureChoice_0").Find("ChooseButton").GetComponent<Button>();
        claim.onClick.Invoke(); claim.onClick.Invoke();
        Assert.AreEqual(1, calls); Assert.AreEqual(20 + choices[0].moneyAmount, state.money);
        Assert.IsTrue(tile.isResolved); Assert.IsTrue(tile.isVisited);
        Assert.IsTrue(Find("ContinueButton").gameObject.activeSelf);
        overlay.Hide(); overlay.Hide(); Assert.AreEqual(1, hidden); Assert.IsFalse(overlay.IsVisible);
    }

    [Test]
    public void FailedClaimKeepsTheChoicesAndCanRetry()
    {
        int calls = 0;
        overlay.Show(Tile(), new[] { Gold(127) }, (BoardTileData t, TreasureRewardChoice c, out TreasureRewardResult result) =>
        { calls++; result = new TreasureRewardResult { success = calls > 1, message = calls > 1 ? "已领取" : "暂时无法领取" }; return result.success; }, NoRelic);
        Button claim = Find("ChooseButton").GetComponent<Button>(); claim.onClick.Invoke();
        Assert.IsTrue(Find("CardRoot").gameObject.activeSelf);
        Assert.AreEqual("暂时无法领取", Find("Subtitle").GetComponent<TMP_Text>().text);
        claim.onClick.Invoke(); Assert.AreEqual(2, calls); Assert.IsFalse(Find("CardRoot").gameObject.activeSelf);
    }

    [Test]
    public void UnavailableAndStaleChoicesCannotReachTheClaimHandler()
    {
        int calls = 0; TreasureRewardChoice unavailable = Gold(73); unavailable.isAvailable = false; unavailable.unavailableReason = "无可用奖励";
        overlay.Show(Tile(), new[] { unavailable }, (BoardTileData t, TreasureRewardChoice c, out TreasureRewardResult result) =>
        { calls++; result = new TreasureRewardResult { success = true }; return true; }, NoRelic);
        Find("ChooseButton").GetComponent<Button>().onClick.Invoke();
        typeof(MapTreasureOverlay).GetMethod("OnChoiceClicked", Private).Invoke(overlay, new object[] { Gold(999) });
        Assert.AreEqual(0, calls); Assert.IsFalse(Find("ChooseButton").GetComponent<Button>().interactable);
        Assert.AreEqual("无可用奖励", Text("TreasureChoice_0", "ChoiceDetail"));
    }

    [Test]
    public void ClaimHandlerCannotReenterThroughItsOwnButton()
    {
        int calls = 0; Button claim = null;
        overlay.Show(Tile(), new[] { Gold(45) }, (BoardTileData t, TreasureRewardChoice c, out TreasureRewardResult result) =>
        { calls++; claim.onClick.Invoke(); result = new TreasureRewardResult { success = true }; return true; }, NoRelic);
        claim = Find("ChooseButton").GetComponent<Button>(); claim.onClick.Invoke(); Assert.AreEqual(1, calls);
    }

    [Test]
    public void BagUsesExistingTwoStageServiceAndResolvesOnlyAfterConfirmation()
    {
        RunState state = new RunState(); BoardTileData tile = Tile();
        var choices = TreasureRewardService.BuildTileChoices(state, Table(), tile);
        int selected = 0, confirmed = 0;
        overlay.Show(tile, choices, (BoardTileData t, TreasureRewardChoice c, out TreasureRewardResult result) =>
        { selected++; result = TreasureRewardService.ApplyTileChoice(state, Table(), t, c); return result.success; },
        (BoardTileData t, BattleSettlementRelicChoiceData bagData, out string message) =>
        { confirmed++; var result = TreasureRewardService.ApplyTileRelicChoice(state, t, bagData); message = result.message; return result.success; });
        Find("TreasureChoice_2").Find("ChooseButton").GetComponent<Button>().onClick.Invoke();
        Assert.AreEqual(1, selected); Assert.IsFalse(tile.isResolved);
        var data = (BattleSettlementRelicChoiceData)typeof(MapTreasureOverlay).GetField("activeRelicChoiceData", Private).GetValue(overlay);
        Button confirm = Find("ConfirmRelicChoiceButton").GetComponent<Button>(); confirm.onClick.Invoke(); Assert.AreEqual(0, confirmed);
        Find("TreasureRelicChoice_0").GetComponent<Button>().onClick.Invoke();
        Assert.AreEqual("已选择", Text("TreasureRelicChoice_0", "Label"));
        confirm.onClick.Invoke(); confirm.onClick.Invoke();
        Assert.AreEqual(1, confirmed); Assert.IsTrue(data.rewardApplied); Assert.IsTrue(tile.isResolved); Assert.IsTrue(tile.isVisited);
    }

    [Test]
    public void InitialRelicChoiceKeepsItsDirectConfirmationCallback()
    {
        var data = RelicChoices(); RunState state = new RunState(); int calls = 0;
        overlay.ShowRelicChoice(data, (BattleSettlementRelicChoiceData c, out string message) =>
        { calls++; var result = TreasureRewardService.ApplyRelicChoice(state, c); message = result.message; return result.success; });
        Find("TreasureRelicChoice_1").GetComponent<Button>().onClick.Invoke();
        Find("ConfirmRelicChoiceButton").GetComponent<Button>().onClick.Invoke();
        Assert.AreEqual(1, calls); Assert.IsTrue(data.rewardApplied); Assert.AreSame(data.choices[1], data.selectedChoice);
    }

    [Test]
    public void SpecialDungeonIconOverrideAndDirectRelicIdentityArePreserved()
    {
        Sprite icon = Icon(); Relic_SO relic = Relic("真实遗物", LootRarity.Rare); relic.icon = Icon();
        Show(new[] { new TreasureRewardChoice { rewardType = TreasureRewardType.Power, relic = relic, iconOverride = icon, isAvailable = true, specialDungeonOptionIndex = 0 } });
        Assert.AreSame(icon, Find("RewardIcon").GetComponent<Image>().sprite);
        Assert.AreEqual("真实遗物", Text("TreasureChoice_0", "ChoiceTitle"));
    }

    [Test]
    public void MoreThanThreeChoicesAreReachableByHorizontalScroll()
    {
        Show(Enumerable.Range(1, 6).Select(i => Gold(i * 10)).ToArray());
        ScrollRect scroll = Find("CardRoot").GetComponent<ScrollRect>();
        Assert.IsTrue(scroll.horizontal); Assert.IsFalse(scroll.vertical);
        Assert.Greater(scroll.content.sizeDelta.x, scroll.viewport.sizeDelta.x);
        Assert.NotNull(Find("TreasureChoice_5").Find("ChooseButton")); Assert.NotNull(scroll.viewport.GetComponent<RectMask2D>());
    }

    [Test]
    public void FullRelicDescriptionRemainsScrollableAndDoesNotSelectOnScroll()
    {
        var data = RelicChoices(); data.choices[0].description = string.Concat(Enumerable.Repeat("每回合开始时，恢复生命并获得护盾。", 35));
        overlay.ShowRelicChoice(data, (BattleSettlementRelicChoiceData c, out string m) => { m = "ok"; return true; });
        ScrollRect scroll = Find("TreasureRelicChoice_0").Find("DescriptionViewport").GetComponent<ScrollRect>();
        Assert.Greater(scroll.content.sizeDelta.y, scroll.viewport.sizeDelta.y * 2);
        scroll.verticalNormalizedPosition = 0; Assert.IsNull(data.selectedChoice);
        Assert.AreEqual(data.choices[0].description, Text("TreasureRelicChoice_0", "ChoiceDetail"));
    }

    [TestCase(1920, 1080, 0, 0)]
    [TestCase(2340, 1080, 110, 35)]
    [TestCase(1600, 1200, 0, 0)]
    [TestCase(1280, 720, 45, 20)]
    public void ViewportFitIncludesTheChestAndEveryCard(int width, int height, int insetX, int insetY)
    {
        Show(new[] { Gold(45), Gold(50), Gold(60) });
        Canvas canvas = overlay.GetComponentInChildren<Canvas>(); canvas.GetComponent<CanvasScaler>().enabled = false;
        canvas.renderMode = RenderMode.WorldSpace; RectTransform root = (RectTransform)canvas.transform;
        root.sizeDelta = new Vector2(1080f * width / height, 1080); root.localScale = Vector3.one;
        overlay.ApplyViewport(new Vector2(width, height), new Rect(insetX, insetY, width - 2 * insetX, height - 2 * insetY));
        Canvas.ForceUpdateCanvases(); RectTransform safe = (RectTransform)Find("SafeArea");
        foreach (string name in new[] { "TreasureHeader", "Panel" })
        {
            Vector3[] corners = new Vector3[4]; ((RectTransform)Find(name)).GetWorldCorners(corners);
            foreach (Vector3 point in corners)
            {
                Vector3 local = safe.InverseTransformPoint(point);
                Assert.GreaterOrEqual(local.x, safe.rect.xMin - 1); Assert.LessOrEqual(local.x, safe.rect.xMax + 1);
                Assert.GreaterOrEqual(local.y, safe.rect.yMin - 1); Assert.LessOrEqual(local.y, safe.rect.yMax + 1);
            }
        }
    }

    [Test]
    public void ThemeHasIndependentTextFreeArtworkAndAllFiveBagRarities()
    {
        MapTreasureArt theme = MapTreasureArt.Load(); Assert.NotNull(theme); Assert.NotNull(theme.card); Assert.NotNull(theme.header);
        Assert.NotNull(theme.coins); Assert.AreEqual(5, theme.relicBags.Length); Assert.AreEqual(5, theme.relicBags.Distinct().Count());
        Assert.IsTrue(theme.relicBags.All(s => s != null)); Assert.NotNull(theme.headingFont); Assert.NotNull(theme.bodyFont);
    }

    private static TreasureRewardTable_SO Table() => Resources.Load<TreasureRewardTable_SO>("DefaultTreasureRewardTable");
    private static BoardTileData Tile() => new BoardTileData { coord = Vector2Int.one, tileType = MapTileType.Treasure, encounterSeed = 12345 };
    private static TreasureRewardChoice Gold(int amount) => new TreasureRewardChoice { rewardType = TreasureRewardType.Wealth, moneyAmount = amount, title = "财富", isAvailable = true };
    private void Show(IReadOnlyList<TreasureRewardChoice> choices) => overlay.Show(Tile(), choices, (BoardTileData t, TreasureRewardChoice c, out TreasureRewardResult r) => { r = new TreasureRewardResult { success = true }; return true; }, NoRelic);
    private static bool NoRelic(BoardTileData t, BattleSettlementRelicChoiceData c, out string message) { message = "ok"; return true; }
    private Transform Find(string name) => overlay.GetComponentsInChildren<Transform>(true).First(t => t.name == name);
    private string Text(string card, string name) => Find(card).GetComponentsInChildren<TMP_Text>(true).First(t => t.name == name).text;
    private Relic_SO Relic(string name, LootRarity rarity) { var relic = ScriptableObject.CreateInstance<Relic_SO>(); relic.relicName = name; relic.rarity = rarity; owned.Add(relic); return relic; }
    private BattleSettlementRelicChoiceData RelicChoices() => BattleSettlementRelicChoiceBuilder.Build(new[] { Relic("琥珀核心", LootRarity.Epic), Relic("银牙", LootRarity.Epic) });
    private Sprite Icon() { var texture = new Texture2D(4, 4); var sprite = Sprite.Create(texture, new Rect(0, 0, 4, 4), Vector2.one * 0.5f); owned.Add(sprite); owned.Add(texture); return sprite; }
}
