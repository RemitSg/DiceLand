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
    [SerializeField] private Vector2 panelSize = new Vector2(980f, 520f);

    public bool IsVisible => panelRoot != null && panelRoot.gameObject.activeSelf;

    private Canvas canvas;
    private RectTransform canvasRoot;
    private RectTransform panelRoot;
    private TextMeshProUGUI titleText;
    private TextMeshProUGUI bodyText;
    private Button backdropButton;
    private Button continueButton;
    private TextMeshProUGUI continueLabel;
    private Action continueCallback;

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
        SpeedButton.InputSuppressed = true;
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
        if (canvasRoot != null)
            canvasRoot.gameObject.SetActive(false);

        SpeedButton.InputSuppressed = false;

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
        canvas.sortingOrder = 950;

        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

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
}
