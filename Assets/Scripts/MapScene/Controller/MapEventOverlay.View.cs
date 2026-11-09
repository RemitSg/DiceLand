using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public partial class MapEventOverlay
{
    private const int EventDetailSize = 32;
    private static readonly Color Ivory = new Color(.96f, .94f, .84f);
    private static readonly Color Gold = new Color(.83f, .68f, .38f);
    private Canvas canvas;
    private CanvasGroup presentationGroup, choiceGroup, previewGroup;
    private RectTransform root, safeAreaRect, panelRect, headerDivider, storyAccent, optionSection, footerRect;
    private RectTransform illustrationRect, previewRect, previewNotch;
    private Image illustration, previousIllustration;
    private float illustrationAge = 1;
    private TextMeshProUGUI titleText, eventMetaText, contentText, consequenceTitleText, consequenceText, statusText;
    private TextMeshProUGUI speakerText, dialogueHint, continuationMark;
    private ScrollRect contentScrollRect, consequenceScrollRect, optionScrollRect;
    private RectTransform contentViewportRect, contentContentRect, consequenceViewportRect, consequenceContentRect, optionListRoot;
    private Button confirmButton, dialogueButton;
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
        GameObject go = new GameObject("EventOverlayCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(Image), typeof(CanvasGroup));
        go.transform.SetParent(transform, false);
        canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = EventOverlayCanvasSortingOrder;
        presentationGroup = go.GetComponent<CanvasGroup>();
        CanvasScaler scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 1;
        root = (RectTransform)go.transform; Stretch(root);
        go.GetComponent<Image>().color = new Color(.013f, .026f, .020f, 1);
        illustrationRect = Rect(root, "SceneArt"); Stretch(illustrationRect);
        previousIllustration = ArtImage(illustrationRect, "PreviousIllustration");
        illustration = ArtImage(illustrationRect, "Illustration");
        RectTransform atmosphere = Rect(root, "SceneShade"); Stretch(atmosphere);
        atmosphere.gameObject.AddComponent<MapEventAtmosphereGraphic>().raycastTarget = false;
        safeAreaRect = Rect(root, "SafeArea"); Stretch(safeAreaRect);
        panelRect = Rect(safeAreaRect, "Panel");
        titleText = Text(panelRect, "Title", Mathf.Max(66, titleFontSize), Ivory, true);
        eventMetaText = Text(panelRect, "EventMeta", 28, Gold, true);
        headerDivider = Rule(panelRect, "HeaderDivider", Gold);
        footerRect = Rect(panelRect, "Footer");
        Image dialogueFace = footerRect.gameObject.AddComponent<Image>();
        dialogueFace.color = new Color(.018f, .046f, .030f, .95f);
        dialogueButton = footerRect.gameObject.AddComponent<Button>();
        dialogueButton.targetGraphic = dialogueFace;
        dialogueButton.transition = Selectable.Transition.None;
        dialogueButton.navigation = new Navigation { mode = Navigation.Mode.None };
        dialogueButton.onClick.AddListener(AdvanceDialogue);
        RectTransform frame = Rect(footerRect, "DialogueFrame"); Stretch(frame);
        MapEventFrameGraphic border = frame.gameObject.AddComponent<MapEventFrameGraphic>(); border.raycastTarget = false;
        speakerText = Text(footerRect, "Speaker", 30, Gold, true);
        storyAccent = Rule(footerRect, "StoryAccent", Gold);
        contentScrollRect = ScrollArea(footerRect, "ContentScrollRect", out contentViewportRect, out contentContentRect);
        contentScrollRect.vertical = false;
        contentScrollRect.GetComponent<Image>().raycastTarget = false;
        contentViewportRect.GetComponent<Image>().raycastTarget = false;
        contentText = Text(contentContentRect, "ContentText", Mathf.Max(36, bodyFontSize), Ivory);
        contentText.alignment = TextAlignmentOptions.TopLeft;
        contentText.overflowMode = TextOverflowModes.Page;
        contentText.lineSpacing = 9; contentText.paragraphSpacing = 9;
        dialogueHint = Text(footerRect, "DialogueHint", 23, Gold);
        dialogueHint.alignment = TextAlignmentOptions.MidlineRight;
        continuationMark = Text(footerRect, "ContinuationMark", 28, Gold);
        continuationMark.text = "◆"; continuationMark.alignment = TextAlignmentOptions.Center;
        optionSection = Rect(panelRect, "OptionSection");
        choiceGroup = optionSection.gameObject.AddComponent<CanvasGroup>();
        TextMeshProUGUI section = Text(optionSection, "OptionSectionTitle", 25, Gold, true);
        section.text = GameText.Get("ui.event.choose", "选择行动");
        optionScrollRect = ScrollArea(optionSection, "OptionScrollRect", out _, out optionListRoot);
        confirmButton = BuildActionButton();
        previewRect = Rect(panelRect, "ConsequencePreview");
        previewGroup = previewRect.gameObject.AddComponent<CanvasGroup>();
        Image previewFace = previewRect.gameObject.AddComponent<Image>(); previewFace.color = new Color(.018f, .042f, .026f, .97f);
        RectTransform previewFrame = Rect(previewRect, "PreviewFrame"); Stretch(previewFrame);
        previewFrame.gameObject.AddComponent<MapEventFrameGraphic>().raycastTarget = false;
        previewNotch = Rect(previewRect, "PreviewNotch");
        previewNotch.gameObject.AddComponent<MapEventTooltipNotch>().raycastTarget = false;
        consequenceTitleText = Text(previewRect, "ConsequenceTitle", 27, Gold, true);
        consequenceScrollRect = ScrollArea(previewRect, "ConsequenceScrollRect", out consequenceViewportRect, out consequenceContentRect);
        consequenceText = Text(consequenceContentRect, "ConsequenceText", EventDetailSize, Ivory);
        consequenceText.alignment = TextAlignmentOptions.TopLeft; consequenceText.lineSpacing = 5;
        statusText = Text(previewRect, "Status", 26, new Color(1, .72f, .49f));
        BuildInventoryEntry(); ApplyFonts(); layoutDirty = true;
    }

    private Image ArtImage(Transform parent, string name)
    {
        Image image = Rect(parent, name).gameObject.AddComponent<Image>();
        image.rectTransform.anchorMin = image.rectTransform.anchorMax = image.rectTransform.pivot = Vector2.one * .5f;
        image.raycastTarget = false;
        return image;
    }
    private void ChangeIllustration(MapEvent_SO data)
    {
        Sprite art = MapEventSceneArt.Resolve(data);
        if (illustration.sprite == art) return;
        previousIllustration.sprite = illustration.sprite;
        previousIllustration.gameObject.SetActive(previousIllustration.sprite != null);
        illustration.sprite = art;
        illustrationAge = AnimateStory ? 0 : 1;
        previousIllustration.transform.SetAsLastSibling();
        previousIllustration.color = new Color(1, 1, 1, AnimateStory ? 1 : 0);
        FitIllustration();
    }
    private void FitIllustration()
    {
        Vector2 size = new Vector2(lastViewport.x * 1080 / Mathf.Max(1, lastViewport.y), 1080);
        foreach (Image image in new[] { illustration, previousIllustration })
        {
            if (image.sprite == null) continue;
            Vector2 pixels = image.sprite.rect.size;
            image.rectTransform.sizeDelta = pixels * Mathf.Max(size.x / pixels.x, size.y / pixels.y);
            image.rectTransform.anchoredPosition = Vector2.zero;
        }
    }

    /// <summary>The artwork bleeds to the display edge; controls stay within the safe area.</summary>
    public void ApplyViewport(Vector2 viewport, Rect safePixels)
    {
        if (canvas == null) return;
        float width = Mathf.Max(1, viewport.x), height = Mathf.Max(1, viewport.y);
        float x = Mathf.Clamp(safePixels.xMin, 0, width - 1), y = Mathf.Clamp(safePixels.yMin, 0, height - 1);
        Rect safe = new Rect(x, y, Mathf.Clamp(safePixels.width, 1, width - x), Mathf.Clamp(safePixels.height, 1, height - y));
        safeAreaRect.anchorMin = new Vector2(safe.xMin / width, safe.yMin / height);
        safeAreaRect.anchorMax = new Vector2(safe.xMax / width, safe.yMax / height);
        safeAreaRect.offsetMin = safeAreaRect.offsetMax = Vector2.zero;
        Vector2 available = safe.size * (1080 / height);
        panelRect.localScale = Vector3.one; panelRect.anchoredPosition = new Vector2(48, -32);
        panelRect.sizeDelta = Vector2.Max(new Vector2(320, 320), available - new Vector2(96, 64));
        lastViewport = viewport; lastSafeArea = safePixels;
        FitIllustration(); layoutDirty = true;
    }

    private Button BuildOptionButton(MapEventOptionData option, EventOptionPreviewData preview, int index)
    {
        RectTransform rect = Rect(optionListRoot, "Option_" + index.ToString("00"));
        MapEventChoiceGraphic face = rect.gameObject.AddComponent<MapEventChoiceGraphic>(); face.SetDetailVisible(false);
        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = face; button.transition = Selectable.Transition.None;
        button.interactable = preview == null || preview.isSelectable;
        button.onClick.AddListener(() => { if (CanReceiveStoryInput && AreChoicesReady) SelectOption(option); });
        rect.gameObject.AddComponent<MapCraftingButtonFeedback>();
        rect.gameObject.AddComponent<MapEventChoiceFeedback>().Initialize(this, option);
        OptionRow row = new OptionRow { button = button, face = face };
        row.number = Text(rect, "Number", 27, Gold, true); row.number.text = index.ToString("00"); row.number.alignment = TextAlignmentOptions.Midline;
        row.label = Text(rect, "Label", Mathf.Max(32, buttonFontSize), Ivory, true); row.label.text = FormatOptionLabel(option, preview);
        row.detail = Text(rect, "Detail", EventDetailSize, Ivory); row.detail.text = FormatOptionDetail(option, preview); row.detail.gameObject.SetActive(false);
        optionRows.Add(row);
        return button;
    }
    private void RefreshOptionButtonVisuals()
    {
        foreach (OptionRow row in optionRows)
        {
            bool selected = optionLookup.TryGetValue(row.button, out MapEventOptionData option) && option == selectedOption;
            row.face.SetState(selected, row.button.interactable);
            float alpha = row.button.interactable ? 1 : .54f;
            row.number.color = new Color(Gold.r, Gold.g, Gold.b, alpha); row.label.color = new Color(Ivory.r, Ivory.g, Ivory.b, alpha);
        }
    }
    private void RefreshLayout()
    {
        if (canvas == null) return;
        bool hadReadAll = IsNarrationComplete;
        float previousPreviewY = previewRect.anchoredPosition.y;
        float width = panelRect.sizeDelta.x, height = panelRect.sizeDelta.y;
        contentText.fontSize = MainMenuSettingsPersistence.DetailFontSize(Mathf.Max(36, bodyFontSize));
        consequenceText.fontSize = MainMenuSettingsPersistence.DetailFontSize(EventDetailSize);
        float choiceWidth = Mathf.Clamp(width * .29f, 420, 650), previewWidth = Mathf.Clamp(width * .235f, 350, 530);
        float titleWidth = Mathf.Max(300, width - 240);
        Place(eventMetaText.rectTransform, 12, 0, titleWidth, 42);
        float titleHeight = Mathf.Max(82, PreferredHeight(titleText, titleWidth));
        Place(titleText.rectTransform, 12, 48, titleWidth, titleHeight); Place(headerDivider, 14, titleHeight + 61, 156, 2);
        float dialogueHeight = Mathf.Max(316, Mathf.Min(height * .31f, 340)), dialogueTop = height - dialogueHeight;
        Place(footerRect, 0, dialogueTop, width, dialogueHeight);
        Place(speakerText.rectTransform, 46, 17, width - 92, 42); Place(storyAccent, 47, 67, 138, 1);
        Place(contentScrollRect.GetComponent<RectTransform>(), 46, 84, width - 92, dialogueHeight - 132);
        Stretch(contentViewportRect); contentViewportRect.offsetMax = Vector2.zero;
        Stretch(contentContentRect); Stretch(contentText.rectTransform);
        contentScrollRect.verticalScrollbar.gameObject.SetActive(false);
        Place(dialogueHint.rectTransform, width - 364, dialogueHeight - 42, 286, 30);
        Place(continuationMark.rectTransform, width - 66, dialogueHeight - 44, 32, 32);
        float choiceTop = Mathf.Max(titleHeight + 96, height * .20f);
        float choiceMaxHeight = Mathf.Max(110, dialogueTop - choiceTop - 146);
        float rowHeight = 88;
        foreach (OptionRow row in optionRows) rowHeight = Mathf.Max(rowHeight, PreferredHeight(row.label, choiceWidth - 162) + 28);
        float rowsHeight = Mathf.Max(0, optionRows.Count * (rowHeight + 14) - 14), viewportHeight = Mathf.Min(rowsHeight, choiceMaxHeight);
        float rowY = 0;
        foreach (OptionRow row in optionRows)
        {
            Place((RectTransform)row.button.transform, 6, rowY, choiceWidth - 12, rowHeight);
            Place(row.number.rectTransform, 10, 10, 65, rowHeight - 20);
            Place(row.label.rectTransform, 90, 10, choiceWidth - 162, rowHeight - 20); rowY += rowHeight + 14;
        }
        choicePosition = new Vector2(width - choiceWidth, -choiceTop);
        Place(optionSection, width - choiceWidth, choiceTop, choiceWidth, viewportHeight + 138);
        TextMeshProUGUI section = optionSection.Find("OptionSectionTitle").GetComponent<TextMeshProUGUI>();
        Place(section.rectTransform, 8, 0, choiceWidth - 16, 32);
        Place(optionScrollRect.GetComponent<RectTransform>(), 0, 44, choiceWidth, Mathf.Max(1, viewportHeight));
        optionListRoot.sizeDelta = new Vector2(0, Mathf.Max(viewportHeight, rowsHeight));
        Place((RectTransform)confirmButton.transform, 6, 44 + viewportHeight + 20, choiceWidth - 12, 74);
        // Match the actual viewport minus its reserved scrollbar and inner text padding.
        float detailHeight = PreferredHeight(consequenceText, previewWidth - 78);
        float errorHeight = string.IsNullOrWhiteSpace(statusText.text) ? 0 : Mathf.Min(96, PreferredHeight(statusText, previewWidth - 58) + 10);
        float tooltipHeight = Mathf.Clamp(detailHeight + 120 + errorHeight, 155, Mathf.Min(380, dialogueTop - choiceTop - 12));
        Place(previewRect, Mathf.Max(0, width - choiceWidth - previewWidth - 24), choiceTop + 44, previewWidth, tooltipHeight);
        if (AnimateStory && narrationSource == contentText.text) previewRect.anchoredPosition = new Vector2(previewRect.anchoredPosition.x, previousPreviewY);
        Place(consequenceTitleText.rectTransform, 28, 18, previewWidth - 56, 38);
        Place(consequenceScrollRect.GetComponent<RectTransform>(), 28, 68, previewWidth - 56, tooltipHeight - 89 - errorHeight);
        Place(statusText.rectTransform, 28, tooltipHeight - errorHeight - 16, previewWidth - 56, errorHeight); statusText.gameObject.SetActive(errorHeight > 0);
        previewRect.gameObject.SetActive(!(currentEvent != null && currentEvent.hideConsequences || isShowingResultStep));
        Canvas.ForceUpdateCanvases();
        FitText(consequenceText, consequenceContentRect, consequenceViewportRect);
        UpdateScrollbar(consequenceScrollRect); UpdateScrollbar(optionScrollRect);
        contentText.ForceMeshUpdate(true, true); ReflowNarration(hadReadAll); layoutDirty = false;
        UpdatePreviewAnchor();
        if (!AnimateStory) previewRect.anchoredPosition = previewPosition;
    }
    private void UpdatePreviewAnchor()
    {
        if (previewRect == null || footerRect == null) return;
        OptionRow row = optionRows.Find(r => optionLookup.TryGetValue(r.button, out MapEventOptionData option) && option == (previewedOption ?? selectedOption));
        if (row == null) return;
        RectTransform target = (RectTransform)row.button.transform;
        float center = -panelRect.InverseTransformPoint(target.TransformPoint(new Vector3(0, -target.rect.height * .5f, 0))).y;
        float height = previewRect.sizeDelta.y;
        float top = Mathf.Clamp(center - height * .5f, Mathf.Max(175, -choicePosition.y - 50), -footerRect.anchoredPosition.y - height - 24);
        previewPosition = new Vector2(previewRect.anchoredPosition.x, -top);
        Place(previewNotch, previewRect.sizeDelta.x - 3, Mathf.Clamp(center - top, 23, height - 23) - 13, 20, 26);
    }
    private static void UpdateScrollbar(ScrollRect scroll)
    {
        float h = scroll.content.rect.height, viewport = scroll.viewport.rect.height;
        scroll.verticalScrollbar.gameObject.SetActive(h > viewport + .5f);
        scroll.verticalScrollbar.size = h > 0 ? Mathf.Clamp01(viewport / h) : 1;
    }
    private static float PreferredHeight(TMP_Text text, float width)
    {
        string processed = text.textPreprocessor != null ? text.textPreprocessor.PreprocessText(text.text) : text.text;
        return text.GetPreferredValues(processed, Mathf.Max(40, width), 0).y;
    }
    private static void FitText(TextMeshProUGUI text, RectTransform content, RectTransform viewport)
    {
        float width = Mathf.Max(40, viewport.rect.width - 8), height = Mathf.Max(viewport.rect.height, PreferredHeight(text, width) + 12);
        content.sizeDelta = new Vector2(0, height); Place(text.rectTransform, 0, 0, width, height); text.ForceMeshUpdate();
    }
    private ScrollRect ScrollArea(RectTransform parent, string name, out RectTransform viewport, out RectTransform content)
    {
        RectTransform rect = Rect(parent, name);
        Image hitArea = rect.gameObject.AddComponent<Image>(); hitArea.color = Color.clear;
        ScrollRect scroll = rect.gameObject.AddComponent<ScrollRect>();
        viewport = Rect(rect, "Viewport"); Stretch(viewport); viewport.offsetMax = new Vector2(-14, 0);
        viewport.gameObject.AddComponent<Image>().color = Color.clear; viewport.gameObject.AddComponent<RectMask2D>();
        content = Rect(viewport, "Content");
        content.anchorMin = new Vector2(0, 1); content.anchorMax = Vector2.one; content.pivot = new Vector2(.5f, 1);
        content.anchoredPosition = content.sizeDelta = Vector2.zero;
        RectTransform track = Rect(rect, "Scrollbar");
        track.anchorMin = new Vector2(1, 0); track.anchorMax = track.pivot = Vector2.one;
        track.offsetMin = new Vector2(-5, 0); track.offsetMax = Vector2.zero;
        track.gameObject.AddComponent<Image>().color = new Color(Gold.r, Gold.g, Gold.b, .12f);
        Scrollbar scrollbar = track.gameObject.AddComponent<Scrollbar>();
        RectTransform handle = Rect(track, "Handle"); Stretch(handle);
        Image handleImage = handle.gameObject.AddComponent<Image>(); handleImage.color = Gold;
        scrollbar.handleRect = handle; scrollbar.targetGraphic = handleImage; scrollbar.direction = Scrollbar.Direction.BottomToTop;
        scroll.viewport = viewport; scroll.content = content; scroll.horizontal = false; scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 36;
        scroll.verticalScrollbar = scrollbar; scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
        return scroll;
    }
    private Button BuildActionButton()
    {
        RectTransform rect = Rect(optionSection, "ConfirmButton");
        MapEventFrameGraphic image = rect.gameObject.AddComponent<MapEventFrameGraphic>(); image.buttonFace = true;
        Button button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image; button.onClick.AddListener(OnConfirmClicked);
        rect.gameObject.AddComponent<MapCraftingButtonFeedback>();
        TextMeshProUGUI label = Text(rect, "Text", Mathf.Max(32, buttonFontSize), new Color(.12f, .105f, .045f), true);
        label.alignment = TextAlignmentOptions.Midline; Stretch(label.rectTransform);
        return button;
    }
    private TextMeshProUGUI Text(RectTransform parent, string name, int size, Color color, bool keyword = false)
    {
        TextMeshProUGUI text = Rect(parent, name).gameObject.AddComponent<TextMeshProUGUI>();
        MapEventStoryTheme theme = MapEventStoryTheme.Load();
        TMP_FontAsset font = keyword ? theme != null ? theme.headingFont : keywordFont ?? bodyFont : theme != null ? theme.bodyFont : bodyFont ?? keywordFont;
        if (font != null) text.font = font;
        text.fontSize = size; text.color = color; text.fontStyle = FontStyles.Normal; text.alignment = TextAlignmentOptions.MidlineLeft;
        text.enableWordWrapping = true; text.overflowMode = TextOverflowModes.Overflow; text.richText = true; text.raycastTarget = false;
        ResourceKeywordText.Attach(text); return text;
    }
    private void ApplyFonts()
    {
        if (canvas == null) return;
        MapEventStoryTheme theme = MapEventStoryTheme.Load();
        foreach (TextMeshProUGUI text in canvas.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            bool keyword = text == titleText || text == eventMetaText || text == consequenceTitleText || text == speakerText
                || text.name == "OptionSectionTitle" || text.name == "Number" || text.name == "Label" || text.name == "Text";
            TMP_FontAsset font = keyword ? theme != null ? theme.headingFont : keywordFont ?? bodyFont : theme != null ? theme.bodyFont : bodyFont ?? keywordFont;
            if (font != null) text.font = font;
        }
    }
    private static RectTransform Rect(Transform parent, string name)
    {
        RectTransform rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false); rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1); return rect;
    }
    private static RectTransform Rule(Transform parent, string name, Color color)
    {
        RectTransform rect = Rect(parent, name); Image image = rect.gameObject.AddComponent<Image>(); image.color = color; image.raycastTarget = false; return rect;
    }
    private static void Stretch(RectTransform rect) { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero; }
    private static void Place(RectTransform rect, float x, float y, float width, float height) { rect.anchoredPosition = new Vector2(x, -y); rect.sizeDelta = new Vector2(width, height); }
    private static void ResetScroll(ScrollRect scroll) { if (scroll == null) return; scroll.StopMovement(); scroll.content.anchoredPosition = Vector2.zero; scroll.verticalNormalizedPosition = 1; }
    private void SetButtonLabel(string label) => confirmButton.GetComponentInChildren<TMP_Text>().text = label;
    private void SetConfirmButtonInteractable(bool interactable) => confirmButton.interactable = interactable;
    private void LateUpdate()
    {
        if (!IsVisible) return;
        Vector2 viewport = new Vector2(Screen.width, Screen.height);
        if (viewport != lastViewport || Screen.safeArea != lastSafeArea) ApplyViewport(viewport, Screen.safeArea);
        if (layoutDirty || contentText.fontSize != MainMenuSettingsPersistence.DetailFontSize(Mathf.Max(36, bodyFontSize))) RefreshLayout();
    }
    private void OnValidate() { if (canvas == null) return; ApplyFonts(); layoutDirty = true; }
}
