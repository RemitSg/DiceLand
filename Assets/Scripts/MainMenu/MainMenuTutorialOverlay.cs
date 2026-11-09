using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
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

    private Font headingFont, buttonFont;
    private TutorialForestView view;
    private Canvas canvas;
    private RectTransform panelRect;
    private Text summaryTitleText;
    private Text summaryBodyText;
    private Text startButtonText;
    private Text backButtonText;
    private Button startButton;
    private Button backButton;
    private Action backHandler;
    private bool startRequested;
    private ScrollRect sectionScrollRect;
    private RectTransform sectionContainer;
    private readonly List<GameObject> sectionViews = new List<GameObject>();

    public bool IsVisible => canvas != null && canvas.isActiveAndEnabled;
    public Canvas PresentationCanvas => canvas;
    public TutorialForestView Presentation => view;
    public Text DebugSummaryTitleText => summaryTitleText;
    public Text DebugSummaryBodyText => summaryBodyText;
    public Text DebugStartButtonText => startButtonText;
    public Text DebugBackButtonText => backButtonText;
    public int DebugSectionCount => sectionViews.Count;

    public void ConfigureFonts(Font summaryBodyFontOverride, Font heading = null, Font buttons = null)
    {
        if (summaryBodyFontOverride != null)
            summaryBodyFont = summaryBodyFontOverride;

        if (heading != null) headingFont = heading;
        if (buttons != null) buttonFont = buttons;
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
        backHandler = onBack;
        startRequested = false;
        startButton.interactable = true;

        TutorialMenuCatalog_SO safeCatalog = catalog;
        summaryTitleText.text = !string.IsNullOrEmpty(titleOverride)
            ? titleOverride
            : (!string.IsNullOrEmpty(safeCatalog != null ? safeCatalog.summaryTitle : null) ? GameText.Get("tutorial.catalog.summaryTitle", safeCatalog.summaryTitle) : GameText.Get("ui.menu.tutorial", DefaultTitle));
        summaryBodyText.text = bodyOverride != null
            ? bodyOverride
            : (!string.IsNullOrEmpty(safeCatalog != null ? safeCatalog.summaryBody : null) ? GameText.Get("tutorial.catalog.summaryBody", safeCatalog.summaryBody) : GameText.Get("tutorial.catalog.empty", DefaultBody));
        startButtonText.text = !string.IsNullOrEmpty(startButtonLabelOverride)
            ? startButtonLabelOverride
            : (!string.IsNullOrEmpty(safeCatalog != null ? safeCatalog.startButtonLabel : null) ? GameText.Get("tutorial.catalog.startButtonLabel", safeCatalog.startButtonLabel) : GameText.Get("tutorial.menu.start", DefaultStartLabel));
        backButtonText.text = !string.IsNullOrEmpty(backButtonLabelOverride)
            ? backButtonLabelOverride
            : (!string.IsNullOrEmpty(safeCatalog != null ? safeCatalog.backButtonLabel : null) ? GameText.Get("tutorial.catalog.backButtonLabel", safeCatalog.backButtonLabel) : GameText.Get("ui.back", DefaultBackLabel));

        startButton.onClick.RemoveAllListeners();
        backButton.onClick.RemoveAllListeners();
        startButton.onClick.AddListener(() =>
        {
            if (startRequested || !IsVisible || !startButton.IsInteractable()) return;
            startRequested = true;
            startButton.interactable = false;
            if (onStart != null) view.Close(() => { HideImmediately(false); onStart(); });
        });
        backButton.onClick.AddListener(Hide);

        RebuildSections(safeCatalog);
        view.CatalogVisible = !hideSections;
        ApplyConfiguredFonts();
        view.Show();
        view.Reading.StopMovement(); view.Reading.verticalNormalizedPosition = 1;
        sectionScrollRect.StopMovement(); sectionScrollRect.verticalNormalizedPosition = 1;
    }

    public void Hide()
    {
        if (view != null) view.Close(() => HideImmediately(true));
        else HideImmediately(true);
    }

    private void HideImmediately(bool invokeBack)
    {
        view?.HideImmediately();
        Action callback = invokeBack ? backHandler : null;
        backHandler = null;
        callback?.Invoke();
    }

    private void LateUpdate() { AdvancePresentation(Time.unscaledDeltaTime); }
    public void AdvancePresentation(float delta) { view?.Advance(delta); }
    private void OnDisable() { HideImmediately(false); }
    private void OnDestroy() { view?.Dispose(); }

    private void EnsureUi()
    {
        if (canvas != null) return;
        // The menu controller lives under the hidden legacy gallery canvas.
        view = new TutorialForestView("MainMenuTutorialOverlay", null, 9550, false, true);
        canvas = view.Canvas; panelRect = view.Panel;
        SceneManager.MoveGameObjectToScene(canvas.gameObject, gameObject.scene);
        summaryTitleText = (Text)view.Title; summaryBodyText = (Text)view.Body;
        startButton = view.Primary; backButton = view.Secondary;
        startButtonText = (Text)view.PrimaryLabel; backButtonText = (Text)view.SecondaryLabel;
        startButton.gameObject.AddComponent<BattleSkillOverlayCancel>().close = Hide;
        backButton.gameObject.AddComponent<BattleSkillOverlayCancel>().close = Hide;

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

        view.Catalog = sectionsRect;
        ApplyConfiguredFonts();
    }

    private void ApplyConfiguredFonts()
    {
        view?.SetLegacyFonts(headingFont, summaryBodyFont, buttonFont);
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

            var itemBackground = itemRect.gameObject.AddComponent<MapCraftingFrameGraphic>();
            itemBackground.craftingStyle = MapCraftingFrameGraphic.Style.Material;
            itemBackground.raycastTarget = false;

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

            Text titleText = CreateText("Title", itemRect, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(0f, 34f), font, 29, TextAnchor.MiddleLeft, TutorialForestView.Gold);
            titleText.text = section != null ? GameText.Content("tutorial.catalog.section." + section.sectionId, "title", section.title) ?? string.Empty : string.Empty;

            Text bodyText = CreateText("Summary", itemRect, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(0f, 0f), font, 26, TextAnchor.UpperLeft, TutorialForestView.Ivory);
            bodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
            bodyText.verticalOverflow = VerticalWrapMode.Overflow;
            bodyText.text = section != null ? GameText.Content("tutorial.catalog.section." + section.sectionId, "summary", section.summary) ?? string.Empty : string.Empty;

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

    private static Text CreateText(string name, RectTransform parent, Vector2 anchorPivot, Vector2 anchoredPosition, Vector2 size, Font font, int fontSize, TextAnchor alignment, Color color)
    {
        RectTransform rect = CreateRect(name, parent, anchorPivot, anchoredPosition, size);
        Text text = rect.gameObject.AddComponent<ResourceKeywordLegacyText>();
        text.font = font;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = color;
        text.raycastTarget = false;
        text.lineSpacing = 1.15f;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }
}
