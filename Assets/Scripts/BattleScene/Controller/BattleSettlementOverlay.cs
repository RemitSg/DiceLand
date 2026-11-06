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
    public int resultingMaxExperience;
    public Sprite portrait;
    public List<int> experienceRequirements = new List<int>();
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
    public List<Relic_SO> refreshPool;
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
    public int moneyGained;
    public int startingMoney;
    public bool hasMoneyBalance;
    public string retrySceneName;
    public List<BattleSettlementEntry> partyEntries = new List<BattleSettlementEntry>();
    public List<BattleSettlementLootEntry> lootEntries = new List<BattleSettlementLootEntry>();
    public BattleSettlementRelicChoiceData relicChoice;
}

[DisallowMultipleComponent]
public partial class BattleSettlementOverlay : MonoBehaviour
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
        public float displayedCredit;
        public int creditedExperience;
        public int shownLevel, shownExperience, shownMaxExperience;
        public RectTransform root;
    }

    [Serializable]
    private sealed class LootItemView
    {
        public BattleSettlementLootEntry entry;
        public Button button;
        public Image slotImage;
        public Image iconImage;
        public TextMeshProUGUI countText;
        public TextMeshProUGUI nameText;
        public CanvasGroup revealGroup;
        public GameObject sealedLabel;
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
    [SerializeField] private float buttonFontSize = 24f;

    [Header("Colors")]
    [SerializeField] private Color titleColor = Color.white;
    [SerializeField] private Color titleOutlineColor = new Color(0.24f, 0.1f, 0.02f, 1f);
    [SerializeField, Range(0f, 1f)] private float titleOutlineWidth = 0.24f;
    [SerializeField] private Color subtitleColor = new Color(0.86f, 0.92f, 1f, 0.95f);
    [SerializeField] private Color expColor = new Color(1f, 0.86f, 0.32f, 1f);
    [SerializeField] private Color deadRowColor = new Color(1f, 0.55f, 0.55f, 1f);
    [SerializeField] private Color hpTextColor = new Color(1f, 0.32f, 0.32f, 1f);
    [SerializeField] private Color buttonColor = new Color(0.18f, 0.24f, 0.34f, 1f);
    [SerializeField] private Color buttonTextColor = Color.white;

    [Header("Buttons")]
    [SerializeField] private Vector2 buttonSize = new Vector2(220f, 62f);
    [SerializeField] private float buttonSpacing = 24f;
    [SerializeField] private float buttonReservedHeight = 120f;
    [SerializeField] private Sprite buttonBackgroundSprite;
    [SerializeField] private bool showRetryButton = true;
    [SerializeField] private string returnButtonLabel = "Return To Map";
    [SerializeField] private string defeatReturnButtonLabel = "Abandon Battle";
    [SerializeField] private string retryButtonLabel = "Retry Battle";

    [Header("Experience Rows")]
    [SerializeField] private Color expTextColor = new Color(1f, 0.92f, 0.7f, 1f);

    [Header("Summary Row Layout")]

    [Header("Loot Display")]
    [SerializeField] private Vector2 lootSlotSize = new Vector2(72f, 72f);
    [SerializeField] private Vector2 lootDetailIconSize = new Vector2(112f, 112f);
    [SerializeField] private Vector2 lootDetailPanelSize = new Vector2(640f, 360f);
    [SerializeField] private float lootGridSpacing = 12f;
    [SerializeField] private int lootGridColumnCount = 6;
    [SerializeField] private float lootTitleFontSize = 22f;
    [SerializeField] private float lootBodyFontSize = 20f;
    [SerializeField] private Color lootTitleColor = new Color(1f, 0.9f, 0.52f, 1f);
    [SerializeField] private Color lootTextColor = new Color(0.95f, 0.95f, 0.95f, 1f);
    [SerializeField] private Color lootSlotColor = new Color(0.18f, 0.15f, 0.12f, 0.95f);
    [SerializeField] private Color lootSelectedSlotColor = new Color(0.42f, 0.32f, 0.16f, 1f);
    [SerializeField] private Color lootDetailPanelColor = new Color(0.12f, 0.09f, 0.07f, 0.95f);
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
        CancelPresentation();
        if (overlayCanvas != null) DestroySettlementObject(overlayCanvas.gameObject);
        if (runtimeFallbackLootSprite != null) DestroySettlementObject(runtimeFallbackLootSprite);
    }

    private void Update()
    {
        if (relicChoiceTooltipRoot != null && relicChoiceTooltipRoot.gameObject.activeSelf)
            PositionRelicChoiceTooltip(GetCurrentPointerScreenPosition());
        if (!IsVisible) return;
        if (settlementViewport != new Vector2(Screen.width, Screen.height) || settlementSafePixels != Screen.safeArea)
            ApplyRuntimeLayoutAdjustments();
        AdvancePresentation(Time.unscaledDeltaTime);
    }

    private void OnDisable()
    {
        HideSharedRelicPanel();
        HideLootDetailModal();
        if (showRoutine != null) { StopCoroutine(showRoutine); showRoutine = null; }
        CancelPresentation();
        SetVisible(false);
    }

    public void ShowResult(BattleSettlementData data)
    {
        if (data == null) return;
        HideSharedRelicPanel();
        if (showRoutine != null) { StopCoroutine(showRoutine); showRoutine = null; }
        CancelPresentation();
        EnsureEventSystem();
        EnsureCanvas();
        ClearRows(); ClearLootViews(); ClearRelicChoiceModalViews(); ClearButtons();
        rowViews.Clear(); lootViews.Clear(); relicChoiceViews.Clear();
        currentSelectedLootView = null; hoveredRelicChoiceView = null; selectedRelicChoiceView = null;
        activeRelicChoiceData = data.relicChoice;
        SetRelicChoiceModalVisible(false);
        HideLootDetailModal();
        titleText.text = string.IsNullOrWhiteSpace(data.title) || data.title == "Victory" || data.title == "Defeat"
            ? (data.isVictory ? GameText.Get("battle.settlement.victory", "战斗胜利") : GameText.Get("battle.settlement.defeat", "战斗失败"))
            : data.title == "Tutorial Failed" ? GameText.Get("battle.settlement.tutorial_defeat", "教学挑战失败") : data.title;
        victoryEnglishText.text = GameText.LanguageCode == GameText.English ? "BATTLE RESULTS" : data.isVictory ? "VICTORY" : "DEFEAT";
        titleText.color = data.isVictory ? new Color32(241, 204, 111, 255) : new Color32(220, 148, 132, 255);
        subtitleText.text = data.subtitle ?? string.Empty;
        BuildSummaryRows(data.partyEntries);
        BuildLootSection(data);
        BuildButtons(data);
        StyleForestButtons();
        LayoutRebuilder.ForceRebuildLayoutImmediate(panelRoot);
        ApplyRuntimeLayoutAdjustments();
        PreparePresentation(data);
        if (Application.isPlaying) showRoutine = StartCoroutine(ShowResultRoutine(data));
        else { SetVisible(true); SetPanelVisible(true); BeginPresentation(); }
    }

    public void HideResult()
    {
        HideSharedRelicPanel();
        HideLootDetailModal();
        if (showRoutine != null)
        {
            StopCoroutine(showRoutine);
            showRoutine = null;
        }

        CancelPresentation();
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
        BattleDeathCameraMotion deathMotion = BattleDeathCameraMotion.FindActive(gameObject.scene);
        if (deathMotion != null)
        {
            SetVisible(false);
            while (deathMotion != null && deathMotion.IsActive) yield return null;
        }
        SetVisible(true);

        bool playVictoryIntro = data.isVictory && victoryIntroSprite != null;
        if (playVictoryIntro)
        {
            SetPanelVisible(false);
            yield return PlayVictoryIntro();
        }

        SetVictoryIntroVisible(false);
        SetPanelVisible(true);
        BeginPresentation();
        showRoutine = null;
    }

    private void BuildSummaryRows(IReadOnlyList<BattleSettlementEntry> entries)
    {
        BuildPortraitSummaryRows(entries);
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
        lootHeaderText.color = SettlementIvory;
        lootHeaderText.text = GameText.Get("battle.settlement.build_loot_section.loot", "战利品");
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

        UpdateLootGridLayout(lootViews.Count);
        HideLootDetailModal();
    }


    private BattleSettlementLootEntry CreateRelicChoiceEntranceEntry(BattleSettlementRelicChoiceData choiceData)
    {
        LootRarity rarity = choiceData != null ? choiceData.Rarity : LootRarity.Common;
        return new BattleSettlementLootEntry
        {
            amount = 1,
            displayName = GameText.Get("battle.relic.reward", relicChoiceEntranceName),
            description = GameText.Get("battle.relic.open_reward", relicChoiceEntranceDescription),
            rarity = rarity,
            icon = GetRelicChoiceQuestionSprite(rarity),
            isRelicChoiceEntrance = true,
            relicChoiceData = choiceData
        };
    }

    private void UpdateLootGridLayout(int itemCount)
    {
        LayoutForestLoot(itemCount);
    }

    private LootItemView CreateLootItemView(BattleSettlementLootEntry entry)
    {
        return BuildForestLootItem(entry);
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
            lootDetailView.HideImmediately();
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

        if (view.nameText != null && presentationEntry != null)
        {
            view.nameText.text = presentationEntry.displayName;
            view.nameText.color = FSRarity(presentationEntry.rarity);
        }

        if (view.countText != null)
        {
            if (view.entry.isRelicChoiceEntrance && view.entry.relicChoiceData != null && view.entry.relicChoiceData.selectedChoice == null)
                view.countText.text = relicChoiceEntranceLabel;
            else
                view.countText.text = $"×{Mathf.Max(1, presentationEntry != null ? presentationEntry.amount : view.entry.amount)}";
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
        BattleEncounterData encounter = BattleEncounterRuntime.ActiveEncounter;
        string victoryLabel = GameText.Get("battle.settlement.return", returnButtonLabel);
        if (encounter != null && encounter.tileType == MapTileType.Boss && !encounter.isTutorialEncounter)
            victoryLabel = GameText.Get("battle.settlement.finish_run", "完成冒险");
        else if (encounter != null && encounter.isTutorialEncounter && encounter.returnToMainMenuOnVictory)
            victoryLabel = GameText.Get("battle.settlement.finish_tutorial", "完成教程");

        CreateActionButton(
            buttonRoot,
            "ReturnButton",
            data.isVictory ? victoryLabel : GameText.Get("battle.settlement.end_run", defeatReturnButtonLabel),
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
            GameText.Get("battle.settlement.retry", retryButtonLabel),
            () => HandleRetryBattle(data != null ? data.retrySceneName : null));
    }

    private void HandleReturnToMap(BattleSettlementData data)
    {
        CompletePresentation();
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
            : GameText.Get("battle.relic.choice_required", relicChoiceRequiredMessage);
        OpenRelicChoiceModal(choiceData);
        if (relicChoiceModalTitleText != null)
        {
            relicChoiceModalTitleText.color = relicChoiceRequiredColor;
            relicChoiceModalTitleText.text = choiceData != null && !string.IsNullOrWhiteSpace(choiceData.requiredMessage)
                ? choiceData.requiredMessage
                : GameText.Get("battle.relic.choice_required", relicChoiceRequiredMessage);
        }
    }

    private void HandleRetryBattle(string sceneName)
    {
        CompletePresentation();
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
        CompletePresentation();
        BattleEncounterData activeEncounter = BattleEncounterRuntime.ActiveEncounter;
        RunState settlementRunState = null;
        if (activeEncounter != null && MapRunRuntime.TryGetSnapshot(activeEncounter.sourceSceneName, out MapRunSnapshot snapshot))
        {
            settlementRunState = snapshot.runState;
            RunSettlementFlowService.SyncResolvedPartyMembers(settlementRunState, activeEncounter);
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
        // Own generated UI even in EditMode, where Awake/OnDestroy may never run.
        // Preview failures must not leave a root canvas in the user's active scene.
        canvasGo.transform.SetParent(transform, false);
        canvasGo.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
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
        ConfigureForestView();
        SetVisible(false);
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

        lootHeaderText = CreateLabel(lootRoot, "LootHeader", lootTitleFontSize, lootTitleColor, GameText.Get("battle.settlement.build_loot_section.loot", "战利品"));
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
        if (overlayRoot == null) return;
        lootDetailView = MapInventoryDetailView.Create(overlayRoot, "LootDetailPanel");
        lootDetailView.ConfigureFonts(titleFont != null ? titleFont : bodyFont, bodyFont);
        lootDetailModalRoot = lootDetailView.ModalRoot;
        lootDetailModalBackdropImage = lootDetailView.Backdrop;
        lootDetailView.Hidden += () =>
        {
            currentSelectedLootView = null;
            UpdateLootSlotColors();
        };
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

        relicChoiceModalTitleText = CreateLabel(panelRect, "RelicChoiceTitle", lootTitleFontSize + 6f, lootTitleColor, GameText.Get("battle.relic.choose_one", relicChoiceHeaderText));
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
        relicChoiceRefreshButton = CreateActionButton(relicChoiceModalButtonRoot, "RelicChoiceRefreshButton", GameText.Get("ui.refresh", relicChoiceRefreshLabel), RefreshRelicChoiceModal, false);
        relicChoiceRefreshButtonText = relicChoiceRefreshButton != null ? relicChoiceRefreshButton.GetComponentInChildren<TextMeshProUGUI>(true) : null;
        CreateActionButton(relicChoiceModalButtonRoot, "RelicChoiceConfirmButton", GameText.Get("ui.confirm", relicChoiceConfirmLabel), CloseRelicChoiceModal, false);
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

        relicChoiceDetailHintText = CreateLabel(relicChoiceTooltipRoot, "RelicChoiceDetailHint", lootBodyFontSize - 2f, subtitleColor, GameText.Get("battle.relic.instructions", relicChoiceDetailEmptyText));
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
        if (TryOpenSharedRelicPanel(choiceData)) return;

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
                : GameText.Get("battle.relic.choose_one", relicChoiceHeaderText);
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

        TextMeshProUGUI hintText = CreateLabel(cardGo.transform, "Hint", lootBodyFontSize - 2f, subtitleColor, GameText.Get("battle.settlement.hover_select", "悬停 / 点击"));
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
                ? $" | {GameText.Get("ui.selected", relicChoiceSelectedPrefix)}"
                : string.Empty;
            relicChoiceDetailMetaText.text = GameText.Format("battle.settlement.relic", "{getLootRarityLabel} | 遗物{selectedText}", ("getLootRarityLabel", GetLootRarityLabel(entry.rarity)), ("selectedText", selectedText));
        }
        if (relicChoiceDetailDescriptionText != null)
            relicChoiceDetailDescriptionText.text = string.IsNullOrWhiteSpace(entry.description) ? GameText.Get("ui.no_description", "暂无描述。") : entry.description;
        if (relicChoiceDetailHintText != null)
            relicChoiceDetailHintText.text = GameText.Get("battle.relic.instructions", relicChoiceDetailEmptyText);

        UpdateRelicChoiceCardColors();
    }

    private void ShowEmptyRelicChoiceDetail()
    {
        SetRelicChoiceTooltipVisible(false);
        if (relicChoiceDetailIcon != null)
            relicChoiceDetailIcon.gameObject.SetActive(false);
        if (relicChoiceDetailNameText != null)
            relicChoiceDetailNameText.text = GameText.Get("battle.relic.choose_one", relicChoiceHeaderText);
        if (relicChoiceDetailMetaText != null)
            relicChoiceDetailMetaText.text = string.Empty;
        if (relicChoiceDetailDescriptionText != null)
            relicChoiceDetailDescriptionText.text = GameText.Get("battle.relic.instructions", relicChoiceDetailEmptyText);
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
            relicChoiceRefreshButtonText.text = $"{GameText.Get("ui.refresh", relicChoiceRefreshLabel)} ({Mathf.Max(0, activeRelicChoiceData.refreshesRemaining)})";
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
                    : GameText.Get("battle.relic.choice_required", relicChoiceRequiredMessage);
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
                return GameText.Get("battle.settlement.uncommon", "精良");
            case LootRarity.Rare:
                return GameText.Get("battle.settlement.rare", "稀有");
            case LootRarity.Epic:
                return GameText.Get("battle.settlement.epic", "史诗");
            case LootRarity.Legendary:
                return GameText.Get("battle.settlement.legendary", "传说");
            case LootRarity.Common:
            default:
                return GameText.Get("ui.difficulty.normal", "普通");
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
        ResourceKeywordText.Attach(text);
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
        ResourceKeywordText.Attach(text);
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
                DestroySettlementObject(spawnedRows[i]);
        }

        spawnedRows.Clear();
    }

    private void ClearButtons()
    {
        for (int i = 0; i < spawnedButtons.Count; i++)
        {
            if (spawnedButtons[i] != null)
                DestroySettlementObject(spawnedButtons[i].gameObject);
        }

        spawnedButtons.Clear();
    }

    private void ClearLootViews()
    {
        for (int i = 0; i < spawnedLootObjects.Count; i++)
        {
            if (spawnedLootObjects[i] != null)
                DestroySettlementObject(spawnedLootObjects[i]);
        }

        spawnedLootObjects.Clear();
    }

    private void ClearRelicChoiceModalViews()
    {
        for (int i = 0; i < spawnedRelicChoiceObjects.Count; i++)
        {
            if (spawnedRelicChoiceObjects[i] != null)
                DestroySettlementObject(spawnedRelicChoiceObjects[i]);
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
        ApplyViewport(new Vector2(Screen.width, Screen.height), Screen.safeArea);
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

    private void ApplyResolvedSummaryState(SummaryRowView row)
    {
        BattleSettlementEntry entry = row.entry;
        int resolvedLevel = Mathf.Max(1, entry.resultingLevel > 0 ? entry.resultingLevel : entry.level);
        int resolvedExp = Mathf.Max(0, entry.resultingLevel > 0 ? entry.resultingExperience : entry.currentExperience);
        int resolvedMaxExp = RequirementFor(row, resolvedLevel);
        int resolvedHP = entry.resultingHP > 0 || !entry.isAlive ? entry.resultingHP : entry.currentHP;
        int resolvedMaxHP = entry.resultingMaxHP > 0 ? entry.resultingMaxHP : entry.maxHP;
        UpdateSummaryRow(row, resolvedLevel, resolvedExp, resolvedMaxExp, resolvedHP, resolvedMaxHP);
    }

    private void UpdateSummaryRow(SummaryRowView row, int level, int currentExp, int maxExp, int displayedCurrentHP, int displayedMaxHP)
    {
        row.shownLevel = level;
        row.shownExperience = currentExp;
        row.shownMaxExperience = maxExp;
        if (row.hpText != null)
        {
            row.hpText.color = row.entry.isAlive ? hpTextColor : deadRowColor;
            row.hpText.text = row.entry.isAlive
                ? $"HP {displayedCurrentHP} / {displayedMaxHP}"
                : GameText.Format("battle.settlement.downed", "倒下 {current} / {max}", ("current", displayedCurrentHP), ("max", displayedMaxHP));
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

    private static string BuildSettlementName(BattleSettlementEntry entry)
    {
        return entry != null && !string.IsNullOrWhiteSpace(entry.displayName)
            ? entry.displayName
            : GameText.Get("battle.settlement.character", "角色");
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
        object maximum = maxExp <= 0 ? (object)"MAX" : Mathf.Max(1, maxExp);
        return GameText.Format("battle.settlement.exp_bar", "经验值 {current} / {max}", ("current", Mathf.Max(0, currentExp)), ("max", maximum));
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
