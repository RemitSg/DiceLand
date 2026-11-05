using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public partial class BattleSkillInfoOverlay
{
    private const int EnemyProfileBodySize = 30;
    private const int EnemyNoPassivesSize = 28;

    private RectTransform enemyProfileRoot, enemyProfileRight, enemyPortraitWell, enemyPortraitMotion;
    private RectTransform enemyIdBadge, enemyHealthBadge, enemyProfileBody, enemyProfileViewport, enemyProfileContent;
    private RectTransform enemyProfileDivider, enemyDescriptionSection, enemyPassiveSection;
    private Image enemyProfileBackdrop, enemyPortrait;
    private TextMeshProUGUI enemyProfileName, enemyProfileId, enemyProfileHealth, enemyProfileDescription;
    private TextMeshProUGUI enemyNoPassives, enemyPortraitPlaceholder, enemyScrollHint;
    private CanvasGroup enemyPortraitGroup, enemyProfileGroup;
    private ScrollRect enemyProfileScroll;
    private readonly List<EnemyPassiveView> enemyPassiveViews = new List<EnemyPassiveView>();

    private sealed class EnemyPassiveView
    {
        public RectTransform rect;
        public TextMeshProUGUI name, description;
        public BattleEnemyProfileGraphic frame;
    }

    private void BuildEnemyProfileUi()
    {
        RectTransform art = SkillRect(panelRoot, "EnemyDossierArtwork"); art.SetAsFirstSibling();
        enemyProfileBackdrop = art.gameObject.AddComponent<Image>();
        enemyProfileBackdrop.sprite = Resources.Load<Sprite>("BattleUI/EnemyDossierFrame");
        enemyProfileBackdrop.raycastTarget = false;
        enemyProfileRoot = SkillRect(panelRoot, "EnemyProfile");
        enemyPortraitWell = SkillRect(enemyProfileRoot, "PortraitWell");
        ProfileFace(enemyPortraitWell, BattleEnemyProfileGraphic.ProfileStyle.Portrait);
        enemyPortraitMotion = SkillRect(enemyPortraitWell, "PortraitReveal"); StretchSkill(enemyPortraitMotion);
        enemyPortraitGroup = enemyPortraitMotion.gameObject.AddComponent<CanvasGroup>();
        RectTransform portrait = SkillRect(enemyPortraitMotion, "EnemyPortrait"); StretchSkill(portrait, 8, 8);
        enemyPortrait = portrait.gameObject.AddComponent<Image>(); enemyPortrait.preserveAspect = true; enemyPortrait.raycastTarget = false;
        MapItemIconFit.Attach(enemyPortrait);
        enemyPortraitPlaceholder = SkillText(enemyPortraitMotion, "MissingPortrait", 28, SkillMuted, GameText.Get("battle.skill.rules.no_portrait_available", "暂无立绘"));
        StretchSkill(enemyPortraitPlaceholder.rectTransform); enemyPortraitPlaceholder.alignment = TextAlignmentOptions.Midline;
        enemyIdBadge = SkillRect(enemyProfileRoot, "EnemyIdBadge"); ProfileFace(enemyIdBadge, BattleEnemyProfileGraphic.ProfileStyle.Identifier);
        enemyProfileId = SkillText(enemyIdBadge, "EnemyId", 29, SkillIvory, string.Empty);
        StretchSkill(enemyProfileId.rectTransform, 18, 2); enemyProfileId.alignment = TextAlignmentOptions.Midline;
        ResourceKeywordText.Attach(enemyProfileId, false);
        enemyProfileDivider = SkillRect(enemyProfileRoot, "PortraitDivider"); ProfileFace(enemyProfileDivider, BattleEnemyProfileGraphic.ProfileStyle.Divider);
        enemyProfileRight = SkillRect(enemyProfileRoot, "EnemyInformation");
        enemyProfileGroup = enemyProfileRight.gameObject.AddComponent<CanvasGroup>();
        enemyProfileName = SkillText(enemyProfileRight, "EnemyName", 50, SkillIvory, string.Empty);
        enemyProfileName.enableWordWrapping = true; enemyProfileName.alignment = TextAlignmentOptions.TopLeft;
        enemyProfileName.enableAutoSizing = true; enemyProfileName.fontSizeMin = 30; enemyProfileName.fontSizeMax = 50;
        ResourceKeywordText.Attach(enemyProfileName, false);
        enemyHealthBadge = SkillRect(enemyProfileRight, "MaximumHealthBadge"); ProfileFace(enemyHealthBadge, BattleEnemyProfileGraphic.ProfileStyle.Badge);
        RectTransform heart = SkillRect(enemyHealthBadge, "Heart"); PlaceSkill(heart, 19, 10, 34, 34);
        Image healthIcon = heart.gameObject.AddComponent<Image>(); healthIcon.sprite = ResourceKeywordText.HealthIcon;
        healthIcon.preserveAspect = true; healthIcon.raycastTarget = false;
        enemyProfileHealth = SkillText(enemyHealthBadge, "MaximumHealth", 30, SkillIvory, string.Empty);
        StretchSkill(enemyProfileHealth.rectTransform); enemyProfileHealth.rectTransform.offsetMin = new Vector2(64, 2);
        enemyProfileHealth.rectTransform.offsetMax = new Vector2(-18, -2); ResourceKeywordText.Attach(enemyProfileHealth, false);
        enemyProfileBody = SkillRect(enemyProfileRight, "EnemyReadingArea");
        enemyProfileScroll = BuildSkillScroll(enemyProfileBody, "EnemyProfile", out enemyProfileViewport, out enemyProfileContent, out _);
        enemyProfileScroll.onValueChanged.AddListener(_ => UpdateEnemyScrollHint());
        enemyDescriptionSection = BuildEnemySection(enemyProfileContent, "EnemyDescriptionSection", GameText.Get("battle.enemy.description", "敌人描述"));
        enemyProfileDescription = SkillText(enemyProfileContent, "EnemyDescription", EnemyProfileBodySize, SkillIvory, string.Empty);
        enemyProfileDescription.enableWordWrapping = true; enemyProfileDescription.alignment = TextAlignmentOptions.TopLeft;
        enemyProfileDescription.lineSpacing = 5; ResourceKeywordText.Attach(enemyProfileDescription);
        enemyPassiveSection = BuildEnemySection(enemyProfileContent, "EnemyPassiveSection", GameText.Get("battle.enemy.passives", "特殊被动"));
        enemyNoPassives = SkillText(enemyProfileContent, "NoPassives", EnemyNoPassivesSize, SkillMuted, GameText.Get("battle.enemy.no_passives", "暂无特殊被动。"));
        enemyNoPassives.enableWordWrapping = true; ResourceKeywordText.Attach(enemyNoPassives, false);
        enemyScrollHint = SkillText(enemyProfileRight, "ReadingHint", 21, SkillMuted, GameText.Get("ui.scroll_more", "向下滚动查看更多"));
        enemyScrollHint.alignment = TextAlignmentOptions.MidlineRight;
        enemyProfileRoot.gameObject.SetActive(false); art.gameObject.SetActive(false);
    }

    private RectTransform BuildEnemySection(Transform parent, string name, string label)
    {
        RectTransform section = SkillRect(parent, name);
        ProfileFace(section, BattleEnemyProfileGraphic.ProfileStyle.Section);
        TextMeshProUGUI text = SkillText(section, "Label", 29, SkillGold, label);
        PlaceSkill(text.rectTransform, 26, 0, 184, 44); text.alignment = TextAlignmentOptions.Midline;
        return section;
    }

    private static BattleEnemyProfileGraphic ProfileFace(RectTransform parent, BattleEnemyProfileGraphic.ProfileStyle style)
    {
        var face = parent.gameObject.AddComponent<BattleEnemyProfileGraphic>(); face.profileStyle = style;
        face.raycastTarget = style == BattleEnemyProfileGraphic.ProfileStyle.Passive;
        return face;
    }

    private void SetEnemyProfileVisible(bool visible)
    {
        if (enemyProfileRoot == null) return;
        enemyProfileRoot.gameObject.SetActive(visible);
        enemyProfileBackdrop.gameObject.SetActive(visible && enemyProfileBackdrop.sprite != null);
        panelRoot.Find("ForestFrame").gameObject.SetActive(!visible || enemyProfileBackdrop.sprite == null);
        titlePlaque.GetComponent<MapItemDetailFrameGraphic>().enabled = !visible || enemyProfileBackdrop.sprite == null;
        subtitleText.gameObject.SetActive(!visible); contextText.gameObject.SetActive(!visible);
        panelRoot.Find("HeaderRule").gameObject.SetActive(!visible);
        detailRegion.gameObject.SetActive(!visible);
        titleText.fontSize = visible ? 46 : 42;
        if (!visible) return;
        listArea.gameObject.SetActive(false); listHeading.gameObject.SetActive(false); columnRule.gameObject.SetActive(false);
        footerHint.gameObject.SetActive(false);
    }

    private void RenderEnemyProfile(bool reset)
    {
        EnemyData_SO data = currentEnemy != null ? currentEnemy.enemyData : null;
        titleText.text = GameText.Get("battle.enemy.profile", "敌人详情");
        enemyProfileName.text = data != null && !string.IsNullOrWhiteSpace(data.enemyName) ? data.LocalizedName : GameText.Get("battle.enemy", "敌人");
        enemyProfileId.text = GameText.Format("battle.enemy.id", "编号 {id}", ("id", data != null ? data.enemyID.ToString() : "—"));
        // Runtime maximum includes encounter/relic modifiers; an uninitialised preview uses the authored maximum.
        int maximum = currentEnemy != null && currentEnemy.MaxHP > 0 ? currentEnemy.MaxHP : data != null ? data.baseHP : 0;
        enemyProfileHealth.text = GameText.Format("battle.enemy.health", "最大生命：<color=#F2DDAB>{max}</color>", ("max", maximum));
        enemyPortrait.sprite = currentEnemy != null ? currentEnemy.GetCurrentStageSprite() : null;
        enemyPortrait.enabled = enemyPortrait.sprite != null;
        enemyPortraitPlaceholder.gameObject.SetActive(enemyPortrait.sprite == null);
        enemyProfileDescription.text = data == null ? GameText.Get("battle.enemy.missing", "未找到敌人数据。")
            : string.IsNullOrWhiteSpace(data.enemyDescription) ? GameText.Get("ui.no_description", "暂无描述。") : data.LocalizedDescription;
        int index = 0;
        if (data != null && data.passiveDescriptions != null)
        {
            for (int i = 0; i < data.passiveDescriptions.Count; i++)
            {
                EnemyData_SO.PassiveDescriptionEntry entry = data.passiveDescriptions[i];
                if (string.IsNullOrWhiteSpace(entry.passiveName) && string.IsNullOrWhiteSpace(entry.description)) continue;
                EnsureEnemyPassiveView(index);
                EnemyPassiveView view = enemyPassiveViews[index++]; view.rect.gameObject.SetActive(true);
                view.name.text = data.Localize($"passive.{i}.name", entry.passiveName) ?? string.Empty;
                view.name.color = GetPassiveNameColor(entry.displayColorType);
                view.name.gameObject.SetActive(!string.IsNullOrWhiteSpace(view.name.text));
                view.frame.hasHeading = view.name.gameObject.activeSelf;
                view.description.text = FormatEnemyPassive(data.Localize($"passive.{i}.description", entry.description) ?? string.Empty);
                view.description.gameObject.SetActive(!string.IsNullOrWhiteSpace(view.description.text));
                view.frame.accent = view.name.color; view.frame.SetVerticesDirty();
            }
        }
        for (int i = index; i < enemyPassiveViews.Count; i++) enemyPassiveViews[i].rect.gameObject.SetActive(false);
        enemyNoPassives.gameObject.SetActive(index == 0);
        LayoutEnemyProfile(reset);
        if (reset) switchProgress = Application.isPlaying ? 0 : 1;
    }

    private void EnsureEnemyPassiveView(int index)
    {
        if (index < enemyPassiveViews.Count) return;
        var view = new EnemyPassiveView { rect = SkillRect(enemyProfileContent, "EnemyPassive_" + index) };
        view.frame = ProfileFace(view.rect, BattleEnemyProfileGraphic.ProfileStyle.Passive);
        view.name = SkillText(view.rect, "PassiveName", 33, SkillGold, string.Empty);
        view.name.enableWordWrapping = true; view.name.alignment = TextAlignmentOptions.TopLeft;
        view.description = SkillText(view.rect, "PassiveDescription", EnemyProfileBodySize, SkillIvory, string.Empty);
        view.description.enableWordWrapping = true; view.description.alignment = TextAlignmentOptions.TopLeft; view.description.lineSpacing = 5;
        ResourceKeywordText.Attach(view.name, false); ResourceKeywordText.Attach(view.description);
        enemyPassiveViews.Add(view);
    }

    private void LayoutEnemyProfile(bool reset)
    {
        if (enemyProfileRoot == null) return;
        float bodySize = MainMenuSettingsPersistence.DetailFontSize(EnemyProfileBodySize);
        enemyProfileDescription.fontSize = bodySize;
        enemyNoPassives.fontSize = MainMenuSettingsPersistence.DetailFontSize(EnemyNoPassivesSize);
        frameState = new SkillFrameState { width = 1240, height = 770 };
        float w = frameState.width, h = frameState.height;
        float oldPosition = enemyProfileScroll.verticalNormalizedPosition;
        panelRoot.sizeDelta = new Vector2(w, h);
        PlaceSkill(enemyProfileBackdrop.rectTransform, -44, -54, 1328, 1328 * 2f / 3f);
        PlaceSkill(titlePlaque, (w - 450) * .5f, -32, 450, 76);
        PlaceSkill((RectTransform)closeButton.transform, w - 62, -2, 62, 62);
        PlaceSkill(enemyProfileRoot, 0, 0, w, h);
        PlaceSkill(enemyPortraitWell, 38, 88, 458, 510);
        PlaceSkill(enemyIdBadge, 142, 616, 246, 52);
        PlaceSkill(enemyProfileDivider, 521, 68, 14, 594);
        float rightWidth = w - 568 - 48;
        PlaceSkill(enemyProfileRight, 568, 66, rightWidth, h - 176);
        float nameHeight = Mathf.Clamp(SkillTextHeight(enemyProfileName, rightWidth, 64), 64, 122);
        PlaceSkill(enemyProfileName.rectTransform, 0, 0, rightWidth, nameHeight);
        float badgeWidth = Mathf.Min(rightWidth, Mathf.Max(318, enemyProfileHealth.GetPreferredValues(enemyProfileHealth.text).x + 88));
        PlaceSkill(enemyHealthBadge, 0, nameHeight + 10, badgeWidth, 54);
        float top = nameHeight + 90;
        float available = h - 186 - top;
        PlaceSkill(enemyProfileBody, -10, top, rightWidth + 10, available);
        float width = rightWidth - 7, cursor = 0;
        PlaceSkill(enemyDescriptionSection, 0, cursor, width, 44); cursor += 54;
        float descriptionHeight = SkillTextHeight(enemyProfileDescription, width - 16, 40);
        PlaceSkill(enemyProfileDescription.rectTransform, 10, cursor, width - 16, descriptionHeight); cursor += descriptionHeight + 22;
        PlaceSkill(enemyPassiveSection, 0, cursor, width, 44); cursor += 58;
        if (enemyNoPassives.gameObject.activeSelf)
        {
            float height = SkillTextHeight(enemyNoPassives, width - 36, 40);
            PlaceSkill(enemyNoPassives.rectTransform, 24, cursor + 4, width - 36, height); cursor += height + 20;
        }
        foreach (EnemyPassiveView view in enemyPassiveViews)
        {
            if (!view.rect.gameObject.activeSelf) continue;
            view.description.fontSize = bodySize;
            float nameHeightInCard = view.name.gameObject.activeSelf ? SkillTextHeight(view.name, width - 76, 40) : 0;
            float textHeight = view.description.gameObject.activeSelf ? SkillTextHeight(view.description, width - 52, 38) : 0;
            float textTop = 18 + nameHeightInCard + (nameHeightInCard > 0 && textHeight > 0 ? 6 : 0);
            float height = textTop + textHeight + 22;
            PlaceSkill(view.rect, 0, cursor, width, height);
            PlaceSkill(view.name.rectTransform, 56, 16, width - 76, Mathf.Max(1, nameHeightInCard));
            PlaceSkill(view.description.rectTransform, 26, textTop, width - 52, Mathf.Max(1, textHeight));
            cursor += height + 16;
        }
        if (enemyPassiveViews.Exists(view => view.rect.gameObject.activeSelf)) cursor -= 16;
        enemyProfileContent.sizeDelta = new Vector2(0, cursor);
        bool overflow = cursor > available + 1;
        enemyProfileScroll.verticalScrollbar.gameObject.SetActive(overflow);
        PlaceSkill(enemyScrollHint.rectTransform, 0, top + available + 3, rightWidth - 5, 27);
        PlaceSkill(footerRule, 38, h - 88, w - 76, 5);
        PlaceSkill((RectTransform)footerClose.transform, (w - 290) * .5f, h - 68, 290, 56);
        Canvas.ForceUpdateCanvases();
        enemyProfileScroll.StopMovement(); enemyProfileScroll.verticalNormalizedPosition = reset ? 1 : oldPosition;
        if (!overflow) enemyProfileContent.anchoredPosition = Vector2.zero;
        UpdateEnemyScrollHint();
    }

    private void UpdateEnemyScrollHint()
    {
        if (enemyScrollHint == null || enemyProfileScroll == null) return;
        bool more = enemyProfileContent.rect.height > enemyProfileViewport.rect.height + 1;
        enemyScrollHint.gameObject.SetActive(more);
        enemyScrollHint.text = enemyProfileScroll.verticalNormalizedPosition > .015f ? GameText.Get("ui.scroll_more", "向下滚动查看更多") : GameText.Get("battle.skill.rules.end_of_details", "已显示全部内容");
    }

    private void ApplyEnemyProfileMotion(float panelEase)
    {
        if (enemyProfileRoot == null || !enemyProfileRoot.gameObject.activeSelf) return;
        float reveal = Mathf.Clamp01(openProgress * 1.3f - .12f);
        float eased = 1 - Mathf.Pow(1 - reveal, 3);
        enemyPortraitGroup.alpha = eased;
        enemyPortraitMotion.anchoredPosition = new Vector2(-12 * (1 - eased), 0);
        enemyPortraitMotion.localScale = Vector3.one * Mathf.Lerp(.98f, 1, eased);
        enemyProfileGroup.alpha = panelEase * Mathf.Lerp(.72f, 1, 1 - Mathf.Pow(1 - switchProgress, 3));
    }

    // Emphasise references, quantities and the weakness keyword without changing authored wording or rich text.
    private static string FormatEnemyPassive(string source)
    {
        StringBuilder result = new StringBuilder(source.Length + 80);
        int protectedDepth = 0;
        foreach (string part in Regex.Split(source, "(<[^>]+>)"))
        {
            if (part.StartsWith("<"))
            {
                if (part.StartsWith("<color", System.StringComparison.OrdinalIgnoreCase) || part.Equals("<noparse>", System.StringComparison.OrdinalIgnoreCase)) protectedDepth++;
                else if (part.StartsWith("</color", System.StringComparison.OrdinalIgnoreCase) || part.Equals("</noparse>", System.StringComparison.OrdinalIgnoreCase)) protectedDepth = Mathf.Max(0, protectedDepth - 1);
                result.Append(part); continue;
            }
            result.Append(protectedDepth > 0 ? part : Regex.Replace(part, @"【[^】]+】|\d+(?:\.\d+)?%?|虚弱", match =>
                "<color=#" + (match.Value.StartsWith("【") ? "63DADD" : match.Value == "虚弱" ? "C5A2D8" : "F47E73") + ">" + match.Value + "</color>"));
        }
        return result.ToString();
    }
}
