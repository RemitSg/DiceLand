using TMPro;
using UnityEngine;
using UnityEngine.UI;

public partial class BattleSkillInfoOverlay
{
    private static readonly Color RangeDeltaYellow = new Color32(250, 210, 110, 255);

    private sealed class DiceRangeRowView
    {
        public RectTransform rect;
        public TextMeshProUGUI source, minimum, maximum;
        public Image icon;
    }

    private static string DiceRangeText(string chinese, string english)
        => GameText.LanguageCode == GameText.English ? english : chinese;

    private static string GetDiceTargetLabel(BattleDiceRangePreview preview)
    {
        if (string.IsNullOrEmpty(preview.TargetName))
            return DiceRangeText("未连线 · 选定目标后计入目标相关修正", "No target · Link a target to include its modifiers");
        return (preview.IsClash ? DiceRangeText("拼点目标：", "Clash target: ") : DiceRangeText("攻击目标：", "Attack target: "))
            + preview.TargetName;
    }

    private void BuildDiceRangeDetails(DiceCardView card)
    {
        card.rangeCaption = SkillText(card.rect, "DiceRangeCaption", 23, SkillMuted, string.Empty);
        RectTransform toggle = SkillRect(card.rect, "DiceRangeToggle");
        var face = SkillFace(toggle, MapCraftingFrameGraphic.Style.Badge);
        face.raycastTarget = true;
        card.rangeToggle = toggle.gameObject.AddComponent<Button>();
        card.rangeToggle.targetGraphic = face;
        card.rangeToggle.transition = Selectable.Transition.None;
        toggle.gameObject.AddComponent<MapCraftingButtonFeedback>().scaleFeedback = false;
        card.rangeToggle.onClick.AddListener(() => ToggleDiceRange(card));
        card.rangeToggleLabel = SkillText(toggle, "ToggleLabel", 22, SkillGold, string.Empty);
        StretchSkill(card.rangeToggleLabel.rectTransform, 14);
        card.rangeToggleArrow = SkillRect(toggle, "ToggleChevron");
        card.rangeToggleArrow.anchorMin = card.rangeToggleArrow.anchorMax = new Vector2(1, 0.5f);
        card.rangeToggleArrow.pivot = new Vector2(0.5f, 0.5f);
        card.rangeToggleArrow.anchoredPosition = new Vector2(-20, 0);
        card.rangeToggleArrow.sizeDelta = new Vector2(18, 18);
        var chevron = card.rangeToggleArrow.gameObject.AddComponent<BattleSkillFrameGraphic>();
        chevron.skillStyle = BattleSkillFrameGraphic.SkillStyle.Chevron;
        chevron.raycastTarget = false;

        card.rangeTable = SkillRect(card.rect, "DiceRangeBreakdown");
        SkillFace(card.rangeTable, MapCraftingFrameGraphic.Style.Preview);
        card.rangeTableRule = SkillRect(card.rangeTable, "HeaderRule");
        AddSkillRule(card.rangeTableRule);
        card.rangeTable.gameObject.SetActive(false);
    }

    private void RenderDiceRange(DiceCardView card, BattleDiceRangeBreakdown breakdown, Color resultColor, bool reset)
    {
        card.breakdown = breakdown;
        if (reset)
            card.rangeExpanded = false;
        card.rangeCaption.text = breakdown.WillRoll
            ? DiceRangeText("最终范围", "Final range") : DiceRangeText("当前范围", "Current range");
        card.range.text = $"{breakdown.Range.minPower} ~ {breakdown.Range.maxPower}";
        card.range.color = resultColor;
        UpdateDiceRangeToggle(card);
        if (card.rangeExpanded)
            RenderDiceRangeRows(card);
    }

    private void ToggleDiceRange(DiceCardView card)
    {
        if (closing || card.breakdown == null)
            return;

        float offset = detailContent.anchoredPosition.y;
        card.rangeExpanded = !card.rangeExpanded;
        UpdateDiceRangeToggle(card);
        if (card.rangeExpanded)
            RenderDiceRangeRows(card);
        LayoutSkillContent(false);

        float overflow = detailContent.rect.height - detailViewport.rect.height;
        detailScrollRect.verticalNormalizedPosition = overflow > 0 ? 1 - Mathf.Clamp01(offset / overflow) : 1;
    }

    private static void UpdateDiceRangeToggle(DiceCardView card)
    {
        int count = card.breakdown.Modifiers.Count;
        card.rangeToggleLabel.text = card.rangeExpanded ? DiceRangeText("收起明细", "Hide details")
            : count > 0 ? DiceRangeText($"查看修正 · {count} 项", $"View modifiers · {count}")
            : DiceRangeText("查看范围明细", "View range details");
        card.rangeToggleArrow.localRotation = Quaternion.Euler(0, 0, card.rangeExpanded ? 180 : 0);
        card.rangeTable.gameObject.SetActive(card.rangeExpanded);
    }

    private void RenderDiceRangeRows(DiceCardView card)
    {
        BattleDiceRangeBreakdown breakdown = card.breakdown;
        int count = breakdown.Modifiers.Count + 2;
        while (card.rangeRows.Count < count)
        {
            RectTransform rect = SkillRect(card.rangeTable, "RangeRow_" + card.rangeRows.Count);
            var row = new DiceRangeRowView { rect = rect };
            row.source = SkillText(rect, "Source", 22, SkillIvory, string.Empty);
            row.source.textPreprocessor = null;
            row.source.enableWordWrapping = true;
            row.minimum = SkillText(rect, "Minimum", 23, SkillIvory, string.Empty);
            row.maximum = SkillText(rect, "Maximum", 23, SkillIvory, string.Empty);
            row.minimum.alignment = row.maximum.alignment = TextAlignmentOptions.Midline;
            row.icon = SkillRect(rect, "SourceIcon").gameObject.AddComponent<Image>();
            row.icon.preserveAspect = true;
            row.icon.raycastTarget = false;
            card.rangeRows.Add(row);
        }

        for (int i = 0; i < card.rangeRows.Count; i++)
            card.rangeRows[i].rect.gameObject.SetActive(i < count);
        SetDiceRangeRow(card.rangeRows[0], DiceRangeText("影响来源", "Modifier source"),
            DiceRangeText("下限", "Min"), DiceRangeText("上限", "Max"), SkillGold, SkillGold, null);
        SetDiceRangeRow(card.rangeRows[1], DiceRangeText("基础范围", "Base range"),
            breakdown.BaseRange.minPower.ToString(), breakdown.BaseRange.maxPower.ToString(), SkillMuted, SkillIvory, null);
        for (int i = 0; i < breakdown.Modifiers.Count; i++)
        {
            BattleDiceRangeBreakdown.Modifier modifier = breakdown.Modifiers[i];
            string label = modifier.ClashOnly ? DiceRangeText("【仅拼点】", "[Clash only] ") + modifier.Name : modifier.Name;
            Sprite icon = modifier.Icon != null ? modifier.Icon : modifier.IsSkill ? MapForestArt.Get("Sword") : null;
            SetDiceRangeRow(card.rangeRows[i + 2], label, FormatSignedInt(modifier.Minimum), FormatSignedInt(modifier.Maximum),
                SkillIvory, RangeDeltaYellow, icon);
        }
    }

    private static void SetDiceRangeRow(DiceRangeRowView row, string label, string minimum, string maximum, Color labelColor, Color valueColor, Sprite icon)
    {
        row.source.text = label;
        row.source.color = labelColor;
        row.minimum.text = minimum;
        row.maximum.text = maximum;
        row.minimum.color = row.maximum.color = valueColor;
        row.icon.sprite = icon;
        row.icon.enabled = icon != null;
    }

    private float LayoutDiceCard(DiceCardView card)
    {
        const float padding = 24;
        const float contentTop = 82;
        const float toggleHeight = 38;
        float tableWidth = Mathf.Clamp(bodyWidth * 0.46f, 370, 475);
        float tableX = bodyWidth - padding - tableWidth;
        float rulesWidth = tableX - padding - 22;
        float rulesHeight = SkillTextHeight(card.clauses, rulesWidth, 34);
        PlaceSkill(card.heading.rectTransform, padding, 14, rulesWidth, 44);
        PlaceSkill((RectTransform)card.icon.transform, tableX + 8, 15, 40, 40);
        PlaceSkill(card.rangeCaption.rectTransform, tableX + 57, 15, tableWidth - 211, 40);
        PlaceSkill(card.range.rectTransform, bodyWidth - padding - 146, 12, 137, 46);
        PlaceSkill(card.rule, 22, 66, bodyWidth - 44, 3);
        PlaceSkill(card.clauses.rectTransform, padding, contentTop, rulesWidth, rulesHeight);
        PlaceSkill((RectTransform)card.rangeToggle.transform, tableX, contentTop, tableWidth, toggleHeight);
        float tableBottom = contentTop + toggleHeight;
        if (card.rangeExpanded)
        {
            float tableHeight = LayoutDiceRangeTable(card, tableWidth);
            PlaceSkill(card.rangeTable, tableX, tableBottom + 6, tableWidth, tableHeight);
            tableBottom += tableHeight + 6;
        }
        return Mathf.Max(contentTop + rulesHeight, tableBottom) + 16;
    }

    private static float LayoutDiceRangeTable(DiceCardView card, float width)
    {
        const float side = 12;
        const float numberWidth = 68;
        float y = 6;
        for (int i = 0; i < card.rangeRows.Count && card.rangeRows[i].rect.gameObject.activeSelf; i++)
        {
            DiceRangeRowView row = card.rangeRows[i];
            float labelX = row.icon.enabled ? 38 : 4;
            float labelWidth = width - side * 2 - numberWidth * 2 - labelX;
            float rowHeight = SkillTextHeight(row.source, labelWidth, 34);
            PlaceSkill(row.rect, side, y, width - side * 2, rowHeight);
            PlaceSkill(row.source.rectTransform, labelX, 0, labelWidth, rowHeight);
            PlaceSkill(row.icon.rectTransform, 3, (rowHeight - 30) * 0.5f, 30, 30);
            PlaceSkill(row.minimum.rectTransform, labelX + labelWidth, 0, numberWidth, rowHeight);
            PlaceSkill(row.maximum.rectTransform, labelX + labelWidth + numberWidth, 0, numberWidth, rowHeight);
            y += rowHeight;
            if (i == 0)
            {
                PlaceSkill(card.rangeTableRule, side, y, width - side * 2, 2);
                y += 4;
            }
        }
        return y + 7;
    }
}
