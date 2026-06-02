using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class MapHUD : MonoBehaviour
{
    private const int DefaultCanvasSortingOrder = 8000;

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

    [Header("Party Panel")]
    [SerializeField] private Vector2 partyPanelSize = new Vector2(420f, 420f);
    [SerializeField] private Vector2 partyPanelCollapsedSize = new Vector2(420f, 64f);
    [SerializeField] private Vector2 partyPanelOffset = new Vector2(22f, -256f);
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
    private TextMeshProUGUI actionPointText;
    private TextMeshProUGUI moneyText;
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

        if (actionPointText != null)
        {
            int current = actionPointSystem != null ? actionPointSystem.CurrentPoints : 0;
            int total = actionPointSystem != null ? actionPointSystem.CurrentMaxPoints : 0;
            actionPointText.text = $"行动点 {current}/{total}";
        }

        if (moneyText != null)
            moneyText.text = $"金钱 {(runState != null ? runState.money : 0)}";

        if (keysText != null)
        {
            int currentKeys = runState != null ? runState.currentKeys : 0;
            int required = runState != null ? runState.requiredKeys : 0;
            keysText.text = $"钥匙 {currentKeys}/{required}";
        }

        if (calamityText != null)
            calamityText.text = $"灾厄 {Mathf.Max(0, runState != null ? runState.calamityTier : 0)}";

        if (difficultyText != null)
            difficultyText.text = $"难度 {GetDisplayDifficultyName(runState != null ? runState.difficultyName : string.Empty)}";

        if (currentTile != null)
            ShowTile(currentTile, currentMoveCost, currentDetailed);

        RefreshPartyPanel();
        RefreshInventoryView();
    }

    public void ShowTile(BoardTileData tile, int moveCost = -1, bool detailed = false)
    {
        Initialize();
        currentTile = tile;
        currentMoveCost = moveCost;
        currentDetailed = detailed;

        if (tile == null)
        {
            if (tileTitleText != null)
                tileTitleText.text = "未选中格子";
            if (tileBodyText != null)
                tileBodyText.text = "点击任意格子查看详情。";
            if (tileAttributeLabelText != null)
                tileAttributeLabelText.gameObject.SetActive(false);
            if (tileAttributeValueText != null)
                tileAttributeValueText.gameObject.SetActive(false);
            return;
        }

        if (tileTitleText != null)
            tileTitleText.text = $"[{tile.coord.x},{tile.coord.y}] {GetTileTypeLabel(tile.tileType)}";

        List<string> lines = new List<string>
        {
            $"{(tile.isVisited ? "已探索" : "未探索")} | {(tile.isResolved ? "已解决" : "未解决")}",
            $"移动消耗 {Mathf.Max(0, moveCost)} 行动点"
        };

        if (tile.IsHostileTile)
        {
            string attributeLine = BuildAttributeLine(tile.assignedEnemyGroup);
            if (!string.IsNullOrEmpty(attributeLine))
                lines.Add(attributeLine);

            bool isSpecialDungeonTile = tile.sourceSpecialDungeon != null;
            string dangerLine = $"危险度 {MapDangerRatingService.GetRatingRichText(tile.previewDangerRating)}";
            if (!isSpecialDungeonTile)
                dangerLine = $"{dangerLine} | 区域 {WrapColor(GetDangerBandLabel(tile.dangerBand), tileRegionHighlightColor)}";
            lines.Add(dangerLine);

            if (!string.IsNullOrWhiteSpace(tile.previewTag))
                lines.Add($"预览标签 {tile.previewTag}");
            if (detailed)
                lines.Add($"遭遇种子 {tile.encounterSeed}");
        }
        else
        {
            lines.Add(GetTileDescription(tile.tileType));
        }

        if (tileBodyText != null)
            tileBodyText.text = string.Join("\n", lines);

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

        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        rootRect = canvasGo.GetComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

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
        RectTransform panel = CreateRect("TopLeftPanel", rootRect, new Vector2(0f, 1f), new Vector2(22f, -22f), new Vector2(420f, 220f));
        Image topPanelImage = panel.gameObject.AddComponent<Image>();
        topPanelImage.color = panelColor;

        actionPointText = CreateText(panel, "ActionPoints", new Vector2(18f, -18f), new Vector2(380f, 34f), titleFontSize + 2, titleColor, string.Empty, keywordFont);
        moneyText = CreateText(panel, "Money", new Vector2(18f, -58f), new Vector2(240f, 30f), bodyFontSize, accentColor, string.Empty, keywordFont);
        keysText = CreateText(panel, "Keys", new Vector2(18f, -94f), new Vector2(240f, 30f), bodyFontSize, bodyColor, string.Empty, bodyFont);
        calamityText = CreateText(panel, "Calamity", new Vector2(18f, -130f), new Vector2(240f, 30f), bodyFontSize, bodyColor, string.Empty, bodyFont);
        difficultyText = CreateText(panel, "Difficulty", new Vector2(248f, -18f), new Vector2(154f, 30f), bodyFontSize, accentColor, string.Empty, keywordFont);
    }

    private void BuildPartyPanel()
    {
        partyPanelRect = CreateRect("PartyPanel", rootRect, new Vector2(0f, 1f), partyPanelOffset, partyPanelSize);
        partyPanelImage = partyPanelRect.gameObject.AddComponent<Image>();
        partyPanelImage.type = Image.Type.Sliced;

        partyPanelTitleText = CreateText(partyPanelRect, "PartyTitle", partyPanelTitleOffset, partyPanelTitleSize, partyTitleFontSize, titleColor, "当前队伍", GetPartyTitleFont());
        partyToggleButton = BuildPartyToggleButton(partyPanelRect);
        partyToggleButtonText = partyToggleButton != null ? partyToggleButton.GetComponentInChildren<TextMeshProUGUI>(true) : null;
        partyListViewport = CreateRect("PartyListViewport", partyPanelRect, new Vector2(0f, 1f), partyListOffset, new Vector2(partyPanelSize.x - 32f, partyPanelSize.y - 76f));
        partyListMask = partyListViewport.gameObject.AddComponent<RectMask2D>();
        partyEmptyText = CreateText(partyListViewport, "PartyEmpty", Vector2.zero, new Vector2(partyPanelSize.x - 32f, 28f), partyStatFontSize, bodyColor, "当前没有队伍成员。", GetPartyStatFont());
        partyListRoot = CreateRect("PartyList", partyListViewport, new Vector2(0f, 1f), Vector2.zero, new Vector2(partyPanelSize.x - 32f, partyPanelSize.y - 76f));

        if (!partyCollapseStateInitialized)
        {
            isPartyPanelCollapsed = partyStartCollapsed;
            partyPanelReveal = isPartyPanelCollapsed ? 0f : 1f;
            partyCollapseStateInitialized = true;
        }
    }

    private void BuildTilePanel()
    {
        tilePanelRect = CreateRect("TilePanel", rootRect, new Vector2(1f, 0f), Vector2.zero, tilePanelSize);
        tilePanelImage = tilePanelRect.gameObject.AddComponent<Image>();
        tilePanelImage.type = Image.Type.Sliced;

        tileTitleText = CreateText(tilePanelRect, "TileTitle", Vector2.zero, tileTitleSize, titleFontSize, titleColor, string.Empty, keywordFont);
        tileBodyText = CreateText(tilePanelRect, "TileBody", Vector2.zero, tileBodySize, bodyFontSize, bodyColor, string.Empty, bodyFont);
        tileBodyText.enableWordWrapping = true;
        tileBodyText.overflowMode = TextOverflowModes.Overflow;
        tileAttributeLabelText = CreateText(tilePanelRect, "TileAttributeLabel", Vector2.zero, tileAttributeLabelSize, bodyFontSize, bodyColor, string.Empty, bodyFont);
        tileAttributeValueText = CreateText(tilePanelRect, "TileAttributeValue", Vector2.zero, tileAttributeValueSize, bodyFontSize, tileAttributeHighlightColor, string.Empty, attributeFont != null ? attributeFont : bodyFont);
        tileAttributeValueText.enableWordWrapping = true;
        tileAttributeValueText.overflowMode = TextOverflowModes.Overflow;
        tileAttributeLabelText.gameObject.SetActive(false);
        tileAttributeValueText.gameObject.SetActive(false);
    }

    private void BuildStatusText()
    {
        statusText = CreateText(rootRect, "StatusText", new Vector2(22f, 22f), new Vector2(860f, 84f), bodyFontSize, accentColor, string.Empty, bodyFont);
        RectTransform rect = statusText.rectTransform;
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(0f, 0f);
        rect.pivot = new Vector2(0f, 0f);
    }

    private void BuildInventoryButton()
    {
        RectTransform rect = CreateRect("InventoryButton", rootRect, new Vector2(1f, 1f), inventoryButtonOffset, inventoryButtonFallbackSize);
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = inventoryButtonSprite != null ? inventoryButtonSpriteColor : inventorySlotColor;
        if (inventoryButtonSprite != null)
        {
            image.sprite = inventoryButtonSprite;
            image.type = Image.Type.Sliced;
        }

        inventoryButton = rect.gameObject.AddComponent<Button>();
        inventoryButton.transition = Selectable.Transition.ColorTint;
        inventoryButton.onClick.AddListener(ToggleInventoryPanel);
    }

    private void BuildInventoryPanel()
    {
        inventoryPanelRect = CreateRect("InventoryPanel", rootRect, new Vector2(1f, 1f), inventoryPanelOffset, inventoryPanelSize);
        inventoryPanelImage = inventoryPanelRect.gameObject.AddComponent<Image>();
        inventoryPanelImage.type = Image.Type.Sliced;

        inventoryTabRoot = CreateRect("TabRoot", inventoryPanelRect, new Vector2(0f, 1f), inventoryTabRootOffset, new Vector2(inventoryPanelSize.x - 36f, inventoryTabSize.y));
        lootTabButton = BuildTabButton(inventoryTabRoot, "LootTab", Vector2.zero, "战利品", () => SetInventoryTab(InventoryTab.Loot));
        weaponTabButton = BuildTabButton(inventoryTabRoot, "WeaponTab", Vector2.zero, "武器", () => SetInventoryTab(InventoryTab.Weapons));
        blueprintTabButton = BuildTabButton(inventoryTabRoot, "BlueprintTab", Vector2.zero, "图纸", () => SetInventoryTab(InventoryTab.Blueprints));
        relicTabButton = BuildTabButton(inventoryTabRoot, "RelicTab", Vector2.zero, "遗物", () => SetInventoryTab(InventoryTab.Relics));

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
        RectTransform detailRoot = CreateRect("InventoryDetail", rootRect, new Vector2(0.5f, 0.5f), inventoryDetailOffset, inventoryDetailSize);
        Image bg = detailRoot.gameObject.AddComponent<Image>();
        bg.type = Image.Type.Sliced;
        bg.sprite = inventoryDetailBackgroundSprite;
        bg.color = inventoryDetailBackgroundSprite != null ? Color.white : panelColor;
        detailRoot.gameObject.SetActive(false);

        Image icon = CreateImage(detailRoot, "Icon", inventoryDetailIconOffset, inventoryDetailIconSize, null, Color.white);
        TextMeshProUGUI title = CreateText(detailRoot, "Title", new Vector2(162f, -20f), new Vector2(inventoryDetailSize.x - 240f, 42f), titleFontSize, titleColor, string.Empty, keywordFont);
        TextMeshProUGUI meta = CreateText(detailRoot, "Meta", new Vector2(162f, -68f), new Vector2(inventoryDetailSize.x - 240f, 34f), bodyFontSize, accentColor, string.Empty, keywordFont);
        TextMeshProUGUI description = CreateText(detailRoot, "Description", new Vector2(162f, -112f), new Vector2(inventoryDetailSize.x - 200f, inventoryDetailSize.y - 132f), bodyFontSize, bodyColor, string.Empty, bodyFont);
        description.enableWordWrapping = true;
        description.overflowMode = TextOverflowModes.Overflow;
        TextMeshProUGUI relicAdditionalDescription = CreateText(detailRoot, "RelicAdditionalDescription", inventoryDetailRelicAdditionalDescriptionOffset, inventoryDetailRelicAdditionalDescriptionSize, bodyFontSize - 2, inventoryDetailRelicAdditionalDescriptionColor, string.Empty, bodyFont);
        relicAdditionalDescription.enableWordWrapping = true;
        relicAdditionalDescription.overflowMode = TextOverflowModes.Overflow;
        relicAdditionalDescription.gameObject.SetActive(false);
        TextMeshProUGUI actionStatus = CreateText(detailRoot, "ActionStatus", inventoryDetailActionStatusOffset, inventoryDetailActionStatusSize, bodyFontSize - 2, accentColor, string.Empty, bodyFont);
        Button action = BuildInventoryActionButton(detailRoot, "ActionButton", inventoryDetailActionButtonOffset, inventoryDetailActionButtonSize, string.Empty, null);

        Button close = BuildSimpleButton(detailRoot, "CloseButton", new Vector2(inventoryDetailSize.x - 66f, -18f), new Vector2(48f, 48f), "X", HideInventoryDetail);
        runtimeInventoryDetailView = detailRoot.gameObject.AddComponent<MapInventoryDetailView>();
        runtimeInventoryDetailView.ConfigureRuntime(icon, title, meta, description, relicAdditionalDescription, actionStatus, action, close);
        runtimeInventoryDetailView.ConfigureFonts(keywordFont, bodyFont);
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
        if (memberCount <= 0)
            return partyPanelSize;

        float requiredHeight = 76f + memberCount * partyMemberCardSize.y + Mathf.Max(0, memberCount - 1) * partyMemberCardSpacing + 16f;
        return new Vector2(partyPanelSize.x, Mathf.Max(partyPanelSize.y, requiredHeight));
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

        string memberName = member != null ? member.DisplayName : "未知成员";
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
        if (view.hpText != null)
            view.hpText.text = $"{currentHP}/{maxHP}";
        if (view.expText != null)
            view.expText.text = expToNext > 0 ? $"{currentExp}/{expToNext}" : "满级";

        SetBarFill(view.hpFillImage, hpRatio);
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

        ApplyFont(actionPointText, keywordFont);
        ApplyFont(moneyText, keywordFont);
        ApplyFont(keysText, bodyFont);
        ApplyFont(calamityText, bodyFont);
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
        if (tilePanelRect == null)
            return;

        tilePanelRect.anchorMin = new Vector2(1f, 0f);
        tilePanelRect.anchorMax = new Vector2(1f, 0f);
        tilePanelRect.pivot = new Vector2(1f, 0f);
        tilePanelRect.anchoredPosition = new Vector2(tilePanelOffset.x, tilePanelOffset.y);
        tilePanelRect.sizeDelta = tilePanelSize;

        if (tilePanelImage != null)
        {
            tilePanelImage.sprite = tilePanelBackgroundSprite;
            tilePanelImage.color = tilePanelBackgroundSprite != null ? Color.white : tilePanelBackgroundColor;
            tilePanelImage.type = Image.Type.Sliced;
        }

        ApplyRect(tileTitleText.rectTransform, new Vector2(0f, 1f), tileTitleOffset, tileTitleSize);
        ApplyRect(tileBodyText.rectTransform, new Vector2(0f, 1f), tileBodyOffset, tileBodySize);
        ApplyRect(tileAttributeLabelText.rectTransform, new Vector2(0f, 1f), tileAttributeLabelOffset, tileAttributeLabelSize);
        ApplyRect(tileAttributeValueText.rectTransform, new Vector2(0f, 1f), tileAttributeValueOffset, tileAttributeValueSize);
    }

    private void ApplyPartyPanelLayout()
    {
        if (partyPanelRect == null)
            return;

        Vector2 expandedPanelSize = ResolvePartyPanelSize();
        Vector2 resolvedPanelSize = Vector2.Lerp(partyPanelCollapsedSize, expandedPanelSize, Mathf.Clamp01(partyPanelReveal));
        float titleAvailableWidth = Mathf.Max(140f, partyToggleButtonOffset.x - partyPanelTitleOffset.x - 12f);
        Vector2 resolvedTitleSize = new Vector2(Mathf.Min(partyPanelTitleSize.x, titleAvailableWidth), partyPanelTitleSize.y);
        float fullListHeight = Mathf.Max(0f, expandedPanelSize.y - 76f);
        float viewportHeight = Mathf.Max(0f, fullListHeight * Mathf.Clamp01(partyPanelReveal));
        int memberCount = currentRunState != null && currentRunState.partyMembers != null ? currentRunState.partyMembers.Count : 0;
        bool showPartyDetails = partyPanelReveal > 0.001f;
        ApplyRect(partyPanelRect, new Vector2(0f, 1f), partyPanelOffset, resolvedPanelSize);
        if (partyPanelImage != null)
        {
            partyPanelImage.sprite = partyPanelBackgroundSprite;
            partyPanelImage.color = partyPanelBackgroundSprite != null ? Color.white : partyPanelColor;
            partyPanelImage.type = Image.Type.Sliced;
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
            ApplyRect(partyListViewport, new Vector2(0f, 1f), partyListOffset, new Vector2(expandedPanelSize.x - 32f, viewportHeight));
            partyListViewport.gameObject.SetActive(showPartyDetails);
        }
        if (partyEmptyText != null)
        {
            ApplyRect(partyEmptyText.rectTransform, new Vector2(0f, 1f), Vector2.zero, new Vector2(expandedPanelSize.x - 32f, 28f));
            partyEmptyText.gameObject.SetActive(showPartyDetails && memberCount <= 0);
        }
        if (partyListRoot != null)
        {
            ApplyRect(partyListRoot, new Vector2(0f, 1f), Vector2.zero, new Vector2(expandedPanelSize.x - 32f, fullListHeight));
            partyListRoot.gameObject.SetActive(showPartyDetails);
        }

        if (partyToggleButtonText != null)
        {
            partyToggleButtonText.text = isPartyPanelCollapsed ? partyCollapsedButtonLabel : partyExpandedButtonLabel;
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
        {
            RectTransform detailRect = runtimeInventoryDetailView.GetComponent<RectTransform>();
            detailRect.anchorMin = new Vector2(0.5f, 0.5f);
            detailRect.anchorMax = new Vector2(0.5f, 0.5f);
            detailRect.pivot = new Vector2(0.5f, 0.5f);
            detailRect.anchoredPosition = inventoryDetailOffset;
            detailRect.sizeDelta = inventoryDetailSize;

            Image detailBg = runtimeInventoryDetailView.GetComponent<Image>();
            if (detailBg != null)
            {
                detailBg.sprite = inventoryDetailBackgroundSprite;
                detailBg.color = inventoryDetailBackgroundSprite != null ? Color.white : panelColor;
                detailBg.type = Image.Type.Sliced;
            }

            Transform detailIcon = runtimeInventoryDetailView.transform.Find("Icon");
            if (detailIcon is RectTransform detailIconRect)
                ApplyRect(detailIconRect, new Vector2(0f, 1f), inventoryDetailIconOffset, inventoryDetailIconSize);

            Transform actionStatus = runtimeInventoryDetailView.transform.Find("ActionStatus");
            if (actionStatus is RectTransform actionStatusRect)
                ApplyRect(actionStatusRect, new Vector2(0f, 1f), inventoryDetailActionStatusOffset, inventoryDetailActionStatusSize);

            Transform relicAdditionalDescription = runtimeInventoryDetailView.transform.Find("RelicAdditionalDescription");
            if (relicAdditionalDescription is RectTransform relicAdditionalDescriptionRect)
            {
                ApplyRect(relicAdditionalDescriptionRect, new Vector2(0f, 1f), inventoryDetailRelicAdditionalDescriptionOffset, inventoryDetailRelicAdditionalDescriptionSize);
                TMP_Text relicAdditionalDescriptionText = relicAdditionalDescriptionRect.GetComponent<TMP_Text>();
                if (relicAdditionalDescriptionText != null)
                    relicAdditionalDescriptionText.color = inventoryDetailRelicAdditionalDescriptionColor;
            }

            Transform actionButton = runtimeInventoryDetailView.transform.Find("ActionButton");
            if (actionButton is RectTransform actionButtonRect)
                ApplyRect(actionButtonRect, new Vector2(0f, 1f), inventoryDetailActionButtonOffset, inventoryDetailActionButtonSize);
        }
    }

    private void ToggleInventoryPanel()
    {
        if (inventoryPanelRect == null)
            return;

        bool nextState = !inventoryPanelRect.gameObject.activeSelf;
        inventoryPanelRect.gameObject.SetActive(nextState);
        if (!nextState)
        {
            HideInventoryDetail();
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

        HideInventoryDetail();
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
        if (string.IsNullOrWhiteSpace(difficultyName))
            return "普通";

        return difficultyName;
    }

    private static string GetTileTypeLabel(MapTileType tileType)
    {
        switch (tileType)
        {
            case MapTileType.Empty:
                return "空白";
            case MapTileType.Battle:
                return "战斗";
            case MapTileType.Elite:
                return "精英";
            case MapTileType.Event:
                return "事件";
            case MapTileType.Merchant:
                return "商人";
            case MapTileType.Treasure:
                return "宝藏";
            case MapTileType.Forge:
                return "锻造";
            case MapTileType.Rest:
                return "休息";
            case MapTileType.Key:
                return "钥匙";
            case MapTileType.Boss:
                return "Boss";
            case MapTileType.SpecialDungeonExit:
                return "副本出口";
            default:
                return "未知";
        }
    }

    private static string GetDangerBandLabel(MapDangerBand band)
    {
        switch (band)
        {
            case MapDangerBand.Outer:
                return "外围";
            case MapDangerBand.Mid:
                return "中层";
            case MapDangerBand.Inner:
                return "内层";
            case MapDangerBand.Core:
                return "核心";
            default:
                return "未知";
        }
    }

    private static string GetTileDescription(MapTileType tileType)
    {
        switch (tileType)
        {
            case MapTileType.Empty:
                return "空白格，无特殊效果";
            case MapTileType.Event:
                return "事件格，可触发随机事件";
            case MapTileType.Merchant:
                return "商人格，可消耗金钱购买商品";
            case MapTileType.Treasure:
                return "宝藏格，进入后可选择一份奖励";
            case MapTileType.Forge:
                return "锻造格，可消耗材料与图纸锻造武器";
            case MapTileType.Rest:
                return "休息格，可用于恢复与调整";
            case MapTileType.Key:
                return "钥匙格，进入后可获得钥匙";
            case MapTileType.Boss:
                return "Boss 格，满足钥匙条件后可挑战首领";
            case MapTileType.SpecialDungeonExit:
                return "离开副本并返回主地图事件格。";
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
            runtimeInventoryDetailView.ConfigureActionButton("当前装备", false, null, $"当前装备者：{equippedMember.DisplayName}");
            SetActionButtonVisual(false);
            return;
        }

        RunPartyMemberState targetMember = currentRunState.GetPreferredCompatibleMember(weaponData);
        if (targetMember == null)
        {
            runtimeInventoryDetailView.ConfigureActionButton("无法装备", false, null, "当前队伍中没有适配该武器的角色。");
            SetActionButtonVisual(false);
            return;
        }

        runtimeInventoryDetailView.ConfigureActionButton("装备", true, OnEquipSelectedWeapon, $"将装备给：{targetMember.DisplayName}");
        SetActionButtonVisual(true);
    }

    private void OnEquipSelectedWeapon()
    {
        if (!(selectedInventoryState is RunWeaponState weaponState) || currentRunState == null || weaponState.weaponData == null)
            return;

        if (currentRunState.TryEquipWeapon(weaponState.weaponData, out RunPartyMemberState equippedMember, out string errorMessage))
        {
            string successMessage = equippedMember != null
                ? $"{equippedMember.DisplayName} 已装备 {weaponState.weaponData.weaponName}。"
                : $"{weaponState.weaponData.weaponName} 已装备。";
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

        return $"敌人属性： {attributeValues}";
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
            parts.Add(WrapColor(WrapFont(attribute.attributeName, attributeFont), color));
        }

        return string.Join("  ", parts);
    }
}



