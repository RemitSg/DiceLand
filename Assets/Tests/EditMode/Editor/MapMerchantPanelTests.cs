using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public sealed class MapMerchantPanelTests
{
    private GameObject host;
    private MapMerchantOverlay overlay;
    private RunState state;
    private BoardTileData tile;
    private readonly List<Object> owned = new List<Object>();
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

    [SetUp]
    public void SetUp()
    {
        host = new GameObject("MerchantPanelTest");
        overlay = host.AddComponent<MapMerchantOverlay>();
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath("c6e7143cd2df45b45a784ef8c61a210f"));
        Assert.IsNotNull(font);
        overlay.ConfigureFonts(font, font);
        state = new RunState { money = 250 };
        tile = new BoardTileData { tileType = MapTileType.Merchant, merchantStockGenerated = true };
        for (int i = 0; i < 6; i++) tile.merchantOffers.Add(Offer(MerchantItemType.Loot, i));
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(host);
        foreach (Object item in owned) Object.DestroyImmediate(item);
        owned.Clear();
    }

    [TestCase(2340, 1080, 0, 0)]
    [TestCase(1920, 1080, 0, 0)]
    [TestCase(1600, 1200, 0, 0)]
    [TestCase(2400, 1080, 140, 50)]
    public void SixCardsAndPurchaseRemainInsideSafeAreaWithNoOverlaps(int width, int height, int left, int right)
    {
        Show(); Layout(width, height, new Rect(left, 24, width - left - right, height - 48));
        Contains(Bounds(Find("SafeArea")), Bounds(Find("Panel")));
        Contains(Bounds(Find("Panel")), Bounds(Find("DetailRoot")));
        Contains(Bounds(Find("Panel")), Bounds(Find("RefreshButton")));
        Contains(Bounds(Find("DetailRoot")), Bounds(Find("BuyButton")));
        Assert.IsFalse(Bounds(Find("OfferScrollRect")).Overlaps(Bounds(Find("DetailRoot"))));
        Assert.IsFalse(Bounds(Find("DetailScrollRect")).Overlaps(Bounds(Find("PurchaseSummary"))));
        Transform[] cards = Cards(); Assert.AreEqual(6, cards.Length);
        for (int i = 0; i < cards.Length; i++)
        {
            Contains(Bounds(Get<ScrollRect>("offerScrollRect").viewport), Bounds(cards[i]));
            Contains(Bounds(cards[i]), Bounds(cards[i].Find("IconWell")));
            Contains(Bounds(cards[i].Find("IconWell")), Bounds(cards[i].Find("Icon")));
            Assert.IsFalse(Bounds(cards[i].Find("IconWell")).Overlaps(Bounds(cards[i].Find("Name"))));
            Contains(Bounds(cards[i].Find("MetaBadge")), Bounds(cards[i].Find("Meta")));
            Assert.IsFalse(Bounds(cards[i].Find("MetaBadge")).Overlaps(Bounds(cards[i].Find("Price"))));
            Assert.IsFalse(cards[i].Find("IconWell").GetComponent<Graphic>().raycastTarget);
            Assert.IsFalse(cards[i].Find("MetaBadge").GetComponent<Graphic>().raycastTarget);
            for (int j = i + 1; j < cards.Length; j++) Assert.IsFalse(Bounds(cards[i]).Overlaps(Bounds(cards[j])));
        }
        Contains(Bounds(Find("DetailScrollRect")), Bounds(Get<RectTransform>("detailIconWell")));
        Contains(Bounds(Get<RectTransform>("detailIconWell")), Bounds(Get<Image>("iconImage").rectTransform));
        Assert.IsFalse(Bounds(Get<RectTransform>("detailIconWell")).Overlaps(Bounds(Get<TMP_Text>("goodsNameText").rectTransform)));
        Assert.Greater(Bounds(cards[1]).xMin, Bounds(cards[0]).xMax);
        Assert.IsFalse(Get<ScrollRect>("offerScrollRect").verticalScrollbar.gameObject.activeSelf);
        Assert.IsFalse(Get<ScrollRect>("detailScrollRect").verticalScrollbar.gameObject.activeSelf);
    }

    public static IEnumerable<TestCaseData> RarityCases()
    {
        string[] colors = { "808080", "38B256", "407EFF", "A35CFF", "E6B42C", "7E5330" };
        foreach (MerchantItemType type in Enum.GetValues(typeof(MerchantItemType)))
            for (int rarity = 0; rarity < (type == MerchantItemType.Weapon ? 5 : 6); rarity++)
                yield return new TestCaseData(type, rarity, colors[rarity]).SetName($"NameColour_{type}_{rarity}_SurvivesSelectionAndSoldOut");
    }

    [TestCaseSource(nameof(RarityCases))]
    public void AllItemTypesUseActualRarityForBothNamesAcrossSelectionAndSoldOut(MerchantItemType type, int rarity, string hex)
    {
        MerchantOfferState offer = Offer(type, rarity);
        tile.merchantOffers = new List<MerchantOfferState> { offer, Offer(MerchantItemType.Loot, 0) };
        Show(); Layout();
        ColorUtility.TryParseHtmlString("#" + hex, out Color expected);
        TMP_Text cardName = Cards()[0].Find("Name").GetComponent<TMP_Text>();
        Assert.AreEqual(expected, cardName.color);
        Assert.AreEqual(expected, Get<TMP_Text>("goodsNameText").color);
        Cards()[1].GetComponent<Button>().onClick.Invoke();
        Assert.AreEqual(expected, cardName.color);
        Cards()[0].GetComponent<Button>().onClick.Invoke();
        offer.isSoldOut = true;
        Show(); Layout();
        cardName = Cards()[0].Find("Name").GetComponent<TMP_Text>();
        cardName.ForceMeshUpdate(true, true);
        Assert.AreEqual(expected, cardName.color);
        Assert.AreEqual(expected, Get<TMP_Text>("goodsNameText").color);
        Assert.IsTrue(cardName.textInfo.characterInfo.Take(cardName.textInfo.characterCount).Any(c => c.isVisible && c.color.Equals((Color32)expected)));
        Assert.IsFalse(Get<Button>("buyButton").interactable);
    }

    [Test]
    public void ResourceWordsInNamesKeepRarityWhileDescriptionsKeepResourceIcons()
    {
        LootItem_SO item = tile.merchantOffers[0].lootItem;
        item.lootName = "金币钥匙行动点";
        item.rarity = LootRarity.Epic;
        item.description = "获得金币，恢复行动点，获得钥匙。";
        Show(); Layout();
        Color32 expected = new Color32(163, 92, 255, 255);
        foreach (TMP_Text name in new[] { Cards()[0].Find("Name").GetComponent<TMP_Text>(), Get<TMP_Text>("goodsNameText") })
        {
            name.ForceMeshUpdate(true, true);
            TMP_CharacterInfo[] visible = name.textInfo.characterInfo.Take(name.textInfo.characterCount).Where(c => c.isVisible).ToArray();
            Assert.AreEqual(item.lootName.Length, visible.Length);
            Assert.IsTrue(visible.All(c => c.elementType == TMP_TextElementType.Character && c.color.Equals(expected)));
        }
        TMP_Text description = Get<TMP_Text>("descriptionText");
        description.ForceMeshUpdate(true, true);
        Assert.AreEqual(3, description.textInfo.characterInfo.Take(description.textInfo.characterCount).Count(c => c.elementType == TMP_TextElementType.Sprite));
    }

    [TestCase(2640, 1216, 1540, 950)]
    [TestCase(1920, 1080, 1540, 950)]
    [TestCase(1600, 1200, 1540, 950)]
    [TestCase(2640, 1216, 1440, 900)]
    [TestCase(1920, 1080, 1440, 900)]
    [TestCase(1600, 1200, 1440, 900)]
    public void EnglishNamesKeepWholeWordsAndEffectHeadingClearsTheDescription(int width, int height, int panelWidth, int panelHeight)
    {
        typeof(MapMerchantOverlay).GetField("panelSize", Private).SetValue(overlay, new Vector2(panelWidth, panelHeight));
        string[] names = { "Flesh Bearing", "Bloodcleaver Blueprint", "Iron Ore", "Corroded Fang", "Chrysanthemum Pendant", "Counterweight" };
        for (int i = 0; i < names.Length; i++) tile.merchantOffers[i].lootItem.lootName = names[i];
        Show();
        Get<TMP_Text>("effectLabel").text = "Item Effect";
        foreach (Transform card in Cards()) card.Find("Meta").GetComponent<TMP_Text>().text = "Blueprints · Fine";
        Layout(width, height);
        foreach (Transform card in Cards())
        {
            TMP_Text name = card.Find("Name").GetComponent<TMP_Text>();
            AssertWholeWords(name);
            Assert.GreaterOrEqual(name.fontSize, 20, name.text);
            Assert.IsFalse(Bounds(name.transform).Overlaps(Bounds(card.Find("MetaBadge"))));
            TMP_Text meta = card.Find("Meta").GetComponent<TMP_Text>();
            meta.ForceMeshUpdate(true, true);
            Assert.AreEqual(1, meta.textInfo.lineCount);
            Assert.IsFalse(meta.isTextOverflowing, meta.text);
        }
        TMP_Text heading = Get<TMP_Text>("effectLabel");
        heading.ForceMeshUpdate(true, true);
        Assert.AreEqual(1, heading.textInfo.lineCount);
        Assert.Greater(Bounds(heading.transform).yMin, Bounds(Get<TMP_Text>("descriptionText").transform).yMax);
        AssertWholeWords(Get<TMP_Text>("goodsNameText"));
    }

    private static void AssertWholeWords(TMP_Text text)
    {
        text.ForceMeshUpdate(true, true);
        Assert.AreEqual(text.text.Length, text.textInfo.characterCount, text.text);
        for (int i = 1; i < text.textInfo.characterCount; i++)
        {
            TMP_CharacterInfo previous = text.textInfo.characterInfo[i - 1], current = text.textInfo.characterInfo[i];
            if (char.IsLetter(previous.character) && char.IsLetter(current.character))
                Assert.AreEqual(previous.lineNumber, current.lineNumber, text.text);
            if (current.isVisible)
                Assert.LessOrEqual(current.topRight.x, text.rectTransform.rect.xMax + 1, text.text);
        }
    }

    [Test]
    public void LongNamesDescriptionsAndFeedbackScrollWithoutCoveringPurchase()
    {
        LootItem_SO item = tile.merchantOffers[0].lootItem;
        item.lootName = string.Concat(Enumerable.Repeat("长商品名称", 6));
        item.description = string.Join("\n\n", Enumerable.Repeat("获得 25 金币，恢复 3 行动点，并获得 1 把钥匙。", 30));
        tile.merchantOffers.AddRange(tile.merchantOffers.ToArray());
        overlay.Show(state, tile, (t, o) => string.Join("\n", Enumerable.Repeat("购买结果已完整保留。", 16)), t => string.Empty, t => 5);
        Layout();
        Assert.IsTrue(Get<ScrollRect>("offerScrollRect").verticalScrollbar.gameObject.activeSelf);
        Assert.IsTrue(Get<ScrollRect>("detailScrollRect").verticalScrollbar.gameObject.activeSelf);
        TMP_Text name = Cards()[0].Find("Name").GetComponent<TMP_Text>();
        Assert.AreEqual(item.lootName, name.text);
        Assert.Greater(name.rectTransform.rect.height, 80);
        Assert.AreEqual(item.description, Get<TMP_Text>("descriptionText").text);
        Get<Button>("buyButton").onClick.Invoke();
        Layout();
        Assert.Greater(Get<TMP_Text>("statusText").text.Length, 150);
        Assert.IsFalse(Bounds(Find("DetailScrollRect")).Overlaps(Bounds(Find("PurchaseSummary"))));
        Assert.IsFalse(Get<TMP_Text>("descriptionText").enableAutoSizing);
    }

    [Test]
    public void DiscountAndWalletUseServicePriceWithoutChangingRawOfferOrState()
    {
        Relic_SO discount = Own<Relic_SO>();
        discount.builtInEffects.Add(new Relic_SO.RelicBuiltInEffectEntry { trigger = Relic_SO.RelicTrigger.OnMerchantPriceCalculated,
            effectType = Relic_SO.RelicBuiltInEffectType.ReduceMerchantPricePercent, amount = 50, chance = 1 });
        state.relicInventory.Add(new RunRelicState { relicData = discount, amount = 1 });
        Show(); Layout();
        string price = Get<TMP_Text>("priceText").text;
        StringAssert.Contains("<s>20 金币</s>", price);
        StringAssert.Contains("10 金币", price);
        Assert.AreEqual(20, tile.merchantOffers[0].price);
        Assert.AreEqual(250, state.money);
        Assert.AreEqual("250 金币", Get<TMP_Text>("moneyText").text);
        Assert.IsNotNull(Get<TMP_Text>("priceText").textPreprocessor);
    }

    [Test]
    public void PurchaseAndRefreshRebindWalletSelectionAvailabilityAndFullFeedback()
    {
        int bought = 0, refreshed = 0;
        overlay.Show(state, tile, (t, o) => { bought++; state.money -= 20; o.isSoldOut = true; return "支付 20 金币。"; },
            t => { refreshed++; state.money -= 5; t.merchantOffers = new List<MerchantOfferState> { Offer(MerchantItemType.Relic, 4) }; return "支付 5 金币。"; }, t => 5);
        Get<Button>("buyButton").onClick.Invoke(); Get<Button>("buyButton").onClick.Invoke();
        Assert.AreEqual(1, bought);
        Assert.AreEqual("230 金币", Get<TMP_Text>("moneyText").text);
        Assert.IsFalse(Get<Button>("buyButton").interactable);
        StringAssert.Contains("20 金币", Get<TMP_Text>("statusText").text);
        Get<Button>("refreshButton").onClick.Invoke();
        Assert.AreEqual(1, refreshed); Assert.AreEqual(225, state.money);
        Assert.AreEqual(1, Cards().Length);
        Assert.AreEqual(tile.merchantOffers[0].DisplayName, Get<TMP_Text>("goodsNameText").text);
        StringAssert.Contains("5 金币", Get<TMP_Text>("statusText").text);
    }

    [Test]
    public void InsufficientMoneyBlocksHandlersAndEmptyStockCanReopenAndCloseOnce()
    {
        int calls = 0, hidden = 0;
        state.money = 0;
        overlay.Hidden += () => hidden++;
        overlay.Show(state, tile, (t, o) => { calls++; return string.Empty; }, t => { calls++; return string.Empty; }, t => 5);
        Assert.IsFalse(Get<Button>("buyButton").interactable);
        Assert.IsFalse(Get<Button>("refreshButton").interactable);
        Get<Button>("buyButton").onClick.Invoke(); Get<Button>("refreshButton").onClick.Invoke();
        Assert.AreEqual(0, calls);
        StringAssert.Contains("金钱不足", Get<TMP_Text>("availabilityText").text);
        tile.merchantOffers.Clear(); Show(); Layout();
        Assert.IsTrue(Find("EmptyStock").gameObject.activeSelf);
        Assert.IsFalse(Get<Button>("buyButton").interactable);
        Get<Button>("closeButton").onClick.Invoke(); overlay.Hide();
        Assert.AreEqual(1, hidden); Assert.IsFalse(overlay.IsVisible);
        Show(); Assert.IsTrue(overlay.IsVisible);
    }

    private MerchantOfferState Offer(MerchantItemType type, int rarity)
    {
        MerchantOfferState offer = new MerchantOfferState { itemType = type, price = 20 };
        switch (type)
        {
            case MerchantItemType.Loot: offer.lootItem = Own<LootItem_SO>(); offer.lootItem.lootName = "品质样例"; offer.lootItem.rarity = (LootRarity)rarity; offer.lootItem.description = "用于检查商品说明。"; break;
            case MerchantItemType.Weapon: offer.weaponData = Own<Weapon_SO>(); offer.weaponData.weaponName = "品质样例"; offer.weaponData.rarity = (Rarity)rarity; break;
            case MerchantItemType.Blueprint: offer.blueprintData = Own<CraftingBlueprint_SO>(); offer.blueprintData.blueprintName = "品质样例"; offer.blueprintData.rarity = (LootRarity)rarity; break;
            case MerchantItemType.Relic: offer.relicData = Own<Relic_SO>(); offer.relicData.relicName = "品质样例"; offer.relicData.rarity = (LootRarity)rarity; break;
        }
        return offer;
    }
    private T Own<T>() where T : ScriptableObject { T value = ScriptableObject.CreateInstance<T>(); owned.Add(value); return value; }
    private void Show() => overlay.Show(state, tile, (t, o) => string.Empty, t => string.Empty, t => 5);
    private T Get<T>(string field) => (T)typeof(MapMerchantOverlay).GetField(field, Private).GetValue(overlay);
    private Transform Find(string name) => host.GetComponentsInChildren<Transform>(true).First(t => t.name == name);
    private Transform[] Cards() => Get<RectTransform>("listRoot").Cast<Transform>().Where(t => t.gameObject.activeSelf).ToArray();
    private void Layout(int width = 2340, int height = 1080, Rect? safe = null)
    {
        Canvas canvas = host.GetComponentInChildren<Canvas>(); canvas.GetComponent<CanvasScaler>().enabled = false; canvas.renderMode = RenderMode.WorldSpace;
        RectTransform rect = (RectTransform)canvas.transform; rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(1080f * width / height, 1080);
        overlay.ApplyViewport(new Vector2(width, height), safe ?? new Rect(0, 0, width, height));
        typeof(MapMerchantOverlay).GetMethod("RefreshLayout", Private).Invoke(overlay, null);
        Canvas.ForceUpdateCanvases();
    }
    private static Rect Bounds(Transform target) { Vector3[] c = new Vector3[4]; ((RectTransform)target).GetWorldCorners(c); return Rect.MinMaxRect(c[0].x, c[0].y, c[2].x, c[2].y); }
    private static void Contains(Rect outer, Rect inner) { Assert.GreaterOrEqual(inner.xMin, outer.xMin - 0.2); Assert.GreaterOrEqual(inner.yMin, outer.yMin - 0.2); Assert.LessOrEqual(inner.xMax, outer.xMax + 0.2); Assert.LessOrEqual(inner.yMax, outer.yMax + 0.2); }
}
