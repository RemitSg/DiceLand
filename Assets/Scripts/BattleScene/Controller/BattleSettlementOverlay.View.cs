using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public partial class BattleSettlementOverlay
{
    private static readonly Color SettlementIvory = new Color32(242, 235, 209, 255);
    private static readonly Color SettlementGold = new Color32(208, 170, 94, 255);
    private static readonly Color SettlementExperience = new Color32(184, 201, 244, 255);
    private RectTransform settlementSafeArea, walletRoot, experienceFlightRoot;
    private TextMeshProUGUI moneyRewardText, lootCountText, victoryEnglishText, emptyLootText, walletAmountText, walletCaptionText;
    private ScrollRect partyScroll, settlementLootScroll;
    private MapResourceFeedback walletFeedback;
    private Vector2 settlementViewport;
    private Rect settlementSafePixels;
    private const float PartyWidth = 844f, LootWidth = 790f;

    public void ConfigureFonts(TMP_FontAsset keyword, TMP_FontAsset body)
    {
        if (keyword != null) { titleFont = keyword; buttonFont = keyword; }
        if (body != null) bodyFont = body;
        if (overlayCanvas != null)
            foreach (TMP_Text text in overlayCanvas.GetComponentsInChildren<TMP_Text>(true))
                text.font = text == titleText ? titleFont : bodyFont != null ? bodyFont : titleFont;
    }

    private void ConfigureForestView()
    {
        panelSize = new Vector2(1760f, 940f);
        buttonSize = new Vector2(356f, 76f);
        buttonSpacing = 32f;
        buttonFontSize = 34f;
        returnButtonLabel = "回到地图";
        defeatReturnButtonLabel = "结束冒险";
        retryButtonLabel = "重试战斗";
        hpTextColor = SettlementIvory;
        deadRowColor = new Color32(203, 139, 128, 255);
        lootSlotColor = new Color(0.025f, 0.05f, 0.035f, 1f);
        lootSelectedSlotColor = new Color(0.08f, 0.2f, 0.12f, 1f);
        relicChoiceSlotColor = lootSlotColor;
        relicChoiceSelectedSlotColor = lootSelectedSlotColor;
        overlayCanvas.pixelPerfect = false;
        CanvasScaler scaler = overlayCanvas.GetComponent<CanvasScaler>();
        scaler.referenceResolution = new Vector2(2340, 1080);
        scaler.matchWidthOrHeight = 1f;
        settlementSafeArea = FSRect(overlayRoot, "SettlementSafeArea");
        settlementSafeArea.SetSiblingIndex(1);
        panelRoot.SetParent(settlementSafeArea, false);
        panelRoot.GetComponent<VerticalLayoutGroup>().enabled = false;
        panelRoot.sizeDelta = panelSize;
        panelImage.sprite = null;
        panelImage.color = Color.clear;
        FSFrame(panelRoot, MapMerchantFrameGraphic.Style.Panel).transform.SetAsFirstSibling();
        summaryRoot.GetComponent<VerticalLayoutGroup>().enabled = false;
        contentFlexSpacerRoot.gameObject.SetActive(false);
        buttonReserveRoot.gameObject.SetActive(false);

        FSPlace(titleText.rectTransform, 790, 20, 340, 96);
        titleText.fontSize = 72;
        titleText.enableAutoSizing = true;
        titleText.fontSizeMin = 28;
        titleText.fontSizeMax = 72;
        titleText.enableWordWrapping = false;
        titleText.color = new Color32(241, 204, 111, 255);
        titleText.alignment = TextAlignmentOptions.Center;
        backdropImage.color = new Color(0, 0, 0, 0.58f);
        ApplyTextOutline(titleText, new Color(0.15f, 0.11f, 0.04f), 0.08f);
        victoryEnglishText = FSText(panelRoot, "VictoryEnglish", "VICTORY", 25, SettlementGold, false);
        FSPlace(victoryEnglishText.rectTransform, 560, 106, 640, 34);
        victoryEnglishText.alignment = TextAlignmentOptions.Center;
        FSPlace(subtitleText.rectTransform, 260, 143, 1240, 42);
        subtitleText.fontSize = 29;
        subtitleText.color = SettlementIvory;
        Image leftSword = FSIcon(panelRoot, "VictorySwordLeft", MapForestArt.Get("Sword"), 674, 26, 100, 100);
        Image rightSword = FSIcon(panelRoot, "VictorySwordRight", MapForestArt.Get("Sword"), 674, 26, 100, 100);
        foreach (Image sword in new[] { leftSword, rightSword })
        {
            sword.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            sword.rectTransform.anchoredPosition = new Vector2(724, -76);
        }
        leftSword.rectTransform.localRotation = Quaternion.Euler(0, 0, -38);
        rightSword.rectTransform.localRotation = Quaternion.Euler(0, 0, 38);

        RectTransform moneyCapsule = FSPanel(panelRoot, "MoneyRewardCapsule", MapMerchantFrameGraphic.Style.Wallet);
        RectTransform experienceCapsule = FSPanel(panelRoot, "ExperienceRewardCapsule", MapMerchantFrameGraphic.Style.Wallet);
        RectTransform lootCapsule = FSPanel(panelRoot, "LootCountCapsule", MapMerchantFrameGraphic.Style.Wallet);
        FSPlace(moneyCapsule, 250, 203, 396, 70);
        FSPlace(experienceCapsule, 682, 203, 396, 70);
        FSPlace(lootCapsule, 1114, 203, 396, 70);
        moneyRewardText = FSText(moneyCapsule, "MoneyReward", "金币 +0", 35, SettlementIvory);
        expText.transform.SetParent(experienceCapsule, false);
        expText.fontSize = 35;
        expText.color = SettlementExperience;
        lootCountText = FSText(lootCapsule, "LootCount", "战利品 ×0", 34, SettlementIvory, false);
        foreach (TMP_Text label in new TMP_Text[] { moneyRewardText, expText, lootCountText })
        { FSStretch(label.rectTransform, 16, 8); label.alignment = TextAlignmentOptions.Center; label.enableAutoSizing = false; }
        FSIcon(lootCapsule, "LootRewardIcon", MapForestArt.Get("Treasure"), 47, 10, 50, 50);
        FSPlace(lootCountText.rectTransform, 112, 8, 270, 54);
        lootCountText.alignment = TextAlignmentOptions.MidlineLeft;
        FSDivider(panelRoot, "HeaderDivider", 42, 292, 1676);
        TextMeshProUGUI partyHeading = FSText(panelRoot, "PartyHeading", "队伍状态", 35, SettlementIvory, false);
        FSPlace(partyHeading.rectTransform, 50, 307, 250, 50);
        lootHeaderText.transform.SetParent(panelRoot, false);
        FSPlace(lootHeaderText.rectTransform, 926, 307, 760, 50);
        lootHeaderText.fontSize = 35;
        lootHeaderText.color = SettlementIvory;
        lootHeaderText.alignment = TextAlignmentOptions.MidlineLeft;
        partyScroll = FSScroll("PartyScroll", summaryRoot);
        FSPlace((RectTransform)partyScroll.transform, 48, 368, PartyWidth + 18, 438);
        settlementLootScroll = FSScroll("LootScroll", lootGridRoot);
        FSPlace((RectTransform)settlementLootScroll.transform, 924, 368, LootWidth + 18, 438);
        lootRoot.gameObject.SetActive(false);
        TextMeshProUGUI hint = FSText(panelRoot, "LootHint", "点击战利品查看详情", 23, new Color(0.74f, 0.76f, 0.69f), false);
        FSPlace(hint.rectTransform, 930, 808, 744, 36);
        hint.alignment = TextAlignmentOptions.Center;
        emptyLootText = FSText(settlementLootScroll.viewport, "EmptyLoot", "本次没有获得战利品。", 28, SettlementIvory, false);
        FSPlace(emptyLootText.rectTransform, 0, 90, LootWidth, 80);
        emptyLootText.alignment = TextAlignmentOptions.Center;
        FSDivider(panelRoot, "FooterDivider", 42, 844, 1676);
        FSPlace(buttonRoot, 508, 852, 744, 76);

        walletRoot = FSPanel(settlementSafeArea, "SettlementWallet", MapMerchantFrameGraphic.Style.Wallet);
        walletRoot.anchorMin = walletRoot.anchorMax = walletRoot.pivot = Vector2.one;
        walletRoot.sizeDelta = new Vector2(410, 106);
        walletCaptionText = FSText(walletRoot, "WalletCaption", "持有", 23, SettlementIvory, false);
        FSPlace(walletCaptionText.rectTransform, 22, 12, 94, 32);
        FSIcon(walletRoot, "MoneyIcon", MapForestArt.Get("Coins"), 28, 49, 42, 38);
        TextMeshProUGUI currency = FSText(walletRoot, "CurrencyName", "金币", 26, SettlementIvory, false);
        ResourceKeywordText.Attach(currency, false);
        FSPlace(currency.rectTransform, 84, 48, 80, 38);
        walletAmountText = FSText(walletRoot, "Money", "0", 36, new Color32(240, 204, 107, 255), false);
        FSPlace(walletAmountText.rectTransform, 170, 43, 216, 46);
        walletAmountText.alignment = TextAlignmentOptions.MidlineRight;
        GameObject walletHost = new GameObject("SettlementWalletFeedback");
        walletHost.transform.SetParent(transform, false);
        walletFeedback = walletHost.AddComponent<MapResourceFeedback>();
        walletFeedback.AutomaticUpdate = false;
        walletFeedback.SetCanvasSortingOrder(canvasSortOrder + 10);
        walletRoot.gameObject.SetActive(false);
        experienceFlightRoot = FSRect(overlayRoot, "ExperienceFlights");
        FSStretch(experienceFlightRoot);
        experienceFlightRoot.SetSiblingIndex(2);
        ApplyViewport(new Vector2(Screen.width, Screen.height), Screen.safeArea);
    }

    public void ApplyViewport(Vector2 viewport, Rect safePixels)
    {
        if (settlementSafeArea == null) return;
        float width = Mathf.Max(1, viewport.x), height = Mathf.Max(1, viewport.y);
        Rect safe = new Rect(Mathf.Clamp(safePixels.x, 0, width - 1), Mathf.Clamp(safePixels.y, 0, height - 1),
            Mathf.Max(1, safePixels.width), Mathf.Max(1, safePixels.height));
        safe.width = Mathf.Min(safe.width, width - safe.x);
        safe.height = Mathf.Min(safe.height, height - safe.y);
        settlementSafeArea.anchorMin = new Vector2(safe.xMin / width, safe.yMin / height);
        settlementSafeArea.anchorMax = new Vector2(safe.xMax / width, safe.yMax / height);
        settlementSafeArea.offsetMin = settlementSafeArea.offsetMax = Vector2.zero;
        Vector2 available = safe.size * (1080f / height);
        float scale = Mathf.Min(1f, (available.x - 48f) / panelSize.x, (available.y - 48f) / panelSize.y);
        panelRoot.anchorMin = panelRoot.anchorMax = panelRoot.pivot = new Vector2(0.5f, 0.5f);
        panelRoot.anchoredPosition = Vector2.zero;
        panelRoot.localScale = Vector3.one * Mathf.Max(0.1f, scale);
        walletRoot.localScale = Vector3.one * Mathf.Min(1, Mathf.Max(0.5f, scale));
        settlementViewport = viewport;
        settlementSafePixels = safePixels;
        if (lootDetailView != null) lootDetailView.ApplyViewport(viewport, safe);
    }

    private void BuildPortraitSummaryRows(IReadOnlyList<BattleSettlementEntry> entries)
    {
        float top = 0;
        if (entries != null) for (int i = 0; i < entries.Count; i++)
        {
            BattleSettlementEntry entry = entries[i];
            if (entry == null) continue;
            RectTransform root = FSPanel(summaryRoot, "SummaryRow_" + i, MapMerchantFrameGraphic.Style.Inset);
            spawnedRows.Add(root.gameObject);
            TextMeshProUGUI name = FSText(root, "NameText", BuildSettlementName(entry), 32, entry.isAlive ? SettlementIvory : deadRowColor, false);
            name.enableWordWrapping = true;
            float nameHeight = Mathf.Max(44, name.GetPreferredValues(172, 0).y + 4);
            float rowHeight = Mathf.Max(200, nameHeight + 90);
            FSPlace(root, 0, top, PartyWidth, rowHeight);
            RectTransform portrait = FSPanel(root, "PortraitWell", MapMerchantFrameGraphic.Style.IconWell);
            FSPlace(portrait, 18, 20, 150, 158);
            Sprite sprite = entry.portrait;
            Image icon = FSIcon(portrait, "Portrait", sprite, 8, 8, 134, 142);
            icon.color = entry.isAlive ? Color.white : new Color(0.48f, 0.48f, 0.48f);
            if (sprite == null)
            {
                TextMeshProUGUI fallback = FSText(portrait, "PortraitFallback", string.IsNullOrEmpty(entry.displayName) ? "?" : entry.displayName.Substring(0, 1), 52, SettlementIvory, false);
                FSStretch(fallback.rectTransform, 8, 8); fallback.alignment = TextAlignmentOptions.Center;
            }
            FSPlace(name.rectTransform, 190, 24, 172, nameHeight);
            TextMeshProUGUI level = FSText(root, "LevelText", "Lv." + Mathf.Max(1, entry.level), 29, SettlementGold, false);
            FSPlace(level.rectTransform, 190, 35 + nameHeight, 160, 42);
            TextMeshProUGUI hp = FSText(root, "HpText", "", 27, hpTextColor, false);
            TextMeshProUGUI xp = FSText(root, "ExpText", "", 27, SettlementExperience);
            FSPlace(hp.rectTransform, 390, 24, 426, 38);
            FSPlace(xp.rectTransform, 390, 104, 426, 40);
            RectTransform hpBar = FSBar(root, "HpBar", 390, 67, 426, 22, new Color32(229, 58, 67, 255), out RectTransform hpFill);
            RectTransform xpBar = FSBar(root, "ExpBar", 390, 153, 426, 20, SettlementExperience, out RectTransform xpFill);
            SummaryRowView row = new SummaryRowView { root = root, entry = entry, nameText = name, hpText = hp, expText = xp, levelText = level,
                hpFillRect = hpFill, hpFillWidth = 422, expFillRect = xpFill, expFillWidth = 422 };
            rowViews.Add(row);
            UpdateSummaryRow(row, Mathf.Max(1, entry.level), entry.currentExperience, RequirementFor(row, entry.level), entry.currentHP, entry.maxHP);
            top += rowHeight + 14;
        }
        summaryRoot.sizeDelta = new Vector2(PartyWidth, Mathf.Max(1, top - 14));
        partyScroll.verticalScrollbar.gameObject.SetActive(top - 14 > 438);
        partyScroll.verticalNormalizedPosition = 1f;
    }

    private LootItemView BuildForestLootItem(BattleSettlementLootEntry entry)
    {
        RectTransform root = FSRect(lootGridRoot, "Loot_" + entry.displayName);
        spawnedLootObjects.Add(root.gameObject);
        Image background = root.gameObject.AddComponent<Image>(); background.color = lootSlotColor;
        RectTransform well = FSPanel(root, "LootIconWell", MapMerchantFrameGraphic.Style.IconWell);
        FSPlace(well, 5, 0, 241, 151);
        CanvasGroup reveal = FSRect(root, "RevealedItem").gameObject.AddComponent<CanvasGroup>();
        FSStretch((RectTransform)reveal.transform);
        Image icon = FSIcon(reveal.transform, "Icon", entry.icon ?? GetFallbackLootSprite(), 56, 12, 138, 126);
        TextMeshProUGUI count = FSText(reveal.transform, "Count", "×" + Mathf.Max(1, entry.amount), 26, SettlementIvory, false);
        FSPlace(count.rectTransform, 150, 108, 82, 38); count.alignment = TextAlignmentOptions.MidlineRight;
        TextMeshProUGUI name = FSText(reveal.transform, "LootName", entry.displayName, 25, FSRarity(entry.rarity), false);
        FSPlace(name.rectTransform, 5, 160, 241, 65); name.alignment = TextAlignmentOptions.Top;
        TextMeshProUGUI sealedText = FSText(root, "SealedLoot", "?", 48, new Color(0.59f, 0.52f, 0.33f), false);
        FSPlace(sealedText.rectTransform, 6, 30, 240, 80); sealedText.alignment = TextAlignmentOptions.Center;
        Button button = root.gameObject.AddComponent<Button>(); button.targetGraphic = background;
        LootItemView view = new LootItemView { entry = entry, button = button, slotImage = background, iconImage = icon, countText = count,
            nameText = name, revealGroup = reveal, sealedLabel = sealedText.gameObject };
        button.onClick.AddListener(() =>
        {
            if (entry.isRelicChoiceEntrance) OpenRelicChoiceModal(entry.relicChoiceData);
            else if (currentSelectedLootView == view) HideLootDetailModal();
            else ShowLootDetailModal(entry, view);
        });
        RefreshLootItemViewPresentation(view);
        return view;
    }

    private void LayoutForestLoot(int count)
    {
        GridLayoutGroup grid = lootGridRoot.GetComponent<GridLayoutGroup>();
        grid.constraintCount = 3;
        float width = (LootWidth - 36) / 3f, height = 204;
        foreach (LootItemView view in lootViews)
            if (view.nameText != null) height = Mathf.Max(height, 166 + view.nameText.GetPreferredValues(width - 10, 0).y + 8);
        grid.cellSize = new Vector2(width, height);
        grid.spacing = new Vector2(18, 18);
        grid.childAlignment = TextAnchor.UpperLeft;
        lootGridRoot.sizeDelta = new Vector2(LootWidth, Mathf.Max(1, Mathf.CeilToInt(count / 3f) * (height + 18) - 18));
        foreach (LootItemView view in lootViews)
        {
            RectTransform root = (RectTransform)view.button.transform;
            FSPlace((RectTransform)root.Find("LootIconWell"), 5, 0, width - 10, 151);
            FSPlace(view.iconImage.rectTransform, (width - 138) / 2, 12, 138, 126);
            FSPlace(view.countText.rectTransform, width - 90, 108, 76, 38);
            FSPlace(view.nameText.rectTransform, 5, 160, width - 10, height - 160);
        }
        emptyLootText.gameObject.SetActive(count == 0);
        settlementLootScroll.verticalScrollbar.gameObject.SetActive(lootGridRoot.sizeDelta.y > 438);
        settlementLootScroll.verticalNormalizedPosition = 1f;
    }

    private void StyleForestButtons()
    {
        foreach (Button button in spawnedButtons)
        {
            bool primary = button.name == "ReturnButton";
            Image image = button.GetComponent<Image>(); image.sprite = null; image.color = Color.clear; image.raycastTarget = true;
            MapMerchantFrameGraphic frame = FSFrame((RectTransform)button.transform, primary ? MapMerchantFrameGraphic.Style.GoldButton : MapMerchantFrameGraphic.Style.SecondaryButton);
            frame.transform.SetAsFirstSibling();
            button.targetGraphic = frame;
            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            label.fontSize = 34; label.color = primary ? new Color(0.17f, 0.11f, 0.035f) : SettlementIvory;
            FSStretch(label.rectTransform, 16, 8); label.rectTransform.offsetMin = new Vector2(68, 8);
            RectTransform glyphRect = FSRect(button.transform, "ActionGlyph"); FSPlace(glyphRect, 28, 19, 38, 38);
            MapHudGlyph glyph = glyphRect.gameObject.AddComponent<MapHudGlyph>();
            glyph.shape = primary ? MapHudGlyph.Shape.Book : MapHudGlyph.Shape.Refresh;
            glyph.color = label.color; glyph.raycastTarget = false;
        }
    }

    private ScrollRect FSScroll(string name, RectTransform content)
    {
        RectTransform root = FSRect(panelRoot, name);
        root.gameObject.AddComponent<Image>().color = Color.clear;
        ScrollRect scroll = root.gameObject.AddComponent<ScrollRect>();
        RectTransform viewport = FSRect(root, "Viewport"); FSStretch(viewport); viewport.offsetMax = new Vector2(-18, 0);
        viewport.gameObject.AddComponent<Image>().color = Color.clear;
        viewport.gameObject.AddComponent<RectMask2D>();
        content.SetParent(viewport, false); content.anchorMin = content.anchorMax = content.pivot = new Vector2(0, 1); content.anchoredPosition = Vector2.zero;
        RectTransform track = FSRect(root, "Scrollbar");
        track.anchorMin = new Vector2(1, 0); track.anchorMax = track.pivot = Vector2.one; track.offsetMin = new Vector2(-8, 0); track.offsetMax = Vector2.zero;
        track.gameObject.AddComponent<Image>().color = new Color(0.75f, 0.6f, 0.3f, 0.1f);
        Scrollbar bar = track.gameObject.AddComponent<Scrollbar>();
        RectTransform handle = FSRect(track, "Handle"); FSStretch(handle);
        Image handleImage = handle.gameObject.AddComponent<Image>(); handleImage.color = SettlementGold;
        bar.handleRect = handle; bar.targetGraphic = handleImage; bar.direction = Scrollbar.Direction.BottomToTop;
        scroll.viewport = viewport; scroll.content = content; scroll.horizontal = false; scroll.scrollSensitivity = 32;
        scroll.movementType = ScrollRect.MovementType.Clamped; scroll.verticalScrollbar = bar; scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
        return scroll;
    }

    private RectTransform FSBar(Transform parent, string name, float x, float y, float width, float height, Color tint, out RectTransform fill)
    {
        Image border = CreateImage(parent, name, new Color(0.65f, 0.54f, 0.31f, 0.7f), null);
        FSPlace(border.rectTransform, x, y, width, height);
        Image background = CreateImage(border.transform, "Track", new Color(0.055f, 0.07f, 0.052f), null);
        FSStretch(background.rectTransform, 2, 2);
        Image foreground = CreateImage(background.transform, "Fill", tint, null); foreground.raycastTarget = false;
        fill = foreground.rectTransform; fill.anchorMin = Vector2.zero; fill.anchorMax = new Vector2(0, 1); fill.pivot = new Vector2(0, 0.5f);
        fill.offsetMin = fill.offsetMax = Vector2.zero;
        return border.rectTransform;
    }

    private TextMeshProUGUI FSText(Transform parent, string name, string value, float size, Color tint, bool keywords = true)
    {
        TextMeshProUGUI text = FSRect(parent, name).gameObject.AddComponent<TextMeshProUGUI>();
        if (bodyFont != null || titleFont != null) text.font = bodyFont != null ? bodyFont : titleFont;
        text.text = value; text.fontSize = size; text.color = tint; text.raycastTarget = false;
        text.enableWordWrapping = true; text.enableAutoSizing = false; text.overflowMode = TextOverflowModes.Overflow;
        text.alignment = TextAlignmentOptions.MidlineLeft;
        if (keywords) ResourceKeywordText.Attach(text);
        return text;
    }

    private static RectTransform FSRect(Transform parent, string name)
    {
        RectTransform rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); rect.SetParent(parent, false); return rect;
    }
    private static void FSPlace(RectTransform rect, float x, float y, float width, float height)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(x, -y); rect.sizeDelta = new Vector2(width, height);
    }
    private static void FSStretch(RectTransform rect, float x = 0, float y = 0)
    { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = new Vector2(x, y); rect.offsetMax = new Vector2(-x, -y); }
    private static MapMerchantFrameGraphic FSFrame(RectTransform rect, MapMerchantFrameGraphic.Style style)
    {
        RectTransform frame = FSRect(rect, "BrassSurface"); FSStretch(frame);
        MapMerchantFrameGraphic graphic = frame.gameObject.AddComponent<MapMerchantFrameGraphic>(); graphic.style = style; graphic.raycastTarget = false; return graphic;
    }
    private static RectTransform FSPanel(Transform parent, string name, MapMerchantFrameGraphic.Style style)
    { RectTransform rect = FSRect(parent, name); FSFrame(rect, style); return rect; }
    private static Image FSIcon(Transform parent, string name, Sprite sprite, float x, float y, float width, float height)
    {
        Image icon = FSRect(parent, name).gameObject.AddComponent<Image>(); FSPlace(icon.rectTransform, x, y, width, height);
        icon.sprite = sprite; icon.enabled = sprite != null; icon.preserveAspect = true; icon.raycastTarget = false; return icon;
    }
    private static void FSDivider(Transform parent, string name, float x, float y, float width)
    {
        RectTransform rect = FSPanel(parent, name, MapMerchantFrameGraphic.Style.Divider); FSPlace(rect, x, y, width, 8);
    }
    private static Color FSRarity(LootRarity rarity)
    {
        switch (rarity)
        {
            case LootRarity.Common: return new Color32(160, 162, 153, 255);
            case LootRarity.Uncommon: return new Color32(56, 178, 86, 255);
            case LootRarity.Rare: return new Color32(64, 126, 255, 255);
            case LootRarity.Epic: return new Color32(163, 92, 255, 255);
            case LootRarity.Legendary: return new Color32(230, 180, 44, 255);
            default: return new Color32(149, 115, 77, 255);
        }
    }
}
