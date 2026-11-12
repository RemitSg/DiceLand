using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public partial class MapCraftingOverlay
{
    private sealed class FusionWeaponCard
    {
        public Button Button;
        public Image Icon;
        public TextMeshProUGUI Name;
        public TextMeshProUGUI Detail;
    }

    private sealed class FusionSkillRow
    {
        public EnemySkill_SO Skill;
        public Button Button;
        public MapCraftingFrameGraphic Face;
    }

    private RectTransform fusionPage, fusionPicker, fusionSkillBadge;
    private Button forgeTab, fusionTab, confirmFusion;
    private FusionWeaponCard fusionSubjectCard, fusionDonorCard;
    private Image fusionPreviewIcon;
    private TextMeshProUGUI fusionPreviewName, fusionPreviewEmpty, fusionSkillName, fusionSkillHint;
    private TextMeshProUGUI fusionRule, fusionWarning, fusionCost, fusionStatus, fusionPickerTitle, fusionPickerEmpty;
    private ScrollRect fusionSkills, fusionWeaponChoices;
    private readonly List<FusionSkillRow> fusionSkillRows = new List<FusionSkillRow>();
    private readonly List<GameObject> fusionPickerRows = new List<GameObject>();
    private float fusionSkillListWidth, fusionSkillListHeight;
    private bool showingFusion;
    private RunWeaponState fusionSubject, fusionDonor;
    private EnemySkill_SO fusionSkill;
    private string fusionFeedback;

    private static string FusionText(string chinese, string english) => GameText.LanguageCode == GameText.Chinese ? chinese : english;

    private void BuildFusionUi()
    {
        float w = panelSize.x, h = panelSize.y;
        float tabX = (w - 556) * 0.5f;
        forgeTab = FusionButton(panelRect, "ForgeTab", MapCraftingFrameGraphic.Style.ForgeTab, () => ShowFusionTab(false));
        fusionTab = FusionButton(panelRect, "FusionTab", MapCraftingFrameGraphic.Style.ForgeTab, () => ShowFusionTab(true));
        Place((RectTransform)forgeTab.transform, tabX, 94, 268, 56);
        Place((RectTransform)fusionTab.transform, tabX + 288, 94, 268, 56);
        FusionLabel(forgeTab.transform, "Label", FusionText("锻造", "Forge"), 29, Ivory, 35, 0, 198, 56);
        FusionLabel(fusionTab.transform, "Label", FusionText("融合", "Fusion"), 29, Ivory, 35, 0, 198, 56);
        Rule(panelRect, "TabsLeftRule", 34, 117, tabX - 58, 8);
        Rule(panelRect, "TabsRightRule", tabX + 580, 117, tabX - 58, 8);

        fusionPage = Rect(panelRect, "FusionPage");
        Stretch(fusionPage);
        const float leftX = 38;
        float leftWidth = w * 0.252f;
        float centerX = leftX + leftWidth + 26;
        float rightX = w * 0.677f;
        float centerWidth = rightX - centerX - 24;
        float rightWidth = w - rightX - 38;
        float bodyBottom = h - 138;
        Rule(fusionPage, "FusionLeftDivider", centerX - 15, 177, 8, bodyBottom - 177);
        Rule(fusionPage, "FusionRightDivider", rightX - 15, 177, 8, bodyBottom - 177);

        FusionLabel(fusionPage, "SubjectHeading", FusionText("主体武器", "Base Weapon"), 30, Ivory, leftX, 188, leftWidth, 52);
        fusionSubjectCard = BuildFusionWeaponCard(fusionPage, "FusionSubject", MapCraftingFrameGraphic.Style.FusionWeapon,
            leftX, 250, leftWidth, 118, () => OpenFusionPicker(false));
        Rule(fusionPage, "FusionWeaponDivider", leftX, 395, leftWidth, 8);
        FusionLabel(fusionPage, "DonorHeading", FusionText("被融合武器", "Donor Weapon"), 30, Ivory, leftX, 422, leftWidth, 52);
        fusionDonorCard = BuildFusionWeaponCard(fusionPage, "FusionDonor", MapCraftingFrameGraphic.Style.FusionWeapon,
            leftX, 484, leftWidth, 118, () => OpenFusionPicker(true));
        fusionRule = FusionLabel(fusionPage, "FusionRule", string.Empty, 23, Muted, leftX, 625, leftWidth, 56);

        RectTransform preview = Rect(fusionPage, "FusionPreview");
        Place(preview, centerX, 175, centerWidth, bodyBottom - 175);
        Face(preview, MapCraftingFrameGraphic.Style.Preview).raycastTarget = false;
        FusionLabel(preview, "PreviewHeading", FusionText("融合预览", "Fusion Preview"), 32, Ivory, 10, 5, centerWidth - 20, 53);
        RectTransform nameBadge = Rect(preview, "FusionNameBadge");
        float nameWidth = centerWidth - 80;
        Place(nameBadge, 40, 65, nameWidth, 46);
        Face(nameBadge, MapCraftingFrameGraphic.Style.Badge).raycastTarget = false;
        fusionPreviewName = FusionLabel(nameBadge, "ResultName", string.Empty, 29, Ivory, 10, 0, nameWidth - 20, 46);
        float sealHeight = bodyBottom - 175 - 188;
        RectTransform seal = Rect(preview, "FusionAnvilSeal");
        Place(seal, 22, 111, centerWidth - 44, sealHeight);
        MapCraftingFrameGraphic engravingFace = Face(seal, MapCraftingFrameGraphic.Style.Engraving);
        engravingFace.glow = 0.12f;
        engravingFace.raycastTarget = false;
        fusionPreviewIcon = Icon(preview, "FusionWeaponIcon", null);
        Place(fusionPreviewIcon.rectTransform, 25, 107, centerWidth - 50, sealHeight + 8);
        fusionPreviewEmpty = FusionLabel(preview, "EmptyFusionPreview", FusionText("选择主体武器查看预览", "Choose a base weapon to preview"),
            24, Muted, 22, 111, centerWidth - 44, sealHeight);
        fusionSkillBadge = Rect(preview, "FusionSkillBadge");
        float badgeWidth = centerWidth - 68;
        Place(fusionSkillBadge, 34, bodyBottom - 175 - 63, badgeWidth, 54);
        Face(fusionSkillBadge, MapCraftingFrameGraphic.Style.Badge).raycastTarget = false;
        FusionLabel(fusionSkillBadge, "NewSkillLabel", FusionText("新增技能", "New Skill"), 23, Jade, 12, 0, 116, 54);
        fusionSkillName = FusionLabel(fusionSkillBadge, "NewSkillName", string.Empty, 29, Ivory, 136, 0, badgeWidth - 148, 54);

        FusionLabel(fusionPage, "SkillsHeading", FusionText("选择继承技能", "Choose a Skill to Inherit"), 30, Ivory,
            rightX, 178, rightWidth, 50);
        fusionSkillHint = FusionLabel(fusionPage, "SkillSource", string.Empty, 23, Muted, rightX + 6, 230, rightWidth - 12, 46);
        fusionSkills = ScrollArea(fusionPage, "FusionSkillList");
        fusionSkillListHeight = bodyBottom - 282;
        fusionSkillListWidth = rightWidth - 18;
        Place((RectTransform)fusionSkills.transform, rightX, 282, rightWidth, fusionSkillListHeight);

        FusionLabel(fusionPage, "WarningMark", "!", 32, Coral, 39, h - 96, 30, 38);
        fusionWarning = FusionLabel(fusionPage, "FusionWarning", string.Empty, 25, Coral, 76, h - 96, w - 647, 39, TextAlignmentOptions.MidlineLeft);
        fusionStatus = FusionLabel(fusionPage, "FusionStatus", string.Empty, 23, Jade, 76, h - 55, w - 647, 37, TextAlignmentOptions.MidlineLeft);
        Image coins = Icon(fusionPage, "FusionCoins", MapForestArt.Get("Coins"));
        Place(coins.rectTransform, w - 555, h - 82, 52, 50);
        string price = RunWeaponFusionService.Cost + FusionText(" 金币", " Gold");
        fusionCost = FusionLabel(fusionPage, "FusionCost", price, 31, Ivory,
            w - 490, h - 92, 170, 70, TextAlignmentOptions.MidlineLeft);
        confirmFusion = Action(fusionPage, "ConfirmFusion", FusionText("确认融合", "Confirm Fusion"), ConfirmFusion, true);
        Place((RectTransform)confirmFusion.transform, w - 302, h - 92, 262, 70);
        TMP_Text confirmLabel = confirmFusion.GetComponentInChildren<TMP_Text>();
        confirmLabel.enableAutoSizing = true;
        confirmLabel.fontSizeMin = 22;
        confirmLabel.fontSizeMax = 34;

        BuildFusionPicker();
        fusionPage.gameObject.SetActive(false);
    }

    private void BuildFusionPicker()
    {
        fusionPicker = Rect(fusionPage, "FusionWeaponPicker");
        Stretch(fusionPicker);
        Image shade = fusionPicker.gameObject.AddComponent<Image>();
        shade.color = new Color(0.01f, 0.02f, 0.015f, 0.8f);
        Button dismiss = fusionPicker.gameObject.AddComponent<Button>();
        dismiss.targetGraphic = shade;
        dismiss.transition = Selectable.Transition.None;
        dismiss.navigation = new Navigation { mode = Navigation.Mode.None };
        dismiss.onClick.AddListener(CloseFusionPicker);

        RectTransform window = Rect(fusionPicker, "PickerWindow");
        Place(window, (panelSize.x - 678) * 0.5f, 141, 678, 552);
        Face(window, MapCraftingFrameGraphic.Style.Window);
        fusionPickerTitle = FusionLabel(window, "PickerTitle", string.Empty, 34, Ivory, 52, 20, 562, 52);
        Button close = Action(window, "ClosePicker", "×", CloseFusionPicker, false);
        Place((RectTransform)close.transform, 602, 18, 58, 58);
        Rule(window, "PickerRule", 30, 79, 618, 8);
        fusionWeaponChoices = ScrollArea(window, "PickerList");
        Place((RectTransform)fusionWeaponChoices.transform, 30, 94, 618, 423);
        fusionPickerEmpty = FusionLabel(window, "PickerEmpty", string.Empty, 27, Muted, 46, 142, 586, 248);
        fusionPicker.gameObject.SetActive(false);
    }

    private Button FusionButton(Transform parent, string name, MapCraftingFrameGraphic.Style style, UnityAction action)
    {
        RectTransform rect = Rect(parent, name);
        rect.pivot = Vector2.one * 0.5f;
        MapCraftingFrameGraphic face = Face(rect, style);
        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = face;
        button.transition = Selectable.Transition.None;
        button.onClick.AddListener(action);
        rect.gameObject.AddComponent<MapCraftingButtonFeedback>();
        return button;
    }

    private TextMeshProUGUI FusionLabel(Transform parent, string name, string value, int size, Color color,
        float x, float y, float w, float h, TextAlignmentOptions alignment = TextAlignmentOptions.Midline)
    {
        TextMeshProUGUI label = Text(parent, name, size, color, value);
        label.alignment = alignment;
        label.enableAutoSizing = true;
        label.fontSizeMin = Mathf.Min(18, size);
        label.fontSizeMax = size;
        Place(label.rectTransform, x, y, w, h);
        return label;
    }

    private FusionWeaponCard BuildFusionWeaponCard(Transform parent, string name, MapCraftingFrameGraphic.Style style,
        float x, float y, float w, float h, UnityAction action)
    {
        Button button = FusionButton(parent, name, style, action);
        Place((RectTransform)button.transform, x, y, w, h);
        float iconSize = h - 26;
        RectTransform well = Rect(button.transform, "IconWell");
        Face(well, MapCraftingFrameGraphic.Style.IconWell).raycastTarget = false;
        Place(well, 13, 13, iconSize, iconSize);
        Image icon = Icon(well, "Icon", null);
        Inset(icon.rectTransform, 5, 5);
        float textX = iconSize + 28;
        TextMeshProUGUI title = FusionLabel(button.transform, "WeaponName", string.Empty, 29, Ivory,
            textX, 9, w - textX - 49, h * 0.46f, TextAlignmentOptions.MidlineLeft);
        TextMeshProUGUI detail = FusionLabel(button.transform, "WeaponDetails", string.Empty, 22, Muted,
            textX, h * 0.51f, w - textX - 49, h * 0.37f, TextAlignmentOptions.MidlineLeft);
        return new FusionWeaponCard { Button = button, Icon = icon, Name = title, Detail = detail };
    }

    private void ResetFusion()
    {
        fusionSubject = null;
        fusionDonor = null;
        fusionSkill = null;
        fusionFeedback = null;
        CloseFusionPicker();
        RebuildFusionSkills();
    }

    private void ShowFusionTab(bool show)
    {
        if (!IsVisible || closing || handlingAction) return;
        CloseFusionPicker();
        showingFusion = show;
        forgePage.gameObject.SetActive(!show);
        fusionPage.gameObject.SetActive(show);
        forgeTab.GetComponent<MapCraftingFrameGraphic>().SetState(!show, true);
        fusionTab.GetComponent<MapCraftingFrameGraphic>().SetState(show, true);
        if (show)
        {
            if (!RunWeaponFusionService.Owns(currentRunState, fusionSubject)) fusionSubject = null;
            if (!RunWeaponFusionService.CanPair(currentRunState, fusionSubject, fusionDonor))
            {
                fusionDonor = null;
                fusionSkill = null;
            }
            RebuildFusionSkills();
            RefreshFusion();
        }
        else RefreshAvailability();
    }

    private void OpenFusionPicker(bool choosingDonor)
    {
        if (!showingFusion || closing || handlingAction || currentRunState == null) return;
        if (choosingDonor && !CanChooseFusionDonor()) return;
        CloseFusionPicker();
        fusionPickerTitle.text = choosingDonor ? FusionText("选择被融合武器", "Choose Donor Weapon") : FusionText("选择主体武器", "Choose Base Weapon");
        if (currentRunState.weaponInventory != null)
        {
            foreach (RunWeaponState entry in currentRunState.weaponInventory)
            {
                if (!RunWeaponFusionService.Owns(currentRunState, entry) ||
                    (choosingDonor && !RunWeaponFusionService.CanPair(currentRunState, fusionSubject, entry))) continue;
                AddFusionPickerRow(entry, choosingDonor);
            }
        }
        bool empty = fusionPickerRows.Count == 0;
        fusionPickerEmpty.text = choosingDonor
            ? FusionText("没有另一把同类型的武器", "No other weapon of this type")
            : FusionText("背包里没有武器", "No weapons in your pack");
        fusionPickerEmpty.gameObject.SetActive(empty);
        fusionWeaponChoices.content.sizeDelta = new Vector2(0, Mathf.Max(423, fusionPickerRows.Count * 100));
        fusionPicker.gameObject.SetActive(true);
        ResetScroll(fusionWeaponChoices);
        UpdateScrollbar(fusionWeaponChoices);
    }

    private void AddFusionPickerRow(RunWeaponState entry, bool choosingDonor)
    {
        int index = fusionPickerRows.Count;
        FusionWeaponCard row = BuildFusionWeaponCard(fusionWeaponChoices.content, $"PickerWeapon_{index:00}",
            MapCraftingFrameGraphic.Style.Blueprint, 2, index * 100 + 2, 598, 90, () => SelectFusionWeapon(entry, choosingDonor));
        Weapon_SO weapon = entry.weaponData;
        bool selected = entry == (choosingDonor ? fusionDonor : fusionSubject);
        bool available = choosingDonor || weapon.FusionCount < weapon.FusionLimit;
        row.Button.interactable = available;
        row.Button.GetComponent<MapCraftingFrameGraphic>().SetState(selected && available, available);
        row.Name.text = entry.DisplayName;
        row.Name.color = selected && available ? Ink : available ? Ivory : Muted;
        row.Detail.color = selected && available ? Ink : Muted;
        row.Detail.text = FusionWeaponType(weapon) + $" · ×{entry.amount}";
        RunPartyMemberState owner = currentRunState.GetEquippedMember(weapon);
        if (owner != null) row.Detail.text += " · " + owner.DisplayName;
        if (!available) row.Detail.text += " · " + FusionText("已达融合上限", "Fusion limit reached");
        else if (!choosingDonor && weapon.FusionLimit > 1) row.Detail.text += $" · {weapon.FusionCount}/{weapon.FusionLimit}";
        if (weapon.InheritedSkills.Count > 0)
            row.Detail.text += " · " + string.Join(" / ", weapon.InheritedSkills.Select(skill => skill.Skill.LocalizedName));
        SetIcon(row.Icon, weapon.icon);
        fusionPickerRows.Add(row.Button.gameObject);
    }

    private void CloseFusionPicker()
    {
        if (fusionPicker != null) fusionPicker.gameObject.SetActive(false);
        foreach (GameObject row in fusionPickerRows) RemoveUi(row);
        fusionPickerRows.Clear();
    }

    private void SelectFusionWeapon(RunWeaponState weapon, bool choosingDonor)
    {
        if (!fusionPicker.gameObject.activeSelf || !RunWeaponFusionService.Owns(currentRunState, weapon)) return;
        if (choosingDonor)
        {
            if (!CanChooseFusionDonor() || !RunWeaponFusionService.CanPair(currentRunState, fusionSubject, weapon)) return;
            fusionDonor = weapon;
        }
        else
        {
            if (weapon.weaponData.FusionCount >= weapon.weaponData.FusionLimit) return;
            fusionSubject = weapon;
            if (!RunWeaponFusionService.CanPair(currentRunState, fusionSubject, fusionDonor)) fusionDonor = null;
        }
        fusionSkill = null;
        fusionFeedback = null;
        CloseFusionPicker();
        RebuildFusionSkills();
        RefreshFusion();
    }

    private void RebuildFusionSkills()
    {
        foreach (FusionSkillRow row in fusionSkillRows) RemoveUi(row.Button.gameObject);
        fusionSkillRows.Clear();
        EnemySkill_SO[] skills = fusionDonor?.weaponData != null ? fusionDonor.weaponData.skillPool : null;
        if (skills != null)
        {
            var seen = new HashSet<EnemySkill_SO>();
            foreach (EnemySkill_SO skill in skills)
                if (skill != null && seen.Add(skill)) AddFusionSkillRow(skill);
        }
        fusionSkills.content.sizeDelta = new Vector2(0, Mathf.Max(fusionSkillListHeight, fusionSkillRows.Count * 123 - 8));
        ResetScroll(fusionSkills);
        UpdateScrollbar(fusionSkills);
    }

    private void AddFusionSkillRow(EnemySkill_SO skill)
    {
        int index = fusionSkillRows.Count;
        bool known = RunWeaponFusionService.HasSkill(fusionSubject?.weaponData, skill);
        Button button = FusionButton(fusionSkills.content, $"FusionSkill_{index:00}", MapCraftingFrameGraphic.Style.FusionSkill,
            () => SelectFusionSkill(skill));
        button.interactable = !known;
        Place((RectTransform)button.transform, 2, index * 123 + 2, fusionSkillListWidth, 111);
        RectTransform well = Rect(button.transform, "SkillIconWell");
        Place(well, 10, 10, 88, 91);
        Face(well, MapCraftingFrameGraphic.Style.IconWell).raycastTarget = false;
        Image icon = Icon(well, "SkillIcon", FusionSkillIcon(skill));
        Inset(icon.rectTransform, 12, 12);
        icon.color = known ? Muted : Color.white;
        float textWidth = fusionSkillListWidth - 162;
        FusionLabel(button.transform, "SkillName", skill.LocalizedName, 29, known ? Muted : Ivory,
            112, 8, textWidth, 42, TextAlignmentOptions.MidlineLeft);
        FusionLabel(button.transform, "SkillStats", known ? FusionText("主体已拥有", "Already on base weapon") : FusionSkillStats(skill),
            22, Muted, 112, 51, textWidth, 53, TextAlignmentOptions.MidlineLeft);
        fusionSkillRows.Add(new FusionSkillRow { Skill = skill, Button = button, Face = button.GetComponent<MapCraftingFrameGraphic>() });
    }

    private void SelectFusionSkill(EnemySkill_SO skill)
    {
        if (!showingFusion || closing || handlingAction || fusionPicker.gameObject.activeSelf ||
            !RunWeaponFusionService.HasSkill(fusionDonor?.weaponData, skill) ||
            RunWeaponFusionService.HasSkill(fusionSubject?.weaponData, skill)) return;
        fusionSkill = skill;
        fusionFeedback = null;
        RefreshFusion();
    }

    private static Sprite FusionSkillIcon(EnemySkill_SO skill)
    {
        for (int i = 0; i < skill.diceCount; i++)
        {
            EnemySkill_SO.StatusEffectEntry[] effects = skill.GetDiceEffectSettings(i).statusEffects;
            if (effects == null) continue;
            foreach (EnemySkill_SO.StatusEffectEntry effect in effects)
                if (effect.statusData != null && effect.statusData.IconSprite != null) return effect.statusData.IconSprite;
        }
        return MapForestArt.Get("Sword");
    }

    private static string FusionSkillStats(EnemySkill_SO skill)
    {
        var stats = new List<string>();
        for (int i = 0; i < skill.diceCount; i++)
        {
            EnemySkill_SO.DicePowerRange range = skill.GetDicePowerRange(i);
            string category = skill.GetDiceSkillCategory(i) == EnemySkill_SO.SkillCategory.Defense
                ? FusionText("防御", "DEF") : FusionText("攻击", "ATK");
            string dice = range.minPower == range.maxPower ? range.minPower.ToString() : $"{range.minPower}–{range.maxPower}";
            stats.Add(category + " " + dice);
        }
        stats.Add(FusionText("能耗 ", "Energy ") + skill.energyCost);
        return string.Join(" · ", stats);
    }

    private static string FusionWeaponType(Weapon_SO weapon)
        => weapon.weaponType == WeaponType.Sword ? FusionText("单手剑", "Sword") : GetWeaponTypeLabel(weapon.weaponType);

    private bool CanChooseFusionDonor()
        => RunWeaponFusionService.Owns(currentRunState, fusionSubject) && fusionSubject.weaponData.FusionCount < fusionSubject.weaponData.FusionLimit;

    private void RefreshFusion()
    {
        Weapon_SO subject = fusionSubject?.weaponData;
        Weapon_SO donor = fusionDonor?.weaponData;
        UpdateFusionCard(fusionSubjectCard, subject, FusionText("选择主体武器", "Choose a base weapon"), currentRunState != null);
        UpdateFusionCard(fusionDonorCard, donor, FusionText("选择被融合武器", "Choose a donor weapon"), CanChooseFusionDonor());
        fusionRule.text = subject == null ? FusionText("仅限同类型", "Matching weapon types only") :
            FusionText("仅限同类型 · 融合 ", "Same type · Fusions ") + $"{subject.FusionCount}/{subject.FusionLimit}";
        fusionPreviewName.text = subject != null ? subject.FusedDisplayName : string.Empty;
        fusionPreviewName.transform.parent.gameObject.SetActive(subject != null);
        fusionPreviewEmpty.gameObject.SetActive(subject == null);
        SetIcon(fusionPreviewIcon, subject != null ? subject.icon : null);
        fusionSkillBadge.gameObject.SetActive(fusionSkill != null);
        fusionSkillName.text = fusionSkill != null ? fusionSkill.LocalizedName : string.Empty;
        fusionSkillHint.text = donor == null
            ? FusionText("选择被融合武器后可挑选技能", "Choose a donor to see its skills")
            : FusionText($"从{donor.LocalizedName}选择 1 个技能", $"Choose 1 skill from {donor.LocalizedName}");
        if (donor != null && fusionSkillRows.Count == 0)
            fusionSkillHint.text = FusionText("这把武器没有可继承的技能", "This weapon has no skills to inherit");
        else if (donor != null && fusionSkillRows.TrueForAll(row => !row.Button.interactable))
            fusionSkillHint.text = FusionText("主体武器已拥有这些技能", "The base weapon already has these skills");
        foreach (FusionSkillRow row in fusionSkillRows)
            row.Face.SetState(row.Skill == fusionSkill, row.Button.interactable);

        RunPartyMemberState unequipped = RunWeaponFusionService.GetDonorOwnerToUnequip(currentRunState, fusionSubject, fusionDonor);
        fusionWarning.text = donor == null ? FusionText("被融合武器将被消耗，融合不可撤销", "The donor will be consumed. Fusion is permanent.") :
            unequipped != null
                ? FusionText($"{donor.LocalizedName}将从{unequipped.DisplayName}卸下并消耗，融合不可撤销",
                    $"{donor.LocalizedName} will be unequipped and consumed. Fusion is permanent.")
                : FusionText($"{donor.LocalizedName}将被消耗，融合不可撤销", $"{donor.LocalizedName} will be consumed. Fusion is permanent.");

        var availability = RunWeaponFusionService.Check(currentRunState, fusionSubject, fusionDonor, fusionSkill);
        bool enabled = availability == RunWeaponFusionService.Availability.Ready && !closing && !handlingAction;
        confirmFusion.interactable = enabled;
        confirmFusion.GetComponent<MapCraftingFrameGraphic>().SetState(false, enabled);
        confirmFusion.GetComponentInChildren<TMP_Text>().color = enabled ? Ivory : Muted;
        fusionCost.color = currentRunState != null && currentRunState.money >= RunWeaponFusionService.Cost ? Ivory : Coral;
        fusionStatus.text = fusionFeedback ?? FusionUnavailableMessage(availability);
        fusionStatus.color = fusionFeedback != null ? Jade : Coral;
    }

    private void UpdateFusionCard(FusionWeaponCard card, Weapon_SO weapon, string placeholder, bool enabled)
    {
        bool selected = weapon != null;
        card.Button.interactable = enabled;
        card.Button.GetComponent<MapCraftingFrameGraphic>().SetState(selected, enabled);
        card.Name.text = selected ? weapon.LocalizedName : placeholder;
        card.Name.color = selected ? Ink : enabled ? Ivory : Muted;
        card.Detail.text = selected ? FusionWeaponType(weapon) : string.Empty;
        card.Detail.color = selected ? new Color32(28, 83, 48, 255) : Muted;
        SetIcon(card.Icon, selected ? weapon.icon : null);
    }

    private string FusionUnavailableMessage(RunWeaponFusionService.Availability availability)
    {
        switch (availability)
        {
            case RunWeaponFusionService.Availability.FusionLimitReached:
                return FusionText("主体武器已达融合次数上限", "The base weapon has reached its fusion limit");
            case RunWeaponFusionService.Availability.InsufficientGold:
                return FusionText($"金币不足（持有 {currentRunState.money}）", $"Not enough gold (have {currentRunState.money})");
            case RunWeaponFusionService.Availability.InvalidSkill:
                return FusionText("请选择被融合武器拥有的技能", "Choose a skill on the donor weapon");
            case RunWeaponFusionService.Availability.AlreadyKnown:
                return FusionText("主体武器已拥有这个技能", "The base weapon already has this skill");
            default:
                return string.Empty;
        }
    }

    private void ConfirmFusion()
    {
        if (!IsVisible || !showingFusion || closing || handlingAction || fusionPicker.gameObject.activeSelf) return;
        handlingAction = true;
        try
        {
            EnemySkill_SO skill = fusionSkill;
            var availability = RunWeaponFusionService.TryFuse(currentRunState, fusionSubject, fusionDonor, skill, out RunWeaponState result);
            if (availability != RunWeaponFusionService.Availability.Ready) return;

            fusionSubject = result;
            fusionDonor = null;
            fusionSkill = null;
            fusionFeedback = FusionText($"{result.DisplayName}已获得「{skill.LocalizedName}」",
                $"{result.DisplayName} inherited {skill.LocalizedName}");
            RebuildFusionSkills();
            using (MapFeedbackOrigin.FromText(fusionStatus, (RectTransform)confirmFusion.transform))
                WeaponFused?.Invoke(fusionFeedback);
        }
        finally
        {
            handlingAction = false;
            RefreshFusion();
        }
    }
}
