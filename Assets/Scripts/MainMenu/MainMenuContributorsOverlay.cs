using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class MainMenuContributorsOverlay : MonoBehaviour
{
    private const string FallbackFontName = "LegacyRuntime.ttf";

    [System.Serializable]
    public sealed class DisplayConfig
    {
        public string title = "Contributors";
        [TextArea(2, 5)] public string openingText = "A small game by people who care a lot.";
        [TextArea(6, 20)] public string creditsText = "Director\nName\n\nProgramming\nName";
        [TextArea(2, 5)] public string endingText = "Thank you for playing.";
        public float fadeDuration = 1.2f;
        public float startDelay = 0.6f;
        public float scrollSpeed = 90f;
        public float endDelay = 1.5f;
        public float horizontalPadding = 120f;
        public float verticalPadding = 180f;
        public Color backgroundColor = Color.black;
        public Color textColor = Color.white;
        public int titleFontSize = 60;
        public int bodyFontSize = 36;
    }

    private Canvas canvas;
    private Text titleText;
    private Text creditsText;
    private Coroutine rollRoutine;
    private Font titleFontOverride;
    private Font bodyFontOverride;

    public bool IsVisible => canvas != null && canvas.gameObject.activeSelf;

    public void ConfigureFonts(Font titleFont, Font bodyFont)
    {
        titleFontOverride = titleFont;
        bodyFontOverride = bodyFont;

        ApplyConfiguredFonts();
    }

    public void Show(DisplayConfig config)
    {
        EnsureUi();
        if (rollRoutine != null)
            StopCoroutine(rollRoutine);

        canvas.gameObject.SetActive(true);
        rollRoutine = StartCoroutine(PlayRoll(config ?? new DisplayConfig()));
    }

    public void Hide()
    {
        if (rollRoutine != null)
        {
            StopCoroutine(rollRoutine);
            rollRoutine = null;
        }

        if (canvas != null)
            canvas.gameObject.SetActive(false);
    }

    private void EnsureUi()
    {
        if (canvas != null)
            return;

        Font titleFont = ResolveTitleFont();
        Font bodyFont = ResolveBodyFont();

        GameObject canvasGo = new GameObject("MainMenuContributorsOverlay", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(Image));

        canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9600;

        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform rootRect = canvasGo.GetComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        Image background = canvasGo.GetComponent<Image>();
        background.color = Color.black;

        titleText = CreateText("Title", rootRect, titleFont, 60, TextAnchor.MiddleLeft);
        titleText.rectTransform.anchorMin = new Vector2(0f, 1f);
        titleText.rectTransform.anchorMax = new Vector2(0f, 1f);
        titleText.rectTransform.pivot = new Vector2(0f, 1f);
        titleText.rectTransform.anchoredPosition = new Vector2(96f, -80f);
        titleText.rectTransform.sizeDelta = new Vector2(720f, 120f);

        creditsText = CreateText("Credits", rootRect, bodyFont, 36, TextAnchor.UpperCenter);
        creditsText.horizontalOverflow = HorizontalWrapMode.Wrap;
        creditsText.verticalOverflow = VerticalWrapMode.Overflow;
        creditsText.rectTransform.anchorMin = new Vector2(0.5f, 0f);
        creditsText.rectTransform.anchorMax = new Vector2(0.5f, 0f);
        creditsText.rectTransform.pivot = new Vector2(0.5f, 1f);
        creditsText.rectTransform.anchoredPosition = new Vector2(0f, -120f);
        creditsText.rectTransform.sizeDelta = new Vector2(1600f, 2000f);

        ApplyConfiguredFonts();
        canvas.gameObject.SetActive(false);
    }

    private IEnumerator PlayRoll(DisplayConfig config)
    {
        Image background = canvas.GetComponent<Image>();
        background.color = config.backgroundColor;

        titleText.color = new Color(config.textColor.r, config.textColor.g, config.textColor.b, 0f);
        titleText.fontSize = config.titleFontSize;
        titleText.text = config.title;

        creditsText.color = config.textColor;
        creditsText.fontSize = config.bodyFontSize;
        creditsText.text = BuildCreditsText(config);

        RectTransform titleRect = titleText.rectTransform;
        RectTransform creditsRect = creditsText.rectTransform;

        creditsRect.sizeDelta = new Vector2(
            Mathf.Max(400f, 1920f - config.horizontalPadding * 2f),
            Mathf.Max(900f, EstimateCreditsHeight(config)));
        creditsRect.anchoredPosition = new Vector2(0f, -Mathf.Max(120f, config.verticalPadding));

        float fadeDuration = Mathf.Max(0.01f, config.fadeDuration);
        yield return new WaitForSecondsRealtime(Mathf.Max(0f, config.startDelay));

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float alpha = Mathf.Clamp01(elapsed / fadeDuration);
            titleText.color = new Color(config.textColor.r, config.textColor.g, config.textColor.b, alpha);
            yield return null;
        }

        float targetY = Screen.height + creditsRect.sizeDelta.y + Mathf.Max(0f, config.verticalPadding);
        while (creditsRect.anchoredPosition.y < targetY)
        {
            creditsRect.anchoredPosition += Vector2.up * (Mathf.Max(10f, config.scrollSpeed) * Time.unscaledDeltaTime);

            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(0))
            {
                Hide();
                yield break;
            }

            yield return null;
        }

        yield return new WaitForSecondsRealtime(Mathf.Max(0f, config.endDelay));
        Hide();
    }

    private void ApplyConfiguredFonts()
    {
        if (titleText != null)
            titleText.font = ResolveTitleFont();

        if (creditsText != null)
            creditsText.font = ResolveBodyFont();
    }

    private Font ResolveTitleFont()
    {
        return titleFontOverride != null ? titleFontOverride : ResolveFallbackFont();
    }

    private Font ResolveBodyFont()
    {
        return bodyFontOverride != null ? bodyFontOverride : ResolveFallbackFont();
    }

    private static Font ResolveFallbackFont()
    {
        return Resources.GetBuiltinResource<Font>(FallbackFontName);
    }

    private static string BuildCreditsText(DisplayConfig config)
    {
        string opening = string.IsNullOrWhiteSpace(config.openingText) ? string.Empty : $"{config.openingText}\n\n";
        string ending = string.IsNullOrWhiteSpace(config.endingText) ? string.Empty : $"\n\n{config.endingText}";
        return $"{opening}{config.creditsText}{ending}";
    }

    private static float EstimateCreditsHeight(DisplayConfig config)
    {
        string content = BuildCreditsText(config);
        int lineCount = string.IsNullOrWhiteSpace(content) ? 1 : content.Split('\n').Length;
        return Mathf.Max(1600f, lineCount * (config.bodyFontSize * 1.6f));
    }

    private static Text CreateText(string name, RectTransform parent, Font font, int fontSize, TextAnchor anchor)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        Text text = go.GetComponent<Text>();
        text.font = font;
        text.fontSize = fontSize;
        text.alignment = anchor;
        text.supportRichText = true;
        return text;
    }
}
