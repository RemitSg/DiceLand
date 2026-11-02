using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public partial class MapMerchantOverlay
{
    private static readonly Color Ivory = new Color(0.96f, 0.93f, 0.81f);
    private static readonly Color Gold = new Color(0.75f, 0.60f, 0.32f);
    private static readonly Color MoneyGold = new Color32(240, 204, 107, 255);
    private static readonly Color HeadingIvory = new Color(0.90f, 0.84f, 0.65f);
    private static readonly Color Muted = new Color(0.69f, 0.72f, 0.64f);
    private Canvas canvas;
    private RectTransform root, safeAreaRect, panelRect, detailRoot, purchaseRoot, listRoot, offerViewport, detailContent, detailViewport;
    private ScrollRect offerScrollRect, detailScrollRect;
    private TextMeshProUGUI titleText, moneyText, goodsNameText, metaText, descriptionText, flavorText, statusText, availabilityText, priceText, emptyText, effectLabel;
    private RectTransform flavorRule, descriptionRule, offersRule, columnRule, detailIconWell, detailMetaBadge;
    private Image iconImage;
    private Button refreshButton, buyButton, closeButton;
    private bool layoutDirty;
    private Vector2 lastViewport;
    private Rect lastSafeArea;
    private readonly List<OfferCard> cards = new List<OfferCard>();

    private sealed class OfferCard
    {
        public MerchantOfferState offer;
        public Button button;
        public MapMerchantCardGraphic face;
        public Image icon;
        public MapMerchantFrameGraphic iconWell;
        public RectTransform metaBadge, soldOutBadge;
        public TextMeshProUGUI name, meta, price;
    }

    private void EnsureUi()
    {
        if (canvas != null) return;
        // Migrate old serialized UI dimensions without changing merchant or run data.
        panelSize = new Vector2(Mathf.Max(1440, panelSize.x), Mathf.Max(900, panelSize.y));
        GameObject go = new GameObject("MerchantOverlayCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(Image));
        go.transform.SetParent(transform, false);
        canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9060;
        go.GetComponent<Image>().color = new Color(0.005f, 0.012f, 0.009f, 0.58f);
        CanvasScaler scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 1f;
        root = (RectTransform)go.transform;
        Stretch(root);
        safeAreaRect = Rect(root, "SafeArea"); Stretch(safeAreaRect);
        panelRect = Rect(safeAreaRect, "Panel");
        panelRect.anchorMin = panelRect.anchorMax = panelRect.pivot = new Vector2(1f, 0.5f);
        panelRect.sizeDelta = panelSize;
        panelRect.gameObject.AddComponent<Image>().color = Color.clear;
        Frame(panelRect, MapMerchantFrameGraphic.Style.Panel);
        Image shop = Icon(panelRect, "ShopIcon", MapForestArt.Get("Merchant"));
        Place(shop.rectTransform, 40, 24, 92, 92);
        titleText = Text(panelRect, "Title", titleFontSize, Ivory, "林间商店", true);
        Place(titleText.rectTransform, 150, 30, 670, 72);
        RectTransform wallet = Panel(panelRect, "Wallet", Color.clear);
        Frame(wallet, MapMerchantFrameGraphic.Style.Wallet);
        Place(wallet, panelSize.x - 460, 30, 310, 72);
        moneyText = Text(wallet, "Money", 32, MoneyGold, string.Empty, true);
        Inset(moneyText.rectTransform, 16, 8);
        moneyText.alignment = TextAlignmentOptions.Midline;
        Button closeX = Action(panelRect, "CloseXButton", "×", Hide, true);
        Place((RectTransform)closeX.transform, panelSize.x - 116, 30, 72, 72);
        Rule(panelRect, "HeaderRule", 30, 128, panelSize.x - 60, 1.5f);
        TextMeshProUGUI heading = Text(panelRect, "OffersHeading", 30, HeadingIvory, "在售商品", true);
        Place(heading.rectTransform, 44, 142, 280, 44);
        offersRule = Rule(panelRect, "OffersRule", 252, 162, 300, 12, true);
        columnRule = Rule(panelRect, "ColumnRule", 0, 0, 8, 500);
        offerScrollRect = ScrollArea(panelRect, "OfferScrollRect", out offerViewport, out listRoot);
        emptyText = Text(offerViewport, "EmptyStock", 30, Muted, "本次商店暂无商品。", true);
        Inset(emptyText.rectTransform, 16, 24);
        emptyText.alignment = TextAlignmentOptions.Top;

        detailRoot = Panel(panelRect, "DetailRoot", Color.clear);
        Frame(detailRoot);
        detailScrollRect = ScrollArea(detailRoot, "DetailScrollRect", out detailViewport, out detailContent);
        detailIconWell = Rect(detailContent, "IconWell");
        MapMerchantFrameGraphic detailWell = detailIconWell.gameObject.AddComponent<MapMerchantFrameGraphic>();
        detailWell.style = MapMerchantFrameGraphic.Style.IconWell; detailWell.raycastTarget = false;
        iconImage = Icon(detailContent, "Icon", null);
        goodsNameText = Text(detailContent, "GoodsName", 38, Ivory, string.Empty, true, false);
        detailMetaBadge = Rect(detailContent, "MetaBadge");
        MapMerchantFrameGraphic detailBadge = detailMetaBadge.gameObject.AddComponent<MapMerchantFrameGraphic>();
        detailBadge.style = MapMerchantFrameGraphic.Style.Badge; detailBadge.raycastTarget = false;
        metaText = Text(detailContent, "Meta", 25, Muted, string.Empty);
        effectLabel = Text(detailContent, "EffectHeading", 29, HeadingIvory, "物品效果", true);
        descriptionRule = Rule(detailContent, "EffectRule", 160, 0, 100, 12, true);
        descriptionText = Text(detailContent, "Description", bodyFontSize, Ivory, string.Empty);
        flavorRule = Rule(detailContent, "FlavorRule", 0, 0, 100, 1);
        flavorText = Text(detailContent, "Flavor", 27, Muted, string.Empty);
        flavorText.fontStyle = FontStyles.Italic;
        availabilityText = Text(detailContent, "Availability", 27, new Color(0.96f, 0.65f, 0.53f), string.Empty);
        statusText = Text(detailContent, "Status", 27, Ivory, string.Empty);
        purchaseRoot = Panel(detailRoot, "PurchaseSummary", Color.clear);
        Frame(purchaseRoot);
        TextMeshProUGUI caption = Text(purchaseRoot, "PriceHeading", 26, Ivory, "购买价格");
        Place(caption.rectTransform, 22, 12, 190, 38);
        priceText = Text(purchaseRoot, "Price", 36, MoneyGold, string.Empty, true);
        buyButton = Action(purchaseRoot, "BuyButton", buyButtonLabel, OnBuyClicked, true);
        refreshButton = Action(panelRect, "RefreshButton", refreshButtonLabel, OnRefreshClicked, false);
        RectTransform refreshIconRect = Rect(refreshButton.transform, "RefreshIcon");
        Place(refreshIconRect, 22, 20, 40, 40);
        MapHudGlyph refreshIcon = refreshIconRect.gameObject.AddComponent<MapHudGlyph>();
        refreshIcon.shape = MapHudGlyph.Shape.Refresh; refreshIcon.color = Gold; refreshIcon.raycastTarget = false;
        RectTransform refreshLabel = refreshButton.GetComponentInChildren<TMP_Text>().rectTransform;
        refreshLabel.offsetMin = new Vector2(72, 6);
        closeButton = Action(panelRect, "CloseButton", closeButtonLabel, Hide, false);
        ApplyFonts();
    }

    private OfferCard BuildOfferCard(MerchantOfferState offer)
    {
        RectTransform rect = Rect(listRoot, "Offer_" + cards.Count.ToString("00"));
        MapMerchantCardGraphic face = rect.gameObject.AddComponent<MapMerchantCardGraphic>();
        face.SetState(false, !offer.isSoldOut);
        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = face;
        button.onClick.AddListener(() => SelectOffer(offer));
        OfferCard card = new OfferCard { offer = offer, button = button, face = face };
        RectTransform well = Rect(rect, "IconWell");
        card.iconWell = well.gameObject.AddComponent<MapMerchantFrameGraphic>();
        card.iconWell.style = MapMerchantFrameGraphic.Style.IconWell;
        card.iconWell.raycastTarget = false;
        card.icon = Icon(rect, "Icon", offer.DisplayIcon ?? fallbackIconSprite);
        card.name = Text(rect, "Name", 30, ResolveOfferNameColor(offer), offer.DisplayName, true, false);
        card.metaBadge = Rect(rect, "MetaBadge");
        MapMerchantFrameGraphic metaFrame = card.metaBadge.gameObject.AddComponent<MapMerchantFrameGraphic>();
        metaFrame.style = MapMerchantFrameGraphic.Style.Badge; metaFrame.raycastTarget = false;
        card.meta = Text(rect, "Meta", 24, Muted, GetGoodsTypeLabel(offer.itemType) + " · " + ResolveOfferRarityLabel(offer));
        card.soldOutBadge = Rect(rect, "SoldOutBadge");
        MapMerchantFrameGraphic soldOutFrame = card.soldOutBadge.gameObject.AddComponent<MapMerchantFrameGraphic>();
        soldOutFrame.style = MapMerchantFrameGraphic.Style.Badge; soldOutFrame.raycastTarget = false;
        card.soldOutBadge.gameObject.SetActive(offer.isSoldOut);
        card.price = Text(rect, "Price", 30, offer.isSoldOut ? Muted : MoneyGold, offer.isSoldOut ? "已售罄" : FormatPrice(offer, true), true);
        if (offer.isSoldOut) card.price.alignment = TextAlignmentOptions.Top;
        return card;
    }

    public void ApplyViewport(Vector2 viewport, Rect safePixels)
    {
        if (canvas == null) return;
        float width = Mathf.Max(1, viewport.x), height = Mathf.Max(1, viewport.y);
        float x = Mathf.Clamp(safePixels.xMin, 0, width - 1), y = Mathf.Clamp(safePixels.yMin, 0, height - 1);
        Rect safe = new Rect(x, y, Mathf.Clamp(safePixels.width, 1, width - x), Mathf.Clamp(safePixels.height, 1, height - y));
        safeAreaRect.anchorMin = new Vector2(safe.xMin / width, safe.yMin / height);
        safeAreaRect.anchorMax = new Vector2(safe.xMax / width, safe.yMax / height);
        safeAreaRect.offsetMin = safeAreaRect.offsetMax = Vector2.zero;
        Vector2 available = safe.size * (1080f / height);
        float scale = Mathf.Max(0.01f, Mathf.Min(1f, (available.x - 48) / panelSize.x, (available.y - 48) / panelSize.y));
        panelRect.localScale = Vector3.one * scale;
        float gap = Mathf.Max(0, available.x - panelSize.x * scale);
        float right = Mathf.Min(available.x * 0.075f, gap * 0.5f);
        float limitY = Mathf.Max(0, (available.y - panelSize.y * scale) * 0.5f - 24);
        panelRect.anchoredPosition = new Vector2(-Mathf.Clamp(right - panelOffset.x, 24, Mathf.Max(24, gap - 24)), Mathf.Clamp(panelOffset.y, -limitY, limitY));
        lastViewport = viewport; lastSafeArea = safePixels;
        layoutDirty = true;
    }

    private void RefreshLayout()
    {
        if (canvas == null) return;
        float width = panelSize.x, height = panelSize.y;
        panelRect.sizeDelta = panelSize;
        float leftWidth = (width - 120) * 0.55f, rightX = 40 + leftWidth + 36, rightWidth = width - rightX - 40;
        float footerTop = height - 126;
        Place(offersRule, 252, 162, leftWidth - 212, 12);
        Place(columnRule, 40 + leftWidth + 14, 150, 8, footerTop - 170);
        Place(offerScrollRect.GetComponent<RectTransform>(), 40, 196, leftWidth, footerTop - 212);
        Place(detailRoot, rightX, 158, rightWidth, height - 198);
        Place((RectTransform)refreshButton.transform, 40, footerTop + 10, leftWidth - 246, 80);
        Place((RectTransform)closeButton.transform, 40 + leftWidth - 228, footerTop + 10, 228, 80);
        Canvas.ForceUpdateCanvases();
        float cardWidth = (offerViewport.rect.width - 16) * 0.5f;
        float rowsHeight = 0;
        for (int i = 0; i < cards.Count; i += 2)
        {
            float rowHeight = 192;
            for (int column = 0; column < 2 && i + column < cards.Count; column++)
            {
                OfferCard card = cards[i + column];
                float nameHeight = Preferred(card.name, cardWidth - 194);
                float metaHeight = Preferred(card.meta, cardWidth - 192);
                float priceHeight = Preferred(card.price, cardWidth - 176);
                rowHeight = Mathf.Max(rowHeight, 58 + nameHeight + metaHeight + priceHeight);
                Place(card.iconWell.rectTransform, 14, 14, 132, 164);
                Place(card.icon.rectTransform, 22, 28, 116, 136);
                Place(card.name.rectTransform, 154, 16, cardWidth - 194, nameHeight + 2);
                float badgeWidth = Mathf.Min(cardWidth - 176, Mathf.Max(128, card.meta.GetPreferredValues().x + 20));
                Place(card.metaBadge, 152, 24 + nameHeight, badgeWidth, metaHeight + 8);
                Place(card.meta.rectTransform, 160, 27 + nameHeight, badgeWidth - 16, metaHeight + 2);
                Place(card.soldOutBadge, 152, 40 + nameHeight + metaHeight, cardWidth - 176, priceHeight + 6);
                Place(card.price.rectTransform, 154, 42 + nameHeight + metaHeight, cardWidth - 176, priceHeight + 4);
            }
            for (int column = 0; column < 2 && i + column < cards.Count; column++)
                Place((RectTransform)cards[i + column].button.transform, column * (cardWidth + 16), rowsHeight, cardWidth, rowHeight);
            rowsHeight += rowHeight + 16;
        }
        listRoot.sizeDelta = new Vector2(0, Mathf.Max(offerViewport.rect.height, rowsHeight - 16));
        float purchaseHeight = Mathf.Max(144, Preferred(priceText, rightWidth - 368) + 64);
        Place(purchaseRoot, 18, detailRoot.rect.height - purchaseHeight - 18, rightWidth - 36, purchaseHeight);
        Place(priceText.rectTransform, 22, 48, rightWidth - 368, purchaseHeight - 56);
        Place((RectTransform)buyButton.transform, rightWidth - 342, (purchaseHeight - 88) * 0.5f, 284, 88);
        Place(detailScrollRect.GetComponent<RectTransform>(), 28, 26, rightWidth - 56, detailRoot.rect.height - purchaseHeight - 62);
        Canvas.ForceUpdateCanvases();
        LayoutDetails();
        UpdateScrollbar(offerScrollRect);
        UpdateScrollbar(detailScrollRect);
        layoutDirty = false;
    }

    private void LayoutDetails()
    {
        float width = Mathf.Max(40, detailViewport.rect.width - 6);
        float nameHeight = Preferred(goodsNameText, width - 204), metaHeight = Preferred(metaText, width - 222);
        Place(detailIconWell, 0, 0, 178, 198);
        Place(iconImage.rectTransform, 10, 12, 158, 174);
        Place(goodsNameText.rectTransform, 204, 4, width - 204, nameHeight + 4);
        bool hasMeta = !string.IsNullOrWhiteSpace(metaText.text);
        detailMetaBadge.gameObject.SetActive(hasMeta);
        float badgeWidth = Mathf.Min(width - 204, Mathf.Max(154, metaText.GetPreferredValues().x + 28));
        Place(detailMetaBadge, 204, nameHeight + 22, badgeWidth, metaHeight + 12);
        Place(metaText.rectTransform, 218, nameHeight + 27, badgeWidth - 28, metaHeight + 4);
        float top = Mathf.Max(198, nameHeight + metaHeight + 40) + 30;
        Place(effectLabel.rectTransform, 0, top, 148, 42);
        Place(descriptionRule, 156, top + 17, Mathf.Max(20, width - 156), 12);
        top += 54;
        top = Flow(descriptionText, top, width, 18);
        bool flavor = !string.IsNullOrWhiteSpace(flavorText.text);
        flavorRule.gameObject.SetActive(flavor);
        if (flavor) { Place(flavorRule, 0, top + 4, width, 1); top += 26; }
        top = Flow(flavorText, top, width, 24);
        top = Flow(availabilityText, top, width, 16);
        top = Flow(statusText, top, width, 12);
        detailContent.sizeDelta = new Vector2(0, Mathf.Max(detailViewport.rect.height, top));
    }

    private static float Flow(TextMeshProUGUI text, float top, float width, float spacing)
    {
        bool visible = !string.IsNullOrWhiteSpace(text.text);
        text.gameObject.SetActive(visible);
        if (!visible) return top;
        float height = Preferred(text, width) + 4;
        Place(text.rectTransform, 0, top, width, height);
        return top + height + spacing;
    }

    private static float Preferred(TMP_Text text, float width) => text.GetPreferredValues(Mathf.Max(40, width), 0).y;

    private void ApplyFonts()
    {
        if (canvas == null) return;
        foreach (TMP_Text text in canvas.GetComponentsInChildren<TMP_Text>(true))
        {
            TMP_FontAsset font = text == titleText || text == goodsNameText || text.name == "Name" ? keywordFont ?? bodyFont : bodyFont ?? keywordFont;
            if (font != null) text.font = font;
            text.UpdateMeshPadding(); text.SetAllDirty();
        }
    }

    private TextMeshProUGUI Text(Transform parent, string name, int size, Color color, string value, bool keyword = false, bool resourceTerms = true)
    {
        TextMeshProUGUI text = Rect(parent, name).gameObject.AddComponent<TextMeshProUGUI>();
        TMP_FontAsset font = keyword ? keywordFont ?? bodyFont : bodyFont ?? keywordFont;
        if (font != null) text.font = font;
        text.fontSize = size; text.color = color; text.text = value;
        text.alignment = TextAlignmentOptions.TopLeft;
        text.enableWordWrapping = true; text.overflowMode = TextOverflowModes.Overflow;
        text.raycastTarget = false;
        if (resourceTerms) ResourceKeywordText.Attach(text);
        return text;
    }

    private Button Action(Transform parent, string name, string label, UnityEngine.Events.UnityAction callback, bool gold)
    {
        RectTransform rect = Panel(parent, name, Color.clear);
        MapMerchantFrameGraphic frame = Frame(rect, gold ? MapMerchantFrameGraphic.Style.GoldButton : MapMerchantFrameGraphic.Style.SecondaryButton);
        frame.jadeFace = name == "RefreshButton";
        Button button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = frame;
        ColorBlock colors = button.colors; colors.disabledColor = new Color(0.46f, 0.46f, 0.46f); button.colors = colors;
        button.onClick.AddListener(callback);
        TextMeshProUGUI text = Text(rect, "Label", buttonFontSize, gold ? new Color(0.16f, 0.105f, 0.04f) : Ivory, label, true);
        if (name == "BuyButton") text.fontSize = 36;
        text.alignment = TextAlignmentOptions.Midline; Inset(text.rectTransform, 14, 6);
        return button;
    }

    private static ScrollRect ScrollArea(Transform parent, string name, out RectTransform viewport, out RectTransform content)
    {
        RectTransform rect = Rect(parent, name);
        rect.gameObject.AddComponent<Image>().color = Color.clear;
        ScrollRect scroll = rect.gameObject.AddComponent<ScrollRect>();
        viewport = Rect(rect, "Viewport"); Stretch(viewport); viewport.offsetMax = new Vector2(-18, 0);
        viewport.gameObject.AddComponent<Image>().color = Color.clear;
        viewport.gameObject.AddComponent<RectMask2D>();
        content = Rect(viewport, "Content");
        content.anchorMin = new Vector2(0, 1); content.anchorMax = Vector2.one; content.pivot = new Vector2(0.5f, 1);
        content.sizeDelta = Vector2.zero;
        RectTransform track = Rect(rect, "Scrollbar");
        track.anchorMin = new Vector2(1, 0); track.anchorMax = Vector2.one; track.pivot = Vector2.one;
        track.offsetMin = new Vector2(-8, 0); track.offsetMax = Vector2.zero;
        track.gameObject.AddComponent<Image>().color = new Color(Gold.r, Gold.g, Gold.b, 0.13f);
        Scrollbar scrollbar = track.gameObject.AddComponent<Scrollbar>();
        RectTransform handle = Rect(track, "Handle"); Stretch(handle);
        Image handleImage = handle.gameObject.AddComponent<Image>(); handleImage.color = Gold;
        scrollbar.handleRect = handle; scrollbar.targetGraphic = handleImage; scrollbar.direction = Scrollbar.Direction.BottomToTop;
        scroll.viewport = viewport; scroll.content = content; scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 36;
        scroll.verticalScrollbar = scrollbar; scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
        return scroll;
    }

    private static void UpdateScrollbar(ScrollRect scroll)
    {
        bool overflowing = scroll.content.rect.height > scroll.viewport.rect.height + 0.5f;
        scroll.verticalScrollbar.gameObject.SetActive(overflowing);
        scroll.verticalScrollbar.size = Mathf.Clamp01(scroll.viewport.rect.height / Mathf.Max(1, scroll.content.rect.height));
    }

    private static void ResetScroll(ScrollRect scroll) { scroll.StopMovement(); scroll.content.anchoredPosition = Vector2.zero; scroll.verticalNormalizedPosition = 1; }
    private static RectTransform Rect(Transform parent, string name)
    {
        RectTransform rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false); rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1); return rect;
    }
    private static void Stretch(RectTransform rect) { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero; }
    private static void Inset(RectTransform rect, float x, float y) { Stretch(rect); rect.offsetMin = new Vector2(x, y); rect.offsetMax = new Vector2(-x, -y); }
    private static void Place(RectTransform rect, float x, float y, float width, float height) { rect.anchoredPosition = new Vector2(x, -y); rect.sizeDelta = new Vector2(width, height); }
    private static RectTransform Panel(Transform parent, string name, Color color) { RectTransform rect = Rect(parent, name); rect.gameObject.AddComponent<Image>().color = color; return rect; }
    private static MapMerchantFrameGraphic Frame(RectTransform rect, MapMerchantFrameGraphic.Style style = MapMerchantFrameGraphic.Style.Inset) { RectTransform child = Rect(rect, "Frame"); Stretch(child); MapMerchantFrameGraphic frame = child.gameObject.AddComponent<MapMerchantFrameGraphic>(); frame.style = style; frame.raycastTarget = false; return frame; }
    private static RectTransform Rule(Transform parent, string name, float x, float y, float width, float height, bool diamond = false) { RectTransform rect = Rect(parent, name); MapMerchantFrameGraphic frame = rect.gameObject.AddComponent<MapMerchantFrameGraphic>(); frame.style = MapMerchantFrameGraphic.Style.Divider; frame.leadingDiamond = diamond; frame.raycastTarget = false; Place(rect, x, y, width, height); return rect; }
    private static Image Icon(Transform parent, string name, Sprite sprite) { RectTransform rect = Rect(parent, name); Image icon = rect.gameObject.AddComponent<Image>(); icon.sprite = sprite; icon.enabled = sprite != null; icon.preserveAspect = true; icon.raycastTarget = false; return icon; }

    private void LateUpdate()
    {
        if (!IsVisible) return;
        Vector2 viewport = new Vector2(Screen.width, Screen.height);
        if (viewport != lastViewport || Screen.safeArea != lastSafeArea) ApplyViewport(viewport, Screen.safeArea);
        if (layoutDirty) RefreshLayout();
    }
}
