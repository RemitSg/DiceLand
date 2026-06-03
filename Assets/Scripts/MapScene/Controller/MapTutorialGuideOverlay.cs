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
    [SerializeField] private Vector2 panelSize = new Vector2(980f, 300f);
    [SerializeField] private Vector2 panelAnchoredPosition = new Vector2(0f, 120f);
    [SerializeField] private Color backdropColor = new Color(0f, 0f, 0f, 0.3f);
    [SerializeField] private Color panelColor = new Color(0.07f, 0.09f, 0.13f, 0.96f);
    [SerializeField] private Color titleColor = Color.white;
    [SerializeField] private Color bodyColor = new Color(0.86f, 0.9f, 0.96f, 1f);
    [SerializeField] private Color buttonColor = new Color(0.28f, 0.78f, 1f, 1f);
    [SerializeField] private Color buttonTextColor = Color.black;
    [SerializeField] private int titleFontSize = 34;
    [SerializeField] private int bodyFontSize = 24;
    [SerializeField] private int buttonFontSize = 22;

    private Canvas canvas;
    private Text titleText;
    private Text bodyText;
    private Text buttonText;
    private Button continueButton;
    private Action continueHandler;
    private float previousTimeScale = 1f;
    private bool timeScalePaused;

    public bool IsVisible => canvas != null && canvas.gameObject.activeSelf;

    private void Awake()
    {
        EnsureEventSystem();
        EnsureUi();
        Hide();
    }

    public void Show(string title, string body, Action onContinue, string buttonLabel = "继续")
    {
        EnsureUi();
        titleText.text = string.IsNullOrWhiteSpace(title) ? "教程" : title;
        bodyText.text = body ?? string.Empty;
        buttonText.text = string.IsNullOrWhiteSpace(buttonLabel) ? "继续" : buttonLabel;
        continueHandler = onContinue;
        PauseTimeScale();
        canvas.gameObject.SetActive(true);
    }

    public void Hide()
    {
        continueHandler = null;
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

        Font font = Resources.GetBuiltinResource<Font>(FallbackFontName);
        GameObject canvasGo = new GameObject("MapTutorialGuideCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.transform.SetParent(transform, false);
        canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = canvasSortOrder;

        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = referenceResolution;
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform rootRect = canvasGo.GetComponent<RectTransform>();
        Stretch(rootRect);

        Image backdrop = CreateImage(rootRect, "Backdrop", backdropColor);
        Stretch(backdrop.rectTransform);

        RectTransform panelRect = CreateRect("Panel", rootRect, new Vector2(0.5f, 0f), panelAnchoredPosition, panelSize);
        Image panelImage = panelRect.gameObject.AddComponent<Image>();
        panelImage.color = panelColor;

        titleText = CreateText("Title", panelRect, new Vector2(0.5f, 1f), new Vector2(0f, -38f), new Vector2(860f, 58f), font, titleFontSize, TextAnchor.MiddleCenter, titleColor);
        bodyText = CreateText("Body", panelRect, new Vector2(0.5f, 1f), new Vector2(0f, -132f), new Vector2(860f, 132f), font, bodyFontSize, TextAnchor.UpperCenter, bodyColor);
        bodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
        bodyText.verticalOverflow = VerticalWrapMode.Overflow;

        RectTransform buttonRect = CreateRect("ContinueButton", panelRect, new Vector2(0.5f, 0f), new Vector2(0f, 34f), new Vector2(240f, 58f));
        Image buttonImage = buttonRect.gameObject.AddComponent<Image>();
        buttonImage.color = buttonColor;
        continueButton = buttonRect.gameObject.AddComponent<Button>();
        continueButton.targetGraphic = buttonImage;
        continueButton.onClick.AddListener(HandleContinueClicked);
        buttonText = CreateText("Label", buttonRect, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(240f, 58f), font, buttonFontSize, TextAnchor.MiddleCenter, buttonTextColor);
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
