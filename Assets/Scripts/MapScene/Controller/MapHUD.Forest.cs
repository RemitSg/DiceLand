using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public partial class MapHUD
{
    private static readonly Color ForestInk = new Color(0.045f, 0.075f, 0.068f, 0.97f);
    private static readonly Color ForestIvory = new Color(0.96f, 0.93f, 0.81f);
    private static readonly Color ForestGold = new Color(0.75f, 0.60f, 0.32f);
    private static readonly Color ForestJade = new Color(0.62f, 0.86f, 0.72f);
    private RectTransform forestSafeArea, forestPartyHeader, forestTileDescriptionViewport;
    private RectTransform forestCostStrip;
    private TextMeshProUGUI forestCoordinates, forestExplored, forestResolved, forestMoveCost;
    private Image forestTileIcon, forestExploredPanel, forestResolvedPanel;
    private ScrollRect forestDescriptionScroll;
    private Vector2 forestLastViewport;
    private Rect forestLastSafeArea;
    private readonly List<TextMeshProUGUI> forestLabels = new List<TextMeshProUGUI>();

    private void ApplyForestFonts()
    {
        foreach (TextMeshProUGUI text in forestLabels)
            ApplyFont(text, text == tileBodyText && bodyFont != null ? bodyFont : keywordFont);
    }

    private void ConfigureForestStyle()
    {
        titleColor = ForestIvory;
        bodyColor = new Color(0.88f, 0.90f, 0.84f);
        accentColor = ForestGold;
        panelColor = ForestInk;
        partyPanelSize = new Vector2(552f, 202f);
        partyPanelCollapsedSize = new Vector2(552f, 66f);
        partyPanelTitleOffset = new Vector2(22f, -17f);
        partyPanelTitleSize = new Vector2(350f, 38f);
        partyTitleFontSize = 28;
        partyToggleButtonOffset = new Vector2(438f, -13f);
        partyToggleButtonSize = new Vector2(94f, 40f);
        partyToggleButtonSprite = MapForestArt.Get("ButtonFace");
        partyToggleButtonColor = new Color(0.78f, 0.67f, 0.43f);
        partyToggleButtonTextColor = new Color(0.20f, 0.13f, 0.06f);
        partyListOffset = new Vector2(0f, -76f);
        partyMemberCardSize = new Vector2(552f, 126f);
        partyMemberCardSpacing = 10f;
        partyMemberCardSprite = MapForestArt.Get("PartyCard");
        partyMemberIconOffset = new Vector2(14f, -13f);
        partyMemberIconSize = new Vector2(96f, 96f);
        partyMemberNameOffset = new Vector2(126f, -13f);
        partyMemberNameSize = new Vector2(398f, 38f);
        partyNameFontSize = 28;
        partyStatFontSize = 22;
        partyMemberHpBarOffset = new Vector2(126f, -60f);
        partyMemberHpBarSize = new Vector2(296f, 18f);
        partyMemberHpTextOffset = new Vector2(426f, -53f);
        partyMemberHpTextSize = new Vector2(108f, 34f);
        partyMemberExpBarOffset = new Vector2(126f, -94f);
        partyMemberExpBarSize = new Vector2(296f, 10f);
        partyMemberExpTextOffset = new Vector2(426f, -85f);
        partyMemberExpTextSize = new Vector2(108f, 32f);
        partyMemberNameColor = ForestIvory;
        partyMemberHpTextColor = new Color(1f, 0.64f, 0.57f);
        partyMemberExpTextColor = new Color(0.67f, 0.79f, 0.82f);
        partyMemberHpFillColor = new Color(0.79f, 0.21f, 0.24f);
        partyMemberExpFillColor = new Color(0.32f, 0.58f, 0.67f);
        tilePanelSize = new Vector2(500f, 360f);
        tilePanelOffset = new Vector2(-26f, 26f);
        inventoryButtonOffset = new Vector2(-26f, -26f);
        inventoryButtonFallbackSize = new Vector2(76f, 76f);
        inventoryButtonSprite = MapForestArt.Get("ButtonFace");
        inventoryButtonSpriteColor = Color.white;
        inventoryButtonFallbackGlyphColor = new Color(0.20f, 0.14f, 0.07f);
    }

    /// <summary>Apply the same safe-area layout in the player, editor previews, and layout validation.</summary>
    public void ApplyViewport(Vector2 viewport, Rect safePixels)
    {
        if (rootRect == null || forestSafeArea == null) return;
        float width = Mathf.Max(1f, viewport.x), height = Mathf.Max(1f, viewport.y);
        Rect safe = new Rect(Mathf.Clamp(safePixels.x, 0f, width), Mathf.Clamp(safePixels.y, 0f, height),
            Mathf.Clamp(safePixels.width, 1f, width), Mathf.Clamp(safePixels.height, 1f, height));
        safe.width = Mathf.Min(safe.width, width - safe.x);
        safe.height = Mathf.Min(safe.height, height - safe.y);
        forestSafeArea.anchorMin = new Vector2(safe.xMin / width, safe.yMin / height);
        forestSafeArea.anchorMax = new Vector2(safe.xMax / width, safe.yMax / height);
        forestSafeArea.offsetMin = forestSafeArea.offsetMax = Vector2.zero;
        Vector2 referenceSize = safe.size * (1080f / height);
        float scale = Mathf.Min(1f, referenceSize.x / 2340f);
        rootRect.anchorMin = rootRect.anchorMax = new Vector2(0.5f, 0.5f);
        rootRect.pivot = new Vector2(0.5f, 0.5f);
        rootRect.anchoredPosition = Vector2.zero;
        rootRect.sizeDelta = referenceSize / Mathf.Max(0.1f, scale);
        rootRect.localScale = Vector3.one * scale;
        forestLastViewport = viewport;
        forestLastSafeArea = safePixels;
        ApplyPartyPanelLayout();
    }

    private void Update()
    {
        if (canvas == null) return;
        Vector2 viewport = new Vector2(Screen.width, Screen.height);
        if (viewport != forestLastViewport || Screen.safeArea != forestLastSafeArea)
            ApplyViewport(viewport, Screen.safeArea);
    }

    private void BuildForestResources()
    {
        RectTransform panel = ForestPanel("TopLeftPanel", rootRect, TopLeftHudOffset, TopLeftHudSize, ForestInk);
        RectTransform ap = ForestResourceCell("ActionPointCell", panel, new Vector2(14f, -14f), new Vector2(226f, 148f), ForestInk);
        RectTransform calamity = ForestResourceCell("CalamityCell", panel, new Vector2(248f, -14f), new Vector2(170f, 148f), new Color(0.16f, 0.055f, 0.045f, 0.96f));
        RectTransform money = ForestResourceCell("MoneyCell", panel, new Vector2(14f, -170f), new Vector2(190f, 92f), ForestInk);
        RectTransform keys = ForestResourceCell("KeysCell", panel, new Vector2(212f, -170f), new Vector2(206f, 92f), ForestInk);
        RectTransform difficulty = ForestResourceCell("DifficultyCell", panel, new Vector2(426f, -14f), new Vector2(140f, 248f), ForestInk);
        ForestLabel(ap, "ActionPointLabel", new Vector2(16f, -10f), new Vector2(200f, 38f), 30, "行动资源", ForestIvory);
        actionPointText = ForestLabel(ap, "ActionPoints", new Vector2(12f, -50f), new Vector2(202f, 88f), 74, string.Empty, ForestIvory);
        actionPointText.alignment = TextAlignmentOptions.Center;
        ForestLabel(calamity, "CalamityLabel", new Vector2(12f, -10f), new Vector2(146f, 38f), 28, "灾厄", new Color(0.94f, 0.64f, 0.48f)).alignment = TextAlignmentOptions.Center;
        calamityText = ForestLabel(calamity, "Calamity", new Vector2(14f, -53f), new Vector2(142f, 80f), 64, string.Empty, new Color(0.91f, 0.37f, 0.30f));
        calamityText.alignment = TextAlignmentOptions.Center;
        ForestIcon(money, "MoneyIcon", new Vector2(16f, -25f), new Vector2(48f, 46f), MapForestArt.Get("Coins"));
        ForestLabel(money, "MoneyLabel", new Vector2(76f, -8f), new Vector2(100f, 30f), 23, "金币", ForestIvory);
        moneyText = ForestLabel(money, "Money", new Vector2(76f, -39f), new Vector2(104f, 42f), 33, string.Empty, new Color(0.94f, 0.80f, 0.42f));
        ForestIcon(keys, "KeysIcon", new Vector2(15f, -24f), new Vector2(48f, 48f), MapForestArt.Get("Key"));
        ForestLabel(keys, "KeysLabel", new Vector2(76f, -8f), new Vector2(118f, 30f), 23, "钥匙", ForestIvory);
        keysText = ForestLabel(keys, "Keys", new Vector2(76f, -39f), new Vector2(118f, 42f), 33, string.Empty, new Color(0.72f, 0.85f, 0.90f));
        ForestLabel(difficulty, "DifficultyLabel", new Vector2(10f, -12f), new Vector2(120f, 38f), 27, "难度", ForestIvory).alignment = TextAlignmentOptions.Center;
        ForestIcon(difficulty, "DifficultyCompass", new Vector2(36f, -65f), new Vector2(68f, 68f), MapForestArt.Get("Compass"));
        difficultyText = ForestLabel(difficulty, "Difficulty", new Vector2(10f, -158f), new Vector2(120f, 58f), 37, string.Empty, new Color(0.94f, 0.81f, 0.45f));
        difficultyText.alignment = TextAlignmentOptions.Center;
        difficultyText.enableAutoSizing = true;
        difficultyText.fontSizeMin = 23;
        difficultyText.fontSizeMax = 37;
    }

    private void BuildForestPartyHeader()
    {
        forestPartyHeader = ForestPanel("PartyHeader", partyPanelRect, Vector2.zero, new Vector2(552f, 66f), ForestInk);
        forestPartyHeader.SetAsFirstSibling();
        ScrollRect scroll = partyListViewport.gameObject.AddComponent<ScrollRect>();
        scroll.viewport = partyListViewport;
        scroll.content = partyListRoot;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 28f;
    }

    private void BuildForestTilePanel()
    {
        tilePanelRect = ForestPanel("TilePanel", rootRect, tilePanelOffset, tilePanelSize, ForestInk, new Vector2(1f, 0f));
        tilePanelImage = tilePanelRect.GetComponent<Image>();
        Image iconBase = ForestIcon(tilePanelRect, "TileIconBase", new Vector2(22f, -22f), new Vector2(90f, 90f), MapForestArt.Get("TileFace"));
        iconBase.color = new Color(0.65f, 0.66f, 0.56f);
        forestTileIcon = ForestIcon(tilePanelRect, "TileIcon", new Vector2(39f, -39f), new Vector2(56f, 56f), MapForestArt.Get("Footprint"));
        tileTitleText = ForestLabel(tilePanelRect, "TileTitle", new Vector2(130f, -25f), new Vector2(242f, 48f), 39, "未选中格子", ForestIvory);
        RectTransform coordinate = ForestResourceCell("CoordinateBadge", tilePanelRect, new Vector2(390f, -24f), new Vector2(88f, 38f), new Color(0.12f, 0.14f, 0.10f, 0.9f));
        forestCoordinates = ForestLabel(coordinate, "TileCoordinates", new Vector2(4f, -2f), new Vector2(80f, 34f), 25, string.Empty, ForestIvory);
        forestCoordinates.alignment = TextAlignmentOptions.Center;
        forestExploredPanel = ForestResourceCell("ExploredBadge", tilePanelRect, new Vector2(130f, -80f), new Vector2(166f, 34f), new Color(0.14f, 0.20f, 0.14f)).GetComponent<Image>();
        forestResolvedPanel = ForestResourceCell("ResolvedBadge", tilePanelRect, new Vector2(308f, -80f), new Vector2(170f, 34f), new Color(0.14f, 0.20f, 0.14f)).GetComponent<Image>();
        ForestGlyph(forestExploredPanel.rectTransform, "ExploredEye", new Vector2(10f, -7f), new Vector2(24f, 20f), MapHudGlyph.Shape.Eye, ForestGold);
        ForestGlyph(forestResolvedPanel.rectTransform, "ResolvedCheck", new Vector2(10f, -7f), new Vector2(24f, 20f), MapHudGlyph.Shape.Check, ForestJade);
        forestExplored = ForestLabel(forestExploredPanel.rectTransform, "TileExplored", new Vector2(42f, -2f), new Vector2(120f, 30f), 24, string.Empty, ForestIvory);
        forestResolved = ForestLabel(forestResolvedPanel.rectTransform, "TileResolved", new Vector2(42f, -2f), new Vector2(124f, 30f), 24, string.Empty, ForestIvory);
        ForestGlyph(tilePanelRect, "HeaderDivider", new Vector2(24f, -122f), new Vector2(452f, 10f), MapHudGlyph.Shape.Divider, ForestGold);
        forestCostStrip = ForestResourceCell("MoveCostStrip", tilePanelRect, new Vector2(24f, -144f), new Vector2(452f, 70f), new Color(0.17f, 0.22f, 0.14f, 0.94f));
        ForestIcon(forestCostStrip, "MoveCostIcon", new Vector2(16f, -15f), new Vector2(38f, 40f), MapForestArt.Get("Footprint"));
        ForestLabel(forestCostStrip, "MoveCostLabel", new Vector2(76f, -19f), new Vector2(180f, 36f), 27, "移动消耗", ForestIvory);
        forestMoveCost = ForestLabel(forestCostStrip, "TileMoveCost", new Vector2(284f, -9f), new Vector2(68f, 54f), 48, "0", ForestJade);
        forestMoveCost.alignment = TextAlignmentOptions.Center;
        ForestLabel(forestCostStrip, "MoveCostUnit", new Vector2(354f, -21f), new Vector2(90f, 34f), 24, "行动点", ForestIvory);
        ForestGlyph(tilePanelRect, "DescriptionDivider", new Vector2(24f, -226f), new Vector2(452f, 10f), MapHudGlyph.Shape.Divider, ForestGold);
        ForestGlyph(tilePanelRect, "DescriptionBook", new Vector2(26f, -254f), new Vector2(30f, 28f), MapHudGlyph.Shape.Book, ForestGold);
        forestTileDescriptionViewport = CreateRect("TileDescriptionViewport", tilePanelRect, new Vector2(0f, 1f), new Vector2(72f, -249f), new Vector2(400f, 88f));
        forestTileDescriptionViewport.gameObject.AddComponent<Image>().color = Color.clear;
        forestTileDescriptionViewport.gameObject.AddComponent<RectMask2D>();
        tileBodyText = ForestLabel(forestTileDescriptionViewport, "TileBody", Vector2.zero, new Vector2(398f, 88f), 25, "点击任意格子查看详情。", bodyColor);
        tileBodyText.enableWordWrapping = true;
        tileBodyText.enableAutoSizing = false;
        tileBodyText.overflowMode = TextOverflowModes.Overflow;
        tileBodyText.font = bodyFont != null ? bodyFont : keywordFont;
        forestDescriptionScroll = forestTileDescriptionViewport.gameObject.AddComponent<ScrollRect>();
        forestDescriptionScroll.viewport = forestTileDescriptionViewport;
        forestDescriptionScroll.content = tileBodyText.rectTransform;
        forestDescriptionScroll.horizontal = false;
        forestDescriptionScroll.vertical = true;
        forestDescriptionScroll.movementType = ScrollRect.MovementType.Clamped;
        forestDescriptionScroll.scrollSensitivity = 28f;
        tileAttributeLabelText = ForestLabel(tilePanelRect, "TileAttributeLabel", Vector2.zero, Vector2.one, 20, string.Empty, bodyColor);
        tileAttributeValueText = ForestLabel(tilePanelRect, "TileAttributeValue", Vector2.zero, Vector2.one, 20, string.Empty, bodyColor);
        tileAttributeLabelText.gameObject.SetActive(false);
        tileAttributeValueText.gameObject.SetActive(false);
    }

    private void PresentForestTile(BoardTileData tile, int moveCost)
    {
        if (forestCoordinates == null) return;
        bool selected = tile != null;
        forestCoordinates.text = selected ? $"[{tile.coord.x},{tile.coord.y}]" : "—";
        forestExploredPanel.gameObject.SetActive(selected);
        forestResolvedPanel.gameObject.SetActive(selected);
        forestCostStrip.gameObject.SetActive(selected);
        forestTileIcon.sprite = selected ? MapForestArt.Icon(tile.tileType) : MapForestArt.Get("Footprint");
        forestTileIcon.gameObject.SetActive(forestTileIcon.sprite != null);
        if (!selected) return;
        forestExplored.text = tile.isVisited ? "已探索" : "未探索";
        forestResolved.text = tile.isResolved ? "已解决" : "未解决";
        forestExplored.color = tile.isVisited ? ForestIvory : new Color(0.70f, 0.74f, 0.69f);
        forestResolved.color = tile.isResolved ? ForestIvory : new Color(0.70f, 0.74f, 0.69f);
        forestMoveCost.text = Mathf.Max(0, moveCost).ToString();
    }

    private void ApplyForestTilePanelLayout()
    {
        if (tilePanelRect == null) return;
        float height = currentTile != null && currentTile.IsHostileTile ? 444f : tilePanelSize.y;
        ApplyRect(tilePanelRect, new Vector2(1f, 0f), tilePanelOffset, new Vector2(tilePanelSize.x, height));
        float viewportHeight = Mathf.Max(64f, height - 272f);
        forestTileDescriptionViewport.sizeDelta = new Vector2(400f, viewportHeight);
        float contentHeight = Mathf.Max(viewportHeight, tileBodyText.GetPreferredValues(tileBodyText.text, 398f, 0f).y + 8f);
        tileBodyText.rectTransform.sizeDelta = new Vector2(398f, contentHeight);
    }

    private TextMeshProUGUI ForestLabel(RectTransform parent, string name, Vector2 position, Vector2 size, int fontSize, string value, Color tint)
    {
        TextMeshProUGUI text = CreateText(parent, name, position, size, fontSize, tint, value, keywordFont != null ? keywordFont : bodyFont);
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.enableAutoSizing = true;
        text.fontSizeMin = fontSize * 0.7f;
        text.fontSizeMax = fontSize;
        forestLabels.Add(text);
        return text;
    }

    private Image ForestIcon(RectTransform parent, string name, Vector2 position, Vector2 size, Sprite sprite)
    {
        Image image = CreateImage(parent, name, position, size, sprite, Color.white);
        image.preserveAspect = true;
        image.raycastTarget = false;
        image.enabled = sprite != null;
        return image;
    }

    private RectTransform ForestPanel(string name, RectTransform parent, Vector2 position, Vector2 size, Color fill, Vector2? anchor = null)
    {
        RectTransform rect = CreateRect(name, parent, anchor ?? new Vector2(0f, 1f), position, size);
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = fill;
        Image frame = CreateImage(rect, "BrassFrame", Vector2.zero, size, MapForestArt.Get("PanelFrame"), Color.white);
        frame.type = Image.Type.Sliced;
        frame.pixelsPerUnitMultiplier = 1.8f;
        frame.raycastTarget = false;
        frame.enabled = frame.sprite != null;
        frame.rectTransform.anchorMin = Vector2.zero;
        frame.rectTransform.anchorMax = Vector2.one;
        frame.rectTransform.offsetMin = frame.rectTransform.offsetMax = Vector2.zero;
        return rect;
    }

    private RectTransform ForestResourceCell(string name, RectTransform parent, Vector2 position, Vector2 size, Color fill)
    {
        RectTransform rect = CreateRect(name, parent, new Vector2(0f, 1f), position, size);
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = fill;
        image.raycastTarget = false;
        Color edge = new Color(ForestGold.r, ForestGold.g, ForestGold.b, 0.44f);
        CreateImage(rect, "TopEdge", Vector2.zero, new Vector2(size.x, 1f), null, edge).raycastTarget = false;
        CreateImage(rect, "LeftEdge", Vector2.zero, new Vector2(1f, size.y), null, edge).raycastTarget = false;
        CreateImage(rect, "BottomEdge", new Vector2(0f, 1f - size.y), new Vector2(size.x, 1f), null, edge).raycastTarget = false;
        CreateImage(rect, "RightEdge", new Vector2(size.x - 1f, 0f), new Vector2(1f, size.y), null, edge).raycastTarget = false;
        return rect;
    }

    private void ForestGlyph(RectTransform parent, string name, Vector2 position, Vector2 size, MapHudGlyph.Shape shape, Color tint)
    {
        RectTransform rect = CreateRect(name, parent, new Vector2(0f, 1f), position, size);
        MapHudGlyph graphic = rect.gameObject.AddComponent<MapHudGlyph>();
        graphic.shape = shape;
        graphic.color = tint;
        graphic.raycastTarget = false;
    }
}
