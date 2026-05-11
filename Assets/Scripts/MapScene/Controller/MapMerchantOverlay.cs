using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class MapMerchantOverlay : MonoBehaviour
{
    public event Action Hidden;

    [Header("Font")]
    [SerializeField] private TMP_FontAsset keywordFont;
    [SerializeField] private TMP_FontAsset bodyFont;
    [SerializeField] private int titleFontSize = 34;
    [SerializeField] private int bodyFontSize = 24;
    [SerializeField] private int buttonFontSize = 24;

    [Header("Panel")]
    [SerializeField] private Vector2 panelSize = new Vector2(980f, 580f);
    [SerializeField] private Sprite panelBackgroundSprite;
    [SerializeField] private Color panelBackgroundColor = new Color(0.08f, 0.1f, 0.14f, 0.92f);
    [SerializeField] private Vector2 panelOffset = Vector2.zero;

    [Header("Offer List")]
    [SerializeField] private Vector2 offerListOffset = new Vector2(24f, -78f);
    [SerializeField] private Vector2 offerListViewportSize = new Vector2(296f, 430f);
    [SerializeField] private Vector2 offerButtonSize = new Vector2(280f, 76f);
    [SerializeField] private float offerButtonSpacing = 12f;
    [SerializeField] private Vector2 offerIconSize = new Vector2(52f, 52f);
    [SerializeField] private Vector2 offerIconOffset = new Vector2(12f, -12f);
    [SerializeField] private Sprite offerButtonSprite;
    [SerializeField] private Sprite selectedOfferButtonSprite;
    [SerializeField] private Color offerButtonColor = new Color(0.18f, 0.16f, 0.12f, 0.96f);
    [SerializeField] private Color selectedOfferButtonColor = new Color(1f, 0.83f, 0.35f, 1f);
    [SerializeField] private Color offerButtonTextColor = Color.white;
    [SerializeField] private Color selectedOfferButtonTextColor = Color.black;
    [SerializeField] private Color soldOutOfferButtonColor = new Color(0.36f, 0.36f, 0.36f, 0.88f);
    [SerializeField] private float offerListScrollbarWidth = 18f;
    [SerializeField] private Color offerListScrollbarTrackColor = new Color(0.15f, 0.15f, 0.18f, 0.85f);
    [SerializeField] private Color offerListScrollbarHandleColor = new Color(1f, 0.83f, 0.35f, 0.95f);

    [Header("Buttons")]
    [SerializeField] private Sprite actionButtonSprite;
    [SerializeField] private Vector2 actionButtonSize = new Vector2(180f, 56f);
    [SerializeField] private Color actionButtonColor = new Color(1f, 0.83f, 0.35f, 1f);
    [SerializeField] private Color disabledButtonColor = new Color(0.4f, 0.4f, 0.4f, 0.9f);
    [SerializeField] private Color actionButtonTextColor = Color.black;
    [SerializeField] private Vector2 refreshButtonOffset = new Vector2(-392f, 24f);
    [SerializeField] private Vector2 buyButtonOffset = new Vector2(-208f, 24f);
    [SerializeField] private Vector2 closeButtonOffset = new Vector2(-24f, 24f);
    [SerializeField] private string refreshButtonLabel = "刷新";
    [SerializeField] private string buyButtonLabel = "购买";
    [SerializeField] private string closeButtonLabel = "关闭";

    [Header("Detail")]
    [SerializeField] private Vector2 detailOffset = new Vector2(322f, -72f);
    [SerializeField] private Vector2 detailSize = new Vector2(600f, 360f);
    [SerializeField] private Vector2 detailIconSize = new Vector2(110f, 110f);
    [SerializeField] private Color titleColor = Color.white;
    [SerializeField] private Color bodyColor = new Color(0.92f, 0.96f, 1f, 1f);
    [SerializeField] private Color accentColor = new Color(1f, 0.83f, 0.35f, 1f);
    [SerializeField] private Color soldOutColor = new Color(1f, 0.54f, 0.54f, 1f);
    [SerializeField] private Sprite fallbackIconSprite;

    private Canvas canvas;
    private RectTransform root;
    private RectTransform panelRect;
    private Image panelImage;
    private ScrollRect offerScrollRect;
    private RectTransform offerViewport;
    private RectTransform listRoot;
    private TextMeshProUGUI titleText;
    private TextMeshProUGUI moneyText;
    private TextMeshProUGUI goodsNameText;
    private TextMeshProUGUI metaText;
    private TextMeshProUGUI descriptionText;
    private TextMeshProUGUI statusText;
    private Image iconImage;
    private Button refreshButton;
    private Button buyButton;
    private Button closeButton;
    private readonly List<Button> offerButtons = new List<Button>();
    private readonly List<GameObject> spawnedOfferObjects = new List<GameObject>();
    private readonly Dictionary<Button, MerchantOfferState> offerLookup = new Dictionary<Button, MerchantOfferState>();

    private RunState currentRunState;
    private BoardTileData currentTile;
    private MerchantOfferState selectedOffer;
    private Func<BoardTileData, MerchantOfferState, string> buyHandler;
    private Func<BoardTileData, string> refreshHandler;
    private Func<BoardTileData, int> refreshCostResolver;

    public bool IsVisible =>
        panelRect != null &&
        panelRect.gameObject.activeInHierarchy;

    public void ConfigureFonts(TMP_FontAsset keyword, TMP_FontAsset body)
    {
        if (keyword != null)
            keywordFont = keyword;
        if (body != null)
            bodyFont = body;

        ApplyFonts();
    }

    public void Show(
        RunState runState,
        BoardTileData tile,
        Func<BoardTileData, MerchantOfferState, string> onBuy,
        Func<BoardTileData, string> onRefresh,
        Func<BoardTileData, int> onResolveRefreshCost)
    {
        EnsureUi();
        currentRunState = runState;
        currentTile = tile;
        buyHandler = onBuy;
        refreshHandler = onRefresh;
        refreshCostResolver = onResolveRefreshCost;
        gameObject.SetActive(true);

        RefreshOfferList();
        SelectOffer(ResolveSelectableOffer(selectedOffer));
    }

    public void Hide()
    {
        if (panelRect != null)
            panelRect.gameObject.SetActive(false);
        Hidden?.Invoke();
    }

    private void EnsureUi()
    {
        if (canvas != null)
        {
            panelRect.gameObject.SetActive(true);
            return;
        }

        GameObject canvasGo = new GameObject("MerchantOverlayCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.transform.SetParent(transform, false);
        canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9060;

        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        root = canvasGo.GetComponent<RectTransform>();
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.offsetMin = Vector2.zero;
        root.offsetMax = Vector2.zero;

        panelRect = CreatePanel(root, "Panel", new Vector2(0.5f, 0.5f), panelOffset, panelSize);
        panelImage = panelRect.GetComponent<Image>();
        ApplyPanelVisual();

        titleText = CreateText(panelRect, "Title", new Vector2(26f, -18f), new Vector2(320f, 42f), titleFontSize, titleColor, "商人", true);
        moneyText = CreateText(panelRect, "Money", new Vector2(340f, -18f), new Vector2(220f, 36f), bodyFontSize, accentColor, string.Empty, true);

        BuildOfferScrollArea();

        RectTransform detailRoot = new GameObject("DetailRoot", typeof(RectTransform)).GetComponent<RectTransform>();
        detailRoot.SetParent(panelRect, false);
        detailRoot.anchorMin = new Vector2(0f, 1f);
        detailRoot.anchorMax = new Vector2(0f, 1f);
        detailRoot.pivot = new Vector2(0f, 1f);
        detailRoot.anchoredPosition = detailOffset;
        detailRoot.sizeDelta = detailSize;

        iconImage = BuildIcon(detailRoot);
        goodsNameText = CreateText(detailRoot, "GoodsName", new Vector2(130f, -8f), new Vector2(420f, 42f), titleFontSize - 4, titleColor, "未选择商品", true);
        metaText = CreateText(detailRoot, "Meta", new Vector2(130f, -56f), new Vector2(420f, 34f), bodyFontSize, accentColor, string.Empty, true);
        descriptionText = CreateText(detailRoot, "Description", new Vector2(0f, -138f), new Vector2(detailSize.x - 12f, 150f), bodyFontSize, bodyColor, string.Empty);
        descriptionText.enableWordWrapping = true;
        descriptionText.overflowMode = TextOverflowModes.Overflow;
        statusText = CreateText(detailRoot, "Status", new Vector2(0f, -306f), new Vector2(detailSize.x - 12f, 36f), bodyFontSize - 2, accentColor, string.Empty);

        refreshButton = BuildActionButton(panelRect, "RefreshButton", refreshButtonOffset, refreshButtonLabel, OnRefreshClicked);
        buyButton = BuildActionButton(panelRect, "BuyButton", buyButtonOffset, buyButtonLabel, OnBuyClicked);
        closeButton = BuildActionButton(panelRect, "CloseButton", closeButtonOffset, closeButtonLabel, Hide);

        ApplyFonts();
        panelRect.gameObject.SetActive(true);
    }

    private void RefreshOfferList()
    {
        for (int i = 0; i < spawnedOfferObjects.Count; i++)
            Destroy(spawnedOfferObjects[i]);
        spawnedOfferObjects.Clear();
        offerButtons.Clear();
        offerLookup.Clear();

        if (moneyText != null)
            moneyText.text = $"金钱 {Mathf.Max(0, currentRunState != null ? currentRunState.money : 0)}";

        if (currentTile?.merchantOffers == null)
        {
            RefreshRefreshButtonState();
            return;
        }

        float y = 0f;
        for (int i = 0; i < currentTile.merchantOffers.Count; i++)
        {
            MerchantOfferState offer = currentTile.merchantOffers[i];
            if (offer == null)
                continue;

            Button button = BuildOfferButton(offer, new Vector2(0f, -y));
            offerButtons.Add(button);
            offerLookup[button] = offer;
            spawnedOfferObjects.Add(button.gameObject);
            y += offerButtonSize.y + offerButtonSpacing;
        }

        if (listRoot != null)
            listRoot.sizeDelta = new Vector2(GetOfferListContentWidth(), Mathf.Max(offerListViewportSize.y, y > 0f ? y - offerButtonSpacing : offerListViewportSize.y));

        if (offerScrollRect != null)
            offerScrollRect.verticalNormalizedPosition = 1f;

        RefreshRefreshButtonState();
    }

    private void SelectOffer(MerchantOfferState offer)
    {
        selectedOffer = offer;
        RefreshOfferButtonVisuals();
        RefreshDetail();
    }

    private void RefreshDetail()
    {
        if (selectedOffer == null)
        {
            if (iconImage != null)
                iconImage.sprite = fallbackIconSprite;
            if (goodsNameText != null)
            {
                goodsNameText.text = "未选择商品";
                goodsNameText.color = titleColor;
            }
            if (metaText != null)
                metaText.text = string.Empty;
            if (descriptionText != null)
                descriptionText.text = "请选择一个商品来查看详情。";
            SetBuyButtonInteractable(false);
            RefreshRefreshButtonState();
            return;
        }

        if (iconImage != null)
        {
            iconImage.sprite = selectedOffer.DisplayIcon != null ? selectedOffer.DisplayIcon : fallbackIconSprite;
            iconImage.preserveAspect = true;
        }

        if (goodsNameText != null)
        {
            goodsNameText.text = selectedOffer.DisplayName;
            goodsNameText.color = ResolveOfferNameColor(selectedOffer);
        }

        if (metaText != null)
        {
            string kindLabel = GetGoodsTypeLabel(selectedOffer.itemType);
            string rarityLabel = ResolveOfferRarityLabel(selectedOffer);
            string soldOutLabel = selectedOffer.isSoldOut ? " | 已售罄" : string.Empty;
            metaText.text = BuildDetailMetaLabel(selectedOffer, kindLabel, rarityLabel, ResolveDisplayPrice(selectedOffer), soldOutLabel);
            metaText.color = selectedOffer.isSoldOut ? soldOutColor : accentColor;
        }

        if (descriptionText != null)
            descriptionText.text = string.IsNullOrWhiteSpace(selectedOffer.Description) ? "暂无商品描述。" : selectedOffer.Description;

        int resolvedPrice = ResolveDisplayPrice(selectedOffer);
        SetBuyButtonInteractable(!selectedOffer.isSoldOut && currentRunState != null && currentRunState.money >= Mathf.Max(0, resolvedPrice));
        RefreshRefreshButtonState();
    }

    private void OnBuyClicked()
    {
        if (selectedOffer == null || buyHandler == null || currentTile == null)
            return;

        string result = buyHandler.Invoke(currentTile, selectedOffer);
        if (statusText != null)
            statusText.text = result ?? string.Empty;

        MerchantOfferState retainedOffer = selectedOffer;
        RefreshOfferList();
        SelectOffer(ResolveSelectableOffer(retainedOffer));
    }

    private void OnRefreshClicked()
    {
        if (refreshHandler == null || currentTile == null)
            return;

        string result = refreshHandler.Invoke(currentTile);
        if (statusText != null)
            statusText.text = result ?? string.Empty;

        MerchantOfferState retainedOffer = selectedOffer;
        RefreshOfferList();
        SelectOffer(ResolveSelectableOffer(retainedOffer));
    }

    private MerchantOfferState GetFirstOffer()
    {
        if (currentTile?.merchantOffers == null)
            return null;

        for (int i = 0; i < currentTile.merchantOffers.Count; i++)
        {
            MerchantOfferState offer = currentTile.merchantOffers[i];
            if (offer != null)
                return offer;
        }

        return null;
    }

    private Button BuildOfferButton(MerchantOfferState offer, Vector2 anchoredPosition)
    {
        GameObject buttonGo = new GameObject($"Offer_{offer.DisplayName}", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonGo.transform.SetParent(listRoot, false);

        float buttonWidth = GetOfferListContentWidth();

        RectTransform rect = buttonGo.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = new Vector2(buttonWidth, offerButtonSize.y);

        Image image = buttonGo.GetComponent<Image>();
        image.color = offer.isSoldOut ? soldOutOfferButtonColor : offerButtonColor;
        image.sprite = offerButtonSprite;
        image.type = offerButtonSprite != null ? Image.Type.Sliced : Image.Type.Simple;

        BuildOfferButtonIcon(rect, offer);

        float labelLeft = offerIconOffset.x + offerIconSize.x + 10f;
        float labelWidth = Mathf.Max(80f, buttonWidth - labelLeft - 12f);
        TextMeshProUGUI label = CreateText(rect, "Label", new Vector2(labelLeft, -10f), new Vector2(labelWidth, offerButtonSize.y - 20f), buttonFontSize, offerButtonTextColor, BuildOfferButtonLabel(offer, ResolveDisplayPrice(offer)), true);
        label.enableWordWrapping = true;
        label.raycastTarget = false;

        Button button = buttonGo.GetComponent<Button>();
        button.onClick.AddListener(() => SelectOffer(offer));
        return button;
    }

    private void RefreshOfferButtonVisuals()
    {
        for (int i = 0; i < offerButtons.Count; i++)
        {
            Button button = offerButtons[i];
            if (button == null)
                continue;

            MerchantOfferState matchedOffer = null;
            offerLookup.TryGetValue(button, out matchedOffer);

            bool isSelected = matchedOffer != null && matchedOffer == selectedOffer;
            Image image = button.GetComponent<Image>();
            if (image != null)
            {
                image.sprite = isSelected && selectedOfferButtonSprite != null ? selectedOfferButtonSprite : offerButtonSprite;
                image.type = image.sprite != null ? Image.Type.Sliced : Image.Type.Simple;
                image.color = matchedOffer != null && matchedOffer.isSoldOut
                    ? soldOutOfferButtonColor
                    : (isSelected ? selectedOfferButtonColor : offerButtonColor);
            }

            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
                label.color = isSelected ? selectedOfferButtonTextColor : offerButtonTextColor;
        }
    }

    private void SetBuyButtonInteractable(bool interactable)
    {
        if (buyButton == null)
            return;

        buyButton.interactable = interactable;
        Image image = buyButton.GetComponent<Image>();
        if (image != null)
            image.color = interactable ? actionButtonColor : disabledButtonColor;
    }

    private void RefreshRefreshButtonState()
    {
        if (refreshButton == null)
            return;

        int refreshCost = ResolveRefreshCost();

        TMP_Text label = refreshButton.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
        {
            label.text = refreshCost > 0
                ? $"{refreshButtonLabel}\n{refreshCost} 金钱"
                : refreshButtonLabel;
        }

        bool hasOffer = false;
        if (currentTile != null && currentTile.merchantOffers != null)
        {
            for (int i = 0; i < currentTile.merchantOffers.Count; i++)
            {
                if (currentTile.merchantOffers[i] != null)
                {
                    hasOffer = true;
                    break;
                }
            }
        }

        bool interactable =
            refreshHandler != null &&
            currentTile != null &&
            hasOffer &&
            currentRunState != null &&
            currentRunState.money >= refreshCost;

        refreshButton.interactable = interactable;
        Image image = refreshButton.GetComponent<Image>();
        if (image != null)
            image.color = interactable ? actionButtonColor : disabledButtonColor;
    }

    private int ResolveRefreshCost()
    {
        return refreshCostResolver != null && currentTile != null
            ? Mathf.Max(0, refreshCostResolver.Invoke(currentTile))
            : 0;
    }

    private void ApplyFonts()
    {
        TMP_FontAsset keyword = keywordFont != null ? keywordFont : bodyFont;
        TMP_FontAsset body = bodyFont != null ? bodyFont : keywordFont;

        foreach (TMP_Text text in GetComponentsInChildren<TMP_Text>(true))
        {
            if (text == null)
                continue;

            text.font = body;
            if (text == titleText || text == moneyText || text == goodsNameText || text == metaText)
                text.font = keyword;
            text.UpdateMeshPadding();
            text.SetAllDirty();
        }
    }

    private void ApplyPanelVisual()
    {
        if (panelImage == null)
            return;

        if (panelBackgroundSprite != null)
        {
            panelImage.sprite = panelBackgroundSprite;
            panelImage.type = Image.Type.Sliced;
            panelImage.color = Color.white;
        }
        else
        {
            panelImage.sprite = null;
            panelImage.type = Image.Type.Simple;
            panelImage.color = panelBackgroundColor;
        }
    }

    private RectTransform CreatePanel(Transform parent, string name, Vector2 pivot, Vector2 anchoredPosition, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
        return rect;
    }

    private void BuildOfferScrollArea()
    {
        GameObject scrollGo = new GameObject("OfferScrollRect", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
        scrollGo.transform.SetParent(panelRect, false);

        RectTransform scrollRect = scrollGo.GetComponent<RectTransform>();
        scrollRect.anchorMin = new Vector2(0f, 1f);
        scrollRect.anchorMax = new Vector2(0f, 1f);
        scrollRect.pivot = new Vector2(0f, 1f);
        scrollRect.anchoredPosition = offerListOffset;
        scrollRect.sizeDelta = offerListViewportSize;

        Image scrollImage = scrollGo.GetComponent<Image>();
        scrollImage.color = Color.clear;

        offerScrollRect = scrollGo.GetComponent<ScrollRect>();
        offerScrollRect.horizontal = false;
        offerScrollRect.vertical = true;
        offerScrollRect.scrollSensitivity = 30f;
        offerScrollRect.movementType = ScrollRect.MovementType.Clamped;

        float viewportWidth = GetOfferListContentWidth();

        GameObject viewportGo = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
        viewportGo.transform.SetParent(scrollRect, false);
        offerViewport = viewportGo.GetComponent<RectTransform>();
        offerViewport.anchorMin = new Vector2(0f, 1f);
        offerViewport.anchorMax = new Vector2(0f, 1f);
        offerViewport.pivot = new Vector2(0f, 1f);
        offerViewport.anchoredPosition = Vector2.zero;
        offerViewport.sizeDelta = new Vector2(viewportWidth, offerListViewportSize.y);

        listRoot = new GameObject("OfferList", typeof(RectTransform)).GetComponent<RectTransform>();
        listRoot.SetParent(offerViewport, false);
        listRoot.anchorMin = new Vector2(0f, 1f);
        listRoot.anchorMax = new Vector2(0f, 1f);
        listRoot.pivot = new Vector2(0f, 1f);
        listRoot.anchoredPosition = Vector2.zero;
        listRoot.sizeDelta = new Vector2(viewportWidth, offerListViewportSize.y);

        offerScrollRect.viewport = offerViewport;
        offerScrollRect.content = listRoot;

        GameObject scrollbarGo = new GameObject("VerticalScrollbar", typeof(RectTransform), typeof(Image), typeof(Scrollbar));
        scrollbarGo.transform.SetParent(scrollRect, false);
        RectTransform scrollbarRect = scrollbarGo.GetComponent<RectTransform>();
        scrollbarRect.anchorMin = new Vector2(1f, 0f);
        scrollbarRect.anchorMax = new Vector2(1f, 1f);
        scrollbarRect.pivot = new Vector2(1f, 1f);
        scrollbarRect.anchoredPosition = Vector2.zero;
        scrollbarRect.sizeDelta = new Vector2(offerListScrollbarWidth, 0f);

        Image scrollbarTrackImage = scrollbarGo.GetComponent<Image>();
        scrollbarTrackImage.color = offerListScrollbarTrackColor;
        scrollbarTrackImage.type = Image.Type.Sliced;

        GameObject slidingAreaGo = new GameObject("SlidingArea", typeof(RectTransform));
        slidingAreaGo.transform.SetParent(scrollbarRect, false);
        RectTransform slidingAreaRect = slidingAreaGo.GetComponent<RectTransform>();
        slidingAreaRect.anchorMin = Vector2.zero;
        slidingAreaRect.anchorMax = Vector2.one;
        slidingAreaRect.offsetMin = new Vector2(2f, 2f);
        slidingAreaRect.offsetMax = new Vector2(-2f, -2f);

        GameObject handleGo = new GameObject("Handle", typeof(RectTransform), typeof(Image));
        handleGo.transform.SetParent(slidingAreaRect, false);
        RectTransform handleRect = handleGo.GetComponent<RectTransform>();
        handleRect.anchorMin = new Vector2(0f, 1f);
        handleRect.anchorMax = new Vector2(1f, 1f);
        handleRect.pivot = new Vector2(0.5f, 1f);
        handleRect.sizeDelta = new Vector2(0f, 64f);

        Image handleImage = handleGo.GetComponent<Image>();
        handleImage.color = offerListScrollbarHandleColor;
        handleImage.type = Image.Type.Sliced;

        Scrollbar scrollbar = scrollbarGo.GetComponent<Scrollbar>();
        scrollbar.direction = Scrollbar.Direction.BottomToTop;
        scrollbar.targetGraphic = handleImage;
        scrollbar.handleRect = handleRect;

        offerScrollRect.verticalScrollbar = scrollbar;
        offerScrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
        offerScrollRect.viewport = offerViewport;
    }

    private Image BuildOfferButtonIcon(RectTransform parent, MerchantOfferState offer)
    {
        GameObject iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconGo.transform.SetParent(parent, false);

        RectTransform rect = iconGo.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = offerIconOffset;
        rect.sizeDelta = offerIconSize;

        Image image = iconGo.GetComponent<Image>();
        image.sprite = offer.DisplayIcon != null ? offer.DisplayIcon : fallbackIconSprite;
        image.preserveAspect = true;
        image.raycastTarget = false;
        return image;
    }

    private Image BuildIcon(RectTransform parent)
    {
        GameObject iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconGo.transform.SetParent(parent, false);

        RectTransform rect = iconGo.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(0f, -6f);
        rect.sizeDelta = detailIconSize;

        Image image = iconGo.GetComponent<Image>();
        image.preserveAspect = true;
        return image;
    }

    private Button BuildActionButton(RectTransform parent, string name, Vector2 anchoredPosition, string label, UnityEngine.Events.UnityAction onClick)
    {
        GameObject buttonGo = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonGo.transform.SetParent(parent, false);

        RectTransform rect = buttonGo.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(1f, 0f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = actionButtonSize;

        Image image = buttonGo.GetComponent<Image>();
        image.color = actionButtonColor;
        image.sprite = actionButtonSprite;
        image.type = actionButtonSprite != null ? Image.Type.Sliced : Image.Type.Simple;

        TextMeshProUGUI text = CreateText(rect, "Label", Vector2.zero, actionButtonSize, buttonFontSize, actionButtonTextColor, label, true);
        text.rectTransform.anchorMin = Vector2.zero;
        text.rectTransform.anchorMax = Vector2.one;
        text.rectTransform.offsetMin = Vector2.zero;
        text.rectTransform.offsetMax = Vector2.zero;
        text.alignment = TextAlignmentOptions.Center;

        Button button = buttonGo.GetComponent<Button>();
        button.onClick.AddListener(onClick);
        return button;
    }

    private MerchantOfferState ResolveSelectableOffer(MerchantOfferState preferredOffer)
    {
        if (preferredOffer != null && currentTile != null && currentTile.merchantOffers != null)
        {
            for (int i = 0; i < currentTile.merchantOffers.Count; i++)
            {
                if (currentTile.merchantOffers[i] == preferredOffer)
                    return preferredOffer;
            }
        }

        return GetFirstOffer();
    }

    private TextMeshProUGUI CreateText(RectTransform parent, string name, Vector2 anchoredPosition, Vector2 size, int fontSize, Color color, string value, bool useKeywordFont = false)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        TextMeshProUGUI text = go.GetComponent<TextMeshProUGUI>();
        text.font = useKeywordFont && keywordFont != null ? keywordFont : (bodyFont != null ? bodyFont : keywordFont);
        text.fontSize = fontSize;
        text.color = color;
        text.text = value;
        text.alignment = TextAlignmentOptions.TopLeft;
        text.enableWordWrapping = false;
        text.raycastTarget = false;
        return text;
    }

    private static string GetGoodsTypeLabel(MerchantItemType itemType)
    {
        switch (itemType)
        {
            case MerchantItemType.Loot:
                return "战利品";
            case MerchantItemType.Weapon:
                return "武器";
            case MerchantItemType.Blueprint:
                return "图纸";
            case MerchantItemType.Relic:
                return "遗物";
            default:
                return "商品";
        }
    }

    private int ResolveDisplayPrice(MerchantOfferState offer)
    {
        return currentRunState != null && offer != null
            ? RelicMerchantService.CalculateOfferPrice(currentRunState, currentTile, offer)
            : Mathf.Max(0, offer != null ? offer.price : 0);
    }

    private static string BuildOfferButtonLabel(MerchantOfferState offer, int resolvedPrice)
    {
        string soldOutLabel = offer.isSoldOut ? " [已售罄]" : string.Empty;
        int originalPrice = Mathf.Max(0, offer.price);
        if (resolvedPrice != originalPrice)
        {
            return $"{offer.DisplayName}\n<color=#FF6B6B><s>{originalPrice} 金钱</s></color> {resolvedPrice} 金钱{soldOutLabel}";
        }

        return $"{offer.DisplayName}\n{resolvedPrice} 金钱{soldOutLabel}";
    }

    private static string BuildDetailMetaLabel(MerchantOfferState offer, string kindLabel, string rarityLabel, int resolvedPrice, string soldOutLabel)
    {
        int originalPrice = Mathf.Max(0, offer != null ? offer.price : 0);
        string raritySection = string.IsNullOrWhiteSpace(rarityLabel) ? string.Empty : $" | {rarityLabel}";
        if (resolvedPrice != originalPrice)
        {
            return $"{kindLabel}{raritySection} | <color=#FF6B6B><s>{originalPrice} 金钱</s></color> {resolvedPrice} 金钱{soldOutLabel}";
        }

        return $"{kindLabel}{raritySection} | {resolvedPrice} 金钱{soldOutLabel}";
    }

    private static string ResolveOfferRarityLabel(MerchantOfferState offer)
    {
        if (offer == null)
            return string.Empty;

        switch (offer.itemType)
        {
            case MerchantItemType.Weapon:
                return GetWeaponRarityLabel(offer.weaponData != null ? offer.weaponData.rarity : Rarity.Common);
            case MerchantItemType.Loot:
            case MerchantItemType.Blueprint:
            case MerchantItemType.Relic:
                return GetLootRarityLabel(ResolveOfferLootRarity(offer));
            default:
                return string.Empty;
        }
    }

    private static Color ResolveOfferNameColor(MerchantOfferState offer)
    {
        if (offer == null)
            return Color.white;

        switch (offer.itemType)
        {
            case MerchantItemType.Weapon:
                return GetWeaponRarityColor(offer.weaponData != null ? offer.weaponData.rarity : Rarity.Common);
            case MerchantItemType.Loot:
            case MerchantItemType.Blueprint:
            case MerchantItemType.Relic:
                return GetLootRarityColor(ResolveOfferLootRarity(offer));
            default:
                return Color.white;
        }
    }

    private static LootRarity ResolveOfferLootRarity(MerchantOfferState offer)
    {
        if (offer == null)
            return LootRarity.Common;

        switch (offer.itemType)
        {
            case MerchantItemType.Loot:
                return offer.lootItem != null ? offer.lootItem.rarity : LootRarity.Common;
            case MerchantItemType.Blueprint:
                return offer.blueprintData != null ? offer.blueprintData.rarity : LootRarity.Common;
            case MerchantItemType.Relic:
                return offer.relicData != null ? offer.relicData.rarity : LootRarity.Common;
            default:
                return LootRarity.Common;
        }
    }

    private static string GetLootRarityLabel(LootRarity rarity)
    {
        switch (rarity)
        {
            case LootRarity.Common:
                return "普通";
            case LootRarity.Uncommon:
                return "优秀";
            case LootRarity.Rare:
                return "稀有";
            case LootRarity.Epic:
                return "史诗";
            case LootRarity.Legendary:
                return "传说";
            default:
                return rarity.ToString();
        }
    }

    private static string GetWeaponRarityLabel(Rarity rarity)
    {
        switch (rarity)
        {
            case Rarity.Common:
                return "普通";
            case Rarity.Uncommon:
                return "优秀";
            case Rarity.Rare:
                return "稀有";
            case Rarity.Epic:
                return "史诗";
            case Rarity.Legendary:
                return "传说";
            default:
                return rarity.ToString();
        }
    }

    private static Color GetLootRarityColor(LootRarity rarity)
    {
        switch (rarity)
        {
            case LootRarity.Common:
                return new Color32(128, 128, 128, 255);
            case LootRarity.Uncommon:
                return new Color32(56, 178, 86, 255);
            case LootRarity.Rare:
                return new Color32(64, 126, 255, 255);
            case LootRarity.Epic:
                return new Color32(163, 92, 255, 255);
            case LootRarity.Legendary:
                return new Color32(230, 180, 44, 255);
            default:
                return Color.white;
        }
    }

    private static Color GetWeaponRarityColor(Rarity rarity)
    {
        switch (rarity)
        {
            case Rarity.Common:
                return new Color32(128, 128, 128, 255);
            case Rarity.Uncommon:
                return new Color32(56, 178, 86, 255);
            case Rarity.Rare:
                return new Color32(64, 126, 255, 255);
            case Rarity.Epic:
                return new Color32(163, 92, 255, 255);
            case Rarity.Legendary:
                return new Color32(230, 180, 44, 255);
            default:
                return Color.white;
        }
    }

    private float GetOfferListContentWidth()
    {
        return Mathf.Max(120f, offerListViewportSize.x - offerListScrollbarWidth - 8f);
    }
}

