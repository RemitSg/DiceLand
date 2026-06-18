using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class MapTutorialGuideOverlay : MonoBehaviour
{
    private const string FallbackFontName = "LegacyRuntime.ttf";

    [SerializeField] private int canvasSortOrder = 9650;
    [SerializeField] private Vector2 referenceResolution = new Vector2(1920f, 1080f);
    [SerializeField] private float matchWidthOrHeight = 0.5f;
    [SerializeField] private Vector2 panelSize = new Vector2(980f, 420f);
    [SerializeField] private Vector2 panelAnchoredPosition = new Vector2(0f, 120f);
    [SerializeField] private Color backdropColor = new Color(0f, 0f, 0f, 0.3f);
    [SerializeField] private Color highlightBackdropColor = new Color(0f, 0f, 0f, 0.62f);
    [SerializeField] private Color panelColor = new Color(0.07f, 0.09f, 0.13f, 0.96f);
    [SerializeField] private Color titleColor = Color.white;
    [SerializeField] private Color bodyColor = new Color(0.86f, 0.9f, 0.96f, 1f);
    [SerializeField] private Color buttonColor = new Color(0.28f, 0.78f, 1f, 1f);
    [SerializeField] private Color buttonTextColor = Color.black;
    [SerializeField] private Color highlightFrameColor = new Color(1f, 0.84f, 0.36f, 0.95f);
    [SerializeField] private float highlightFrameThickness = 5f;
    [SerializeField] private float highlightBleedPixels = 2f;
    [Header("Fonts")]
    [SerializeField] private Font titleFont;
    [SerializeField] private Font bodyFont;
    [SerializeField] private Font buttonFont;
    [SerializeField] private int titleFontSize = 34;
    [SerializeField] private int bodyFontSize = 24;
    [SerializeField] private int buttonFontSize = 22;

    private Canvas canvas;
    private Image backdropImage;
    private RectTransform panelRoot;
    private Text titleText;
    private Text bodyText;
    private Text buttonText;
    private Button continueButton;
    private RectTransform highlightDimRoot;
    private RectTransform highlightRoot;
    private readonly RectTransform[] highlightDimRects = new RectTransform[4];
    private Action continueHandler;
    private float previousTimeScale = 1f;
    private bool timeScalePaused;
    private bool showRequested;

    public bool IsVisible => canvas != null && canvas.gameObject.activeInHierarchy;
    public bool IsModalVisible => panelRoot != null && panelRoot.gameObject.activeInHierarchy;
    public bool IsTargetHighlightVisible => highlightRoot != null && highlightRoot.gameObject.activeInHierarchy && !IsModalVisible;

    public void ConfigureFonts(Font titleFontOverride, Font bodyFontOverride, Font buttonFontOverride)
    {
        if (titleFontOverride != null)
            titleFont = titleFontOverride;
        if (bodyFontOverride != null)
            bodyFont = bodyFontOverride;
        if (buttonFontOverride != null)
            buttonFont = buttonFontOverride;

        ApplyFonts();
    }

    private void Awake()
    {
        EnsureEventSystem();
        EnsureUi();
        if (!showRequested)
            Hide();
    }

    public void Show(string title, string body, Action onContinue, string buttonLabel = "继续")
    {
        Show(title, body, onContinue, buttonLabel, false, Vector2.zero, Vector2.zero);
    }

    public void Show(
        string title,
        string body,
        Action onContinue,
        string buttonLabel,
        bool showHighlight,
        Vector2 highlightAnchor,
        Vector2 highlightSize)
    {
        showRequested = true;
        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        EnsureUi();
        if (panelRoot != null)
            panelRoot.gameObject.SetActive(true);
        titleText.text = string.IsNullOrWhiteSpace(title) ? "教程" : title;
        bodyText.text = body ?? string.Empty;
        buttonText.text = string.IsNullOrWhiteSpace(buttonLabel) ? "继续" : buttonLabel;
        continueHandler = onContinue;
        ConfigureHighlight(showHighlight, highlightAnchor, highlightSize);
        PauseTimeScale();
        canvas.gameObject.SetActive(true);
    }

    public void ShowTargetHighlight(Rect screenRect)
    {
        showRequested = true;
        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        EnsureUi();
        continueHandler = null;
        if (panelRoot != null)
            panelRoot.gameObject.SetActive(false);

        ConvertScreenRectToReferenceHighlight(screenRect, out Vector2 anchor, out Vector2 size);
        ConfigureHighlight(true, anchor, size);
        RestoreTimeScale();
        canvas.gameObject.SetActive(true);
    }

    public void Hide()
    {
        showRequested = false;
        continueHandler = null;
        ConfigureHighlight(false, Vector2.zero, Vector2.zero);
        if (panelRoot != null)
            panelRoot.gameObject.SetActive(false);
        RestoreTimeScale();
        if (canvas != null)
            canvas.gameObject.SetActive(false);
    }

    private void HandleContinueClicked()
    {
        Action handler = continueHandler;
        Hide();
        handler?.Invoke();
    }

    private void EnsureUi()
    {
        if (canvas != null)
            return;

        Font font = ResolveFallbackFont();
        GameObject canvasGo = new GameObject("MapTutorialGuideCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.transform.SetParent(transform, false);
        canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = canvasSortOrder;

        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = referenceResolution;
        scaler.matchWidthOrHeight = matchWidthOrHeight;

        RectTransform rootRect = canvasGo.GetComponent<RectTransform>();
        Stretch(rootRect);

        backdropImage = CreateImage(rootRect, "Backdrop", backdropColor);
        Stretch(backdropImage.rectTransform);

        highlightDimRoot = CreateRect("HighlightDimming", rootRect, new Vector2(0f, 1f), Vector2.zero, Vector2.zero);
        highlightDimRects[0] = CreateImageRect("DimTop", highlightDimRoot, highlightBackdropColor);
        highlightDimRects[1] = CreateImageRect("DimBottom", highlightDimRoot, highlightBackdropColor);
        highlightDimRects[2] = CreateImageRect("DimLeft", highlightDimRoot, highlightBackdropColor);
        highlightDimRects[3] = CreateImageRect("DimRight", highlightDimRoot, highlightBackdropColor);
        highlightDimRoot.gameObject.SetActive(false);

        panelRoot = CreateRect("Panel", rootRect, new Vector2(0.5f, 0f), panelAnchoredPosition, panelSize);
        Image panelImage = panelRoot.gameObject.AddComponent<Image>();
        panelImage.color = panelColor;

        titleText = CreateText("Title", panelRoot, new Vector2(0.5f, 1f), new Vector2(0f, -34f), new Vector2(860f, 54f), ResolveTitleFont(font), titleFontSize, TextAnchor.MiddleCenter, titleColor);
        bodyText = CreateText("Body", panelRoot, new Vector2(0.5f, 1f), new Vector2(0f, -94f), new Vector2(860f, 230f), ResolveBodyFont(font), bodyFontSize, TextAnchor.UpperCenter, bodyColor);
        bodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
        bodyText.verticalOverflow = VerticalWrapMode.Overflow;

        RectTransform buttonRect = CreateRect("ContinueButton", panelRoot, new Vector2(0.5f, 0f), new Vector2(0f, 26f), new Vector2(240f, 58f));
        Image buttonImage = buttonRect.gameObject.AddComponent<Image>();
        buttonImage.color = buttonColor;
        continueButton = buttonRect.gameObject.AddComponent<Button>();
        continueButton.targetGraphic = buttonImage;
        continueButton.onClick.AddListener(HandleContinueClicked);
        buttonText = CreateText("Label", buttonRect, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(240f, 58f), ResolveButtonFont(font), buttonFontSize, TextAnchor.MiddleCenter, buttonTextColor);

        highlightRoot = CreateRect("HighlightFrame", rootRect, new Vector2(0f, 1f), Vector2.zero, Vector2.zero);
        CreateFrameLine("Top", highlightRoot);
        CreateFrameLine("Bottom", highlightRoot);
        CreateFrameLine("Left", highlightRoot);
        CreateFrameLine("Right", highlightRoot);
        highlightRoot.gameObject.SetActive(false);
    }

    private void ApplyFonts()
    {
        Font fallbackFont = ResolveFallbackFont();
        if (titleText != null)
            titleText.font = ResolveTitleFont(fallbackFont);
        if (bodyText != null)
            bodyText.font = ResolveBodyFont(fallbackFont);
        if (buttonText != null)
            buttonText.font = ResolveButtonFont(fallbackFont);
    }

    private Font ResolveTitleFont(Font fallbackFont)
    {
        return titleFont != null ? titleFont : fallbackFont;
    }

    private Font ResolveBodyFont(Font fallbackFont)
    {
        return bodyFont != null ? bodyFont : fallbackFont;
    }

    private Font ResolveButtonFont(Font fallbackFont)
    {
        if (buttonFont != null)
            return buttonFont;
        if (bodyFont != null)
            return bodyFont;
        return fallbackFont;
    }

    private static Font ResolveFallbackFont()
    {
        return Resources.GetBuiltinResource<Font>(FallbackFontName);
    }

    private void ConfigureHighlight(bool visible, Vector2 anchor, Vector2 size)
    {
        if (highlightRoot == null)
            return;

        if (backdropImage != null)
            backdropImage.gameObject.SetActive(!visible);
        if (highlightDimRoot != null)
            highlightDimRoot.gameObject.SetActive(visible);

        highlightRoot.gameObject.SetActive(visible);
        if (!visible)
            return;

        Vector2 canvasSize = GetCurrentCanvasReferenceSize();
        UpdateHighlightDimBackdrop(anchor, size, canvasSize);

        highlightRoot.anchorMin = new Vector2(0f, 1f);
        highlightRoot.anchorMax = new Vector2(0f, 1f);
        highlightRoot.pivot = new Vector2(0f, 1f);
        highlightRoot.anchoredPosition = anchor;
        highlightRoot.sizeDelta = size;

        float t = Mathf.Max(1f, highlightFrameThickness);
        SetFrameLine(highlightRoot.Find("Top") as RectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -t * 0.5f), new Vector2(size.x, t));
        SetFrameLine(highlightRoot.Find("Bottom") as RectTransform, new Vector2(0.5f, 0f), new Vector2(0f, t * 0.5f), new Vector2(size.x, t));
        SetFrameLine(highlightRoot.Find("Left") as RectTransform, new Vector2(0f, 0.5f), new Vector2(t * 0.5f, 0f), new Vector2(t, size.y));
        SetFrameLine(highlightRoot.Find("Right") as RectTransform, new Vector2(1f, 0.5f), new Vector2(-t * 0.5f, 0f), new Vector2(t, size.y));
    }

    private void UpdateHighlightDimBackdrop(Vector2 anchor, Vector2 size, Vector2 canvasSize)
    {
        if (highlightDimRoot == null)
            return;

        float width = Mathf.Max(1f, canvasSize.x);
        float height = Mathf.Max(1f, canvasSize.y);
        float left = Mathf.Clamp(anchor.x, 0f, width);
        float top = Mathf.Clamp(-anchor.y, 0f, height);
        float right = Mathf.Clamp(left + Mathf.Max(0f, size.x), 0f, width);
        float bottom = Mathf.Clamp(top + Mathf.Max(0f, size.y), 0f, height);

        SetTopLeftRect(highlightDimRects[0], 0f, 0f, width, top);
        SetTopLeftRect(highlightDimRects[1], 0f, bottom, width, height - bottom);
        SetTopLeftRect(highlightDimRects[2], 0f, top, left, size.y);
        SetTopLeftRect(highlightDimRects[3], right, top, width - right, size.y);
    }

    private static void SetTopLeftRect(RectTransform rect, float x, float y, float width, float height)
    {
        if (rect == null)
            return;

        bool active = width > 0.5f && height > 0.5f;
        rect.gameObject.SetActive(active);
        if (!active)
            return;

        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(x, -y);
        rect.sizeDelta = new Vector2(width, height);
    }

    private void ConvertScreenRectToReferenceHighlight(Rect screenRect, out Vector2 anchor, out Vector2 size)
    {
        Vector2 canvasSize = GetCurrentCanvasReferenceSize();
        Rect referenceRect = TutorialHighlightCoordinateUtility.ConvertScreenRectToReferenceRect(
            screenRect,
            new Vector2(Screen.width, Screen.height),
            referenceResolution,
            matchWidthOrHeight,
            highlightBleedPixels);
        float xMin = Mathf.Clamp(referenceRect.xMin, 0f, canvasSize.x);
        float xMax = Mathf.Clamp(referenceRect.xMax, 0f, canvasSize.x);
        float yMin = Mathf.Clamp(referenceRect.yMin, 0f, canvasSize.y);
        float yMax = Mathf.Clamp(referenceRect.yMax, 0f, canvasSize.y);

        size = new Vector2(Mathf.Max(0f, xMax - xMin), Mathf.Max(0f, yMax - yMin));
        anchor = new Vector2(xMin, -(canvasSize.y - yMax));
    }

    private Vector2 GetCurrentCanvasReferenceSize()
    {
        return TutorialHighlightCoordinateUtility.CalculateCanvasReferenceSize(
            new Vector2(Screen.width, Screen.height),
            referenceResolution,
            matchWidthOrHeight);
    }

    private void OnDestroy()
    {
        RestoreTimeScale();
    }

    private void PauseTimeScale()
    {
        if (timeScalePaused)
            return;

        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        timeScalePaused = true;
    }

    private void RestoreTimeScale()
    {
        if (!timeScalePaused)
            return;

        Time.timeScale = previousTimeScale;
        timeScalePaused = false;
    }

    private static RectTransform CreateRect(string name, Transform parent, Vector2 anchorPivot, Vector2 anchoredPosition, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchorPivot;
        rect.anchorMax = anchorPivot;
        rect.pivot = anchorPivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
        return rect;
    }

    private static Image CreateImage(Transform parent, string name, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        Image image = go.GetComponent<Image>();
        image.color = color;
        return image;
    }

    private static RectTransform CreateImageRect(string name, Transform parent, Color color)
    {
        Image image = CreateImage(parent, name, color);
        return image.rectTransform;
    }

    private RectTransform CreateFrameLine(string name, Transform parent)
    {
        RectTransform line = CreateRect(name, parent, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        Image image = line.gameObject.AddComponent<Image>();
        image.color = highlightFrameColor;
        image.raycastTarget = false;
        return line;
    }

    private static void SetFrameLine(RectTransform line, Vector2 anchorPivot, Vector2 anchoredPosition, Vector2 size)
    {
        if (line == null)
            return;

        line.anchorMin = anchorPivot;
        line.anchorMax = anchorPivot;
        line.pivot = anchorPivot;
        line.anchoredPosition = anchoredPosition;
        line.sizeDelta = size;
    }

    private static Text CreateText(string name, Transform parent, Vector2 anchorPivot, Vector2 anchoredPosition, Vector2 size, Font font, int fontSize, TextAnchor alignment, Color color)
    {
        RectTransform rect = CreateRect(name, parent, anchorPivot, anchoredPosition, size);
        Text text = rect.gameObject.AddComponent<Text>();
        text.font = font;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = color;
        text.text = string.Empty;
        return text;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void EnsureEventSystem()
    {
        if (EventSystem.current != null)
            return;

        GameObject eventSystemGo = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        DontDestroyOnLoad(eventSystemGo);
    }
}
