using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[Serializable]
public sealed class BattleSettlementEntry
{
    public string displayName;
    public int currentHP;
    public int maxHP;
    public int level;
    public bool isAlive;
    public int currentExperience;
    public int maxExperience;
    public int experienceGained;
    public int resultingHP;
    public int resultingMaxHP;
    public int resultingLevel;
    public int resultingExperience;
}

[Serializable]
public sealed class BattleSettlementLootEntry
{
    public LootItem_SO lootItem;
    public Relic_SO relicData;
    public int amount;
    public string displayName;
    public string description;
    public LootRarity rarity;
    public LootAttribute_SO attribute;
    public Sprite icon;
    public bool isRelicChoiceOption;
    public bool isRelicChoiceEntrance;
    [NonSerialized]
    public BattleSettlementRelicChoiceData relicChoiceData;
}

[Serializable]
public sealed class BattleSettlementRelicChoiceData
{
    public string title;
    public string requiredMessage;
    public RelicTable_SO sourceTable;
    public int choiceCount = 3;
    public int refreshesRemaining;
    public List<BattleSettlementLootEntry> choices = new List<BattleSettlementLootEntry>();
    [NonSerialized] public BattleSettlementLootEntry selectedChoice;
    [NonSerialized] public bool rewardApplied;

    public bool HasChoices => choices != null && choices.Count > 0;
    public LootRarity Rarity => ResolveRarity();

    private LootRarity ResolveRarity()
    {
        if (choices == null)
            return LootRarity.Common;

        for (int i = 0; i < choices.Count; i++)
        {
            BattleSettlementLootEntry choice = choices[i];
            if (choice != null)
                return choice.rarity;
        }

        return LootRarity.Common;
    }
}

[Serializable]
public sealed class BattleSettlementData
{
    public bool isVictory;
    public string title;
    public string subtitle;
    public int experienceGained;
    public string returnSceneName;
    public string retrySceneName;
    public List<BattleSettlementEntry> partyEntries = new List<BattleSettlementEntry>();
    public List<BattleSettlementLootEntry> lootEntries = new List<BattleSettlementLootEntry>();
    public BattleSettlementRelicChoiceData relicChoice;
}

[DisallowMultipleComponent]
public class BattleSettlementOverlay : MonoBehaviour
{
    private enum SettlementImageScaleMode
    {
        Sliced,
        SimpleStretch,
        PreserveAspect
    }

    [Serializable]
    private sealed class SummaryRowView
    {
        public BattleSettlementEntry entry;
        public TextMeshProUGUI hpText;
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI expText;
        public TextMeshProUGUI levelText;
        public RectTransform hpFillRect;
        public float hpFillWidth;
        public RectTransform expFillRect;
        public float expFillWidth;
    }

    [Serializable]
    private sealed class LootItemView
    {
        public BattleSettlementLootEntry entry;
        public Button button;
        public Image slotImage;
        public Image iconImage;
        public TextMeshProUGUI countText;
    }

    [Header("Victory Intro")]
    [SerializeField] private Sprite victoryIntroSprite;
    [SerializeField] private float victoryIntroScale = 1f;
    [SerializeField] private Vector2 victoryIntroOffset = Vector2.zero;
    [SerializeField] private Color victoryIntroColor = Color.white;
    [SerializeField] private float victoryIntroStartScale = 1.45f;
    [SerializeField] private float victoryIntroEndScale = 1f;
    [SerializeField] private float victoryIntroFadeDuration = 0.22f;
    [SerializeField] private float victoryIntroHoldDuration = 0.5f;

    [Header("Canvas")]
    [SerializeField] private int canvasSortOrder = 9500;
    [SerializeField] private string mainMenuSceneName = BattleExitFlowService.DefaultMainMenuSceneName;

    [Header("Run Settlement")]
    [SerializeField] private string runSettlementSceneName = RunSettlementFlowService.DefaultSettlementSceneName;
    [SerializeField] private RunScoreConfig_SO runScoreConfig;

    [Header("Panel")]
    [SerializeField] private Vector2 panelSize = new Vector2(900f, 620f);
    [SerializeField] private Color backdropColor = new Color(0f, 0f, 0f, 0.72f);
    [SerializeField] private Color panelColor = new Color(0.08f, 0.11f, 0.16f, 0.96f);
    [SerializeField] private Sprite panelBackgroundSprite;
    [SerializeField] private Vector2 panelPadding = new Vector2(36f, 32f);
    [SerializeField] private float rowSpacing = 14f;

    [Header("Fonts")]
    [SerializeField] private TMP_FontAsset titleFont;
    [SerializeField] private TMP_FontAsset bodyFont;
    [SerializeField] private TMP_FontAsset buttonFont;
    [SerializeField] private float titleFontSize = 44f;
    [SerializeField] private float subtitleFontSize = 24f;
    [SerializeField] private float bodyFontSize = 24f;
    [SerializeField] private float rowFontSize = 22f;
    [SerializeField] private float buttonFontSize = 24f;

    [Header("Colors")]
    [SerializeField] private Color titleColor = Color.white;
    [SerializeField] private Color titleOutlineColor = new Color(0.24f, 0.1f, 0.02f, 1f);
    [SerializeField, Range(0f, 1f)] private float titleOutlineWidth = 0.24f;
    [SerializeField] private Color subtitleColor = new Color(0.86f, 0.92f, 1f, 0.95f);
    [SerializeField] private Color expColor = new Color(1f, 0.86f, 0.32f, 1f);
    [SerializeField] private Color rowColor = new Color(0.92f, 0.96f, 1f, 1f);
    [SerializeField] private Color deadRowColor = new Color(1f, 0.55f, 0.55f, 1f);
    [SerializeField] private Color hpTextColor = new Color(1f, 0.32f, 0.32f, 1f);
    [SerializeField] private Color buttonColor = new Color(0.18f, 0.24f, 0.34f, 1f);
    [SerializeField] private Color buttonTextColor = Color.white;

    [Header("Buttons")]
    [SerializeField] private Vector2 buttonSize = new Vector2(220f, 62f);
    [SerializeField] private float buttonSpacing = 24f;
    [SerializeField] private Vector2 buttonRootOffset = Vector2.zero;
    [SerializeField] private float buttonBottomInset = 26f;
    [SerializeField] private float buttonReservedHeight = 120f;
    [SerializeField] private Sprite buttonBackgroundSprite;
    [SerializeField] private bool showRetryButton = true;
    [SerializeField] private string returnButtonLabel = "Return To Map";
    [SerializeField] private string defeatReturnButtonLabel = "Abandon Battle";
    [SerializeField] private string retryButtonLabel = "Retry Battle";

    [Header("Experience Rows")]
    [SerializeField] private Vector2 expBarSize = new Vector2(420f, 14f);
    [SerializeField] private Color expBarBackgroundColor = new Color(1f, 1f, 1f, 0.12f);
    [SerializeField] private Color expBarFillColor = new Color(1f, 0.84f, 0.26f, 1f);
    [SerializeField] private Color expBarBorderColor = new Color(1f, 1f, 1f, 0.4f);
    [SerializeField] private float expFontSize = 20f;
    [SerializeField] private Color expTextColor = new Color(1f, 0.92f, 0.7f, 1f);
    [SerializeField] private float expAnimationDurationPerFill = 0.45f;
    [SerializeField] private float hpAnimationDuration = 0.42f;
    [SerializeField] private float expLevelUpPauseDuration = 0.14f;

    [Header("Summary Row Layout")]
    [SerializeField] private float summaryRowLeftIndent = 0f;
    [SerializeField] private float summaryRowHeight = 40f;
    [SerializeField] private float summaryNameWidth = 170f;
    [SerializeField] private float summaryHpWidth = 112f;
    [SerializeField] private float summaryHpBarWidth = 130f;
    [SerializeField] private float summaryExpTextWidth = 112f;
    [SerializeField] private float summaryExpBarWidth = 140f;
    [SerializeField] private float summaryLevelWidth = 60f;
    [SerializeField] private Color hpBarBackgroundColor = new Color(0.34f, 0.08f, 0.08f, 0.5f);
    [SerializeField] private Color hpBarFillColor = new Color(1f, 0.28f, 0.28f, 1f);
    [SerializeField] private Color hpBarBorderColor = new Color(1f, 0.38f, 0.38f, 0.55f);

    [Header("Loot Display")]
    [SerializeField] private Vector2 lootSlotSize = new Vector2(72f, 72f);
    [SerializeField] private Vector2 lootIconSize = new Vector2(48f, 48f);
    [SerializeField] private Vector2 lootDetailIconSize = new Vector2(112f, 112f);
    [SerializeField] private Vector2 lootDetailPanelSize = new Vector2(640f, 360f);
    [SerializeField] private float lootGridSpacing = 12f;
    [SerializeField] private int lootGridColumnCount = 6;
    [SerializeField] private float lootTitleFontSize = 22f;
    [SerializeField] private float lootBodyFontSize = 20f;
    [SerializeField] private Color lootTitleColor = new Color(1f, 0.9f, 0.52f, 1f);
    [SerializeField] private Color lootTextColor = new Color(0.95f, 0.95f, 0.95f, 1f);
    [SerializeField] private Color lootCountColor = Color.white;
    [SerializeField] private Color lootSlotColor = new Color(0.18f, 0.15f, 0.12f, 0.95f);
    [SerializeField] private Color lootSelectedSlotColor = new Color(0.42f, 0.32f, 0.16f, 1f);
    [SerializeField] private Color lootDetailPanelColor = new Color(0.12f, 0.09f, 0.07f, 0.95f);
    [SerializeField] private Sprite lootSlotBackgroundSprite;
    [SerializeField] private Sprite lootDetailBackgroundSprite;
    [SerializeField] private Sprite lootFallbackSprite;

    [Header("Relic Choice")]
    [SerializeField] private string relicChoiceHeaderText = "选择 1 个遗物";
    [SerializeField] private string relicChoiceRequiredMessage = "返回前请选择一个遗物。";
    [SerializeField] private string relicChoiceEntranceName = "精英遗物奖励";
    [SerializeField] private string relicChoiceEntranceDescription = "点击以选择一个遗物奖励。";
    [SerializeField] private string relicChoiceEntranceLabel = "?";
    [SerializeField] private string relicChoiceSelectedPrefix = "已选择";
    [SerializeField] private string relicChoiceConfirmLabel = "确认";
    [SerializeField] private string relicChoiceRefreshLabel = "刷新";
    [SerializeField] private string relicChoiceDetailEmptyText = "悬停遗物查看效果；点击遗物进行选择，再次点击可取消选择。";
    [SerializeField] private Color relicChoiceSlotColor = new Color(0.16f, 0.22f, 0.32f, 0.95f);
    [SerializeField] private Color relicChoiceSelectedSlotColor = new Color(0.36f, 0.52f, 0.86f, 1f);
    [SerializeField] private Color relicChoiceRequiredColor = new Color(1f, 0.45f, 0.38f, 1f);
    [SerializeField] private Sprite relicChoiceSlotBackgroundSprite;
    [SerializeField] private Sprite relicChoiceCommonQuestionSprite;
    [SerializeField] private Sprite relicChoiceUncommonQuestionSprite;
    [SerializeField] private Sprite relicChoiceRareQuestionSprite;
    [SerializeField] private Sprite relicChoiceEpicQuestionSprite;
    [SerializeField] private Sprite relicChoiceLegendaryQuestionSprite;

    [Header("Relic Choice Modal")]
    [SerializeField] private Vector2 relicChoiceModalSize = new Vector2(1020f, 620f);
    [SerializeField] private Vector2 relicChoiceCardSize = new Vector2(260f, 340f);
    [SerializeField] private Vector2 relicChoiceModalIconSize = new Vector2(94f, 94f);
    [SerializeField] private float relicChoiceModalSpacing = 24f;
    [SerializeField] private Vector2 relicChoiceTooltipSize = new Vector2(600f, 320f);
    [SerializeField] private Vector2 relicChoiceTooltipOffset = new Vector2(24f, 28f);
    [SerializeField] private SettlementImageScaleMode relicChoiceModalBackgroundScaleMode = SettlementImageScaleMode.PreserveAspect;
    [SerializeField] private SettlementImageScaleMode relicChoiceCardBackgroundScaleMode = SettlementImageScaleMode.PreserveAspect;
    [SerializeField] private SettlementImageScaleMode relicChoiceTooltipBackgroundScaleMode = SettlementImageScaleMode.Sliced;
    [SerializeField] private SettlementImageScaleMode relicChoiceEntranceSlotBackgroundScaleMode = SettlementImageScaleMode.Sliced;
    [SerializeField] private Color relicChoiceModalBackdropColor = new Color(0f, 0f, 0f, 0.42f);
    [SerializeField] private Color relicChoiceModalPanelColor = new Color(0.08f, 0.1f, 0.14f, 0.98f);
    [SerializeField] private Color relicChoiceCardColor = new Color(0.14f, 0.16f, 0.2f, 0.96f);
    [SerializeField] private Color relicChoiceHoveredCardColor = new Color(0.24f, 0.27f, 0.33f, 0.98f);
    [SerializeField] private Color relicChoiceDetailPanelColor = new Color(0.1f, 0.08f, 0.06f, 0.98f);
    [SerializeField] private Sprite relicChoiceModalBackgroundSprite;
    [SerializeField] private Sprite relicChoiceCardBackgroundSprite;
    [SerializeField] private Sprite relicChoiceDetailBackgroundSprite;

    private Canvas overlayCanvas;
    private RectTransform overlayRoot;
    private Image backdropImage;
    private RectTransform panelRoot;
    private Image panelImage;
    private TextMeshProUGUI titleText;
    private TextMeshProUGUI subtitleText;
    private TextMeshProUGUI expText;
    private RectTransform summaryRoot;
    private RectTransform lootRoot;
    private RectTransform contentFlexSpacerRoot;
    private RectTransform buttonReserveRoot;
    private TextMeshProUGUI lootHeaderText;
    private RectTransform lootGridRoot;
    private RectTransform lootDetailModalRoot;
    private Image lootDetailModalBackdropImage;
    private MapInventoryDetailView lootDetailView;
    private RectTransform relicChoiceModalRoot;
    private Image relicChoiceModalBackdropImage;
    private Image relicChoiceModalPanelImage;
    private TextMeshProUGUI relicChoiceModalTitleText;
    private RectTransform relicChoiceCardsRoot;
    private RectTransform relicChoiceTooltipRoot;
    private Image relicChoiceDetailPanelImage;
    private Image relicChoiceDetailIcon;
    private TextMeshProUGUI relicChoiceDetailNameText;
    private TextMeshProUGUI relicChoiceDetailMetaText;
    private TextMeshProUGUI relicChoiceDetailDescriptionText;
    private TextMeshProUGUI relicChoiceDetailHintText;
    private RectTransform relicChoiceModalButtonRoot;
    private Button relicChoiceRefreshButton;
    private TextMeshProUGUI relicChoiceRefreshButtonText;
    private RectTransform buttonRoot;
    private RectTransform victoryIntroRoot;
    private Image victoryIntroImage;
    private readonly List<GameObject> spawnedRows = new List<GameObject>();
    private readonly List<Button> spawnedButtons = new List<Button>();
    private readonly List<SummaryRowView> rowViews = new List<SummaryRowView>();
    private readonly List<GameObject> spawnedLootObjects = new List<GameObject>();
    private readonly List<LootItemView> lootViews = new List<LootItemView>();
    private readonly List<GameObject> spawnedRelicChoiceObjects = new List<GameObject>();
    private readonly List<LootItemView> relicChoiceViews = new List<LootItemView>();
    private LootItemView currentSelectedLootView;
    private LootItemView hoveredRelicChoiceView;
    private LootItemView selectedRelicChoiceView;
    private BattleSettlementRelicChoiceData activeRelicChoiceData;
    private Coroutine showRoutine;
    private BattleManager battleManager;
    private Sprite runtimeFallbackLootSprite;

    public bool IsVisible => overlayCanvas != null && overlayCanvas.gameObject.activeInHierarchy;

    private void Awake()
    {
        battleManager = GetComponent<BattleManager>();
        if (battleManager == null)
            battleManager = FindObjectOfType<BattleManager>();

        EnsureEventSystem();
        EnsureCanvas();
        SetVisible(false);
    }

    private void OnDestroy()
    {
        if (overlayCanvas != null)
            Destroy(overlayCanvas.gameObject);
    }

    private void Update()
    {
        if (relicChoiceTooltipRoot != null && relicChoiceTooltipRoot.gameObject.activeSelf)
            PositionRelicChoiceTooltip(GetCurrentPointerScreenPosition());
    }

    public void ShowResult(BattleSettlementData data)
    {
        if (data == null)
            return;

        EnsureEventSystem();
        EnsureCanvas();
        ClearRows();
        ClearLootViews();
        ClearRelicChoiceModalViews();
        ClearButtons();
        rowViews.Clear();
        lootViews.Clear();
        relicChoiceViews.Clear();
        currentSelectedLootView = null;
        hoveredRelicChoiceView = null;
        selectedRelicChoiceView = null;
        activeRelicChoiceData = data.relicChoice;
        SetRelicChoiceModalVisible(false);
        HideLootDetailModal();

        titleText.text = string.IsNullOrWhiteSpace(data.title)
            ? (data.isVictory ? "Victory" : "Defeat")
            : data.title;
        subtitleText.text = data.subtitle ?? string.Empty;
        expText.text = $"EXP +{Mathf.Max(0, data.experienceGained)}";

        BuildSummaryRows(data.partyEntries);
        BuildLootSection(data);
        BuildButtons(data);
        LayoutRebuilder.ForceRebuildLayoutImmediate(panelRoot);
        ApplyRuntimeLayoutAdjustments();

        if (showRoutine != null)
            StopCoroutine(showRoutine);
        showRoutine = StartCoroutine(ShowResultRoutine(data));
    }

    public void HideResult()
    {
        if (showRoutine != null)
        {
            StopCoroutine(showRoutine);
            showRoutine = null;
        }

        SetVictoryIntroVisible(false);
        SetPanelVisible(true);
        SetVisible(false);
        activeRelicChoiceData = null;
        hoveredRelicChoiceView = null;
        selectedRelicChoiceView = null;
        currentSelectedLootView = null;
        SetRelicChoiceModalVisible(false);
    }

    private IEnumerator ShowResultRoutine(BattleSettlementData data)
    {
        SetVisible(true);

        bool playVictoryIntro = data.isVictory && victoryIntroSprite != null;
        if (playVictoryIntro)
        {
            SetPanelVisible(false);
            yield return PlayVictoryIntro();
        }

        SetVictoryIntroVisible(false);
        SetPanelVisible(true);
        yield return AnimateSummaryRows();
        showRoutine = null;
    }

    private void BuildSummaryRows(IReadOnlyList<BattleSettlementEntry> entries)
    {
        if (entries == null)
            return;

        for (int i = 0; i < entries.Count; i++)
        {
            BattleSettlementEntry entry = entries[i];
            if (entry == null)
                continue;

            GameObject rowGo = new GameObject($"SummaryRow_{i}", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            rowGo.transform.SetParent(summaryRoot, false);
            spawnedRows.Add(rowGo);

            HorizontalLayoutGroup layout = rowGo.GetComponent<HorizontalLayoutGroup>();
            layout.childControlWidth = true;
            layout.childForceExpandWidth = false;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = false;
            layout.spacing = 12f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            AddLayoutElement(
                rowGo.GetComponent<RectTransform>(),
                panelSize.x - panelPadding.x * 2f,
                panelSize.x - panelPadding.x * 2f,
                0f,
                summaryRowHeight,
                summaryRowHeight);

            CreateSpacer(rowGo.transform, summaryRowLeftIndent);

            TextMeshProUGUI nameText = CreateLabel(
                rowGo.transform,
                "NameText",
                rowFontSize,
                entry.isAlive ? rowColor : deadRowColor,
                BuildSettlementName(entry));
            nameText.alignment = TextAlignmentOptions.MidlineRight;
            nameText.enableWordWrapping = false;
            nameText.overflowMode = TextOverflowModes.Ellipsis;
            AddLayoutElement(nameText.rectTransform, summaryNameWidth, summaryNameWidth, 0f, summaryRowHeight, summaryRowHeight);

            TextMeshProUGUI hpText = CreateLabel(
                rowGo.transform,
                "HpText",
                rowFontSize - 2f,
                entry.isAlive ? hpTextColor : deadRowColor,
                entry.isAlive
                    ? $"生命 {entry.currentHP} / {entry.maxHP}"
                    : $"倒下 {entry.currentHP} / {entry.maxHP}");
            hpText.alignment = TextAlignmentOptions.MidlineLeft;
            hpText.text = entry.isAlive
                ? $"HP {entry.currentHP} / {entry.maxHP}"
                : $"倒下 {entry.currentHP} / {entry.maxHP}";
            hpText.enableWordWrapping = false;
            hpText.overflowMode = TextOverflowModes.Ellipsis;
            AddLayoutElement(hpText.rectTransform, summaryHpWidth, summaryHpWidth, 0f, summaryRowHeight, summaryRowHeight);

            RectTransform hpBarRoot = CreateStatusBar(
                rowGo.transform,
                "HpBar",
                hpBarBorderColor,
                hpBarBackgroundColor,
                hpBarFillColor,
                summaryHpBarWidth,
                expBarSize.y,
                out RectTransform hpFillRect,
                out float hpFillWidth);
            SetBarFill(hpFillRect, hpFillWidth, ResolveHpRatio(entry.currentHP, entry.maxHP));

            TextMeshProUGUI expText = CreateLabel(
                rowGo.transform,
                "ExpText",
                expFontSize,
                expTextColor,
                BuildExpLabel(entry.currentExperience, ResolveExpMax(entry.level, entry.maxExperience)));
            expText.alignment = TextAlignmentOptions.MidlineLeft;
            expText.enableWordWrapping = false;
            expText.overflowMode = TextOverflowModes.Ellipsis;
            AddLayoutElement(expText.rectTransform, summaryExpTextWidth, summaryExpTextWidth, 0f, summaryRowHeight, summaryRowHeight);

            RectTransform expBarRoot = CreateExpBar(rowGo.transform, out RectTransform expFillRect, out float expFillWidth);
            SetBarFill(expFillRect, expFillWidth, ResolveExpRatio(entry.currentExperience, ResolveExpMax(entry.level, entry.maxExperience)));

            TextMeshProUGUI levelText = CreateLabel(
                rowGo.transform,
                "LevelText",
                rowFontSize,
                entry.isAlive ? rowColor : deadRowColor,
                $"Lv.{Mathf.Max(1, entry.level)}");
            levelText.alignment = TextAlignmentOptions.MidlineRight;
            AddLayoutElement(levelText.rectTransform, summaryLevelWidth, summaryLevelWidth, 0f, summaryRowHeight, summaryRowHeight);

            rowViews.Add(new SummaryRowView
            {
                entry = entry,
                hpText = hpText,
                nameText = nameText,
                expText = expText,
                levelText = levelText,
                hpFillRect = hpFillRect,
                hpFillWidth = hpFillWidth,
                expFillRect = expFillRect,
                expFillWidth = expFillWidth
            });
        }
    }

    private void BuildLootSection(BattleSettlementData data)
    {
        if (lootHeaderText == null || lootGridRoot == null)
            return;

        ClearLootViews();
        lootViews.Clear();
        currentSelectedLootView = null;
        selectedRelicChoiceView = null;

        IReadOnlyList<BattleSettlementLootEntry> entries = data != null ? data.lootEntries : null;
        BattleSettlementRelicChoiceData choiceData = data != null ? data.relicChoice : null;
        bool hasRelicChoice = choiceData != null && choiceData.HasChoices;
        int count = entries != null ? entries.Count : 0;
        int displayCount = count + (hasRelicChoice ? 1 : 0);
        lootHeaderText.color = lootTitleColor;
        lootHeaderText.text = BuildLootHeaderText(displayCount);
        UpdateLootGridLayout(displayCount);

        if (displayCount <= 0)
        {
            HideLootDetailModal();
            return;
        }

        if (hasRelicChoice)
        {
            LootItemView choiceEntranceView = CreateLootItemView(CreateRelicChoiceEntranceEntry(choiceData));
            if (choiceEntranceView != null)
                lootViews.Add(choiceEntranceView);
        }

        for (int i = 0; i < count; i++)
        {
            BattleSettlementLootEntry entry = entries[i];
            if (entry == null)
                continue;

            LootItemView view = CreateLootItemView(entry);
            if (view != null)
                lootViews.Add(view);
        }

        HideLootDetailModal();
    }

    private string BuildLootHeaderText(int displayCount)
    {
        return displayCount > 0 ? $"战利品 x{displayCount}" : "战利品";
    }

    private BattleSettlementLootEntry CreateRelicChoiceEntranceEntry(BattleSettlementRelicChoiceData choiceData)
    {
        LootRarity rarity = choiceData != null ? choiceData.Rarity : LootRarity.Common;
        return new BattleSettlementLootEntry
        {
            amount = 1,
            displayName = relicChoiceEntranceName,
            description = relicChoiceEntranceDescription,
            rarity = rarity,
            icon = GetRelicChoiceQuestionSprite(rarity),
            isRelicChoiceEntrance = true,
            relicChoiceData = choiceData
        };
    }

    private void UpdateLootGridLayout(int itemCount)
    {
        if (lootGridRoot == null)
            return;

        int safeItemCount = Mathf.Max(0, itemCount);
        int maxColumns = Mathf.Max(1, lootGridColumnCount);
        int columns = safeItemCount > 0 ? Mathf.Clamp(safeItemCount, 1, maxColumns) : 1;
        int rows = safeItemCount > 0 ? Mathf.CeilToInt(safeItemCount / (float)columns) : 1;
        float width = columns * lootSlotSize.x + Mathf.Max(0, columns - 1) * lootGridSpacing;
        float height = rows * lootSlotSize.y + Mathf.Max(0, rows - 1) * lootGridSpacing;

        GridLayoutGroup gridLayout = lootGridRoot.GetComponent<GridLayoutGroup>();
        if (gridLayout != null)
            gridLayout.constraintCount = columns;

        AddLayoutElement(
            lootGridRoot,
            width,
            width,
            0f,
            height,
            height);
    }

    private LootItemView CreateLootItemView(BattleSettlementLootEntry entry)
    {
        GameObject slotGo = new GameObject($"Loot_{entry.displayName}", typeof(RectTransform), typeof(Image), typeof(Button), typeof(EventTrigger));
        slotGo.transform.SetParent(lootGridRoot, false);
        spawnedLootObjects.Add(slotGo);

        Image slotImage = slotGo.GetComponent<Image>();
        slotImage.color = entry.isRelicChoiceEntrance ? relicChoiceSlotColor : lootSlotColor;
        Sprite slotSprite = entry.isRelicChoiceEntrance && relicChoiceSlotBackgroundSprite != null
            ? relicChoiceSlotBackgroundSprite
            : lootSlotBackgroundSprite;
        slotImage.sprite = slotSprite;
        slotImage.type = slotSprite != null ? Image.Type.Sliced : Image.Type.Simple;
        if (entry.isRelicChoiceEntrance)
            ApplyImageScaleMode(slotImage, relicChoiceEntranceSlotBackgroundScaleMode);

        RectTransform slotRect = slotGo.GetComponent<RectTransform>();
        slotRect.sizeDelta = lootSlotSize;

        Image iconImage = CreateIconImage(slotGo.transform, "Icon", entry.icon != null ? entry.icon : GetFallbackLootSprite());
        RectTransform iconRect = iconImage.rectTransform;
        iconRect.anchorMin = new Vector2(0.5f, 0.5f);
        iconRect.anchorMax = new Vector2(0.5f, 0.5f);
        iconRect.pivot = new Vector2(0.5f, 0.5f);
        iconRect.sizeDelta = lootIconSize;
        iconRect.anchoredPosition = Vector2.zero;

        TextMeshProUGUI countText = CreateLabel(
            slotGo.transform,
            "Count",
            lootBodyFontSize,
            lootCountColor,
            entry.isRelicChoiceEntrance ? relicChoiceEntranceLabel : $"x{Mathf.Max(1, entry.amount)}");
        RectTransform countRect = countText.rectTransform;
        countRect.anchorMin = new Vector2(1f, 0f);
        countRect.anchorMax = new Vector2(1f, 0f);
        countRect.pivot = new Vector2(1f, 0f);
        countRect.anchoredPosition = new Vector2(-6f, 6f);
        countText.alignment = TextAlignmentOptions.BottomRight;
        countText.raycastTarget = false;

        Button button = slotGo.GetComponent<Button>();
        LootItemView view = new LootItemView
        {
            entry = entry,
            button = button,
            slotImage = slotImage,
            iconImage = iconImage,
            countText = countText
        };

        button.onClick.AddListener(() =>
        {
            if (entry.isRelicChoiceEntrance)
            {
                OpenRelicChoiceModal(entry.relicChoiceData);
                return;
            }

            if (currentSelectedLootView == view)
                HideLootDetailModal();
            else
                ShowLootDetailModal(entry, view);
        });

        RefreshLootItemViewPresentation(view);
        return view;
    }

    private void ShowLootDetailModal(BattleSettlementLootEntry entry, LootItemView selectedView)
    {
        currentSelectedLootView = selectedView;
        UpdateLootSlotColors();

        if (entry == null || lootDetailView == null)
        {
            HideLootDetailModal();
            return;
        }

        BattleSettlementLootEntry detailEntry = ResolvePresentedLootEntry(entry);
        if (detailEntry == null)
        {
            HideLootDetailModal();
            return;
        }

        if (detailEntry.relicData != null)
        {
            lootDetailView.Show(new RunRelicState
            {
                relicData = detailEntry.relicData,
                amount = Mathf.Max(1, detailEntry.amount)
            }, GetFallbackLootSprite());
        }
        else if (detailEntry.lootItem != null)
        {
            lootDetailView.Show(new RunLootState
            {
                lootItem = detailEntry.lootItem,
                amount = Mathf.Max(1, detailEntry.amount)
            }, GetFallbackLootSprite());
        }
        else
        {
            HideLootDetailModal();
            return;
        }

        if (lootDetailModalRoot != null)
            lootDetailModalRoot.gameObject.SetActive(true);
    }

    private void HideLootDetailModal()
    {
        currentSelectedLootView = null;
        if (lootDetailView != null)
            lootDetailView.Hide();
        if (lootDetailModalRoot != null)
            lootDetailModalRoot.gameObject.SetActive(false);
        UpdateLootSlotColors();
    }

    private void UpdateLootSlotColors()
    {
        for (int i = 0; i < lootViews.Count; i++)
        {
            LootItemView view = lootViews[i];
            if (view == null || view.slotImage == null)
                continue;

            RefreshLootItemViewPresentation(view);

            if (view.entry != null && view.entry.isRelicChoiceEntrance && view.entry.relicChoiceData != null && view.entry.relicChoiceData.selectedChoice != null)
                view.slotImage.color = relicChoiceSelectedSlotColor;
            else if (view == currentSelectedLootView)
                view.slotImage.color = lootSelectedSlotColor;
            else if (view.entry != null && view.entry.isRelicChoiceEntrance)
                view.slotImage.color = relicChoiceSlotColor;
            else
                view.slotImage.color = lootSlotColor;
        }
    }

    private void RefreshLootItemViewPresentation(LootItemView view)
    {
        if (view == null || view.entry == null)
            return;

        BattleSettlementLootEntry presentationEntry = ResolvePresentedLootEntry(view.entry);
        if (view.iconImage != null)
            SetIconSprite(view.iconImage, presentationEntry != null && presentationEntry.icon != null ? presentationEntry.icon : GetFallbackLootSprite());

        if (view.countText != null)
        {
            if (view.entry.isRelicChoiceEntrance && view.entry.relicChoiceData != null && view.entry.relicChoiceData.selectedChoice == null)
                view.countText.text = relicChoiceEntranceLabel;
            else
                view.countText.text = $"x{Mathf.Max(1, presentationEntry != null ? presentationEntry.amount : view.entry.amount)}";
        }
    }

    private BattleSettlementLootEntry ResolvePresentedLootEntry(BattleSettlementLootEntry entry)
    {
        if (entry != null && entry.isRelicChoiceEntrance && entry.relicChoiceData != null && entry.relicChoiceData.selectedChoice != null)
            return entry.relicChoiceData.selectedChoice;

        return entry;
    }

    private void BuildButtons(BattleSettlementData data)
    {
        if (data != null && data.isVictory && showRetryButton)
        {
            CreateRetryButton(data);
            CreateReturnButton(data);
            return;
        }

        CreateReturnButton(data);

        if (showRetryButton)
            CreateRetryButton(data);
    }

    private void CreateReturnButton(BattleSettlementData data)
    {
        CreateActionButton(
            buttonRoot,
            "ReturnButton",
            data.isVictory ? returnButtonLabel : defeatReturnButtonLabel,
            () =>
            {
                if (data != null && !data.isVictory && !ShouldReturnToMapAfterDefeat())
                    HandleAbandonToMainMenu();
                else
                    HandleReturnToMap(data);
            });
    }

    private void CreateRetryButton(BattleSettlementData data)
    {
        CreateActionButton(
            buttonRoot,
            "RetryButton",
            retryButtonLabel,
            () => HandleRetryBattle(data != null ? data.retrySceneName : null));
    }

    private void HandleReturnToMap(BattleSettlementData data)
    {
        if (!TryApplyRequiredRelicChoice(data))
            return;

        BattleEncounterData activeEncounter = BattleEncounterRuntime.ActiveEncounter;
        string sceneName = data != null ? data.returnSceneName : null;
        string targetScene = !string.IsNullOrWhiteSpace(sceneName)
            ? sceneName
            : "MapScene";

        if (activeEncounter != null && data != null && data.isVictory && activeEncounter.tileType == MapTileType.Boss && !activeEncounter.isTutorialEncounter)
        {
            MapRunRuntime.ApplyEncounterResult(activeEncounter);
            if (MapRunRuntime.TryGetSnapshot(activeEncounter.sourceSceneName, out MapRunSnapshot snapshot))
            {
                ExecuteRunSettlement(snapshot.runState, true, activeEncounter.sourceSceneName);
                return;
            }
        }

        if (activeEncounter != null && activeEncounter.isTutorialEncounter && activeEncounter.returnToMainMenuOnVictory)
        {
            MapRunRuntime.Clear(activeEncounter.sourceSceneName);
            MainMenuRuntime.ClearTutorialRequest();
            TutorialBattleRuntime.Clear();
        }
        else
        {
            MapRunRuntime.ApplyEncounterResult(activeEncounter);
            TutorialBattleRuntime.Clear();
        }

        BattleEncounterRuntime.CompleteActiveEncounter();
        SpeedButton.CombatLocked = false;
        HideResult();
        SceneCurtainTransitionController.Instance.TransitionToScene(targetScene);
    }

    private bool TryApplyRequiredRelicChoice(BattleSettlementData data)
    {
        BattleSettlementRelicChoiceData choiceData = data != null ? data.relicChoice : null;
        if (choiceData == null || !choiceData.HasChoices || !data.isVictory)
            return true;

        if (!BattleRelicChoiceSelectionService.TryApply(BattleEncounterRuntime.ActiveEncounter, choiceData, out _))
        {
            ShowRelicChoiceRequiredMessage(choiceData);
            return false;
        }
        return true;
    }

    private void ShowRelicChoiceRequiredMessage(BattleSettlementRelicChoiceData choiceData)
    {
        if (lootHeaderText == null)
            return;

        lootHeaderText.color = relicChoiceRequiredColor;
        lootHeaderText.text = choiceData != null && !string.IsNullOrWhiteSpace(choiceData.requiredMessage)
            ? choiceData.requiredMessage
            : relicChoiceRequiredMessage;
        OpenRelicChoiceModal(choiceData);
        if (relicChoiceModalTitleText != null)
        {
            relicChoiceModalTitleText.color = relicChoiceRequiredColor;
            relicChoiceModalTitleText.text = choiceData != null && !string.IsNullOrWhiteSpace(choiceData.requiredMessage)
                ? choiceData.requiredMessage
                : relicChoiceRequiredMessage;
        }
    }

    private void HandleRetryBattle(string sceneName)
    {
        string targetScene = !string.IsNullOrWhiteSpace(sceneName)
            ? sceneName
            : SceneManager.GetActiveScene().name;

        BattleEncounterRuntime.RequeueActiveEncounter();
        TutorialBattleRuntime.Clear();
        SpeedButton.CombatLocked = false;
        HideResult();
        SceneCurtainTransitionController.Instance.TransitionToScene(targetScene);
    }

    private static bool ShouldReturnToMapAfterDefeat()
    {
        BattleEncounterData activeEncounter = BattleEncounterRuntime.ActiveEncounter;
        return activeEncounter != null && !activeEncounter.wasVictory && activeEncounter.returnToMapOnDefeat;
    }

    private void HandleAbandonToMainMenu()
    {
        BattleEncounterData activeEncounter = BattleEncounterRuntime.ActiveEncounter;
        RunState settlementRunState = null;
        if (activeEncounter != null && MapRunRuntime.TryGetSnapshot(activeEncounter.sourceSceneName, out MapRunSnapshot snapshot))
        {
            settlementRunState = snapshot.runState;
            if (settlementRunState != null && activeEncounter.resolvedPartyMembers != null && activeEncounter.resolvedPartyMembers.Count > 0)
            {
                settlementRunState.partyMembers = new List<RunPartyMemberState>(activeEncounter.resolvedPartyMembers.Count);
                for (int i = 0; i < activeEncounter.resolvedPartyMembers.Count; i++)
                {
                    RunPartyMemberState member = activeEncounter.resolvedPartyMembers[i];
                    if (member != null && !member.isTemporaryBattleGuest)
                        settlementRunState.partyMembers.Add(member.Clone());
                }
                settlementRunState.SyncLegacyPlayerFieldsFromParty();
            }
        }

        if (settlementRunState != null)
        {
            ExecuteRunSettlement(settlementRunState, false, activeEncounter != null ? activeEncounter.sourceSceneName : null);
            return;
        }

        BattleExitFlowService.ExecuteAbandonToMainMenu(
            mainMenuSceneName,
            sceneName => SceneCurtainTransitionController.Instance.TransitionToScene(sceneName),
            HideResult);
    }

    private void ExecuteRunSettlement(RunState settlementRunState, bool runCleared, string sourceSceneName)
    {
        RunSettlementFlowService.ExecuteSettlement(new RunSettlementFlowRequest
        {
            runState = settlementRunState,
            scoreConfig = runScoreConfig,
            runCleared = runCleared,
            settlementSceneName = runSettlementSceneName,
            returnSceneName = mainMenuSceneName,
            deleteSavedRun = MainMenuRunSaveSystem.DeleteSavedRun,
            clearRuntimeState = () =>
            {
                MapRunRuntime.Clear(sourceSceneName);
                BattleEncounterRuntime.Clear();
                MainMenuRuntime.ClearTutorialRequest();
                TutorialBattleRuntime.Clear();
                SpeedButton.CombatLocked = false;
                SpeedButton.InputSuppressed = false;
                HideResult();
            },
            transitionToScene = sceneName => SceneCurtainTransitionController.Instance.TransitionToScene(sceneName)
        });
    }

    private void EnsureCanvas()
    {
        if (overlayCanvas != null)
            return;

        GameObject canvasGo = new GameObject("BattleSettlementCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.transform.SetParent(null, false);
        canvasGo.transform.localPosition = Vector3.zero;
        canvasGo.transform.localRotation = Quaternion.identity;
        canvasGo.transform.localScale = Vector3.one;

        overlayCanvas = canvasGo.GetComponent<Canvas>();
        overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        overlayCanvas.sortingOrder = canvasSortOrder;
        overlayCanvas.pixelPerfect = true;

        GraphicRaycaster raycaster = canvasGo.GetComponent<GraphicRaycaster>();
        if (raycaster != null)
        {
            raycaster.ignoreReversedGraphics = true;
            raycaster.blockingObjects = GraphicRaycaster.BlockingObjects.None;
        }

        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        overlayRoot = canvasGo.GetComponent<RectTransform>();
        overlayRoot.anchorMin = Vector2.zero;
        overlayRoot.anchorMax = Vector2.one;
        overlayRoot.offsetMin = Vector2.zero;
        overlayRoot.offsetMax = Vector2.zero;

        backdropImage = CreateImage(overlayRoot, "Backdrop", backdropColor, null);
        RectTransform backdropRect = backdropImage.rectTransform;
        backdropRect.anchorMin = Vector2.zero;
        backdropRect.anchorMax = Vector2.one;
        backdropRect.offsetMin = Vector2.zero;
        backdropRect.offsetMax = Vector2.zero;

        panelImage = CreateImage(overlayRoot, "Panel", panelColor, panelBackgroundSprite);
        panelRoot = panelImage.rectTransform;
        panelRoot.anchorMin = new Vector2(0.5f, 0.5f);
        panelRoot.anchorMax = new Vector2(0.5f, 0.5f);
        panelRoot.pivot = new Vector2(0.5f, 0.5f);
        panelRoot.sizeDelta = panelSize;
        panelRoot.anchoredPosition = Vector2.zero;

        VerticalLayoutGroup panelLayout = panelRoot.gameObject.AddComponent<VerticalLayoutGroup>();
        panelLayout.padding = new RectOffset(
            Mathf.RoundToInt(panelPadding.x),
            Mathf.RoundToInt(panelPadding.x),
            Mathf.RoundToInt(panelPadding.y),
            Mathf.RoundToInt(panelPadding.y));
        panelLayout.spacing = rowSpacing;
        panelLayout.childControlWidth = true;
        panelLayout.childForceExpandWidth = true;
        panelLayout.childControlHeight = false;
        panelLayout.childForceExpandHeight = false;

        titleText = CreateLabel(panelRoot, "Title", titleFontSize, titleColor, "Victory");
        titleText.font = titleFont != null ? titleFont : titleText.font;
        titleText.alignment = TextAlignmentOptions.Center;
        ApplyTextOutline(titleText, titleOutlineColor, titleOutlineWidth);

        subtitleText = CreateLabel(panelRoot, "Subtitle", subtitleFontSize, subtitleColor, string.Empty);
        subtitleText.font = bodyFont != null ? bodyFont : subtitleText.font;
        subtitleText.alignment = TextAlignmentOptions.Center;

        expText = CreateLabel(panelRoot, "Exp", bodyFontSize, expColor, "EXP +0");
        expText.font = bodyFont != null ? bodyFont : expText.font;
        expText.alignment = TextAlignmentOptions.Center;

        summaryRoot = CreateVerticalRoot(panelRoot, "SummaryRoot", rowSpacing * 0.6f);
        VerticalLayoutGroup summaryLayout = summaryRoot.GetComponent<VerticalLayoutGroup>();
        if (summaryLayout != null)
        {
            summaryLayout.childControlHeight = true;
            summaryLayout.childForceExpandHeight = false;
        }
        contentFlexSpacerRoot = CreateVerticalRoot(panelRoot, "ContentFlexSpacer", 0f);
        LayoutElement contentFlexLayout = contentFlexSpacerRoot.gameObject.AddComponent<LayoutElement>();
        contentFlexLayout.flexibleHeight = 1f;
        contentFlexLayout.minHeight = 8f;
        lootRoot = CreateVerticalRoot(panelRoot, "LootRoot", rowSpacing * 0.45f);
        BuildLootRoot();
        buttonReserveRoot = CreateVerticalRoot(panelRoot, "ButtonReserveRoot", 0f);
        AddLayoutElement(buttonReserveRoot, panelSize.x - panelPadding.x * 2f, panelSize.x - panelPadding.x * 2f, 0f, buttonReservedHeight, buttonReservedHeight);
        buttonRoot = CreateHorizontalRoot(panelRoot, "ButtonRoot", buttonSpacing);
        LayoutElement buttonLayout = buttonRoot.gameObject.AddComponent<LayoutElement>();
        buttonLayout.ignoreLayout = true;

        BuildRelicChoiceModalRoot();
        BuildLootDetailModalRoot();

        victoryIntroImage = CreateImage(overlayRoot, "VictoryIntro", victoryIntroColor, victoryIntroSprite);
        victoryIntroImage.preserveAspect = true;
        victoryIntroRoot = victoryIntroImage.rectTransform;
        victoryIntroRoot.anchorMin = new Vector2(0.5f, 0.5f);
        victoryIntroRoot.anchorMax = new Vector2(0.5f, 0.5f);
        victoryIntroRoot.pivot = new Vector2(0.5f, 0.5f);
        victoryIntroRoot.sizeDelta = GetVictoryIntroSize();
        victoryIntroRoot.anchoredPosition = victoryIntroOffset;
        SetVictoryIntroVisible(false);
    }

    private RectTransform CreateVerticalRoot(Transform parent, string name, float spacing)
    {
        GameObject rootGo = new GameObject(name, typeof(RectTransform), typeof(VerticalLayoutGroup));
        rootGo.transform.SetParent(parent, false);
        VerticalLayoutGroup layout = rootGo.GetComponent<VerticalLayoutGroup>();
        layout.spacing = spacing;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        return rootGo.GetComponent<RectTransform>();
    }

    private RectTransform CreateHorizontalRoot(Transform parent, string name, float spacing)
    {
        GameObject rootGo = new GameObject(name, typeof(RectTransform), typeof(HorizontalLayoutGroup));
        rootGo.transform.SetParent(parent, false);
        HorizontalLayoutGroup layout = rootGo.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = spacing;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        return rootGo.GetComponent<RectTransform>();
    }

    private void BuildLootRoot()
    {
        if (lootRoot == null)
            return;

        VerticalLayoutGroup lootLayout = lootRoot.GetComponent<VerticalLayoutGroup>();
        if (lootLayout != null)
        {
            lootLayout.childAlignment = TextAnchor.MiddleCenter;
            lootLayout.childControlWidth = false;
            lootLayout.childForceExpandWidth = false;
            lootLayout.childControlHeight = true;
            lootLayout.childForceExpandHeight = false;
        }

        lootHeaderText = CreateLabel(lootRoot, "LootHeader", lootTitleFontSize, lootTitleColor, "战利品");
        lootHeaderText.alignment = TextAlignmentOptions.Center;
        lootHeaderText.font = bodyFont != null ? bodyFont : lootHeaderText.font;
        AddLayoutElement(lootHeaderText.rectTransform, panelSize.x - panelPadding.x * 2f, 0f, 0f, lootTitleFontSize + 10f, lootTitleFontSize + 10f);

        GameObject gridGo = new GameObject("LootGrid", typeof(RectTransform), typeof(GridLayoutGroup));
        gridGo.transform.SetParent(lootRoot, false);
        lootGridRoot = gridGo.GetComponent<RectTransform>();
        GridLayoutGroup gridLayout = gridGo.GetComponent<GridLayoutGroup>();
        gridLayout.cellSize = lootSlotSize;
        gridLayout.spacing = new Vector2(lootGridSpacing, lootGridSpacing);
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = Mathf.Max(1, lootGridColumnCount);
        gridLayout.startAxis = GridLayoutGroup.Axis.Horizontal;
        gridLayout.startCorner = GridLayoutGroup.Corner.UpperLeft;
        gridLayout.childAlignment = TextAnchor.MiddleCenter;
        AddLayoutElement(lootGridRoot, lootSlotSize.x, lootSlotSize.x, 0f, lootSlotSize.y, lootSlotSize.y);
    }

    private void BuildLootDetailModalRoot()
    {
        if (overlayRoot == null)
            return;

        lootDetailModalBackdropImage = CreateImage(overlayRoot, "LootDetailBackdrop", new Color(0f, 0f, 0f, 0.42f), null);
        lootDetailModalRoot = lootDetailModalBackdropImage.rectTransform;
        lootDetailModalRoot.anchorMin = Vector2.zero;
        lootDetailModalRoot.anchorMax = Vector2.one;
        lootDetailModalRoot.offsetMin = Vector2.zero;
        lootDetailModalRoot.offsetMax = Vector2.zero;

        Button backdropButton = lootDetailModalBackdropImage.gameObject.AddComponent<Button>();
        backdropButton.transition = Selectable.Transition.None;
        backdropButton.onClick.AddListener(HideLootDetailModal);

        Image panelImage = CreateImage(lootDetailModalRoot, "LootDetailPanel", lootDetailPanelColor, lootDetailBackgroundSprite);
        Button panelClickBlocker = panelImage.gameObject.AddComponent<Button>();
        panelClickBlocker.transition = Selectable.Transition.None;
        RectTransform detailRoot = panelImage.rectTransform;
        detailRoot.anchorMin = new Vector2(0.5f, 0.5f);
        detailRoot.anchorMax = new Vector2(0.5f, 0.5f);
        detailRoot.pivot = new Vector2(0.5f, 0.5f);
        detailRoot.sizeDelta = lootDetailPanelSize;
        detailRoot.anchoredPosition = Vector2.zero;

        Image icon = CreateImage(detailRoot, "Icon", Color.white, GetFallbackLootSprite());
        RectTransform iconRect = icon.rectTransform;
        iconRect.anchorMin = new Vector2(0f, 1f);
        iconRect.anchorMax = new Vector2(0f, 1f);
        iconRect.pivot = new Vector2(0f, 1f);
        iconRect.anchoredPosition = new Vector2(34f, -34f);
        iconRect.sizeDelta = lootDetailIconSize;
        icon.preserveAspect = true;

        TextMeshProUGUI title = CreateModalText(detailRoot, "Title", new Vector2(168f, -28f), new Vector2(lootDetailPanelSize.x - 244f, 46f), lootTitleFontSize + 4f, lootTitleColor, string.Empty, true);
        TextMeshProUGUI meta = CreateModalText(detailRoot, "Meta", new Vector2(168f, -82f), new Vector2(lootDetailPanelSize.x - 244f, 34f), lootBodyFontSize, expTextColor, string.Empty, true);
        TextMeshProUGUI description = CreateModalText(detailRoot, "Description", new Vector2(168f, -126f), new Vector2(lootDetailPanelSize.x - 210f, lootDetailPanelSize.y - 162f), lootBodyFontSize, lootTextColor, string.Empty, false);
        description.enableWordWrapping = true;
        description.overflowMode = TextOverflowModes.Overflow;

        TextMeshProUGUI relicAdditionalDescription = CreateModalText(detailRoot, "RelicAdditionalDescription", new Vector2(34f, -lootDetailPanelSize.y + 92f), new Vector2(lootDetailPanelSize.x - 68f, 54f), lootBodyFontSize - 2f, expTextColor, string.Empty, false);
        relicAdditionalDescription.enableWordWrapping = true;
        relicAdditionalDescription.overflowMode = TextOverflowModes.Overflow;
        relicAdditionalDescription.gameObject.SetActive(false);

        TextMeshProUGUI actionStatus = CreateModalText(detailRoot, "ActionStatus", new Vector2(34f, -lootDetailPanelSize.y + 34f), new Vector2(lootDetailPanelSize.x - 132f, 34f), lootBodyFontSize - 2f, expTextColor, string.Empty, false);
        Button action = CreateModalButton(detailRoot, "ActionButton", new Vector2(0f, 0f), new Vector2(0f, 0f), string.Empty, null);
        action.gameObject.SetActive(false);
        Button close = CreateModalButton(detailRoot, "CloseButton", new Vector2(lootDetailPanelSize.x - 58f, -18f), new Vector2(44f, 44f), "X", HideLootDetailModal);

        lootDetailView = detailRoot.gameObject.AddComponent<MapInventoryDetailView>();
        lootDetailView.ConfigureRuntime(icon, title, meta, description, relicAdditionalDescription, actionStatus, action, close);
        lootDetailView.ConfigureFonts(titleFont != null ? titleFont : bodyFont, bodyFont);
        lootDetailModalRoot.gameObject.SetActive(false);
    }

    private void BuildRelicChoiceModalRoot()
    {
        if (overlayRoot == null)
            return;

        relicChoiceModalBackdropImage = CreateImage(overlayRoot, "RelicChoiceModalBackdrop", relicChoiceModalBackdropColor, null);
        relicChoiceModalRoot = relicChoiceModalBackdropImage.rectTransform;
        relicChoiceModalRoot.anchorMin = Vector2.zero;
        relicChoiceModalRoot.anchorMax = Vector2.one;
        relicChoiceModalRoot.offsetMin = Vector2.zero;
        relicChoiceModalRoot.offsetMax = Vector2.zero;

        relicChoiceModalPanelImage = CreateImage(relicChoiceModalRoot, "RelicChoiceModalPanel", relicChoiceModalPanelColor, relicChoiceModalBackgroundSprite);
        RectTransform panelRect = relicChoiceModalPanelImage.rectTransform;
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = ResolveRelicChoiceModalPanelSize();
        panelRect.anchoredPosition = Vector2.zero;
        ApplyImageScaleMode(relicChoiceModalPanelImage, relicChoiceModalBackgroundScaleMode);

        relicChoiceModalTitleText = CreateLabel(panelRect, "RelicChoiceTitle", lootTitleFontSize + 6f, lootTitleColor, relicChoiceHeaderText);
        relicChoiceModalTitleText.font = titleFont != null ? titleFont : relicChoiceModalTitleText.font;
        relicChoiceModalTitleText.alignment = TextAlignmentOptions.Center;
        RectTransform titleRect = relicChoiceModalTitleText.rectTransform;
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -24f);
        titleRect.sizeDelta = new Vector2(-56f, lootTitleFontSize + 26f);

        relicChoiceCardsRoot = CreateHorizontalRoot(panelRect, "RelicChoiceCards", relicChoiceModalSpacing);
        relicChoiceCardsRoot.anchorMin = new Vector2(0.5f, 0.5f);
        relicChoiceCardsRoot.anchorMax = new Vector2(0.5f, 0.5f);
        relicChoiceCardsRoot.pivot = new Vector2(0.5f, 0.5f);
        relicChoiceCardsRoot.anchoredPosition = new Vector2(0f, 4f);
        relicChoiceCardsRoot.sizeDelta = new Vector2(
            relicChoiceCardSize.x * 3f + relicChoiceModalSpacing * 2f,
            relicChoiceCardSize.y);
        HorizontalLayoutGroup cardsLayout = relicChoiceCardsRoot.GetComponent<HorizontalLayoutGroup>();
        cardsLayout.childAlignment = TextAnchor.MiddleCenter;
        cardsLayout.childControlWidth = false;
        cardsLayout.childControlHeight = false;
        cardsLayout.childForceExpandWidth = false;
        cardsLayout.childForceExpandHeight = false;

        relicChoiceModalButtonRoot = CreateHorizontalRoot(panelRect, "RelicChoiceButtons", buttonSpacing);
        relicChoiceModalButtonRoot.anchorMin = new Vector2(0.5f, 0f);
        relicChoiceModalButtonRoot.anchorMax = new Vector2(0.5f, 0f);
        relicChoiceModalButtonRoot.pivot = new Vector2(0.5f, 0f);
        relicChoiceModalButtonRoot.anchoredPosition = new Vector2(0f, 30f);
        relicChoiceModalButtonRoot.sizeDelta = new Vector2(buttonSize.x * 2f + buttonSpacing, buttonSize.y);
        relicChoiceRefreshButton = CreateActionButton(relicChoiceModalButtonRoot, "RelicChoiceRefreshButton", relicChoiceRefreshLabel, RefreshRelicChoiceModal, false);
        relicChoiceRefreshButtonText = relicChoiceRefreshButton != null ? relicChoiceRefreshButton.GetComponentInChildren<TextMeshProUGUI>(true) : null;
        CreateActionButton(relicChoiceModalButtonRoot, "RelicChoiceConfirmButton", relicChoiceConfirmLabel, CloseRelicChoiceModal, false);
        BuildRelicChoiceTooltipRoot();
        SetRelicChoiceModalVisible(false);
    }

    private void BuildRelicChoiceTooltipRoot()
    {
        relicChoiceDetailPanelImage = CreateImage(overlayRoot, "RelicChoiceTooltip", relicChoiceDetailPanelColor, relicChoiceDetailBackgroundSprite);
        ApplyImageScaleMode(relicChoiceDetailPanelImage, relicChoiceTooltipBackgroundScaleMode);
        relicChoiceDetailPanelImage.raycastTarget = false;
        relicChoiceTooltipRoot = relicChoiceDetailPanelImage.rectTransform;
        relicChoiceTooltipRoot.anchorMin = new Vector2(0.5f, 0.5f);
        relicChoiceTooltipRoot.anchorMax = new Vector2(0.5f, 0.5f);
        relicChoiceTooltipRoot.pivot = new Vector2(0f, 1f);
        relicChoiceTooltipRoot.sizeDelta = relicChoiceTooltipSize;
        relicChoiceTooltipRoot.SetAsLastSibling();

        VerticalLayoutGroup detailLayout = relicChoiceTooltipRoot.gameObject.AddComponent<VerticalLayoutGroup>();
        detailLayout.padding = new RectOffset(22, 22, 18, 18);
        detailLayout.spacing = 10f;
        detailLayout.childControlWidth = true;
        detailLayout.childControlHeight = false;
        detailLayout.childForceExpandWidth = true;
        detailLayout.childForceExpandHeight = false;

        RectTransform headerRoot = CreateHorizontalRoot(relicChoiceTooltipRoot, "TooltipHeader", 14f);
        AddLayoutElement(headerRoot, 0f, 0f, 1f, relicChoiceModalIconSize.y, relicChoiceModalIconSize.y);
        HorizontalLayoutGroup headerLayout = headerRoot.GetComponent<HorizontalLayoutGroup>();
        headerLayout.childControlWidth = true;
        headerLayout.childForceExpandWidth = false;
        headerLayout.childAlignment = TextAnchor.MiddleLeft;

        relicChoiceDetailIcon = CreateIconImage(headerRoot, "RelicChoiceDetailIcon", GetFallbackLootSprite());
        relicChoiceDetailIcon.preserveAspect = true;
        AddLayoutElement(relicChoiceDetailIcon.rectTransform, relicChoiceModalIconSize.x, relicChoiceModalIconSize.x, 0f, relicChoiceModalIconSize.y, relicChoiceModalIconSize.y);

        RectTransform titleRoot = CreateVerticalRoot(headerRoot, "TooltipTitleRoot", 4f);
        AddLayoutElement(titleRoot, relicChoiceTooltipSize.x - relicChoiceModalIconSize.x - 72f, 0f, 1f, relicChoiceModalIconSize.y, relicChoiceModalIconSize.y);

        relicChoiceDetailNameText = CreateLabel(titleRoot, "RelicChoiceDetailName", lootTitleFontSize + 2f, lootTitleColor, string.Empty);
        relicChoiceDetailNameText.alignment = TextAlignmentOptions.MidlineLeft;
        relicChoiceDetailNameText.enableWordWrapping = true;
        relicChoiceDetailNameText.overflowMode = TextOverflowModes.Ellipsis;
        relicChoiceDetailNameText.raycastTarget = false;
        AddLayoutElement(relicChoiceDetailNameText.rectTransform, 0f, 0f, 1f, lootTitleFontSize + 18f, lootTitleFontSize + 18f);

        relicChoiceDetailMetaText = CreateLabel(titleRoot, "RelicChoiceDetailMeta", lootBodyFontSize, lootTextColor, string.Empty);
        relicChoiceDetailMetaText.alignment = TextAlignmentOptions.MidlineLeft;
        relicChoiceDetailMetaText.raycastTarget = false;
        AddLayoutElement(relicChoiceDetailMetaText.rectTransform, 0f, 0f, 1f, lootBodyFontSize + 10f, lootBodyFontSize + 10f);

        relicChoiceDetailDescriptionText = CreateLabel(relicChoiceTooltipRoot, "RelicChoiceDetailDescription", lootBodyFontSize, lootTextColor, string.Empty);
        relicChoiceDetailDescriptionText.alignment = TextAlignmentOptions.TopLeft;
        relicChoiceDetailDescriptionText.enableWordWrapping = true;
        relicChoiceDetailDescriptionText.overflowMode = TextOverflowModes.Overflow;
        relicChoiceDetailDescriptionText.raycastTarget = false;
        AddLayoutElement(relicChoiceDetailDescriptionText.rectTransform, 0f, 0f, 1f, relicChoiceTooltipSize.y - relicChoiceModalIconSize.y - 96f, relicChoiceTooltipSize.y - relicChoiceModalIconSize.y - 120f);

        relicChoiceDetailHintText = CreateLabel(relicChoiceTooltipRoot, "RelicChoiceDetailHint", lootBodyFontSize - 2f, subtitleColor, relicChoiceDetailEmptyText);
        relicChoiceDetailHintText.alignment = TextAlignmentOptions.BottomLeft;
        relicChoiceDetailHintText.enableWordWrapping = true;
        relicChoiceDetailHintText.overflowMode = TextOverflowModes.Overflow;
        relicChoiceDetailHintText.raycastTarget = false;
        AddLayoutElement(relicChoiceDetailHintText.rectTransform, 0f, 0f, 1f, 42f, 32f);

        SetRelicChoiceTooltipVisible(false);
    }

    private void OpenRelicChoiceModal(BattleSettlementRelicChoiceData choiceData)
    {
        if (choiceData == null || !choiceData.HasChoices)
            return;

        HideLootDetailModal();
        activeRelicChoiceData = choiceData;
        EnsureCanvas();
        ClearRelicChoiceModalViews();
        relicChoiceViews.Clear();
        hoveredRelicChoiceView = null;
        selectedRelicChoiceView = null;

        if (relicChoiceModalTitleText != null)
        {
            relicChoiceModalTitleText.color = lootTitleColor;
            relicChoiceModalTitleText.text = !string.IsNullOrWhiteSpace(choiceData.title)
                ? choiceData.title
                : relicChoiceHeaderText;
        }

        for (int i = 0; i < choiceData.choices.Count; i++)
        {
            BattleSettlementLootEntry choice = choiceData.choices[i];
            if (choice == null)
                continue;

            LootItemView view = CreateRelicChoiceCard(choice);
            if (view != null)
            {
                relicChoiceViews.Add(view);
                if (choiceData.selectedChoice == choice)
                    selectedRelicChoiceView = view;
            }
        }

        if (choiceData.selectedChoice != null)
            ShowRelicChoiceDetail(choiceData.selectedChoice, selectedRelicChoiceView);
        else
            ShowEmptyRelicChoiceDetail();

        UpdateRelicChoiceCardColors();
        UpdateRelicChoiceRefreshButton();
        if (relicChoiceCardsRoot != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(relicChoiceCardsRoot);
        SetRelicChoiceModalVisible(true);
    }

    private LootItemView CreateRelicChoiceCard(BattleSettlementLootEntry entry)
    {
        GameObject cardGo = new GameObject($"RelicChoice_{entry.displayName}", typeof(RectTransform), typeof(Image), typeof(Button), typeof(EventTrigger));
        cardGo.transform.SetParent(relicChoiceCardsRoot, false);
        spawnedRelicChoiceObjects.Add(cardGo);

        Image cardImage = cardGo.GetComponent<Image>();
        cardImage.color = relicChoiceCardColor;
        cardImage.sprite = relicChoiceCardBackgroundSprite;
        cardImage.type = relicChoiceCardBackgroundSprite != null ? Image.Type.Sliced : Image.Type.Simple;
        ApplyImageScaleMode(cardImage, relicChoiceCardBackgroundScaleMode);
        RectTransform cardRect = cardGo.GetComponent<RectTransform>();
        cardRect.sizeDelta = relicChoiceCardSize;
        AddLayoutElement(cardRect, relicChoiceCardSize.x, relicChoiceCardSize.x, 0f, relicChoiceCardSize.y, relicChoiceCardSize.y);

        VerticalLayoutGroup cardLayout = cardGo.AddComponent<VerticalLayoutGroup>();
        cardLayout.padding = new RectOffset(16, 16, 18, 18);
        cardLayout.spacing = 12f;
        cardLayout.childAlignment = TextAnchor.UpperCenter;
        cardLayout.childControlWidth = true;
        cardLayout.childControlHeight = false;
        cardLayout.childForceExpandWidth = true;
        cardLayout.childForceExpandHeight = false;

        Image icon = CreateIconImage(cardGo.transform, "Icon", entry.icon != null ? entry.icon : GetFallbackLootSprite());
        AddLayoutElement(icon.rectTransform, relicChoiceModalIconSize.x, relicChoiceModalIconSize.x, 0f, relicChoiceModalIconSize.y, relicChoiceModalIconSize.y);

        TextMeshProUGUI nameText = CreateLabel(cardGo.transform, "Name", lootTitleFontSize, lootTitleColor, entry.displayName);
        nameText.alignment = TextAlignmentOptions.Center;
        nameText.enableWordWrapping = true;
        nameText.overflowMode = TextOverflowModes.Ellipsis;
        nameText.raycastTarget = false;
        AddLayoutElement(nameText.rectTransform, 0f, 0f, 1f, 88f, 54f);

        TextMeshProUGUI rarityText = CreateLabel(cardGo.transform, "Rarity", lootBodyFontSize, lootTextColor, GetLootRarityLabel(entry.rarity));
        rarityText.alignment = TextAlignmentOptions.Center;
        rarityText.raycastTarget = false;
        AddLayoutElement(rarityText.rectTransform, 0f, 0f, 1f, lootBodyFontSize + 10f, lootBodyFontSize + 10f);

        TextMeshProUGUI hintText = CreateLabel(cardGo.transform, "Hint", lootBodyFontSize - 2f, subtitleColor, "悬停 / 点击");
        hintText.alignment = TextAlignmentOptions.Center;
        hintText.raycastTarget = false;
        AddLayoutElement(hintText.rectTransform, 0f, 0f, 1f, lootBodyFontSize + 10f, lootBodyFontSize + 10f);

        Button button = cardGo.GetComponent<Button>();
        LootItemView view = new LootItemView
        {
            entry = entry,
            button = button,
            slotImage = cardImage
        };

        button.onClick.AddListener(() => SelectRelicChoice(entry, view));

        EventTrigger trigger = cardGo.GetComponent<EventTrigger>();
        AddEventTrigger(trigger, EventTriggerType.PointerEnter, eventData => ShowRelicChoiceDetail(entry, view, ExtractPointerScreenPosition(eventData)));
        AddEventTrigger(trigger, EventTriggerType.PointerExit, _ => HideRelicChoiceDetailIfNotSelected(entry));
        return view;
    }

    private void SelectRelicChoice(BattleSettlementLootEntry entry, LootItemView selectedView)
    {
        if (entry == null || !entry.isRelicChoiceOption)
            return;

        bool wasSelected = activeRelicChoiceData != null && activeRelicChoiceData.selectedChoice == entry;
        if (wasSelected)
        {
            activeRelicChoiceData.selectedChoice = null;
            selectedRelicChoiceView = null;
            ShowEmptyRelicChoiceDetail();
            UpdateRelicChoiceCardColors();
            UpdateRelicChoiceRefreshButton();
            UpdateLootSlotColors();
            return;
        }

        if (activeRelicChoiceData != null)
            activeRelicChoiceData.selectedChoice = entry;

        selectedRelicChoiceView = selectedView;
        ShowRelicChoiceDetail(entry, selectedView);
        UpdateRelicChoiceCardColors();
        UpdateRelicChoiceRefreshButton();
        UpdateLootSlotColors();
    }

    private void RefreshRelicChoiceModal()
    {
        if (BattleRelicChoiceSelectionService.TryRefresh(activeRelicChoiceData, new System.Random()))
            OpenRelicChoiceModal(activeRelicChoiceData);
    }

    private void ShowRelicChoiceDetail(BattleSettlementLootEntry entry, LootItemView hoveredView)
    {
        ShowRelicChoiceDetail(entry, hoveredView, GetCurrentPointerScreenPosition());
    }

    private void ShowRelicChoiceDetail(BattleSettlementLootEntry entry, LootItemView hoveredView, Vector2 screenPosition)
    {
        hoveredRelicChoiceView = hoveredView;
        if (entry == null)
        {
            ShowEmptyRelicChoiceDetail();
            return;
        }

        SetRelicChoiceTooltipVisible(true);
        PositionRelicChoiceTooltip(screenPosition);
        if (relicChoiceDetailIcon != null)
        {
            relicChoiceDetailIcon.gameObject.SetActive(true);
            SetIconSprite(relicChoiceDetailIcon, entry.icon != null ? entry.icon : GetFallbackLootSprite());
        }
        if (relicChoiceDetailNameText != null)
            relicChoiceDetailNameText.text = entry.displayName;
        if (relicChoiceDetailMetaText != null)
        {
            string selectedText = activeRelicChoiceData != null && activeRelicChoiceData.selectedChoice == entry
                ? $" | {relicChoiceSelectedPrefix}"
                : string.Empty;
            relicChoiceDetailMetaText.text = $"{GetLootRarityLabel(entry.rarity)} | 遗物{selectedText}";
        }
        if (relicChoiceDetailDescriptionText != null)
            relicChoiceDetailDescriptionText.text = string.IsNullOrWhiteSpace(entry.description) ? "暂无描述。" : entry.description;
        if (relicChoiceDetailHintText != null)
            relicChoiceDetailHintText.text = relicChoiceDetailEmptyText;

        UpdateRelicChoiceCardColors();
    }

    private void ShowEmptyRelicChoiceDetail()
    {
        SetRelicChoiceTooltipVisible(false);
        if (relicChoiceDetailIcon != null)
            relicChoiceDetailIcon.gameObject.SetActive(false);
        if (relicChoiceDetailNameText != null)
            relicChoiceDetailNameText.text = relicChoiceHeaderText;
        if (relicChoiceDetailMetaText != null)
            relicChoiceDetailMetaText.text = string.Empty;
        if (relicChoiceDetailDescriptionText != null)
            relicChoiceDetailDescriptionText.text = relicChoiceDetailEmptyText;
        if (relicChoiceDetailHintText != null)
            relicChoiceDetailHintText.text = string.Empty;
    }

    private void UpdateRelicChoiceCardColors()
    {
        for (int i = 0; i < relicChoiceViews.Count; i++)
        {
            LootItemView view = relicChoiceViews[i];
            if (view == null || view.slotImage == null)
                continue;

            if (view == selectedRelicChoiceView)
                view.slotImage.color = relicChoiceSelectedSlotColor;
            else if (view == hoveredRelicChoiceView)
                view.slotImage.color = relicChoiceHoveredCardColor;
            else
                view.slotImage.color = relicChoiceCardColor;
        }
    }

    private void UpdateRelicChoiceRefreshButton()
    {
        if (relicChoiceRefreshButton == null)
            return;

        bool canRefresh = BattleRelicChoiceSelectionService.CanRefresh(activeRelicChoiceData)
            && activeRelicChoiceData.selectedChoice == null;
        relicChoiceRefreshButton.gameObject.SetActive(activeRelicChoiceData != null && activeRelicChoiceData.refreshesRemaining > 0);
        relicChoiceRefreshButton.interactable = canRefresh;
        if (relicChoiceRefreshButtonText != null && activeRelicChoiceData != null)
            relicChoiceRefreshButtonText.text = $"{relicChoiceRefreshLabel} ({Mathf.Max(0, activeRelicChoiceData.refreshesRemaining)})";
    }

    private static HashSet<Relic_SO> BuildRelicChoiceRefreshExclusions(BattleSettlementRelicChoiceData choiceData)
    {
        return BattleRelicChoiceSelectionService.RefreshExclusions(choiceData);
    }

    private void CloseRelicChoiceModal()
    {
        if (activeRelicChoiceData != null && activeRelicChoiceData.HasChoices && activeRelicChoiceData.selectedChoice == null)
        {
            if (relicChoiceModalTitleText != null)
            {
                relicChoiceModalTitleText.color = relicChoiceRequiredColor;
                relicChoiceModalTitleText.text = !string.IsNullOrWhiteSpace(activeRelicChoiceData.requiredMessage)
                    ? activeRelicChoiceData.requiredMessage
                    : relicChoiceRequiredMessage;
            }
            return;
        }

        SetRelicChoiceModalVisible(false);
    }

    private void SetRelicChoiceModalVisible(bool visible)
    {
        if (relicChoiceModalRoot != null)
            relicChoiceModalRoot.gameObject.SetActive(visible);
        if (!visible)
        {
            SetRelicChoiceTooltipVisible(false);
        }
    }

    private void SetRelicChoiceTooltipVisible(bool visible)
    {
        if (relicChoiceTooltipRoot != null)
            relicChoiceTooltipRoot.gameObject.SetActive(visible);
    }

    private void HideRelicChoiceDetailIfNotSelected(BattleSettlementLootEntry entry)
    {
        if (hoveredRelicChoiceView != null && hoveredRelicChoiceView.entry == entry)
            hoveredRelicChoiceView = null;

        ShowEmptyRelicChoiceDetail();
        UpdateRelicChoiceCardColors();
    }

    private static string GetLootRarityLabel(LootRarity rarity)
    {
        if (rarity == LootRarity.Trash)
            return "Trash";

        switch (rarity)
        {
            case LootRarity.Uncommon:
                return "精良";
            case LootRarity.Rare:
                return "稀有";
            case LootRarity.Epic:
                return "史诗";
            case LootRarity.Legendary:
                return "传说";
            case LootRarity.Common:
            default:
                return "普通";
        }
    }

    private void PositionRelicChoiceTooltip(Vector2 screenPosition)
    {
        if (overlayRoot == null || relicChoiceTooltipRoot == null)
            return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            overlayRoot,
            screenPosition,
            null,
            out Vector2 localPoint);

        Vector2 target = new Vector2(
            localPoint.x + relicChoiceTooltipOffset.x,
            localPoint.y - relicChoiceTooltipOffset.y);

        Rect rootRect = overlayRoot.rect;
        Vector2 size = relicChoiceTooltipRoot.rect.size;
        if (size.x <= 0f || size.y <= 0f)
            size = relicChoiceTooltipSize;

        float minX = rootRect.xMin + 12f;
        float maxX = rootRect.xMax - size.x - 12f;
        float minY = rootRect.yMin + size.y + 12f;
        float maxY = rootRect.yMax - 12f;

        if (target.x > maxX)
            target.x = localPoint.x - size.x - relicChoiceTooltipOffset.x;
        target.x = Mathf.Clamp(target.x, minX, Mathf.Max(minX, maxX));
        target.y = Mathf.Clamp(target.y, minY, Mathf.Max(minY, maxY));
        relicChoiceTooltipRoot.anchoredPosition = target;
    }

    private RectTransform CreateExpBar(Transform parent, out RectTransform fillRect, out float fillWidth)
    {
        return CreateStatusBar(
            parent,
            "ExpBar",
            expBarBorderColor,
            expBarBackgroundColor,
            expBarFillColor,
            summaryExpBarWidth,
            expBarSize.y,
            out fillRect,
            out fillWidth);
    }

    private RectTransform CreateStatusBar(
        Transform parent,
        string name,
        Color borderColor,
        Color backgroundColor,
        Color fillColor,
        float width,
        float height,
        out RectTransform fillRect,
        out float fillWidth)
    {
        Image border = CreateImage(parent, $"{name}Border", borderColor, null);
        RectTransform borderRect = border.rectTransform;
        Vector2 effectiveBarSize = new Vector2(Mathf.Max(1f, width), Mathf.Max(1f, height));
        borderRect.sizeDelta = effectiveBarSize;
        AddLayoutElement(borderRect, effectiveBarSize.x, effectiveBarSize.x, 0f, effectiveBarSize.y, effectiveBarSize.y);

        Image background = CreateImage(borderRect, $"{name}Background", backgroundColor, null);
        RectTransform backgroundRect = background.rectTransform;
        backgroundRect.anchorMin = Vector2.zero;
        backgroundRect.anchorMax = Vector2.one;
        backgroundRect.offsetMin = new Vector2(2f, 2f);
        backgroundRect.offsetMax = new Vector2(-2f, -2f);

        Image fill = CreateImage(backgroundRect, $"{name}Fill", fillColor, null);
        fill.raycastTarget = false;
        fillRect = fill.rectTransform;
        fillRect.anchorMin = new Vector2(0f, 0f);
        fillRect.anchorMax = new Vector2(0f, 1f);
        fillRect.pivot = new Vector2(0f, 0.5f);
        fillRect.anchoredPosition = Vector2.zero;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        fillWidth = effectiveBarSize.x - 4f;
        fillRect.sizeDelta = new Vector2(0f, 0f);
        return borderRect;
    }

    private TextMeshProUGUI CreateModalText(RectTransform parent, string name, Vector2 anchoredPosition, Vector2 size, float fontSize, Color color, string value, bool richText)
    {
        TextMeshProUGUI text = CreateLabel(parent, name, fontSize, color, value);
        text.richText = richText;
        text.font = bodyFont != null ? bodyFont : text.font;
        RectTransform rect = text.rectTransform;
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
        return text;
    }

    private Button CreateModalButton(RectTransform parent, string name, Vector2 anchoredPosition, Vector2 size, string label, Action onClick)
    {
        GameObject buttonGo = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonGo.transform.SetParent(parent, false);
        Image image = buttonGo.GetComponent<Image>();
        image.color = buttonColor;
        image.sprite = buttonBackgroundSprite;
        image.type = buttonBackgroundSprite != null ? Image.Type.Sliced : Image.Type.Simple;

        RectTransform rect = buttonGo.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        Button button = buttonGo.GetComponent<Button>();
        button.onClick.AddListener(() => onClick?.Invoke());
        TextMeshProUGUI text = CreateLabel(buttonGo.transform, "Label", buttonFontSize, buttonTextColor, label);
        text.alignment = TextAlignmentOptions.Center;
        RectTransform textRect = text.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        return button;
    }

    private Button CreateActionButton(Transform parent, string name, string label, Action onClick, bool trackButton = true)
    {
        GameObject buttonGo = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonGo.transform.SetParent(parent, false);

        Image image = buttonGo.GetComponent<Image>();
        image.color = buttonColor;
        image.sprite = buttonBackgroundSprite;
        image.type = buttonBackgroundSprite != null ? Image.Type.Sliced : Image.Type.Simple;

        RectTransform rect = buttonGo.GetComponent<RectTransform>();
        rect.sizeDelta = buttonSize;

        Button button = buttonGo.GetComponent<Button>();
        if (trackButton)
            spawnedButtons.Add(button);
        button.onClick.AddListener(() => onClick?.Invoke());

        TextMeshProUGUI text = CreateLabel(buttonGo.transform, "Label", buttonFontSize, buttonTextColor, label);
        text.font = buttonFont != null ? buttonFont : text.font;
        text.alignment = TextAlignmentOptions.Center;
        RectTransform textRect = text.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        return button;
    }

    private static void AddEventTrigger(EventTrigger trigger, EventTriggerType eventType, Action<BaseEventData> callback)
    {
        if (trigger == null || callback == null)
            return;

        EventTrigger.Entry entry = new EventTrigger.Entry
        {
            eventID = eventType
        };
        entry.callback.AddListener(eventData => callback.Invoke(eventData));
        trigger.triggers.Add(entry);
    }

    private TextMeshProUGUI CreateLabel(Transform parent, string name, float fontSize, Color color, string value)
    {
        GameObject textGo = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        textGo.transform.SetParent(parent, false);
        TextMeshProUGUI text = textGo.GetComponent<TextMeshProUGUI>();
        text.font = bodyFont != null ? bodyFont : text.font;
        text.fontSize = fontSize;
        text.color = color;
        text.text = value;
        text.enableWordWrapping = false;
        return text;
    }

    private Image CreateImage(Transform parent, string name, Color color, Sprite sprite)
    {
        GameObject imageGo = new GameObject(name, typeof(RectTransform), typeof(Image));
        imageGo.transform.SetParent(parent, false);
        Image image = imageGo.GetComponent<Image>();
        image.color = color;
        image.sprite = sprite;
        image.type = sprite != null ? Image.Type.Sliced : Image.Type.Simple;
        return image;
    }

    private Image CreateIconImage(Transform parent, string name, Sprite sprite)
    {
        Image image = CreateImage(parent, name, Color.white, sprite);
        image.type = Image.Type.Simple;
        image.preserveAspect = true;
        image.raycastTarget = false;
        return image;
    }

    private static void SetIconSprite(Image image, Sprite sprite)
    {
        if (image == null)
            return;

        image.sprite = sprite;
        image.type = Image.Type.Simple;
        image.preserveAspect = true;
    }

    private static void ApplyImageScaleMode(Image image, SettlementImageScaleMode scaleMode)
    {
        if (image == null)
            return;

        image.preserveAspect = false;
        switch (scaleMode)
        {
            case SettlementImageScaleMode.PreserveAspect:
                image.type = Image.Type.Simple;
                image.preserveAspect = image.sprite != null;
                break;
            case SettlementImageScaleMode.SimpleStretch:
                image.type = Image.Type.Simple;
                break;
            case SettlementImageScaleMode.Sliced:
            default:
                image.type = image.sprite != null ? Image.Type.Sliced : Image.Type.Simple;
                break;
        }
    }

    private Vector2 ResolveRelicChoiceModalPanelSize()
    {
        Vector2 maxSize = new Vector2(Mathf.Max(1f, relicChoiceModalSize.x), Mathf.Max(1f, relicChoiceModalSize.y));
        if (relicChoiceModalBackgroundScaleMode != SettlementImageScaleMode.PreserveAspect || relicChoiceModalBackgroundSprite == null)
            return maxSize;

        Vector2 fittedSize = ResolveAspectFittedSize(relicChoiceModalBackgroundSprite, maxSize);
        Vector2 minContentSize = new Vector2(
            relicChoiceCardSize.x * 3f + relicChoiceModalSpacing * 2f + 96f,
            relicChoiceCardSize.y + buttonSize.y + lootTitleFontSize + 128f);
        if (fittedSize.x >= minContentSize.x && fittedSize.y >= minContentSize.y)
            return fittedSize;

        float scale = Mathf.Max(minContentSize.x / Mathf.Max(1f, fittedSize.x), minContentSize.y / Mathf.Max(1f, fittedSize.y));
        return fittedSize * scale;
    }

    private static Vector2 ResolveAspectFittedSize(Sprite sprite, Vector2 maxSize)
    {
        if (sprite == null)
            return maxSize;

        Rect spriteRect = sprite.rect;
        float spriteWidth = Mathf.Max(1f, spriteRect.width);
        float spriteHeight = Mathf.Max(1f, spriteRect.height);
        float scale = Mathf.Min(
            Mathf.Max(1f, maxSize.x) / spriteWidth,
            Mathf.Max(1f, maxSize.y) / spriteHeight);

        return new Vector2(spriteWidth * scale, spriteHeight * scale);
    }

    private Vector2 ExtractPointerScreenPosition(BaseEventData eventData)
    {
        PointerEventData pointerData = eventData as PointerEventData;
        return pointerData != null ? pointerData.position : GetCurrentPointerScreenPosition();
    }

    private Vector2 GetCurrentPointerScreenPosition()
    {
        return Input.mousePosition;
    }

    private void ClearRows()
    {
        for (int i = 0; i < spawnedRows.Count; i++)
        {
            if (spawnedRows[i] != null)
                Destroy(spawnedRows[i]);
        }

        spawnedRows.Clear();
    }

    private void ClearButtons()
    {
        for (int i = 0; i < spawnedButtons.Count; i++)
        {
            if (spawnedButtons[i] != null)
                Destroy(spawnedButtons[i].gameObject);
        }

        spawnedButtons.Clear();
    }

    private void ClearLootViews()
    {
        for (int i = 0; i < spawnedLootObjects.Count; i++)
        {
            if (spawnedLootObjects[i] != null)
                Destroy(spawnedLootObjects[i]);
        }

        spawnedLootObjects.Clear();
    }

    private void ClearRelicChoiceModalViews()
    {
        for (int i = 0; i < spawnedRelicChoiceObjects.Count; i++)
        {
            if (spawnedRelicChoiceObjects[i] != null)
                Destroy(spawnedRelicChoiceObjects[i]);
        }

        spawnedRelicChoiceObjects.Clear();
        relicChoiceViews.Clear();
        hoveredRelicChoiceView = null;
        selectedRelicChoiceView = null;
        ShowEmptyRelicChoiceDetail();
    }

    private void SetVisible(bool visible)
    {
        if (overlayCanvas == null)
            return;

        overlayCanvas.gameObject.SetActive(visible);
    }

    private void ApplyRuntimeLayoutAdjustments()
    {
        if (buttonRoot != null)
        {
            buttonRoot.anchorMin = new Vector2(0.5f, 0f);
            buttonRoot.anchorMax = new Vector2(0.5f, 0f);
            buttonRoot.pivot = new Vector2(0.5f, 0f);
            buttonRoot.anchoredPosition = new Vector2(buttonRootOffset.x, buttonBottomInset + buttonRootOffset.y);
        }

        if (buttonReserveRoot != null)
            AddLayoutElement(buttonReserveRoot, panelSize.x - panelPadding.x * 2f, panelSize.x - panelPadding.x * 2f, 0f, buttonReservedHeight, buttonReservedHeight);

        if (lootGridRoot != null)
        {
            GridLayoutGroup gridLayout = lootGridRoot.GetComponent<GridLayoutGroup>();
            if (gridLayout != null)
            {
                gridLayout.cellSize = lootSlotSize;
                gridLayout.spacing = new Vector2(lootGridSpacing, lootGridSpacing);
            }
        }
    }

    private void SetPanelVisible(bool visible)
    {
        if (panelRoot != null)
            panelRoot.gameObject.SetActive(visible);
    }

    private void SetVictoryIntroVisible(bool visible)
    {
        if (victoryIntroRoot == null || victoryIntroImage == null)
            return;

        victoryIntroRoot.gameObject.SetActive(visible);
    }

    private IEnumerator PlayVictoryIntro()
    {
        if (victoryIntroRoot == null || victoryIntroImage == null)
            yield break;

        victoryIntroRoot.sizeDelta = GetVictoryIntroSize();
        victoryIntroRoot.anchoredPosition = victoryIntroOffset;
        victoryIntroImage.sprite = victoryIntroSprite;
        victoryIntroImage.preserveAspect = true;
        SetVictoryIntroVisible(true);

        float duration = Mathf.Max(0.01f, victoryIntroFadeDuration);
        Color targetColor = victoryIntroColor;
        Color color = targetColor;
        color.a = 0f;
        victoryIntroImage.color = color;
        victoryIntroRoot.localScale = Vector3.one * Mathf.Max(0.01f, victoryIntroStartScale);

        for (float time = 0f; time < duration; time += Time.unscaledDeltaTime)
        {
            float normalized = Mathf.Clamp01(time / duration);
            float eased = 1f - Mathf.Pow(1f - normalized, 3f);

            Color frameColor = targetColor;
            frameColor.a = Mathf.Lerp(0f, targetColor.a, eased);
            victoryIntroImage.color = frameColor;
            float scale = Mathf.Lerp(victoryIntroStartScale, victoryIntroEndScale, eased);
            victoryIntroRoot.localScale = Vector3.one * scale;
            yield return null;
        }

        victoryIntroImage.color = targetColor;
        victoryIntroRoot.localScale = Vector3.one * victoryIntroEndScale;

        if (victoryIntroHoldDuration > 0f)
            yield return new WaitForSecondsRealtime(victoryIntroHoldDuration);
    }

    private IEnumerator AnimateSummaryRows()
    {
        for (int i = 0; i < rowViews.Count; i++)
        {
            SummaryRowView row = rowViews[i];
            if (row == null || row.entry == null)
                continue;

            yield return AnimateExperienceRow(row);
        }
    }

    private IEnumerator AnimateExperienceRow(SummaryRowView row)
    {
        BattleSettlementEntry entry = row.entry;
        int currentLevel = Mathf.Max(1, entry.level);
        int currentExp = Mathf.Max(0, entry.currentExperience);
        int remainingGain = Mathf.Max(0, entry.experienceGained);
        int maxExp = ResolveExpMax(currentLevel, entry.maxExperience);

        UpdateSummaryRow(row, currentLevel, currentExp, maxExp, entry.currentHP, entry.maxHP);

        if (remainingGain <= 0 || maxExp <= 0)
        {
            ApplyResolvedSummaryState(row);
            yield break;
        }

        while (remainingGain > 0 && maxExp > 0)
        {
            currentExp = Mathf.Clamp(currentExp, 0, maxExp);
            int available = Mathf.Max(0, maxExp - currentExp);
            if (available <= 0)
            {
                currentLevel += 1;
                currentExp = 0;
                maxExp = ResolveExpMax(currentLevel, 0);
                UpdateSummaryRow(row, currentLevel, currentExp, maxExp, entry.currentHP, entry.maxHP);
                if (expLevelUpPauseDuration > 0f)
                    yield return new WaitForSecondsRealtime(expLevelUpPauseDuration);
                continue;
            }

            int applied = Mathf.Min(remainingGain, available);
            int startExp = currentExp;
            int endExp = currentExp + applied;
            float normalizedFill = maxExp > 0 ? applied / (float)maxExp : 0f;
            float duration = Mathf.Max(0.01f, expAnimationDurationPerFill * Mathf.Max(0.18f, normalizedFill));

            for (float time = 0f; time < duration; time += Time.unscaledDeltaTime)
            {
                float normalized = Mathf.Clamp01(time / duration);
                float eased = EvaluateCubicBezierEase(normalized, 0.2f, 0.88f);
                int displayExp = Mathf.RoundToInt(Mathf.Lerp(startExp, endExp, eased));
                UpdateSummaryRow(row, currentLevel, displayExp, maxExp, entry.currentHP, entry.maxHP);
                yield return null;
            }

            currentExp = endExp;
            remainingGain -= applied;
            UpdateSummaryRow(row, currentLevel, currentExp, maxExp, entry.currentHP, entry.maxHP);

            if (currentExp >= maxExp && remainingGain > 0)
            {
                currentLevel += 1;
                currentExp = 0;
                maxExp = ResolveExpMax(currentLevel, 0);
                UpdateSummaryRow(row, currentLevel, currentExp, maxExp, entry.currentHP, entry.maxHP);
                if (expLevelUpPauseDuration > 0f)
                    yield return new WaitForSecondsRealtime(expLevelUpPauseDuration);
            }
        }

        yield return AnimateHpToResolvedState(row, currentLevel, currentExp, maxExp);
        ApplyResolvedSummaryState(row);
    }

    private IEnumerator AnimateHpToResolvedState(SummaryRowView row, int level, int currentExp, int maxExp)
    {
        if (row == null || row.entry == null)
            yield break;

        BattleSettlementEntry entry = row.entry;
        int startHP = entry.currentHP;
        int startMaxHP = entry.maxHP;
        int targetHP = entry.resultingHP > 0 || !entry.isAlive ? entry.resultingHP : entry.currentHP;
        int targetMaxHP = entry.resultingMaxHP > 0 ? entry.resultingMaxHP : entry.maxHP;
        if (startHP == targetHP && startMaxHP == targetMaxHP)
            yield break;

        float duration = Mathf.Max(0.01f, hpAnimationDuration);
        for (float time = 0f; time < duration; time += Time.unscaledDeltaTime)
        {
            float eased = EvaluateCubicBezierEase(Mathf.Clamp01(time / duration), 0.22f, 0.86f);
            int displayHP = Mathf.RoundToInt(Mathf.Lerp(startHP, targetHP, eased));
            int displayMaxHP = Mathf.RoundToInt(Mathf.Lerp(startMaxHP, targetMaxHP, eased));
            UpdateSummaryRow(row, level, currentExp, maxExp, displayHP, displayMaxHP);
            yield return null;
        }

        UpdateSummaryRow(row, level, currentExp, maxExp, targetHP, targetMaxHP);
    }

    private void ApplyResolvedSummaryState(SummaryRowView row)
    {
        BattleSettlementEntry entry = row.entry;
        int resolvedLevel = Mathf.Max(1, entry.resultingLevel > 0 ? entry.resultingLevel : entry.level);
        int resolvedExp = Mathf.Max(0, entry.resultingExperience);
        int resolvedMaxExp = ResolveExpMax(resolvedLevel, 0);
        int resolvedHP = entry.resultingHP > 0 || !entry.isAlive ? entry.resultingHP : entry.currentHP;
        int resolvedMaxHP = entry.resultingMaxHP > 0 ? entry.resultingMaxHP : entry.maxHP;
        UpdateSummaryRow(row, resolvedLevel, resolvedExp, resolvedMaxExp, resolvedHP, resolvedMaxHP);
    }

    private void UpdateSummaryRow(SummaryRowView row, int level, int currentExp, int maxExp, int displayedCurrentHP, int displayedMaxHP)
    {
        if (row.hpText != null)
        {
            row.hpText.color = row.entry.isAlive ? hpTextColor : deadRowColor;
            row.hpText.text = row.entry.isAlive
                ? $"生命 {displayedCurrentHP} / {displayedMaxHP}"
                : $"倒下 {displayedCurrentHP} / {displayedMaxHP}";
        }
        if (row.hpText != null)
        {
            row.hpText.color = row.entry.isAlive ? hpTextColor : deadRowColor;
            row.hpText.text = row.entry.isAlive
                ? $"HP {displayedCurrentHP} / {displayedMaxHP}"
                : $"倒下 {displayedCurrentHP} / {displayedMaxHP}";
        }
        if (row.nameText != null)
            row.nameText.text = BuildSettlementName(row.entry);
        if (row.expText != null)
            row.expText.text = BuildExpLabel(currentExp, maxExp);
        if (row.levelText != null)
            row.levelText.text = $"Lv.{Mathf.Max(1, level)}";
        SetBarFill(row.hpFillRect, row.hpFillWidth, ResolveHpRatio(displayedCurrentHP, displayedMaxHP));
        SetBarFill(row.expFillRect, row.expFillWidth, ResolveExpRatio(currentExp, maxExp));
    }

    private void SetBarFill(RectTransform fillRect, float fillWidth, float ratio)
    {
        if (fillRect == null)
            return;

        fillRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, fillWidth * Mathf.Clamp01(ratio));
    }

    private static float EvaluateCubicBezierEase(float t, float controlY1, float controlY2)
    {
        float normalized = Mathf.Clamp01(t);
        float inverse = 1f - normalized;
        return (3f * inverse * inverse * normalized * Mathf.Clamp01(controlY1))
            + (3f * inverse * normalized * normalized * Mathf.Clamp01(controlY2))
            + (normalized * normalized * normalized);
    }

    private static string BuildSettlementName(BattleSettlementEntry entry)
    {
        return entry != null && !string.IsNullOrWhiteSpace(entry.displayName)
            ? entry.displayName
            : "角色";
    }

    private string BuildHpLabel(BattleSettlementEntry entry, int currentHP, int maxHP)
    {
        string displayName = entry != null && !string.IsNullOrWhiteSpace(entry.displayName)
            ? entry.displayName
            : "角色";
        string stateLabel = entry == null || entry.isAlive ? "生命" : "倒下";
        return $"{displayName}  {stateLabel} {currentHP} / {maxHP}";
    }

    private float ResolveHpRatio(int currentHP, int maxHP)
    {
        if (maxHP <= 0)
            return 0f;

        return Mathf.Clamp01(currentHP / (float)Mathf.Max(1, maxHP));
    }

    private float ResolveExpRatio(int currentExp, int maxExp)
    {
        if (maxExp <= 0)
            return 1f;

        return Mathf.Clamp01(currentExp / (float)Mathf.Max(1, maxExp));
    }

    private int ResolveExpMax(int level, int fallback)
    {
        LevelTable_SO levelTable = battleManager != null ? battleManager.LevelTable : null;
        int configured = levelTable != null ? levelTable.GetExperienceToNextLevel(level) : 0;
        if (configured > 0)
            return configured;

        return Mathf.Max(1, fallback > 0 ? fallback : 100);
    }

    private static string BuildExpLabel(int currentExp, int maxExp)
    {
        if (maxExp <= 0)
            return $"EXP {Mathf.Max(0, currentExp)} / MAX";

        return $"EXP {Mathf.Max(0, currentExp)} / {Mathf.Max(1, maxExp)}";
    }

    private static void AddLayoutElement(
        RectTransform rectTransform,
        float preferredWidth,
        float minWidth,
        float flexibleWidth,
        float preferredHeight = -1f,
        float minHeight = -1f)
    {
        if (rectTransform == null)
            return;

        LayoutElement layoutElement = rectTransform.GetComponent<LayoutElement>();
        if (layoutElement == null)
            layoutElement = rectTransform.gameObject.AddComponent<LayoutElement>();

        layoutElement.preferredWidth = preferredWidth;
        layoutElement.minWidth = minWidth;
        layoutElement.flexibleWidth = flexibleWidth;

        if (preferredHeight >= 0f)
            layoutElement.preferredHeight = preferredHeight;
        if (minHeight >= 0f)
            layoutElement.minHeight = minHeight;
    }

    private static void CreateSpacer(Transform parent, float width)
    {
        if (parent == null || width <= 0f)
            return;

        GameObject spacerGo = new GameObject("Spacer", typeof(RectTransform), typeof(LayoutElement));
        spacerGo.transform.SetParent(parent, false);
        LayoutElement layoutElement = spacerGo.GetComponent<LayoutElement>();
        layoutElement.preferredWidth = width;
        layoutElement.minWidth = width;
        layoutElement.flexibleWidth = 0f;
    }

    private static void ApplyTextOutline(TextMeshProUGUI text, Color outlineColor, float outlineWidth)
    {
        if (text == null)
            return;

        text.fontMaterial = new Material(text.fontMaterial);
        text.fontMaterial.SetColor(ShaderUtilities.ID_OutlineColor, outlineColor);
        text.fontMaterial.SetFloat(ShaderUtilities.ID_OutlineWidth, Mathf.Clamp01(outlineWidth));
        text.UpdateMeshPadding();
    }

    private Sprite GetFallbackLootSprite()
    {
        if (lootFallbackSprite != null)
            return lootFallbackSprite;
        if (runtimeFallbackLootSprite != null)
            return runtimeFallbackLootSprite;

        Texture2D texture = Texture2D.whiteTexture;
        runtimeFallbackLootSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            100f);
        return runtimeFallbackLootSprite;
    }

    private Sprite GetRelicChoiceQuestionSprite(LootRarity rarity)
    {
        switch (rarity)
        {
            case LootRarity.Common:
                return relicChoiceCommonQuestionSprite != null ? relicChoiceCommonQuestionSprite : GetFallbackLootSprite();
            case LootRarity.Uncommon:
                return relicChoiceUncommonQuestionSprite != null ? relicChoiceUncommonQuestionSprite : GetFallbackLootSprite();
            case LootRarity.Rare:
                return relicChoiceRareQuestionSprite != null ? relicChoiceRareQuestionSprite : GetFallbackLootSprite();
            case LootRarity.Epic:
                return relicChoiceEpicQuestionSprite != null ? relicChoiceEpicQuestionSprite : GetFallbackLootSprite();
            case LootRarity.Legendary:
                return relicChoiceLegendaryQuestionSprite != null ? relicChoiceLegendaryQuestionSprite : GetFallbackLootSprite();
            default:
                return GetFallbackLootSprite();
        }
    }

    private Vector2 GetVictoryIntroSize()
    {
        float safeScale = Mathf.Max(0.01f, victoryIntroScale);
        if (victoryIntroSprite == null)
            return new Vector2(512f, 256f) * safeScale;

        Rect spriteRect = victoryIntroSprite.rect;
        return new Vector2(
            Mathf.Max(1f, spriteRect.width) * safeScale,
            Mathf.Max(1f, spriteRect.height) * safeScale);
    }

    private void EnsureEventSystem()
    {
        EventSystemUtility.EnsureSingleEventSystem();
    }
}
