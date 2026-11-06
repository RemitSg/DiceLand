using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

[DisallowMultipleComponent]
public partial class MapHUD : MonoBehaviour
{
    private const int DefaultCanvasSortingOrder = 8000;
    private const float PartyPanelTopGap = 24f;
    private static readonly Vector2 TopLeftHudOffset = new Vector2(26f, -26f);
    private static readonly Vector2 TopLeftHudSize = new Vector2(580f, 276f);

    private sealed class PartyMemberHudView
    {
        public RectTransform root;
        public Image backgroundImage;
        public Image iconImage;
        public TextMeshProUGUI nameText;
        public Image hpFillImage;
        public TextMeshProUGUI hpText;
        public Image expFillImage;
        public TextMeshProUGUI expText;
    }

    private enum InventoryTab
    {
        Loot,
        Weapons,
        Blueprints,
        Relics
    }

    private const string MainColumnBackgroundAssetPath = "Assets/Assets/UI/MainColumn.png";

    [Header("Fonts")]
    [FormerlySerializedAs("uiFont")]
    [SerializeField] private TMP_FontAsset keywordFont;
    [SerializeField] private TMP_FontAsset bodyFont;
    [SerializeField] private TMP_FontAsset attributeFont;
    [SerializeField] private int titleFontSize = 26;
    [SerializeField] private int bodyFontSize = 22;

    [Header("Party Fonts")]
    [SerializeField] private TMP_FontAsset partyTitleFont;
    [SerializeField] private TMP_FontAsset partyNameFont;
    [SerializeField] private TMP_FontAsset partyStatFont;
    [SerializeField] private int partyTitleFontSize = 26;
    [SerializeField] private int partyNameFontSize = 22;
    [SerializeField] private int partyStatFontSize = 17;

    [Header("Common Colors")]
    [SerializeField] private Color panelColor = new Color(0.1f, 0.12f, 0.18f, 0.92f);
    [SerializeField] private Color titleColor = Color.white;
    [SerializeField] private Color bodyColor = new Color(0.94f, 0.96f, 1f, 1f);
    [SerializeField] private Color accentColor = new Color(1f, 0.84f, 0.36f, 1f);

    [Header("Top HUD")]
    [SerializeField] private Sprite mainColumnBackgroundSprite;
    [SerializeField] private Color mainColumnBackgroundColor = Color.white;

    [Header("Party Panel")]
    [SerializeField] private Vector2 partyPanelSize = new Vector2(420f, 420f);
    [SerializeField] private Vector2 partyPanelCollapsedSize = new Vector2(420f, 64f);
    [SerializeField] private Sprite partyPanelBackgroundSprite;
    [SerializeField] private Color partyPanelColor = new Color(0.1f, 0.12f, 0.18f, 0.94f);
    [SerializeField] private Vector2 partyPanelTitleOffset = new Vector2(18f, -18f);
    [SerializeField] private Vector2 partyPanelTitleSize = new Vector2(360f, 36f);
    [SerializeField] private Vector2 partyToggleButtonOffset = new Vector2(326f, -14f);
    [SerializeField] private Vector2 partyToggleButtonSize = new Vector2(76f, 34f);
    [SerializeField] private Sprite partyToggleButtonSprite;
    [SerializeField] private Color partyToggleButtonColor = new Color(1f, 0.84f, 0.36f, 1f);
    [SerializeField] private Color partyToggleButtonTextColor = Color.black;
    [SerializeField] private string partyExpandedButtonLabel = "折叠";
    [SerializeField] private string partyCollapsedButtonLabel = "展开";
    [SerializeField] private bool partyStartCollapsed;
    [SerializeField] private Vector2 partyListOffset = new Vector2(16f, -60f);
    [SerializeField, Min(0.01f)] private float partyToggleAnimationDuration = 0.22f;
    [SerializeField] private AnimationCurve partyToggleAnimationCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private Vector2 partyMemberCardSize = new Vector2(388f, 108f);
    [SerializeField] private float partyMemberCardSpacing = 12f;
    [SerializeField] private Sprite partyMemberCardSprite;
    [SerializeField] private Color partyMemberCardColor = new Color(0.18f, 0.2f, 0.28f, 0.96f);
    [SerializeField] private Vector2 partyMemberIconOffset = new Vector2(12f, -12f);
    [SerializeField] private Vector2 partyMemberIconSize = new Vector2(84f, 84f);
    [SerializeField] private Vector2 partyMemberNameOffset = new Vector2(108f, -12f);
    [SerializeField] private Vector2 partyMemberNameSize = new Vector2(220f, 28f);
    [SerializeField] private Vector2 partyMemberHpBarOffset = new Vector2(108f, -46f);
    [SerializeField] private Vector2 partyMemberHpBarSize = new Vector2(204f, 18f);
    [SerializeField] private Vector2 partyMemberHpTextOffset = new Vector2(320f, -44f);
    [SerializeField] private Vector2 partyMemberHpTextSize = new Vector2(56f, 22f);
    [SerializeField] private Vector2 partyMemberExpBarOffset = new Vector2(108f, -74f);
    [SerializeField] private Vector2 partyMemberExpBarSize = new Vector2(204f, 14f);
    [SerializeField] private Vector2 partyMemberExpTextOffset = new Vector2(320f, -72f);
    [SerializeField] private Vector2 partyMemberExpTextSize = new Vector2(96f, 22f);
    [SerializeField] private Color partyMemberNameColor = Color.white;
    [SerializeField] private Color partyMemberHpBackgroundColor = new Color(0.2f, 0.12f, 0.14f, 0.95f);
    [SerializeField] private Color partyMemberHpFillColor = new Color(0.92f, 0.28f, 0.32f, 1f);
    [SerializeField] private Color partyMemberHpTextColor = new Color(1f, 0.95f, 0.95f, 1f);
    [SerializeField] private Color partyMemberExpBackgroundColor = new Color(0.12f, 0.18f, 0.28f, 0.95f);
    [SerializeField] private Color partyMemberExpFillColor = new Color(0.3f, 0.78f, 1f, 1f);
    [SerializeField] private Color partyMemberExpTextColor = new Color(0.88f, 0.96f, 1f, 1f);

    [Header("Tile Detail")]
    [SerializeField] private Vector2 tilePanelSize = new Vector2(460f, 260f);
    [SerializeField] private Vector2 tilePanelOffset = new Vector2(-28f, 28f);
    [SerializeField] private Sprite tilePanelBackgroundSprite;
    [SerializeField] private Color tilePanelBackgroundColor = new Color(0.12f, 0.12f, 0.16f, 0.92f);
    [SerializeField] private Vector2 tileTitleOffset = new Vector2(18f, -18f);
    [SerializeField] private Vector2 tileTitleSize = new Vector2(420f, 40f);
    [SerializeField] private Vector2 tileBodyOffset = new Vector2(18f, -62f);
    [SerializeField] private Vector2 tileBodySize = new Vector2(420f, 154f);
    [SerializeField] private Vector2 tileAttributeLabelOffset = new Vector2(18f, -170f);
    [SerializeField] private Vector2 tileAttributeLabelSize = new Vector2(120f, 32f);
    [SerializeField] private Vector2 tileAttributeValueOffset = new Vector2(136f, -170f);
    [SerializeField] private Vector2 tileAttributeValueSize = new Vector2(290f, 54f);
    [SerializeField] private Color tileDangerHighlightColor = new Color(1f, 0.68f, 0.3f, 1f);
    [SerializeField] private Color tileRegionHighlightColor = new Color(0.58f, 0.86f, 1f, 1f);
    [SerializeField] private Color tileAttributeHighlightColor = new Color(0.82f, 1f, 0.72f, 1f);

    [Header("Inventory Button")]
    [SerializeField] private Sprite inventoryButtonSprite;
    [SerializeField] private Color inventoryButtonSpriteColor = Color.white;
    [SerializeField] private Color inventoryButtonFallbackColor = new Color(0.62f, 0.34f, 0.2f, 0.96f);
    [SerializeField] private Color inventoryButtonFallbackGlyphColor = new Color(1f, 0.86f, 0.68f, 1f);
    [SerializeField] private Vector2 inventoryButtonOffset = new Vector2(-32f, -24f);
    [SerializeField] private Vector2 inventoryButtonFallbackSize = new Vector2(132f, 72f);

    [Header("Inventory Panel")]
    [SerializeField] private Vector2 inventoryPanelSize = new Vector2(500f, 610f);
    [SerializeField] private Vector2 inventoryPanelOffset = new Vector2(-24f, -120f);
    [SerializeField] private Sprite inventoryPanelBackgroundSprite;
    [SerializeField] private Color inventoryPanelColor = new Color(0.1f, 0.12f, 0.18f, 0.94f);
    [SerializeField] private Vector2 inventoryPanelBackgroundOffset = Vector2.zero;
    [SerializeField] private Vector2 inventoryPanelBackgroundSize = Vector2.zero;

    [Header("Inventory Tabs")]
    [SerializeField] private Vector2 inventoryTabSize = new Vector2(104f, 52f);
    [SerializeField] private float inventoryTabSpacing = 12f;
    [SerializeField] private Vector2 inventoryTabRootOffset = new Vector2(18f, -18f);
    [SerializeField] private Vector2 lootTabOffset = Vector2.zero;
    [SerializeField] private Vector2 weaponTabOffset = Vector2.zero;
    [SerializeField] private Vector2 blueprintTabOffset = Vector2.zero;
    [SerializeField] private Vector2 relicTabOffset = Vector2.zero;
    [SerializeField] private Sprite inventoryTabSprite;
    [SerializeField] private Sprite inventoryTabSelectedSprite;

    [Header("Inventory Slots")]
    [SerializeField] private Vector2 inventorySlotSize = new Vector2(96f, 96f);
    [SerializeField] private Vector2 inventoryIconSize = new Vector2(64f, 64f);
    [SerializeField] private Vector2 inventoryIconOffset = new Vector2(0f, -8f);
    [SerializeField] private Vector2 inventoryGridSpacing = new Vector2(14f, 14f);
    [SerializeField] private int inventoryGridColumns = 4;
    [SerializeField] private Vector2 inventoryGridOffset = new Vector2(18f, -88f);
    [SerializeField] private Vector2 inventorySlotOffset = Vector2.zero;
    [SerializeField] private Sprite inventorySlotBackgroundSprite;
    [SerializeField] private Color inventorySlotColor = new Color(0.2f, 0.19f, 0.16f, 0.96f);
    [SerializeField] private Color inventorySlotSpriteColor = Color.white;
    [SerializeField] private Color inventoryCountColor = Color.white;
    [SerializeField] private Sprite inventoryFallbackSprite;

    [Header("Inventory Scroll")]
    [SerializeField] private Vector2 inventoryViewportSize = Vector2.zero;
    [SerializeField] private Vector2 inventoryContentSizePadding = Vector2.zero;
    [SerializeField] private float inventoryScrollSensitivity = 30f;
    [SerializeField] private bool inventoryScrollbarAlwaysVisible = true;
    [SerializeField] private float inventoryScrollbarWidth = 18f;
    [SerializeField] private Vector2 inventoryScrollbarOffset = Vector2.zero;
    [SerializeField] private float inventoryScrollbarHandleMinHeight = 48f;
    [SerializeField] private Sprite inventoryScrollbarTrackSprite;
    [SerializeField] private Sprite inventoryScrollbarHandleSprite;
    [SerializeField] private Color inventoryScrollbarTrackColor = new Color(0.08f, 0.08f, 0.1f, 0.78f);
    [SerializeField] private Color inventoryScrollbarHandleColor = new Color(1f, 0.84f, 0.36f, 0.96f);

    [Header("Inventory Detail")]
    [SerializeField] private Sprite inventoryDetailBackgroundSprite;
    [SerializeField] private Vector2 inventoryDetailSize = new Vector2(640f, 240f);
    [SerializeField] private Vector2 inventoryDetailOffset = Vector2.zero;
    [SerializeField] private Vector2 inventoryDetailIconOffset = new Vector2(28f, -28f);
    [SerializeField] private Vector2 inventoryDetailIconSize = new Vector2(112f, 112f);
    [SerializeField] private Vector2 inventoryDetailRelicAdditionalDescriptionOffset = new Vector2(162f, -156f);
    [SerializeField] private Vector2 inventoryDetailRelicAdditionalDescriptionSize = new Vector2(440f, 58f);
    [SerializeField] private Color inventoryDetailRelicAdditionalDescriptionColor = new Color(0.92f, 0.82f, 0.56f, 1f);
    [SerializeField] private Vector2 inventoryDetailActionStatusOffset = new Vector2(162f, -168f);
    [SerializeField] private Vector2 inventoryDetailActionStatusSize = new Vector2(320f, 28f);
    [SerializeField] private Vector2 inventoryDetailActionButtonOffset = new Vector2(162f, -198f);
    [SerializeField] private Vector2 inventoryDetailActionButtonSize = new Vector2(140f, 42f);
    [SerializeField] private Sprite inventoryDetailActionButtonSprite;
    [SerializeField] private Color inventoryDetailActionButtonColor = new Color(1f, 0.84f, 0.36f, 1f);
    [SerializeField] private Color inventoryDetailActionButtonTextColor = Color.black;
    [SerializeField] private Color inventoryDetailActionDisabledColor = new Color(0.42f, 0.42f, 0.42f, 0.9f);

    private Canvas canvas;
    private RectTransform rootRect;
    private Image tilePanelImage;
    private RectTransform tilePanelRect;
    private bool battlePreviewSuppressesDetails;
    private bool detailsWereActiveBeforePreview;
    private TextMeshProUGUI actionPointText;
    private TextMeshProUGUI moneyText;
    private MapResourceFeedback resourceFeedback;
    private TextMeshProUGUI keysText;
    private TextMeshProUGUI calamityText;
    private TextMeshProUGUI difficultyText;
    private TextMeshProUGUI tileTitleText;
    private TextMeshProUGUI tileBodyText;
    private TextMeshProUGUI tileAttributeLabelText;
    private TextMeshProUGUI tileAttributeValueText;
    private TextMeshProUGUI statusText;
    private RectTransform partyPanelRect;
    private Image partyPanelImage;
    private TextMeshProUGUI partyPanelTitleText;
    private TextMeshProUGUI partyEmptyText;
    private RectTransform partyListViewport;
    private RectMask2D partyListMask;
    private RectTransform partyListRoot;
    private Button partyToggleButton;
    private TextMeshProUGUI partyToggleButtonText;
    private readonly List<PartyMemberHudView> partyMemberViews = new List<PartyMemberHudView>();
    private Button inventoryButton;
    private RectTransform inventoryPanelRect;
    private Image inventoryPanelImage;
    private Button lootTabButton;
    private Button weaponTabButton;
    private Button blueprintTabButton;
    private Button relicTabButton;
    private RectTransform inventoryTabRoot;
    private RectTransform inventoryScrollRoot;
    private ScrollRect inventoryScrollRect;
    private RectTransform inventoryViewportRoot;
    private RectTransform inventoryGridRoot;
    private RectTransform inventoryScrollbarRoot;
    private Image inventoryScrollbarTrackImage;
    private RectTransform inventoryScrollbarHandleRoot;
    private Image inventoryScrollbarHandleImage;
    private readonly List<GameObject> inventorySlotObjects = new List<GameObject>();
    private MapInventoryDetailView runtimeInventoryDetailView;
    private InventoryTab currentInventoryTab = InventoryTab.Loot;
    private RunState currentRunState;
    private ActionPointSystem currentActionPointSystem;
    private LevelTable_SO levelTable;
    private BoardTileData currentTile;
    private int currentMoveCost;
    private bool currentDetailed;
    private object selectedInventoryState;
    private Sprite runtimeFallbackSprite;
    private bool isPartyPanelCollapsed;
    private bool partyCollapseStateInitialized;
    private float partyPanelReveal = 1f;
    private Coroutine partyToggleAnimationRoutine;

    public void Initialize()
    {
        EnsureUi();
        if (Application.isPlaying)
        {
            if (resourceFeedback == null) resourceFeedback = GetComponent<MapResourceFeedback>() ?? gameObject.AddComponent<MapResourceFeedback>();
            resourceFeedback.Bind(currentRunState, moneyText);
        }
        ApplyViewport(new Vector2(Screen.width, Screen.height), Screen.safeArea);
        ApplyFonts();
        ApplyPartyPanelLayout();
        ApplyTilePanelLayout();
        ApplyInventoryPanelLayout();
        RefreshPartyPanel();
        RefreshInventoryView();
    }

    public void ConfigureFont(TMP_FontAsset fontAsset)
    {
        keywordFont = fontAsset;
        bodyFont = fontAsset;
        attributeFont = fontAsset;
        Initialize();
    }

    public void ConfigureFonts(TMP_FontAsset keywordFontAsset, TMP_FontAsset bodyFontAsset)
    {
        ConfigureFonts(keywordFontAsset, bodyFontAsset, null);
    }

    public void ConfigureFonts(TMP_FontAsset keywordFontAsset, TMP_FontAsset bodyFontAsset, TMP_FontAsset attributeFontAsset)
    {
        if (keywordFontAsset != null)
            keywordFont = keywordFontAsset;
        if (bodyFontAsset != null)
            bodyFont = bodyFontAsset;
        if (attributeFontAsset != null)
            attributeFont = attributeFontAsset;

        Initialize();
    }

    public void ConfigureLevelTable(LevelTable_SO configuredLevelTable)
    {
        levelTable = configuredLevelTable;
        RefreshPartyPanel();
    }

    public void Refresh(RunState runState, ActionPointSystem actionPointSystem)
    {
        currentRunState = runState;
        currentActionPointSystem = actionPointSystem;
        Initialize();
        if (runtimeInventoryDetailView != null) runtimeInventoryDetailView.ConfigureInventoryContext(runState);

        if (actionPointText != null)
        {
            int current = actionPointSystem != null ? actionPointSystem.CurrentPoints : 0;
            int total = actionPointSystem != null ? actionPointSystem.CurrentMaxPoints : 0;
            actionPointText.text = $"{current}<size=66%><color=#{ColorUtility.ToHtmlStringRGB(new Color(0.62f, 0.95f, 0.82f, 1f))}>/{total}</color></size>";
        }

        if (resourceFeedback != null && resourceFeedback.isActiveAndEnabled)
            resourceFeedback.ObserveMoney(runState != null ? runState.money : 0);
        else if (moneyText != null)
            moneyText.text = (runState != null ? runState.money : 0).ToString();

        if (keysText != null)
        {
            int currentKeys = runState != null ? runState.currentKeys : 0;
            int required = runState != null ? runState.requiredKeys : 0;
            keysText.text = $"{currentKeys}/{required}";
        }

        if (calamityText != null)
            calamityText.text = Mathf.Max(0, runState != null ? runState.calamityTier : 0).ToString();

        if (difficultyText != null)
            difficultyText.text = GetDisplayDifficultyName(runState != null ? runState.difficultyName : string.Empty);

        if (currentTile != null)
            ShowTile(currentTile, currentMoveCost, currentDetailed);

        RefreshPartyPanel();
        RefreshInventoryView();
    }

    public void SetBattlePreviewDetailsSuppressed(bool suppressed)
    {
        if (suppressed) Initialize();
        if (battlePreviewSuppressesDetails == suppressed || tilePanelRect == null) return;
        if (suppressed) detailsWereActiveBeforePreview = tilePanelRect.gameObject.activeSelf;
        battlePreviewSuppressesDetails = suppressed;
        tilePanelRect.gameObject.SetActive(suppressed ? false : detailsWereActiveBeforePreview);
    }

    public void ShowTile(BoardTileData tile, int moveCost = -1, bool detailed = false)
    {
        Initialize();
        bool resetDescription = currentTile == null || tile == null || currentTile.coord != tile.coord || currentDetailed != detailed;
        currentTile = tile;
        currentMoveCost = moveCost;
        currentDetailed = detailed;
        PresentForestTile(tile, moveCost);

        if (tile == null)
        {
            if (tileTitleText != null)
                tileTitleText.text = GameText.Get("map.tile.none", "未选中格子");
            if (tileBodyText != null)
                tileBodyText.text = GameText.Get("map.tile.inspect", "点击任意格子查看详情。");
            if (tileAttributeLabelText != null)
                tileAttributeLabelText.gameObject.SetActive(false);
            if (tileAttributeValueText != null)
                tileAttributeValueText.gameObject.SetActive(false);
            ApplyTilePanelLayout();
            return;
        }

        if (tileTitleText != null)
            tileTitleText.text = GetTileTypeLabel(tile.tileType);

        List<string> lines = new List<string>();

        if (tile.IsHostileTile)
        {
            string attributeLine = BuildAttributeLine(tile.assignedEnemyGroup);
            if (!string.IsNullOrEmpty(attributeLine))
                lines.Add(attributeLine);

            bool isSpecialDungeonTile = tile.sourceSpecialDungeon != null;
            string dangerLine = GameText.Format("map.tile.danger", "危险度 {value}", ("value", MapDangerRatingService.GetRatingRichText(tile.previewDangerRating)));
            if (!isSpecialDungeonTile)
                dangerLine += " | " + GameText.Format("map.tile.region", "区域 {value}", ("value", WrapColor(GetDangerBandLabel(tile.dangerBand), tileRegionHighlightColor)));
            lines.Add(dangerLine);

            string tag = GetPreviewTag(tile);
            if (!string.IsNullOrWhiteSpace(tag))
                lines.Add(GameText.Format("ui.map_hud.preview_tag", "预览标签 {tag}", ("tag", tag)));
            if (detailed)
                lines.Add(GameText.Format("ui.map_hud.encounter_seed", "遭遇种子 {encounterSeed}", ("encounterSeed", tile.encounterSeed)));
        }
        else
        {
            lines.Add(GetTileDescription(tile.tileType));
        }

        if (tileBodyText != null)
            tileBodyText.text = string.Join("\n", lines);
        ApplyTilePanelLayout();
        if (resetDescription && forestDescriptionScroll != null)
            forestDescriptionScroll.verticalNormalizedPosition = 1f;

        if (tileAttributeLabelText != null)
            tileAttributeLabelText.gameObject.SetActive(false);
        if (tileAttributeValueText != null)
            tileAttributeValueText.gameObject.SetActive(false);
    }

    public void ShowStatus(string message)
    {
        Initialize();
        if (statusText != null)
            statusText.text = message ?? string.Empty;
    }

    private void EnsureUi()
    {
        if (canvas != null)
            return;

        GameObject canvasGo = new GameObject("MapHudCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.transform.SetParent(transform, false);
        canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = DefaultCanvasSortingOrder;

        ConfigureForestStyle();
        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(2340f, 1080f);
        scaler.matchWidthOrHeight = 1f;

        forestSafeArea = CreateRect("SafeArea", canvasGo.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        forestSafeArea.anchorMin = Vector2.zero;
        forestSafeArea.anchorMax = Vector2.one;
        forestSafeArea.offsetMin = forestSafeArea.offsetMax = Vector2.zero;
        rootRect = CreateRect("MapHudContent", forestSafeArea, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(2340f, 1080f));

        BuildTopLeftHud();
        BuildPartyPanel();
        BuildTilePanel();
        BuildStatusText();
        BuildInventoryButton();
        BuildInventoryPanel();
        BuildInventoryDetail();
    }

    private void BuildTopLeftHud()
    {
        BuildForestResources();
    }

    private Sprite ResolveMainColumnBackgroundSprite()
    {
        if (mainColumnBackgroundSprite != null)
            return mainColumnBackgroundSprite;

#if UNITY_EDITOR
        mainColumnBackgroundSprite = AssetDatabase.LoadAssetAtPath<Sprite>(MainColumnBackgroundAssetPath);
        if (mainColumnBackgroundSprite != null)
            return mainColumnBackgroundSprite;
#endif

        return null;
    }

    private void BuildPartyPanel()
    {
        partyPanelRect = CreateRect("PartyPanel", rootRect, new Vector2(0f, 1f), ResolvePartyPanelOffset(), partyPanelSize);
        partyPanelImage = partyPanelRect.gameObject.AddComponent<Image>();
        partyPanelImage.type = Image.Type.Sliced;

        partyPanelTitleText = CreateText(partyPanelRect, "PartyTitle", partyPanelTitleOffset, partyPanelTitleSize, partyTitleFontSize, titleColor, GameText.Get("ui.map_hud.party", "当前队伍"), GetPartyTitleFont());
        partyToggleButton = BuildPartyToggleButton(partyPanelRect);
        partyToggleButtonText = partyToggleButton != null ? partyToggleButton.GetComponentInChildren<TextMeshProUGUI>(true) : null;
        partyListViewport = CreateRect("PartyListViewport", partyPanelRect, new Vector2(0f, 1f), partyListOffset, new Vector2(partyPanelSize.x, partyPanelSize.y - 76f));
        partyListMask = partyListViewport.gameObject.AddComponent<RectMask2D>();
        partyEmptyText = CreateText(partyListViewport, "PartyEmpty", Vector2.zero, new Vector2(partyPanelSize.x, 28f), partyStatFontSize, bodyColor, GameText.Get("ui.map_hud.your_party_is_empty", "当前没有队伍成员。"), GetPartyStatFont());
        partyListRoot = CreateRect("PartyList", partyListViewport, new Vector2(0f, 1f), Vector2.zero, new Vector2(partyPanelSize.x, partyPanelSize.y - 76f));

        BuildForestPartyHeader();
        if (!partyCollapseStateInitialized)
        {
            isPartyPanelCollapsed = partyStartCollapsed;
            partyPanelReveal = isPartyPanelCollapsed ? 0f : 1f;
            partyCollapseStateInitialized = true;
        }
    }

    private void BuildTilePanel()
    {
        BuildForestTilePanel();
    }

    private void BuildStatusText()
    {
        statusText = CreateText(rootRect, "StatusText", new Vector2(26f, 66f), new Vector2(650f, 68f), 21, ForestIvory, string.Empty, bodyFont);
        statusText.enableWordWrapping = true;
        statusText.overflowMode = TextOverflowModes.Ellipsis;
        RectTransform rect = statusText.rectTransform;
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(0f, 0f);
        rect.pivot = new Vector2(0f, 0f);
        TextMeshProUGUI hint = CreateText(rootRect, "MapInteractionHint", new Vector2(26f, 24f), new Vector2(1100f, 34f), 23, ForestIvory,
            GameText.Get("map.controls", "点击格子查看详情 · 拖动地图 · 双指缩放"), bodyFont);
        hint.rectTransform.anchorMin = hint.rectTransform.anchorMax = hint.rectTransform.pivot = Vector2.zero;
    }

    private void BuildInventoryButton()
    {
        RectTransform rect = CreateRect("InventoryButton", rootRect, new Vector2(1f, 1f), inventoryButtonOffset, inventoryButtonFallbackSize);
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = inventoryButtonSprite != null ? inventoryButtonSpriteColor : inventoryButtonFallbackColor;
        if (inventoryButtonSprite != null)
        {
            image.sprite = inventoryButtonSprite;
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 2f;
        }
        else
        {
            image.type = Image.Type.Simple;
        }
        BuildInventoryButtonFallbackGlyph(rect);

        inventoryButton = rect.gameObject.AddComponent<Button>();
        inventoryButton.transition = Selectable.Transition.ColorTint;
        inventoryButton.onClick.AddListener(ToggleInventoryPanel);
    }

    private void BuildInventoryButtonFallbackGlyph(RectTransform parent)
    {
        Vector2 barSize = new Vector2(
            Mathf.Max(12f, inventoryButtonFallbackSize.x * 0.48f),
            Mathf.Max(3f, inventoryButtonFallbackSize.y * 0.09f));
        float spacing = Mathf.Max(6f, inventoryButtonFallbackSize.y * 0.2f);

        for (int i = 0; i < 3; i++)
        {
            float y = (1 - i) * spacing;
            Image bar = CreateCenteredImage(
                parent,
                "FallbackMenuBar",
                new Vector2(0f, y),
                barSize,
                null,
                inventoryButtonFallbackGlyphColor);
            bar.raycastTarget = false;
        }
    }

    private void BuildInventoryPanel()
    {
        inventoryPanelRect = CreateRect("InventoryPanel", rootRect, new Vector2(1f, 1f), inventoryPanelOffset, inventoryPanelSize);
        inventoryPanelImage = inventoryPanelRect.gameObject.AddComponent<Image>();
        inventoryPanelImage.type = Image.Type.Sliced;

        inventoryTabRoot = CreateRect("TabRoot", inventoryPanelRect, new Vector2(0f, 1f), inventoryTabRootOffset, new Vector2(inventoryPanelSize.x - 36f, inventoryTabSize.y));
        lootTabButton = BuildTabButton(inventoryTabRoot, "LootTab", Vector2.zero, GameText.Get("battle.settlement.build_loot_section.loot", "战利品"), () => SetInventoryTab(InventoryTab.Loot));
        weaponTabButton = BuildTabButton(inventoryTabRoot, "WeaponTab", Vector2.zero, GameText.Get("ui.map_crafting.weapon", "武器"), () => SetInventoryTab(InventoryTab.Weapons));
        blueprintTabButton = BuildTabButton(inventoryTabRoot, "BlueprintTab", Vector2.zero, GameText.Get("ui.map_hud.blueprints", "图纸"), () => SetInventoryTab(InventoryTab.Blueprints));
        relicTabButton = BuildTabButton(inventoryTabRoot, "RelicTab", Vector2.zero, GameText.Get("ui.map_hud.relics", "遗物"), () => SetInventoryTab(InventoryTab.Relics));

        BuildInventoryScrollArea();
        inventoryPanelRect.gameObject.SetActive(false);
    }

    private void BuildInventoryScrollArea()
    {
        Vector2 viewportSize = ResolveInventoryViewportSize();
        inventoryScrollRoot = CreateRect("InventoryScrollRect", inventoryPanelRect, new Vector2(0f, 1f), inventoryGridOffset, viewportSize);
        Image scrollImage = inventoryScrollRoot.gameObject.AddComponent<Image>();
        scrollImage.color = Color.clear;

        inventoryScrollRect = inventoryScrollRoot.gameObject.AddComponent<ScrollRect>();
        inventoryScrollRect.horizontal = false;
        inventoryScrollRect.vertical = true;
        inventoryScrollRect.scrollSensitivity = inventoryScrollSensitivity;
        inventoryScrollRect.movementType = ScrollRect.MovementType.Clamped;

        inventoryViewportRoot = CreateRect("InventoryViewport", inventoryScrollRoot, new Vector2(0f, 1f), Vector2.zero, ResolveInventoryViewportContentSize(viewportSize));
        inventoryViewportRoot.gameObject.AddComponent<RectMask2D>();

        inventoryGridRoot = CreateRect("InventoryGrid", inventoryViewportRoot, new Vector2(0f, 1f), Vector2.zero, ResolveInventoryContentSize(0));
        inventoryScrollRect.viewport = inventoryViewportRoot;
        inventoryScrollRect.content = inventoryGridRoot;

        BuildInventoryScrollbar();
    }

    private void BuildInventoryScrollbar()
    {
        inventoryScrollbarRoot = CreateRect("VerticalScrollbar", inventoryScrollRoot, new Vector2(1f, 1f), inventoryScrollbarOffset, new Vector2(inventoryScrollbarWidth, ResolveInventoryViewportSize().y));
        inventoryScrollbarRoot.anchorMin = new Vector2(1f, 0f);
        inventoryScrollbarRoot.anchorMax = new Vector2(1f, 1f);
        inventoryScrollbarRoot.pivot = new Vector2(1f, 1f);
        inventoryScrollbarRoot.sizeDelta = new Vector2(inventoryScrollbarWidth, 0f);

        inventoryScrollbarTrackImage = inventoryScrollbarRoot.gameObject.AddComponent<Image>();
        inventoryScrollbarTrackImage.sprite = inventoryScrollbarTrackSprite;
        inventoryScrollbarTrackImage.type = inventoryScrollbarTrackSprite != null ? Image.Type.Sliced : Image.Type.Simple;
        inventoryScrollbarTrackImage.color = inventoryScrollbarTrackColor;

        Scrollbar scrollbar = inventoryScrollbarRoot.gameObject.AddComponent<Scrollbar>();
        scrollbar.direction = Scrollbar.Direction.BottomToTop;

        RectTransform slidingArea = CreateRect("SlidingArea", inventoryScrollbarRoot, new Vector2(0f, 0f), Vector2.zero, Vector2.zero);
        slidingArea.anchorMin = Vector2.zero;
        slidingArea.anchorMax = Vector2.one;
        slidingArea.offsetMin = new Vector2(2f, 2f);
        slidingArea.offsetMax = new Vector2(-2f, -2f);

        inventoryScrollbarHandleRoot = CreateRect("Handle", slidingArea, new Vector2(0f, 1f), Vector2.zero, new Vector2(0f, inventoryScrollbarHandleMinHeight));
        inventoryScrollbarHandleRoot.anchorMin = new Vector2(0f, 1f);
        inventoryScrollbarHandleRoot.anchorMax = new Vector2(1f, 1f);
        inventoryScrollbarHandleRoot.pivot = new Vector2(0.5f, 1f);

        inventoryScrollbarHandleImage = inventoryScrollbarHandleRoot.gameObject.AddComponent<Image>();
        inventoryScrollbarHandleImage.sprite = inventoryScrollbarHandleSprite;
        inventoryScrollbarHandleImage.type = inventoryScrollbarHandleSprite != null ? Image.Type.Sliced : Image.Type.Simple;
        inventoryScrollbarHandleImage.color = inventoryScrollbarHandleColor;

        scrollbar.targetGraphic = inventoryScrollbarHandleImage;
        scrollbar.handleRect = inventoryScrollbarHandleRoot;

        if (inventoryScrollRect != null)
        {
            inventoryScrollRect.verticalScrollbar = scrollbar;
            inventoryScrollRect.verticalScrollbarVisibility = inventoryScrollbarAlwaysVisible
                ? ScrollRect.ScrollbarVisibility.Permanent
                : ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
        }
    }

    private void BuildInventoryDetail()
    {
        runtimeInventoryDetailView = MapInventoryDetailView.Create((RectTransform)canvas.transform, "InventoryDetail");
        runtimeInventoryDetailView.ConfigureFonts(keywordFont, bodyFont);
        runtimeInventoryDetailView.Hidden += () => selectedInventoryState = null;
    }

    private void RefreshPartyPanel()
    {
        if (partyPanelRect == null)
            return;

        IReadOnlyList<RunPartyMemberState> members = currentRunState != null && currentRunState.partyMembers != null
            ? currentRunState.partyMembers
            : null;
        int memberCount = members != null ? members.Count : 0;

        for (int i = 0; i < memberCount; i++)
        {
            PartyMemberHudView view = GetOrCreatePartyMemberView(i);
            RunPartyMemberState member = members[i];
            if (view.root != null)
                view.root.gameObject.SetActive(true);

            ApplyPartyMemberView(view, member);
        }

        for (int i = memberCount; i < partyMemberViews.Count; i++)
        {
            PartyMemberHudView view = partyMemberViews[i];
            if (view != null && view.root != null)
                view.root.gameObject.SetActive(false);
        }

        ApplyPartyPanelLayout();
    }

    private Vector2 ResolvePartyPanelSize()
    {
        int memberCount = currentRunState != null && currentRunState.partyMembers != null ? currentRunState.partyMembers.Count : 0;
        float required = 76f + Mathf.Max(1, memberCount) * partyMemberCardSize.y + Mathf.Max(0, memberCount - 1) * partyMemberCardSpacing;
        float available = rootRect != null ? Mathf.Max(202f, rootRect.rect.height + ResolvePartyPanelOffset().y - 140f) : 614f;
        return new Vector2(partyPanelSize.x, Mathf.Min(required, available));
    }

    private static Vector2 ResolvePartyPanelOffset()
    {
        return new Vector2(TopLeftHudOffset.x, TopLeftHudOffset.y - TopLeftHudSize.y - PartyPanelTopGap);
    }

    private PartyMemberHudView GetOrCreatePartyMemberView(int index)
    {
        while (partyMemberViews.Count <= index)
            partyMemberViews.Add(CreatePartyMemberView(partyMemberViews.Count));

        return partyMemberViews[index];
    }

    private PartyMemberHudView CreatePartyMemberView(int index)
    {
        PartyMemberHudView view = new PartyMemberHudView();
        view.root = CreateRect($"PartyMember_{index}", partyListRoot, new Vector2(0f, 1f), Vector2.zero, partyMemberCardSize);
        view.backgroundImage = view.root.gameObject.AddComponent<Image>();
        view.backgroundImage.sprite = partyMemberCardSprite;
        view.backgroundImage.type = partyMemberCardSprite != null ? Image.Type.Sliced : Image.Type.Simple;
        view.backgroundImage.color = partyMemberCardSprite != null ? Color.white : partyMemberCardColor;
        view.backgroundImage.pixelsPerUnitMultiplier = 1.5f;

        view.iconImage = CreateImage(view.root, "Icon", partyMemberIconOffset, partyMemberIconSize, GetFallbackSprite(), Color.white);
        view.iconImage.preserveAspect = true;

        view.nameText = CreateText(view.root, "Name", partyMemberNameOffset, partyMemberNameSize, partyNameFontSize, partyMemberNameColor, string.Empty, GetPartyNameFont());
        view.hpFillImage = CreateStatusBar(view.root, "HpBar", partyMemberHpBarOffset, partyMemberHpBarSize, partyMemberHpBackgroundColor, partyMemberHpFillColor);
        view.hpText = CreateText(view.root, "HpText", partyMemberHpTextOffset, partyMemberHpTextSize, partyStatFontSize, partyMemberHpTextColor, string.Empty, GetPartyStatFont());
        view.expFillImage = CreateStatusBar(view.root, "ExpBar", partyMemberExpBarOffset, partyMemberExpBarSize, partyMemberExpBackgroundColor, partyMemberExpFillColor);
        view.expText = CreateText(view.root, "ExpText", partyMemberExpTextOffset, partyMemberExpTextSize, partyStatFontSize, partyMemberExpTextColor, string.Empty, GetPartyStatFont());
        if (view.hpText != null)
            view.hpText.alignment = TextAlignmentOptions.TopRight;
        if (view.expText != null)
            view.expText.alignment = TextAlignmentOptions.TopRight;

        ApplyPartyMemberCardLayout(view, index);
        return view;
    }

    private void ApplyPartyMemberCardLayout(PartyMemberHudView view, int index)
    {
        if (view == null || view.root == null)
            return;

        ApplyRect(
            view.root,
            new Vector2(0f, 1f),
            new Vector2(0f, -index * (partyMemberCardSize.y + partyMemberCardSpacing)),
            partyMemberCardSize);

        if (view.iconImage != null)
            ApplyRect(view.iconImage.rectTransform, new Vector2(0f, 1f), partyMemberIconOffset, partyMemberIconSize);
        if (view.nameText != null)
            ApplyRect(view.nameText.rectTransform, new Vector2(0f, 1f), partyMemberNameOffset, partyMemberNameSize);
        if (view.hpText != null)
            ApplyRect(view.hpText.rectTransform, new Vector2(0f, 1f), partyMemberHpTextOffset, partyMemberHpTextSize);
        if (view.expText != null)
            ApplyRect(view.expText.rectTransform, new Vector2(0f, 1f), partyMemberExpTextOffset, partyMemberExpTextSize);

        RectTransform hpBarRoot = view.hpFillImage != null ? view.hpFillImage.transform.parent as RectTransform : null;
        if (hpBarRoot != null)
            ApplyRect(hpBarRoot, new Vector2(0f, 1f), partyMemberHpBarOffset, partyMemberHpBarSize);

        RectTransform expBarRoot = view.expFillImage != null ? view.expFillImage.transform.parent as RectTransform : null;
        if (expBarRoot != null)
            ApplyRect(expBarRoot, new Vector2(0f, 1f), partyMemberExpBarOffset, partyMemberExpBarSize);
    }

    private void ApplyPartyMemberView(PartyMemberHudView view, RunPartyMemberState member)
    {
        if (view == null)
            return;

        string memberName = member != null ? member.DisplayName : GameText.Get("ui.map_hud.unknown_member", "未知成员");
        int level = member != null ? Mathf.Max(1, member.level) : 1;
        int maxHP = member != null ? Mathf.Max(1, member.maxHP) : 1;
        int currentHP = member != null ? Mathf.Clamp(member.currentHP, 0, maxHP) : 0;
        int currentExp = member != null ? Mathf.Max(0, member.experience) : 0;
        int expToNext = levelTable != null ? Mathf.Max(0, levelTable.GetExperienceToNextLevel(level)) : 0;
        float hpRatio = maxHP > 0 ? Mathf.Clamp01(currentHP / (float)maxHP) : 0f;
        float expRatio = expToNext > 0 ? Mathf.Clamp01(currentExp / (float)expToNext) : 1f;

        if (view.iconImage != null)
        {
            Sprite iconSprite = ResolvePartyMemberIcon(member);
            view.iconImage.sprite = iconSprite != null ? iconSprite : GetFallbackSprite();
            view.iconImage.color = Color.white;
            view.iconImage.preserveAspect = true;
        }

        if (view.nameText != null)
            view.nameText.text = $"{memberName}  Lv.{level}";
        if (resourceFeedback != null && resourceFeedback.isActiveAndEnabled)
            resourceFeedback.PresentHealth(member, view.hpFillImage, view.hpText, currentHP, maxHP);
        else
        {
            if (view.hpText != null) view.hpText.text = $"{currentHP}/{maxHP}";
            SetBarFill(view.hpFillImage, hpRatio);
        }
        if (view.expText != null)
            view.expText.text = expToNext > 0 ? $"{currentExp}/{expToNext}" : GameText.Get("ui.map_hud.max_level", "满级");

        SetBarFill(view.expFillImage, expRatio);
    }

    private Sprite ResolvePartyMemberIcon(RunPartyMemberState member)
    {
        if (member == null)
            return GetFallbackSprite();

        if (member.characterData != null)
        {
            if (member.characterData.icon != null)
                return member.characterData.icon;
            if (member.characterData.battleSprite != null)
                return member.characterData.battleSprite;
        }

        return member.BattleSprite != null ? member.BattleSprite : GetFallbackSprite();
    }

    private Image CreateStatusBar(RectTransform parent, string name, Vector2 anchoredPosition, Vector2 size, Color backgroundColor, Color fillColor)
    {
        RectTransform root = CreateRect(name, parent, new Vector2(0f, 1f), anchoredPosition, size);
        Image background = root.gameObject.AddComponent<Image>();
        background.color = backgroundColor;

        RectTransform fillRect = CreateRect("Fill", root, new Vector2(0f, 0.5f), Vector2.zero, size);
        fillRect.anchorMin = new Vector2(0f, 0f);
        fillRect.anchorMax = new Vector2(1f, 1f);
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        Image fillImage = fillRect.gameObject.AddComponent<Image>();
        fillImage.color = fillColor;
        fillImage.type = Image.Type.Simple;
        return fillImage;
    }

    private static void SetBarFill(Image fillImage, float ratio)
    {
        if (fillImage == null)
            return;

        RectTransform rect = fillImage.rectTransform;
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(Mathf.Clamp01(ratio), 1f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private void ApplyFonts()
    {
        RegisterFontAsset(keywordFont);
        RegisterFontAsset(bodyFont);
        RegisterFontAsset(attributeFont);
        RegisterFontAsset(partyTitleFont);
        RegisterFontAsset(partyNameFont);
        RegisterFontAsset(partyStatFont);

        EnsureFontFallback(keywordFont, bodyFont);
        EnsureFontFallback(attributeFont, bodyFont);
        EnsureFontFallback(attributeFont, keywordFont);
        EnsureFontFallback(partyTitleFont, keywordFont);
        EnsureFontFallback(partyTitleFont, bodyFont);
        EnsureFontFallback(partyNameFont, keywordFont);
        EnsureFontFallback(partyNameFont, bodyFont);
        EnsureFontFallback(partyStatFont, bodyFont);
        EnsureFontFallback(partyStatFont, keywordFont);

        ApplyForestFonts();
        ApplyFont(actionPointText, keywordFont);
        ApplyFont(moneyText, keywordFont);
        ApplyFont(keysText, keywordFont);
        ApplyFont(calamityText, keywordFont);
        ApplyFont(difficultyText, keywordFont);
        ApplyFont(tileTitleText, keywordFont);
        ApplyFont(tileBodyText, bodyFont != null ? bodyFont : keywordFont);
        ApplyFont(tileAttributeLabelText, bodyFont != null ? bodyFont : keywordFont);
        ApplyFont(tileAttributeValueText, attributeFont != null ? attributeFont : (bodyFont != null ? bodyFont : keywordFont));
        ApplyFont(statusText, bodyFont != null ? bodyFont : keywordFont);
        ApplyTextStyle(partyPanelTitleText, GetPartyTitleFont(), partyTitleFontSize);
        ApplyTextStyle(partyEmptyText, GetPartyStatFont(), partyStatFontSize);
        ApplyTextStyle(partyToggleButtonText, GetPartyStatFont(), partyStatFontSize);

        for (int i = 0; i < partyMemberViews.Count; i++)
        {
            PartyMemberHudView view = partyMemberViews[i];
            if (view == null)
                continue;

            ApplyTextStyle(view.nameText, GetPartyNameFont(), partyNameFontSize);
            ApplyTextStyle(view.hpText, GetPartyStatFont(), partyStatFontSize);
            ApplyTextStyle(view.expText, GetPartyStatFont(), partyStatFontSize);
        }

        ApplyFontToButton(lootTabButton, keywordFont);
        ApplyFontToButton(weaponTabButton, keywordFont);
        ApplyFontToButton(blueprintTabButton, keywordFont);

        if (runtimeInventoryDetailView != null)
            runtimeInventoryDetailView.ConfigureFonts(keywordFont, bodyFont);
    }

    private void ApplyTilePanelLayout()
    {
        ApplyForestTilePanelLayout();
    }

    private void ApplyPartyPanelLayout()
    {
        if (partyPanelRect == null)
            return;

        Vector2 expandedPanelSize = ResolvePartyPanelSize();
        Vector2 resolvedPanelSize = Vector2.Lerp(partyPanelCollapsedSize, expandedPanelSize, Mathf.Clamp01(partyPanelReveal));
        float titleAvailableWidth = Mathf.Max(140f, partyToggleButtonOffset.x - partyPanelTitleOffset.x - 12f);
        Vector2 resolvedTitleSize = new Vector2(Mathf.Min(partyPanelTitleSize.x, titleAvailableWidth), partyPanelTitleSize.y);
        int layoutCount = currentRunState != null && currentRunState.partyMembers != null ? currentRunState.partyMembers.Count : 0;
        float fullListHeight = Mathf.Max(expandedPanelSize.y - 76f, layoutCount * partyMemberCardSize.y + Mathf.Max(0, layoutCount - 1) * partyMemberCardSpacing);
        float viewportHeight = Mathf.Max(0f, (expandedPanelSize.y - 76f) * Mathf.Clamp01(partyPanelReveal));
        int memberCount = currentRunState != null && currentRunState.partyMembers != null ? currentRunState.partyMembers.Count : 0;
        bool showPartyDetails = partyPanelReveal > 0.001f;
        ApplyRect(partyPanelRect, new Vector2(0f, 1f), ResolvePartyPanelOffset(), resolvedPanelSize);
        if (partyPanelImage != null)
        {
            partyPanelImage.sprite = null;
            partyPanelImage.color = Color.clear;
            partyPanelImage.raycastTarget = false;
        }

        if (partyPanelTitleText != null)
            ApplyRect(partyPanelTitleText.rectTransform, new Vector2(0f, 1f), partyPanelTitleOffset, resolvedTitleSize);
        if (partyToggleButton != null)
        {
            ApplyRect(partyToggleButton.GetComponent<RectTransform>(), new Vector2(0f, 1f), partyToggleButtonOffset, partyToggleButtonSize);
            Image toggleImage = partyToggleButton.GetComponent<Image>();
            if (toggleImage != null)
            {
                toggleImage.sprite = partyToggleButtonSprite;
                toggleImage.type = partyToggleButtonSprite != null ? Image.Type.Sliced : Image.Type.Simple;
                toggleImage.color = partyToggleButtonSprite != null ? Color.white : partyToggleButtonColor;
            }
        }
        if (partyListViewport != null)
        {
            ApplyRect(partyListViewport, new Vector2(0f, 1f), partyListOffset, new Vector2(expandedPanelSize.x, viewportHeight));
            partyListViewport.gameObject.SetActive(showPartyDetails);
        }
        if (partyEmptyText != null)
        {
            ApplyRect(partyEmptyText.rectTransform, new Vector2(0f, 1f), Vector2.zero, new Vector2(expandedPanelSize.x, 28f));
            partyEmptyText.gameObject.SetActive(showPartyDetails && memberCount <= 0);
        }
        if (partyListRoot != null)
        {
            ApplyRect(partyListRoot, new Vector2(0f, 1f), Vector2.zero, new Vector2(expandedPanelSize.x, fullListHeight));
            partyListRoot.gameObject.SetActive(showPartyDetails);
        }

        if (partyToggleButtonText != null)
        {
            partyToggleButtonText.text = isPartyPanelCollapsed ? GameText.Get("ui.expand", partyCollapsedButtonLabel) : GameText.Get("ui.collapse", partyExpandedButtonLabel);
            partyToggleButtonText.color = partyToggleButtonTextColor;
        }

        for (int i = 0; i < partyMemberViews.Count; i++)
            ApplyPartyMemberCardLayout(partyMemberViews[i], i);
    }

    private void ApplyInventoryPanelLayout()
    {
        if (inventoryPanelRect == null)
            return;

        inventoryPanelRect.anchorMin = new Vector2(1f, 1f);
        inventoryPanelRect.anchorMax = new Vector2(1f, 1f);
        inventoryPanelRect.pivot = new Vector2(1f, 1f);
        inventoryPanelRect.anchoredPosition = inventoryPanelOffset;
        inventoryPanelRect.sizeDelta = inventoryPanelSize;

        if (inventoryPanelImage != null)
        {
            inventoryPanelImage.sprite = inventoryPanelBackgroundSprite;
            inventoryPanelImage.color = inventoryPanelBackgroundSprite != null ? Color.white : inventoryPanelColor;
            inventoryPanelImage.type = Image.Type.Sliced;
        }

        if (inventoryTabRoot != null)
            ApplyRect(inventoryTabRoot, new Vector2(0f, 1f), inventoryTabRootOffset, new Vector2(inventoryPanelSize.x - 36f, inventoryTabSize.y));

        PositionTabButton(lootTabButton, inventoryTabRoot, 0, lootTabOffset);
        PositionTabButton(weaponTabButton, inventoryTabRoot, 1, weaponTabOffset);
        PositionTabButton(blueprintTabButton, inventoryTabRoot, 2, blueprintTabOffset);
        PositionTabButton(relicTabButton, inventoryTabRoot, 3, relicTabOffset);

        UpdateInventoryScrollLayout(false);

        if (runtimeInventoryDetailView != null)
            runtimeInventoryDetailView.ApplyViewport(new Vector2(Screen.width, Screen.height), Screen.safeArea);
    }

    private void ToggleInventoryPanel()
    {
        if (inventoryPanelRect == null)
            return;

        bool nextState = !inventoryPanelRect.gameObject.activeSelf;
        inventoryPanelRect.gameObject.SetActive(nextState);
        if (!nextState)
        {
            selectedInventoryState = null;
            if (runtimeInventoryDetailView != null) runtimeInventoryDetailView.HideImmediately();
            ResetCanvasSortOrder();
        }
        else
            RefreshInventoryView();
    }

    public void ShowInventoryPanel(int sortOrderOverride = -1)
    {
        Initialize();
        if (canvas != null)
            canvas.sortingOrder = sortOrderOverride > 0 ? sortOrderOverride : DefaultCanvasSortingOrder;

        if (inventoryPanelRect == null)
            return;

        inventoryPanelRect.gameObject.SetActive(true);
        RefreshInventoryView();
    }

    public void HideInventoryPanel()
    {
        if (inventoryPanelRect != null)
            inventoryPanelRect.gameObject.SetActive(false);

        selectedInventoryState = null;
        if (runtimeInventoryDetailView != null) runtimeInventoryDetailView.HideImmediately();
        ResetCanvasSortOrder();
    }

    public void ResetCanvasSortOrder()
    {
        if (canvas != null)
            canvas.sortingOrder = DefaultCanvasSortingOrder;
    }

    private void HideInventoryDetail()
    {
        selectedInventoryState = null;
        if (runtimeInventoryDetailView != null)
            runtimeInventoryDetailView.Hide();
    }

    private void SetInventoryTab(InventoryTab tab)
    {
        currentInventoryTab = tab;
        selectedInventoryState = null;
        RefreshInventoryView();
        HideInventoryDetail();
    }

    private void RefreshInventoryView()
    {
        if (inventoryPanelRect == null)
            return;

        ApplyTabVisual(lootTabButton, currentInventoryTab == InventoryTab.Loot);
        ApplyTabVisual(weaponTabButton, currentInventoryTab == InventoryTab.Weapons);
        ApplyTabVisual(blueprintTabButton, currentInventoryTab == InventoryTab.Blueprints);
        ApplyTabVisual(relicTabButton, currentInventoryTab == InventoryTab.Relics);

        foreach (GameObject slotObject in inventorySlotObjects)
            Destroy(slotObject);
        inventorySlotObjects.Clear();

        if (!inventoryPanelRect.gameObject.activeSelf || currentRunState == null)
        {
            UpdateInventoryScrollLayout(true);
            return;
        }

        switch (currentInventoryTab)
        {
            case InventoryTab.Loot:
                BuildLootSlots();
                break;
            case InventoryTab.Weapons:
                BuildWeaponSlots();
                break;
            case InventoryTab.Blueprints:
                BuildBlueprintSlots();
                break;
            case InventoryTab.Relics:
                BuildRelicSlots();
                break;
        }

        UpdateInventoryScrollLayout(true);
        RefreshSelectedInventoryDetail();
    }

    private void BuildLootSlots()
    {
        if (currentRunState.lootInventory == null)
            return;

        int visibleIndex = 0;
        foreach (RunLootState lootState in currentRunState.lootInventory)
        {
            if (lootState == null || lootState.lootItem == null || lootState.amount <= 0)
                continue;

            CreateInventorySlot(visibleIndex++, lootState.lootItem.icon, lootState.amount.ToString(), () =>
            {
                if (Equals(selectedInventoryState, lootState))
                {
                    HideInventoryDetail();
                    return;
                }

                selectedInventoryState = lootState;
                runtimeInventoryDetailView.Show(lootState, GetFallbackSprite());
                runtimeInventoryDetailView.ConfigureActionButton(string.Empty, false, null, string.Empty);
            });
        }
    }

    private void BuildWeaponSlots()
    {
        if (currentRunState.weaponInventory == null)
            return;

        int visibleIndex = 0;
        foreach (RunWeaponState weaponState in currentRunState.weaponInventory)
        {
            if (weaponState == null || weaponState.weaponData == null || weaponState.amount <= 0)
                continue;

            CreateInventorySlot(visibleIndex++, weaponState.weaponData.icon, weaponState.amount.ToString(), () =>
            {
                if (Equals(selectedInventoryState, weaponState))
                {
                    HideInventoryDetail();
                    return;
                }

                selectedInventoryState = weaponState;
                runtimeInventoryDetailView.Show(weaponState, GetFallbackSprite());
                RefreshSelectedInventoryDetail();
            });
        }
    }

    private void BuildBlueprintSlots()
    {
        if (currentRunState.blueprintInventory == null)
            return;

        int visibleIndex = 0;
        foreach (RunBlueprintState blueprintState in currentRunState.blueprintInventory)
        {
            if (blueprintState == null || blueprintState.blueprintData == null || blueprintState.amount <= 0)
                continue;

            CreateInventorySlot(visibleIndex++, blueprintState.blueprintData.DisplayIcon, blueprintState.amount.ToString(), () =>
            {
                if (Equals(selectedInventoryState, blueprintState))
                {
                    HideInventoryDetail();
                    return;
                }

                selectedInventoryState = blueprintState;
                runtimeInventoryDetailView.Show(blueprintState, GetFallbackSprite());
                runtimeInventoryDetailView.ConfigureActionButton(string.Empty, false, null, string.Empty);
            });
        }
    }

    private void BuildRelicSlots()
    {
        if (currentRunState.relicInventory == null)
            return;

        int visibleIndex = 0;
        foreach (RunRelicState relicState in currentRunState.relicInventory)
        {
            if (relicState == null || relicState.relicData == null || relicState.amount <= 0)
                continue;

            CreateInventorySlot(visibleIndex++, relicState.relicData.icon, relicState.amount.ToString(), () =>
            {
                if (Equals(selectedInventoryState, relicState))
                {
                    HideInventoryDetail();
                    return;
                }

                selectedInventoryState = relicState;
                runtimeInventoryDetailView.Show(relicState, GetFallbackSprite());
                runtimeInventoryDetailView.ConfigureActionButton(string.Empty, false, null, string.Empty);
            });
        }
    }

    private void CreateInventorySlot(int index, Sprite iconSprite, string countText, UnityEngine.Events.UnityAction onClick)
    {
        int columns = Mathf.Max(1, inventoryGridColumns);
        int row = index / columns;
        int column = index % columns;
        float x = column * (inventorySlotSize.x + inventoryGridSpacing.x);
        float y = row * (inventorySlotSize.y + inventoryGridSpacing.y);
        Vector2 slotPosition = new Vector2(x + inventorySlotOffset.x, -y + inventorySlotOffset.y);

        RectTransform slotRect = CreateRect($"Slot_{index}", inventoryGridRoot, new Vector2(0f, 1f), slotPosition, inventorySlotSize);
        Image slotImage = slotRect.gameObject.AddComponent<Image>();
        slotImage.sprite = inventorySlotBackgroundSprite;
        slotImage.type = Image.Type.Sliced;
        slotImage.color = inventorySlotBackgroundSprite != null ? inventorySlotSpriteColor : inventorySlotColor;

        Button button = slotRect.gameObject.AddComponent<Button>();
        button.onClick.AddListener(onClick);

        Image icon = CreateCenteredImage(
            slotRect,
            "Icon",
            Vector2.zero,
            inventoryIconSize,
            iconSprite != null ? iconSprite : GetFallbackSprite(),
            Color.white);
        icon.preserveAspect = true;
        MapItemIconFit.Attach(icon);

        TextMeshProUGUI count = CreateText(slotRect, "Count", new Vector2(0f, -(inventorySlotSize.y - 34f)), new Vector2(inventorySlotSize.x - 10f, 24f), bodyFontSize - 2, inventoryCountColor, $"x{countText}", keywordFont);
        count.alignment = TextAlignmentOptions.BottomRight;
        count.raycastTarget = false;

        inventorySlotObjects.Add(slotRect.gameObject);
    }

    private Vector2 ResolveInventoryViewportSize()
    {
        Vector2 defaultSize = new Vector2(
            Mathf.Max(1f, inventoryPanelSize.x - 36f),
            Mathf.Max(1f, inventoryPanelSize.y - inventoryTabSize.y - 54f));

        return new Vector2(
            inventoryViewportSize.x > 0f ? inventoryViewportSize.x : defaultSize.x,
            inventoryViewportSize.y > 0f ? inventoryViewportSize.y : defaultSize.y);
    }

    private Vector2 ResolveInventoryViewportContentSize(Vector2 viewportSize)
    {
        float reservedScrollbarWidth = Mathf.Max(0f, inventoryScrollbarWidth);
        float width = Mathf.Max(1f, viewportSize.x - reservedScrollbarWidth - Mathf.Max(0f, inventoryScrollbarOffset.x));
        return new Vector2(width, Mathf.Max(1f, viewportSize.y));
    }

    private Vector2 ResolveInventoryContentSize(int slotCount)
    {
        Vector2 viewportSize = ResolveInventoryViewportSize();
        Vector2 contentViewportSize = ResolveInventoryViewportContentSize(viewportSize);
        int columns = Mathf.Max(1, inventoryGridColumns);
        int rows = slotCount > 0 ? Mathf.CeilToInt(slotCount / (float)columns) : 0;
        float occupiedWidth = columns * inventorySlotSize.x + Mathf.Max(0, columns - 1) * inventoryGridSpacing.x + Mathf.Max(0f, inventoryContentSizePadding.x);
        float occupiedHeight = rows > 0
            ? rows * inventorySlotSize.y + Mathf.Max(0, rows - 1) * inventoryGridSpacing.y + Mathf.Abs(inventorySlotOffset.y) + Mathf.Max(0f, inventoryContentSizePadding.y)
            : contentViewportSize.y;

        return new Vector2(
            Mathf.Max(contentViewportSize.x, occupiedWidth),
            Mathf.Max(contentViewportSize.y, occupiedHeight));
    }

    private void UpdateInventoryScrollLayout(bool resetToTop)
    {
        if (inventoryScrollRoot == null || inventoryGridRoot == null)
            return;

        Vector2 viewportSize = ResolveInventoryViewportSize();
        Vector2 viewportContentSize = ResolveInventoryViewportContentSize(viewportSize);
        Vector2 contentSize = ResolveInventoryContentSize(inventorySlotObjects.Count);
        bool hasOverflow = contentSize.y > viewportContentSize.y + 0.01f;

        ApplyRect(inventoryScrollRoot, new Vector2(0f, 1f), inventoryGridOffset, viewportSize);

        if (inventoryViewportRoot != null)
            ApplyRect(inventoryViewportRoot, new Vector2(0f, 1f), Vector2.zero, viewportContentSize);

        ApplyRect(inventoryGridRoot, new Vector2(0f, 1f), Vector2.zero, contentSize);

        if (inventoryScrollRect != null)
        {
            inventoryScrollRect.horizontal = false;
            inventoryScrollRect.vertical = hasOverflow;
            inventoryScrollRect.scrollSensitivity = inventoryScrollSensitivity;
            inventoryScrollRect.movementType = ScrollRect.MovementType.Clamped;
            inventoryScrollRect.viewport = inventoryViewportRoot;
            inventoryScrollRect.content = inventoryGridRoot;
            inventoryScrollRect.verticalScrollbarVisibility = inventoryScrollbarAlwaysVisible
                ? ScrollRect.ScrollbarVisibility.Permanent
                : ScrollRect.ScrollbarVisibility.AutoHide;

            if (resetToTop)
                inventoryScrollRect.verticalNormalizedPosition = 1f;
        }

        ApplyInventoryScrollbarLayout(viewportSize, hasOverflow);
    }

    private void ApplyInventoryScrollbarLayout(Vector2 viewportSize, bool hasOverflow)
    {
        if (inventoryScrollbarRoot == null)
            return;

        bool showScrollbar = inventoryScrollbarAlwaysVisible || hasOverflow;
        inventoryScrollbarRoot.gameObject.SetActive(showScrollbar);
        ApplyRect(inventoryScrollbarRoot, new Vector2(1f, 1f), inventoryScrollbarOffset, new Vector2(inventoryScrollbarWidth, viewportSize.y));
        inventoryScrollbarRoot.anchorMin = new Vector2(1f, 0f);
        inventoryScrollbarRoot.anchorMax = new Vector2(1f, 1f);
        inventoryScrollbarRoot.pivot = new Vector2(1f, 1f);
        inventoryScrollbarRoot.sizeDelta = new Vector2(inventoryScrollbarWidth, 0f);

        if (inventoryScrollbarTrackImage != null)
        {
            inventoryScrollbarTrackImage.sprite = inventoryScrollbarTrackSprite;
            inventoryScrollbarTrackImage.type = inventoryScrollbarTrackSprite != null ? Image.Type.Sliced : Image.Type.Simple;
            inventoryScrollbarTrackImage.color = inventoryScrollbarTrackColor;
        }

        if (inventoryScrollbarHandleRoot != null)
            inventoryScrollbarHandleRoot.sizeDelta = new Vector2(0f, Mathf.Max(1f, inventoryScrollbarHandleMinHeight));

        if (inventoryScrollbarHandleImage != null)
        {
            inventoryScrollbarHandleImage.sprite = inventoryScrollbarHandleSprite;
            inventoryScrollbarHandleImage.type = inventoryScrollbarHandleSprite != null ? Image.Type.Sliced : Image.Type.Simple;
            inventoryScrollbarHandleImage.color = inventoryScrollbarHandleColor;
        }
    }

    private Button BuildTabButton(RectTransform parent, string name, Vector2 anchoredPosition, string label, UnityEngine.Events.UnityAction onClick)
    {
        RectTransform rect = CreateRect(name, parent, new Vector2(0f, 1f), anchoredPosition, inventoryTabSize);
        Image image = rect.gameObject.AddComponent<Image>();
        image.type = Image.Type.Sliced;
        Button button = rect.gameObject.AddComponent<Button>();
        button.onClick.AddListener(onClick);

        TextMeshProUGUI text = CreateText(rect, "Label", Vector2.zero, inventoryTabSize, bodyFontSize, Color.black, label, keywordFont);
        text.alignment = TextAlignmentOptions.Center;
        return button;
    }

    private void ApplyTabVisual(Button button, bool selected)
    {
        if (button == null)
            return;

        Image image = button.GetComponent<Image>();
        if (image != null)
        {
            image.sprite = selected ? (inventoryTabSelectedSprite != null ? inventoryTabSelectedSprite : inventoryTabSprite) : inventoryTabSprite;
            image.color = Color.white;
        }

        TMP_Text text = button.GetComponentInChildren<TMP_Text>(true);
        if (text != null)
        {
            ApplyFont(text, keywordFont);
            text.color = selected ? Color.black : bodyColor;
        }
    }

    private void PositionTabButton(Button button, RectTransform parent, int index, Vector2 extraOffset)
    {
        if (button == null || parent == null)
            return;

        float baseX = index * (inventoryTabSize.x + inventoryTabSpacing);
        ApplyRect(button.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(baseX + extraOffset.x, extraOffset.y), inventoryTabSize);
    }

    private Button BuildSimpleButton(RectTransform parent, string name, Vector2 anchoredPosition, Vector2 size, string label, UnityEngine.Events.UnityAction onClick)
    {
        RectTransform rect = CreateRect(name, parent, new Vector2(0f, 1f), anchoredPosition, size);
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = accentColor;
        Button button = rect.gameObject.AddComponent<Button>();
        button.onClick.AddListener(onClick);

        TextMeshProUGUI text = CreateText(rect, "Label", Vector2.zero, size, bodyFontSize, Color.black, label, keywordFont);
        text.alignment = TextAlignmentOptions.Center;
        return button;
    }

    private Button BuildPartyToggleButton(RectTransform parent)
    {
        RectTransform rect = CreateRect("PartyToggleButton", parent, new Vector2(0f, 1f), partyToggleButtonOffset, partyToggleButtonSize);
        Image image = rect.gameObject.AddComponent<Image>();
        image.sprite = partyToggleButtonSprite;
        image.type = partyToggleButtonSprite != null ? Image.Type.Sliced : Image.Type.Simple;
        image.pixelsPerUnitMultiplier = 2.5f;
        image.color = partyToggleButtonSprite != null ? Color.white : partyToggleButtonColor;

        Button button = rect.gameObject.AddComponent<Button>();
        button.onClick.AddListener(TogglePartyPanelCollapsed);

        TextMeshProUGUI text = CreateText(rect, "Label", Vector2.zero, partyToggleButtonSize, partyStatFontSize, partyToggleButtonTextColor, string.Empty, GetPartyStatFont());
        text.alignment = TextAlignmentOptions.Center;
        return button;
    }

    private Button BuildInventoryActionButton(RectTransform parent, string name, Vector2 anchoredPosition, Vector2 size, string label, UnityEngine.Events.UnityAction onClick)
    {
        RectTransform rect = CreateRect(name, parent, new Vector2(0f, 1f), anchoredPosition, size);
        Image image = rect.gameObject.AddComponent<Image>();
        image.sprite = inventoryDetailActionButtonSprite;
        image.type = inventoryDetailActionButtonSprite != null ? Image.Type.Sliced : Image.Type.Simple;
        image.color = inventoryDetailActionButtonColor;

        Button button = rect.gameObject.AddComponent<Button>();
        if (onClick != null)
            button.onClick.AddListener(onClick);

        TextMeshProUGUI text = CreateText(rect, "Label", Vector2.zero, size, bodyFontSize, inventoryDetailActionButtonTextColor, label, keywordFont);
        text.alignment = TextAlignmentOptions.Center;
        rect.gameObject.SetActive(false);
        return button;
    }

    private static RectTransform CreateRect(string name, RectTransform parent, Vector2 anchorPivot, Vector2 anchoredPosition, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = anchorPivot;
        rect.anchorMax = anchorPivot;
        rect.pivot = anchorPivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
        return rect;
    }

    private TextMeshProUGUI CreateText(RectTransform parent, string name, Vector2 anchoredPosition, Vector2 size, int fontSize, Color color, string text, TMP_FontAsset font)
    {
        RectTransform rect = CreateRect(name, parent, new Vector2(0f, 1f), anchoredPosition, size);
        TextMeshProUGUI tmp = rect.gameObject.AddComponent<TextMeshProUGUI>();
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.text = text;
        tmp.richText = true;
        tmp.alignment = TextAlignmentOptions.TopLeft;
        tmp.enableWordWrapping = false;
        tmp.raycastTarget = false;
        ApplyFont(tmp, font);
        ResourceKeywordText.Attach(tmp, name != "MoveCostUnit" && name != "TileTitle");
        return tmp;
    }

    private TextMeshProUGUI CreateCenteredText(RectTransform parent, string name, Vector2 anchoredPosition, Vector2 size, int fontSize, Color color, string text, TMP_FontAsset font)
    {
        RectTransform rect = CreateRect(name, parent, new Vector2(0.5f, 0.5f), anchoredPosition, size);
        TextMeshProUGUI tmp = rect.gameObject.AddComponent<TextMeshProUGUI>();
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.text = text;
        tmp.richText = true;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableWordWrapping = false;
        tmp.raycastTarget = false;
        ApplyFont(tmp, font);
        ResourceKeywordText.Attach(tmp, name != "MoveCostUnit" && name != "TileTitle");
        return tmp;
    }

    private Image CreateImage(RectTransform parent, string name, Vector2 anchoredPosition, Vector2 size, Sprite sprite, Color color)
    {
        RectTransform rect = CreateRect(name, parent, new Vector2(0f, 1f), anchoredPosition, size);
        Image image = rect.gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        return image;
    }

    private Image CreateCenteredImage(RectTransform parent, string name, Vector2 anchoredPosition, Vector2 size, Sprite sprite, Color color)
    {
        RectTransform rect = CreateRect(name, parent, new Vector2(0.5f, 0.5f), anchoredPosition, size);
        Image image = rect.gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        return image;
    }

    private static void ApplyRect(RectTransform rect, Vector2 anchorPivot, Vector2 anchoredPosition, Vector2 size)
    {
        if (rect == null)
            return;

        rect.anchorMin = anchorPivot;
        rect.anchorMax = anchorPivot;
        rect.pivot = anchorPivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
    }

    private void ApplyFontToButton(Button button, TMP_FontAsset font)
    {
        if (button == null)
            return;

        TMP_Text[] texts = button.GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < texts.Length; i++)
            ApplyFont(texts[i], font);
    }

    private void TogglePartyPanelCollapsed()
    {
        isPartyPanelCollapsed = !isPartyPanelCollapsed;
        if (partyToggleAnimationRoutine != null)
            StopCoroutine(partyToggleAnimationRoutine);

        float targetReveal = isPartyPanelCollapsed ? 0f : 1f;
        partyToggleAnimationRoutine = StartCoroutine(AnimatePartyPanelReveal(targetReveal));
    }

    private System.Collections.IEnumerator AnimatePartyPanelReveal(float targetReveal)
    {
        float startReveal = partyPanelReveal;
        float duration = Mathf.Max(0.01f, partyToggleAnimationDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float normalized = Mathf.Clamp01(elapsed / duration);
            float eased = partyToggleAnimationCurve != null
                ? partyToggleAnimationCurve.Evaluate(normalized)
                : normalized;
            partyPanelReveal = Mathf.Lerp(startReveal, targetReveal, eased);
            ApplyPartyPanelLayout();
            yield return null;
        }

        partyPanelReveal = targetReveal;
        ApplyPartyPanelLayout();
        partyToggleAnimationRoutine = null;
    }

    private void ApplyFont(TMP_Text text, TMP_FontAsset font)
    {
        if (text == null || font == null)
            return;

        text.font = font;
        text.UpdateMeshPadding();
        text.SetAllDirty();
    }

    private void ApplyTextStyle(TMP_Text text, TMP_FontAsset font, int fontSize)
    {
        if (text == null)
            return;

        text.fontSize = Mathf.Max(1, fontSize);
        ApplyFont(text, font);
    }

    private TMP_FontAsset GetPartyTitleFont()
    {
        if (partyTitleFont != null)
            return partyTitleFont;
        if (keywordFont != null)
            return keywordFont;
        return bodyFont;
    }

    private TMP_FontAsset GetPartyNameFont()
    {
        if (partyNameFont != null)
            return partyNameFont;
        if (keywordFont != null)
            return keywordFont;
        return bodyFont;
    }

    private TMP_FontAsset GetPartyStatFont()
    {
        if (partyStatFont != null)
            return partyStatFont;
        if (bodyFont != null)
            return bodyFont;
        return keywordFont;
    }

    private static void EnsureFontFallback(TMP_FontAsset primary, TMP_FontAsset fallback)
    {
        if (primary == null || fallback == null || primary == fallback)
            return;

        if (primary.fallbackFontAssetTable == null)
            primary.fallbackFontAssetTable = new List<TMP_FontAsset>();

        if (!primary.fallbackFontAssetTable.Contains(fallback))
            primary.fallbackFontAssetTable.Add(fallback);
    }

    private static void RegisterFontAsset(TMP_FontAsset fontAsset)
    {
        if (fontAsset == null)
            return;

        MaterialReferenceManager.AddFontAsset(fontAsset);
    }

    private Sprite GetFallbackSprite()
    {
        if (inventoryFallbackSprite != null)
            return inventoryFallbackSprite;

        if (runtimeFallbackSprite != null)
            return runtimeFallbackSprite;

        Texture2D texture = Texture2D.whiteTexture;
        runtimeFallbackSprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
        return runtimeFallbackSprite;
    }

    private static string GetDisplayDifficultyName(string difficultyName)
    {
        if (string.IsNullOrWhiteSpace(difficultyName)) return GameText.Get("ui.difficulty.normal", "普通");
        switch (difficultyName.Trim().ToLowerInvariant())
        {
            case "easy": return GameText.Get("ui.difficulty.easy", "简单");
            case "normal": case "medium": return GameText.Get("ui.difficulty.normal", "普通");
            case "hard": return GameText.Get("ui.difficulty.hard", "困难");
            case "extreme": return GameText.Get("ui.difficulty.extreme", "极难");
            default: return difficultyName;
        }
    }

    private static string GetPreviewTag(BoardTileData tile)
    {
        if (tile.sourceSpecialDungeon == null)
        {
            // Old saves keep the authored preview tags. Translate them only for display.
            switch (tile.previewTag)
            {
                case "教程终战": return GameText.Get("map.tutorial.tag.final_battle", "教程终战");
                case "教程战斗": return GameText.Get("map.tutorial.tag.battle", "教程战斗");
                case "教程营地": return GameText.Get("map.tutorial.tag.rest", "教程营地");
                case "教程商店": return GameText.Get("map.tutorial.tag.shop", "教程商店");
                case "教程锻造": return GameText.Get("map.tutorial.tag.forge", "教程锻造");
                case "教程钥匙": return GameText.Get("map.tutorial.tag.key", "教程钥匙");
                case "教程首领": return GameText.Get("map.tutorial.tag.boss", "教程首领");
                case "教程路线": return GameText.Get("map.tutorial.tag.route", "教程路线");
                default: return tile.previewTag;
            }
        }
        if (tile.tileType == MapTileType.SpecialDungeonExit) return GameText.Get("map.dungeon.exit", "副本出口");
        if (tile.isSpawnTile) return GameText.Get("map.dungeon.start", "副本起点");
        return tile.sourceSpecialDungeon.LocalizeTile(tile.specialDungeonEntryIndex, "tag", tile.previewTag);
    }

    private static string GetTileTypeLabel(MapTileType tileType)
    {
        switch (tileType)
        {
            case MapTileType.Empty:
                return GameText.Get("map.tile.empty", "空白");
            case MapTileType.Battle:
                return GameText.Get("map.tile.battle", "战斗");
            case MapTileType.Elite:
                return GameText.Get("ui.map_hud.elite", "精英");
            case MapTileType.Event:
                return GameText.Get("ui.event.title", "事件");
            case MapTileType.Merchant:
                return GameText.Get("ui.map_hud.merchant", "商人");
            case MapTileType.Treasure:
                return GameText.Get("ui.map_hud.treasure", "宝藏");
            case MapTileType.Forge:
                return GameText.Get("map.forge.craft", "锻造");
            case MapTileType.Rest:
                return GameText.Get("rest.heal.title", "休息");
            case MapTileType.Key:
                return GameText.Get("ui.map_hud.keys", "钥匙");
            case MapTileType.Boss:
                return "Boss";
            case MapTileType.SpecialDungeonExit:
                return GameText.Get("ui.map_hud.dungeon_exit", "副本出口");
            default:
                return GameText.Get("ui.map_battle_tile_preview.unknown", "未知");
        }
    }

    private static string GetDangerBandLabel(MapDangerBand band)
    {
        switch (band)
        {
            case MapDangerBand.Outer:
                return GameText.Get("map.region.outer", "外围");
            case MapDangerBand.Mid:
                return GameText.Get("map.region.mid", "中层");
            case MapDangerBand.Inner:
                return GameText.Get("map.region.inner", "内层");
            case MapDangerBand.Core:
                return GameText.Get("map.region.core", "核心");
            default:
                return GameText.Get("ui.map_battle_tile_preview.unknown", "未知");
        }
    }

    private static string GetTileDescription(MapTileType tileType)
    {
        switch (tileType)
        {
            case MapTileType.Empty:
                return GameText.Get("map.tile.empty_detail", "空白格，无特殊效果");
            case MapTileType.Event:
                return GameText.Get("map.tile.event_detail", "事件格，可触发随机事件");
            case MapTileType.Merchant:
                return GameText.Get("ui.map_hud.buy_items_here_with_gold", "商人格，可消耗金钱购买商品");
            case MapTileType.Treasure:
                return GameText.Get("ui.map_hud.enter_to_choose_one_treasure_reward", "宝藏格，进入后可选择一份奖励");
            case MapTileType.Forge:
                return GameText.Get("ui.map_hud.use_blueprints_and_materials_here_to_forge_weapons", "锻造格，可消耗材料与图纸锻造武器");
            case MapTileType.Rest:
                return GameText.Get("ui.map_hud.rest_here_to_recover_and_regroup", "休息格，可用于恢复与调整");
            case MapTileType.Key:
                return GameText.Get("ui.map_hud.enter_to_collect_a_key", "钥匙格，进入后可获得钥匙");
            case MapTileType.Boss:
                return GameText.Get("ui.map_hud.collect_the_required_keys_to_challenge_the_boss", "Boss 格，满足钥匙条件后可挑战首领");
            case MapTileType.SpecialDungeonExit:
                return GameText.Get("ui.map_hud.leave_the_dungeon_and_return_to_the_event", "离开副本并返回主地图事件格。");
            default:
                return string.Empty;
        }
    }

    private static string WrapColor(string text, Color color)
    {
        return $"<color=#{ColorUtility.ToHtmlStringRGB(color)}>{text}</color>";
    }

    private void RefreshSelectedInventoryDetail()
    {
        if (runtimeInventoryDetailView == null || selectedInventoryState == null)
            return;

        if (selectedInventoryState is RunWeaponState weaponState)
        {
            runtimeInventoryDetailView.Show(weaponState, GetFallbackSprite());
            ConfigureWeaponDetailAction(weaponState);
            return;
        }

        if (selectedInventoryState is RunLootState lootState)
        {
            runtimeInventoryDetailView.Show(lootState, GetFallbackSprite());
            runtimeInventoryDetailView.ConfigureActionButton(string.Empty, false, null, string.Empty);
            return;
        }

        if (selectedInventoryState is RunBlueprintState blueprintState)
        {
            runtimeInventoryDetailView.Show(blueprintState, GetFallbackSprite());
            runtimeInventoryDetailView.ConfigureActionButton(string.Empty, false, null, string.Empty);
            return;
        }

        if (selectedInventoryState is RunRelicState relicState)
        {
            runtimeInventoryDetailView.Show(relicState, GetFallbackSprite());
            runtimeInventoryDetailView.ConfigureActionButton(string.Empty, false, null, string.Empty);
        }
    }

    private void ConfigureWeaponDetailAction(RunWeaponState weaponState)
    {
        if (runtimeInventoryDetailView == null || weaponState == null || weaponState.weaponData == null || currentRunState == null)
            return;

        Weapon_SO weaponData = weaponState.weaponData;
        RunPartyMemberState equippedMember = currentRunState.GetEquippedMember(weaponData);
        if (equippedMember != null)
        {
            runtimeInventoryDetailView.ConfigureActionButton(GameText.Get("ui.map_hud.equipped", "当前装备"), false, null, GameText.Format("ui.map_hud.equipped_by", "当前装备者：{displayName}", ("displayName", equippedMember.DisplayName)));
            SetActionButtonVisual(false);
            return;
        }

        RunPartyMemberState targetMember = currentRunState.GetPreferredCompatibleMember(weaponData);
        if (targetMember == null)
        {
            runtimeInventoryDetailView.ConfigureActionButton(GameText.Get("ui.map_hud.cannot_equip", "无法装备"), false, null, GameText.Get("ui.map_hud.no_party_member_can_use_this_weapon", "当前队伍中没有适配该武器的角色。"));
            SetActionButtonVisual(false);
            return;
        }

        runtimeInventoryDetailView.ConfigureActionButton(GameText.Get("ui.map_hud.equip", "装备"), true, OnEquipSelectedWeapon, GameText.Format("ui.map_hud.equip_on", "将装备给：{displayName}", ("displayName", targetMember.DisplayName)));
        SetActionButtonVisual(true);
    }

    private void OnEquipSelectedWeapon()
    {
        if (!(selectedInventoryState is RunWeaponState weaponState) || currentRunState == null || weaponState.weaponData == null)
            return;

        if (currentRunState.TryEquipWeapon(weaponState.weaponData, out RunPartyMemberState equippedMember, out string errorMessage))
        {
            string successMessage = equippedMember != null
                ? GameText.Format("ui.map_hud.on_equip_selected_weapon.equipped", "{displayName} 已装备 {localizedName}。", ("displayName", equippedMember.DisplayName), ("localizedName", weaponState.weaponData.LocalizedName))
                : GameText.Format("ui.map_hud.on_equip_selected_weapon.equipped_2", "{localizedName} 已装备。", ("localizedName", weaponState.weaponData.LocalizedName));
            ShowStatus(successMessage);
        }
        else if (!string.IsNullOrWhiteSpace(errorMessage))
        {
            ShowStatus(errorMessage);
        }

        RefreshSelectedInventoryDetail();
    }

    private void SetActionButtonVisual(bool interactable)
    {
        if (runtimeInventoryDetailView == null)
            return;

        Transform actionTransform = runtimeInventoryDetailView.transform.Find("ActionButton");
        if (!(actionTransform is RectTransform))
            return;

        Button actionButton = actionTransform.GetComponent<Button>();
        if (actionButton == null)
            return;

        Image image = actionButton.GetComponent<Image>();
        if (image != null)
            image.color = interactable ? inventoryDetailActionButtonColor : inventoryDetailActionDisabledColor;
    }
    private string BuildAttributeLine(EnemyGroup_SO enemyGroup)
    {
        string attributeValues = BuildAttributeValueText(enemyGroup);
        if (string.IsNullOrEmpty(attributeValues))
            return string.Empty;

        return GameText.Format("ui.map_hud.enemy_attributes", "敌人属性： {attributeValues}", ("attributeValues", attributeValues));
    }

    private string WrapFont(string text, TMP_FontAsset font)
    {
        if (string.IsNullOrEmpty(text) || font == null)
            return text;

        return $"<font=\"{font.name}\">{text}</font>";
    }
    private string BuildAttributeValueText(EnemyGroup_SO enemyGroup)
    {
        if (enemyGroup == null)
            return string.Empty;

        IReadOnlyList<LootAttribute_SO> attributes = enemyGroup.GetAttributes();
        if (attributes == null || attributes.Count == 0)
            return string.Empty;

        List<string> parts = new List<string>();
        for (int i = 0; i < attributes.Count; i++)
        {
            LootAttribute_SO attribute = attributes[i];
            if (attribute == null || string.IsNullOrWhiteSpace(attribute.attributeName))
                continue;

            Color color = attribute.displayColor.a <= 0f ? tileAttributeHighlightColor : attribute.displayColor;
            parts.Add(WrapColor(WrapFont(ResourceKeywordText.AttributeLabel(attribute.LocalizedName), attributeFont), color));
        }

        return string.Join("  ", parts);
    }
}



