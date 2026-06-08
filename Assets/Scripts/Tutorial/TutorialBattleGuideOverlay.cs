using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class TutorialBattleGuideOverlay : MonoBehaviour
{
    [SerializeField] private TMP_FontAsset keywordFont;
    [SerializeField] private TMP_FontAsset bodyFont;
    [SerializeField] private Color backdropColor = new Color(0f, 0f, 0f, 0.65f);
    [SerializeField] private Color panelColor = new Color(0.09f, 0.08f, 0.06f, 0.97f);
    [SerializeField] private Color titleColor = Color.white;
    [SerializeField] private Color bodyColor = new Color(0.94f, 0.92f, 0.86f, 1f);
    [SerializeField] private Color accentColor = new Color(0.93f, 0.75f, 0.28f, 1f);
    [SerializeField] private Vector2 referenceResolution = new Vector2(1920f, 1080f);
    [SerializeField] private float matchWidthOrHeight = 0.5f;
    [SerializeField] private Vector2 panelSize = new Vector2(980f, 520f);
    [SerializeField] private int canvasSortOrder = 9800;
    [SerializeField] private Color highlightBackdropColor = new Color(0f, 0f, 0f, 0.72f);
    [SerializeField] private Color highlightFrameColor = new Color(1f, 0.84f, 0.28f, 0.98f);
    [SerializeField] private float highlightFrameThickness = 5f;
    [SerializeField] private float highlightBleedPixels = 2f;

    public bool IsVisible => canvasRoot != null && canvasRoot.gameObject.activeSelf;
    public bool IsModalVisible => panelRoot != null && panelRoot.gameObject.activeSelf;
    public bool IsTargetHighlightVisible => highlightRoot != null && highlightRoot.gameObject.activeSelf && !IsModalVisible;

    private Canvas canvas;
    private RectTransform canvasRoot;
    private RectTransform panelRoot;
    private TextMeshProUGUI titleText;
    private TextMeshProUGUI bodyText;
    private Button backdropButton;
    private Button continueButton;
    private TextMeshProUGUI continueLabel;
    private RectTransform highlightDimRoot;
    private RectTransform highlightRoot;
    private readonly RectTransform[] highlightDimRects = new RectTransform[4];
    private Action continueCallback;
    private bool timeScalePaused;
    private float previousTimeScale = 1f;
    private Rect activeTargetScreenRect;

    private void Awake()
    {
        EnsureUi();
        Hide(false);
    }

    public void ConfigureFonts(TMP_FontAsset keywordFontAsset, TMP_FontAsset bodyFontAsset)
    {
        if (keywordFontAsset != null)
            keywordFont = keywordFontAsset;
        if (bodyFontAsset != null)
            bodyFont = bodyFontAsset;

        ApplyFonts();
    }

    public void Show(string title, string body, string buttonLabel, Action onContinue)
    {
        EnsureUi();
        titleText.text = string.IsNullOrWhiteSpace(title) ? "战斗教程" : title;
        bodyText.text = string.IsNullOrWhiteSpace(body) ? "暂无说明。" : body;
        continueLabel.text = string.IsNullOrWhiteSpace(buttonLabel) ? "继续" : buttonLabel;
        continueCallback = onContinue;
        canvasRoot.gameObject.SetActive(true);
        panelRoot.gameObject.SetActive(true);
        backdropButton.gameObject.SetActive(true);
        ConfigureHighlight(false, Rect.zero);
        PauseTimeScale();
        SpeedButton.InputSuppressed = true;
        SpeedButton.ClearTutorialAllowedScreenRect();
    }

    public void ShowTargetHighlight(Rect screenRect)
    {
        EnsureUi();
        continueCallback = null;
        activeTargetScreenRect = screenRect;
        canvasRoot.gameObject.SetActive(true);
        panelRoot.gameObject.SetActive(false);
        backdropButton.gameObject.SetActive(false);
        ConfigureHighlight(true, screenRect);
        RestoreTimeScale();
        SpeedButton.InputSuppressed = false;
        SpeedButton.SetTutorialAllowedScreenRect(screenRect);
    }

    public bool IsScreenPointInsideTarget(Vector2 screenPoint)
    {
        return !IsTargetHighlightVisible || activeTargetScreenRect.Contains(screenPoint);
    }

    public void Hide()
    {
        Hide(true);
    }

    public void Hide(bool invokeCallback)
    {
        if (backdropButton != null)
            backdropButton.gameObject.SetActive(false);
        if (panelRoot != null)
            panelRoot.gameObject.SetActive(false);
        ConfigureHighlight(false, Rect.zero);
        activeTargetScreenRect = default;
        if (canvasRoot != null)
            canvasRoot.gameObject.SetActive(false);

        RestoreTimeScale();
        SpeedButton.InputSuppressed = false;
        SpeedButton.ClearTutorialAllowedScreenRect();

        if (invokeCallback && continueCallback != null)
        {
            Action callback = continueCallback;
            continueCallback = null;
            callback.Invoke();
        }
        else
        {
            continueCallback = null;
        }
    }

    private void EnsureUi()
    {
        if (canvas != null)
            return;

        GameObject canvasGo = new GameObject("TutorialBattleGuideOverlay", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.transform.SetParent(transform, false);

        canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = canvasSortOrder;

        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = referenceResolution;
        scaler.matchWidthOrHeight = matchWidthOrHeight;

        canvasRoot = canvasGo.GetComponent<RectTransform>();
        canvasRoot.anchorMin = Vector2.zero;
        canvasRoot.anchorMax = Vector2.one;
        canvasRoot.offsetMin = Vector2.zero;
        canvasRoot.offsetMax = Vector2.zero;

        GameObject backdropGo = new GameObject("Backdrop", typeof(RectTransform), typeof(Image), typeof(Button));
        backdropGo.transform.SetParent(canvasRoot, false);
        RectTransform backdropRect = backdropGo.GetComponent<RectTransform>();
        backdropRect.anchorMin = Vector2.zero;
        backdropRect.anchorMax = Vector2.one;
        backdropRect.offsetMin = Vector2.zero;
        backdropRect.offsetMax = Vector2.zero;

        Image backdropImage = backdropGo.GetComponent<Image>();
        backdropImage.color = backdropColor;
        backdropButton = backdropGo.GetComponent<Button>();
        backdropButton.onClick.AddListener(() => { });

        highlightDimRoot = CreateRect("HighlightDimming", canvasRoot, new Vector2(0f, 1f), Vector2.zero, Vector2.zero);
        highlightDimRects[0] = CreateImageRect("DimTop", highlightDimRoot, highlightBackdropColor);
        highlightDimRects[1] = CreateImageRect("DimBottom", highlightDimRoot, highlightBackdropColor);
        highlightDimRects[2] = CreateImageRect("DimLeft", highlightDimRoot, highlightBackdropColor);
        highlightDimRects[3] = CreateImageRect("DimRight", highlightDimRoot, highlightBackdropColor);
        highlightDimRoot.gameObject.SetActive(false);

        GameObject panelGo = new GameObject("Panel", typeof(RectTransform), typeof(Image));
        panelGo.transform.SetParent(canvasRoot, false);
        panelRoot = panelGo.GetComponent<RectTransform>();
        panelRoot.anchorMin = new Vector2(0.5f, 0.5f);
        panelRoot.anchorMax = new Vector2(0.5f, 0.5f);
        panelRoot.pivot = new Vector2(0.5f, 0.5f);
        panelRoot.sizeDelta = panelSize;

        Image panelImage = panelGo.GetComponent<Image>();
        panelImage.color = panelColor;

        titleText = CreateText(panelRoot, "Title", new Vector2(42f, -36f), new Vector2(panelSize.x - 84f, 56f), 42, titleColor, true, TextAlignmentOptions.TopLeft);
        bodyText = CreateText(panelRoot, "Body", new Vector2(42f, -112f), new Vector2(panelSize.x - 84f, panelSize.y - 210f), 28, bodyColor, false, TextAlignmentOptions.TopLeft);
        bodyText.enableWordWrapping = true;
        bodyText.overflowMode = TextOverflowModes.Overflow;

        GameObject buttonGo = new GameObject("ContinueButton", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonGo.transform.SetParent(panelRoot, false);
        RectTransform buttonRect = buttonGo.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(1f, 0f);
        buttonRect.anchorMax = new Vector2(1f, 0f);
        buttonRect.pivot = new Vector2(1f, 0f);
        buttonRect.anchoredPosition = new Vector2(-38f, 34f);
        buttonRect.sizeDelta = new Vector2(200f, 68f);

        Image buttonImage = buttonGo.GetComponent<Image>();
        buttonImage.color = accentColor;

        continueButton = buttonGo.GetComponent<Button>();
        continueButton.onClick.AddListener(Hide);

        continueLabel = CreateText(buttonRect, "Label", new Vector2(0f, 0f), buttonRect.sizeDelta, 28, Color.black, true, TextAlignmentOptions.Center);
        continueLabel.rectTransform.anchorMin = Vector2.zero;
        continueLabel.rectTransform.anchorMax = Vector2.one;
        continueLabel.rectTransform.offsetMin = Vector2.zero;
        continueLabel.rectTransform.offsetMax = Vector2.zero;

        highlightRoot = CreateRect("HighlightFrame", canvasRoot, new Vector2(0f, 1f), Vector2.zero, Vector2.zero);
        CreateFrameLine("Top", highlightRoot);
        CreateFrameLine("Bottom", highlightRoot);
        CreateFrameLine("Left", highlightRoot);
        CreateFrameLine("Right", highlightRoot);
        highlightRoot.gameObject.SetActive(false);

        ApplyFonts();
    }

    private void ApplyFonts()
    {
        ApplyFont(titleText, keywordFont != null ? keywordFont : bodyFont);
        ApplyFont(bodyText, bodyFont != null ? bodyFont : keywordFont);
        ApplyFont(continueLabel, keywordFont != null ? keywordFont : bodyFont);
    }

    private TextMeshProUGUI CreateText(RectTransform parent, string name, Vector2 anchoredPosition, Vector2 size, int fontSize, Color color, bool useKeywordFont, TextAlignmentOptions alignment)
    {
        GameObject textGo = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        textGo.transform.SetParent(parent, false);

        RectTransform rect = textGo.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        TextMeshProUGUI text = textGo.GetComponent<TextMeshProUGUI>();
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = alignment;
        ApplyFont(text, useKeywordFont ? keywordFont : bodyFont);
        return text;
    }

    private static void ApplyFont(TMP_Text text, TMP_FontAsset font)
    {
        if (text == null || font == null)
            return;

        text.font = font;
        text.UpdateMeshPadding();
        text.SetAllDirty();
    }

    private void ConfigureHighlight(bool visible, Rect screenRect)
    {
        if (highlightRoot == null)
            return;

        if (highlightDimRoot != null)
            highlightDimRoot.gameObject.SetActive(visible);
        highlightRoot.gameObject.SetActive(visible);
        if (!visible)
            return;

        Vector2 canvasSize = GetCurrentCanvasReferenceSize();
        Rect referenceRect = ConvertScreenRectToReferenceRect(screenRect);
        UpdateHighlightDimBackdrop(referenceRect, canvasSize);

        highlightRoot.anchorMin = new Vector2(0f, 1f);
        highlightRoot.anchorMax = new Vector2(0f, 1f);
        highlightRoot.pivot = new Vector2(0f, 1f);
        highlightRoot.anchoredPosition = new Vector2(referenceRect.xMin, -(canvasSize.y - referenceRect.yMax));
        highlightRoot.sizeDelta = new Vector2(referenceRect.width, referenceRect.height);

        float t = Mathf.Max(1f, highlightFrameThickness);
        SetFrameLine(highlightRoot.Find("Top") as RectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -t * 0.5f), new Vector2(referenceRect.width, t));
        SetFrameLine(highlightRoot.Find("Bottom") as RectTransform, new Vector2(0.5f, 0f), new Vector2(0f, t * 0.5f), new Vector2(referenceRect.width, t));
        SetFrameLine(highlightRoot.Find("Left") as RectTransform, new Vector2(0f, 0.5f), new Vector2(t * 0.5f, 0f), new Vector2(t, referenceRect.height));
        SetFrameLine(highlightRoot.Find("Right") as RectTransform, new Vector2(1f, 0.5f), new Vector2(-t * 0.5f, 0f), new Vector2(t, referenceRect.height));
    }

    private Rect ConvertScreenRectToReferenceRect(Rect screenRect)
    {
        return TutorialHighlightCoordinateUtility.ConvertScreenRectToReferenceRect(
            screenRect,
            new Vector2(Screen.width, Screen.height),
            referenceResolution,
            matchWidthOrHeight,
            highlightBleedPixels);
    }

    private Vector2 GetCurrentCanvasReferenceSize()
    {
        return TutorialHighlightCoordinateUtility.CalculateCanvasReferenceSize(
            new Vector2(Screen.width, Screen.height),
            referenceResolution,
            matchWidthOrHeight);
    }

    private void UpdateHighlightDimBackdrop(Rect referenceRect, Vector2 canvasSize)
    {
        float width = Mathf.Max(1f, canvasSize.x);
        float height = Mathf.Max(1f, canvasSize.y);
        float left = Mathf.Clamp(referenceRect.xMin, 0f, width);
        float right = Mathf.Clamp(referenceRect.xMax, 0f, width);
        float bottom = Mathf.Clamp(referenceRect.yMin, 0f, height);
        float top = Mathf.Clamp(referenceRect.yMax, 0f, height);

        SetTopLeftRect(highlightDimRects[0], 0f, 0f, width, height - top);
        SetTopLeftRect(highlightDimRects[1], 0f, height - bottom, width, bottom);
        SetTopLeftRect(highlightDimRects[2], 0f, height - top, left, top - bottom);
        SetTopLeftRect(highlightDimRects[3], right, height - top, width - right, top - bottom);
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

    private static RectTransform CreateImageRect(string name, Transform parent, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        Image image = go.GetComponent<Image>();
        image.color = color;
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
}
