using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuContributorsRollController : MonoBehaviour
{
    private DemoScenesGalleryManager owner;
    private Canvas canvasRoot;
    private GraphicRaycaster graphicRaycaster;
    private Image backgroundImage;
    private RectTransform viewportRect;
    private RectTransform scrollRootRect;
    private TextMeshProUGUI creditsText;
    private Coroutine playCoroutine;
    private bool initialized;

    public void Initialize(DemoScenesGalleryManager manager)
    {
        owner = manager;

        if (!initialized)
        {
            EnsureUi();
            initialized = true;
        }

        gameObject.SetActive(false);
    }

    public void Play(DemoScenesGalleryManager.ContributorsRollSettings settings)
    {
        if (settings == null)
            return;

        Initialize(owner);

        if (playCoroutine != null)
            StopCoroutine(playCoroutine);

        gameObject.SetActive(true);
        transform.SetAsLastSibling();
        playCoroutine = StartCoroutine(PlayRoutine(settings));
    }

    private void EnsureUi()
    {
        RectTransform hostRect = transform as RectTransform;
        if (hostRect == null)
            hostRect = gameObject.AddComponent<RectTransform>();

        hostRect.anchorMin = Vector2.zero;
        hostRect.anchorMax = Vector2.one;
        hostRect.offsetMin = Vector2.zero;
        hostRect.offsetMax = Vector2.zero;
        hostRect.localScale = Vector3.one;

        canvasRoot = GetComponent<Canvas>();
        if (canvasRoot == null)
            canvasRoot = gameObject.AddComponent<Canvas>();
        canvasRoot.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasRoot.sortingOrder = 20000;

        if (GetComponent<CanvasScaler>() == null)
        {
            CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
        }

        graphicRaycaster = GetComponent<GraphicRaycaster>();
        if (graphicRaycaster == null)
            graphicRaycaster = gameObject.AddComponent<GraphicRaycaster>();

        GameObject backgroundObject = new GameObject("Background", typeof(RectTransform), typeof(Image));
        backgroundObject.transform.SetParent(transform, false);

        RectTransform backgroundRect = backgroundObject.GetComponent<RectTransform>();
        backgroundRect.anchorMin = Vector2.zero;
        backgroundRect.anchorMax = Vector2.one;
        backgroundRect.offsetMin = Vector2.zero;
        backgroundRect.offsetMax = Vector2.zero;

        backgroundImage = backgroundObject.GetComponent<Image>();
        backgroundImage.raycastTarget = true;
        backgroundImage.color = new Color(0f, 0f, 0f, 0f);

        GameObject viewportObject = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
        viewportObject.transform.SetParent(transform, false);

        viewportRect = viewportObject.GetComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = Vector2.zero;
        viewportRect.offsetMax = Vector2.zero;

        GameObject scrollRootObject = new GameObject("Scroll Root", typeof(RectTransform));
        scrollRootObject.transform.SetParent(viewportRect, false);
        scrollRootRect = scrollRootObject.GetComponent<RectTransform>();
        scrollRootRect.anchorMin = new Vector2(0.5f, 0f);
        scrollRootRect.anchorMax = new Vector2(0.5f, 0f);
        scrollRootRect.pivot = new Vector2(0.5f, 0f);
        scrollRootRect.anchoredPosition = Vector2.zero;

        GameObject textObject = new GameObject("Credits Text", typeof(RectTransform));
        textObject.transform.SetParent(scrollRootRect, false);
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.5f, 0f);
        textRect.anchorMax = new Vector2(0.5f, 0f);
        textRect.pivot = new Vector2(0.5f, 0f);
        textRect.anchoredPosition = Vector2.zero;

        creditsText = textObject.AddComponent<TextMeshProUGUI>();
        creditsText.enableWordWrapping = true;
        creditsText.overflowMode = TextOverflowModes.Overflow;
        creditsText.alignment = TextAlignmentOptions.Center;
        creditsText.raycastTarget = false;
        creditsText.richText = true;
        creditsText.color = Color.white;
        creditsText.text = string.Empty;
    }

    private IEnumerator PlayRoutine(DemoScenesGalleryManager.ContributorsRollSettings settings)
    {
        backgroundImage.color = new Color(settings.backgroundColor.r, settings.backgroundColor.g, settings.backgroundColor.b, 0f);
        creditsText.alpha = 0f;
        creditsText.color = settings.textColor;
        creditsText.fontSize = settings.bodyFontSize;
        creditsText.text = BuildCreditsMarkup(settings);

        float viewportWidth = Mathf.Max(200f, viewportRect.rect.width - (settings.horizontalPadding * 2f));
        float viewportHeight = Mathf.Max(200f, viewportRect.rect.height - (settings.verticalPadding * 2f));

        viewportRect.offsetMin = new Vector2(settings.horizontalPadding, settings.verticalPadding);
        viewportRect.offsetMax = new Vector2(-settings.horizontalPadding, -settings.verticalPadding);

        RectTransform textRect = creditsText.rectTransform;
        textRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, viewportWidth);
        textRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Max(viewportHeight, creditsText.preferredHeight));
        scrollRootRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, viewportWidth);
        scrollRootRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, textRect.rect.height);
        yield return null;

        textRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Max(viewportHeight, creditsText.preferredHeight));
        scrollRootRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, textRect.rect.height);

        float fadeDuration = Mathf.Max(0.01f, settings.fadeDuration);
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);
            backgroundImage.color = new Color(settings.backgroundColor.r, settings.backgroundColor.g, settings.backgroundColor.b, t);
            creditsText.alpha = t;
            yield return null;
        }

        backgroundImage.color = settings.backgroundColor;
        creditsText.alpha = 1f;

        if (settings.startDelay > 0f)
            yield return new WaitForSecondsRealtime(settings.startDelay);

        float startY = -viewportHeight;
        float endY = scrollRootRect.rect.height + viewportHeight;
        Vector2 anchoredPosition = scrollRootRect.anchoredPosition;
        anchoredPosition.y = startY;
        scrollRootRect.anchoredPosition = anchoredPosition;

        while (scrollRootRect.anchoredPosition.y < endY)
        {
            anchoredPosition = scrollRootRect.anchoredPosition;
            anchoredPosition.y += settings.scrollSpeed * Time.unscaledDeltaTime;
            scrollRootRect.anchoredPosition = anchoredPosition;
            yield return null;
        }

        if (settings.endDelay > 0f)
            yield return new WaitForSecondsRealtime(settings.endDelay);

        elapsed = 0f;
        Color backgroundColor = settings.backgroundColor;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = 1f - Mathf.Clamp01(elapsed / fadeDuration);
            backgroundImage.color = new Color(backgroundColor.r, backgroundColor.g, backgroundColor.b, t);
            creditsText.alpha = t;
            yield return null;
        }

        gameObject.SetActive(false);
        playCoroutine = null;
    }

    private string BuildCreditsMarkup(DemoScenesGalleryManager.ContributorsRollSettings settings)
    {
        StringBuilder builder = new StringBuilder();

        if (!string.IsNullOrWhiteSpace(settings.rollTitle))
        {
            builder.Append("<size=");
            builder.Append(Mathf.RoundToInt(settings.titleFontSize));
            builder.Append("><b>");
            builder.Append(settings.rollTitle);
            builder.AppendLine("</b></size>");
            builder.AppendLine();
        }

        if (!string.IsNullOrWhiteSpace(settings.openingText))
        {
            builder.AppendLine(settings.openingText);
            builder.AppendLine();
            builder.AppendLine();
        }

        if (!string.IsNullOrWhiteSpace(settings.creditsText))
        {
            builder.AppendLine(settings.creditsText);
            builder.AppendLine();
            builder.AppendLine();
        }

        if (!string.IsNullOrWhiteSpace(settings.endingText))
            builder.AppendLine(settings.endingText);

        return builder.ToString();
    }
}
