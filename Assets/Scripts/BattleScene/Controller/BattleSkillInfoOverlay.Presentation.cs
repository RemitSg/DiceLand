using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public partial class BattleSkillInfoOverlay
{
    private enum SkillViewMode { Ally, EnemySkill, EnemyData }
    private SkillViewMode viewMode;
    private static readonly Color SkillGold = new Color32(226, 187, 101, 255);
    private static readonly Color SkillIvory = new Color32(242, 232, 205, 255);
    private static readonly Color SkillMuted = new Color32(173, 190, 163, 255);
    private RectTransform safeRoot, titlePlaque, listArea, listHeading, columnRule, detailRegion, metadataRoot, bodyArea, summaryRoot, footerRule;
    private TextMeshProUGUI contextText, skillNameText, metadataText, requirementsText, descriptionHeading, descriptionBody, footerHint;
    private TextMeshProUGUI hoverContextText, hoverHintText;
    private TextMeshProUGUI energyMetadata, diceMetadata;
    private RectTransform hoverRangeWell, hoverRule;
    private ScrollRect skillListScroll;
    private Scrollbar listScrollbar;
    private Button footerClose;
    private CanvasGroup panelGroup, contentGroup, hoverGroup;
    private Image shade;
    private SkillFrameState frameState;
    private EnemySkill_SO displayedSkill, hoverSkill;
    private BattleEnemyController currentHoverEnemy;
    private SpeedButton currentEnemySourceButton;
    private readonly List<SkillChoiceView> choiceViews = new List<SkillChoiceView>();
    private readonly List<DiceCardView> diceCards = new List<DiceCardView>();
    private readonly List<Button> diceLinks = new List<Button>();
    private readonly List<TextMeshProUGUI> diceLinkLabels = new List<TextMeshProUGUI>();
    private float bodyWidth, detailWidth, bodyTop, summaryHeight;
    private int selectedChoiceIndex = -1;

    private sealed class SkillChoiceView
    {
        public EnemySkill_SO skill;
        public Button button;
        public RectTransform rect;
        public TextMeshProUGUI name, cost, summary, reason, equipped;
        public BattleSkillFrameGraphic face, icon;
        public RectTransform check;
    }

    private sealed class DiceCardView
    {
        public RectTransform rect, rule;
        public TextMeshProUGUI heading, range, clauses;
        public BattleSkillFrameGraphic icon;
        public float top;
    }

    // Stored as a value so resize and visual capture can restore the same layout without touching a scene.
    private struct SkillFrameState { public float width, height, listWidth; }

    private void BuildForestUi()
    {
        GameObject go = new GameObject("BattleSkillInfoCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        go.transform.SetParent(transform, false);
        go.hideFlags = HideFlags.DontSave;
        canvas = go.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 9600;
        CanvasScaler scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = 0.5f;
        canvasRoot = (RectTransform)go.transform; StretchSkill(canvasRoot);
        RectTransform backdrop = SkillRect(canvasRoot, "Backdrop"); StretchSkill(backdrop);
        shade = backdrop.gameObject.AddComponent<Image>(); shade.color = new Color(0.004f, 0.012f, 0.008f, 0.6f);
        backdropButton = backdrop.gameObject.AddComponent<Button>(); backdropButton.transition = Selectable.Transition.None;
        backdropButton.onClick.AddListener(Hide);
        safeRoot = SkillRect(canvasRoot, "SkillSafeArea"); StretchSkill(safeRoot);
        panelRoot = SkillRect(safeRoot, "Panel");
        panelRoot.anchorMin = panelRoot.anchorMax = panelRoot.pivot = Vector2.one * 0.5f;
        Image hit = panelRoot.gameObject.AddComponent<Image>(); hit.color = Color.clear;
        panelGroup = panelRoot.gameObject.AddComponent<CanvasGroup>();
        RectTransform frame = SkillRect(panelRoot, "ForestFrame"); StretchSkill(frame);
        SkillFace(frame, MapCraftingFrameGraphic.Style.Window);
        titlePlaque = SkillRect(panelRoot, "TitlePlaque");
        var plaque = titlePlaque.gameObject.AddComponent<MapItemDetailFrameGraphic>();
        plaque.detailStyle = MapItemDetailFrameGraphic.DetailStyle.Title; plaque.raycastTarget = false;
        titleText = SkillText(titlePlaque, "Title", 42, SkillGold, GameText.Get("battle.skill.ally", "武器技能"));
        StretchSkill(titleText.rectTransform, 44, 2); titleText.alignment = TextAlignmentOptions.Midline;
        closeButton = SkillButton(panelRoot, "CloseButton", "×", Hide);
        subtitleText = SkillText(panelRoot, "Actor", 30, SkillIvory, string.Empty);
        contextText = SkillText(panelRoot, "Context", 25, SkillMuted, string.Empty); contextText.alignment = TextAlignmentOptions.MidlineRight;
        RectTransform headerRule = SkillRect(panelRoot, "HeaderRule"); AddSkillRule(headerRule);
        listHeading = SkillRect(panelRoot, "ListHeading");
        TextMeshProUGUI listTitle = SkillText(listHeading, "Label", 32, SkillGold, GameText.Get("battle.skill.available", "可用技能")); StretchSkill(listTitle.rectTransform);
        listArea = SkillRect(panelRoot, "SkillListArea");
        skillListScroll = BuildSkillScroll(listArea, "SkillList", out RectTransform listViewport, out skillListRoot, out listScrollbar);
        columnRule = SkillRect(panelRoot, "ColumnRule");
        Image column = columnRule.gameObject.AddComponent<Image>(); column.color = new Color32(163, 134, 70, 180); column.raycastTarget = false;
        detailRegion = SkillRect(panelRoot, "CurrentSkill");
        contentGroup = detailRegion.gameObject.AddComponent<CanvasGroup>();
        skillNameText = SkillText(detailRegion, "SkillName", 44, SkillIvory, string.Empty); skillNameText.enableWordWrapping = true;
        metadataRoot = SkillRect(detailRegion, "Metadata");
        var metadata = new TextMeshProUGUI[3];
        for (int i = 0; i < metadata.Length; i++)
        {
            RectTransform badge = SkillRect(metadataRoot, "Metadata_" + i); SkillFace(badge, MapCraftingFrameGraphic.Style.Badge);
            metadata[i] = SkillText(badge, "Label", 26, SkillIvory, string.Empty); StretchSkill(metadata[i].rectTransform, 12, 3);
            metadata[i].alignment = TextAlignmentOptions.Midline;
        }
        metadataText = metadata[0]; energyMetadata = metadata[1]; diceMetadata = metadata[2];
        requirementsText = SkillText(detailRegion, "Requirements", SkillRequirementsSize, SkillMuted, string.Empty); requirementsText.enableWordWrapping = true;
        summaryRoot = SkillRect(detailRegion, "DiceOverview");
        bodyArea = SkillRect(detailRegion, "DetailArea");
        detailScrollRect = BuildSkillScroll(bodyArea, "Detail", out detailViewport, out detailContent, out _);
        requirementsText.transform.SetParent(detailContent, false);
        summaryRoot.SetParent(detailContent, false);
        detailText = SkillText(detailContent, "EnemyData", 27, SkillIvory, string.Empty); detailText.enableWordWrapping = true;
        descriptionHeading = SkillText(detailContent, "DescriptionHeading", 26, SkillGold, GameText.Get("ui.description", "描述"));
        descriptionBody = SkillText(detailContent, "Description", SkillDescriptionSize, SkillIvory, string.Empty); descriptionBody.enableWordWrapping = true;
        footerRule = SkillRect(panelRoot, "FooterRule"); AddSkillRule(footerRule);
        footerHint = SkillText(panelRoot, "FooterHint", 23, SkillMuted, GameText.Get("battle.skill.select_hint", "点击技能即切换"));
        footerClose = SkillButton(panelRoot, "FooterCloseButton", GameText.Get("ui.close", "关闭"), Hide);
        footerClose.gameObject.AddComponent<BattleSkillOverlayCancel>().close = Hide;
        closeButton.gameObject.AddComponent<BattleSkillOverlayCancel>().close = Hide;
        BuildEnemyProfileUi();
        BuildHoverUi();
        ApplyFonts();
        backdrop.gameObject.SetActive(false); panelRoot.gameObject.SetActive(false); hoverPanelRoot.gameObject.SetActive(false);
        ApplyViewport(new Vector2(Screen.width, Screen.height), Screen.safeArea);
    }

    private void BuildHoverUi()
    {
        hoverPanelRoot = SkillRect(safeRoot, "EnemyHoverPanel");
        hoverPanelRoot.anchorMin = hoverPanelRoot.anchorMax = hoverPanelRoot.pivot = Vector2.one;
        hoverGroup = hoverPanelRoot.gameObject.AddComponent<CanvasGroup>(); hoverGroup.blocksRaycasts = false; hoverGroup.interactable = false;
        RectTransform surface = SkillRect(hoverPanelRoot, "Frame"); StretchSkill(surface);
        var hoverFace = surface.gameObject.AddComponent<BattleSkillFrameGraphic>(); hoverFace.skillStyle = BattleSkillFrameGraphic.SkillStyle.HoverPanel; hoverFace.raycastTarget = false;
        hoverTitleText = SkillText(hoverPanelRoot, "HoverTitle", enemyHoverTitleFontSize, SkillGold, string.Empty);
        hoverContextText = SkillText(hoverPanelRoot, "HoverContext", 22, SkillMuted, string.Empty);
        hoverRangeWell = SkillRect(hoverPanelRoot, "HoverRanges"); SkillFace(hoverRangeWell, MapCraftingFrameGraphic.Style.Material);
        hoverDetailText = SkillText(hoverRangeWell, "HoverDiceSummary", enemyHoverBodyFontSize, SkillIvory, string.Empty); hoverDetailText.enableWordWrapping = true;
        hoverRule = SkillRect(hoverPanelRoot, "HoverRule"); AddSkillRule(hoverRule);
        hoverClauseViewport = SkillRect(hoverPanelRoot, "HoverClauseViewport"); hoverClauseViewport.gameObject.AddComponent<RectMask2D>();
        hoverClauseContent = SkillRect(hoverClauseViewport, "Content");
        hoverClauseContent.anchorMin = new Vector2(0, 1); hoverClauseContent.anchorMax = Vector2.one; hoverClauseContent.pivot = new Vector2(0, 1);
        hoverClauseText = SkillText(hoverClauseContent, "Clauses", enemyHoverBodyFontSize, SkillIvory, string.Empty); hoverClauseText.enableWordWrapping = true;
        hoverClauseAutoScroller = hoverClauseViewport.gameObject.AddComponent<BattleSkillClauseAutoScroller>();
        hoverHintText = SkillText(hoverPanelRoot, "HoverHint", 20, SkillMuted, GameText.Get("battle.skill.rules.select_the_speed_slot_for_details", "点击速度按钮查看详情")); hoverHintText.alignment = TextAlignmentOptions.Midline;
    }

    private void LayoutForestPanel()
    {
        bool profile = viewMode == SkillViewMode.EnemyData;
        SetEnemyProfileVisible(profile);
        if (profile) { LayoutEnemyProfile(false); return; }
        bool ally = viewMode == SkillViewMode.Ally;
        frameState = new SkillFrameState { width = ally ? 1480 : 1240, height = 880, listWidth = 310 };
        float w = frameState.width, h = frameState.height;
        panelRoot.sizeDelta = new Vector2(w, h);
        PlaceSkill(titlePlaque, (w - 450) / 2, -22, 450, 76);
        PlaceSkill((RectTransform)closeButton.transform, w - 86, 20, 60, 60);
        PlaceSkill(subtitleText.rectTransform, 44, 72, ally ? 740 : w - 104, 46);
        PlaceSkill(contextText.rectTransform, w - 570, 80, 430, 34); contextText.gameObject.SetActive(viewMode != SkillViewMode.EnemyData);
        PlaceSkill((RectTransform)panelRoot.Find("HeaderRule"), 40, 130, w - 80, 5);
        listArea.gameObject.SetActive(ally); listHeading.gameObject.SetActive(ally); columnRule.gameObject.SetActive(ally);
        PlaceSkill(listHeading, 46, 152, frameState.listWidth, 42);
        PlaceSkill(listArea, 42, 200, frameState.listWidth, h - 300);
        PlaceSkill(columnRule, 375, 154, 2, h - 274);
        float x = ally ? 410 : 46;
        detailWidth = w - x - 46;
        PlaceSkill(detailRegion, x, 154, detailWidth, h - 270);
        PlaceSkill(footerRule, 40, h - 100, w - 80, 5);
        PlaceSkill(footerHint.rectTransform, 46, h - 77, ally ? 720 : 200, 40); footerHint.gameObject.SetActive(ally);
        PlaceSkill((RectTransform)footerClose.transform, ally ? w - 322 : (w - 270) / 2, h - 78, 270, 56);
        if (skillNameText != null) LayoutSkillContent(false);
    }

    private ScrollRect BuildSkillScroll(RectTransform parent, string name, out RectTransform viewport, out RectTransform content, out Scrollbar scrollbar)
    {
        ScrollRect scroll = parent.gameObject.AddComponent<ScrollRect>();
        scroll.horizontal = false; scroll.vertical = true; scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 38; scroll.inertia = true;
        viewport = SkillRect(parent, name + "Viewport"); StretchSkill(viewport); viewport.offsetMax = new Vector2(-15, 0);
        viewport.gameObject.AddComponent<RectMask2D>();
        Image hit = viewport.gameObject.AddComponent<Image>(); hit.color = Color.clear;
        content = SkillRect(viewport, name + "Content");
        content.anchorMin = new Vector2(0, 1); content.anchorMax = Vector2.one; content.pivot = new Vector2(0, 1);
        content.sizeDelta = Vector2.zero;
        RectTransform track = SkillRect(parent, name + "Scrollbar");
        track.anchorMin = new Vector2(1, 0); track.anchorMax = Vector2.one; track.pivot = new Vector2(1, 0.5f); track.sizeDelta = new Vector2(7, 0);
        Image trackFace = track.gameObject.AddComponent<Image>(); trackFace.color = new Color32(23, 37, 24, 230);
        scrollbar = track.gameObject.AddComponent<Scrollbar>(); scrollbar.direction = Scrollbar.Direction.BottomToTop;
        RectTransform slide = SkillRect(track, "SlidingArea"); StretchSkill(slide, 1, 1);
        RectTransform handle = SkillRect(slide, "Handle"); StretchSkill(handle);
        Image handleFace = handle.gameObject.AddComponent<Image>(); handleFace.color = new Color32(193, 157, 82, 255);
        scrollbar.handleRect = handle; scrollbar.targetGraphic = handleFace;
        scroll.viewport = viewport; scroll.content = content; scroll.verticalScrollbar = scrollbar;
        // The viewport keeps a stable width when this automatically hides; text does not reflow under the pointer.
        scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
        return scroll;
    }

    private TextMeshProUGUI SkillText(Transform parent, string name, int size, Color tint, string value)
    {
        var text = SkillRect(parent, name).gameObject.AddComponent<TextMeshProUGUI>();
        ConfigureText(text, size, tint, name == "Title", TextAlignmentOptions.MidlineLeft);
        text.text = value; text.overflowMode = TextOverflowModes.Overflow; return text;
    }

    private Button SkillButton(Transform parent, string name, string label, UnityEngine.Events.UnityAction handler)
    {
        RectTransform rect = SkillRect(parent, name);
        var face = SkillFace(rect, MapCraftingFrameGraphic.Style.Secondary); face.raycastTarget = true;
        Button button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = face; button.transition = Selectable.Transition.None;
        Navigation nav = button.navigation; nav.mode = Navigation.Mode.Automatic; button.navigation = nav;
        if (handler != null) button.onClick.AddListener(handler);
        rect.gameObject.AddComponent<MapCraftingButtonFeedback>();
        TextMeshProUGUI text = SkillText(rect, "Label", label == "×" ? 38 : 30, new Color32(48, 33, 12, 255), label);
        StretchSkill(text.rectTransform, 10, 1); text.alignment = TextAlignmentOptions.Midline;
        return button;
    }

    private static RectTransform SkillRect(Transform parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false); return (RectTransform)go.transform;
    }
    private static void PlaceSkill(RectTransform rect, float x, float y, float width, float height)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(x, -y); rect.sizeDelta = new Vector2(Mathf.Max(1, width), Mathf.Max(1, height));
    }
    private static void StretchSkill(RectTransform rect, float x = 0, float y = 0)
    {
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.pivot = Vector2.one * 0.5f;
        rect.offsetMin = new Vector2(x, y); rect.offsetMax = new Vector2(-x, -y);
    }
    private static MapCraftingFrameGraphic SkillFace(RectTransform rect, MapCraftingFrameGraphic.Style style)
    {
        var face = rect.gameObject.AddComponent<MapCraftingFrameGraphic>(); face.craftingStyle = style; face.raycastTarget = false; return face;
    }
    private static void AddSkillRule(RectTransform rect)
    {
        var rule = rect.gameObject.AddComponent<MapMerchantFrameGraphic>(); rule.style = MapMerchantFrameGraphic.Style.Divider; rule.raycastTarget = false;
    }
    private static float SkillTextHeight(TMP_Text text, float width, float minimum = 0)
    {
        return Mathf.Max(minimum, text.GetPreferredValues(text.text, Mathf.Max(40, width), float.PositiveInfinity).y + 6);
    }
}
