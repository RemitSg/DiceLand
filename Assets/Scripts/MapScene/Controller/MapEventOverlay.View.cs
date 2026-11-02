using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public partial class MapEventOverlay
{
    private static readonly Color Ivory = new Color(0.96f, 0.93f, 0.81f);
    private static readonly Color Gold = new Color(0.75f, 0.60f, 0.32f);
    private Canvas canvas;
    private RectTransform root, safeAreaRect, panelRect, headerDivider, storyAccent, optionSection, footerRect;
    private TextMeshProUGUI titleText, eventMetaText, contentText, consequenceTitleText, consequenceText, statusText;
    private ScrollRect contentScrollRect, consequenceScrollRect, optionScrollRect;
    private RectTransform contentViewportRect, contentContentRect, consequenceViewportRect, consequenceContentRect, optionListRoot;
    private Button confirmButton;
    private bool layoutDirty;
    private Vector2 lastViewport;
    private Rect lastSafeArea;
    private readonly List<OptionRow> optionRows = new List<OptionRow>();

    private sealed class OptionRow
    {
        public Button button;
        public MapEventChoiceGraphic face;
        public TextMeshProUGUI number, label, detail;
    }

    private void EnsureUi()
    {
        if (canvas != null) return;
        GameObject go = new GameObject("EventOverlayCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(Image));
        go.transform.SetParent(transform, false);
        canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = EventOverlayCanvasSortingOrder;
        CanvasScaler scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 1f;
        root = (RectTransform)go.transform;
        Stretch(root);
        go.GetComponent<Image>().color = backdropColor;
        safeAreaRect = Rect(root, "SafeArea");
        Stretch(safeAreaRect);
        panelRect = Rect(safeAreaRect, "Panel");
        panelRect.anchorMin = panelRect.anchorMax = panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = panelSize;
        Image panel = panelRect.gameObject.AddComponent<Image>();
        panel.color = panelBackgroundColor;
        Shadow shadow = panelRect.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.55f);
        shadow.effectDistance = new Vector2(6f, -10f);

        titleText = Text(panelRect, "Title", titleFontSize, Ivory, true);
        eventMetaText = Text(panelRect, "EventMeta", 28, Gold, true);
        eventMetaText.text = "旅途事件";
        eventMetaText.alignment = TextAlignmentOptions.MidlineRight;
        headerDivider = Rule(panelRect, "HeaderDivider", Gold);
        storyAccent = Rule(panelRect, "StoryAccent", Gold);
        contentScrollRect = ScrollArea(panelRect, "ContentScrollRect", out contentViewportRect, out contentContentRect);
        contentText = Text(contentContentRect, "ContentText", bodyFontSize, Ivory);
        contentText.alignment = TextAlignmentOptions.TopLeft;
        contentText.lineSpacing = 8f;
        contentText.paragraphSpacing = 12f;
        optionSection = Rect(panelRect, "OptionSection");
        TextMeshProUGUI section = Text(optionSection, "OptionSectionTitle", 29, Gold, true);
        section.text = "选择行动";
        Place(section.rectTransform, 0f, 0f, 166f, 44f);
        RectTransform divider = Glyph(optionSection, "OptionDivider", MapHudGlyph.Shape.Divider, Gold);
        Place(divider, 166f, 12f, panelSize.x - 254f, 20f);
        optionScrollRect = ScrollArea(panelRect, "OptionScrollRect", out _, out optionListRoot);
        footerRect = Rect(panelRect, "Footer");
        Image footer = footerRect.gameObject.AddComponent<Image>();
        footer.color = new Color(0.105f, 0.080f, 0.043f, 0.94f);
        footer.raycastTarget = false;
        RectTransform footerRule = Rule(footerRect, "FooterDivider", Gold);
        Place(footerRule, 0f, 0f, panelSize.x - 40f, 1f);
        statusText = Text(panelRect, "Status", 26, new Color(1f, 0.72f, 0.49f));
        consequenceTitleText = Text(panelRect, "ConsequenceTitle", 28, Gold, true);
        consequenceScrollRect = ScrollArea(panelRect, "ConsequenceScrollRect", out consequenceViewportRect, out consequenceContentRect);
        consequenceText = Text(consequenceContentRect, "ConsequenceText", 28, Ivory);
        consequenceText.alignment = TextAlignmentOptions.TopLeft;
        confirmButton = BuildActionButton();

        RectTransform frame = Rect(panelRect, "PanelFrame");
        Stretch(frame);
        MapEventFrameGraphic border = frame.gameObject.AddComponent<MapEventFrameGraphic>();
        border.raycastTarget = false;
        ApplyFonts();
        layoutDirty = true;
    }

    /// <summary>Shared by screen UI, isolated previews and safe-area checks. Units are based on 1080 screen height.</summary>
    public void ApplyViewport(Vector2 viewport, Rect safePixels)
    {
        if (canvas == null) return;
        float width = Mathf.Max(1f, viewport.x), height = Mathf.Max(1f, viewport.y);
        float x = Mathf.Clamp(safePixels.xMin, 0f, width - 1f), y = Mathf.Clamp(safePixels.yMin, 0f, height - 1f);
        Rect safe = new Rect(x, y, Mathf.Clamp(safePixels.width, 1f, width - x), Mathf.Clamp(safePixels.height, 1f, height - y));
        safeAreaRect.anchorMin = new Vector2(safe.xMin / width, safe.yMin / height);
        safeAreaRect.anchorMax = new Vector2(safe.xMax / width, safe.yMax / height);
        safeAreaRect.offsetMin = safeAreaRect.offsetMax = Vector2.zero;
        Vector2 available = safe.size * (1080f / height);
        float scale = Mathf.Min(1f, (available.x - 48f) / panelSize.x, (available.y - 48f) / panelSize.y);
        scale = Mathf.Max(0.01f, scale);
        panelRect.localScale = Vector3.one * scale;
        Vector2 limit = Vector2.Max(Vector2.zero, (available - panelSize * scale) * 0.5f - Vector2.one * 24f);
        panelRect.anchoredPosition = new Vector2(Mathf.Clamp(panelOffset.x, -limit.x, limit.x), Mathf.Clamp(panelOffset.y, -limit.y, limit.y));
        lastViewport = viewport;
        lastSafeArea = safePixels;
        layoutDirty = true;
    }

    private Button BuildOptionButton(MapEventOptionData option, EventOptionPreviewData preview, int index)
    {
        RectTransform rect = Rect(optionListRoot, "Option_" + index.ToString("00"));
        MapEventChoiceGraphic face = rect.gameObject.AddComponent<MapEventChoiceGraphic>();
        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = face;
        button.interactable = preview == null || preview.isSelectable;
        button.onClick.AddListener(() => SelectOption(option));
        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(1.12f, 1.12f, 1.06f);
        colors.pressedColor = new Color(0.78f, 0.87f, 0.80f);
        colors.disabledColor = new Color(0.70f, 0.70f, 0.70f);
        button.colors = colors;
        OptionRow row = new OptionRow { button = button, face = face };
        row.number = Text(rect, "Number", 28, Gold, true);
        row.number.text = index.ToString("00");
        row.number.alignment = TextAlignmentOptions.Midline;
        row.label = Text(rect, "Label", buttonFontSize, Ivory, true);
        row.label.text = FormatOptionLabel(option, preview);
        row.detail = Text(rect, "Detail", 28, Ivory);
        row.detail.text = FormatOptionDetail(option, preview);
        optionRows.Add(row);
        return button;
    }

    private void RefreshOptionButtonVisuals()
    {
        foreach (OptionRow row in optionRows)
        {
            bool selected = optionLookup.TryGetValue(row.button, out MapEventOptionData option) && option == selectedOption;
            row.face.SetState(selected, row.button.interactable);
            float alpha = row.button.interactable ? 1f : 0.64f;
            row.number.color = new Color(Gold.r, Gold.g, Gold.b, alpha);
            row.label.color = new Color(Ivory.r, Ivory.g, Ivory.b, alpha);
            Color detail = selected ? new Color(0.88f, 0.94f, 0.86f) : Ivory;
            row.detail.color = new Color(detail.r, detail.g, detail.b, alpha);
        }
    }

    private void RefreshLayout()
    {
        if (canvas == null) return;
        float width = Mathf.Max(960f, panelSize.x), height = Mathf.Max(800f, panelSize.y);
        panelRect.sizeDelta = new Vector2(width, height);
        float titleHeight = Mathf.Max(70f, PreferredHeight(titleText, width - 326f) + 4f);
        float headerHeight = Mathf.Max(104f, titleHeight + 42f);
        Place(titleText.rectTransform, 52f, 22f, width - 326f, titleHeight);
        Place(eventMetaText.rectTransform, width - 250f, 38f, 196f, 48f);
        Place(headerDivider, 30f, headerHeight, width - 60f, 1.5f);

        float rowsHeight = 0f, rowWidth = width - 82f;
        foreach (OptionRow row in optionRows)
        {
            float labelHeight = PreferredHeight(row.label, 240f);
            float detailHeight = PreferredHeight(row.detail, rowWidth - 444f);
            float rowHeight = Mathf.Max(82f, Mathf.Max(labelHeight, detailHeight) + 24f);
            bool tall = rowHeight > 110f;
            row.number.alignment = tall ? TextAlignmentOptions.Top : TextAlignmentOptions.Midline;
            row.label.alignment = tall ? TextAlignmentOptions.TopLeft : TextAlignmentOptions.MidlineLeft;
            row.detail.alignment = tall ? TextAlignmentOptions.TopLeft : TextAlignmentOptions.MidlineLeft;
            Place((RectTransform)row.button.transform, 0f, rowsHeight, rowWidth, rowHeight);
            Place(row.number.rectTransform, 10f, 10f, 74f, rowHeight - 20f);
            Place(row.label.rectTransform, 112f, 10f, 240f, rowHeight - 20f);
            Place(row.detail.rectTransform, 382f, 10f, rowWidth - 444f, rowHeight - 20f);
            rowsHeight += rowHeight + 10f;
        }
        rowsHeight = Mathf.Max(0f, rowsHeight - 10f);
        float statusHeight = string.IsNullOrWhiteSpace(statusText.text) ? 0f : PreferredHeight(statusText, width - 88f) + 14f;
        float footerTop = height - 118f - statusHeight;
        float optionsHeight = Mathf.Clamp(rowsHeight, 82f, 230f);
        float optionsTop = footerTop - 18f - optionsHeight;
        float contentTop = headerHeight + 26f;
        float contentBottom = isShowingResultStep ? footerTop - 24f : optionsTop - 58f;
        Place(contentScrollRect.GetComponent<RectTransform>(), 66f, contentTop, width - 114f, Mathf.Max(80f, contentBottom - contentTop));
        Place(storyAccent, 44f, contentTop + 4f, 2f, Mathf.Min(184f, Mathf.Max(60f, contentBottom - contentTop - 10f)));
        Place(optionSection, 44f, optionsTop - 54f, width - 88f, 44f);
        Place(optionScrollRect.GetComponent<RectTransform>(), 32f, optionsTop, width - 64f, optionsHeight);
        optionListRoot.sizeDelta = new Vector2(0f, Mathf.Max(optionsHeight, rowsHeight));
        Place(footerRect, 20f, footerTop, width - 40f, height - footerTop - 20f);
        Place(statusText.rectTransform, 44f, footerTop + 6f, width - 88f, statusHeight);
        statusText.gameObject.SetActive(statusHeight > 0f);
        float previewTop = footerTop + statusHeight;
        Place(consequenceTitleText.rectTransform, 46f, previewTop + 7f, 240f, 36f);
        Place(consequenceScrollRect.GetComponent<RectTransform>(), 46f, previewTop + 47f, width - 370f, 52f);
        Place((RectTransform)confirmButton.transform, width - 294f, previewTop + 27f, 246f, 68f);
        Canvas.ForceUpdateCanvases();
        FitText(contentText, contentContentRect, contentViewportRect);
        FitText(consequenceText, consequenceContentRect, consequenceViewportRect);
        UpdateScrollbar(contentScrollRect);
        UpdateScrollbar(consequenceScrollRect);
        UpdateScrollbar(optionScrollRect);
        layoutDirty = false;
    }

    private static void UpdateScrollbar(ScrollRect scroll)
    {
        float contentHeight = scroll.content.rect.height;
        float viewportHeight = scroll.viewport.rect.height;
        scroll.verticalScrollbar.gameObject.SetActive(contentHeight > viewportHeight + 0.5f);
        scroll.verticalScrollbar.size = contentHeight > 0f ? Mathf.Clamp01(viewportHeight / contentHeight) : 1f;
    }

    private static float PreferredHeight(TMP_Text text, float width)
    {
        return text.GetPreferredValues(text.text, Mathf.Max(40f, width), 0f).y;
    }

    private static void FitText(TextMeshProUGUI text, RectTransform content, RectTransform viewport)
    {
        float width = Mathf.Max(40f, viewport.rect.width - 8f);
        float height = Mathf.Max(viewport.rect.height, PreferredHeight(text, width) + 12f);
        content.sizeDelta = new Vector2(0f, height);
        Place(text.rectTransform, 0f, 0f, width, height);
        text.ForceMeshUpdate();
    }

    private ScrollRect ScrollArea(RectTransform parent, string name, out RectTransform viewport, out RectTransform content)
    {
        RectTransform rect = Rect(parent, name);
        Image hitArea = rect.gameObject.AddComponent<Image>();
        hitArea.color = Color.clear;
        ScrollRect scroll = rect.gameObject.AddComponent<ScrollRect>();
        viewport = Rect(rect, "Viewport");
        Stretch(viewport);
        viewport.offsetMax = new Vector2(-18f, 0f);
        viewport.gameObject.AddComponent<Image>().color = Color.clear;
        viewport.gameObject.AddComponent<RectMask2D>();
        content = Rect(viewport, "Content");
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = Vector2.zero;
        RectTransform track = Rect(rect, "Scrollbar");
        track.anchorMin = new Vector2(1f, 0f);
        track.anchorMax = new Vector2(1f, 1f);
        track.pivot = Vector2.one;
        track.offsetMin = new Vector2(-8f, 0f);
        track.offsetMax = Vector2.zero;
        track.gameObject.AddComponent<Image>().color = new Color(Gold.r, Gold.g, Gold.b, 0.13f);
        Scrollbar scrollbar = track.gameObject.AddComponent<Scrollbar>();
        RectTransform handle = Rect(track, "Handle");
        Stretch(handle);
        Image handleImage = handle.gameObject.AddComponent<Image>();
        handleImage.color = new Color(Gold.r, Gold.g, Gold.b, 0.8f);
        scrollbar.handleRect = handle;
        scrollbar.targetGraphic = handleImage;
        scrollbar.direction = Scrollbar.Direction.BottomToTop;
        scroll.viewport = viewport;
        scroll.content = content;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 36f;
        scroll.verticalScrollbar = scrollbar;
        // Width is reserved consistently, so showing a scrollbar never reflows the text.
        scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
        return scroll;
    }

    private Button BuildActionButton()
    {
        RectTransform rect = Rect(panelRect, "ConfirmButton");
        MapEventFrameGraphic image = rect.gameObject.AddComponent<MapEventFrameGraphic>();
        image.buttonFace = true;
        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(OnConfirmClicked);
        TextMeshProUGUI label = Text(rect, "Text", buttonFontSize, new Color(0.16f, 0.105f, 0.04f), true);
        label.alignment = TextAlignmentOptions.Midline;
        Stretch(label.rectTransform);
        return button;
    }

    private TextMeshProUGUI Text(RectTransform parent, string name, int size, Color color, bool keyword = false)
    {
        TextMeshProUGUI text = Rect(parent, name).gameObject.AddComponent<TextMeshProUGUI>();
        TMP_FontAsset font = keyword ? keywordFont ?? bodyFont : bodyFont ?? keywordFont;
        if (font != null) text.font = font;
        text.fontSize = size;
        text.color = color;
        // The configured map face is already Black; synthetic bold reduces its counters.
        text.fontStyle = FontStyles.Normal;
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.enableWordWrapping = true;
        text.overflowMode = TextOverflowModes.Overflow;
        text.richText = true;
        text.raycastTarget = false;
        return text;
    }

    private void ApplyFonts()
    {
        if (canvas == null) return;
        foreach (TextMeshProUGUI text in canvas.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            string name = text.gameObject.name;
            bool keyword = text == titleText || text == eventMetaText || text == consequenceTitleText
                || name == "OptionSectionTitle" || name == "Number" || name == "Label" || name == "Text";
            TMP_FontAsset font = keyword ? keywordFont ?? bodyFont : bodyFont ?? keywordFont;
            if (font != null) text.font = font;
        }
    }

    private static RectTransform Rect(Transform parent, string name)
    {
        RectTransform rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
        return rect;
    }

    private static RectTransform Rule(Transform parent, string name, Color color)
    {
        RectTransform rect = Rect(parent, name);
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return rect;
    }

    private static RectTransform Glyph(Transform parent, string name, MapHudGlyph.Shape shape, Color color)
    {
        RectTransform rect = Rect(parent, name);
        MapHudGlyph glyph = rect.gameObject.AddComponent<MapHudGlyph>();
        glyph.shape = shape;
        glyph.color = color;
        glyph.raycastTarget = false;
        return rect;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    private static void Place(RectTransform rect, float x, float y, float width, float height)
    {
        rect.anchoredPosition = new Vector2(x, -y);
        rect.sizeDelta = new Vector2(width, height);
    }

    private static void ResetScroll(ScrollRect scroll)
    {
        if (scroll == null) return;
        scroll.StopMovement();
        scroll.content.anchoredPosition = Vector2.zero;
        scroll.verticalNormalizedPosition = 1f;
    }

    private void SetButtonLabel(string label) { confirmButton.GetComponentInChildren<TMP_Text>().text = label; }

    private void SetConfirmButtonInteractable(bool interactable)
    {
        confirmButton.interactable = interactable;
    }

    private void LateUpdate()
    {
        if (!IsVisible) return;
        Vector2 viewport = new Vector2(Screen.width, Screen.height);
        if (viewport != lastViewport || Screen.safeArea != lastSafeArea) ApplyViewport(viewport, Screen.safeArea);
        if (layoutDirty) RefreshLayout();
    }

    private void OnValidate()
    {
        panelSize = new Vector2(Mathf.Max(960f, panelSize.x), Mathf.Max(800f, panelSize.y));
        if (canvas == null) return;
        ApplyFonts();
        layoutDirty = true;
    }
}
