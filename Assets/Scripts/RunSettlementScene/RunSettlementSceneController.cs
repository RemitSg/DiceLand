using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class RunSettlementSceneController : MonoBehaviour
{
    [SerializeField] private string fallbackReturnSceneName = "Menu Selection";
    [SerializeField] private Color backgroundColor = new Color(0.03f, 0.035f, 0.035f, 1f);
    [SerializeField] private Color goldColor = new Color(0.86f, 0.74f, 0.48f, 1f);
    [SerializeField] private Color textColor = new Color(0.9f, 0.86f, 0.78f, 1f);
    [SerializeField] private Color mutedTextColor = new Color(0.64f, 0.59f, 0.5f, 1f);
    [SerializeField] private Color sidePanelColor = new Color(0.04f, 0.05f, 0.055f, 0.96f);
    [SerializeField] private Color statPanelColor = new Color(0.08f, 0.12f, 0.16f, 0.94f);
    [SerializeField] private Color calamityPanelColor = new Color(0.13f, 0.24f, 0.17f, 0.94f);
    [Header("Background Sprites")]
    [SerializeField] private Sprite settlementBackgroundSprite;
    [SerializeField] private Sprite titleLogoSprite;
    [SerializeField] private Sprite scorePanelBackgroundSprite;
    [SerializeField] private Sprite bossDefeatedBackgroundSprite;
    [SerializeField] private Sprite bossFailedBackgroundSprite;
    [SerializeField] private Sprite calamityBackgroundSprite;
    [SerializeField] private Sprite returnButtonBackgroundSprite;
    [SerializeField] private Sprite statsPanelBackgroundSprite;
    [SerializeField] private Sprite statsListBackgroundSprite;
    [Header("Statistic Icons")]
    [SerializeField] private Sprite normalBattleIcon;
    [SerializeField] private Sprite eliteBattleIcon;
    [SerializeField] private Sprite specialDungeonIcon;
    [SerializeField] private Sprite exploredTilesIcon;
    [SerializeField] private Sprite moneyGainedIcon;
    [SerializeField] private Sprite partyMemberIcon;
    [SerializeField] private Sprite relicIcon;
    [SerializeField] private Sprite craftedWeaponIcon;
    [SerializeField] private Sprite eventTileIcon;
    [SerializeField] private Sprite eventBattleIcon;
    [SerializeField] private Sprite keyIcon;
    [SerializeField] private Sprite bossIcon;
    [Header("Fonts")]
    [SerializeField] private TMP_FontAsset titleFont;
    [SerializeField] private TMP_FontAsset numberFont;
    [SerializeField] private TMP_FontAsset bodyFont;
    [SerializeField] private TMP_FontAsset buttonFont;
    [Header("Reveal Animation")]
    [SerializeField, Min(0.1f)] private float totalScoreRevealSeconds = 5f;
    [SerializeField, Min(0.1f)] private float statItemRevealSeconds = 2f;
    [SerializeField, Min(0.1f)] private float zeroStatRevealSeconds = 0.8f;
    [Header("Layout Tuning")]
    [SerializeField, Min(32f)] private float titleLogoWidth = 360f;
    [SerializeField, Min(32f)] private float titleLogoHeight = 120f;
    [SerializeField, Min(8f)] private float statIconSize = 32f;
    [SerializeField, Min(0f)] private float leftPanelSpacing = 12f;
    [SerializeField, Min(0)] private int leftPanelPaddingLeft = 28;
    [SerializeField, Min(0)] private int leftPanelPaddingRight = 28;
    [SerializeField, Min(0)] private int leftPanelPaddingTop = 26;
    [SerializeField, Min(0)] private int leftPanelPaddingBottom = 22;

    private RunSettlementData data;
    private readonly List<SettlementValueRow> statRows = new List<SettlementValueRow>();
    private TextMeshProUGUI totalScoreText;
    private bool skipRequested;

    private void Start()
    {
        data = RunSettlementRuntime.Current ?? BuildFallbackData();
        BuildUi(data);
        StartCoroutine(PlayRevealSequence());
    }

    private void Update()
    {
        bool touchBegan = Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began;
        if (!Input.GetMouseButtonDown(0) && !touchBegan)
            return;

        skipRequested = true;
    }

    private void BuildUi(RunSettlementData settlementData)
    {
        Canvas canvas = CreateCanvas();
        RectTransform root = CreatePanel("Root", canvas.transform, backgroundColor);
        ApplyBackground(root, settlementBackgroundSprite, backgroundColor);
        Stretch(root);

        HorizontalLayoutGroup rootLayout = root.gameObject.AddComponent<HorizontalLayoutGroup>();
        rootLayout.padding = new RectOffset(22, 22, 24, 24);
        rootLayout.spacing = 16f;
        rootLayout.childForceExpandHeight = true;
        rootLayout.childForceExpandWidth = true;

        RectTransform side = CreatePanel("SummaryPanel", root, sidePanelColor);
        LayoutElement sideLayout = side.gameObject.AddComponent<LayoutElement>();
        sideLayout.preferredWidth = 430f;
        sideLayout.flexibleWidth = 0f;
        VerticalLayoutGroup sideGroup = side.gameObject.AddComponent<VerticalLayoutGroup>();
        sideGroup.padding = new RectOffset(leftPanelPaddingLeft, leftPanelPaddingRight, leftPanelPaddingTop, leftPanelPaddingBottom);
        sideGroup.spacing = leftPanelSpacing;
        sideGroup.childAlignment = TextAnchor.UpperCenter;
        sideGroup.childForceExpandWidth = true;
        sideGroup.childForceExpandHeight = false;

        AddTitleLogo(side);
        AddScorePanel(side, settlementData);
        AddBossStatus(side, settlementData.runCleared);
        AddCalamityPanel(side, settlementData);
        AddReturnButton(side, settlementData);

        RectTransform stats = CreatePanel("StatsPanel", root, statPanelColor);
        ApplyBackground(stats, statsPanelBackgroundSprite, statPanelColor);
        VerticalLayoutGroup statsGroup = stats.gameObject.AddComponent<VerticalLayoutGroup>();
        statsGroup.padding = new RectOffset(40, 96, 34, 36);
        statsGroup.spacing = 14f;
        statsGroup.childForceExpandWidth = true;
        statsGroup.childForceExpandHeight = false;

        AddHeader(stats, settlementData);
        AddStatsList(stats, settlementData.displayStats);
    }

    private Canvas CreateCanvas()
    {
        GameObject canvasGo = new GameObject("RunSettlementCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10000;

        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        return canvas;
    }

    private void AddHeader(RectTransform parent, RunSettlementData settlementData)
    {
        RectTransform header = CreateTransparent("Header", parent);
        header.gameObject.AddComponent<LayoutElement>().preferredHeight = 78f;
        HorizontalLayoutGroup group = header.gameObject.AddComponent<HorizontalLayoutGroup>();
        group.childAlignment = TextAnchor.MiddleCenter;
        group.childForceExpandWidth = false;

        TextMeshProUGUI title = AddText(header, "本局统计信息", 32, textColor, TextAlignmentOptions.Left, FontStyles.Bold, 66f, titleFont);
        LayoutElement titleLayout = title.GetComponent<LayoutElement>();
        titleLayout.preferredWidth = 360f;
        titleLayout.flexibleWidth = 0f;

        RectTransform spacer = CreateTransparent("HeaderSpacer", header);
        spacer.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

        TextMeshProUGUI duration = AddText(header, $"游戏时长 {FormatDuration(settlementData.displayStats.durationSeconds)}", 18, mutedTextColor, TextAlignmentOptions.Right, FontStyles.Normal, 66f, bodyFont);
        duration.enableWordWrapping = false;
        duration.overflowMode = TextOverflowModes.Overflow;
        LayoutElement durationLayout = duration.GetComponent<LayoutElement>();
        durationLayout.preferredWidth = 210f;
        durationLayout.flexibleWidth = 0f;
    }

    private void AddStatsList(RectTransform parent, RunSettlementDisplayStats stats)
    {
        statRows.Clear();
        RectTransform list = CreatePanel("StatsList", parent, new Color(0.05f, 0.08f, 0.11f, 0.72f));
        ApplyBackground(list, statsListBackgroundSprite, new Color(0.05f, 0.08f, 0.11f, 0.72f));
        list.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1f;

        VerticalLayoutGroup group = list.gameObject.AddComponent<VerticalLayoutGroup>();
        group.padding = new RectOffset(42, 58, 28, 26);
        group.spacing = 6f;
        group.childForceExpandWidth = true;
        group.childForceExpandHeight = false;

        AddStatListRow(list, "通过普通战斗", SumBands(stats.normalBattleOuterCleared, stats.normalBattleMidCleared, stats.normalBattleInnerCleared, stats.normalBattleCoreCleared), normalBattleIcon);
        AddStatListRow(list, "通过精英战斗", SumBands(stats.eliteBattleOuterCleared, stats.eliteBattleMidCleared, stats.eliteBattleInnerCleared, stats.eliteBattleCoreCleared), eliteBattleIcon);
        AddStatListRow(list, "通关特殊副本", stats.specialDungeonCleared, specialDungeonIcon);
        AddStatListRow(list, "探索格子数", stats.unexploredTilesVisited, exploredTilesIcon);
        AddStatListRow(list, "总金币获取量", stats.totalMoneyGained, moneyGainedIcon);
        AddStatListRow(list, "队伍人数", stats.partyMemberCount, partyMemberIcon);
        AddStatListRow(list, "遗物数量", stats.relicCount, relicIcon);
        AddStatListRow(list, "武器打造", stats.craftedWeaponCount, craftedWeaponIcon);
        AddStatListRow(list, "事件格子", stats.eventTilesCompleted, eventTileIcon);
        AddStatListRow(list, "事件战斗", stats.eventBattlesCompleted, eventBattleIcon);
        AddStatListRow(list, "获得钥匙", stats.keysCollected, keyIcon);
        AddStatListRow(list, "通过 BOSS", stats.bossesDefeated, bossIcon);
    }

    private void AddStatListRow(RectTransform parent, string label, int targetValue, Sprite icon)
    {
        RectTransform row = CreateTransparent($"{label}Row", parent);
        row.gameObject.AddComponent<LayoutElement>().preferredHeight = 50f;

        HorizontalLayoutGroup group = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        group.spacing = 12f;
        group.childAlignment = TextAnchor.MiddleCenter;
        group.childForceExpandWidth = false;
        group.childForceExpandHeight = true;

        if (icon != null)
            AddStatIcon(row, icon);

        TextMeshProUGUI labelText = AddText(row, label, 22, textColor, TextAlignmentOptions.Left, FontStyles.Bold, 50f, bodyFont);
        labelText.enableWordWrapping = false;
        LayoutElement labelLayout = labelText.GetComponent<LayoutElement>();
        labelLayout.preferredWidth = icon != null ? 226f : 272f;
        labelLayout.flexibleWidth = 0f;

        RectTransform lineSlot = CreateTransparent("LineSlot", row);
        lineSlot.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

        GameObject lineGo = new GameObject("Divider", typeof(RectTransform), typeof(Image));
        lineGo.transform.SetParent(lineSlot, false);
        RectTransform line = lineGo.GetComponent<RectTransform>();
        line.anchorMin = new Vector2(0f, 0.5f);
        line.anchorMax = new Vector2(1f, 0.5f);
        line.offsetMin = new Vector2(0f, -1f);
        line.offsetMax = new Vector2(0f, 1f);
        lineGo.GetComponent<Image>().color = new Color(goldColor.r, goldColor.g, goldColor.b, 0.38f);

        TextMeshProUGUI valueText = AddText(row, string.Empty, 28, new Color(1f, 0.9f, 0.66f, 1f), TextAlignmentOptions.Right, FontStyles.Bold, 50f, numberFont != null ? numberFont : bodyFont);
        valueText.enableWordWrapping = false;
        valueText.overflowMode = TextOverflowModes.Overflow;
        LayoutElement valueLayout = valueText.GetComponent<LayoutElement>();
        valueLayout.preferredWidth = 92f;
        valueLayout.flexibleWidth = 0f;
        statRows.Add(new SettlementValueRow(valueText, Mathf.Max(0, targetValue)));
    }

    private void AddTitleLogo(RectTransform parent)
    {
        if (titleLogoSprite == null)
        {
            AddText(parent, "骰掷大陆", 58, goldColor, TextAlignmentOptions.Center, FontStyles.Bold, titleLogoHeight, titleFont);
            return;
        }

        GameObject logoGo = new GameObject("TitleLogo", typeof(RectTransform), typeof(Image));
        logoGo.transform.SetParent(parent, false);
        LayoutElement layout = logoGo.AddComponent<LayoutElement>();
        layout.preferredHeight = titleLogoHeight;
        layout.preferredWidth = titleLogoWidth;
        layout.flexibleWidth = 0f;
        layout.flexibleHeight = 0f;

        Image image = logoGo.GetComponent<Image>();
        image.sprite = titleLogoSprite;
        image.color = Color.white;
        image.preserveAspect = true;
    }

    private void AddStatIcon(RectTransform parent, Sprite icon)
    {
        GameObject iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconGo.transform.SetParent(parent, false);

        LayoutElement layout = iconGo.AddComponent<LayoutElement>();
        float iconSize = Mathf.Max(8f, statIconSize);
        layout.preferredWidth = iconSize;
        layout.preferredHeight = iconSize;
        layout.flexibleWidth = 0f;
        layout.flexibleHeight = 0f;

        Image image = iconGo.GetComponent<Image>();
        image.sprite = icon;
        image.color = Color.white;
        image.type = Image.Type.Simple;
        image.preserveAspect = true;
    }

    private void AddScorePanel(RectTransform parent, RunSettlementData settlementData)
    {
        Color scorePanelColor = new Color(0.04f, 0.05f, 0.055f, 0.01f);
        RectTransform panel = CreatePanel("ScorePanel", parent, scorePanelColor);
        ApplyBackground(panel, scorePanelBackgroundSprite, scorePanelColor);
        panel.gameObject.AddComponent<LayoutElement>().preferredHeight = 276f;

        VerticalLayoutGroup group = panel.gameObject.AddComponent<VerticalLayoutGroup>();
        group.padding = new RectOffset(18, 18, 26, 22);
        group.spacing = 2f;
        group.childAlignment = TextAnchor.MiddleCenter;
        group.childForceExpandWidth = true;
        group.childForceExpandHeight = false;

        AddText(panel, "总得分", 20, goldColor, TextAlignmentOptions.Center, FontStyles.Bold, 26f, bodyFont);
        totalScoreText = AddText(panel, "0", 68, new Color(1f, 0.88f, 0.56f, 1f), TextAlignmentOptions.Center, FontStyles.Bold, 82f, numberFont);
        AddText(panel, "获得能力点", 20, goldColor, TextAlignmentOptions.Center, FontStyles.Bold, 28f, bodyFont);
        AddText(panel, $"+{settlementData.gainedAbilityPoints}", 40, textColor, TextAlignmentOptions.Center, FontStyles.Bold, 46f, numberFont);
        AddText(panel, $"当前能力点 {settlementData.abilityPointsAfter}", 16, mutedTextColor, TextAlignmentOptions.Center, FontStyles.Normal, 24f, bodyFont);
    }

    private void AddBossStatus(RectTransform parent, bool runCleared)
    {
        RectTransform status = CreatePanel("BossStatus", parent, new Color(0.11f, 0.13f, 0.15f, 0.94f));
        Sprite statusSprite = runCleared ? bossDefeatedBackgroundSprite : bossFailedBackgroundSprite;
        ApplyBackground(status, statusSprite, new Color(0.11f, 0.13f, 0.15f, 0.94f));
        status.gameObject.AddComponent<LayoutElement>().preferredHeight = 112f;
        TextMeshProUGUI statusText = AddText(status, runCleared ? "已击败最终威胁" : "未能击败最终威胁", 24, textColor, TextAlignmentOptions.Center, FontStyles.Bold, 112f, bodyFont);
        statusText.enableWordWrapping = false;
        statusText.overflowMode = TextOverflowModes.Overflow;
    }

    private void AddCalamityPanel(RectTransform parent, RunSettlementData settlementData)
    {
        RunSettlementDisplayStats stats = settlementData.displayStats;
        Color panelColor = settlementData.runCleared ? calamityPanelColor : new Color(0.25f, 0.13f, 0.13f, 0.94f);
        RectTransform panel = CreatePanel("CalamityPanel", parent, panelColor);
        ApplyBackground(panel, calamityBackgroundSprite, panelColor);
        panel.gameObject.AddComponent<LayoutElement>().preferredHeight = 190f;
        VerticalLayoutGroup group = panel.gameObject.AddComponent<VerticalLayoutGroup>();
        group.padding = new RectOffset(18, 18, 16, 14);
        group.spacing = 8f;
        group.childAlignment = TextAnchor.MiddleCenter;
        group.childForceExpandWidth = true;

        AddText(panel, "灾厄等级", 24, textColor, TextAlignmentOptions.Center, FontStyles.Bold, 34f, bodyFont);
        AddText(panel, $"Lv. {stats.calamityTier}", 58, Color.white, TextAlignmentOptions.Center, FontStyles.Bold, 70f, numberFont);
        AddText(panel, stats.calamityScoreCounted ? "通关计分" : "未通关不计分", 18, mutedTextColor, TextAlignmentOptions.Center, FontStyles.Normal, 30f, bodyFont);
    }

    private void AddReturnButton(RectTransform parent, RunSettlementData settlementData)
    {
        GameObject buttonGo = new GameObject("ReturnMainMenuButton", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonGo.transform.SetParent(parent, false);
        RectTransform rect = buttonGo.GetComponent<RectTransform>();
        rect.gameObject.AddComponent<LayoutElement>().preferredHeight = 72f;
        ApplyBackground(rect, returnButtonBackgroundSprite, new Color(0.2f, 0.17f, 0.12f, 1f));
        Button button = buttonGo.GetComponent<Button>();
        button.onClick.AddListener(() =>
        {
            RunSettlementRuntime.Clear();
            SceneManager.LoadScene(string.IsNullOrWhiteSpace(settlementData.returnSceneName) ? fallbackReturnSceneName : settlementData.returnSceneName);
        });

        AddText(rect, "返回主菜单", 22, textColor, TextAlignmentOptions.Center, FontStyles.Bold, 72f, buttonFont != null ? buttonFont : bodyFont);
    }

    private System.Collections.IEnumerator PlayRevealSequence()
    {
        int finalScore = data != null && data.scoreResult != null ? Mathf.Max(0, data.scoreResult.finalScore) : 0;
        yield return AnimateValue(totalScoreText, finalScore, totalScoreRevealSeconds, true);

        for (int i = 0; i < statRows.Count; i++)
        {
            float duration = statRows[i].targetValue == 0 ? zeroStatRevealSeconds : statItemRevealSeconds;
            yield return AnimateValue(statRows[i].text, statRows[i].targetValue, duration, false);
        }

    }

    private System.Collections.IEnumerator AnimateValue(TextMeshProUGUI targetText, int targetValue, float duration, bool easeOut)
    {
        if (targetText == null)
            yield break;

        skipRequested = false;
        targetText.text = "0";

        float elapsed = 0f;
        duration = Mathf.Max(0.1f, duration);
        targetValue = Mathf.Max(0, targetValue);

        while (elapsed < duration && !skipRequested)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            if (easeOut)
                progress = 1f - Mathf.Pow(1f - progress, 3f);

            int currentValue = Mathf.RoundToInt(Mathf.Lerp(0f, targetValue, progress));
            targetText.text = FormatNumber(currentValue);
            yield return null;
        }

        targetText.text = FormatNumber(targetValue);
        skipRequested = false;
    }

    private TextMeshProUGUI AddText(Transform parent, string text, float size, Color color, TextAlignmentOptions alignment, FontStyles style, float height, TMP_FontAsset font)
    {
        GameObject go = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        TextMeshProUGUI label = go.GetComponent<TextMeshProUGUI>();
        if (font != null)
            label.font = font;
        label.text = text;
        label.fontSize = size;
        label.color = color;
        label.alignment = alignment;
        label.fontStyle = style;
        label.enableWordWrapping = true;
        RectTransform rect = label.rectTransform;
        rect.gameObject.AddComponent<LayoutElement>().preferredHeight = height;
        return label;
    }

    private RectTransform CreatePanel(string name, Transform parent, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        Image image = go.GetComponent<Image>();
        image.color = color;
        return go.GetComponent<RectTransform>();
    }

    private static void ApplyBackground(RectTransform target, Sprite sprite, Color fallbackColor)
    {
        Image image = target.GetComponent<Image>();
        if (image == null)
            image = target.gameObject.AddComponent<Image>();

        image.sprite = sprite;
        image.color = Color.white;
        image.type = sprite != null ? Image.Type.Sliced : Image.Type.Simple;
        image.preserveAspect = false;

        if (sprite == null)
            image.color = fallbackColor;
    }

    private RectTransform CreateTransparent(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static string FormatNumber(int value)
    {
        return Mathf.Max(0, value).ToString("N0", System.Globalization.CultureInfo.InvariantCulture);
    }

    private static int SumBands(int outer, int mid, int inner, int core)
    {
        return outer + mid + inner + core;
    }

    private static string FormatDuration(int seconds)
    {
        seconds = Mathf.Max(0, seconds);
        return $"{seconds / 60:00}:{seconds % 60:00}";
    }

    private static RunSettlementData BuildFallbackData()
    {
        RunSettlementScoreResult result = new RunSettlementScoreResult
        {
            finalScore = 0,
            gainedAbilityPoints = 0,
            displayStats = new RunSettlementDisplayStats()
        };
        return new RunSettlementData
        {
            runCleared = false,
            scoreResult = result,
            displayStats = result.displayStats,
            returnSceneName = "Menu Selection"
        };
    }

    private readonly struct SettlementValueRow
    {
        public readonly TextMeshProUGUI text;
        public readonly int targetValue;

        public SettlementValueRow(TextMeshProUGUI text, int targetValue)
        {
            this.text = text;
            this.targetValue = targetValue;
        }
    }
}
