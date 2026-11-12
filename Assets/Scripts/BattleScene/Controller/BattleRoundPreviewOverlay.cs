using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

internal sealed class BattleRoundPreviewOverlay
{
    private const int VisibleRows = 3;
    private const float RowHeight = 44f;
    private const float HeaderHeight = 35f;
    private const float BadgeSize = 36f;
    private static readonly Color Gold = new Color32(239, 193, 101, 255);
    private static readonly Color Ivory = new Color32(246, 235, 204, 255);
    private static readonly Color Red = new Color32(255, 126, 110, 255);
    private static readonly Color Blue = new Color32(132, 207, 242, 255);
    private static readonly Color Muted = new Color32(175, 190, 167, 255);
    private static readonly float[] BadgeFractions = { 0.5f, 0.36f, 0.64f, 0.25f, 0.75f };
    private static RectTransform pointerPanel;

    private sealed class Row
    {
        public RectTransform Rect;
        public BattleRoundPreviewFrameGraphic Frame;
        public TMP_Text Order, Actors, Kind;
        public SpeedButton Source, Target;
        public BattleCombatActionType Type;
        public Color Accent;
        public bool Unintercepted;
    }

    private sealed class Badge
    {
        public RectTransform Rect;
        public BattleRoundPreviewFrameGraphic Frame;
        public TMP_Text Number;
    }

    private readonly RectTransform root, panel, titleFrame, warningFrame, viewport, content;
    private readonly TMP_FontAsset bodyFont;
    private readonly TMP_Text title, warning, position, empty;
    private readonly ScrollRect scroll;
    private readonly Scrollbar scrollbar;
    private readonly List<Row> rows = new List<Row>();
    private readonly Badge[] badges = new Badge[VisibleRows];
    private int count, selected = -1, warningCount;
    private bool layoutDirty = true;
    private Rect previousSafeArea, previousActionBounds;
    private Vector2 previousScreen, previousCanvas;

    public BattleRoundPreviewOverlay(RectTransform canvas, TMP_FontAsset headingFont, TMP_FontAsset bodyFont)
    {
        this.bodyFont = bodyFont != null ? bodyFont : headingFont;
        root = CreateRect(canvas, "BattleRoundPreview");
        Fill(root, 0, 0, 0, 0);
        for (int i = 0; i < badges.Length; i++)
        {
            RectTransform rect = CreateRect(root, "RoundActionBadge_" + i);
            var frame = rect.gameObject.AddComponent<BattleRoundPreviewFrameGraphic>();
            frame.raycastTarget = false;
            TMP_Text label = Label(rect, "Number", this.bodyFont, 24, Ivory);
            Fill(label.rectTransform, 0, 0, 0, 0);
            badges[i] = new Badge { Rect = rect, Frame = frame, Number = label };
            rect.gameObject.SetActive(false);
        }

        panel = CreateRect(root, "RoundPreviewPanel");
        var panelFrame = panel.gameObject.AddComponent<MapMerchantFrameGraphic>();
        panelFrame.style = MapMerchantFrameGraphic.Style.Panel;
        panelFrame.raycastTarget = false;

        RectTransform list = CreateRect(panel, "RoundActionList");
        Fill(list, 0, 0, 0, 0);
        scroll = list.gameObject.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = false;
        scroll.inertia = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = RowHeight;
        viewport = CreateRect(list, "Viewport");
        viewport.gameObject.AddComponent<RectMask2D>();
        viewport.gameObject.AddComponent<Image>().color = Color.clear;
        content = CreateRect(viewport, "Actions");
        content.anchorMin = new Vector2(0, 1);
        content.anchorMax = content.pivot = new Vector2(1, 1);
        scroll.viewport = viewport;
        scroll.content = content;
        scrollbar = BuildScrollbar(list);
        scroll.verticalScrollbar = scrollbar;
        scroll.onValueChanged.AddListener(_ => UpdatePosition());

        position = Label(panel, "VisibleActions", this.bodyFont, 19, Muted);
        Place(position.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 7), new Vector2(166, 22));
        empty = Label(viewport, "EmptyActions", this.bodyFont, 23, Muted);
        Fill(empty.rectTransform, 4, 0, 4, 0);

        titleFrame = CreateRect(panel, "RoundPreviewTitle");
        var plaque = titleFrame.gameObject.AddComponent<MapMerchantFrameGraphic>();
        plaque.style = MapMerchantFrameGraphic.Style.SecondaryButton;
        plaque.raycastTarget = false;
        title = Label(titleFrame, "Title", headingFont != null ? headingFont : this.bodyFont, 26, Gold);
        Fill(title.rectTransform, 6, 1, 6, 0);

        warningFrame = CreateRect(panel, "Unintercepted");
        var warningFace = warningFrame.gameObject.AddComponent<BattleRoundPreviewFrameGraphic>();
        warningFace.SetAppearance(BattleRoundPreviewFrameGraphic.Shape.Warning, Red);
        AddButton(warningFrame, warningFace, FocusNextWarning);
        warning = Label(warningFrame, "WarningLabel", this.bodyFont, 22, Red);
        Fill(warning.rectTransform, 29, 0, 7, 0);
        root.gameObject.SetActive(false);
    }

    public void Show(IReadOnlyList<BattleCombatAction> actions)
    {
        int oldFirst = FirstVisibleIndex();
        Row oldRow = selected >= 0 ? rows[selected] : null;
        SpeedButton oldSource = oldRow?.Source;
        SpeedButton oldTarget = oldRow?.Target;
        BattleCombatActionType oldType = oldRow?.Type ?? default;
        if (selected >= 0)
            ApplyEmphasis(false);
        selected = -1;
        count = actions.Count;
        warningCount = 0;
        double?[] rates = BattleClashWinRateService.Estimate(actions);
        for (int i = 0; i < count; i++)
        {
            if (i == rows.Count)
                rows.Add(BuildRow(i));
            BattleCombatAction action = actions[i];
            Row row = rows[i];
            SetRow(row, action, i, rates[i]);
            if (row.Source == oldSource && row.Target == oldTarget && row.Type == oldType)
                selected = i;
            if (row.Unintercepted)
                warningCount++;
        }
        for (int i = count; i < rows.Count; i++)
        {
            rows[i].Rect.gameObject.SetActive(false);
            rows[i].Source = rows[i].Target = null;
        }

        title.text = Local("回合预览", "Round preview");
        warning.text = Local($"未拦截 {warningCount}", $"Unopposed {warningCount}");
        warningFrame.gameObject.SetActive(warningCount > 0);
        empty.text = Local("本回合暂无行动", "No planned actions");
        empty.gameObject.SetActive(count == 0);
        scroll.vertical = count > VisibleRows;
        scrollbar.gameObject.SetActive(scroll.vertical);
        position.gameObject.SetActive(scroll.vertical);
        content.sizeDelta = new Vector2(0, count * RowHeight);
        SetFirstVisible(oldFirst);
        if (selected >= 0)
            BringIntoView(selected);
        layoutDirty = true;
        RefreshRowFocus();
        SetVisible(true);
    }

    public void SetVisible(bool visible)
    {
        if (root == null)
        {
            if (selected >= 0)
                ApplyEmphasis(false);
            return;
        }
        if (root.gameObject.activeSelf == visible)
            return;
        root.gameObject.SetActive(visible);
        if (visible)
            pointerPanel = panel;
        else if (pointerPanel == panel)
            pointerPanel = null;
        if (selected >= 0)
            ApplyEmphasis(visible);
    }

    public void Clear()
    {
        SetVisible(false);
        for (int i = 0; i < count; i++)
        {
            if (rows[i].Rect != null)
                rows[i].Rect.gameObject.SetActive(false);
            rows[i].Source = rows[i].Target = null;
        }
        count = 0;
        selected = -1;
        if (content != null)
            SetFirstVisible(0);
    }

    public static bool ContainsPointer(Vector2 point)
    {
        return pointerPanel != null && pointerPanel.gameObject.activeInHierarchy
            && RectTransformUtility.RectangleContainsScreenPoint(pointerPanel, point);
    }

    public void ApplyViewport(Rect safeArea, Vector2 pixels, Rect actionBounds)
    {
        Vector2 canvasSize = root.rect.size;
        if (pixels.x <= 0 || pixels.y <= 0 || canvasSize.x <= 0 || canvasSize.y <= 0)
            return;
        if (!layoutDirty && previousSafeArea == safeArea && previousScreen == pixels
            && previousCanvas == canvasSize && previousActionBounds == actionBounds)
            return;

        layoutDirty = false;
        previousSafeArea = safeArea;
        previousScreen = pixels;
        previousCanvas = canvasSize;
        previousActionBounds = actionBounds;
        Vector2 scale = new Vector2(canvasSize.x / pixels.x, canvasSize.y / pixels.y);
        Vector2 min = Vector2.Scale(scale, safeArea.min);
        Vector2 max = Vector2.Scale(scale, safeArea.max);
        const float gap = 19f;
        float actionRight = actionBounds.width > 0 ? actionBounds.xMax * scale.x : (min.x + max.x) * 0.5f + 225f;
        float width = Mathf.Min(660f, max.x - gap - actionRight - gap);
        float bottom = min.y + 34f;
        if (width < 460f)
        {
            width = Mathf.Max(1f, Mathf.Min(660f, max.x - min.x - gap * 2));
            bottom = actionBounds.height > 0 ? actionBounds.yMax * scale.y + gap : min.y + 180f;
        }
        bool overflow = count > VisibleRows;
        float listBottom = overflow ? 33f : 12f;
        float height = HeaderHeight + listBottom + Mathf.Clamp(count, 1, VisibleRows) * RowHeight;
        bottom = Mathf.Clamp(bottom, min.y + 18f, Mathf.Max(min.y + 18f, max.y - height - 24f));
        Place(panel, Vector2.zero, Vector2.zero, new Vector2(max.x - gap - width, bottom), new Vector2(width, height));
        Fill(viewport, 20, listBottom, overflow ? 32f : 20f, HeaderHeight);
        RectTransform bar = scrollbar.GetComponent<RectTransform>();
        bar.anchorMin = new Vector2(1, 0);
        bar.anchorMax = Vector2.one;
        bar.offsetMin = new Vector2(-23, listBottom);
        bar.offsetMax = new Vector2(-16, -HeaderHeight);

        float warningWidth = Mathf.Clamp(width * 0.30f, 102f, 177f);
        float titleWidth = Mathf.Min(width * 0.44f, 202f);
        float titleCenter = warningCount > 0
            ? Mathf.Min(width * 0.5f, width - warningWidth - titleWidth * 0.5f - 26f)
            : width * 0.5f;
        Place(titleFrame, new Vector2(0, 1), new Vector2(0.5f, 0.5f),
            new Vector2(titleCenter, -1), new Vector2(titleWidth, 39));
        Place(warningFrame, Vector2.one, new Vector2(1, 0.5f),
            new Vector2(-15, -1), new Vector2(warningWidth, 35));
    }

    public void UpdateBadges(Camera camera)
    {
        int first = selected >= 0 ? selected : FirstVisibleIndex();
        for (int i = 0; i < badges.Length; i++)
        {
            Badge badge = badges[i];
            int rowIndex = first + i;
            if (rowIndex >= count || (selected >= 0 && i > 0)
                || !TryGetBadgeAnchor(rows[rowIndex].Source, camera, i, out Vector2 anchor))
            {
                badge.Rect.gameObject.SetActive(false);
                continue;
            }

            badge.Rect.gameObject.SetActive(true);
            Place(badge.Rect, anchor, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(BadgeSize, BadgeSize));
            badge.Number.text = (rowIndex + 1).ToString(CultureInfo.InvariantCulture);
            badge.Frame.SetAppearance(BattleRoundPreviewFrameGraphic.Shape.Order, rows[rowIndex].Accent);
        }
    }

    private bool TryGetBadgeAnchor(SpeedButton source, Camera camera, int index, out Vector2 anchor)
    {
        anchor = default;
        if (source == null)
            return false;
        foreach (float fraction in BadgeFractions)
        {
            if (!source.TryGetRoundPreviewAnchor(camera, fraction, out anchor))
                continue;
            bool overlaps = false;
            for (int i = 0; i < index; i++)
            {
                RectTransform other = badges[i].Rect;
                if (other.gameObject.activeSelf
                    && Vector2.Scale(other.anchorMin - anchor, root.rect.size).sqrMagnitude < BadgeSize * BadgeSize)
                {
                    overlaps = true;
                    break;
                }
            }
            if (!overlaps)
                return true;
        }
        return false;
    }

    private Row BuildRow(int index)
    {
        RectTransform rect = CreateRect(content, "RoundAction_" + (index + 1));
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(1, 1);
        rect.pivot = new Vector2(0.5f, 1);
        rect.anchoredPosition = new Vector2(0, -index * RowHeight);
        rect.sizeDelta = new Vector2(0, RowHeight - 2);
        var frame = rect.gameObject.AddComponent<BattleRoundPreviewFrameGraphic>();
        AddButton(rect, frame, () => Focus(index == selected ? -1 : index));
        TMP_Text order = Label(rect, "Order", bodyFont, 24, Ivory);
        TMP_Text actors = Label(rect, "Actors", bodyFont, 24, Ivory, TextAlignmentOptions.MidlineLeft);
        TMP_Text kind = Label(rect, "ActionKind", bodyFont, 23, Gold, TextAlignmentOptions.MidlineRight);
        PlaceColumn(order.rectTransform, 0f, 0.105f, 2, 2);
        PlaceColumn(actors.rectTransform, 0.105f, 0.65f, 3, 4);
        PlaceColumn(kind.rectTransform, 0.65f, 1, 2, 12);
        return new Row { Rect = rect, Frame = frame, Order = order, Actors = actors, Kind = kind };
    }

    private static void SetRow(Row row, BattleCombatAction action, int index, double? winRate)
    {
        bool enemyFirst = action.Type == BattleCombatActionType.EnemySingleAttack;
        bool clash = action.Type == BattleCombatActionType.Clash;
        row.Type = action.Type;
        row.Source = enemyFirst ? action.EnemyButton : action.AllyButton;
        row.Target = row.Source != null ? row.Source.ConnectedTarget : null;
        string allyName = (enemyFirst ? action.TargetAlly : action.Ally)?.GetSettlementDisplayName() ?? Local("友方", "Ally");
        string enemyName = action.Enemy.enemyData?.LocalizedName ?? action.Enemy.name;
        row.Actors.text = clash ? $"{allyName} ↔ {enemyName}"
            : enemyFirst ? $"{enemyName} → {allyName}" : $"{allyName} → {enemyName}";
        row.Order.text = (index + 1).ToString(CultureInfo.InvariantCulture);
        if (clash)
        {
            row.Unintercepted = false;
            row.Accent = Gold;
            row.Kind.text = winRate.HasValue
                ? Local("拼点胜率", "Clash win") + $" <color=#{ColorUtility.ToHtmlStringRGB(WinRateColor(winRate.Value))}>{FormatRate(winRate.Value)}</color>"
                : Local("拼点", "Clash");
        }
        else
        {
            EnemySkill_SO skill = enemyFirst ? action.Enemy.GetSelectedSkillForButton(action.EnemyButton)
                : action.Ally.GetSelectedSkillForButton(action.AllyButton);
            bool attacks = BattleClashWinRateService.HasAttackDice(skill);
            row.Unintercepted = enemyFirst && attacks;
            row.Accent = row.Unintercepted ? Red : attacks ? Blue : Muted;
            row.Kind.text = attacks ? Local("单方面攻击", "One-sided attack") : Local("使用技能", "Skill use");
        }
        row.Kind.color = row.Accent;
        row.Rect.gameObject.SetActive(true);
    }

    private void FocusNextWarning()
    {
        for (int i = 1; i <= count; i++)
        {
            int index = (selected + i) % count;
            if (!rows[index].Unintercepted)
                continue;
            Focus(index);
            BringIntoView(index);
            return;
        }
    }

    private void Focus(int index)
    {
        if (selected == index)
            return;
        selected = index;
        RefreshRowFocus();
    }

    private void RefreshRowFocus()
    {
        for (int i = 0; i < count; i++)
        {
            Row row = rows[i];
            row.Order.color = i == selected ? Ivory : row.Accent;
            row.Frame.SetAppearance(BattleRoundPreviewFrameGraphic.Shape.Row, row.Accent, row.Unintercepted, i == selected);
        }
        ApplyEmphasis(root.gameObject.activeSelf && selected >= 0);
    }

    private void ApplyEmphasis(bool focused)
    {
        for (int i = 0; i < count; i++)
        {
            Row row = rows[i];
            float intensity = !focused ? 1f : i == selected ? 1.45f : 0.30f;
            if (row.Source != null)
                row.Source.SetRoundPreviewEmphasis(intensity);
            if (row.Type == BattleCombatActionType.Clash && row.Target != null)
                row.Target.SetRoundPreviewEmphasis(intensity);
        }
    }

    private int FirstVisibleIndex()
        => Mathf.Clamp(Mathf.RoundToInt(content.anchoredPosition.y / RowHeight), 0, Mathf.Max(0, count - VisibleRows));

    private void BringIntoView(int index)
    {
        int first = FirstVisibleIndex();
        if (index < first || index >= first + VisibleRows)
            SetFirstVisible(index - 1);
    }

    private void SetFirstVisible(int index)
    {
        scroll.StopMovement();
        content.anchoredPosition = new Vector2(0, Mathf.Clamp(index, 0, Mathf.Max(0, count - VisibleRows)) * RowHeight);
        UpdatePosition();
    }

    private void UpdatePosition()
    {
        if (count <= VisibleRows)
            return;
        int first = FirstVisibleIndex();
        position.text = $"{first + 1}–{first + VisibleRows} / {count}";
    }

    private static string FormatRate(double rate)
    {
        if (rate > 0 && rate < 0.001)
            return "≤0.1%";
        if (rate > 0.999 && rate < 1 - 1e-12)
            return "≥99.9%";
        return (rate * 100).ToString("0.#", CultureInfo.InvariantCulture) + "%";
    }

    private static Color WinRateColor(double rate)
    {
        var middle = new Color32(244, 208, 124, 255);
        var high = new Color32(133, 232, 156, 255);
        float p = Mathf.Clamp01((float)rate);
        return p <= 0.5f ? Color.Lerp(Red, middle, p * 2f) : Color.Lerp(middle, high, p * 2f - 1f);
    }

    private static string Local(string chinese, string english) => GameText.LanguageCode == GameText.English ? english : chinese;

    private static Scrollbar BuildScrollbar(RectTransform parent)
    {
        RectTransform rect = CreateRect(parent, "ActionScrollbar");
        var track = rect.gameObject.AddComponent<Image>();
        track.color = new Color32(89, 71, 37, 210);
        var bar = rect.gameObject.AddComponent<Scrollbar>();
        RectTransform slide = CreateRect(rect, "SlidingArea");
        Fill(slide, 1, 1, 1, 1);
        RectTransform grip = CreateRect(slide, "Handle");
        Fill(grip, 0, 0, 0, 0);
        var image = grip.gameObject.AddComponent<Image>();
        image.color = Gold;
        bar.handleRect = grip;
        bar.targetGraphic = image;
        bar.direction = Scrollbar.Direction.BottomToTop;
        return bar;
    }

    private static void AddButton(RectTransform rect, Graphic face, UnityEngine.Events.UnityAction click)
    {
        face.raycastTarget = true;
        var button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = face;
        button.onClick.AddListener(click);
    }

    private static TMP_Text Label(RectTransform parent, string name, TMP_FontAsset font, float size, Color tint,
        TextAlignmentOptions alignment = TextAlignmentOptions.Midline)
    {
        var label = CreateRect(parent, name).gameObject.AddComponent<TextMeshProUGUI>();
        if (font != null)
            label.font = font;
        label.fontSize = size;
        label.fontSizeMax = size;
        label.fontSizeMin = size * 0.70f;
        label.enableAutoSizing = true;
        label.enableWordWrapping = false;
        label.overflowMode = TextOverflowModes.Ellipsis;
        label.alignment = alignment;
        label.color = tint;
        label.raycastTarget = false;
        return label;
    }

    private static RectTransform CreateRect(Transform parent, string name)
    {
        var rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
        rect.SetParent(parent, false);
        return rect;
    }

    private static void Fill(RectTransform rect, float left, float bottom, float right, float top)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(-right, -top);
    }

    private static void Place(RectTransform rect, Vector2 anchor, Vector2 pivot, Vector2 offset, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = anchor;
        rect.pivot = pivot;
        rect.anchoredPosition = offset;
        rect.sizeDelta = size;
    }

    private static void PlaceColumn(RectTransform rect, float start, float end, float left, float right)
    {
        rect.anchorMin = new Vector2(start, 0);
        rect.anchorMax = new Vector2(end, 1);
        rect.offsetMin = new Vector2(left, 0);
        rect.offsetMax = new Vector2(-right, 0);
    }
}
