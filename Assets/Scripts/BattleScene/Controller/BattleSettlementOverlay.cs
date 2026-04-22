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
}

[DisallowMultipleComponent]
public class BattleSettlementOverlay : MonoBehaviour
{
    [Serializable]
    private sealed class SummaryRowView
    {
        public BattleSettlementEntry entry;
        public TextMeshProUGUI hpText;
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI expText;
        public TextMeshProUGUI levelText;
        public RectTransform expFillRect;
        public float expFillWidth;
    }

    [Serializable]
    private sealed class LootItemView
    {
        public BattleSettlementLootEntry entry;
        public Button button;
        public Image slotImage;
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
    [SerializeField] private string defeatReturnButtonLabel = "接受命运";
    [SerializeField] private string retryButtonLabel = "Retry Battle";

    [Header("Experience Rows")]
    [SerializeField] private Vector2 expBarSize = new Vector2(420f, 14f);
    [SerializeField] private Color expBarBackgroundColor = new Color(1f, 1f, 1f, 0.12f);
    [SerializeField] private Color expBarFillColor = new Color(1f, 0.84f, 0.26f, 1f);
    [SerializeField] private Color expBarBorderColor = new Color(1f, 1f, 1f, 0.4f);
    [SerializeField] private float expFontSize = 18f;
    [SerializeField] private Color expTextColor = new Color(1f, 0.92f, 0.7f, 1f);
    [SerializeField] private float expAnimationDurationPerFill = 0.45f;
    [SerializeField] private float expLevelUpPauseDuration = 0.14f;

    [Header("Summary Row Layout")]
    [SerializeField] private float summaryRowLeftIndent = 120f;
    [SerializeField] private float summaryHpWidth = 130f;
    [SerializeField] private float summaryNameWidth = 230f;
    [SerializeField] private float summaryExpTextWidth = 110f;
    [SerializeField] private float summaryExpBarWidth = 170f;
    [SerializeField] private float summaryLevelWidth = 70f;

    [Header("Loot Display")]
    [SerializeField] private Vector2 lootSlotSize = new Vector2(84f, 84f);
    [SerializeField] private Vector2 lootIconSize = new Vector2(56f, 56f);
    [SerializeField] private Vector2 lootDetailIconSize = new Vector2(84f, 84f);
    [SerializeField] private Vector2 lootDetailPanelSize = new Vector2(0f, 130f);
    [SerializeField] private float lootDetailInfoWidth = 240f;
    [SerializeField] private float lootDetailDescriptionLeftInset = 36f;
    [SerializeField] private float lootGridSpacing = 12f;
    [SerializeField] private int lootGridColumnCount = 6;
    [SerializeField] private float lootTitleFontSize = 24f;
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
    private RectTransform buttonReserveRoot;
    private TextMeshProUGUI lootHeaderText;
    private RectTransform lootGridRoot;
    private Image lootDetailPanelImage;
    private RectTransform lootDetailRoot;
    private Image lootDetailIcon;
    private TextMeshProUGUI lootDetailNameText;
    private TextMeshProUGUI lootDetailMetaText;
    private TextMeshProUGUI lootDetailDescriptionText;
    private RectTransform buttonRoot;
    private RectTransform victoryIntroRoot;
    private Image victoryIntroImage;
    private readonly List<GameObject> spawnedRows = new List<GameObject>();
    private readonly List<Button> spawnedButtons = new List<Button>();
    private readonly List<SummaryRowView> rowViews = new List<SummaryRowView>();
    private readonly List<GameObject> spawnedLootObjects = new List<GameObject>();
    private readonly List<LootItemView> lootViews = new List<LootItemView>();
    private LootItemView currentSelectedLootView;
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

    public void ShowResult(BattleSettlementData data)
    {
        if (data == null)
            return;

        EnsureEventSystem();
        EnsureCanvas();
        ClearRows();
        ClearLootViews();
        ClearButtons();
        rowViews.Clear();
        lootViews.Clear();
        currentSelectedLootView = null;

        titleText.text = string.IsNullOrWhiteSpace(data.title)
            ? (data.isVictory ? "Victory" : "Defeat")
            : data.title;
        subtitleText.text = data.subtitle ?? string.Empty;
        expText.text = $"EXP +{Mathf.Max(0, data.experienceGained)}";

        BuildSummaryRows(data.partyEntries);
        BuildLootSection(data.lootEntries);
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
            layout.childForceExpandWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandHeight = false;
            layout.spacing = 0f;
            layout.childAlignment = TextAnchor.UpperLeft;

            GameObject contentGo = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup));
            contentGo.transform.SetParent(rowGo.transform, false);
            spawnedRows.Add(contentGo);

            VerticalLayoutGroup contentLayout = contentGo.GetComponent<VerticalLayoutGroup>();
            contentLayout.childControlWidth = true;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childControlHeight = false;
            contentLayout.childForceExpandHeight = false;
            contentLayout.spacing = 6f;

            GameObject topRowGo = new GameObject("TopRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            topRowGo.transform.SetParent(contentGo.transform, false);
            spawnedRows.Add(topRowGo);

            HorizontalLayoutGroup topLayout = topRowGo.GetComponent<HorizontalLayoutGroup>();
            topLayout.childControlWidth = true;
            topLayout.childForceExpandWidth = false;
            topLayout.childControlHeight = true;
            topLayout.childForceExpandHeight = false;
            topLayout.spacing = 12f;
            topLayout.childAlignment = TextAnchor.MiddleLeft;

            CreateSpacer(topRowGo.transform, summaryRowLeftIndent);

            TextMeshProUGUI nameText = CreateLabel(
                topRowGo.transform,
                "NameText",
                rowFontSize,
                entry.isAlive ? rowColor : deadRowColor,
                entry.displayName);
            nameText.alignment = TextAlignmentOptions.MidlineLeft;
            nameText.overflowMode = TextOverflowModes.Ellipsis;
            AddLayoutElement(nameText.rectTransform, summaryNameWidth, summaryNameWidth, 0f, rowFontSize + 10f, rowFontSize + 10f);

            TextMeshProUGUI hpText = CreateLabel(
                topRowGo.transform,
                "HpText",
                rowFontSize,
                entry.isAlive ? hpTextColor : deadRowColor,
                entry.isAlive
                    ? $"HP {entry.currentHP} / {entry.maxHP}"
                    : $"KO   {entry.currentHP} / {entry.maxHP}");
            hpText.alignment = TextAlignmentOptions.MidlineLeft;
            AddLayoutElement(hpText.rectTransform, summaryHpWidth, summaryHpWidth, 0f, rowFontSize + 10f, rowFontSize + 10f);

            TextMeshProUGUI expText = CreateLabel(
                topRowGo.transform,
                "ExpText",
                expFontSize,
                expTextColor,
                BuildExpLabel(entry.currentExperience, ResolveExpMax(entry.level, entry.maxExperience)));
            expText.alignment = TextAlignmentOptions.MidlineRight;
            AddLayoutElement(expText.rectTransform, summaryExpTextWidth, summaryExpTextWidth, 0f, expFontSize + 10f, expFontSize + 10f);

            RectTransform expBarRoot = CreateExpBar(topRowGo.transform, out RectTransform expFillRect, out float expFillWidth);
            spawnedRows.Add(expBarRoot.gameObject);
            SetExpFill(expFillRect, expFillWidth, ResolveExpRatio(entry.currentExperience, ResolveExpMax(entry.level, entry.maxExperience)));

            TextMeshProUGUI levelText = CreateLabel(
                topRowGo.transform,
                "LevelText",
                rowFontSize,
                entry.isAlive ? rowColor : deadRowColor,
                $"Lv.{Mathf.Max(1, entry.level)}");
            levelText.alignment = TextAlignmentOptions.MidlineRight;
            AddLayoutElement(levelText.rectTransform, summaryLevelWidth, summaryLevelWidth, 0f, rowFontSize + 10f, rowFontSize + 10f);

            rowViews.Add(new SummaryRowView
            {
                entry = entry,
                hpText = hpText,
                nameText = nameText,
                expText = expText,
                levelText = levelText,
                expFillRect = expFillRect,
                expFillWidth = expFillWidth
            });
        }
    }

    private void BuildLootSection(IReadOnlyList<BattleSettlementLootEntry> entries)
    {
        if (lootHeaderText == null || lootGridRoot == null || lootDetailRoot == null)
            return;

        ClearLootViews();
        lootViews.Clear();
        currentSelectedLootView = null;

        int count = entries != null ? entries.Count : 0;
        lootHeaderText.text = count > 0 ? $"Loot x{count}" : "Loot";
        UpdateLootGridLayout(count);

        if (count <= 0)
        {
            ShowLootDetail(null, null);
            return;
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

        ShowLootDetail(null, null);
    }

    private void UpdateLootGridLayout(int itemCount)
    {
        if (lootGridRoot == null)
            return;

        int columns = Mathf.Max(1, lootGridColumnCount);
        int rows = Mathf.Max(1, Mathf.CeilToInt(itemCount / (float)columns));
        float height = rows * lootSlotSize.y + Mathf.Max(0, rows - 1) * lootGridSpacing;
        AddLayoutElement(
            lootGridRoot,
            panelSize.x - panelPadding.x * 2f,
            panelSize.x - panelPadding.x * 2f,
            0f,
            height,
            height);
    }

    private LootItemView CreateLootItemView(BattleSettlementLootEntry entry)
    {
        GameObject slotGo = new GameObject($"Loot_{entry.displayName}", typeof(RectTransform), typeof(Image), typeof(Button));
        slotGo.transform.SetParent(lootGridRoot, false);
        spawnedLootObjects.Add(slotGo);

        Image slotImage = slotGo.GetComponent<Image>();
        slotImage.color = lootSlotColor;
        slotImage.sprite = lootSlotBackgroundSprite;
        slotImage.type = lootSlotBackgroundSprite != null ? Image.Type.Sliced : Image.Type.Simple;

        RectTransform slotRect = slotGo.GetComponent<RectTransform>();
        slotRect.sizeDelta = lootSlotSize;

        Image iconImage = CreateImage(slotGo.transform, "Icon", Color.white, entry.icon != null ? entry.icon : GetFallbackLootSprite());
        RectTransform iconRect = iconImage.rectTransform;
        iconRect.anchorMin = new Vector2(0.5f, 0.5f);
        iconRect.anchorMax = new Vector2(0.5f, 0.5f);
        iconRect.pivot = new Vector2(0.5f, 0.5f);
        iconRect.sizeDelta = lootIconSize;
        iconRect.anchoredPosition = Vector2.zero;
        iconImage.preserveAspect = true;

        TextMeshProUGUI countText = CreateLabel(slotGo.transform, "Count", lootBodyFontSize, lootCountColor, $"x{Mathf.Max(1, entry.amount)}");
        RectTransform countRect = countText.rectTransform;
        countRect.anchorMin = new Vector2(1f, 0f);
        countRect.anchorMax = new Vector2(1f, 0f);
        countRect.pivot = new Vector2(1f, 0f);
        countRect.anchoredPosition = new Vector2(-6f, 6f);
        countText.alignment = TextAlignmentOptions.BottomRight;

        Button button = slotGo.GetComponent<Button>();
        LootItemView view = new LootItemView
        {
            entry = entry,
            button = button,
            slotImage = slotImage
        };

        button.onClick.AddListener(() =>
        {
            if (currentSelectedLootView == view)
                ShowLootDetail(null, null);
            else
                ShowLootDetail(entry, view);
        });
        return view;
    }

    private void ShowLootDetail(BattleSettlementLootEntry entry, LootItemView selectedView)
    {
        currentSelectedLootView = selectedView;

        for (int i = 0; i < lootViews.Count; i++)
        {
            LootItemView view = lootViews[i];
            if (view == null || view.slotImage == null)
                continue;

            view.slotImage.color = view == selectedView ? lootSelectedSlotColor : lootSlotColor;
        }

        if (lootDetailPanelImage == null || lootDetailIcon == null || lootDetailNameText == null ||
            lootDetailMetaText == null || lootDetailDescriptionText == null)
            return;

        bool hasEntry = entry != null;
        lootDetailPanelImage.gameObject.SetActive(hasEntry);
        if (!hasEntry)
            return;

        lootDetailIcon.sprite = entry.icon != null ? entry.icon : GetFallbackLootSprite();
        lootDetailIcon.preserveAspect = true;
        lootDetailNameText.text = entry.displayName;

        string typeName = entry.relicData != null
            ? "Relic"
            : (entry.attribute != null && !string.IsNullOrWhiteSpace(entry.attribute.attributeName)
                ? entry.attribute.attributeName
                : "None");
        lootDetailMetaText.text = $"{entry.rarity} | {typeName} | x{Mathf.Max(1, entry.amount)}";
        lootDetailDescriptionText.text = string.IsNullOrWhiteSpace(entry.description)
            ? "No description."
            : entry.description;
    }

    private void BuildButtons(BattleSettlementData data)
    {
        CreateActionButton(
            buttonRoot,
            "ReturnButton",
            data.isVictory ? returnButtonLabel : defeatReturnButtonLabel,
            () => HandleReturnToMap(data.returnSceneName));

        if (!showRetryButton)
            return;

        CreateActionButton(
            buttonRoot,
            "RetryButton",
            retryButtonLabel,
            () => HandleRetryBattle(data.retrySceneName));
    }

    private void HandleReturnToMap(string sceneName)
    {
        BattleEncounterData activeEncounter = BattleEncounterRuntime.ActiveEncounter;
        string targetScene = !string.IsNullOrWhiteSpace(sceneName)
            ? sceneName
            : "MapScene";

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

    private void EnsureCanvas()
    {
        if (overlayCanvas != null)
            return;

        GameObject canvasGo = new GameObject("BattleSettlementCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.transform.SetParent(transform, false);

        overlayCanvas = canvasGo.GetComponent<Canvas>();
        overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        overlayCanvas.sortingOrder = canvasSortOrder;

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
        lootRoot = CreateVerticalRoot(panelRoot, "LootRoot", rowSpacing * 0.45f);
        BuildLootRoot();
        buttonReserveRoot = CreateVerticalRoot(panelRoot, "ButtonReserveRoot", 0f);
        AddLayoutElement(buttonReserveRoot, panelSize.x - panelPadding.x * 2f, panelSize.x - panelPadding.x * 2f, 0f, buttonReservedHeight, buttonReservedHeight);
        buttonRoot = CreateHorizontalRoot(panelRoot, "ButtonRoot", buttonSpacing);
        LayoutElement buttonLayout = buttonRoot.gameObject.AddComponent<LayoutElement>();
        buttonLayout.ignoreLayout = true;

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

        lootHeaderText = CreateLabel(lootRoot, "LootHeader", lootTitleFontSize, lootTitleColor, "Loot");
        lootHeaderText.alignment = TextAlignmentOptions.MidlineLeft;
        lootHeaderText.font = bodyFont != null ? bodyFont : lootHeaderText.font;

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
        AddLayoutElement(lootGridRoot, panelSize.x - panelPadding.x * 2f, panelSize.x - panelPadding.x * 2f, 0f);

        lootDetailPanelImage = CreateImage(lootRoot, "LootDetailPanel", lootDetailPanelColor, lootDetailBackgroundSprite);
        lootDetailRoot = lootDetailPanelImage.rectTransform;
        if (lootDetailPanelSize.y > 0f)
            AddLayoutElement(lootDetailRoot, panelSize.x - panelPadding.x * 2f, panelSize.x - panelPadding.x * 2f, 0f, lootDetailPanelSize.y, lootDetailPanelSize.y);

        HorizontalLayoutGroup detailLayout = lootDetailRoot.gameObject.AddComponent<HorizontalLayoutGroup>();
        detailLayout.padding = new RectOffset(16, 16, 14, 14);
        detailLayout.spacing = 16f;
        detailLayout.childAlignment = TextAnchor.MiddleLeft;
        detailLayout.childControlWidth = false;
        detailLayout.childControlHeight = false;
        detailLayout.childForceExpandWidth = false;
        detailLayout.childForceExpandHeight = false;

        lootDetailIcon = CreateImage(lootDetailRoot, "LootDetailIcon", Color.white, GetFallbackLootSprite());
        RectTransform detailIconRect = lootDetailIcon.rectTransform;
        detailIconRect.sizeDelta = lootDetailIconSize;

        RectTransform detailInfoRoot = CreateVerticalRoot(lootDetailRoot, "LootDetailInfoRoot", 6f);
        AddLayoutElement(detailInfoRoot, lootDetailInfoWidth, lootDetailInfoWidth, 0f);
        VerticalLayoutGroup detailInfoLayout = detailInfoRoot.GetComponent<VerticalLayoutGroup>();
        detailInfoLayout.childControlHeight = true;
        detailInfoLayout.childForceExpandHeight = false;

        lootDetailNameText = CreateLabel(detailInfoRoot, "LootDetailName", lootTitleFontSize, lootTitleColor, "No Loot");
        lootDetailNameText.alignment = TextAlignmentOptions.MidlineLeft;
        AddLayoutElement(lootDetailNameText.rectTransform, 0f, 0f, 1f, lootTitleFontSize + 8f, lootTitleFontSize + 8f);

        lootDetailMetaText = CreateLabel(detailInfoRoot, "LootDetailMeta", lootBodyFontSize, lootTextColor, string.Empty);
        lootDetailMetaText.alignment = TextAlignmentOptions.MidlineLeft;
        AddLayoutElement(lootDetailMetaText.rectTransform, 0f, 0f, 1f, lootBodyFontSize + 8f, lootBodyFontSize + 8f);

        GameObject detailDescriptionSpacer = new GameObject("LootDetailDescriptionSpacer", typeof(RectTransform), typeof(LayoutElement));
        detailDescriptionSpacer.transform.SetParent(lootDetailRoot, false);
        LayoutElement descriptionSpacerLayout = detailDescriptionSpacer.GetComponent<LayoutElement>();
        descriptionSpacerLayout.preferredWidth = lootDetailDescriptionLeftInset;
        descriptionSpacerLayout.minWidth = lootDetailDescriptionLeftInset;
        descriptionSpacerLayout.flexibleWidth = 0f;

        RectTransform detailDescriptionRoot = CreateVerticalRoot(lootDetailRoot, "LootDetailDescriptionRoot", 0f);
        AddLayoutElement(detailDescriptionRoot, panelSize.x - panelPadding.x * 2f - lootDetailIconSize.x - lootDetailInfoWidth - lootDetailDescriptionLeftInset - 72f, 0f, 1f);
        VerticalLayoutGroup detailDescriptionLayout = detailDescriptionRoot.GetComponent<VerticalLayoutGroup>();
        detailDescriptionLayout.childControlHeight = true;
        detailDescriptionLayout.childForceExpandHeight = true;

        lootDetailDescriptionText = CreateLabel(detailDescriptionRoot, "LootDetailDescription", lootBodyFontSize, lootTextColor, string.Empty);
        lootDetailDescriptionText.alignment = TextAlignmentOptions.TopLeft;
        lootDetailDescriptionText.enableWordWrapping = true;
        lootDetailDescriptionText.overflowMode = TextOverflowModes.Ellipsis;
        AddLayoutElement(
            lootDetailDescriptionText.rectTransform,
            0f,
            0f,
            1f,
            lootDetailPanelSize.y > 0f ? lootDetailPanelSize.y - 28f : 96f,
            40f);
        lootDetailPanelImage.gameObject.SetActive(false);
    }

    private RectTransform CreateExpBar(Transform parent, out RectTransform fillRect, out float fillWidth)
    {
        Image border = CreateImage(parent, "ExpBarBorder", expBarBorderColor, null);
        RectTransform borderRect = border.rectTransform;
        Vector2 effectiveBarSize = new Vector2(summaryExpBarWidth, expBarSize.y);
        borderRect.sizeDelta = effectiveBarSize;
        AddLayoutElement(borderRect, effectiveBarSize.x, effectiveBarSize.x, 0f, effectiveBarSize.y, effectiveBarSize.y);

        Image background = CreateImage(borderRect, "ExpBarBackground", expBarBackgroundColor, null);
        RectTransform backgroundRect = background.rectTransform;
        backgroundRect.anchorMin = Vector2.zero;
        backgroundRect.anchorMax = Vector2.one;
        backgroundRect.offsetMin = new Vector2(2f, 2f);
        backgroundRect.offsetMax = new Vector2(-2f, -2f);

        Image fill = CreateImage(backgroundRect, "ExpBarFill", expBarFillColor, null);
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

    private Button CreateActionButton(Transform parent, string name, string label, Action onClick)
    {
        GameObject buttonGo = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonGo.transform.SetParent(parent, false);
        spawnedButtons.Add(buttonGo.GetComponent<Button>());

        Image image = buttonGo.GetComponent<Image>();
        image.color = buttonColor;
        image.sprite = buttonBackgroundSprite;
        image.type = buttonBackgroundSprite != null ? Image.Type.Sliced : Image.Type.Simple;

        RectTransform rect = buttonGo.GetComponent<RectTransform>();
        rect.sizeDelta = buttonSize;

        Button button = buttonGo.GetComponent<Button>();
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
                gridLayout.constraintCount = Mathf.Max(1, lootGridColumnCount);
            }
        }

        if (lootDetailRoot != null && lootDetailPanelSize.y > 0f)
            AddLayoutElement(lootDetailRoot, panelSize.x - panelPadding.x * 2f, panelSize.x - panelPadding.x * 2f, 0f, lootDetailPanelSize.y, lootDetailPanelSize.y);
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
                float eased = 1f - Mathf.Pow(1f - normalized, 3f);
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

        ApplyResolvedSummaryState(row);
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
                ? $"HP {displayedCurrentHP} / {displayedMaxHP}"
                : $"KO   {displayedCurrentHP} / {displayedMaxHP}";
        }
        if (row.nameText != null)
            row.nameText.text = row.entry.displayName;
        if (row.expText != null)
            row.expText.text = BuildExpLabel(currentExp, maxExp);
        if (row.levelText != null)
            row.levelText.text = $"Lv.{Mathf.Max(1, level)}";
        SetExpFill(row.expFillRect, row.expFillWidth, ResolveExpRatio(currentExp, maxExp));
    }

    private void SetExpFill(RectTransform fillRect, float fillWidth, float ratio)
    {
        if (fillRect == null)
            return;

        fillRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, fillWidth * Mathf.Clamp01(ratio));
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
