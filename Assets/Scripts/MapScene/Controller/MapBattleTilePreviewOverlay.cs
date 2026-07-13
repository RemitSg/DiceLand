using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class MapBattleTilePreviewOverlay : MonoBehaviour
{
    private const int CanvasSortingOrder = 8010;
    private const float LandscapeAspectThreshold = 1.45f;
    private static readonly Vector2 ReferenceResolution = new Vector2(1920f, 1080f);
    private static readonly Vector2 LandscapePanelSize = new Vector2(500f, 620f);
    private static readonly Vector2 PortraitPanelSize = new Vector2(920f, 330f);

    [Header("Fonts")]
    [SerializeField] private TMP_FontAsset keywordFont;
    [SerializeField] private TMP_FontAsset bodyFont;
    [SerializeField] private TMP_FontAsset attributeFont;

    [Header("Colors")]
    [SerializeField] private Color panelColor = new Color(0.075f, 0.085f, 0.09f, 0.97f);
    [SerializeField] private Color bodyColor = new Color(0.9f, 0.91f, 0.9f, 1f);
    [SerializeField] private Color mutedColor = new Color(0.67f, 0.7f, 0.69f, 1f);
    [SerializeField] private Color failureColor = new Color(1f, 0.55f, 0.46f, 1f);
    [SerializeField] private Color ordinaryAccent = new Color(0.32f, 0.65f, 0.68f, 1f);
    [SerializeField] private Color eliteAccent = new Color(0.92f, 0.66f, 0.24f, 1f);
    [SerializeField] private Color bossAccent = new Color(0.72f, 0.18f, 0.2f, 1f);

    public event Action ConfirmRequested;
    public event Action CancelRequested;

    private Canvas canvas;
    private RectTransform panel;
    private Image panelImage;
    private Image accentImage;
    private TextMeshProUGUI battleClassText;
    private TextMeshProUGUI dangerText;
    private TextMeshProUGUI attributeHeadingText;
    private readonly TextMeshProUGUI[] attributeTexts = new TextMeshProUGUI[3];
    private TextMeshProUGUI statusText;
    private Button confirmButton;
    private Button returnButton;
    private TextMeshProUGUI confirmLabelText;
    private TextMeshProUGUI returnLabelText;
    private MapBattleTilePreviewContext currentContext;
    private bool interactionEnabled = true;
    private Vector2 cachedScreenSize;
    private Rect cachedSafeArea;
    private bool hasCachedLayout;
    private bool isApplyingResponsiveLayout;

    public bool IsVisible => canvas != null && canvas.gameObject.activeInHierarchy;

    public void ConfigureFonts(TMP_FontAsset keyword, TMP_FontAsset body, TMP_FontAsset attribute)
    {
        if (keyword != null)
            keywordFont = keyword;
        if (body != null)
            bodyFont = body;
        if (attribute != null)
            attributeFont = attribute;

        ApplyFonts();
    }

    public void Show(MapBattleTilePreviewContext context)
    {
        EnsureUi();
        currentContext = context;
        interactionEnabled = true;
        gameObject.SetActive(true);
        canvas.gameObject.SetActive(true);
        ResetResponsiveLayoutCache();
        RefreshResponsiveLayoutIfChanged(GetCurrentScreenSize(), Screen.safeArea);
        RenderContext();
    }

    public void SetInteractionEnabled(bool enabled)
    {
        interactionEnabled = enabled;
        RefreshButtonStates();
    }

    public void ShowFailure(string message)
    {
        if (!IsVisible)
            return;

        interactionEnabled = true;
        statusText.text = string.IsNullOrWhiteSpace(message) ? "战斗预览当前不可用。" : message;
        statusText.color = failureColor;
        RefreshButtonStates();
    }

    public void Hide()
    {
        currentContext = null;
        interactionEnabled = true;
        ResetResponsiveLayoutCache();
        if (statusText != null)
        {
            statusText.text = string.Empty;
            statusText.color = mutedColor;
        }

        if (canvas != null)
            canvas.gameObject.SetActive(false);
    }

    public void ApplyResponsiveLayout(Vector2 screenSize)
    {
        ApplyResponsiveLayout(screenSize, new Rect(Vector2.zero, screenSize));
    }

    public void ApplyResponsiveLayout()
    {
        ApplyResponsiveLayout(GetCurrentScreenSize(), Screen.safeArea);
    }

    public void ApplyResponsiveLayout(Vector2 screenSize, Rect safeArea)
    {
        EnsureUi();
        NormalizeLayoutInputs(ref screenSize, ref safeArea);
        ApplyResponsiveLayoutInternal(screenSize, safeArea);
    }

    public bool RefreshResponsiveLayoutIfChanged(Vector2 screenSize, Rect safeArea)
    {
        if (!IsVisible || isApplyingResponsiveLayout)
            return false;

        NormalizeLayoutInputs(ref screenSize, ref safeArea);
        if (hasCachedLayout && cachedScreenSize == screenSize && cachedSafeArea == safeArea)
            return false;

        ApplyResponsiveLayoutInternal(screenSize, safeArea);
        return true;
    }

    private void ApplyResponsiveLayoutInternal(Vector2 screenSize, Rect safeArea)
    {
        if (isApplyingResponsiveLayout)
            return;

        isApplyingResponsiveLayout = true;
        cachedScreenSize = screenSize;
        cachedSafeArea = safeArea;
        hasCachedLayout = true;

        try
        {
            float scaleFactor = ResolveReferenceScaleFactor(screenSize);
            float canvasWidth = screenSize.x / scaleFactor;
            float canvasHeight = screenSize.y / scaleFactor;
            float safeLeft = safeArea.xMin / scaleFactor;
            float safeRight = (screenSize.x - safeArea.xMax) / scaleFactor;
            float safeBottom = safeArea.yMin / scaleFactor;
            float safeTop = (screenSize.y - safeArea.yMax) / scaleFactor;

            if (screenSize.x / screenSize.y >= LandscapeAspectThreshold)
                ApplyLandscapeLayout(canvasHeight, safeRight, safeBottom, safeTop);
            else
                ApplyPortraitLayout(canvasWidth, safeLeft, safeRight, safeBottom);
        }
        finally
        {
            isApplyingResponsiveLayout = false;
        }
    }

    public void HandleCancelInput()
    {
        if (!IsVisible || !interactionEnabled)
            return;

        CancelRequested?.Invoke();
    }

    private void LateUpdate()
    {
        if (IsVisible)
            RefreshResponsiveLayoutIfChanged(GetCurrentScreenSize(), Screen.safeArea);
    }

    private void OnRectTransformDimensionsChange()
    {
        if (IsVisible)
            RefreshResponsiveLayoutIfChanged(GetCurrentScreenSize(), Screen.safeArea);
    }

    private void OnDestroy()
    {
        if (confirmButton != null)
            confirmButton.onClick.RemoveListener(OnConfirmClicked);
        if (returnButton != null)
            returnButton.onClick.RemoveListener(OnReturnClicked);
    }

    private void EnsureUi()
    {
        EventSystemUtility.EnsureSingleEventSystem();
        if (canvas != null)
            return;

        GameObject canvasObject = new GameObject(
            "BattleTilePreviewCanvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);

        canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = CanvasSortingOrder;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = ReferenceResolution;
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
        Stretch(canvasRect);

        panel = CreateRect("PreviewPanel", canvasRect);
        panelImage = panel.gameObject.AddComponent<Image>();
        panelImage.color = panelColor;

        RectTransform accent = CreateRect("ClassAccent", panel);
        accentImage = accent.gameObject.AddComponent<Image>();
        accentImage.raycastTarget = false;

        battleClassText = CreateText("BattleClassText", panel, 34, FontStyles.Bold);
        dangerText = CreateText("DangerText", panel, 24, FontStyles.Normal);
        dangerText.richText = true;
        attributeHeadingText = CreateText("AttributeHeadingText", panel, 20, FontStyles.Normal);
        attributeHeadingText.text = "\u5c5e\u6027\u503e\u5411";
        attributeHeadingText.color = mutedColor;

        for (int i = 0; i < attributeTexts.Length; i++)
            attributeTexts[i] = CreateText($"Attribute{i}", panel, 22, FontStyles.Bold);

        statusText = CreateText("StatusText", panel, 20, FontStyles.Normal);
        statusText.enableWordWrapping = true;
        statusText.overflowMode = TextOverflowModes.Ellipsis;

        confirmButton = CreateButton("ConfirmButton", panel, out confirmLabelText, "ConfirmLabel");
        returnButton = CreateButton("ReturnButton", panel, out returnLabelText, "ReturnLabel");
        confirmButton.onClick.AddListener(OnConfirmClicked);
        returnButton.onClick.AddListener(OnReturnClicked);

        ApplyFonts();
        ApplyLandscapeLayout(ReferenceResolution.y, 0f, 0f, 0f);
        canvasObject.SetActive(false);
    }

    private void RenderContext()
    {
        bool valid = IsValidContext(currentContext);
        MapTileType tileType = valid ? currentContext.tile.tileType : MapTileType.Battle;
        Color accent = ResolveAccent(tileType);
        accentImage.color = accent;
        battleClassText.color = accent;
        battleClassText.text = valid ? GetBattleClassLabel(tileType) : "战斗预览";

        if (valid)
        {
            int dangerRating = MapDangerRatingService.ClampRating(currentContext.dangerRating);
            dangerText.text = $"\u5371\u9669\u5ea6: {MapDangerRatingService.GetRatingRichText(dangerRating)}  {dangerRating}/8";
            SetAttributes(currentContext.attributeNames);
            confirmLabelText.text = string.IsNullOrWhiteSpace(currentContext.confirmLabel)
                ? "进入战斗"
                : currentContext.confirmLabel;
            statusText.text = currentContext.canConfirm
                ? string.Empty
                : (string.IsNullOrWhiteSpace(currentContext.blockReason) ? "当前无法进入战斗。" : currentContext.blockReason);
        }
        else
        {
            dangerText.text = string.Empty;
            SetAttributes(Array.Empty<string>());
            confirmLabelText.text = "进入战斗";
            statusText.text = "预览不可用：该格子当前无法进入战斗。";
        }

        statusText.color = mutedColor;
        returnLabelText.text = "返回地图";
        RefreshButtonStates();
        ApplyFonts();
    }

    private void SetAttributes(string[] names)
    {
        int visibleCount = 0;
        for (int sourceIndex = 0; names != null && sourceIndex < names.Length && visibleCount < attributeTexts.Length; sourceIndex++)
        {
            string value = names[sourceIndex];
            if (string.IsNullOrWhiteSpace(value))
                continue;

            TextMeshProUGUI label = attributeTexts[visibleCount++];
            label.text = value.Trim();
            label.gameObject.SetActive(true);
        }

        for (int i = visibleCount; i < attributeTexts.Length; i++)
        {
            attributeTexts[i].text = string.Empty;
            attributeTexts[i].gameObject.SetActive(false);
        }

        attributeHeadingText.gameObject.SetActive(visibleCount > 0);
    }

    private void RefreshButtonStates()
    {
        if (confirmButton == null || returnButton == null)
            return;

        confirmButton.interactable = interactionEnabled && IsValidContext(currentContext) && currentContext.canConfirm;
        returnButton.interactable = interactionEnabled;
    }

    private void ApplyFonts()
    {
        ApplyFont(battleClassText, keywordFont);
        ApplyFont(dangerText, bodyFont);
        ApplyFont(attributeHeadingText, bodyFont);
        ApplyFont(statusText, bodyFont);
        for (int i = 0; i < attributeTexts.Length; i++)
            ApplyFont(attributeTexts[i], attributeFont);
        ApplyFont(confirmLabelText, keywordFont);
        ApplyFont(returnLabelText, keywordFont);
    }

    private void ApplyLandscapeLayout(
        float canvasHeight,
        float safeRight,
        float safeBottom,
        float safeTop)
    {
        if (panel == null)
            return;

        const float horizontalMargin = 40f;
        const float verticalMargin = 24f;
        float safeHeight = Mathf.Max(1f, canvasHeight - safeBottom - safeTop);
        float panelHeight = Mathf.Min(LandscapePanelSize.y, Mathf.Max(1f, safeHeight - verticalMargin * 2f));
        float safeCenterY = safeBottom + safeHeight * 0.5f;

        panel.anchorMin = new Vector2(1f, 0.5f);
        panel.anchorMax = new Vector2(1f, 0.5f);
        panel.pivot = new Vector2(1f, 0.5f);
        panel.sizeDelta = new Vector2(LandscapePanelSize.x, panelHeight);
        panel.anchoredPosition = new Vector2(
            -horizontalMargin - safeRight,
            safeCenterY - canvasHeight * 0.5f);

        SetTopStretch(accentImage.rectTransform, 0f, 0f, 6f);
        SetTopLeft(battleClassText.rectTransform, 32f, 26f, 436f, 48f);
        SetTopLeft(dangerText.rectTransform, 32f, 88f, 436f, 42f);
        SetTopLeft(attributeHeadingText.rectTransform, 32f, 150f, 436f, 28f);
        for (int i = 0; i < attributeTexts.Length; i++)
            SetTopLeft(attributeTexts[i].rectTransform, 44f, 188f + i * 42f, 424f, 34f);
        SetTopLeft(statusText.rectTransform, 32f, 326f, 436f, 104f);
        SetBottomLeft(confirmButton.GetComponent<RectTransform>(), 32f, 96f, 436f, 58f);
        SetBottomLeft(returnButton.GetComponent<RectTransform>(), 32f, 28f, 436f, 52f);

        battleClassText.alignment = TextAlignmentOptions.Left;
        dangerText.alignment = TextAlignmentOptions.Left;
        statusText.alignment = TextAlignmentOptions.TopLeft;
    }

    private void ApplyPortraitLayout(
        float canvasWidth,
        float safeLeft,
        float safeRight,
        float safeBottom)
    {
        if (panel == null)
            return;

        const float horizontalMargin = 16f;
        const float bottomMargin = 24f;
        float safeWidth = Mathf.Max(1f, canvasWidth - safeLeft - safeRight);
        float panelWidth = Mathf.Min(
            PortraitPanelSize.x,
            Mathf.Max(1f, safeWidth - horizontalMargin * 2f));
        float safeCenterX = safeLeft + safeWidth * 0.5f;
        panel.anchorMin = new Vector2(0.5f, 0f);
        panel.anchorMax = new Vector2(0.5f, 0f);
        panel.pivot = new Vector2(0.5f, 0f);
        panel.sizeDelta = new Vector2(panelWidth, PortraitPanelSize.y);
        panel.anchoredPosition = new Vector2(
            safeCenterX - canvasWidth * 0.5f,
            safeBottom + bottomMargin);

        float leftWidth = Mathf.Min(500f, panelWidth * 0.56f);
        float rightX = leftWidth + 24f;
        float rightWidth = Mathf.Max(120f, panelWidth - rightX - 32f);
        float attributeWidth = Mathf.Max(78f, (leftWidth - 56f) / 3f);
        float buttonWidth = Mathf.Max(120f, (panelWidth - 80f) / 2f);

        SetTopStretch(accentImage.rectTransform, 0f, 0f, 6f);
        SetTopLeft(battleClassText.rectTransform, 32f, 18f, leftWidth - 48f, 56f);
        SetTopLeft(dangerText.rectTransform, 32f, 80f, leftWidth - 48f, 42f);
        SetTopLeft(attributeHeadingText.rectTransform, 32f, 132f, leftWidth - 48f, 34f);
        for (int i = 0; i < attributeTexts.Length; i++)
            SetTopLeft(attributeTexts[i].rectTransform, 32f + i * (attributeWidth + 8f), 172f, attributeWidth, 36f);
        SetTopLeft(statusText.rectTransform, rightX, 26f, rightWidth, 116f);
        SetBottomLeft(returnButton.GetComponent<RectTransform>(), 32f, 22f, buttonWidth, 58f);
        SetBottomLeft(confirmButton.GetComponent<RectTransform>(), 48f + buttonWidth, 22f, buttonWidth, 58f);

        battleClassText.alignment = TextAlignmentOptions.Left;
        dangerText.alignment = TextAlignmentOptions.Left;
        statusText.alignment = TextAlignmentOptions.TopLeft;
    }

    private Button CreateButton(
        string name,
        RectTransform parent,
        out TextMeshProUGUI label,
        string labelName)
    {
        RectTransform rect = CreateRect(name, parent);
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = new Color(0.22f, 0.24f, 0.24f, 1f);

        Button button = rect.gameObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.22f, 0.24f, 0.24f, 1f);
        colors.highlightedColor = new Color(0.32f, 0.35f, 0.34f, 1f);
        colors.pressedColor = new Color(0.15f, 0.17f, 0.17f, 1f);
        colors.disabledColor = new Color(0.13f, 0.14f, 0.14f, 0.86f);
        colors.colorMultiplier = 1f;
        button.colors = colors;

        label = CreateText(labelName, rect, 21, FontStyles.Bold);
        Stretch(label.rectTransform);
        label.alignment = TextAlignmentOptions.Center;
        label.color = bodyColor;
        return button;
    }

    private TextMeshProUGUI CreateText(string name, RectTransform parent, float size, FontStyles style)
    {
        RectTransform rect = CreateRect(name, parent);
        TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.fontSize = size;
        text.fontStyle = style;
        text.color = bodyColor;
        text.raycastTarget = false;
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Ellipsis;
        return text;
    }

    private static RectTransform CreateRect(string name, RectTransform parent)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform));
        RectTransform rect = gameObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        return rect;
    }

    private static void SetTopLeft(RectTransform rect, float x, float y, float width, float height)
    {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(x, -y);
        rect.sizeDelta = new Vector2(width, height);
    }

    private static void SetBottomLeft(RectTransform rect, float x, float y, float width, float height)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.zero;
        rect.pivot = Vector2.zero;
        rect.anchoredPosition = new Vector2(x, y);
        rect.sizeDelta = new Vector2(width, height);
    }

    private static void SetTopStretch(RectTransform rect, float left, float right, float height)
    {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 1f);
        rect.offsetMin = new Vector2(left, -height);
        rect.offsetMax = new Vector2(-right, 0f);
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static bool IsValidContext(MapBattleTilePreviewContext context)
    {
        return context != null &&
               context.canPreview &&
               context.tile != null &&
               context.tile.IsHostileTile;
    }

    private static string GetBattleClassLabel(MapTileType tileType)
    {
        switch (tileType)
        {
            case MapTileType.Elite:
                return "\u7cbe\u82f1\u6218\u6597";
            case MapTileType.Boss:
                return "Boss";
            case MapTileType.Battle:
            default:
                return "\u666e\u901a\u6218\u6597";
        }
    }

    private Color ResolveAccent(MapTileType tileType)
    {
        switch (tileType)
        {
            case MapTileType.Elite:
                return eliteAccent;
            case MapTileType.Boss:
                return bossAccent;
            case MapTileType.Battle:
            default:
                return ordinaryAccent;
        }
    }

    private static void ApplyFont(TMP_Text text, TMP_FontAsset font)
    {
        if (text != null && font != null)
            text.font = font;
    }

    private static Vector2 GetCurrentScreenSize()
    {
        return new Vector2(Screen.width, Screen.height);
    }

    private static Rect NormalizeSafeArea(Vector2 screenSize, Rect safeArea)
    {
        float xMin = Mathf.Clamp(safeArea.xMin, 0f, screenSize.x);
        float yMin = Mathf.Clamp(safeArea.yMin, 0f, screenSize.y);
        float xMax = Mathf.Clamp(safeArea.xMax, 0f, screenSize.x);
        float yMax = Mathf.Clamp(safeArea.yMax, 0f, screenSize.y);
        if (xMax <= xMin || yMax <= yMin)
            return new Rect(Vector2.zero, screenSize);

        return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
    }

    private static void NormalizeLayoutInputs(ref Vector2 screenSize, ref Rect safeArea)
    {
        if (screenSize.x <= 0f || screenSize.y <= 0f)
        {
            screenSize = ReferenceResolution;
            safeArea = new Rect(Vector2.zero, screenSize);
            return;
        }

        safeArea = NormalizeSafeArea(screenSize, safeArea);
    }

    private void ResetResponsiveLayoutCache()
    {
        cachedScreenSize = Vector2.zero;
        cachedSafeArea = default;
        hasCachedLayout = false;
    }

    private static float ResolveReferenceScaleFactor(Vector2 screenSize)
    {
        float widthScale = screenSize.x / ReferenceResolution.x;
        float heightScale = screenSize.y / ReferenceResolution.y;
        return Mathf.Sqrt(widthScale * heightScale);
    }

    private void OnConfirmClicked()
    {
        if (confirmButton.interactable)
            ConfirmRequested?.Invoke();
    }

    private void OnReturnClicked()
    {
        if (returnButton.interactable)
            CancelRequested?.Invoke();
    }
}
