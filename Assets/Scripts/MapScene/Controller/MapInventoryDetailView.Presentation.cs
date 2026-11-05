using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public partial class MapInventoryDetailView
{
    private const float DescriptionSize = 28;
    private const float AdditionalDescriptionSize = 26;

    private static readonly Color DetailIvory = new Color32(242, 231, 200, 255);
    private static readonly Color DetailGold = new Color32(220, 182, 97, 255);
    private TMP_FontAsset detailHeadingFont, detailBodyFont;
    private TextMeshProUGUI categoryText, quantityText, headingText, descriptionHeading, extraHeading, closeLabel, actionLabel;
    private RectTransform modalRoot, detailSafeArea, detailRect, titlePlaque, descriptionWell, footerRule;
    private Image detailBackdrop;
    private CanvasGroup panelGroup;
    private ScrollRect descriptionScroll;
    private Scrollbar descriptionScrollbar;
    private Button closeXButton;
    private MapItemDetailFrameGraphic rarityFace, categoryFace, displayEngraving;
    private MapCraftingFrameGraphic actionFace;
    private UnityEngine.Object displayedItem;
    private Vector2 layoutViewport, layoutCanvasSize;
    private Rect layoutSafeArea;
    private float detailScale = 1;
    private bool hasDescriptionLayout;
    public event Action Hidden;
    public RectTransform ModalRoot => modalRoot;
    public Image Backdrop => detailBackdrop;
    public bool IsVisible => modalRoot != null ? modalRoot.gameObject.activeSelf : gameObject.activeSelf;

    /// <summary>Both inventory and battle settlement use this same runtime view.</summary>
    public static MapInventoryDetailView Create(RectTransform parent, string name)
    {
        RectTransform modal = DetailRect(parent, name + "Modal");
        StretchDetail(modal);
        modal.gameObject.SetActive(false);
        Image backdrop = modal.gameObject.AddComponent<Image>();
        backdrop.color = new Color(0.004f, 0.012f, 0.008f, 0.6f);
        RectTransform safe = DetailRect(modal, "ItemDetailSafeArea"); StretchDetail(safe);
        RectTransform panel = DetailRect(safe, name);
        panel.anchorMin = panel.anchorMax = panel.pivot = Vector2.one * 0.5f;
        panel.sizeDelta = new Vector2(1000, 560);
        // The transparent image blocks clicks inside the window; painted ornaments do not intercept them.
        Image blocker = panel.gameObject.AddComponent<Image>(); blocker.color = Color.clear;
        panel.gameObject.AddComponent<Button>().transition = Selectable.Transition.None;
        MapInventoryDetailView view = panel.gameObject.AddComponent<MapInventoryDetailView>();
        view.modalRoot = modal; view.detailSafeArea = safe; view.detailRect = panel; view.detailBackdrop = backdrop;
        view.panelGroup = panel.gameObject.AddComponent<CanvasGroup>();
        view.BuildPresentation();
        Button backdropButton = modal.gameObject.AddComponent<Button>();
        backdropButton.transition = Selectable.Transition.None;
        backdropButton.onClick.AddListener(view.Hide);
        modal.gameObject.SetActive(false);
        return view;
    }

    private void BuildPresentation()
    {
        RectTransform surface = DetailRect(detailRect, "ItemDetailFrame"); StretchDetail(surface);
        DetailFace(surface, MapCraftingFrameGraphic.Style.Window);
        titlePlaque = DetailRect(detailRect, "TitlePlaque"); PlaceDetail(titlePlaque, 298, -18, 404, 70);
        DetailCustomFace(titlePlaque, MapItemDetailFrameGraphic.DetailStyle.Title);
        headingText = DetailText(titlePlaque, "Heading", 36, DetailGold, GameText.Get("ui.map_inventory_detail_view.item_details", "物品详情"));
        StretchDetail(headingText.rectTransform, 44, 7); headingText.alignment = TextAlignmentOptions.Midline;
        closeXButton = DetailButton(detailRect, "CloseXButton", "×", false, Hide, out _);
        PlaceDetail((RectTransform)closeXButton.transform, 920, 20, 58, 58);
        RectTransform rule = DetailRect(detailRect, "ColumnRule"); PlaceDetail(rule, 416, 66, 6, 376); DetailRule(rule);
        RectTransform display = DetailRect(detailRect, "DisplayEngraving"); PlaceDetail(display, 48, 66, 344, 330);
        displayEngraving = DetailCustomFace(display, MapItemDetailFrameGraphic.DetailStyle.Engraving);
        iconImage = DetailRect(detailRect, "Icon").gameObject.AddComponent<Image>();
        iconImage.raycastTarget = false; iconImage.preserveAspect = true; MapItemIconFit.Attach(iconImage);
        PlaceDetail(iconImage.rectTransform, 94, 100, 252, 252);
        RectTransform quantity = DetailRect(detailRect, "QuantityBadge"); PlaceDetail(quantity, 122, 400, 196, 44);
        DetailFace(quantity, MapCraftingFrameGraphic.Style.Badge);
        quantityText = DetailText(quantity, "Quantity", 26, DetailIvory, string.Empty);
        quantityText.enableAutoSizing = true; quantityText.fontSizeMin = 18; quantityText.fontSizeMax = 26;
        StretchDetail(quantityText.rectTransform, 8, 1); quantityText.alignment = TextAlignmentOptions.Midline;
        titleText = DetailText(detailRect, "Title", 48, DetailIvory, string.Empty);
        PlaceDetail(titleText.rectTransform, 460, 62, 432, 78);
        titleText.enableAutoSizing = true; titleText.fontSizeMin = 24; titleText.fontSizeMax = 48;
        titleText.enableWordWrapping = true;
        titleText.alignment = TextAlignmentOptions.MidlineLeft;
        RectTransform titleRule = DetailRect(detailRect, "NameRule"); PlaceDetail(titleRule, 460, 142, 470, 6); DetailRule(titleRule);
        RectTransform rarity = DetailRect(detailRect, "RarityBadge"); PlaceDetail(rarity, 460, 164, 164, 46);
        rarityFace = DetailCustomFace(rarity, MapItemDetailFrameGraphic.DetailStyle.Rarity);
        metaText = DetailText(rarity, "Meta", 28, DetailIvory, string.Empty);
        StretchDetail(metaText.rectTransform, 9, 0); metaText.alignment = TextAlignmentOptions.Midline;
        RectTransform category = DetailRect(detailRect, "CategoryBadge"); PlaceDetail(category, 648, 164, 200, 46);
        categoryFace = DetailCustomFace(category, MapItemDetailFrameGraphic.DetailStyle.Category);
        categoryText = DetailText(category, "Category", 28, DetailGold, string.Empty);
        StretchDetail(categoryText.rectTransform, 32, 0); categoryText.alignment = TextAlignmentOptions.Midline;
        descriptionWell = DetailRect(detailRect, "DescriptionWell"); PlaceDetail(descriptionWell, 454, 238, 492, 204);
        DetailFace(descriptionWell, MapCraftingFrameGraphic.Style.Material);
        descriptionHeading = DetailText(descriptionWell, "DescriptionHeading", 25, DetailGold, GameText.Get("ui.map_inventory_detail_view.description", "物品描述"));
        PlaceDetail(descriptionHeading.rectTransform, 22, 9, 390, 38);
        RectTransform scrollRoot = DetailRect(descriptionWell, "DescriptionScroll");
        descriptionScroll = scrollRoot.gameObject.AddComponent<ScrollRect>();
        descriptionScroll.horizontal = false; descriptionScroll.vertical = true;
        descriptionScroll.movementType = ScrollRect.MovementType.Clamped;
        descriptionScroll.scrollSensitivity = 32;
        RectTransform viewport = DetailRect(scrollRoot, "Viewport"); StretchDetail(viewport);
        viewport.gameObject.AddComponent<RectMask2D>();
        Image scrollHit = viewport.gameObject.AddComponent<Image>(); scrollHit.color = Color.clear;
        RectTransform content = DetailRect(viewport, "Content");
        content.anchorMin = new Vector2(0, 1); content.anchorMax = Vector2.one; content.pivot = new Vector2(0, 1);
        descriptionScroll.viewport = viewport; descriptionScroll.content = content;
        descriptionText = DetailText(content, "Description", DescriptionSize, DetailIvory, string.Empty);
        descriptionText.enableWordWrapping = true; descriptionText.alignment = TextAlignmentOptions.TopLeft;
        extraHeading = DetailText(content, "ExtraHeading", 24, DetailGold, GameText.Get("ui.map_inventory_detail_view.additional_notes", "补充说明"));
        relicAdditionalDescriptionText = DetailText(content, "RelicAdditionalDescription", AdditionalDescriptionSize, new Color32(188, 202, 168, 255), string.Empty);
        relicAdditionalDescriptionText.enableWordWrapping = true;
        BuildDescriptionScrollbar();
        footerRule = DetailRect(detailRect, "FooterRule"); PlaceDetail(footerRule, 36, 462, 928, 6); DetailRule(footerRule);
        actionStatusText = DetailText(detailRect, "ActionStatus", 22, new Color32(182, 197, 167, 255), string.Empty);
        actionStatusText.enableWordWrapping = true; actionStatusText.alignment = TextAlignmentOptions.Midline;
        actionButton = DetailButton(detailRect, "ActionButton", string.Empty, true, null, out actionLabel);
        actionFace = actionButton.GetComponent<MapCraftingFrameGraphic>(); actionButton.gameObject.SetActive(false);
        closeButton = DetailButton(detailRect, "CloseButton", GameText.Get("ui.close", "关闭"), false, Hide, out closeLabel);
        PlaceDetail((RectTransform)closeButton.transform, 370, 484, 260, 56);
    }

    private void BuildDescriptionScrollbar()
    {
        RectTransform track = DetailRect(descriptionWell, "DescriptionScrollbar");
        Image image = track.gameObject.AddComponent<Image>(); image.color = new Color32(19, 30, 20, 255);
        descriptionScrollbar = track.gameObject.AddComponent<Scrollbar>(); descriptionScrollbar.direction = Scrollbar.Direction.BottomToTop;
        RectTransform handle = DetailRect(track, "Handle");
        StretchDetail(handle);
        Image handleImage = handle.gameObject.AddComponent<Image>(); handleImage.color = new Color32(148, 130, 70, 255);
        descriptionScrollbar.handleRect = handle; descriptionScrollbar.targetGraphic = handleImage;
        descriptionScroll.verticalScrollbar = descriptionScrollbar;
    }

    private void ApplyPresentationFonts()
    {
        if (modalRoot == null) return;
        foreach (TMP_Text text in new TMP_Text[] { headingText, categoryText, quantityText, descriptionHeading, extraHeading, actionLabel, closeLabel })
            ApplyFont(text, detailBodyFont);
        ApplyFont(headingText, detailHeadingFont);
        ApplyFont(titleText, detailBodyFont);
        foreach (TMP_Text text in closeXButton.GetComponentsInChildren<TMP_Text>(true)) ApplyFont(text, detailBodyFont);
        RefreshDescriptionLayout(false);
    }

    private void RefreshDescriptionLayout(bool reset)
    {
        if (modalRoot == null) return;
        descriptionText.fontSize = MainMenuSettingsPersistence.DetailFontSize(DescriptionSize);
        relicAdditionalDescriptionText.fontSize = MainMenuSettingsPersistence.DetailFontSize(AdditionalDescriptionSize);
        bool action = actionButton.gameObject.activeSelf;
        bool status = actionStatusText != null && !string.IsNullOrWhiteSpace(actionStatusText.text);
        float wellHeight = status ? 177 : 204;
        PlaceDetail(descriptionWell, 454, 238, 492, wellHeight);
        PlaceDetail((RectTransform)descriptionScroll.transform, 22, 52, 444, wellHeight - 68);
        PlaceDetail((RectTransform)descriptionScrollbar.transform, 474, 54, 7, wellHeight - 74);
        float width = 438;
        float descriptionHeight = PreferredDetailHeight(descriptionText, width);
        PlaceDetail(descriptionText.rectTransform, 0, 0, width, descriptionHeight);
        bool extra = relicAdditionalDescriptionText.gameObject.activeSelf && !string.IsNullOrWhiteSpace(relicAdditionalDescriptionText.text);
        extraHeading.gameObject.SetActive(extra);
        float height = descriptionHeight;
        if (extra)
        {
            PlaceDetail(extraHeading.rectTransform, 0, height + 16, width, 36);
            float extraHeight = PreferredDetailHeight(relicAdditionalDescriptionText, width);
            PlaceDetail(relicAdditionalDescriptionText.rectTransform, 0, height + 56, width, extraHeight);
            height += 56 + extraHeight;
        }
        descriptionScroll.content.sizeDelta = new Vector2(0, height);
        descriptionScrollbar.gameObject.SetActive(height > wellHeight - 68 + 1);
        actionStatusText.gameObject.SetActive(status);
        PlaceDetail(actionStatusText.rectTransform, 446, 418, 502, 38);
        PlaceDetail((RectTransform)actionButton.transform, 514, 484, 260, 56);
        PlaceDetail((RectTransform)closeButton.transform, action ? 226 : 370, 484, 260, 56);
        if (reset || !hasDescriptionLayout)
        {
            descriptionScroll.StopMovement();
            descriptionScroll.content.anchoredPosition = Vector2.zero;
            descriptionScroll.verticalNormalizedPosition = 1;
        }
        hasDescriptionLayout = true;
    }

    private static float PreferredDetailHeight(TMP_Text text, float width)
    {
        // Factories configure fonts while the modal is inactive, before TMP's first Awake.
        if (text.font == null || text.fontSharedMaterial == null) return 36;
        return Mathf.Max(36, text.GetPreferredValues(text.text, width, 0).y + 8);
    }

    public void ApplyViewport(Vector2 viewport, Rect safeArea)
    {
        if (modalRoot == null) return;
        layoutViewport = viewport; layoutSafeArea = safeArea;
        float w = Mathf.Max(1, viewport.x), h = Mathf.Max(1, viewport.y);
        detailSafeArea.anchorMin = new Vector2(safeArea.xMin / w, safeArea.yMin / h);
        detailSafeArea.anchorMax = new Vector2(safeArea.xMax / w, safeArea.yMax / h);
        detailSafeArea.offsetMin = detailSafeArea.offsetMax = Vector2.zero;
        float parentWidth = modalRoot.rect.width > 1 ? modalRoot.rect.width : 1920;
        float parentHeight = modalRoot.rect.height > 1 ? modalRoot.rect.height : 1080;
        layoutCanvasSize = modalRoot.rect.size;
        float availableWidth = parentWidth * safeArea.width / w, availableHeight = parentHeight * safeArea.height / h;
        detailScale = Mathf.Min(1.2f, Mathf.Min((availableWidth - 64) / 1000, (availableHeight - 100) / 578));
        detailScale = Mathf.Max(0.05f, detailScale);
        ApplyDetailMotion();
    }

    private static RectTransform DetailRect(Transform parent, string name)
    {
        RectTransform rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false); return rect;
    }
    private static void StretchDetail(RectTransform rect, float x = 0, float y = 0)
    {
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.pivot = Vector2.one * 0.5f;
        rect.offsetMin = new Vector2(x, y); rect.offsetMax = new Vector2(-x, -y);
    }
    private static void PlaceDetail(RectTransform rect, float x, float y, float w, float h)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0, 1); rect.pivot = Vector2.one * 0.5f;
        rect.sizeDelta = new Vector2(w, h); rect.anchoredPosition = new Vector2(x + w / 2, -y - h / 2);
    }
    private TextMeshProUGUI DetailText(Transform parent, string name, float size, Color color, string value)
    {
        TextMeshProUGUI text = DetailRect(parent, name).gameObject.AddComponent<TextMeshProUGUI>();
        text.fontSize = size; text.color = color; text.text = value; text.raycastTarget = false;
        text.richText = true; text.enableWordWrapping = false; text.overflowMode = TextOverflowModes.Truncate;
        ApplyFont(text, detailBodyFont); return text;
    }
    private static MapCraftingFrameGraphic DetailFace(RectTransform rect, MapCraftingFrameGraphic.Style style)
    {
        MapCraftingFrameGraphic face = rect.gameObject.AddComponent<MapCraftingFrameGraphic>();
        face.craftingStyle = style; face.raycastTarget = false; return face;
    }
    private static MapItemDetailFrameGraphic DetailCustomFace(RectTransform rect, MapItemDetailFrameGraphic.DetailStyle style)
    {
        MapItemDetailFrameGraphic face = rect.gameObject.AddComponent<MapItemDetailFrameGraphic>();
        face.detailStyle = style; face.raycastTarget = false; return face;
    }
    private static void DetailRule(RectTransform rect)
    {
        MapMerchantFrameGraphic rule = rect.gameObject.AddComponent<MapMerchantFrameGraphic>();
        rule.style = MapMerchantFrameGraphic.Style.Divider; rule.raycastTarget = false;
    }
    private Button DetailButton(Transform parent, string name, string label, bool primary, UnityEngine.Events.UnityAction handler, out TextMeshProUGUI text)
    {
        RectTransform rect = DetailRect(parent, name);
        MapCraftingFrameGraphic face = DetailFace(rect, primary ? MapCraftingFrameGraphic.Style.Action : MapCraftingFrameGraphic.Style.Secondary);
        face.raycastTarget = true;
        Button button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = face; button.transition = Selectable.Transition.None;
        if (handler != null) button.onClick.AddListener(handler);
        Navigation navigation = button.navigation; navigation.mode = Navigation.Mode.None; button.navigation = navigation;
        rect.gameObject.AddComponent<MapCraftingButtonFeedback>();
        text = DetailText(rect, "Label", 30, primary ? DetailIvory : new Color32(48, 33, 12, 255), label);
        StretchDetail(text.rectTransform, 12, 0); text.alignment = TextAlignmentOptions.Midline; return button;
    }
}
