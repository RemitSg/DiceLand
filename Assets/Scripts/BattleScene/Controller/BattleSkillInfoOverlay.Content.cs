using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public partial class BattleSkillInfoOverlay
{
    private void RenderCurrentSkill(bool reset)
    {
        EnemySkill_SO skill = viewMode == SkillViewMode.Ally ? currentAlly?.GetSelectedSkillForButton(currentAllySourceButton)
            : viewMode == SkillViewMode.EnemySkill ? currentEnemy?.GetSelectedSkillForButton(currentEnemySourceButton) : null;
        bool changed = displayedSkill != skill || reset;
        displayedSkill = skill;
        titleText.text = viewMode == SkillViewMode.Ally ? "武器技能" : viewMode == SkillViewMode.EnemyData ? "敌人详情" : "敌方技能";
        if (viewMode == SkillViewMode.Ally)
        {
            Weapon_SO weapon = currentAlly != null ? currentAlly.GetComponent<AllyController>()?.weapon?.weaponData : null;
            string source = currentAlly != null && currentAlly.TemporarySkillsOverrideWeaponPool ? "临时技能"
                : weapon != null ? weapon.weaponName : currentAlly != null && currentAlly.HasTemporarySkills ? "临时技能" : "未装备武器";
            subtitleText.text = (currentAlly != null ? currentAlly.GetSettlementDisplayName() : "角色") + " · " + source;
        }
        else subtitleText.text = currentEnemy != null && currentEnemy.enemyData != null ? currentEnemy.enemyData.enemyName : "敌人";
        contextText.text = viewMode == SkillViewMode.Ally ? "当前技能：" + (skill != null ? skill.skillName : "未配置") : "当前技能";
        skillNameText.text = viewMode == SkillViewMode.EnemyData ? "最大生命：" + (currentEnemy != null ? currentEnemy.MaxHP : 0)
            : skill != null ? skill.skillName : "未配置技能";
        metadataRoot.gameObject.SetActive(viewMode != SkillViewMode.EnemyData && skill != null);
        bool attack = false, defense = false;
        if (skill != null)
            for (int i = 0; i < skill.diceCount; i++)
                if (skill.GetDiceSkillCategory(i) == EnemySkill_SO.SkillCategory.Defense) defense = true; else attack = true;
        metadataText.text = attack && defense ? "攻防混合" : defense ? "防御" : "攻击";
        energyMetadata.text = "能耗 " + (skill != null ? Mathf.Max(0, skill.energyCost) : 0);
        diceMetadata.text = "骰子数 " + (skill != null ? Mathf.Max(0, skill.diceCount) : 0);
        requirementsText.text = BuildSkillRequirements(skill);
        bool dataMode = viewMode == SkillViewMode.EnemyData;
        detailText.gameObject.SetActive(dataMode);
        if (dataMode) detailText.text = BuildEnemyDataDetail(currentEnemy);
        descriptionHeading.gameObject.SetActive(!dataMode);
        descriptionBody.gameObject.SetActive(!dataMode);
        descriptionBody.text = skill != null && !string.IsNullOrWhiteSpace(skill.description) ? skill.description : "暂无描述。";
        int count = !dataMode && skill != null ? Mathf.Max(0, skill.diceCount) : 0;
        EnsureDiceCards(count);
        for (int i = 0; i < diceCards.Count; i++)
        {
            DiceCardView card = diceCards[i]; card.rect.gameObject.SetActive(i < count);
            if (i >= count) continue;
            card.heading.text = GetDicePrefixLabel(skill, i);
            card.range.text = BuildDiceRangeLabel(skill, i, skill.GetDicePowerRange(i), DisplayedSkillRange(skill, i));
            card.icon.defense = skill.GetDiceSkillCategory(i) == EnemySkill_SO.SkillCategory.Defense; card.icon.SetVerticesDirty();
            List<string> rules = BuildDiceRuleLines(skill, i, viewMode == SkillViewMode.Ally, false);
            card.clauses.text = rules.Count > 0 ? "<color=#E2BB65>词条</color>\n" + string.Join("\n", rules) : "词条：<noparse>无</noparse>";
        }
        UpdateDiceLinks(count > 1 ? count : 0);
        LayoutSkillContent(changed);
        if (changed) switchProgress = Application.isPlaying ? 0 : 1;
    }

    private EnemySkill_SO.DicePowerRange DisplayedSkillRange(EnemySkill_SO skill, int index)
    {
        if (viewMode == SkillViewMode.Ally && currentAlly != null) return currentAlly.GetDisplayedDicePowerRange(skill, index);
        if (currentEnemy != null) return currentEnemy.GetEffectiveDisplayedDicePowerRange(skill, index);
        return skill.GetDicePowerRange(index);
    }

    private string BuildSkillRequirements(EnemySkill_SO skill)
    {
        if (skill == null) return string.Empty;
        string text = BuildTargetPrioritySummary(skill).Trim();
        if (viewMode == SkillViewMode.Ally && skill.RequiresSelfStatusToUse && skill.requiredSelfStatusData != null)
        {
            int need = Mathf.Max(1, skill.requiredSelfStatusStacks);
            string requirement = $"施放条件：自身至少拥有 {need} 层 {skill.requiredSelfStatusData.DisplayName}（当前 {currentAlly.GetStatusStacks(skill.requiredSelfStatusData)} 层）"
                + (skill.keepRequiredSelfStatusAfterUse ? "，施放后不消耗" : "，施放后消耗");
            text = string.IsNullOrEmpty(text) ? requirement : requirement + "\n" + text;
        }
        return text;
    }

    private void EnsureDiceCards(int count)
    {
        while (diceCards.Count < count)
        {
            RectTransform rect = SkillRect(detailContent, "DiceCard_" + diceCards.Count);
            SkillFace(rect, MapCraftingFrameGraphic.Style.Material);
            var card = new DiceCardView { rect = rect };
            card.heading = SkillText(rect, "DiceHeading", 28, SkillIvory, string.Empty);
            card.range = SkillText(rect, "DiceRange", 34, SkillIvory, string.Empty); card.range.alignment = TextAlignmentOptions.MidlineRight;
            RectTransform icon = SkillRect(rect, "DieIcon");
            card.icon = icon.gameObject.AddComponent<BattleSkillFrameGraphic>(); card.icon.skillStyle = BattleSkillFrameGraphic.SkillStyle.Die; card.icon.raycastTarget = false;
            card.rule = SkillRect(rect, "Rule"); AddSkillRule(card.rule);
            card.clauses = SkillText(rect, "DiceClauses", 26, SkillIvory, string.Empty);
            card.clauses.enableWordWrapping = true; card.clauses.alignment = TextAlignmentOptions.TopLeft; card.clauses.paragraphSpacing = 7;
            diceCards.Add(card);
        }
    }

    private void UpdateDiceLinks(int count)
    {
        while (diceLinks.Count < count)
        {
            int index = diceLinks.Count;
            RectTransform rect = SkillRect(summaryRoot, "DiceLink_" + index);
            var face = rect.gameObject.AddComponent<BattleSkillFrameGraphic>(); face.skillStyle = BattleSkillFrameGraphic.SkillStyle.Choice; face.raycastTarget = true;
            Button link = rect.gameObject.AddComponent<Button>(); link.targetGraphic = face; link.transition = Selectable.Transition.None;
            Navigation nav = link.navigation; nav.mode = Navigation.Mode.None; link.navigation = nav;
            link.onClick.AddListener(() => ScrollToDie(index)); rect.gameObject.AddComponent<MapCraftingButtonFeedback>();
            TextMeshProUGUI label = SkillText(rect, "Label", 23, SkillIvory, string.Empty);
            StretchSkill(label.rectTransform, 10, 0); label.alignment = TextAlignmentOptions.Midline;
            diceLinks.Add(link); diceLinkLabels.Add(label);
        }
        for (int i = 0; i < diceLinks.Count; i++)
        {
            diceLinks[i].gameObject.SetActive(i < count);
            if (i < count)
            {
                bool defense = displayedSkill.GetDiceSkillCategory(i) == EnemySkill_SO.SkillCategory.Defense;
                diceLinkLabels[i].text = (i + 1) + " · " + (defense ? "防御 " : "攻击 ")
                    + BuildDiceRangeLabel(displayedSkill, i, displayedSkill.GetDicePowerRange(i), DisplayedSkillRange(displayedSkill, i));
            }
        }
        summaryRoot.gameObject.SetActive(count > 0);
    }

    private void LayoutSkillContent(bool reset)
    {
        if (detailRegion == null || frameState.height < 1) return;
        float oldPosition = detailScrollRect != null ? detailScrollRect.verticalNormalizedPosition : 1;
        bool dataMode = viewMode == SkillViewMode.EnemyData;
        float nameHeight = Mathf.Clamp(SkillTextHeight(skillNameText, detailWidth, 54), 54, 100);
        PlaceSkill(skillNameText.rectTransform, 0, 0, detailWidth, nameHeight);
        float y = nameHeight + 12;
        if (!dataMode && displayedSkill != null)
        {
            PlaceSkill(metadataRoot, 0, y, detailWidth, 44);
            float chipWidth = (detailWidth - 24) / 3;
            for (int i = 0; i < 3; i++) PlaceSkill((RectTransform)metadataRoot.GetChild(i), i * (chipWidth + 12), 0, chipWidth, 44);
            y += 56;
        }
        bool requirements = !dataMode && !string.IsNullOrEmpty(requirementsText.text);
        requirementsText.gameObject.SetActive(requirements);
        if (requirements)
        {
            float height = SkillTextHeight(requirementsText, detailWidth, 32);
            PlaceSkill(requirementsText.rectTransform, 0, y, detailWidth, height); y += height + 12;
        }
        int count = !dataMode && displayedSkill != null ? Mathf.Max(0, displayedSkill.diceCount) : 0;
        summaryHeight = 0;
        if (count > 1)
        {
            int columns = Mathf.Max(1, Mathf.FloorToInt(detailWidth / 230));
            float width = (detailWidth - (columns - 1) * 10) / columns;
            for (int i = 0; i < count; i++) PlaceSkill((RectTransform)diceLinks[i].transform, (i % columns) * (width + 10), (i / columns) * 52, width, 42);
            summaryHeight = Mathf.Ceil(count / (float)columns) * 52;
            PlaceSkill(summaryRoot, 0, y, detailWidth, summaryHeight); y += summaryHeight + 4;
        }
        bodyTop = y; bodyWidth = detailWidth - 17;
        float available = Mathf.Max(90, frameState.height - 270 - y);
        PlaceSkill(bodyArea, 0, y, detailWidth, available);
        float cursor = 0;
        if (dataMode)
        {
            float height = SkillTextHeight(detailText, bodyWidth, 40);
            PlaceSkill(detailText.rectTransform, 0, 0, bodyWidth, height); cursor = height;
        }
        else
        {
            for (int i = 0; i < count && i < diceCards.Count; i++)
            {
                DiceCardView card = diceCards[i]; card.top = cursor;
                float rulesHeight = SkillTextHeight(card.clauses, bodyWidth - 48, 34);
                float height = 100 + rulesHeight;
                PlaceSkill(card.rect, 0, cursor, bodyWidth, height);
                PlaceSkill(card.heading.rectTransform, 24, 15, bodyWidth - 280, 42);
                PlaceSkill((RectTransform)card.icon.transform, bodyWidth - 255, 16, 42, 42);
                PlaceSkill(card.range.rectTransform, bodyWidth - 205, 15, 181, 44);
                PlaceSkill(card.rule, 22, 70, bodyWidth - 44, 3);
                PlaceSkill(card.clauses.rectTransform, 24, 86, bodyWidth - 48, rulesHeight);
                cursor += height + 16;
            }
            PlaceSkill(descriptionHeading.rectTransform, 6, cursor + 2, bodyWidth - 12, 38); cursor += 48;
            float descriptionHeight = SkillTextHeight(descriptionBody, bodyWidth - 16, 38);
            PlaceSkill(descriptionBody.rectTransform, 6, cursor, bodyWidth - 16, descriptionHeight); cursor += descriptionHeight + 10;
        }
        detailContent.sizeDelta = new Vector2(0, cursor);
        bool overflow = cursor > available + 1;
        detailScrollRect.verticalScrollbar.gameObject.SetActive(overflow);
        Canvas.ForceUpdateCanvases();
        detailScrollRect.StopMovement(); detailScrollRect.verticalNormalizedPosition = reset ? 1 : oldPosition;
        if (!overflow) detailContent.anchoredPosition = Vector2.zero;
    }

    private void ScrollToDie(int index)
    {
        if (closing || index < 0 || index >= diceCards.Count) return;
        float overflow = detailContent.rect.height - detailViewport.rect.height;
        detailScrollRect.StopMovement();
        detailScrollRect.verticalNormalizedPosition = overflow > 0 ? 1 - Mathf.Clamp01(diceCards[index].top / overflow) : 1;
    }

    private void UpdateSkillChoices(BattleAllyController ally, SpeedButton sourceButton)
    {
        if (ally == null || skillListRoot == null) return;
        ResolveBattleButton(); EnemySkill_SO[] skills = ally.GetAvailableSkills() ?? new EnemySkill_SO[0];
        bool same = skills.Length == choiceViews.Count;
        if (same) for (int i = 0; i < skills.Length; i++) if (choiceViews[i].skill != skills[i]) { same = false; break; }
        float oldScroll = skillListScroll.verticalNormalizedPosition;
        if (!same)
        {
            ClearSkillChoices();
            for (int i = 0; i < skills.Length; i++) CreateSkillChoice(skills[i], i);
            oldScroll = 1;
        }
        float cursor = 0, width = frameState.listWidth - 17;
        int selectedIndex = ally.GetSelectedSkillIndexForButton(sourceButton);
        for (int i = 0; i < choiceViews.Count; i++)
        {
            SkillChoiceView row = choiceViews[i]; EnemySkill_SO skill = skills[i];
            bool selected = i == ally.GetSelectedSkillIndexForButton(sourceButton);
            string reason = skill == null ? "技能未配置" : cachedBattleButton != null ? cachedBattleButton.GetAllySkillUnavailableReason(ally, sourceButton, skill) : string.Empty;
            bool available = string.IsNullOrEmpty(reason);
            row.button.interactable = available; row.face.SetState(selected, available);
            row.name.text = skill != null ? skill.skillName : "未配置技能";
            row.cost.text = "能耗 " + (skill != null ? Mathf.Max(0, skill.energyCost) : 0);
            string requirement = skill != null && skill.RequiresSelfStatusToUse && skill.requiredSelfStatusData != null
                ? "需要 " + skill.requiredSelfStatusData.DisplayName + " " + Mathf.Max(1, skill.requiredSelfStatusStacks) : string.Empty;
            row.reason.text = string.IsNullOrEmpty(requirement) ? reason : requirement + (string.IsNullOrEmpty(reason) ? "" : " · " + reason);
            row.reason.color = available ? SkillMuted : new Color32(220, 153, 125, 255);
            row.reason.gameObject.SetActive(!string.IsNullOrEmpty(row.reason.text));
            row.equipped.gameObject.SetActive(selected); row.check.gameObject.SetActive(selected); row.name.color = available ? SkillIvory : SkillMuted;
            bool defense = skill != null && skill.GetDiceSkillCategory(0) == EnemySkill_SO.SkillCategory.Defense;
            row.icon.skillStyle = defense ? BattleSkillFrameGraphic.SkillStyle.Defense : BattleSkillFrameGraphic.SkillStyle.Attack; row.icon.SetVerticesDirty();
            row.summary.text = skill == null ? string.Empty : skill.diceCount == 1
                ? (defense ? "防御骰 " : "攻击骰 ") + BuildDiceRangeLabel(skill, 0, skill.GetDicePowerRange(0), ally.GetDisplayedDicePowerRange(skill, 0))
                : "骰子数 " + Mathf.Max(0, skill.diceCount);
            float nameHeight = Mathf.Max(34, SkillTextHeight(row.name, width - 84) - 6);
            float reasonHeight = row.reason.gameObject.activeSelf ? SkillTextHeight(row.reason, width - 30, 28) : 0;
            float height = nameHeight + 75 + (reasonHeight > 0 ? reasonHeight + 7 : 0);
            PlaceSkill(row.rect, 0, cursor, width, height);
            PlaceSkill((RectTransform)row.icon.transform, 18, 24, 42, 42);
            PlaceSkill(row.name.rectTransform, 76, 10, width - 94, nameHeight);
            PlaceSkill(row.cost.rectTransform, 76, 14 + nameHeight, selected ? width - 194 : width - 92, 26);
            PlaceSkill(row.summary.rectTransform, 76, 40 + nameHeight, width - 92, 26);
            PlaceSkill(row.reason.rectTransform, 15, nameHeight + 77, width - 30, reasonHeight);
            PlaceSkill(row.equipped.rectTransform, width - 91, 15 + nameHeight, 78, 24);
            PlaceSkill(row.check, width - 117, 17 + nameHeight, 20, 20);
            cursor += height + 12;
        }
        skillListRoot.sizeDelta = new Vector2(0, Mathf.Max(0, cursor - 12));
        bool overflows = skillListRoot.rect.height > skillListScroll.viewport.rect.height + 1;
        listScrollbar.gameObject.SetActive(overflows);
        Canvas.ForceUpdateCanvases(); skillListScroll.verticalNormalizedPosition = oldScroll;
        if (!overflows) skillListRoot.anchoredPosition = Vector2.zero;
        if (selectedIndex != selectedChoiceIndex && selectedIndex >= 0 && selectedIndex < choiceViews.Count)
        {
            RectTransform selected = choiceViews[selectedIndex].rect;
            float overflow = skillListRoot.rect.height - skillListScroll.viewport.rect.height;
            float top = -selected.anchoredPosition.y, bottom = top + selected.rect.height;
            float visibleTop = Mathf.Max(0, overflow) * (1 - skillListScroll.verticalNormalizedPosition);
            if (overflow > 0 && (top < visibleTop || bottom > visibleTop + skillListScroll.viewport.rect.height))
            {
                float offset = top < visibleTop ? top : bottom - skillListScroll.viewport.rect.height;
                skillListScroll.verticalNormalizedPosition = 1 - Mathf.Clamp01(offset / overflow);
            }
        }
        selectedChoiceIndex = selectedIndex;
    }

    private void CreateSkillChoice(EnemySkill_SO skill, int index)
    {
        RectTransform rect = SkillRect(skillListRoot, "Skill_" + index);
        var face = rect.gameObject.AddComponent<BattleSkillFrameGraphic>(); face.skillStyle = BattleSkillFrameGraphic.SkillStyle.Choice;
        Button button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = face; button.transition = Selectable.Transition.None;
        Navigation navigation = button.navigation; navigation.mode = Navigation.Mode.Automatic; button.navigation = navigation;
        rect.gameObject.AddComponent<MapCraftingButtonFeedback>();
        var row = new SkillChoiceView { skill = skill, rect = rect, button = button, face = face };
        row.name = SkillText(rect, "Name", 29, SkillIvory, string.Empty); row.name.enableWordWrapping = true;
        row.cost = SkillText(rect, "Cost", 24, SkillIvory, string.Empty);
        row.summary = SkillText(rect, "Summary", 21, SkillMuted, string.Empty);
        row.reason = SkillText(rect, "Reason", 22, SkillMuted, string.Empty); row.reason.enableWordWrapping = true;
        row.equipped = SkillText(rect, "Equipped", 21, SkillGold, "已装备"); row.equipped.alignment = TextAlignmentOptions.MidlineRight;
        row.check = SkillRect(rect, "EquippedCheck");
        var check = row.check.gameObject.AddComponent<BattleSkillFrameGraphic>(); check.skillStyle = BattleSkillFrameGraphic.SkillStyle.Check; check.raycastTarget = false;
        row.icon = SkillRect(rect, "TypeIcon").gameObject.AddComponent<BattleSkillFrameGraphic>(); row.icon.raycastTarget = false;
        button.onClick.AddListener(() =>
        {
            if (closing || currentAlly == null || viewMode != SkillViewMode.Ally || row.skill == null) return;
            ResolveBattleButton();
            if (cachedBattleButton != null && !cachedBattleButton.CanSelectSkillForAllyButton(currentAlly, currentAllySourceButton, row.skill)) { RefreshCurrentView(); return; }
            if (currentAlly.SetSelectedSkillIndexForButton(currentAllySourceButton, index)) RefreshCurrentView();
        });
        choiceViews.Add(row); skillButtons.Add(button); skillButtonTexts.Add(row.name);
    }

    private void ClearSkillChoices()
    {
        foreach (SkillChoiceView row in choiceViews)
        {
            if (row.rect == null) continue;
            row.rect.gameObject.SetActive(false);
            if (Application.isPlaying) Destroy(row.rect.gameObject); else DestroyImmediate(row.rect.gameObject);
        }
        choiceViews.Clear(); skillButtons.Clear(); skillButtonTexts.Clear(); selectedChoiceIndex = -1;
        if (skillListRoot != null) skillListRoot.sizeDelta = Vector2.zero;
    }

    private void RefreshHover(bool reset)
    {
        if (currentHoverEnemy == null || IsVisible) return;
        EnemySkill_SO skill = currentHoverSourceButton != null ? currentHoverEnemy.GetSelectedSkillForButton(currentHoverSourceButton) : currentHoverEnemy.SelectedSkill;
        if (skill == null) { HideEnemyHoverSkillInfo(); return; }
        string[] sections = BuildCompactEnemyHoverSections(currentHoverEnemy, skill);
        string clauses = sections[1] == "词条：\n无" ? "词条：<noparse>无</noparse>" : sections[1];
        bool changed = reset || hoverSkill != skill || hoverClauseText.text != clauses;
        bool wasVisible = hoverPanelRoot.gameObject.activeSelf;
        hoverSkill = skill; hoverTitleText.text = skill.skillName;
        hoverContextText.text = (currentHoverEnemy.enemyData != null ? currentHoverEnemy.enemyData.enemyName : "敌人") + " · 当前技能";
        hoverDetailText.text = sections[0]; hoverClauseText.text = clauses;
        hoverPanelRoot.gameObject.SetActive(true);
        if (!wasVisible) hoverProgress = Application.isPlaying ? 0 : 1;
        LayoutHover();
        if (changed)
            hoverClauseAutoScroller.Configure(hoverClauseViewport, hoverClauseContent, hoverClauseText,
                enemyHoverClauseScrollSpeed, enemyHoverClauseStartDelay, enemyHoverClauseBottomPause);
    }

    private void LayoutHover()
    {
        if (hoverPanelRoot == null || hoverSkill == null) return;
        float width = 570, inner = width - 56;
        PlaceSkill(hoverTitleText.rectTransform, 28, 18, inner, 44);
        PlaceSkill(hoverContextText.rectTransform, 28, 68, inner, 32);
        float rangeHeight = SkillTextHeight(hoverDetailText, inner - 32, 32);
        PlaceSkill(hoverRangeWell, 24, 112, width - 48, rangeHeight + 22);
        PlaceSkill(hoverDetailText.rectTransform, 16, 8, inner - 24, rangeHeight);
        float y = 146 + rangeHeight;
        PlaceSkill(hoverRule, 26, y, width - 52, 4); y += 16;
        float height = SkillTextHeight(hoverClauseText, inner, 34);
        float visibleHeight = Mathf.Min(height, Mathf.Max(80, enemyHoverClauseViewportSize.y));
        PlaceSkill(hoverClauseViewport, 28, y, inner, visibleHeight);
        hoverClauseContent.sizeDelta = new Vector2(0, height);
        PlaceSkill(hoverClauseText.rectTransform, 0, 0, inner, height);
        y += visibleHeight + 10;
        hoverHintText.text = height > visibleHeight + 1 ? "词条自动滚动 · 点击速度按钮查看详情" : "点击速度按钮查看详情";
        PlaceSkill(hoverHintText.rectTransform, 26, y, width - 52, 32);
        hoverPanelRoot.sizeDelta = new Vector2(width, y + 54);
        ApplyHoverTransform();
    }
}
