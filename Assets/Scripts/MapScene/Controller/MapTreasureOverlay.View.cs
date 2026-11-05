using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public partial class MapTreasureOverlay
{
    private const int RelicDescriptionSize = 23;

    private static readonly Color ForestGold = new Color32(250, 214, 135, 255);
    private static readonly Color ParchmentInk = new Color32(54, 34, 15, 255);
    private MapTreasureArt art;
    private RectTransform safeRoot, cardContent, headerRect;
    private Image panelFrame, backdrop;
    private Image resultIcon;
    private CanvasGroup windowGroup;
    private ScrollRect cardScroll;
    private TextMeshProUGUI footerText;
    private Vector2 lastViewport, restingPosition;
    private Rect lastSafeArea;
    private float layoutScale = 1f, openProgress = 1f, successTime;
    private bool closing;
    private readonly List<RelicCardView> relicViews = new List<RelicCardView>();
    private readonly List<RectTransform> rewardSparks = new List<RectTransform>();

    private sealed class RelicCardView
    {
        public BattleSettlementLootEntry entry;
        public MapTreasureCardMotion motion;
        public TextMeshProUGUI label;
        public TextMeshProUGUI description;
        public ScrollRect descriptionScroll;
    }

    public void ConfigureForestPresentation()
    {
        art = MapTreasureArt.Load();
        if (art == null) return;
        panelBackgroundSprite = art.panelFrame;
        optionBackgroundSprite = disabledOptionBackgroundSprite = art.card;
        moneyIcon = art.coins;
        if (art.relicBags != null && art.relicBags.Length >= 5)
        {
            relicBagCommonIcon = art.relicBags[0]; relicBagUncommonIcon = art.relicBags[1];
            relicBagRareIcon = art.relicBags[2]; relicBagEpicIcon = art.relicBags[3];
            relicBagLegendaryIcon = art.relicBags[4]; relicBagIcon = art.relicBags[0];
        }
        if (art.headingFont != null) keywordFont = art.headingFont;
        if (art.bodyFont != null) bodyFont = art.bodyFont;
        ApplyPanelBackground();
        ApplyFonts();
    }

    // Battle settlement confirms the selection here, then grants it through its existing return flow.
    public void ShowRelicSelection(BattleSettlementRelicChoiceData data, DirectRelicChoiceConfirmHandler onConfirm)
    {
        if (data == null || !data.HasChoices || data.rewardApplied) return;
        BattleSettlementLootEntry previous = data.selectedChoice;
        ShowRelicChoice(data, onConfirm);
        closeAfterRelicConfirm = requireDeselectForRefresh = true;
        if (previous != null && data.choices.Contains(previous)) data.selectedChoice = previous;
        subtitleText.text = !string.IsNullOrWhiteSpace(data.requiredMessage) ? data.requiredMessage : GameText.Get("ui.map_treasure.choose_a_relic_and_confirm_to_return_to", "请选择一个遗物，确认后返回战利品。");
        RefreshRelicSelection();
        UpdateRelicRefreshButton();
    }

    private void EnsureForestUi()
    {
        art = MapTreasureArt.Load();
        if (art != null)
        {
            if (panelBackgroundSprite == null) panelBackgroundSprite = art.panelFrame;
            if (optionBackgroundSprite == null) optionBackgroundSprite = art.card;
            if (moneyIcon == null) moneyIcon = art.coins;
            if (keywordFont == null) keywordFont = art.headingFont;
            if (bodyFont == null) bodyFont = art.bodyFont;
        }
        panelSize = new Vector2(Mathf.Max(1480, panelSize.x), Mathf.Max(830, panelSize.y));
        cardSize = new Vector2(396, 566); cardSpacing = 32;
        buttonSize = new Vector2(290, 66);
        GameObject canvasGo = new GameObject("TreasureOverlayCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(Image));
        canvasGo.transform.SetParent(transform, false);
        canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 8020;
        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = 1;
        root = (RectTransform)canvasGo.transform;
        Stretch(root);
        backdrop = canvasGo.GetComponent<Image>(); backdrop.color = new Color(0.003f, 0.011f, 0.008f, 0.72f);
        safeRoot = CreateRect("SafeArea", root, Vector2.one * 0.5f, Vector2.zero, Vector2.zero); Stretch(safeRoot);
        panel = CreateRect("Panel", safeRoot, Vector2.one * 0.5f, Vector2.zero, panelSize);
        windowGroup = panel.gameObject.AddComponent<CanvasGroup>();
        panelImage = panel.gameObject.AddComponent<Image>();
        // Preserve the configurable panel sprite; the separate surface keeps the frame's transparent center opaque.
        RectTransform surface = Box(panel, "ForestSurface", 5, 5, panelSize.x - 10, panelSize.y - 10);
        MapMerchantFrameGraphic grain = surface.gameObject.AddComponent<MapMerchantFrameGraphic>();
        grain.style = MapMerchantFrameGraphic.Style.Panel; grain.raycastTarget = false;
        panelFrame = Box(panel, "GildedFrame", 0, 0, panelSize.x, panelSize.y).gameObject.AddComponent<Image>();
        panelFrame.raycastTarget = false;
        ApplyForestPanelBackground();
        headerRect = Box(panel, "TreasureHeader", (panelSize.x - 900) * 0.5f, -145, 900, 321);
        Image crest = headerRect.gameObject.AddComponent<Image>();
        crest.sprite = art != null ? art.header : MapForestArt.Get("Treasure"); crest.preserveAspect = true; crest.raycastTarget = false;
        titleText = ForestText(panel, "Title", (panelSize.x - 560) * 0.5f, 24, 560, 91, 72, ForestGold, string.Empty, true);
        titleText.alignment = TextAlignmentOptions.Midline;
        titleText.enableAutoSizing = true; titleText.fontSizeMin = 36; titleText.fontSizeMax = 72;
        subtitleText = ForestText(panel, "Subtitle", 100, 139, panelSize.x - 200, 42, 30, ForestGold, string.Empty);
        subtitleText.alignment = TextAlignmentOptions.Center; subtitleText.enableWordWrapping = true;
        cardRoot = Box(panel, "CardRoot", 60, 175, panelSize.x - 120, 592);
        Image viewportImage = cardRoot.gameObject.AddComponent<Image>(); viewportImage.color = Color.clear;
        cardRoot.gameObject.AddComponent<RectMask2D>();
        cardScroll = cardRoot.gameObject.AddComponent<ScrollRect>();
        cardScroll.viewport = cardRoot; cardScroll.horizontal = true; cardScroll.vertical = false;
        cardScroll.movementType = ScrollRect.MovementType.Clamped; cardScroll.scrollSensitivity = 38;
        cardContent = Box(cardRoot, "CardContent", 0, 0, cardRoot.sizeDelta.x, cardRoot.sizeDelta.y);
        cardScroll.content = cardContent;
        footerText = ForestText(panel, "ChoiceRule", 180, panelSize.y - 61, panelSize.x - 360, 40, 28, ForestGold, GameText.Get("ui.map_treasure.claim_one_reward", "仅可领取一项奖励"));
        footerText.alignment = TextAlignmentOptions.Center;
        resultIcon = Box(panel, "ResultRewardIcon", (panelSize.x - 220) * 0.5f, 230, 220, 220).gameObject.AddComponent<Image>();
        resultIcon.preserveAspect = true; resultIcon.raycastTarget = false; MapItemIconFit.Attach(resultIcon);
        resultIcon.gameObject.SetActive(false);
        resultText = ForestText(panel, "Result", 180, 480, panelSize.x - 360, 176, 36, new Color32(246, 231, 193, 255), string.Empty);
        resultText.alignment = TextAlignmentOptions.Center; resultText.enableWordWrapping = true;
        resultText.gameObject.SetActive(false);
        continueButton = BuildButton(panel, "ContinueButton", new Vector2(0, 48), GameText.Get("ui.continue", continueLabel), Hide);
        confirmRelicChoiceButton = BuildButton(panel, "ConfirmRelicChoiceButton", new Vector2(0, 48), GameText.Get("ui.confirm", "确认"), ConfirmRelicChoice);
        refreshRelicChoiceButton = BuildButton(panel, "RefreshRelicChoiceButton", new Vector2(buttonSize.x * 0.5f + 18, 48), GameText.Get("ui.refresh", "刷新"), RefreshRelicChoice);
        foreach (Button button in new[] { continueButton, confirmRelicChoiceButton, refreshRelicChoiceButton }) button.gameObject.SetActive(false);
        for (int i = 0; i < 14; i++)
        {
            RectTransform spark = Box(panel, "RewardSpark_" + i, 0, 0, 4, 4);
            spark.pivot = Vector2.one * 0.5f;
            Image image = spark.gameObject.AddComponent<Image>(); image.color = Color.clear; image.raycastTarget = false;
            rewardSparks.Add(spark);
        }
        ApplyFonts();
        ApplyViewport(new Vector2(Screen.width, Screen.height), Screen.safeArea);
        canvas.gameObject.SetActive(false);
    }

    private void ApplyForestPanelBackground()
    {
        if (panelImage == null) return;
        panelImage.sprite = panelBackgroundSprite;
        panelImage.color = Color.clear;
        if (panelFrame != null)
        {
            panelFrame.sprite = panelBackgroundSprite;
            panelFrame.type = panelBackgroundSprite != null ? Image.Type.Sliced : Image.Type.Simple;
            panelFrame.pixelsPerUnitMultiplier = 2;
            panelFrame.color = panelBackgroundSprite != null ? Color.white : Color.clear;
        }
    }

    private float PrepareCards(int count)
    {
        ClearCards();
        float total = count * cardSize.x + Mathf.Max(0, count - 1) * cardSpacing;
        cardContent.sizeDelta = new Vector2(Mathf.Max(cardRoot.sizeDelta.x, total + 32), cardRoot.sizeDelta.y);
        cardContent.anchoredPosition = Vector2.zero;
        cardScroll.horizontal = total + 32 > cardRoot.sizeDelta.x;
        cardScroll.velocity = Vector2.zero;
        return (cardContent.sizeDelta.x - total) * 0.5f;
    }

    private RectTransform BuildCard(string name, int index, float start, bool available)
    {
        RectTransform rect = Box(cardContent, name, start + index * (cardSize.x + cardSpacing), 8, cardSize.x, cardSize.y);
        Image background = rect.gameObject.AddComponent<Image>();
        background.sprite = available ? optionBackgroundSprite : disabledOptionBackgroundSprite != null ? disabledOptionBackgroundSprite : optionBackgroundSprite;
        background.color = available ? Color.white : new Color(0.57f, 0.59f, 0.56f);
        background.type = Image.Type.Simple;
        rect.gameObject.AddComponent<CanvasGroup>();
        rect.gameObject.AddComponent<MapTreasureCardMotion>().Configure(index, available);
        spawnedCards.Add(rect.gameObject);
        return rect;
    }

    private Image CardIcon(RectTransform card, Sprite sprite, bool available)
    {
        Image icon = Box(card, "RewardIcon", 56, 58, cardSize.x - 112, 205).gameObject.AddComponent<Image>();
        icon.sprite = sprite; icon.preserveAspect = true; icon.raycastTarget = false;
        icon.color = available ? Color.white : Color.gray;
        icon.enabled = sprite != null;
        MapItemIconFit.Attach(icon);
        return icon;
    }

    private void BuildRewardCards(IReadOnlyList<TreasureRewardChoice> choices)
    {
        int count = 0;
        if (choices != null) foreach (TreasureRewardChoice choice in choices) if (choice != null) count++;
        float start = PrepareCards(count); int visualIndex = 0;
        if (choices == null) return;
        for (int i = 0; i < choices.Count; i++)
        {
            TreasureRewardChoice choice = choices[i]; if (choice == null) continue;
            RectTransform card = BuildCard("TreasureChoice_" + i, visualIndex++, start, choice.isAvailable);
            CardIcon(card, ResolveRewardIcon(choice), choice.isAvailable);
            TextMeshProUGUI category = ForestText(card, "Category", 100, 270, cardSize.x - 200, 43, 27, CategoryColor(choice), Category(choice), true);
            category.alignment = TextAlignmentOptions.Midline; category.enableAutoSizing = true; category.fontSizeMin = 18; category.fontSizeMax = 27;
            TextMeshProUGUI name = ForestText(card, "ChoiceTitle", 40, 334, cardSize.x - 80, 80, 38, ParchmentInk, RewardName(choice), true);
            name.alignment = TextAlignmentOptions.Midline; name.enableWordWrapping = true; name.enableAutoSizing = true; name.fontSizeMin = 24; name.fontSizeMax = 42;
            string detail = !choice.isAvailable ? choice.unavailableReason : choice.rewardType == TreasureRewardType.Wealth ? string.Empty : GameText.Get("ui.map_treasure.quantity", "数量 ×1");
            if (choice.specialDungeonOptionIndex >= 0 && !string.IsNullOrWhiteSpace(choice.description)) detail = BuildChoiceDetail(choice);
            TextMeshProUGUI quantity = ForestText(card, "ChoiceDetail", 44, 416, cardSize.x - 88, 62, 25, ParchmentInk, detail);
            quantity.alignment = TextAlignmentOptions.Center; quantity.enableWordWrapping = true;
            quantity.enableAutoSizing = true; quantity.fontSizeMin = 18; quantity.fontSizeMax = 25;
            Button claim = BuildButton(card, "ChooseButton", new Vector2(0, 53), GameText.Get("ui.claim", chooseLabel), () =>
            {
                using (MapFeedbackOrigin.FromText(quantity, card)) OnChoiceClicked(choice);
            });
            claim.interactable = choice.isAvailable;
        }
        footerText.gameObject.SetActive(true);
        resultIcon.gameObject.SetActive(false);
    }

    private void BuildRelicCards(BattleSettlementRelicChoiceData data)
    {
        int count = 0;
        if (data?.choices != null) foreach (BattleSettlementLootEntry entry in data.choices) if (entry != null) count++;
        float start = PrepareCards(count); int visualIndex = 0;
        if (data?.choices == null) return;
        for (int i = 0; i < data.choices.Count; i++)
        {
            BattleSettlementLootEntry entry = data.choices[i]; if (entry == null) continue;
            RectTransform card = BuildCard("TreasureRelicChoice_" + i, visualIndex++, start, true);
            Button selectCard = card.gameObject.AddComponent<Button>(); selectCard.transition = Selectable.Transition.None;
            selectCard.onClick.AddListener(() => SelectRelicChoice(entry));
            CardIcon(card, entry.icon, true);
            TextMeshProUGUI rarity = ForestText(card, "Category", 94, 270, cardSize.x - 188, 43, 26, RarityColor(entry.rarity), PlainRarity(entry.rarity), true);
            rarity.alignment = TextAlignmentOptions.Midline;
            TextMeshProUGUI name = ForestText(card, "ChoiceTitle", 42, 326, cardSize.x - 84, 55, 33, ParchmentInk, entry.displayName, true);
            name.alignment = TextAlignmentOptions.Midline; name.enableAutoSizing = true; name.fontSizeMin = 22; name.fontSizeMax = 33;
            RectTransform viewport = Box(card, "DescriptionViewport", 48, 384, cardSize.x - 96, 93);
            Image input = viewport.gameObject.AddComponent<Image>(); input.color = Color.clear;
            viewport.gameObject.AddComponent<RectMask2D>();
            ScrollRect scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport; scroll.horizontal = false; scroll.vertical = true; scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 32;
            RectTransform content = Box(viewport, "DescriptionContent", 0, 0, viewport.sizeDelta.x, viewport.sizeDelta.y);
            scroll.content = content;
            TextMeshProUGUI description = ForestText(content, "ChoiceDetail", 0, 0, viewport.sizeDelta.x, viewport.sizeDelta.y, RelicDescriptionSize, ParchmentInk, entry.description);
            description.enableWordWrapping = true; description.overflowMode = TextOverflowModes.Overflow;
            Button select = BuildButton(card, "SelectButton", new Vector2(0, 53), GameText.Get("ui.choose", "选择"), () => SelectRelicChoice(entry));
            var view = new RelicCardView
            {
                entry = entry,
                motion = card.GetComponent<MapTreasureCardMotion>(),
                label = select.GetComponentInChildren<TextMeshProUGUI>(),
                description = description,
                descriptionScroll = scroll
            };
            LayoutRelicDescription(view, MainMenuSettingsPersistence.DetailFontSize(RelicDescriptionSize));
            relicViews.Add(view);
        }
        resultIcon.gameObject.SetActive(false);
    }

    private static void LayoutRelicDescription(RelicCardView view, float size)
    {
        view.description.fontSize = size;
        Vector2 viewportSize = view.descriptionScroll.viewport.sizeDelta;
        float height = Mathf.Max(viewportSize.y, view.description.GetPreferredValues(viewportSize.x, 0).y + 8);
        view.descriptionScroll.content.sizeDelta = view.description.rectTransform.sizeDelta = new Vector2(viewportSize.x, height);
    }

    private void RefreshRelicSelection()
    {
        foreach (RelicCardView card in relicViews)
        {
            bool chosen = activeRelicChoiceData != null && activeRelicChoiceData.selectedChoice == card.entry;
            card.motion.SetSelected(chosen); card.label.text = chosen ? GameText.Get("ui.selected", "已选择") : GameText.Get("ui.choose", "选择");
        }
        if (confirmRelicChoiceButton != null) confirmRelicChoiceButton.interactable = activeRelicChoiceData?.selectedChoice != null;
        if (requireDeselectForRefresh) UpdateRelicRefreshButton();
    }

    private string Category(TreasureRewardChoice choice)
    {
        if (choice.rewardType == TreasureRewardType.Wealth) return GameText.Get("term.gold", "金币");
        if (choice.rewardType == TreasureRewardType.Knowledge) return GameText.Get("ui.map_hud.blueprints", "图纸");
        return PlainRarity(choice.relic != null ? choice.relic.rarity : choice.relicBagRarity);
    }

    private Color CategoryColor(TreasureRewardChoice choice) => choice.rewardType == TreasureRewardType.Power
        ? RarityColor(choice.relic != null ? choice.relic.rarity : choice.relicBagRarity) : ForestGold;

    private static string PlainRarity(LootRarity rarity) => System.Text.RegularExpressions.Regex.Replace(TreasureRewardService.GetColoredRarityLabel(rarity), "<[^>]+>", string.Empty);

    private static string CompactRelicTitle(string source)
    {
        if (string.IsNullOrWhiteSpace(source)) return GameText.Get("ui.map_treasure.relic_reward", "遗物馈赠");
        int separator = source.IndexOfAny(new[] { ':', '：', '\n', '\r' });
        return separator > 0 ? source.Substring(0, separator).Trim() : source;
    }

    private static Color RarityColor(LootRarity rarity)
    {
        switch (rarity)
        {
            case LootRarity.Uncommon: return new Color32(163, 226, 161, 255);
            case LootRarity.Rare: return new Color32(155, 210, 250, 255);
            case LootRarity.Epic: return new Color32(225, 176, 255, 255);
            case LootRarity.Legendary: return new Color32(255, 215, 132, 255);
            default: return new Color32(238, 228, 205, 255);
        }
    }

    private string RewardName(TreasureRewardChoice choice)
    {
        if (choice.rewardType == TreasureRewardType.Wealth) return Mathf.Max(0, choice.moneyAmount) + GameText.Get("ui.map_treasure.gold", " 金币");
        if (choice.rewardType == TreasureRewardType.Knowledge) return choice.blueprint != null ? choice.blueprint.LocalizedName : GameText.Get("ui.map_treasure.no_blueprint_available", "暂无可用图纸");
        if (choice.relic != null) return choice.relic.LocalizedName;
        return PlainRarity(choice.relicBagRarity) + GameText.Get("ui.map_treasure.relic_bag", "遗物袋");
    }

    private Button BuildForestButton(RectTransform parent, string name, Vector2 position, string label, UnityEngine.Events.UnityAction action)
    {
        RectTransform rect = CreateRect(name, parent, new Vector2(0.5f, 0f), position, buttonSize);
        rect.pivot = Vector2.one * 0.5f;
        Image image = rect.gameObject.AddComponent<Image>();
        image.sprite = art != null ? art.button : null; image.type = Image.Type.Simple;
        image.pixelsPerUnitMultiplier = 1.4f; image.color = image.sprite != null ? Color.white : buttonColor;
        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image; button.transition = Selectable.Transition.ColorTint;
        ColorBlock colors = button.colors; colors.highlightedColor = Color.white; colors.pressedColor = new Color(0.88f, 0.82f, 0.65f);
        colors.disabledColor = new Color(0.48f, 0.49f, 0.44f); button.colors = colors;
        button.onClick.AddListener(action);
        rect.gameObject.AddComponent<MapCraftingButtonFeedback>();
        TextMeshProUGUI text = CreateText(rect, "Label", Vector2.zero, buttonSize, 34, ParchmentInk, label, true);
        text.textPreprocessor = null; text.fontStyle = FontStyles.Bold;
        Stretch(text.rectTransform); text.alignment = TextAlignmentOptions.Midline;
        return button;
    }

    private TextMeshProUGUI ForestText(RectTransform parent, string name, float x, float y, float width, float height, int size, Color color, string text, bool keyword = false)
    {
        TextMeshProUGUI result = CreateText(parent, name, new Vector2(x, -y), new Vector2(width, height), size, color, text, keyword);
        if (name == "Title" || name == "ChoiceTitle" || name == "Category") result.textPreprocessor = null;
        if (name == "Title" || name == "ChoiceTitle") result.fontStyle = FontStyles.Bold;
        if (color == ParchmentInk) result.overrideColorTags = true;
        return result;
    }

    private RectTransform Box(RectTransform parent, string name, float x, float y, float width, float height)
        => CreateRect(name, parent, new Vector2(0, 1), new Vector2(x, -y), new Vector2(width, height));

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    public void ApplyViewport(Vector2 viewport, Rect safePixels)
    {
        if (canvas == null) return;
        float width = Mathf.Max(1, viewport.x), height = Mathf.Max(1, viewport.y);
        float x = Mathf.Clamp(safePixels.xMin, 0, width - 1), y = Mathf.Clamp(safePixels.yMin, 0, height - 1);
        Rect safe = new Rect(x, y, Mathf.Clamp(safePixels.width, 1, width - x), Mathf.Clamp(safePixels.height, 1, height - y));
        safeRoot.anchorMin = new Vector2(safe.xMin / width, safe.yMin / height);
        safeRoot.anchorMax = new Vector2(safe.xMax / width, safe.yMax / height);
        safeRoot.offsetMin = safeRoot.offsetMax = Vector2.zero;
        Vector2 available = safe.size * (1080f / height);
        layoutScale = Mathf.Max(0.01f, Mathf.Min(1f, (available.x - 48) / panelSize.x, (available.y - 48) / (panelSize.y + 145)));
        Vector2 room = new Vector2(Mathf.Max(0, (available.x - panelSize.x * layoutScale) * 0.5f - 24), Mathf.Max(0, (available.y - (panelSize.y + 145) * layoutScale) * 0.5f - 24));
        restingPosition = new Vector2(Mathf.Clamp(panelOffset.x, -room.x, room.x), Mathf.Clamp(panelOffset.y, -room.y, room.y) - 72.5f * layoutScale);
        lastViewport = viewport; lastSafeArea = safePixels;
        ApplyPresentationMotion();
    }

    private void BeginPresentation()
    {
        closing = false; handlingAction = false; successTime = 0;
        openProgress = Application.isPlaying ? 0 : 1;
        if (windowGroup != null) windowGroup.interactable = true;
        resultIcon.gameObject.SetActive(false);
        ApplyViewport(new Vector2(Screen.width, Screen.height), Screen.safeArea);
        ApplyPresentationMotion();
    }

    private void PresentResult(string message, Sprite icon)
    {
        isShowingResult = true;
        cardRoot.gameObject.SetActive(false); footerText.gameObject.SetActive(false);
        confirmRelicChoiceButton.gameObject.SetActive(false); refreshRelicChoiceButton.gameObject.SetActive(false);
        continueButton.gameObject.SetActive(true);
        resultText.gameObject.SetActive(true);
        resultText.text = string.IsNullOrWhiteSpace(message) ? GameText.Get("ui.map_treasure.treasure_claimed", "已领取宝藏奖励。") : message;
        resultIcon.sprite = icon; resultIcon.gameObject.SetActive(icon != null);
        titleText.text = GameText.Get("ui.map_treasure.reward_claimed", "领取成功");
        subtitleText.text = GameText.Get("ui.map_treasure.the_treasure_has_answered_your_choice", "宝藏已经回应了你的选择。");
        successTime = Application.isPlaying ? 0.65f : 0f;
    }

    private void Update()
    {
        if (canvas == null || !canvas.gameObject.activeSelf) return;
        float descriptionSize = MainMenuSettingsPersistence.DetailFontSize(RelicDescriptionSize);
        foreach (RelicCardView view in relicViews)
            if (view.description.fontSize != descriptionSize)
                LayoutRelicDescription(view, descriptionSize);
        if (lastViewport != new Vector2(Screen.width, Screen.height) || lastSafeArea != Screen.safeArea)
            ApplyViewport(new Vector2(Screen.width, Screen.height), Screen.safeArea);
        float delta = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
        openProgress = Mathf.MoveTowards(openProgress, closing ? 0 : 1, delta / (closing ? 0.16f : 0.25f));
        successTime = Mathf.Max(0, successTime - delta);
        ApplyPresentationMotion();
        if (closing && openProgress <= 0) FinishHide();
    }

    private void ApplyPresentationMotion()
    {
        if (panel == null || windowGroup == null) return;
        float t = Mathf.SmoothStep(0, 1, openProgress);
        windowGroup.alpha = t;
        windowGroup.interactable = !closing && openProgress >= 0.6f;
        panel.localScale = Vector3.one * (layoutScale * Mathf.Lerp(0.965f, 1f, t));
        panel.anchoredPosition = restingPosition + new Vector2(0, -16 * (1f - t));
        if (backdrop != null) backdrop.color = new Color(0.003f, 0.011f, 0.008f, 0.72f * t);
        if (resultIcon != null) resultIcon.transform.localScale = Vector3.one * (1f + Mathf.Sin((1f - successTime / 0.65f) * Mathf.PI) * 0.065f * successTime / 0.65f);
        for (int i = 0; i < rewardSparks.Count; i++)
        {
            float progress = 1f - successTime / 0.65f;
            float angle = i * 2.39996f;
            rewardSparks[i].anchoredPosition = new Vector2(panelSize.x * 0.5f + Mathf.Cos(angle) * (65 + progress * 110), -340 + Mathf.Sin(angle) * (55 + progress * 80));
            rewardSparks[i].localEulerAngles = new Vector3(0, 0, 45 + progress * 80);
            rewardSparks[i].GetComponent<Image>().color = new Color(1, 0.82f, 0.38f, successTime > 0 ? Mathf.Sin(progress * Mathf.PI) * 0.8f : 0);
        }
    }
}
