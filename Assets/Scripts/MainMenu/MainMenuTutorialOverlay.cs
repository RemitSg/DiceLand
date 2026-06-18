using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class MainMenuTutorialOverlay : MonoBehaviour
{
    private const string FallbackFontName = "LegacyRuntime.ttf";
    private const string DefaultTitle = "教程";
    private const string DefaultBody = "暂无教程说明。";
    private const string DefaultStartLabel = "开始";
    private const string DefaultBackLabel = "返回";

    [Header("Fonts")]
    [SerializeField] private Font summaryBodyFont;

    private Canvas canvas;
    private RectTransform panelRect;
    private Text summaryTitleText;
    private Text summaryBodyText;
    private Text startButtonText;
    private Text backButtonText;
    private Button startButton;
    private Button backButton;
    private ScrollRect sectionScrollRect;
    private RectTransform sectionContainer;
    private readonly List<GameObject> sectionViews = new List<GameObject>();

    public bool IsVisible => canvas != null && canvas.gameObject.activeSelf;
    public Text DebugSummaryTitleText => summaryTitleText;
    public Text DebugSummaryBodyText => summaryBodyText;
    public Text DebugStartButtonText => startButtonText;
    public Text DebugBackButtonText => backButtonText;
    public int DebugSectionCount => sectionViews.Count;

    public void ConfigureFonts(Font summaryBodyFontOverride)
    {
        if (summaryBodyFontOverride != null)
            summaryBodyFont = summaryBodyFontOverride;

        ApplyConfiguredFonts();
    }

    public void Show(
        TutorialMenuCatalog_SO catalog,
        Action onStart,
        Action onBack,
        string startButtonLabelOverride = null,
        string backButtonLabelOverride = null,
        string titleOverride = null,
        string bodyOverride = null,
        bool hideSections = false)
    {
        EnsureUi();

        TutorialMenuCatalog_SO safeCatalog = catalog;
        summaryTitleText.text = !string.IsNullOrEmpty(titleOverride)
            ? titleOverride
            : (!string.IsNullOrEmpty(safeCatalog != null ? safeCatalog.summaryTitle : null) ? safeCatalog.summaryTitle : DefaultTitle);
        summaryBodyText.text = !string.IsNullOrEmpty(bodyOverride)
            ? bodyOverride
            : (!string.IsNullOrEmpty(safeCatalog != null ? safeCatalog.summaryBody : null) ? safeCatalog.summaryBody : DefaultBody);
        startButtonText.text = !string.IsNullOrEmpty(startButtonLabelOverride)
            ? startButtonLabelOverride
            : (!string.IsNullOrEmpty(safeCatalog != null ? safeCatalog.startButtonLabel : null) ? safeCatalog.startButtonLabel : DefaultStartLabel);
        backButtonText.text = !string.IsNullOrEmpty(backButtonLabelOverride)
            ? backButtonLabelOverride
            : (!string.IsNullOrEmpty(safeCatalog != null ? safeCatalog.backButtonLabel : null) ? safeCatalog.backButtonLabel : DefaultBackLabel);

        startButton.onClick.RemoveAllListeners();
        backButton.onClick.RemoveAllListeners();
        startButton.onClick.AddListener(() => onStart?.Invoke());
        backButton.onClick.AddListener(() =>
        {
            Hide();
            onBack?.Invoke();
        });

        RebuildSections(safeCatalog);
        ApplyLayout(hideSections);
        canvas.gameObject.SetActive(true);
    }

    public void Hide()
    {
        if (canvas != null)
            canvas.gameObject.SetActive(false);
    }

    private void EnsureUi()
    {
        if (canvas != null)
            return;

        Font font = Resources.GetBuiltinResource<Font>(FallbackFontName);
        Font resolvedSummaryBodyFont = ResolveSummaryBodyFont(font);

        GameObject canvasGo = new GameObject("MainMenuTutorialOverlay", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9550;

        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform rootRect = canvasGo.GetComponent<RectTransform>();
        StretchRect(rootRect);

        panelRect = CreateRect("Panel", rootRect, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1120f, 760f));
        Image panelImage = panelRect.gameObject.AddComponent<Image>();
        panelImage.color = new Color(0.07f, 0.09f, 0.13f, 0.96f);

        summaryTitleText = CreateText("SummaryTitle", panelRect, new Vector2(0.5f, 1f), new Vector2(0f, -56f), new Vector2(860f, 72f), font, 42, TextAnchor.MiddleCenter, Color.white);
        summaryBodyText = CreateText("SummaryBody", panelRect, new Vector2(0.5f, 1f), new Vector2(0f, -124f), new Vector2(860f, 120f), resolvedSummaryBodyFont, 24, TextAnchor.UpperCenter, new Color(0.85f, 0.88f, 0.92f, 1f));
        summaryBodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
        summaryBodyText.verticalOverflow = VerticalWrapMode.Overflow;

        RectTransform sectionsRect = CreateRect("Sections", panelRect, new Vector2(0.5f, 1f), new Vector2(0f, -270f), new Vector2(920f, 360f));
        sectionScrollRect = sectionsRect.gameObject.AddComponent<ScrollRect>();
        sectionScrollRect.horizontal = false;
        sectionScrollRect.vertical = true;
        sectionScrollRect.movementType = ScrollRect.MovementType.Clamped;
        sectionScrollRect.scrollSensitivity = 24f;

        RectTransform viewportRect = CreateStretchRect("Viewport", sectionsRect);
        Image viewportImage = viewportRect.gameObject.AddComponent<Image>();
        viewportImage.color = new Color(1f, 1f, 1f, 0.02f);
        Mask viewportMask = viewportRect.gameObject.AddComponent<Mask>();
        viewportMask.showMaskGraphic = false;

        sectionContainer = CreateStretchRect("Content", viewportRect);
        sectionContainer.pivot = new Vector2(0.5f, 1f);
        sectionContainer.anchorMin = new Vector2(0f, 1f);
        sectionContainer.anchorMax = new Vector2(1f, 1f);
        sectionContainer.anchoredPosition = Vector2.zero;
        sectionContainer.sizeDelta = Vector2.zero;

        sectionScrollRect.viewport = viewportRect;
        sectionScrollRect.content = sectionContainer;

        VerticalLayoutGroup sectionLayout = sectionContainer.gameObject.AddComponent<VerticalLayoutGroup>();
        sectionLayout.childAlignment = TextAnchor.UpperCenter;
        sectionLayout.childControlWidth = true;
        sectionLayout.childControlHeight = true;
        sectionLayout.childForceExpandWidth = true;
        sectionLayout.childForceExpandHeight = false;
        sectionLayout.spacing = 12f;

        ContentSizeFitter sectionSizeFitter = sectionContainer.gameObject.AddComponent<ContentSizeFitter>();
        sectionSizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        sectionSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        startButton = CreateButton("StartButton", panelRect, new Vector2(0.5f, 0f), new Vector2(-126f, 48f), new Vector2(240f, 64f), font, out startButtonText);
        startButtonText.text = "Start";

        backButton = CreateButton("BackButton", panelRect, new Vector2(0.5f, 0f), new Vector2(126f, 48f), new Vector2(240f, 64f), font, out backButtonText);
        backButtonText.text = "Back";

        canvas.gameObject.SetActive(false);
    }

    private void ApplyConfiguredFonts()
    {
        if (summaryBodyText == null)
            return;

        Font fallbackFont = Resources.GetBuiltinResource<Font>(FallbackFontName);
        summaryBodyText.font = ResolveSummaryBodyFont(fallbackFont);
    }

    private Font ResolveSummaryBodyFont(Font fallbackFont)
    {
        return summaryBodyFont != null ? summaryBodyFont : fallbackFont;
    }

    private void ApplyLayout(bool hideSections)
    {
        if (panelRect == null || summaryTitleText == null || summaryBodyText == null || startButton == null || backButton == null)
            return;

        if (hideSections)
        {
            panelRect.sizeDelta = new Vector2(920f, 420f);
            SetRect(summaryTitleText.rectTransform, new Vector2(0f, -60f), new Vector2(780f, 72f));
            SetRect(summaryBodyText.rectTransform, new Vector2(0f, -148f), new Vector2(760f, 150f));
            SetRect(startButton.GetComponent<RectTransform>(), new Vector2(-126f, 58f), new Vector2(240f, 64f));
            SetRect(backButton.GetComponent<RectTransform>(), new Vector2(126f, 58f), new Vector2(240f, 64f));
        }
        else
        {
            panelRect.sizeDelta = new Vector2(1120f, 760f);
            SetRect(summaryTitleText.rectTransform, new Vector2(0f, -56f), new Vector2(860f, 72f));
            SetRect(summaryBodyText.rectTransform, new Vector2(0f, -124f), new Vector2(860f, 120f));
            SetRect(startButton.GetComponent<RectTransform>(), new Vector2(-126f, 48f), new Vector2(240f, 64f));
            SetRect(backButton.GetComponent<RectTransform>(), new Vector2(126f, 48f), new Vector2(240f, 64f));
        }

        if (sectionScrollRect != null)
            sectionScrollRect.gameObject.SetActive(!hideSections);
    }

    private void RebuildSections(TutorialMenuCatalog_SO catalog)
    {
        for (int i = 0; i < sectionViews.Count; i++)
        {
            if (sectionViews[i] != null)
                DestroyImmediate(sectionViews[i]);
        }

        sectionViews.Clear();

        IReadOnlyList<TutorialMenuSection> sections = catalog != null ? catalog.GetOrderedSections() : Array.Empty<TutorialMenuSection>();
        Font font = Resources.GetBuiltinResource<Font>(FallbackFontName);

        for (int i = 0; i < sections.Count; i++)
        {
            TutorialMenuSection section = sections[i];
            RectTransform itemRect = CreateRect(
                $"Section_{i}",
                sectionContainer,
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(920f, 0f));

            Image itemBackground = itemRect.gameObject.AddComponent<Image>();
            itemBackground.color = new Color(0.13f, 0.16f, 0.21f, 0.92f);

            LayoutElement itemLayout = itemRect.gameObject.AddComponent<LayoutElement>();
            itemLayout.preferredHeight = 108f;

            VerticalLayoutGroup itemVerticalLayout = itemRect.gameObject.AddComponent<VerticalLayoutGroup>();
            itemVerticalLayout.padding = new RectOffset(24, 24, 18, 18);
            itemVerticalLayout.spacing = 8f;
            itemVerticalLayout.childAlignment = TextAnchor.UpperLeft;
            itemVerticalLayout.childControlWidth = true;
            itemVerticalLayout.childControlHeight = true;
            itemVerticalLayout.childForceExpandWidth = true;
            itemVerticalLayout.childForceExpandHeight = false;

            ContentSizeFitter itemSizeFitter = itemRect.gameObject.AddComponent<ContentSizeFitter>();
            itemSizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            itemSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            Text titleText = CreateText("Title", itemRect, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(0f, 34f), font, 28, TextAnchor.MiddleLeft, Color.white);
            titleText.text = section != null ? section.title ?? string.Empty : string.Empty;

            Text bodyText = CreateText("Summary", itemRect, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(0f, 0f), font, 20, TextAnchor.UpperLeft, new Color(0.78f, 0.82f, 0.88f, 1f));
            bodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
            bodyText.verticalOverflow = VerticalWrapMode.Overflow;
            bodyText.text = section != null ? section.summary ?? string.Empty : string.Empty;

            sectionViews.Add(itemRect.gameObject);
        }
    }

    private static void StretchRect(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static RectTransform CreateRect(string name, RectTransform parent, Vector2 anchorPivot, Vector2 anchoredPosition, Vector2 size)
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

    private static RectTransform CreateStretchRect(string name, RectTransform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        StretchRect(rect);
        return rect;
    }

    private static void SetRect(RectTransform rect, Vector2 anchoredPosition, Vector2 size)
    {
        if (rect == null)
            return;

        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
    }

    private static Text CreateText(string name, RectTransform parent, Vector2 anchorPivot, Vector2 anchoredPosition, Vector2 size, Font font, int fontSize, TextAnchor alignment, Color color)
    {
        RectTransform rect = CreateRect(name, parent, anchorPivot, anchoredPosition, size);
        Text text = rect.gameObject.AddComponent<Text>();
        text.font = font;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = color;
        return text;
    }

    private static Button CreateButton(string name, RectTransform parent, Vector2 anchorPivot, Vector2 anchoredPosition, Vector2 size, Font font, out Text labelText)
    {
        RectTransform rect = CreateRect(name, parent, anchorPivot, anchoredPosition, size);
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = new Color(0.28f, 0.78f, 1f, 1f);

        Button button = rect.gameObject.AddComponent<Button>();
        labelText = CreateText("Label", rect, new Vector2(0.5f, 0.5f), Vector2.zero, size, font, 22, TextAnchor.MiddleCenter, Color.black);
        return button;
    }
}
