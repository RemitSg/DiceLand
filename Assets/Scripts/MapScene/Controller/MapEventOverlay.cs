using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class MapEventOverlay : MonoBehaviour
{
    public delegate bool EventOptionSelectionHandler(BoardTileData tile, MapEvent_SO eventData, MapEventOptionData optionData, out string message);
    public event Action Hidden;

    [Header("Font")]
    [SerializeField] private TMP_FontAsset keywordFont;
    [SerializeField] private TMP_FontAsset bodyFont;
    [SerializeField] private int titleFontSize = 36;
    [SerializeField] private int bodyFontSize = 24;
    [SerializeField] private int buttonFontSize = 24;

    [Header("Panel")]
    [SerializeField] private Vector2 panelSize = new Vector2(1080f, 640f);
    [SerializeField] private Sprite panelBackgroundSprite;
    [SerializeField] private Color panelBackgroundColor = new Color(0.08f, 0.1f, 0.14f, 0.94f);
    [SerializeField] private Color backdropColor = new Color(0f, 0f, 0f, 0.52f);
    [SerializeField] private Vector2 panelOffset = Vector2.zero;

    [Header("Event")]
    [SerializeField] private Vector2 titleOffset = new Vector2(28f, -18f);
    [SerializeField] private Vector2 titleSize = new Vector2(520f, 42f);
    [SerializeField] private Vector2 contentOffset = new Vector2(28f, -92f);
    [SerializeField] private Vector2 contentSize = new Vector2(460f, 260f);
    [SerializeField] private float contentScrollbarWidth = 14f;
    [SerializeField] private Color contentScrollbarTrackColor = new Color(1f, 1f, 1f, 0.1f);
    [SerializeField] private Color contentScrollbarHandleColor = new Color(1f, 0.83f, 0.35f, 0.95f);
    [SerializeField] private Vector2 consequenceViewportOffset = new Vector2(28f, -420f);
    [SerializeField] private Vector2 consequenceViewportSize = new Vector2(500f, 110f);
    [SerializeField] private float consequenceScrollbarWidth = 14f;
    [SerializeField] private Color consequenceScrollbarTrackColor = new Color(1f, 1f, 1f, 0.1f);
    [SerializeField] private Color consequenceScrollbarHandleColor = new Color(1f, 0.83f, 0.35f, 0.95f);
    [SerializeField] private Vector2 illustrationOffset = new Vector2(-28f, -22f);
    [SerializeField] private Vector2 illustrationSize = new Vector2(120f, 120f);
    [SerializeField] private Color titleColor = Color.white;
    [SerializeField] private Color contentColor = new Color(0.92f, 0.96f, 1f, 1f);
    [SerializeField] private Color accentColor = new Color(1f, 0.83f, 0.35f, 1f);

    [Header("Option List")]
    [SerializeField] private Vector2 optionButtonSize = new Vector2(450f, 108f);
    [SerializeField] private float optionButtonSpacing = 12f;
    [SerializeField] private Sprite optionButtonSprite;
    [SerializeField] private Sprite selectedOptionButtonSprite;
    [SerializeField] private Color optionButtonColor = new Color(0.18f, 0.16f, 0.12f, 0.96f);
    [SerializeField] private Color selectedOptionButtonColor = new Color(1f, 0.83f, 0.35f, 1f);
    [SerializeField] private Color optionButtonTextColor = Color.white;
    [SerializeField] private Color selectedOptionButtonTextColor = Color.black;

    [Header("Buttons")]
    [SerializeField] private Sprite actionButtonSprite;
    [SerializeField] private Vector2 actionButtonSize = new Vector2(190f, 58f);
    [SerializeField] private Color actionButtonColor = new Color(1f, 0.83f, 0.35f, 1f);
    [SerializeField] private Color actionButtonTextColor = Color.black;
    [SerializeField] private Color disabledButtonColor = new Color(0.4f, 0.4f, 0.4f, 0.9f);
    [SerializeField] private string confirmButtonLabel = "确定";
    [SerializeField] private string continueButtonLabel = "继续";
    [SerializeField] private string closeButtonLabel = "关闭";

    private Canvas canvas;
    private RectTransform root;
    private RectTransform panelRect;
    private Image panelImage;
    private TextMeshProUGUI titleText;
    private TextMeshProUGUI contentText;
    private ScrollRect contentScrollRect;
    private RectTransform contentViewportRect;
    private RectTransform contentContentRect;
    private TextMeshProUGUI statusText;
    private TextMeshProUGUI consequenceTitleText;
    private TextMeshProUGUI consequenceText;
    private ScrollRect consequenceScrollRect;
    private RectTransform consequenceViewportRect;
    private RectTransform consequenceContentRect;
    private Image illustrationImage;
    private RectTransform optionListRoot;
    private Button confirmButton;
    private Button closeButton;

    private readonly List<Button> optionButtons = new List<Button>();
    private readonly List<GameObject> spawnedOptionObjects = new List<GameObject>();
    private readonly Dictionary<Button, MapEventOptionData> optionLookup = new Dictionary<Button, MapEventOptionData>();

    private BoardTileData currentTile;
    private MapEvent_SO currentEvent;
    private MapEventOptionData selectedOption;
    private EventOptionSelectionHandler selectionHandler;
    private bool isShowingResultStep;

    public bool IsVisible =>
        canvas != null &&
        canvas.gameObject.activeInHierarchy &&
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

    public void Show(BoardTileData tile, MapEvent_SO eventData, EventOptionSelectionHandler onSelect)
    {
        EnsureUi();
        currentTile = tile;
        currentEvent = eventData;
        selectionHandler = onSelect;
        selectedOption = null;
        isShowingResultStep = false;

        gameObject.SetActive(true);
        canvas.gameObject.SetActive(true);
        panelRect.gameObject.SetActive(true);

        RefreshHeader();
        RefreshOptionList();
        SelectOption(GetFirstOption());
    }

    public void Hide()
    {
        bool wasVisible = IsVisible;
        isShowingResultStep = false;
        selectedOption = null;

        if (panelRect != null)
            panelRect.gameObject.SetActive(false);
        if (canvas != null)
            canvas.gameObject.SetActive(false);

        if (wasVisible)
            Hidden?.Invoke();
    }

    private void EnsureUi()
    {
        if (canvas != null)
        {
            canvas.gameObject.SetActive(true);
            panelRect.gameObject.SetActive(true);
            return;
        }

        GameObject canvasGo = new GameObject("EventOverlayCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(Image));
        canvasGo.transform.SetParent(transform, false);
        canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 7990;

        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        root = canvasGo.GetComponent<RectTransform>();
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.offsetMin = Vector2.zero;
        root.offsetMax = Vector2.zero;

        Image blocker = canvasGo.GetComponent<Image>();
        blocker.color = backdropColor;

        panelRect = CreatePanel(root, "Panel", new Vector2(0.5f, 0.5f), panelOffset, panelSize);
        panelImage = panelRect.GetComponent<Image>();
        ApplyPanelVisual();

        titleText = CreateText(panelRect, "Title", titleOffset, titleSize, titleFontSize, titleColor, "事件", true);
        illustrationImage = BuildIllustration(panelRect);
        BuildContentScrollArea(panelRect);

        consequenceTitleText = CreateText(panelRect, "ConsequenceTitle", new Vector2(28f, -376f), new Vector2(320f, 34f), bodyFontSize, accentColor, "后果预览", true);
        BuildConsequenceScrollArea(panelRect);

        optionListRoot = new GameObject("OptionList", typeof(RectTransform)).GetComponent<RectTransform>();
        optionListRoot.SetParent(panelRect, false);
        optionListRoot.anchorMin = new Vector2(0f, 1f);
        optionListRoot.anchorMax = new Vector2(0f, 1f);
        optionListRoot.pivot = new Vector2(0f, 1f);
        optionListRoot.anchoredPosition = new Vector2(580f, -28f);
        optionListRoot.sizeDelta = new Vector2(460f, 472f);

        statusText = CreateText(panelRect, "Status", new Vector2(28f, -560f), new Vector2(680f, 36f), bodyFontSize - 2, accentColor, string.Empty);
        confirmButton = BuildActionButton(panelRect, "ConfirmButton", new Vector2(-230f, 26f), confirmButtonLabel, OnConfirmClicked);
        closeButton = BuildActionButton(panelRect, "CloseButton", new Vector2(-24f, 26f), closeButtonLabel, Hide);
        closeButton.gameObject.SetActive(false);

        ApplyFonts();
        ApplyLayout();
        panelRect.gameObject.SetActive(true);
    }

    private void RefreshHeader()
    {
        titleText.text = currentEvent != null ? currentEvent.DisplayName : "事件";
        contentText.text = currentEvent != null && !string.IsNullOrWhiteSpace(currentEvent.eventContent)
            ? currentEvent.eventContent
            : "暂无事件内容。";

        if (illustrationImage != null)
        {
            bool hasIllustration = currentEvent != null && currentEvent.eventIllustration != null;
            illustrationImage.sprite = hasIllustration ? currentEvent.eventIllustration : null;
            illustrationImage.enabled = hasIllustration;
            illustrationImage.gameObject.SetActive(hasIllustration);
        }

        statusText.text = string.Empty;
        if (optionListRoot != null)
            optionListRoot.gameObject.SetActive(true);
        consequenceTitleText.text = "后果预览";
        SetButtonLabel(confirmButton, confirmButtonLabel);
        SetConfirmButtonInteractable(true);
        if (closeButton != null)
            closeButton.gameObject.SetActive(false);
        if (contentScrollRect != null)
            contentScrollRect.verticalNormalizedPosition = 1f;
    }

    private void RefreshOptionList()
    {
        for (int i = 0; i < spawnedOptionObjects.Count; i++)
            Destroy(spawnedOptionObjects[i]);
        spawnedOptionObjects.Clear();
        optionButtons.Clear();
        optionLookup.Clear();

        IReadOnlyList<MapEventOptionData> options = currentEvent != null ? currentEvent.GetValidOptions() : Array.Empty<MapEventOptionData>();
        float y = 0f;
        for (int i = 0; i < options.Count; i++)
        {
            MapEventOptionData option = options[i];
            if (option == null)
                continue;

            Button button = BuildOptionButton(option, new Vector2(0f, -y));
            optionButtons.Add(button);
            optionLookup[button] = option;
            spawnedOptionObjects.Add(button.gameObject);
            y += optionButtonSize.y + optionButtonSpacing;
        }
    }

    private void SelectOption(MapEventOptionData option)
    {
        selectedOption = option;
        RefreshOptionButtonVisuals();
        RefreshOptionDetail();
    }

    private void RefreshOptionDetail()
    {
        bool hasOption = selectedOption != null;
        consequenceText.text = hasOption
            ? FormatOptionDetail(selectedOption)
            : "当前事件暂无可选项。";
        if (consequenceScrollRect != null)
            consequenceScrollRect.verticalNormalizedPosition = 1f;
        SetConfirmButtonInteractable(hasOption);
    }

    private void OnConfirmClicked()
    {
        if (isShowingResultStep)
        {
            Hide();
            return;
        }

        if (selectedOption == null || selectionHandler == null || currentTile == null || currentEvent == null)
            return;

        if (selectionHandler.Invoke(currentTile, currentEvent, selectedOption, out string message))
        {
            ShowResultStep(selectedOption, message);
            return;
        }

        if (statusText != null)
            statusText.text = string.IsNullOrWhiteSpace(message) ? "当前选项未能执行。" : message;
    }

    private void ShowResultStep(MapEventOptionData option, string message)
    {
        isShowingResultStep = true;

        if (optionListRoot != null)
            optionListRoot.gameObject.SetActive(false);

        string resultText = option != null && !string.IsNullOrWhiteSpace(option.resultContent)
            ? option.resultContent
            : (!string.IsNullOrWhiteSpace(message) ? message : (option != null ? option.consequenceDescription : string.Empty));
        contentText.text = string.IsNullOrWhiteSpace(resultText) ? "事件已触发。" : resultText;

        consequenceTitleText.text = "选择结果";
        consequenceText.text = !string.IsNullOrWhiteSpace(message)
            ? message
            : "请选择继续以收起事件界面。";

        if (contentScrollRect != null)
            contentScrollRect.verticalNormalizedPosition = 1f;
        if (consequenceScrollRect != null)
            consequenceScrollRect.verticalNormalizedPosition = 1f;

        statusText.text = string.Empty;
        SetButtonLabel(confirmButton, continueButtonLabel);
        SetConfirmButtonInteractable(true);
        if (closeButton != null)
            closeButton.gameObject.SetActive(false);
    }

    private MapEventOptionData GetFirstOption()
    {
        IReadOnlyList<MapEventOptionData> options = currentEvent != null ? currentEvent.GetValidOptions() : Array.Empty<MapEventOptionData>();
        return options.Count > 0 ? options[0] : null;
    }

    private Button BuildOptionButton(MapEventOptionData option, Vector2 anchoredPosition)
    {
        GameObject buttonGo = new GameObject($"Option_{option.DisplayLabel}", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonGo.transform.SetParent(optionListRoot, false);

        RectTransform rect = buttonGo.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = optionButtonSize;

        Image image = buttonGo.GetComponent<Image>();
        image.color = optionButtonColor;
        image.sprite = optionButtonSprite;
        image.type = optionButtonSprite != null ? Image.Type.Sliced : Image.Type.Simple;

        TextMeshProUGUI label = CreateText(rect, "Label", new Vector2(14f, -10f), new Vector2(optionButtonSize.x - 24f, 30f), buttonFontSize, optionButtonTextColor, FormatOptionLabel(option), true);
        TextMeshProUGUI detail = CreateText(rect, "Detail", new Vector2(14f, -46f), new Vector2(optionButtonSize.x - 24f, optionButtonSize.y - 54f), bodyFontSize - 2, optionButtonTextColor, FormatOptionDetail(option));
        detail.enableWordWrapping = true;
        detail.overflowMode = TextOverflowModes.Overflow;

        Button button = buttonGo.GetComponent<Button>();
        button.onClick.AddListener(() => SelectOption(option));
        return button;
    }

    private void RefreshOptionButtonVisuals()
    {
        for (int i = 0; i < optionButtons.Count; i++)
        {
            Button button = optionButtons[i];
            if (button == null)
                continue;

            bool isSelected = optionLookup.TryGetValue(button, out MapEventOptionData option) && option == selectedOption;
            Image image = button.GetComponent<Image>();
            if (image != null)
            {
                image.sprite = isSelected && selectedOptionButtonSprite != null ? selectedOptionButtonSprite : optionButtonSprite;
                image.type = image.sprite != null ? Image.Type.Sliced : Image.Type.Simple;
                image.color = isSelected ? selectedOptionButtonColor : optionButtonColor;
            }

            TMP_Text[] texts = button.GetComponentsInChildren<TMP_Text>(true);
            for (int j = 0; j < texts.Length; j++)
                texts[j].color = isSelected ? selectedOptionButtonTextColor : optionButtonTextColor;
        }
    }

    private Image BuildIllustration(RectTransform parent)
    {
        RectTransform rect = new GameObject("Illustration", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = illustrationOffset;
        rect.sizeDelta = illustrationSize;
        Image image = rect.GetComponent<Image>();
        image.preserveAspect = true;
        return image;
    }

    private void BuildContentScrollArea(RectTransform parent)
    {
        contentScrollRect = BuildScrollArea(
            parent,
            "ContentScrollRect",
            contentOffset,
            contentSize,
            contentScrollbarWidth,
            contentScrollbarTrackColor,
            contentScrollbarHandleColor,
            out contentViewportRect,
            out contentContentRect);

        contentText = CreateText(contentContentRect, "ContentText", Vector2.zero, new Vector2(contentSize.x - contentScrollbarWidth - 24f, contentSize.y), bodyFontSize, contentColor, string.Empty);
        contentText.enableWordWrapping = true;
        contentText.overflowMode = TextOverflowModes.Overflow;
    }

    private void BuildConsequenceScrollArea(RectTransform parent)
    {
        consequenceScrollRect = BuildScrollArea(
            parent,
            "ConsequenceScrollRect",
            consequenceViewportOffset,
            consequenceViewportSize,
            consequenceScrollbarWidth,
            consequenceScrollbarTrackColor,
            consequenceScrollbarHandleColor,
            out consequenceViewportRect,
            out consequenceContentRect);

        consequenceText = CreateText(consequenceContentRect, "ConsequenceText", Vector2.zero, new Vector2(consequenceViewportSize.x - consequenceScrollbarWidth - 24f, consequenceViewportSize.y), bodyFontSize, contentColor, string.Empty);
        consequenceText.enableWordWrapping = true;
        consequenceText.overflowMode = TextOverflowModes.Overflow;
    }

    private ScrollRect BuildScrollArea(
        RectTransform parent,
        string name,
        Vector2 anchoredPosition,
        Vector2 size,
        float scrollbarWidth,
        Color trackColor,
        Color handleColor,
        out RectTransform viewportRect,
        out RectTransform contentRect)
    {
        GameObject scrollGo = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(ScrollRect));
        scrollGo.transform.SetParent(parent, false);
        RectTransform scrollRectTransform = scrollGo.GetComponent<RectTransform>();
        scrollRectTransform.anchorMin = new Vector2(0f, 1f);
        scrollRectTransform.anchorMax = new Vector2(0f, 1f);
        scrollRectTransform.pivot = new Vector2(0f, 1f);
        scrollRectTransform.anchoredPosition = anchoredPosition;
        scrollRectTransform.sizeDelta = size;

        Image scrollBackground = scrollGo.GetComponent<Image>();
        scrollBackground.color = Color.clear;
        scrollBackground.raycastTarget = true;

        ScrollRect scrollRect = scrollGo.GetComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 28f;

        GameObject viewportGo = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
        viewportGo.transform.SetParent(scrollRectTransform, false);
        viewportRect = viewportGo.GetComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = Vector2.zero;
        viewportRect.offsetMax = new Vector2(-scrollbarWidth - 8f, 0f);

        Image viewportImage = viewportGo.GetComponent<Image>();
        viewportImage.color = Color.clear;
        viewportImage.raycastTarget = true;

        GameObject contentGo = new GameObject("Content", typeof(RectTransform));
        contentGo.transform.SetParent(viewportRect, false);
        contentRect = contentGo.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = new Vector2(0f, size.y);

        GameObject scrollbarGo = new GameObject("Scrollbar", typeof(RectTransform), typeof(Image), typeof(Scrollbar));
        scrollbarGo.transform.SetParent(scrollRectTransform, false);
        RectTransform scrollbarRect = scrollbarGo.GetComponent<RectTransform>();
        scrollbarRect.anchorMin = new Vector2(1f, 0f);
        scrollbarRect.anchorMax = new Vector2(1f, 1f);
        scrollbarRect.pivot = new Vector2(1f, 1f);
        scrollbarRect.offsetMin = new Vector2(-scrollbarWidth, 0f);
        scrollbarRect.offsetMax = Vector2.zero;

        Image scrollbarTrack = scrollbarGo.GetComponent<Image>();
        scrollbarTrack.color = trackColor;

        GameObject handleGo = new GameObject("Handle", typeof(RectTransform), typeof(Image));
        handleGo.transform.SetParent(scrollbarRect, false);
        RectTransform handleRect = handleGo.GetComponent<RectTransform>();
        handleRect.anchorMin = Vector2.zero;
        handleRect.anchorMax = Vector2.one;
        handleRect.offsetMin = Vector2.zero;
        handleRect.offsetMax = Vector2.zero;

        Image handleImage = handleGo.GetComponent<Image>();
        handleImage.color = handleColor;

        Scrollbar scrollbar = scrollbarGo.GetComponent<Scrollbar>();
        scrollbar.direction = Scrollbar.Direction.BottomToTop;
        scrollbar.targetGraphic = handleImage;
        scrollbar.handleRect = handleRect;

        scrollRect.viewport = viewportRect;
        scrollRect.content = contentRect;
        scrollRect.verticalScrollbar = scrollbar;
        scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
        return scrollRect;
    }

    private Button BuildActionButton(RectTransform parent, string name, Vector2 anchoredPosition, string label, UnityEngine.Events.UnityAction action)
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
        image.sprite = actionButtonSprite;
        image.type = actionButtonSprite != null ? Image.Type.Sliced : Image.Type.Simple;
        image.color = actionButtonColor;

        Button button = buttonGo.GetComponent<Button>();
        button.onClick.AddListener(action);

        TextMeshProUGUI text = CreateText(rect, "Text", Vector2.zero, actionButtonSize, buttonFontSize, actionButtonTextColor, label, true);
        text.alignment = TextAlignmentOptions.Center;
        text.rectTransform.anchorMin = Vector2.zero;
        text.rectTransform.anchorMax = Vector2.one;
        text.rectTransform.offsetMin = Vector2.zero;
        text.rectTransform.offsetMax = Vector2.zero;
        return button;
    }

    private RectTransform CreatePanel(RectTransform parent, string name, Vector2 anchorPivot, Vector2 anchoredPosition, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchorPivot;
        rect.anchorMax = anchorPivot;
        rect.pivot = anchorPivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
        return rect;
    }

    private TextMeshProUGUI CreateText(RectTransform parent, string name, Vector2 anchoredPosition, Vector2 size, int fontSize, Color color, string text, bool useKeywordFont = false)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.text = text;
        tmp.richText = true;
        tmp.alignment = TextAlignmentOptions.TopLeft;
        tmp.raycastTarget = false;
        ApplyFont(tmp, useKeywordFont ? keywordFont : bodyFont);
        return tmp;
    }

    private void ApplyPanelVisual()
    {
        if (panelImage == null)
            return;

        panelImage.sprite = panelBackgroundSprite;
        panelImage.color = panelBackgroundColor;
        panelImage.type = panelBackgroundSprite != null ? Image.Type.Sliced : Image.Type.Simple;
    }

    private void ApplyLayout()
    {
        if (panelRect != null)
        {
            panelRect.anchoredPosition = panelOffset;
            panelRect.sizeDelta = panelSize;
        }

        if (titleText != null)
        {
            RectTransform rect = titleText.rectTransform;
            rect.anchoredPosition = titleOffset;
            rect.sizeDelta = titleSize;
        }

        if (contentScrollRect != null)
        {
            RectTransform rect = contentScrollRect.GetComponent<RectTransform>();
            rect.anchoredPosition = contentOffset;
            rect.sizeDelta = contentSize;
        }

        if (consequenceScrollRect != null)
        {
            RectTransform rect = consequenceScrollRect.GetComponent<RectTransform>();
            rect.anchoredPosition = consequenceViewportOffset;
            rect.sizeDelta = consequenceViewportSize;
        }

        if (illustrationImage != null)
        {
            RectTransform rect = illustrationImage.rectTransform;
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = illustrationOffset;
            rect.sizeDelta = illustrationSize;
        }
    }

    private void ApplyFonts()
    {
        ApplyFont(titleText, keywordFont);
        ApplyFont(contentText, bodyFont);
        ApplyFont(statusText, bodyFont);
        ApplyFont(consequenceTitleText, keywordFont);
        ApplyFont(consequenceText, bodyFont);

        for (int i = 0; i < optionButtons.Count; i++)
        {
            Button button = optionButtons[i];
            if (button == null)
                continue;

            TMP_Text[] texts = button.GetComponentsInChildren<TMP_Text>(true);
            for (int j = 0; j < texts.Length; j++)
            {
                TMP_Text text = texts[j];
                if (text == null)
                    continue;

                bool useKeyword = string.Equals(text.gameObject.name, "Label", StringComparison.Ordinal);
                ApplyFont(text, useKeyword ? keywordFont : bodyFont);
            }
        }

        ApplyFontToButton(confirmButton, keywordFont);
        ApplyFontToButton(closeButton, keywordFont);
    }

    private string FormatOptionLabel(MapEventOptionData option)
    {
        if (option == null)
            return "未命名选项";

        string label = option.DisplayLabel;
        int requiredMoney = Mathf.Max(0, option.requiredMoney);
        if (requiredMoney > 0)
            label = $"{label}  [需 {requiredMoney} 金]";

        return label;
    }

    private string FormatOptionDetail(MapEventOptionData option)
    {
        if (option == null)
            return "暂无后果说明。";

        string detail = !string.IsNullOrWhiteSpace(option.consequenceDescription)
            ? option.consequenceDescription
            : "暂无后果说明。";

        int requiredMoney = Mathf.Max(0, option.requiredMoney);
        if (requiredMoney > 0)
            detail = $"需要支付 {requiredMoney} 金钱\n{detail}";

        return detail;
    }

    private void LateUpdate()
    {
        UpdateScrollContentLayout(contentText, contentContentRect, contentViewportRect);
        UpdateScrollContentLayout(consequenceText, consequenceContentRect, consequenceViewportRect);
    }

    private static void UpdateScrollContentLayout(TextMeshProUGUI text, RectTransform contentRect, RectTransform viewportRect)
    {
        if (text == null || contentRect == null || viewportRect == null)
            return;

        float availableWidth = Mathf.Max(60f, viewportRect.rect.width - 12f);
        text.rectTransform.sizeDelta = new Vector2(availableWidth, text.rectTransform.sizeDelta.y);
        text.ForceMeshUpdate();
        float preferredHeight = Mathf.Max(viewportRect.rect.height, text.preferredHeight + 8f);
        contentRect.sizeDelta = new Vector2(0f, preferredHeight);
    }

    private void SetConfirmButtonInteractable(bool interactable)
    {
        if (confirmButton == null)
            return;

        confirmButton.interactable = interactable;
        Image image = confirmButton.GetComponent<Image>();
        if (image != null)
            image.color = interactable ? actionButtonColor : disabledButtonColor;
    }

    private void SetButtonLabel(Button button, string label)
    {
        if (button == null)
            return;

        TMP_Text text = button.GetComponentInChildren<TMP_Text>(true);
        if (text != null)
            text.text = label;
    }

    private void ApplyFont(TMP_Text text, TMP_FontAsset font)
    {
        if (text == null || font == null)
            return;

        text.font = font;
        text.UpdateMeshPadding();
        text.SetAllDirty();
    }

    private void ApplyFontToButton(Button button, TMP_FontAsset font)
    {
        if (button == null)
            return;

        TMP_Text text = button.GetComponentInChildren<TMP_Text>(true);
        ApplyFont(text, font);
    }

    private void OnValidate()
    {
        if (!Application.isPlaying && canvas == null)
            return;

        if (panelImage != null)
            ApplyPanelVisual();
        ApplyLayout();
        ApplyFonts();
    }
}
