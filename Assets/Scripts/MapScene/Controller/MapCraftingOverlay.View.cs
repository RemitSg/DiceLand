using TMPro;
using UnityEngine;
using UnityEngine.UI;

public partial class MapCraftingOverlay
{
    private static readonly Color Ivory = new Color32(244, 231, 195, 255);
    private static readonly Color Gold = new Color32(193, 155, 80, 255);
    private static readonly Color Muted = new Color32(159, 168, 143, 255);
    private static readonly Color Jade = new Color32(136, 220, 177, 255);
    private static readonly Color Coral = new Color32(244, 126, 108, 255);
    private static readonly Color Ink = new Color32(48, 33, 12, 255);
    private Canvas canvas;
    private Image backdrop;
    private CanvasGroup panelGroup, previewGroup;
    private RectTransform safeAreaRect, panelRect, titlePlaque, previewRoot, metaBadge, blueprintArtRoot;
    private ScrollRect blueprintScroll, materialScroll, descriptionScroll;
    private TextMeshProUGUI titleText, weaponNameText, metaText, descriptionText, availabilityText, statusText, emptyText, materialEmptyText, craftLabel;
    private Image iconImage, blueprintImage, craftIcon;
    private Button craftButton, closeButton;
    private MapCraftingFrameGraphic craftFace, engraving;
    private readonly RectTransform[] headings = new RectTransform[3];
    private readonly RectTransform[] separators = new RectTransform[2];
    private readonly RectTransform[] sparks = new RectTransform[14];
    private readonly System.Collections.Generic.HashSet<TMP_Text> keywordTexts = new System.Collections.Generic.HashSet<TMP_Text>();
    private Vector2 lastViewport, restingPosition, weaponRestingPosition;
    private UnityEngine.Rect lastSafeArea;
    private float layoutScale = 1;

    private sealed class BlueprintCard
    {
        public CraftingBlueprint_SO blueprint;
        public Button button;
        public MapCraftingFrameGraphic face;
        public TextMeshProUGUI label, readiness;
    }

    private sealed class MaterialRow
    {
        public RectTransform rect, well;
        public Image icon;
        public TextMeshProUGUI name, count;
    }

    private void EnsureUi()
    {
        if (canvas != null) return;
        // Older scenes may have serialized the compact, two-column panel size.
        panelSize = new Vector2(Mathf.Max(1480, panelSize.x), Mathf.Max(840, panelSize.y));
        RectTransform root = Rect(transform, "CraftingOverlayCanvas");
        canvas = root.gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9050;
        CanvasScaler scaler = root.gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 1;
        root.gameObject.AddComponent<GraphicRaycaster>();
        backdrop = root.gameObject.AddComponent<Image>();
        backdrop.color = new Color(0.006f, 0.014f, 0.010f, 0.62f);
        Stretch(root);
        safeAreaRect = Rect(root, "SafeArea"); Stretch(safeAreaRect);
        panelRect = Rect(safeAreaRect, "Panel");
        panelRect.anchorMin = panelRect.anchorMax = panelRect.pivot = Vector2.one * 0.5f;
        panelGroup = panelRect.gameObject.AddComponent<CanvasGroup>();
        Face(panelRect, MapCraftingFrameGraphic.Style.Window);
        titlePlaque = Rect(panelRect, "TitlePlaque");
        Face(titlePlaque, MapCraftingFrameGraphic.Style.Title);
        titleText = Text(titlePlaque, "Title", Mathf.Max(56, titleFontSize), Ivory, "锻 造", true);
        Inset(titleText.rectTransform, 55, 14);
        titleText.alignment = TextAlignmentOptions.Midline;
        Button closeX = Action(panelRect, "CloseXButton", "×", Hide, false);
        Place((RectTransform)closeX.transform, panelSize.x - 86, 20, 62, 62);
        Rule(panelRect, "HeaderRule", 30, 104, panelSize.x - 60, 8);
        string[] labels = { "图纸列表", "成品预览", "所需材料" };
        for (int i = 0; i < headings.Length; i++)
        {
            headings[i] = Rect(panelRect, "Heading_" + i);
            TextMeshProUGUI label = Text(headings[i], "Label", 32, Ivory, labels[i]);
            Stretch(label.rectTransform); label.alignment = TextAlignmentOptions.Center;
        }
        for (int i = 0; i < separators.Length; i++)
            separators[i] = Rule(panelRect, "ColumnRule_" + i, 0, 0, 8, 500);
        blueprintScroll = ScrollArea(panelRect, "BlueprintScroll");
        emptyText = Text(blueprintScroll.viewport, "EmptyBlueprints", 29, Muted, "尚未拥有图纸");
        Inset(emptyText.rectTransform, 14, 18); emptyText.alignment = TextAlignmentOptions.Center;
        blueprintArtRoot = Rect(panelRect, "BlueprintArt");
        blueprintImage = Icon(blueprintArtRoot, "BlueprintIcon", null); Stretch(blueprintImage.rectTransform);
        descriptionScroll = ScrollArea(panelRect, "DescriptionScroll");
        descriptionText = Text(descriptionScroll.content, "Description", Mathf.Max(27, bodyFontSize), Ivory, string.Empty);
        descriptionText.alignment = TextAlignmentOptions.Top;
        previewRoot = Rect(panelRect, "Preview");
        Face(previewRoot, MapCraftingFrameGraphic.Style.Preview);
        RectTransform seal = Rect(previewRoot, "EngravedSeal");
        engraving = Face(seal, MapCraftingFrameGraphic.Style.Engraving);
        engraving.raycastTarget = false;
        previewGroup = previewRoot.gameObject.AddComponent<CanvasGroup>();
        weaponNameText = Text(previewRoot, "WeaponName", 42, Ivory, string.Empty, true);
        weaponNameText.alignment = TextAlignmentOptions.Midline;
        metaBadge = Rect(previewRoot, "MetaBadge"); Face(metaBadge, MapCraftingFrameGraphic.Style.Badge);
        metaText = Text(metaBadge, "Meta", 28, Gold, string.Empty);
        Inset(metaText.rectTransform, 10, 2); metaText.alignment = TextAlignmentOptions.Midline;
        iconImage = Icon(previewRoot, "WeaponIcon", null);
        iconImage.rectTransform.pivot = Vector2.one * 0.5f;
        for (int i = 0; i < sparks.Length; i++)
        {
            sparks[i] = Rect(previewRoot, "ForgeSpark_" + i);
            Image spark = sparks[i].gameObject.AddComponent<Image>();
            spark.color = new Color(1, 0.81f, 0.36f, 0); spark.raycastTarget = false;
            sparks[i].pivot = new Vector2(0.5f, 0.5f);
        }
        materialScroll = ScrollArea(panelRect, "MaterialScroll");
        materialEmptyText = Text(materialScroll.viewport, "EmptyMaterials", 28, Muted, string.Empty);
        Inset(materialEmptyText.rectTransform, 14, 20); materialEmptyText.alignment = TextAlignmentOptions.Top;
        availabilityText = Text(panelRect, "Availability", 29, Coral, string.Empty);
        availabilityText.alignment = TextAlignmentOptions.Center;
        Rule(panelRect, "MaterialStatusRule", panelSize.x * 0.69f + 12, panelSize.y - 226, panelSize.x * 0.31f - 60, 8);
        Rule(panelRect, "FooterRule", 24, panelSize.y - 116, panelSize.x - 48, 8);
        TextMeshProUGUI note = Text(panelRect, "ConsumptionNote", 25, Muted, "锻造将消耗所列材料，图纸保留");
        Place(note.rectTransform, 46, panelSize.y - 91, 720, 36);
        statusText = Text(panelRect, "Status", 27, Jade, string.Empty);
        Place(statusText.rectTransform, 46, panelSize.y - 54, 742, 42);
        craftButton = Action(panelRect, "CraftButton", craftButtonLabel, OnCraftClicked, true);
        craftFace = craftButton.GetComponent<MapCraftingFrameGraphic>();
        craftLabel = craftButton.GetComponentInChildren<TextMeshProUGUI>();
        craftIcon = Icon(craftButton.transform, "Hammer", MapForestArt.Get("Forge"));
        craftLabel.rectTransform.anchorMin = craftLabel.rectTransform.anchorMax = craftLabel.rectTransform.pivot = new Vector2(0, 1);
        closeButton = Action(panelRect, "CloseButton", closeButtonLabel, Hide, false);
        ApplyFonts();
    }

    public void ApplyViewport(Vector2 viewport, UnityEngine.Rect safePixels)
    {
        if (canvas == null) return;
        float width = Mathf.Max(1, viewport.x), height = Mathf.Max(1, viewport.y);
        float x = Mathf.Clamp(safePixels.xMin, 0, width - 1), y = Mathf.Clamp(safePixels.yMin, 0, height - 1);
        var safe = new UnityEngine.Rect(x, y, Mathf.Clamp(safePixels.width, 1, width - x), Mathf.Clamp(safePixels.height, 1, height - y));
        safeAreaRect.anchorMin = new Vector2(safe.xMin / width, safe.yMin / height);
        safeAreaRect.anchorMax = new Vector2(safe.xMax / width, safe.yMax / height);
        safeAreaRect.offsetMin = safeAreaRect.offsetMax = Vector2.zero;
        Vector2 available = safe.size * (1080f / height);
        // Include the plaque and anvil above the main window in the safe-area fit.
        layoutScale = Mathf.Max(0.01f, Mathf.Min(1, (available.x - 48) / panelSize.x, (available.y - 48) / (panelSize.y + 78)));
        float horizontalRoom = Mathf.Max(0, (available.x - panelSize.x * layoutScale) * 0.5f - 24);
        float verticalRoom = Mathf.Max(0, (available.y - (panelSize.y + 78) * layoutScale) * 0.5f - 24);
        restingPosition = new Vector2(Mathf.Clamp(panelOffset.x, -horizontalRoom, horizontalRoom),
            Mathf.Clamp(panelOffset.y, -verticalRoom, verticalRoom) - 39 * layoutScale);
        lastViewport = viewport; lastSafeArea = safePixels;
        ApplyMotion();
    }

    private void RefreshLayout()
    {
        if (canvas == null) return;
        panelRect.sizeDelta = panelSize;
        float leftX = 36, leftWidth = panelSize.x * 0.246f;
        float centerX = leftX + leftWidth + 28, rightX = panelSize.x * 0.69f;
        float centerWidth = rightX - centerX - 28, rightWidth = panelSize.x - rightX - 36;
        float bodyBottom = panelSize.y - 138;
        Place(titlePlaque, (panelSize.x - 440) * 0.5f, -30, 440, 118);
        float[] xs = { leftX, centerX, rightX }, widths = { leftWidth, centerWidth, rightWidth };
        for (int i = 0; i < 3; i++) Place(headings[i], xs[i], 120, widths[i], 52);
        Place(separators[0], centerX - 18, 120, 8, bodyBottom - 118);
        Place(separators[1], rightX - 18, 120, 8, bodyBottom - 118);
        bool multiple = blueprintCards.Count > 1;
        float listHeight = multiple ? bodyBottom - 274 : 86;
        Place((RectTransform)blueprintScroll.transform, leftX, 188, leftWidth, listHeight);
        for (int i = 0; i < blueprintCards.Count; i++)
            Place((RectTransform)blueprintCards[i].button.transform, 3, i * 86 + 3, leftWidth - 24, 78);
        blueprintScroll.content.sizeDelta = new Vector2(0, Mathf.Max(listHeight, blueprintCards.Count * 86 - 2));
        float artTop = 200 + listHeight;
        blueprintArtRoot.gameObject.SetActive(!multiple && selectedBlueprint != null);
        Place(blueprintArtRoot, leftX + 26, artTop, leftWidth - 52, Mathf.Max(32, bodyBottom - artTop - 112));
        Place((RectTransform)descriptionScroll.transform, leftX + 4, bodyBottom - (multiple ? 78 : 100), leftWidth - 8, multiple ? 74 : 96);
        Place(previewRoot, centerX, 184, centerWidth, bodyBottom - 184);
        Place(weaponNameText.rectTransform, 14, 6, centerWidth - 28, 66);
        Place(metaBadge, (centerWidth - 190) * 0.5f, 76, 190, 44);
        Place(iconImage.rectTransform, 24, 128, centerWidth - 48, bodyBottom - 184 - 142);
        Place(engraving.rectTransform, 24, 128, centerWidth - 48, bodyBottom - 184 - 142);
        weaponRestingPosition = iconImage.rectTransform.anchoredPosition;
        Place((RectTransform)materialScroll.transform, rightX, 188, rightWidth, bodyBottom - 296);
        float materialY = 0;
        foreach (MaterialRow row in materialRows)
        {
            float nameWidth = Mathf.Max(70, rightWidth - 260);
            float nameHeight = Mathf.Max(42, row.name.GetPreferredValues(nameWidth, 0).y + 4);
            float height = Mathf.Max(116, nameHeight + 32);
            Place(row.rect, 2, materialY + 2, rightWidth - 22, height);
            Place(row.well, 8, (height - 94) * 0.5f, 94, 94);
            Place(row.icon.rectTransform, 16, (height - 80) * 0.5f, 78, 80);
            Place(row.name.rectTransform, 116, (height - nameHeight) * 0.5f, nameWidth, nameHeight);
            Place(row.count.rectTransform, rightWidth - 134, (height - 48) * 0.5f, 98, 48);
            materialY += height + 14;
        }
        materialScroll.content.sizeDelta = new Vector2(0, Mathf.Max(bodyBottom - 296, materialY - 10));
        Place(availabilityText.rectTransform, rightX + 10, bodyBottom - 80, rightWidth - 20, 70);
        Place((RectTransform)craftButton.transform, panelSize.x - 640, panelSize.y - 92, 320, 70);
        Place((RectTransform)closeButton.transform, panelSize.x - 292, panelSize.y - 92, 252, 70);
        float craftTextWidth = Mathf.Min(200, craftLabel.GetPreferredValues(craftButtonLabel).x + 12);
        float craftContentLeft = (320 - 44 - 12 - craftTextWidth) * 0.5f;
        Place(craftIcon.rectTransform, craftContentLeft, 13, 44, 44);
        Place(craftLabel.rectTransform, craftContentLeft + 56, 0, craftTextWidth, 70);
        Canvas.ForceUpdateCanvases();
        float descriptionWidth = Mathf.Max(40, descriptionScroll.viewport.rect.width - 12);
        float descriptionHeight = descriptionText.GetPreferredValues(descriptionWidth, 0).y + 8;
        Place(descriptionText.rectTransform, 6, 2, descriptionWidth, descriptionHeight);
        descriptionScroll.content.sizeDelta = new Vector2(0, Mathf.Max(multiple ? 74 : 96, descriptionHeight));
        UpdateScrollbar(blueprintScroll); UpdateScrollbar(materialScroll); UpdateScrollbar(descriptionScroll);
    }

    private BlueprintCard BuildBlueprintCard(CraftingBlueprint_SO blueprint)
    {
        RectTransform rect = Rect(blueprintScroll.content, "Blueprint_" + blueprintCards.Count.ToString("00"));
        rect.pivot = Vector2.one * 0.5f;
        MapCraftingFrameGraphic face = Face(rect, MapCraftingFrameGraphic.Style.Blueprint);
        Button button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = face;
        button.onClick.AddListener(() => SelectBlueprint(blueprint));
        button.transition = Selectable.Transition.None;
        rect.gameObject.AddComponent<MapCraftingButtonFeedback>();
        Image thumb = Icon(rect, "Thumbnail", blueprint.DisplayIcon);
        Place(thumb.rectTransform, 12, 10, 54, 58);
        TextMeshProUGUI label = Text(rect, "Label", 27, Ivory, blueprint.DisplayName);
        Place(label.rectTransform, 80, 5, panelSize.x * 0.246f - 133, 38);
        label.enableAutoSizing = true; label.fontSizeMin = 22; label.fontSizeMax = 27;
        label.enableWordWrapping = false;
        label.alignment = TextAlignmentOptions.MidlineLeft;
        TextMeshProUGUI readiness = Text(rect, "Readiness", 22, Muted, string.Empty);
        Place(readiness.rectTransform, 80, 43, panelSize.x * 0.246f - 133, 32);
        return new BlueprintCard { blueprint = blueprint, button = button, face = face, label = label, readiness = readiness };
    }

    private MaterialRow BuildMaterialRow(string name, Sprite sprite, int owned, int required)
    {
        RectTransform rect = Rect(materialScroll.content, "Material_" + materialRows.Count.ToString("00"));
        Face(rect, MapCraftingFrameGraphic.Style.Material).raycastTarget = false;
        RectTransform well = Rect(rect, "IconWell"); Face(well, MapCraftingFrameGraphic.Style.IconWell).raycastTarget = false;
        Image icon = Icon(rect, "Icon", sprite);
        TextMeshProUGUI label = Text(rect, "Name", 29, Ivory, name);
        TextMeshProUGUI count = Text(rect, "Count", 29, owned >= required ? Jade : Coral, owned + " / " + required);
        count.enableAutoSizing = true; count.fontSizeMin = 19; count.fontSizeMax = 29;
        count.enableWordWrapping = false; count.alignment = TextAlignmentOptions.MidlineRight;
        return new MaterialRow { rect = rect, well = well, icon = icon, name = label, count = count };
    }

    private Button Action(Transform parent, string name, string label, UnityEngine.Events.UnityAction callback, bool primary)
    {
        RectTransform rect = Rect(parent, name);
        rect.pivot = Vector2.one * 0.5f;
        MapCraftingFrameGraphic face = Face(rect, primary ? MapCraftingFrameGraphic.Style.Action : MapCraftingFrameGraphic.Style.Secondary);
        Button button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = face;
        button.transition = Selectable.Transition.None;
        button.onClick.AddListener(callback);
        rect.gameObject.AddComponent<MapCraftingButtonFeedback>();
        TextMeshProUGUI text = Text(rect, "Label", Mathf.Max(34, buttonFontSize), primary ? Ivory : Ink, label, true);
        Inset(text.rectTransform, 12, 4); text.alignment = TextAlignmentOptions.Midline;
        return button;
    }

    private void ApplyFonts()
    {
        if (canvas == null) return;
        foreach (TMP_Text text in canvas.GetComponentsInChildren<TMP_Text>(true))
        {
            TMP_FontAsset font = keywordTexts.Contains(text) ? keywordFont ?? bodyFont : bodyFont ?? keywordFont;
            if (font != null) text.font = font;
            text.UpdateMeshPadding(); text.SetAllDirty();
        }
    }

    private TextMeshProUGUI Text(Transform parent, string name, int size, Color color, string value, bool keyword = false)
    {
        TextMeshProUGUI text = Rect(parent, name).gameObject.AddComponent<TextMeshProUGUI>();
        TMP_FontAsset font = keyword ? keywordFont ?? bodyFont : bodyFont ?? keywordFont;
        if (font != null) text.font = font;
        if (keyword) keywordTexts.Add(text);
        text.fontSize = size; text.color = color; text.text = value;
        text.enableWordWrapping = true; text.overflowMode = TextOverflowModes.Ellipsis;
        text.alignment = TextAlignmentOptions.TopLeft; text.raycastTarget = false;
        return text;
    }

    private static ScrollRect ScrollArea(Transform parent, string name)
    {
        RectTransform rect = Rect(parent, name);
        rect.gameObject.AddComponent<Image>().color = Color.clear;
        ScrollRect scroll = rect.gameObject.AddComponent<ScrollRect>();
        RectTransform viewport = Rect(rect, "Viewport"); Stretch(viewport); viewport.offsetMax = new Vector2(-14, 0);
        viewport.gameObject.AddComponent<Image>().color = Color.clear;
        viewport.gameObject.AddComponent<RectMask2D>();
        RectTransform content = Rect(viewport, "Content");
        content.anchorMin = new Vector2(0, 1); content.anchorMax = Vector2.one; content.pivot = new Vector2(0.5f, 1);
        content.sizeDelta = Vector2.zero;
        RectTransform track = Rect(rect, "Scrollbar");
        track.anchorMin = new Vector2(1, 0); track.anchorMax = Vector2.one; track.pivot = Vector2.one;
        track.offsetMin = new Vector2(-5, 0); track.offsetMax = Vector2.zero;
        track.gameObject.AddComponent<Image>().color = new Color(Gold.r, Gold.g, Gold.b, 0.13f);
        Scrollbar scrollbar = track.gameObject.AddComponent<Scrollbar>();
        RectTransform handle = Rect(track, "Handle"); Stretch(handle);
        Image handleImage = handle.gameObject.AddComponent<Image>(); handleImage.color = Gold;
        scrollbar.handleRect = handle; scrollbar.targetGraphic = handleImage; scrollbar.direction = Scrollbar.Direction.BottomToTop;
        scroll.viewport = viewport; scroll.content = content; scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 42;
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
    private static void Place(RectTransform rect, float x, float y, float width, float height) { rect.anchoredPosition = new Vector2(x + width * rect.pivot.x, -y - height * (1 - rect.pivot.y)); rect.sizeDelta = new Vector2(width, height); }
    private static MapCraftingFrameGraphic Face(RectTransform rect, MapCraftingFrameGraphic.Style style) { MapCraftingFrameGraphic face = rect.gameObject.AddComponent<MapCraftingFrameGraphic>(); face.craftingStyle = style; return face; }
    private static RectTransform Rule(Transform parent, string name, float x, float y, float width, float height) { RectTransform rect = Rect(parent, name); MapMerchantFrameGraphic frame = rect.gameObject.AddComponent<MapMerchantFrameGraphic>(); frame.style = MapMerchantFrameGraphic.Style.Divider; frame.leadingDiamond = true; frame.raycastTarget = false; Place(rect, x, y, width, height); return rect; }
    private static Image Icon(Transform parent, string name, Sprite sprite) { Image icon = Rect(parent, name).gameObject.AddComponent<Image>(); icon.preserveAspect = true; icon.raycastTarget = false; MapItemIconFit.Attach(icon); SetIcon(icon, sprite); return icon; }
    private static void SetIcon(Image image, Sprite sprite) { image.sprite = sprite; image.enabled = sprite != null; }
}
