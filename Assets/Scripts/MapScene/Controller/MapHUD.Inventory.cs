using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public partial class MapHUD
{
    private const int InventorySortingOrder = 9400;
    private bool inventoryOnly, inventoryClosing;
    private Canvas inventoryCanvas;
    private RectTransform inventoryLayer, inventoryModal, inventorySafeArea, inventoryWell, inventoryTitle;
    private CanvasGroup inventoryGroup, inventoryPageGroup;
    private TextMeshProUGUI inventorySectionLabel, inventoryHint, inventoryEmpty;
    private Button inventoryCloseButton, inventoryDismissButton;
    private float inventoryReveal, inventoryPageReveal = 1f;
    private GameObject inventoryPreviousSelection;
    private readonly List<GameObject> inventoryEmptySlots = new List<GameObject>();
    private readonly List<TextMeshProUGUI> inventoryLabels = new List<TextMeshProUGUI>();

    public bool IsInventoryVisible => inventoryModal != null && inventoryModal.gameObject.activeInHierarchy;
    public event Action InventoryHidden;

    /// <summary>Use the same inventory and equipment rules without adding the map HUD to another scene.</summary>
    public void InitializeInventoryOnly()
    {
        if (canvas != null) return;
        inventoryOnly = true;
        MapTreasureArt art = MapTreasureArt.Load();
        if (art != null) { keywordFont = art.headingFont; bodyFont = art.bodyFont; }
        Initialize();
    }

    public void SetInventoryEntryInteractable(bool value)
    {
        if (inventoryButton != null) inventoryButton.interactable = value;
    }

    private static string InventoryText(string chinese, string english) => GameText.LanguageCode == GameText.Chinese ? chinese : english;

    private void BuildInventoryButton()
    {
        inventoryButton = MapInventoryEntry.Create(forestSafeArea, ToggleInventoryPanel);
    }

    private void BuildInventoryPanel()
    {
        inventoryLayer = CreateRect("InventoryLayer", (RectTransform)canvas.transform, Vector2.zero, Vector2.zero, Vector2.zero);
        StretchInventoryRect(inventoryLayer);
        inventoryCanvas = inventoryLayer.gameObject.AddComponent<Canvas>();
        inventoryCanvas.overrideSorting = true;
        inventoryCanvas.sortingOrder = InventorySortingOrder;
        inventoryLayer.gameObject.AddComponent<GraphicRaycaster>();
        inventoryModal = CreateRect("InventoryModal", inventoryLayer, Vector2.zero, Vector2.zero, Vector2.zero);
        StretchInventoryRect(inventoryModal);
        inventoryGroup = inventoryModal.gameObject.AddComponent<CanvasGroup>();
        Image backdrop = inventoryModal.gameObject.AddComponent<Image>();
        backdrop.color = new Color(0.006f, 0.018f, 0.013f, 0.7f);
        Button dismiss = inventoryModal.gameObject.AddComponent<Button>();
        dismiss.transition = Selectable.Transition.None;
        dismiss.onClick.AddListener(CloseInventoryPanel);
        inventorySafeArea = CreateRect("SafeArea", inventoryModal, Vector2.zero, Vector2.zero, Vector2.zero);
        StretchInventoryRect(inventorySafeArea);
        inventoryPanelRect = CreateRect("InventoryPanel", inventorySafeArea, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1880f, 880f));
        inventoryPanelImage = inventoryPanelRect.gameObject.AddComponent<Image>();
        inventoryPanelImage.color = Color.clear;
        Button blocker = inventoryPanelRect.gameObject.AddComponent<Button>();
        blocker.transition = Selectable.Transition.None;
        blocker.navigation = new Navigation { mode = Navigation.Mode.None };
        RectTransform skin = CreateRect("ForestFrame", inventoryPanelRect, Vector2.zero, Vector2.zero, Vector2.zero);
        StretchInventoryRect(skin);
        Image frame = skin.gameObject.AddComponent<Image>();
        frame.sprite = Resources.Load<Sprite>("TutorialUI/ForestWindow");
        frame.type = Image.Type.Sliced;
        frame.pixelsPerUnitMultiplier = 3.4f;
        frame.raycastTarget = false;

        inventoryTitle = CreateRect("WoodTitle", inventoryPanelRect, new Vector2(0.5f, 1f), new Vector2(0f, 12f), new Vector2(650f, 140f));
        inventoryTitle.pivot = new Vector2(0.5f, 0.5f);
        Image wood = inventoryTitle.gameObject.AddComponent<Image>();
        wood.sprite = Resources.Load<Sprite>("TutorialUI/WoodTitle");
        wood.preserveAspect = true;
        wood.raycastTarget = false;
        MapItemIconFit.Attach(wood);
        Image titleIcon = CreateCenteredImage(inventoryTitle, "BackpackEmblem", new Vector2(-205f, 0), new Vector2(76f, 76f), Resources.Load<Sprite>("InventoryUI/EntryButton"), Color.white);
        titleIcon.preserveAspect = true; titleIcon.raycastTarget = false;
        TextMeshProUGUI title = InventoryLabel(inventoryTitle, "Title", Vector2.zero, new Vector2(338f, 74f), 56,
            InventoryText("冒险背包", "Adventurer's Pack"), true);
        title.alignment = TextAlignmentOptions.Midline;
        title.fontSizeMin = 24;

        inventoryTabRoot = CreateRect("TabRoot", inventoryPanelRect, new Vector2(0f, 1f), Vector2.zero, Vector2.zero);
        lootTabButton = BuildTabButton(inventoryTabRoot, "LootTab", Vector2.zero, GameText.Get("battle.settlement.build_loot_section.loot", "战利品"), () => SetInventoryTab(InventoryTab.Loot));
        weaponTabButton = BuildTabButton(inventoryTabRoot, "WeaponTab", Vector2.zero, GameText.Get("ui.map_crafting.weapon", "武器"), () => SetInventoryTab(InventoryTab.Weapons));
        blueprintTabButton = BuildTabButton(inventoryTabRoot, "BlueprintTab", Vector2.zero, GameText.Get("ui.map_hud.blueprints", "图纸"), () => SetInventoryTab(InventoryTab.Blueprints));
        relicTabButton = BuildTabButton(inventoryTabRoot, "RelicTab", Vector2.zero, GameText.Get("ui.map_hud.relics", "遗物"), () => SetInventoryTab(InventoryTab.Relics));

        inventoryWell = CreateRect("CollectionWell", inventoryPanelRect, new Vector2(0f, 1f), Vector2.zero, Vector2.zero);
        MapInventoryGraphic inset = inventoryWell.gameObject.AddComponent<MapInventoryGraphic>();
        inset.inventoryStyle = MapInventoryGraphic.InventoryStyle.Well; inset.raycastTarget = false;
        inventorySectionLabel = InventoryLabel(inventoryWell, "SectionTitle", Vector2.zero, new Vector2(260, 48), 34, string.Empty, true);
        RectTransform leftRule = CreateRect("LeftRule", inventoryWell, new Vector2(0f, 1f), Vector2.zero, Vector2.zero);
        leftRule.gameObject.AddComponent<MapMerchantFrameGraphic>().style = MapMerchantFrameGraphic.Style.Divider;
        RectTransform rightRule = CreateRect("RightRule", inventoryWell, new Vector2(0f, 1f), Vector2.zero, Vector2.zero);
        rightRule.gameObject.AddComponent<MapMerchantFrameGraphic>().style = MapMerchantFrameGraphic.Style.Divider;
        inventoryGridOffset = new Vector2(38f, -226f);
        inventoryScrollbarWidth = 10f;
        inventoryScrollbarOffset = Vector2.zero;
        inventoryScrollbarAlwaysVisible = true;
        inventoryScrollbarTrackColor = new Color(0.075f, 0.10f, 0.045f);
        inventoryScrollbarHandleColor = new Color(0.91f, 0.67f, 0.23f);
        inventoryScrollSensitivity = 70f;
        BuildInventoryScrollArea();
        inventoryPageGroup = inventoryViewportRoot.gameObject.AddComponent<CanvasGroup>();
        inventoryEmpty = InventoryLabel(inventoryViewportRoot, "EmptyInventory", Vector2.zero, new Vector2(600, 70), 30,
            InventoryText("背包空空，旅途中会收获更多珍藏", "Treasures from your journey will appear here."), false);
        inventoryEmpty.color = new Color(0.81f, 0.79f, 0.61f);

        inventoryHint = InventoryLabel(inventoryPanelRect, "InventoryHint", Vector2.zero, new Vector2(600, 40), 25,
            InventoryText("点击物品查看详情", "Select an item to view its details"), false);
        inventoryHint.alignment = TextAlignmentOptions.MidlineLeft;
        inventoryCloseButton = InventoryButton(inventoryPanelRect, "CloseButton", GameText.Get("ui.close", "关闭"), 38, CloseInventoryPanel);
        inventoryDismissButton = InventoryButton(inventoryPanelRect, "DismissButton", "×", 58, CloseInventoryPanel);
        inventoryDismissButton.GetComponent<MapMerchantFrameGraphic>().jadeFace = true;
        inventoryModal.gameObject.SetActive(false);
        inventoryLayer.gameObject.SetActive(false);
    }

    private TextMeshProUGUI InventoryLabel(RectTransform parent, string name, Vector2 position, Vector2 size, int fontSize, string value, bool heading)
    {
        TextMeshProUGUI text = CreateCenteredText(parent, name, position, size, fontSize, ForestIvory, value, heading ? keywordFont : bodyFont);
        text.enableAutoSizing = true;
        text.fontSizeMin = Mathf.Max(18, fontSize * 0.65f);
        text.fontSizeMax = fontSize;
        text.overflowMode = TextOverflowModes.Ellipsis;
        inventoryLabels.Add(text);
        return text;
    }

    private Button InventoryButton(RectTransform parent, string name, string label, int size, UnityEngine.Events.UnityAction click)
    {
        RectTransform rect = CreateRect(name, parent, new Vector2(0.5f, 0f), Vector2.zero, new Vector2(300, 76));
        MapMerchantFrameGraphic face = rect.gameObject.AddComponent<MapMerchantFrameGraphic>();
        face.style = MapMerchantFrameGraphic.Style.SecondaryButton;
        Button button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = face;
        button.transition = Selectable.Transition.None; button.onClick.AddListener(click);
        rect.gameObject.AddComponent<MapCraftingButtonFeedback>();
        TextMeshProUGUI text = InventoryLabel(rect, "Label", Vector2.zero, Vector2.zero, size, label, true);
        StretchInventoryRect(text.rectTransform, 10f);
        return button;
    }

    private Button BuildTabButton(RectTransform parent, string name, Vector2 position, string label, UnityEngine.Events.UnityAction click)
    {
        RectTransform rect = CreateRect(name, parent, new Vector2(0f, 1f), position, new Vector2(400, 84));
        MapInventoryGraphic face = rect.gameObject.AddComponent<MapInventoryGraphic>();
        face.inventoryStyle = MapInventoryGraphic.InventoryStyle.Tab;
        Button button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = face;
        button.transition = Selectable.Transition.None; button.onClick.AddListener(click);
        MapInventorySlotFeedback feedback = rect.gameObject.AddComponent<MapInventorySlotFeedback>(); feedback.rim = face;
        TextMeshProUGUI text = InventoryLabel(rect, "Label", Vector2.zero, Vector2.zero, 40, label, true);
        StretchInventoryRect(text.rectTransform, 12f);
        Sprite iconSprite = name == "LootTab" ? MapForestArt.Get("Treasure") : name == "WeaponTab" ? MapForestArt.Get("Sword")
            : name == "RelicTab" ? MapForestArt.Get("Compass") : null;
        if (name == "BlueprintTab")
        {
            BlueprintTable_SO table = Resources.Load<BlueprintTable_SO>("TreasureBlueprintTable");
            if (table != null && table.Blueprints != null)
                foreach (CraftingBlueprint_SO blueprint in table.Blueprints)
                    if (blueprint != null && blueprint.DisplayIcon != null) { iconSprite = blueprint.DisplayIcon; break; }
        }
        Image icon = CreateCenteredImage(rect, "TabIcon", Vector2.zero, new Vector2(66, 66), iconSprite, Color.white);
        icon.raycastTarget = false; icon.preserveAspect = true; MapItemIconFit.Attach(icon);
        return button;
    }

    private void ApplyTabVisual(Button button, bool selected)
    {
        if (button == null) return;
        button.GetComponent<MapInventoryGraphic>().SetState(selected, true);
        TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
        if (label != null) label.color = selected ? new Color(0.20f, 0.12f, 0.035f) : ForestIvory;
    }

    private void ApplyInventoryPanelLayout()
    {
        if (inventoryPanelRect == null) return;
        Vector2 viewport = forestLastViewport.x > 0 ? forestLastViewport : new Vector2(Screen.width, Screen.height);
        Rect safe = forestLastSafeArea.width > 0 ? forestLastSafeArea : new Rect(0, 0, viewport.x, viewport.y);
        float unit = 1080f / Mathf.Max(1f, viewport.y);
        inventorySafeArea.anchorMin = new Vector2(safe.xMin / viewport.x, safe.yMin / viewport.y);
        inventorySafeArea.anchorMax = new Vector2(safe.xMax / viewport.x, safe.yMax / viewport.y);
        inventorySafeArea.offsetMin = inventorySafeArea.offsetMax = Vector2.zero;
        Vector2 available = safe.size * unit;
        inventoryPanelSize = new Vector2(Mathf.Min(1880, available.x - (available.x < 1100 ? 40 : 130)), Mathf.Min(924, available.y - 136));
        inventoryPanelSize = Vector2.Max(new Vector2(280, 500), inventoryPanelSize);
        inventoryPanelRect.sizeDelta = inventoryPanelSize;
        float titleWidth = Mathf.Min(650, inventoryPanelSize.x * 0.74f);
        inventoryTitle.sizeDelta = new Vector2(titleWidth, 140);
        RectTransform titleEmblem = (RectTransform)inventoryTitle.Find("BackpackEmblem");
        bool showEmblem = inventoryPanelSize.x >= 850;
        titleEmblem.gameObject.SetActive(showEmblem);
        titleEmblem.anchoredPosition = new Vector2(-titleWidth * 0.315f, 0);
        TMP_Text title = inventoryTitle.GetComponentInChildren<TMP_Text>();
        if (title != null)
        {
            title.rectTransform.anchoredPosition = Vector2.zero;
            title.rectTransform.sizeDelta = new Vector2(titleWidth * (showEmblem ? 0.52f : 0.68f), 74);
        }
        float innerWidth = inventoryPanelSize.x - 96;
        inventoryTabSize = new Vector2((innerWidth - 36) / 4, 84);
        ApplyRect(inventoryTabRoot, new Vector2(0, 1), new Vector2(48, -66), new Vector2(innerWidth, 84));
        Button[] tabs = { lootTabButton, weaponTabButton, blueprintTabButton, relicTabButton };
        for (int i = 0; i < tabs.Length; i++)
        {
            ApplyRect((RectTransform)tabs[i].transform, new Vector2(0, 1), new Vector2(i * (inventoryTabSize.x + 12), 0), inventoryTabSize);
            RectTransform icon = (RectTransform)tabs[i].transform.Find("TabIcon");
            bool showIcon = inventoryTabSize.x >= 240;
            icon.gameObject.SetActive(showIcon);
            icon.anchoredPosition = new Vector2(-inventoryTabSize.x * 0.25f, 0);
            TMP_Text label = tabs[i].GetComponentInChildren<TMP_Text>(true);
            label.fontSizeMin = 18;
            if (inventoryTabSize.x < 180 && GameText.LanguageCode != GameText.Chinese)
                label.text = i == 0 ? "Loot" : i == 1 ? "Arms" : i == 2 ? "Plans" : "Relics";
            else
                label.text = i == 0 ? GameText.Get("battle.settlement.build_loot_section.loot", "战利品")
                    : i == 1 ? GameText.Get("ui.map_crafting.weapon", "武器")
                    : i == 2 ? GameText.Get("ui.map_hud.blueprints", "图纸") : GameText.Get("ui.map_hud.relics", "遗物");
            label.rectTransform.anchorMin = label.rectTransform.anchorMax = label.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            label.rectTransform.anchoredPosition = new Vector2(showIcon ? 42 : 0, 0);
            label.rectTransform.sizeDelta = new Vector2(inventoryTabSize.x - (showIcon ? 110 : 24), 66);
        }
        ApplyRect(inventoryWell, new Vector2(0, 1), new Vector2(36, -166), new Vector2(inventoryPanelSize.x - 72, inventoryPanelSize.y - 252));
        float wellWidth = inventoryPanelSize.x - 72;
        inventorySectionLabel.rectTransform.anchorMin = inventorySectionLabel.rectTransform.anchorMax = new Vector2(0.5f, 1);
        inventorySectionLabel.rectTransform.anchoredPosition = new Vector2(0, -32);
        float ruleWidth = Mathf.Max(0, (wellWidth - 310) * 0.5f - 28);
        ApplyRect((RectTransform)inventoryWell.Find("LeftRule"), new Vector2(0, 1), new Vector2(28, -25), new Vector2(ruleWidth, 18));
        ApplyRect((RectTransform)inventoryWell.Find("RightRule"), new Vector2(0, 1), new Vector2(wellWidth - ruleWidth - 28, -25), new Vector2(ruleWidth, 18));
        inventoryGridOffset = new Vector2(60, -228);
        inventoryViewportSize = new Vector2(inventoryPanelSize.x - 120, inventoryPanelSize.y - 366);
        inventoryGridColumns = Mathf.Clamp(Mathf.FloorToInt((inventoryViewportSize.x - 18) / 184f), 2, 8);
        inventoryGridSpacing = new Vector2(22, 12);
        float cellWidth = (inventoryViewportSize.x - 30 - (inventoryGridColumns - 1) * inventoryGridSpacing.x) / inventoryGridColumns;
        float iconHeight = Mathf.Max(80f, Mathf.Min(134f, cellWidth * 0.84f, (inventoryViewportSize.y - 24) / 3f - 52));
        inventorySlotSize = new Vector2(cellWidth, iconHeight + 52);
        inventoryIconSize = new Vector2(Mathf.Min(cellWidth - 18, iconHeight - 10), iconHeight - 10);
        inventorySlotOffset = Vector2.zero;
        inventoryContentSizePadding = Vector2.zero;
        ApplyRect((RectTransform)inventoryCloseButton.transform, new Vector2(0.5f, 0), new Vector2(0, 18), new Vector2(Mathf.Min(320, inventoryPanelSize.x * 0.4f), 74));
        ((RectTransform)inventoryCloseButton.transform).pivot = new Vector2(0.5f, 0);
        ApplyRect((RectTransform)inventoryDismissButton.transform, Vector2.one, new Vector2(-20, -12), new Vector2(76, 76));
        ApplyRect(inventoryHint.rectTransform, new Vector2(0, 1), new Vector2(62, -(inventoryPanelSize.y - 126)), new Vector2(inventoryPanelSize.x - 124, 34));
        UpdateInventoryScrollLayout(false);
        for (int i = 0; i < inventorySlotObjects.Count; i++) LayoutInventorySlot((RectTransform)inventorySlotObjects[i].transform, i, true);
        RefreshInventoryEmptySlots();
        ApplyInventoryMotion();
    }

    private void ToggleInventoryPanel()
    {
        if (IsInventoryVisible && !inventoryClosing) CloseInventoryPanel();
        else ShowInventoryPanel();
    }

    public void ShowInventoryPanel(int sortOrderOverride = -1)
    {
        Initialize();
        bool wasVisible = IsInventoryVisible;
        if (!wasVisible && EventSystem.current != null) inventoryPreviousSelection = EventSystem.current.currentSelectedGameObject;
        inventoryCanvas.sortingOrder = Mathf.Max(InventorySortingOrder, sortOrderOverride);
        inventoryLayer.gameObject.SetActive(true);
        inventoryModal.gameObject.SetActive(true);
        inventoryModal.SetAsFirstSibling();
        inventoryClosing = false;
        inventoryGroup.interactable = inventoryGroup.blocksRaycasts = true;
        if (!wasVisible) inventoryReveal = Application.isPlaying ? 0 : 1;
        RefreshInventoryView();
        ApplyInventoryPanelLayout();
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(lootTabButton.gameObject);
    }

    public void CloseInventoryPanel()
    {
        if (!IsInventoryVisible || inventoryClosing) return;
        selectedInventoryState = null;
        runtimeInventoryDetailView?.HideImmediately();
        inventoryClosing = true;
        inventoryGroup.interactable = false;
        if (!Application.isPlaying || !isActiveAndEnabled) HideInventoryPanel();
    }

    public void HideInventoryPanel()
    {
        bool wasVisible = IsInventoryVisible;
        selectedInventoryState = null;
        runtimeInventoryDetailView?.HideImmediately();
        inventoryClosing = false; inventoryReveal = 0;
        if (inventoryModal != null) inventoryModal.gameObject.SetActive(false);
        if (inventoryLayer != null) inventoryLayer.gameObject.SetActive(false);
        ResetCanvasSortOrder();
        if (wasVisible)
        {
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(inventoryPreviousSelection != null && inventoryPreviousSelection.activeInHierarchy
                    ? inventoryPreviousSelection : inventoryButton != null && inventoryButton.gameObject.activeInHierarchy ? inventoryButton.gameObject : null);
            InventoryHidden?.Invoke();
        }
        inventoryPreviousSelection = null;
    }

    public void ResetCanvasSortOrder()
    {
        if (canvas != null) canvas.sortingOrder = DefaultCanvasSortingOrder;
        if (inventoryCanvas != null) inventoryCanvas.sortingOrder = InventorySortingOrder;
    }

    private void AdvanceInventoryPresentation(float delta)
    {
        if (!IsInventoryVisible) return;
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (runtimeInventoryDetailView != null && runtimeInventoryDetailView.IsVisible) HideInventoryDetail();
            else CloseInventoryPanel();
        }
        inventoryReveal = Mathf.MoveTowards(inventoryReveal, inventoryClosing ? 0 : 1, delta / (inventoryClosing ? 0.16f : 0.24f));
        inventoryPageReveal = Mathf.MoveTowards(inventoryPageReveal, 1, delta / 0.16f);
        ApplyInventoryMotion();
        if (inventoryClosing && inventoryReveal <= 0) HideInventoryPanel();
    }

    private void ApplyInventoryMotion()
    {
        if (inventoryGroup == null) return;
        float eased = 1f - Mathf.Pow(1f - inventoryReveal, 3);
        inventoryGroup.alpha = eased;
        inventoryPanelRect.localScale = Vector3.one * Mathf.Lerp(0.965f, 1, eased);
        inventoryPanelRect.anchoredPosition = new Vector2(0, Mathf.Lerp(-16, 0, eased));
        if (inventoryPageGroup != null) inventoryPageGroup.alpha = Mathf.SmoothStep(0, 1, inventoryPageReveal);
    }

    private void OnDisable() { HideInventoryPanel(); }

    private void CreateInventorySlot(int index, Sprite iconSprite, string countText, string itemName, bool equipped, UnityEngine.Events.UnityAction onClick)
    {
        RectTransform rect = CreateRect($"Slot_{index}", inventoryGridRoot, new Vector2(0, 1), Vector2.zero, inventorySlotSize);
        Image hit = rect.gameObject.AddComponent<Image>(); hit.color = Color.clear;
        Button button = rect.gameObject.AddComponent<Button>(); button.transition = Selectable.Transition.None; button.onClick.AddListener(onClick);
        RectTransform rimRect = CreateRect("SlotRim", rect, new Vector2(0, 1), Vector2.zero, Vector2.zero);
        MapInventoryGraphic rim = rimRect.gameObject.AddComponent<MapInventoryGraphic>(); rim.inventoryStyle = MapInventoryGraphic.InventoryStyle.Slot; rim.raycastTarget = false;
        Image icon = CreateCenteredImage(rect, "Icon", Vector2.zero, inventoryIconSize, iconSprite != null ? iconSprite : GetFallbackSprite(), Color.white);
        icon.raycastTarget = false; icon.preserveAspect = true; MapItemIconFit.Attach(icon);
        TextMeshProUGUI count = InventoryLabel(rect, "Count", Vector2.zero, new Vector2(100, 32), 26, $"×{countText}", false);
        count.alignment = TextAlignmentOptions.BottomRight;
        count.outlineColor = new Color(0.015f, 0.03f, 0.02f); count.outlineWidth = 0.16f;
        TextMeshProUGUI label = InventoryLabel(rect, "ItemName", Vector2.zero, Vector2.zero, 26, itemName, false);
        label.enableWordWrapping = true; label.fontSizeMin = 20; label.maxVisibleLines = 2;
        if (equipped)
        {
            TextMeshProUGUI badge = InventoryLabel(rect, "Equipped", Vector2.zero, new Vector2(80, 28), 20, InventoryText("已装备", "Equipped"), false);
            badge.color = ForestJade;
        }
        MapInventorySlotFeedback feedback = rect.gameObject.AddComponent<MapInventorySlotFeedback>(); feedback.icon = icon.rectTransform; feedback.rim = rim;
        LayoutInventorySlot(rect, index, true);
        inventorySlotObjects.Add(rect.gameObject);
    }

    private void LayoutInventorySlot(RectTransform rect, int index, bool occupied)
    {
        float iconHeight = inventorySlotSize.y - 52;
        ApplyRect(rect, new Vector2(0, 1), new Vector2((index % inventoryGridColumns) * (inventorySlotSize.x + inventoryGridSpacing.x), -(index / inventoryGridColumns) * (inventorySlotSize.y + inventoryGridSpacing.y)), inventorySlotSize);
        RectTransform rim = occupied ? (RectTransform)rect.Find("SlotRim") : rect;
        if (occupied) ApplyRect(rim, new Vector2(0, 1), Vector2.zero, new Vector2(inventorySlotSize.x, iconHeight));
        else rect.sizeDelta = new Vector2(inventorySlotSize.x, iconHeight);
        if (!occupied) return;
        RectTransform icon = (RectTransform)rect.Find("Icon");
        icon.anchorMin = icon.anchorMax = icon.pivot = new Vector2(0.5f, 1);
        icon.anchoredPosition = new Vector2(0, -7); icon.sizeDelta = inventoryIconSize;
        ApplyRect((RectTransform)rect.Find("Count"), new Vector2(1, 1), new Vector2(-10, -(iconHeight - 34)), new Vector2(inventorySlotSize.x - 20, 28));
        ApplyRect((RectTransform)rect.Find("ItemName"), new Vector2(0, 1), new Vector2(0, -(iconHeight + 4)), new Vector2(inventorySlotSize.x, 46));
        RectTransform equipped = (RectTransform)rect.Find("Equipped");
        if (equipped != null) ApplyRect(equipped, new Vector2(0, 1), new Vector2(7, -7), new Vector2(inventorySlotSize.x - 14, 26));
    }

    private void RefreshInventoryEmptySlots()
    {
        if (inventoryGridRoot == null) return;
        int visibleRows = Mathf.Max(1, Mathf.FloorToInt((inventoryViewportSize.y + 12.5f) / (inventorySlotSize.y + 12)));
        int count = Mathf.Max(0, inventoryGridColumns * visibleRows - inventorySlotObjects.Count);
        while (inventoryEmptySlots.Count < count)
        {
            RectTransform rect = CreateRect("EmptySlot", inventoryGridRoot, new Vector2(0, 1), Vector2.zero, Vector2.zero);
            MapInventoryGraphic face = rect.gameObject.AddComponent<MapInventoryGraphic>();
            face.inventoryStyle = MapInventoryGraphic.InventoryStyle.Slot; face.color = new Color(1, 1, 1, 0.48f); face.raycastTarget = false;
            inventoryEmptySlots.Add(rect.gameObject);
        }
        for (int i = 0; i < inventoryEmptySlots.Count; i++)
        {
            inventoryEmptySlots[i].SetActive(i < count);
            if (i < count) LayoutInventorySlot((RectTransform)inventoryEmptySlots[i].transform, inventorySlotObjects.Count + i, false);
        }
        inventoryEmpty.gameObject.SetActive(inventorySlotObjects.Count == 0);
        inventoryEmpty.rectTransform.sizeDelta = new Vector2(Mathf.Max(100, inventoryViewportSize.x - 60), 80);
        inventoryEmpty.transform.SetAsLastSibling();
        inventorySectionLabel.text = currentInventoryTab == InventoryTab.Loot ? lootTabButton.GetComponentInChildren<TMP_Text>().text
            : currentInventoryTab == InventoryTab.Weapons ? weaponTabButton.GetComponentInChildren<TMP_Text>().text
            : currentInventoryTab == InventoryTab.Blueprints ? blueprintTabButton.GetComponentInChildren<TMP_Text>().text : relicTabButton.GetComponentInChildren<TMP_Text>().text;
    }

    private Vector2 ResolveInventoryViewportSize() => Vector2.Max(Vector2.one, inventoryViewportSize);
    private Vector2 ResolveInventoryViewportContentSize(Vector2 size) => new Vector2(Mathf.Max(1, size.x - 30), Mathf.Max(1, size.y));
    private void UpdateInventoryScrollLayout(bool resetToTop)
    {
        if (inventoryScrollRoot == null) return;
        Vector2 size = ResolveInventoryViewportSize();
        Vector2 viewportSize = ResolveInventoryViewportContentSize(size);
        int rows = Mathf.CeilToInt(inventorySlotObjects.Count / (float)Mathf.Max(1, inventoryGridColumns));
        float contentHeight = Mathf.Max(viewportSize.y, rows * (inventorySlotSize.y + inventoryGridSpacing.y) - inventoryGridSpacing.y);
        ApplyRect(inventoryScrollRoot, new Vector2(0, 1), inventoryGridOffset, size);
        ApplyRect(inventoryViewportRoot, new Vector2(0, 1), Vector2.zero, viewportSize);
        float previousY = inventoryGridRoot.anchoredPosition.y;
        ApplyRect(inventoryGridRoot, new Vector2(0, 1), new Vector2(0, resetToTop ? 0 : Mathf.Clamp(previousY, 0, contentHeight - viewportSize.y)), new Vector2(viewportSize.x, contentHeight));
        bool overflow = contentHeight > viewportSize.y + 0.1f;
        inventoryScrollRect.vertical = overflow;
        inventoryScrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
        if (resetToTop) { inventoryScrollRect.StopMovement(); inventoryScrollRect.verticalNormalizedPosition = 1; }
        inventoryScrollbarRoot.gameObject.SetActive(true);
        inventoryScrollbarRoot.anchorMin = inventoryScrollbarRoot.anchorMax = inventoryScrollbarRoot.pivot = Vector2.one;
        inventoryScrollbarRoot.anchoredPosition = Vector2.zero;
        inventoryScrollbarRoot.sizeDelta = new Vector2(10, size.y);
        Scrollbar scrollbar = inventoryScrollbarRoot.GetComponent<Scrollbar>();
        scrollbar.interactable = overflow;
    }

    private static void StretchInventoryRect(RectTransform rect, float inset = 0)
    {
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.one * inset; rect.offsetMax = Vector2.one * -inset;
    }
}
