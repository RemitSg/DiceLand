using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public partial class MapInventoryDetailView
{
    private RectTransform weaponRoot, weaponSkillsHeader, weaponSkillsRule, weaponScrollRoot, weaponViewport, weaponContent;
    private RectTransform weaponSpeedBadge, weaponCountBadge;
    private TextMeshProUGUI weaponSkillsTitle, weaponBasis, weaponSpeedText, weaponCountText, weaponReadingHint;
    private Image weaponTitleArt;
    private ScrollRect weaponScroll;
    private Scrollbar weaponScrollbar;
    private MapMerchantFrameGraphic weaponCloseFace, weaponEquipFace, weaponDismissFace;
    private readonly List<WeaponSkillCard> weaponCards = new List<WeaponSkillCard>();
    private readonly List<EnemySkill_SO> weaponSkills = new List<EnemySkill_SO>();
    private float weaponDisplayAge;
    private bool weaponLayoutDirty;
    private bool UsesWeaponLayout => !encyclopediaEntry && displayedItem is Weapon_SO;

    private sealed class WeaponSkillCard
    {
        public RectTransform rect, energyBadge, defaultBadge;
        public TextMeshProUGUI name, energy, defaultLabel, requirements, description;
        public CanvasGroup group;
        public RectTransform descriptionClip;
        public CanvasGroup descriptionGroup;
        public bool descriptionExpanded;
        public float descriptionReveal;
        public readonly List<WeaponDieRow> dice = new List<WeaponDieRow>();
        public int diceCount;
    }

    private sealed class WeaponDieRow
    {
        public RectTransform header;
        public TextMeshProUGUI heading, range, rules;
        public BattleSkillFrameGraphic icon;
    }

    private static string WeaponText(string chinese, string english) => GameText.LanguageCode == GameText.Chinese ? chinese : english;

    private void EnsureWeaponPresentation()
    {
        if (weaponRoot != null) return;
        weaponRoot = DetailRect(detailRect, "WeaponPresentation"); StretchDetail(weaponRoot); weaponRoot.SetAsFirstSibling();
        RectTransform backing = DetailRect(weaponRoot, "WeaponOpaqueBacking"); StretchDetail(backing, 16, 16);
        MapMerchantFrameGraphic backingFace = backing.gameObject.AddComponent<MapMerchantFrameGraphic>();
        backingFace.style = MapMerchantFrameGraphic.Style.Inset; backingFace.raycastTarget = false;
        RectTransform frameRect = DetailRect(weaponRoot, "WeaponForestFrame"); StretchDetail(frameRect);
        Image frame = frameRect.gameObject.AddComponent<Image>();
        frame.sprite = Resources.Load<Sprite>("TutorialUI/ForestWindow"); frame.type = Image.Type.Sliced;
        frame.pixelsPerUnitMultiplier = 3.4f; frame.raycastTarget = false;
        RectTransform plaque = DetailRect(titlePlaque, "WeaponTitleArt"); StretchDetail(plaque); plaque.SetAsFirstSibling();
        weaponTitleArt = plaque.gameObject.AddComponent<Image>();
        weaponTitleArt.sprite = Resources.Load<Sprite>("TutorialUI/WoodTitle"); weaponTitleArt.raycastTarget = false;
        weaponSpeedBadge = WeaponBadge(weaponRoot, "WeaponSpeedBadge");
        weaponSpeedText = WeaponLabel(weaponSpeedBadge, "SpeedSlotCount", 24, string.Empty, false);
        weaponSpeedText.enableAutoSizing = true; weaponSpeedText.fontSizeMin = 16; weaponSpeedText.fontSizeMax = 24;
        StretchDetail(weaponSpeedText.rectTransform, 8, 5); weaponSpeedText.alignment = TextAlignmentOptions.Midline;
        weaponCountBadge = WeaponBadge(weaponRoot, "WeaponSkillCountBadge");
        weaponCountText = WeaponLabel(weaponCountBadge, "WeaponSkillCount", 24, string.Empty, false);
        weaponCountText.enableAutoSizing = true; weaponCountText.fontSizeMin = 16; weaponCountText.fontSizeMax = 24;
        StretchDetail(weaponCountText.rectTransform, 8, 5); weaponCountText.alignment = TextAlignmentOptions.Midline;
        weaponSkillsHeader = DetailRect(weaponRoot, "WeaponSkillsHeader");
        weaponSkillsTitle = WeaponLabel(weaponSkillsHeader, "WeaponSkillsTitle", 38, string.Empty, true);
        weaponBasis = WeaponLabel(weaponSkillsHeader, "WeaponBaseValues", 24, string.Empty, true);
        weaponBasis.alignment = TextAlignmentOptions.MidlineRight;
        weaponSkillsRule = DetailRect(weaponRoot, "WeaponSkillsRule"); DetailRule(weaponSkillsRule);
        weaponScrollRoot = DetailRect(weaponRoot, "WeaponSkillsScroll");
        Image hit = weaponScrollRoot.gameObject.AddComponent<Image>(); hit.color = Color.clear;
        weaponScroll = weaponScrollRoot.gameObject.AddComponent<ScrollRect>();
        weaponScroll.horizontal = false; weaponScroll.vertical = true; weaponScroll.scrollSensitivity = 60;
        weaponScroll.movementType = ScrollRect.MovementType.Clamped; weaponScroll.decelerationRate = .08f;
        weaponViewport = DetailRect(weaponScrollRoot, "Viewport"); weaponViewport.gameObject.AddComponent<RectMask2D>();
        weaponContent = DetailRect(weaponViewport, "SkillCards");
        weaponContent.anchorMin = weaponContent.anchorMax = weaponContent.pivot = new Vector2(0, 1);
        weaponScroll.viewport = weaponViewport; weaponScroll.content = weaponContent;
        RectTransform track = DetailRect(weaponScrollRoot, "SkillsScrollbar");
        Image trackImage = track.gameObject.AddComponent<Image>(); trackImage.color = new Color32(17, 31, 19, 255);
        weaponScrollbar = track.gameObject.AddComponent<Scrollbar>(); weaponScrollbar.direction = Scrollbar.Direction.BottomToTop;
        RectTransform handle = DetailRect(track, "Handle"); StretchDetail(handle);
        Image handleImage = handle.gameObject.AddComponent<Image>(); handleImage.color = new Color32(225, 176, 67, 255);
        weaponScrollbar.handleRect = handle; weaponScrollbar.targetGraphic = handleImage;
        weaponScroll.verticalScrollbar = weaponScrollbar; weaponScroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
        weaponReadingHint = WeaponLabel(weaponRoot, "WeaponReadingHint", 19, string.Empty, false);
        weaponReadingHint.alignment = TextAlignmentOptions.MidlineRight;
        weaponScroll.onValueChanged.AddListener(_ => RefreshWeaponReadingHint());
        weaponCloseFace = CreateWeaponButtonFace(closeButton, MapMerchantFrameGraphic.Style.SecondaryButton);
        weaponEquipFace = CreateWeaponButtonFace(actionButton, MapMerchantFrameGraphic.Style.GoldButton);
        weaponDismissFace = CreateWeaponButtonFace(closeXButton, MapMerchantFrameGraphic.Style.GoldButton);
        weaponRoot.gameObject.SetActive(false);
    }

    private RectTransform WeaponBadge(Transform parent, string name)
    {
        RectTransform rect = DetailRect(parent, name);
        MapMerchantFrameGraphic face = rect.gameObject.AddComponent<MapMerchantFrameGraphic>();
        face.style = MapMerchantFrameGraphic.Style.Badge; face.raycastTarget = false;
        return rect;
    }

    private TextMeshProUGUI WeaponLabel(Transform parent, string name, float size, string value, bool heading)
    {
        TextMeshProUGUI text = DetailText(parent, name, size, DetailIvory, value);
        ApplyFont(text, heading ? detailHeadingFont : detailBodyFont);
        text.overflowMode = TextOverflowModes.Overflow;
        ResourceKeywordText.Attach(text, false);
        return text;
    }

    private void PrepareWeaponPresentation(Weapon_SO weapon, int amount, bool changed)
    {
        if (modalRoot == null) return;
        bool active = weapon != null && !encyclopediaEntry;
        if (active) EnsureWeaponPresentation();
        if (weaponRoot == null) return;
        bool previouslyActive = weaponRoot.gameObject.activeSelf;
        weaponRoot.gameObject.SetActive(active);
        detailRect.Find("ItemDetailFrame").gameObject.SetActive(!active);
        titlePlaque.GetComponent<MapItemDetailFrameGraphic>().enabled = !active;
        weaponTitleArt.gameObject.SetActive(active);
        descriptionWell.GetComponent<MapCraftingFrameGraphic>().enabled = !active;
        descriptionHeading.gameObject.SetActive(!active);
        SetWeaponButtonFace(closeButton, weaponCloseFace, active);
        SetWeaponButtonFace(actionButton, weaponEquipFace, active);
        SetWeaponButtonFace(closeXButton, weaponDismissFace, active);
        headingText.fontSize = active ? 46 : 36;
        headingText.color = active ? DetailIvory : DetailGold;
        titleText.fontSizeMax = active ? 52 : 48;
        ApplyFont(titleText, active ? detailHeadingFont : detailBodyFont);
        if (!active)
        {
            StretchDetail(headingText.rectTransform, 44, 7);
            titleText.alignment = TextAlignmentOptions.MidlineLeft;
            actionStatusText.alignment = TextAlignmentOptions.Midline;
            closeLabel.color = new Color32(48, 33, 12, 255);
            actionLabel.color = DetailIvory;
            descriptionScroll.vertical = true;
            if (previouslyActive) ResourceKeywordText.Attach(descriptionText, !encyclopediaTextOnly);
            return;
        }
        ResourceKeywordText.Attach(descriptionText, false);
        headingText.text = WeaponText("武器详情", "Weapon Details");
        closeLabel.text = inventoryContext != null ? WeaponText("返回背包", "Back to Inventory") : GameText.Get("ui.close", "关闭");
        quantityText.text = WeaponText("持有 ×", "Owned ×") + Mathf.Max(1, amount);
        weaponSkillsTitle.text = WeaponText("武器技能", "Weapon Skills");
        weaponBasis.text = WeaponText("基础数值", "Base Values");
        weaponSkills.Clear();
        if (weapon.skillPool != null)
            foreach (EnemySkill_SO skill in weapon.skillPool)
                if (skill != null && !weaponSkills.Contains(skill)) weaponSkills.Add(skill);
        weaponSpeedText.text = WeaponText("速度槽", "Speed Slots") + "\n<size=135%>" + Mathf.Max(1, weapon.speedButtonCount) + "</size>";
        weaponCountText.text = WeaponText("技能", "Skills") + "\n<size=135%>" + weaponSkills.Count + "</size>";
        if (encyclopediaSkillFormatter == null)
        {
            encyclopediaSkillFormatter = GetComponent<BattleSkillInfoOverlay>() ?? gameObject.AddComponent<BattleSkillInfoOverlay>();
            encyclopediaSkillFormatter.enabled = false;
        }
        int count = Mathf.Max(1, weaponSkills.Count);
        while (weaponCards.Count < count) weaponCards.Add(BuildWeaponSkillCard(weaponCards.Count));
        for (int i = 0; i < weaponCards.Count; i++)
        {
            WeaponSkillCard card = weaponCards[i]; card.rect.gameObject.SetActive(i < count);
            if (i >= count) continue;
            PopulateWeaponSkillCard(card, weaponSkills.Count > 0 ? weaponSkills[i] : null, weapon, changed || !previouslyActive);
        }
        ApplyWeaponFonts();
        weaponLayoutDirty = true;
        if (changed || !previouslyActive)
        {
            weaponDisplayAge = Application.isPlaying ? 0 : 1;
            weaponScroll.StopMovement(); weaponContent.anchoredPosition = Vector2.zero;
        }
    }

    private static void SetWeaponButtonFace(Button button, MapMerchantFrameGraphic face, bool active)
    {
        MapCraftingFrameGraphic original = button.GetComponent<MapCraftingFrameGraphic>();
        original.enabled = !active; face.gameObject.SetActive(active);
        button.targetGraphic = active ? (Graphic)face : original;
        TMP_Text text = button.GetComponentInChildren<TMP_Text>(true);
        text.enableAutoSizing = active; text.fontSizeMin = 22; text.fontSizeMax = active ? 38 : 30;
        text.fontSize = active ? 38 : 30;
    }

    private static MapMerchantFrameGraphic CreateWeaponButtonFace(Button button, MapMerchantFrameGraphic.Style style)
    {
        RectTransform rect = DetailRect(button.transform, "WeaponButtonFace"); StretchDetail(rect); rect.SetAsFirstSibling();
        MapMerchantFrameGraphic face = rect.gameObject.AddComponent<MapMerchantFrameGraphic>(); face.style = style;
        return face;
    }

    private WeaponSkillCard BuildWeaponSkillCard(int index)
    {
        var card = new WeaponSkillCard { rect = DetailRect(weaponContent, "WeaponSkill_" + index) };
        MapWeaponDetailGraphic face = card.rect.gameObject.AddComponent<MapWeaponDetailGraphic>();
        card.rect.gameObject.AddComponent<MapWeaponSkillCardFeedback>().face = face;
        Button read = card.rect.gameObject.AddComponent<Button>(); read.targetGraphic = face; read.transition = Selectable.Transition.None;
        read.navigation = new Navigation { mode = Navigation.Mode.None };
        read.onClick.AddListener(() =>
        {
            if (string.IsNullOrWhiteSpace(card.description.text)) return;
            card.descriptionExpanded = !card.descriptionExpanded;
            if (!Application.isPlaying) card.descriptionReveal = card.descriptionExpanded ? 1 : 0;
            weaponLayoutDirty = true;
            if (!Application.isPlaying) RefreshWeaponLayout(false);
        });
        card.group = card.rect.gameObject.AddComponent<CanvasGroup>();
        card.name = WeaponLabel(card.rect, "SkillName", 35, string.Empty, true); card.name.enableWordWrapping = true;
        card.defaultBadge = WeaponBadge(card.rect, "DefaultSkillBadge");
        card.defaultLabel = WeaponLabel(card.defaultBadge, "DefaultSkill", 21, WeaponText("默认", "Default"), true);
        card.defaultLabel.enableAutoSizing = true; card.defaultLabel.fontSizeMin = 14; card.defaultLabel.fontSizeMax = 21;
        StretchDetail(card.defaultLabel.rectTransform, 5, 2); card.defaultLabel.alignment = TextAlignmentOptions.Midline;
        card.energyBadge = WeaponBadge(card.rect, "EnergyBadge");
        RectTransform bolt = DetailRect(card.energyBadge, "EnergyIcon");
        MapWeaponDetailGraphic glyph = bolt.gameObject.AddComponent<MapWeaponDetailGraphic>(); glyph.part = MapWeaponDetailGraphic.Part.Energy; glyph.raycastTarget = false;
        PlaceDetail(bolt, 8, 6, 28, 28);
        card.energy = WeaponLabel(card.energyBadge, "EnergyCost", 24, string.Empty, false);
        card.energy.alignment = TextAlignmentOptions.Midline; card.energy.enableAutoSizing = true; card.energy.fontSizeMin = 18; card.energy.fontSizeMax = 24;
        card.requirements = WeaponLabel(card.rect, "SkillRequirements", 24, string.Empty, false);
        card.requirements.enableWordWrapping = true; ResourceKeywordText.Attach(card.requirements);
        card.descriptionClip = DetailRect(card.rect, "SkillDescriptionClip"); card.descriptionClip.gameObject.AddComponent<RectMask2D>();
        card.descriptionGroup = card.descriptionClip.gameObject.AddComponent<CanvasGroup>();
        card.description = WeaponLabel(card.descriptionClip, "SkillDescription", 22, string.Empty, false);
        card.description.enableWordWrapping = true; card.description.color = new Color32(172, 190, 166, 255);
        return card;
    }

    private WeaponDieRow BuildWeaponDieRow(WeaponSkillCard card, int index)
    {
        var row = new WeaponDieRow { header = WeaponBadge(card.rect, "DieHeader_" + index) };
        row.heading = WeaponLabel(row.header, "DieHeading", 25, string.Empty, false);
        row.heading.alignment = TextAlignmentOptions.MidlineLeft; row.heading.enableAutoSizing = true; row.heading.fontSizeMin = 18; row.heading.fontSizeMax = 25;
        RectTransform icon = DetailRect(row.header, "DieIcon");
        row.icon = icon.gameObject.AddComponent<BattleSkillFrameGraphic>(); row.icon.skillStyle = BattleSkillFrameGraphic.SkillStyle.Die; row.icon.raycastTarget = false;
        row.range = WeaponLabel(row.header, "DieRange", 34, string.Empty, false);
        row.range.alignment = TextAlignmentOptions.MidlineRight; row.range.enableAutoSizing = true; row.range.fontSizeMin = 22; row.range.fontSizeMax = 34;
        row.rules = WeaponLabel(card.rect, "DieRules_" + index, 24, string.Empty, false);
        row.rules.enableWordWrapping = true; row.rules.paragraphSpacing = 4;
        ResourceKeywordText.Attach(row.rules);
        return row;
    }

    private void PopulateWeaponSkillCard(WeaponSkillCard card, EnemySkill_SO skill, Weapon_SO weapon, bool resetDescription)
    {
        bool fallback = skill == null;
        card.name.text = fallback ? WeaponText("普通攻击", "Basic Attack") : skill.LocalizedName;
        card.energy.text = GameText.Format("battle.skill.energy_cost", "能耗 {cost}", ("cost", fallback ? 0 : Mathf.Max(0, skill.energyCost)));
        card.defaultBadge.gameObject.SetActive(!fallback && skill == weapon.GetDefaultSkill());
        card.requirements.text = fallback ? WeaponText("未配置技能时使用武器的基础攻击骰", "Uses the weapon's fallback die when no skills are configured.")
            : encyclopediaSkillFormatter.DescribeItemSkillRequirements(skill);
        card.requirements.gameObject.SetActive(!string.IsNullOrWhiteSpace(card.requirements.text));
        card.description.text = fallback ? string.Empty : skill.LocalizedDescription;
        card.description.gameObject.SetActive(!string.IsNullOrWhiteSpace(card.description.text));
        card.diceCount = fallback ? 1 : Mathf.Max(0, skill.diceCount);
        while (card.dice.Count < card.diceCount) card.dice.Add(BuildWeaponDieRow(card, card.dice.Count));
        if (!fallback && card.diceCount == 0)
        {
            card.requirements.text = string.IsNullOrWhiteSpace(card.requirements.text) ? WeaponText("无骰子", "No dice")
                : card.requirements.text + "\n" + WeaponText("无骰子", "No dice");
            card.requirements.gameObject.SetActive(true);
        }
        bool hasRules = false;
        for (int i = 0; i < card.dice.Count; i++)
        {
            WeaponDieRow row = card.dice[i]; bool show = i < card.diceCount;
            row.header.gameObject.SetActive(show); row.rules.gameObject.SetActive(show);
            if (!show) continue;
            bool defense = !fallback && skill.GetDiceSkillCategory(i) == EnemySkill_SO.SkillCategory.Defense;
            row.heading.text = fallback ? WeaponText("第1攻击骰", "Attack Die 1") : encyclopediaSkillFormatter.DescribeItemDiceHeading(skill, i);
            EnemySkill_SO.DicePowerRange range = fallback ? new EnemySkill_SO.DicePowerRange { minPower = weapon.minPower, maxPower = weapon.maxPower } : skill.GetDicePowerRange(i);
            row.range.text = range.minPower + " ~ " + range.maxPower;
            row.range.color = defense ? new Color32(110, 187, 248, 255) : new Color32(255, 107, 101, 255);
            row.icon.defense = defense; row.icon.SetVerticesDirty();
            row.rules.text = fallback ? string.Empty : encyclopediaSkillFormatter.DescribeItemDiceRules(skill, i);
            hasRules |= !string.IsNullOrWhiteSpace(row.rules.text);
            if (string.IsNullOrWhiteSpace(row.rules.text)) row.rules.text = GameText.Get("battle.skill.no_effects", "词条：<noparse>无</noparse>");
        }
        if (resetDescription)
        {
            card.descriptionExpanded = !hasRules;
            card.descriptionReveal = card.descriptionExpanded ? 1 : 0;
        }
    }

    private void ApplyWeaponFonts()
    {
        if (weaponRoot == null) return;
        ApplyFont(weaponSkillsTitle, detailHeadingFont); ApplyFont(weaponBasis, detailHeadingFont);
        ApplyFont(weaponSpeedText, detailHeadingFont); ApplyFont(weaponCountText, detailHeadingFont); ApplyFont(weaponReadingHint, detailBodyFont);
        foreach (WeaponSkillCard card in weaponCards)
        {
            ApplyFont(card.name, detailHeadingFont); ApplyFont(card.defaultLabel, detailHeadingFont);
            ApplyFont(card.energy, detailBodyFont); ApplyFont(card.requirements, detailBodyFont); ApplyFont(card.description, detailBodyFont);
            foreach (WeaponDieRow row in card.dice) { ApplyFont(row.heading, detailBodyFont); ApplyFont(row.range, detailBodyFont); ApplyFont(row.rules, detailBodyFont); }
        }
        if (UsesWeaponLayout) ApplyFont(titleText, detailHeadingFont);
    }

    private void RefreshWeaponLayout(bool reset)
    {
        if (weaponRoot == null) return;
        float width = detailRect.rect.width, height = detailRect.rect.height;
        bool compact = width < 1160;
        float footer = height - (compact ? 150 : 126);
        float profileWidth = compact ? width - 88 : Mathf.Clamp(width * .235f, 300, 456);
        float rightX = compact ? 44 : profileWidth + 86;
        float rightWidth = width - rightX - 44;
        float headerY = compact ? 350 : 76;
        PlaceDetail(titlePlaque, (width - 600) * .5f, -60, 600, 150);
        StretchDetail(headingText.rectTransform, 64, 20);
        PlaceDetail((RectTransform)closeXButton.transform, width - 90, 10, 72, 72);
        PlaceDetail(weaponSkillsHeader, rightX, headerY, rightWidth, 54);
        PlaceDetail(weaponSkillsTitle.rectTransform, 0, 0, rightWidth * .65f, 50);
        PlaceDetail(weaponBasis.rectTransform, rightWidth * .65f, 6, rightWidth * .35f, 40);
        PlaceDetail(weaponSkillsRule, rightX, headerY + 56, rightWidth, 4);
        PlaceDetail(weaponScrollRoot, rightX, headerY + 76, rightWidth, Mathf.Max(160, footer - headerY - 114));
        StretchDetail(weaponViewport, 0, 0); weaponViewport.offsetMax = new Vector2(-24, 0);
        PlaceDetail((RectTransform)weaponScrollbar.transform, rightWidth - 13, 0, 9, weaponScrollRoot.rect.height);
        PlaceDetail(weaponReadingHint.rectTransform, rightX, footer - 32, rightWidth - 22, 24);
        PlaceDetail(footerRule, 44, footer, width - 88, 4);
        float buttonWidth = compact ? 260 : 310;
        float buttonY = footer + (compact ? 48 : 28);
        PlaceDetail((RectTransform)closeButton.transform, width * .5f - buttonWidth * .5f, buttonY, buttonWidth, 66);
        PlaceDetail((RectTransform)actionButton.transform, width - buttonWidth - 44, buttonY, buttonWidth, 66);
        if (compact && actionButton.gameObject.activeSelf)
            PlaceDetail((RectTransform)closeButton.transform, 44, buttonY, buttonWidth, 66);
        actionStatusText.gameObject.SetActive(!string.IsNullOrWhiteSpace(actionStatusText.text));
        PlaceDetail(actionStatusText.rectTransform, 44, compact ? footer + 5 : footer + 27, compact ? width - 88 : Mathf.Max(240, profileWidth), compact ? 32 : 42);
        actionStatusText.alignment = TextAlignmentOptions.MidlineLeft;
        weaponEquipFace.SetState(false, actionButton.interactable);
        weaponEquipFace.color = actionButton.interactable ? Color.white : new Color(.56f, .57f, .49f, 1);
        actionLabel.color = actionButton.interactable ? new Color32(50, 31, 10, 255) : new Color32(113, 106, 84, 255);
        closeLabel.color = DetailIvory;
        titleText.alignment = compact ? TextAlignmentOptions.MidlineLeft : TextAlignmentOptions.Midline;
        PlaceDetail(titleText.rectTransform, compact ? 252 : 44, 76, compact ? width - 296 : profileWidth, compact ? 76 : 80);
        float badgeY = compact ? 164 : 164;
        float badgesX = compact ? 252 : 44 + (profileWidth - 240) * .5f;
        PlaceDetail(rarityFace.rectTransform, badgesX, badgeY, 128, 40);
        PlaceDetail(categoryFace.rectTransform, badgesX + 142, badgeY, 98, 40);
        PlaceDetail(nameRule, compact ? 252 : 44, 216, compact ? width - 296 : profileWidth, 3);
        float artTop = compact ? 96 : 230;
        float flavorTop = compact ? 230 : footer - 128;
        float artHeight = compact ? 210 : Mathf.Max(240, flavorTop - artTop - 18);
        PlaceDetail(displayEngraving.rectTransform, compact ? 36 : 44 + 80, artTop, compact ? 194 : profileWidth - 64, compact ? 194 : artHeight);
        PlaceDetail(iconImage.rectTransform, compact ? 56 : 44 + 116, artTop, compact ? 154 : profileWidth - 104, artHeight);
        iconRestPosition = iconImage.rectTransform.anchoredPosition;
        PlaceDetail(weaponSpeedBadge, compact ? width - 294 : 52, compact ? 220 : artTop + 54, 108, 76);
        PlaceDetail(weaponCountBadge, compact ? width - 170 : 52, compact ? 220 : artTop + 148, 108, 76);
        PlaceDetail((RectTransform)quantityText.transform.parent, compact ? 44 : 52, compact ? 304 : flavorTop - 52, 156, 38);
        columnRule.gameObject.SetActive(!compact); PlaceDetail(columnRule, profileWidth + 62, 76, 5, footer - 98);
        float flavorWidth = compact ? Mathf.Max(200, width - 572) : profileWidth;
        PlaceDetail(descriptionWell, compact ? 252 : 44, flavorTop, flavorWidth, compact ? 94 : 110);
        PlaceDetail((RectTransform)descriptionScroll.transform, 0, 0, flavorWidth, compact ? 94 : 110);
        StretchDetail(descriptionScroll.viewport); descriptionScroll.viewport.offsetMax = new Vector2(-12, 0);
        descriptionText.fontSize = MainMenuSettingsPersistence.DetailFontSize(24);
        float descriptionHeight = PreferredDetailHeight(descriptionText, flavorWidth - 16);
        PlaceDetail(descriptionText.rectTransform, 0, 0, flavorWidth - 16, descriptionHeight);
        descriptionScroll.content.sizeDelta = new Vector2(0, descriptionHeight);
        PlaceDetail((RectTransform)descriptionScrollbar.transform, flavorWidth - 7, 0, 5, compact ? 94 : 110);
        descriptionScrollbar.gameObject.SetActive(descriptionHeight > (compact ? 94 : 110) + 1);
        descriptionScroll.vertical = descriptionScrollbar.gameObject.activeSelf;
        readingHint.gameObject.SetActive(false); extraHeading.gameObject.SetActive(false);
        if (reset) { descriptionScroll.StopMovement(); descriptionScroll.content.anchoredPosition = Vector2.zero; }
        LayoutWeaponSkillCards(reset);
        hasDescriptionLayout = true; weaponLayoutDirty = false;
    }

    private void LayoutWeaponSkillCards(bool reset)
    {
        float width = weaponViewport.rect.width;
        if (width < 10) width = weaponScrollRoot.rect.width - 24;
        int activeCards = 0;
        foreach (WeaponSkillCard card in weaponCards) if (card.rect.gameObject.activeSelf) activeCards++;
        int columns = width >= 1000 && activeCards > 1 ? 2 : 1;
        const float gap = 18;
        float cardWidth = (width - gap * (columns - 1)) / columns;
        float y = 0;
        for (int i = 0; i < weaponCards.Count; i += columns)
        {
            float rowHeight = 0;
            for (int column = 0; column < columns && i + column < weaponCards.Count; column++)
            {
                WeaponSkillCard card = weaponCards[i + column];
                if (!card.rect.gameObject.activeSelf) continue;
                float cardHeight = LayoutWeaponSkillCard(card, cardWidth);
                rowHeight = Mathf.Max(rowHeight, cardHeight);
            }
            for (int column = 0; column < columns && i + column < weaponCards.Count; column++)
            {
                WeaponSkillCard card = weaponCards[i + column];
                if (card.rect.gameObject.activeSelf) PlaceDetail(card.rect, column * (cardWidth + gap), y, cardWidth, rowHeight);
            }
            if (rowHeight > 0) y += rowHeight + gap;
        }
        float contentHeight = Mathf.Max(0, y - gap);
        float oldY = weaponContent.anchoredPosition.y;
        weaponContent.sizeDelta = new Vector2(width, contentHeight);
        weaponContent.anchoredPosition = new Vector2(0, reset ? 0 : Mathf.Clamp(oldY, 0, Mathf.Max(0, contentHeight - weaponViewport.rect.height)));
        if (reset) { weaponScroll.StopMovement(); weaponScroll.verticalNormalizedPosition = 1; }
        bool overflow = contentHeight > weaponViewport.rect.height + 1;
        weaponScrollbar.interactable = overflow; weaponScroll.vertical = overflow;
        RefreshWeaponReadingHint();
    }

    private float LayoutWeaponSkillCard(WeaponSkillCard card, float width)
    {
        float bodySize = MainMenuSettingsPersistence.DetailFontSize(24);
        card.requirements.fontSize = bodySize; card.description.fontSize = MainMenuSettingsPersistence.DetailFontSize(22);
        float nameWidth = width - 194 - (card.defaultBadge.gameObject.activeSelf ? 72 : 0);
        float nameHeight = Mathf.Max(42, PreferredDetailHeight(card.name, nameWidth));
        PlaceDetail(card.name.rectTransform, 20, 14, nameWidth, nameHeight);
        PlaceDetail(card.defaultBadge, 28 + nameWidth, 18, 64, 34);
        PlaceDetail(card.energyBadge, width - 158, 16, 138, 40);
        PlaceDetail(card.energy.rectTransform, 34, 0, 100, 40);
        float y = 18 + nameHeight + 8;
        if (card.requirements.gameObject.activeSelf)
        {
            float h = PreferredDetailHeight(card.requirements, width - 40);
            PlaceDetail(card.requirements.rectTransform, 20, y, width - 40, h); y += h + 10;
        }
        for (int i = 0; i < card.diceCount; i++)
        {
            WeaponDieRow row = card.dice[i];
            PlaceDetail(row.header, 20, y, width - 40, 42);
            PlaceDetail(row.heading.rectTransform, 12, 0, width - 234, 42);
            PlaceDetail(row.icon.rectTransform, width - 212, 3, 34, 34);
            PlaceDetail(row.range.rectTransform, width - 174, 0, 116, 42);
            row.rules.fontSize = bodySize;
            float ruleHeight = PreferredDetailHeight(row.rules, width - 48);
            PlaceDetail(row.rules.rectTransform, 24, y + 49, width - 48, ruleHeight);
            y += 49 + ruleHeight + 12;
        }
        bool hasDescription = !string.IsNullOrWhiteSpace(card.description.text);
        card.descriptionClip.gameObject.SetActive(hasDescription && card.descriptionReveal > 0);
        if (hasDescription)
        {
            float h = PreferredDetailHeight(card.description, width - 48);
            float reveal = Mathf.SmoothStep(0, 1, card.descriptionReveal);
            PlaceDetail(card.descriptionClip, 24, y + 2, width - 48, h * reveal);
            PlaceDetail(card.description.rectTransform, 0, 0, width - 48, h);
            card.descriptionGroup.alpha = reveal;
            y += (h + 8) * reveal;
        }
        return y + 16;
    }

    private void RefreshWeaponReadingHint()
    {
        if (weaponReadingHint == null || weaponScroll == null) return;
        bool overflow = weaponContent.rect.height > weaponViewport.rect.height + 1;
        bool hasNotes = false;
        foreach (WeaponSkillCard card in weaponCards)
            if (card.rect.gameObject.activeSelf && !string.IsNullOrWhiteSpace(card.description.text)) { hasNotes = true; break; }
        weaponReadingHint.gameObject.SetActive(overflow || hasNotes);
        string reading = hasNotes ? WeaponText("点按技能卡片展开说明", "Tap a skill card for its description") : string.Empty;
        weaponReadingHint.text = overflow ? (weaponScroll.verticalNormalizedPosition > .015f
            ? GameText.Get("ui.scroll_more", "向下滚动查看更多") : GameText.Get("battle.skill.rules.end_of_details", "已显示全部内容"))
            + (hasNotes ? " · " + reading : string.Empty) : reading;
    }

    private void AdvanceWeaponPresentation(float delta)
    {
        if (!UsesWeaponLayout || weaponRoot == null) return;
        weaponDisplayAge += delta;
        bool changed = false;
        foreach (WeaponSkillCard card in weaponCards)
        {
            if (!card.rect.gameObject.activeSelf) continue;
            float reveal = Mathf.MoveTowards(card.descriptionReveal, card.descriptionExpanded ? 1 : 0, delta / .16f);
            if (reveal == card.descriptionReveal) continue;
            card.descriptionReveal = reveal; changed = true;
        }
        if (changed) weaponLayoutDirty = true;
        if (weaponLayoutDirty) RefreshWeaponLayout(false);
        for (int i = 0; i < weaponCards.Count; i++)
        {
            WeaponSkillCard card = weaponCards[i];
            if (!card.rect.gameObject.activeSelf) continue;
            float progress = Mathf.Clamp01((weaponDisplayAge - Mathf.Min(i, 5) * .025f) / .18f);
            card.group.alpha = Mathf.SmoothStep(0, 1, progress);
        }
    }
}
