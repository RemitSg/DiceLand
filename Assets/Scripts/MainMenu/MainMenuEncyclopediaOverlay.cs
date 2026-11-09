using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class MainMenuEncyclopediaOverlay : MonoBehaviour
{
    private const int MaxColumns = 8;
    private const float CardGap = 12;
    private const float MinCardWidth = 138;
    private const float CardHeight = 126;
    private static readonly Color Ivory = new Color32(242, 231, 200, 255);
    private static readonly Color Gold = new Color32(227, 189, 113, 255);
    private static readonly Color Jade = new Color32(108, 203, 149, 255);
    private static readonly Color Muted = new Color32(165, 177, 152, 255);
    private static readonly EncyclopediaCategory[] Categories =
    {
        EncyclopediaCategory.Enemies, EncyclopediaCategory.Weapons, EncyclopediaCategory.Relics,
        EncyclopediaCategory.Loot, EncyclopediaCategory.Calamities
    };

    private sealed class Section
    {
        public RectTransform header;
        public bool headingOnly;
        public readonly List<RectTransform> cards = new List<RectTransform>();
    }

    private readonly List<Section> sections = new List<Section>();
    private readonly float[] scrollPositions = { 1, 1, 1, 1, 1 };
    private readonly Button[] tabs = new Button[Categories.Length];
    private readonly TMP_Text[] tabLabels = new TMP_Text[Categories.Length];
    private readonly MapCraftingFrameGraphic[] tabFaces = new MapCraftingFrameGraphic[Categories.Length];
    private Canvas canvas;
    private CanvasGroup browserInput;
    private RectTransform safeRoot, panel, plaque, closeRect, progressFrame, progressFill;
    private RectTransform viewport, content, scrollBarRect;
    private TMP_Text heading, progressLabel, progressCount, emptyLabel;
    private ScrollRect scroll;
    private MapInventoryDetailView detail;
    private EncyclopediaCatalog catalog;
    private EncyclopediaProgress progress;
    private EncyclopediaCategory category;
    private Action onClosed;
    private GameObject previousSelection;
    private Vector2 viewportPixels;
    private Rect safePixels;

    public bool IsVisible => canvas != null && canvas.gameObject.activeSelf;
    public Canvas PresentationCanvas => canvas;

    public static string CategoryLabel(EncyclopediaCategory value)
    {
        switch (value)
        {
            case EncyclopediaCategory.Enemies: return GameText.Get("ui.encyclopedia.enemies", "敌人");
            case EncyclopediaCategory.Weapons: return GameText.Get("ui.encyclopedia.weapons", "武器");
            case EncyclopediaCategory.Relics: return GameText.Get("ui.map_hud.relics", "遗物");
            case EncyclopediaCategory.Loot: return GameText.Get("ui.encyclopedia.loot", "战利品");
            default: return GameText.Get("ui.encyclopedia.calamities", "灾厄");
        }
    }

    public void Show(EncyclopediaCatalog entries, EncyclopediaProgress collection, Action closed)
    {
        if (IsVisible)
            return;

        catalog = entries;
        progress = collection;
        onClosed = closed;
        if (canvas == null)
            Build();

        previousSelection = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        canvas.gameObject.SetActive(true);
        RefreshLabels();
        Populate();
        ApplyViewport(new Vector2(Screen.width, Screen.height), Screen.safeArea);
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(tabs[(int)category].gameObject);
    }

    public void Back()
    {
        if (!IsVisible)
            return;
        if (detail.IsVisible)
        {
            detail.Hide();
            return;
        }
        if (!MainMenuPageMotion.TryHide(canvas, ClosePage))
            ClosePage();
    }

    private void ClosePage()
    {
        scrollPositions[(int)category] = scroll.verticalNormalizedPosition;
        canvas.gameObject.SetActive(false);
        Action closed = onClosed;
        onClosed = null;
        closed?.Invoke();
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(previousSelection != null && previousSelection.activeInHierarchy
                ? previousSelection : null);
        previousSelection = null;
    }

    private void OnDestroy()
    {
        if (canvas != null)
            DisposeObject(canvas.gameObject);
    }

    private void LateUpdate()
    {
        Vector2 size = new Vector2(Screen.width, Screen.height);
        if (IsVisible && (viewportPixels != size || safePixels != Screen.safeArea))
        {
            scrollPositions[(int)category] = scroll.verticalNormalizedPosition;
            ApplyViewport(size, Screen.safeArea);
        }
    }

    private void Build()
    {
        var root = new GameObject("EncyclopediaCanvas", typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster));
        SceneManager.MoveGameObjectToScene(root, gameObject.scene);
        canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9650;
        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 1;

        RectTransform backdrop = MakeRect(root.transform, "EncyclopediaBackdrop");
        Stretch(backdrop);
        backdrop.gameObject.AddComponent<Image>().color = new Color(.005f, .018f, .012f, .56f);
        safeRoot = MakeRect(root.transform, "EncyclopediaSafeArea");
        Stretch(safeRoot);
        panel = MakeRect(safeRoot, "EncyclopediaPanel");
        panel.anchorMin = panel.anchorMax = panel.pivot = Vector2.one * .5f;
        browserInput = panel.gameObject.AddComponent<CanvasGroup>();
        AddFace(panel, MapCraftingFrameGraphic.Style.Window);

        plaque = MakeRect(panel, "TitlePlaque");
        var plaqueFace = plaque.gameObject.AddComponent<MapItemDetailFrameGraphic>();
        plaqueFace.detailStyle = MapItemDetailFrameGraphic.DetailStyle.Title;
        plaqueFace.raycastTarget = false;
        heading = Label(plaque, "Heading", 42, Gold, TextAlignmentOptions.Center, true);
        Stretch(heading.rectTransform, 43, 2);

        closeRect = MakeRect(panel, "CloseEncyclopedia");
        AddButton(closeRect, AddFace(closeRect, MapCraftingFrameGraphic.Style.Secondary), Back);
        TMP_Text closeLabel = Label(closeRect, "Label", 34, new Color32(46, 32, 12, 255), TextAlignmentOptions.Center, true);
        closeLabel.text = "×";
        Stretch(closeLabel.rectTransform);

        for (int i = 0; i < Categories.Length; i++)
        {
            EncyclopediaCategory tab = Categories[i];
            RectTransform rect = MakeRect(panel, tab + "Tab");
            tabFaces[i] = AddFace(rect, MapCraftingFrameGraphic.Style.Material);
            tabs[i] = AddButton(rect, tabFaces[i], () => SwitchTab(tab));
            tabLabels[i] = Label(rect, "Label", 30, Ivory, TextAlignmentOptions.Center, true);
            Stretch(tabLabels[i].rectTransform, 8, 3);
        }

        progressFrame = MakeRect(panel, "CollectionProgress");
        AddFace(progressFrame, MapCraftingFrameGraphic.Style.Badge);
        progressLabel = Label(progressFrame, "ProgressLabel", 25, Gold, TextAlignmentOptions.MidlineLeft, true);
        progressCount = Label(progressFrame, "ProgressCount", 26, Ivory, TextAlignmentOptions.MidlineRight, true);
        RectTransform track = MakeRect(progressFrame, "ProgressTrack");
        track.anchorMin = new Vector2(.27f, .38f);
        track.anchorMax = new Vector2(.66f, .62f);
        track.offsetMin = track.offsetMax = Vector2.zero;
        track.gameObject.AddComponent<Image>().color = new Color32(80, 73, 43, 255);
        RectTransform rail = MakeRect(track, "Rail");
        Stretch(rail, 2, 2);
        rail.gameObject.AddComponent<Image>().color = new Color32(6, 25, 17, 255);
        progressFill = MakeRect(rail, "JadeFill");
        progressFill.gameObject.AddComponent<Image>().color = Jade;

        RectTransform scrollRoot = MakeRect(panel, "EncyclopediaScroll");
        Stretch(scrollRoot);
        scroll = scrollRoot.gameObject.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 62;
        viewport = MakeRect(scrollRoot, "Viewport");
        viewport.gameObject.AddComponent<RectMask2D>();
        viewport.gameObject.AddComponent<Image>().color = Color.clear;
        content = MakeRect(viewport, "Entries");
        content.anchorMin = new Vector2(0, 1);
        content.anchorMax = new Vector2(1, 1);
        content.pivot = new Vector2(.5f, 1);
        scroll.viewport = viewport;
        scroll.content = content;
        BuildScrollbar(scrollRoot);
        emptyLabel = Label(viewport, "Empty", 28, Muted, TextAlignmentOptions.Center);
        Stretch(emptyLabel.rectTransform, 12, 12);

        detail = MapInventoryDetailView.Create((RectTransform)canvas.transform, "EncyclopediaDetail");
        detail.ConfigureFonts(catalog != null ? catalog.HeadingFont : null, catalog != null ? catalog.BodyFont : null);
        detail.Hidden += EnableBrowser;
    }

    private void BuildScrollbar(RectTransform parent)
    {
        scrollBarRect = MakeRect(parent, "Scrollbar");
        var track = scrollBarRect.gameObject.AddComponent<Image>();
        track.color = new Color32(94, 79, 42, 255);
        var bar = scrollBarRect.gameObject.AddComponent<Scrollbar>();
        var slidingArea = MakeRect(scrollBarRect, "SlidingArea");
        Stretch(slidingArea, 2, 2);
        RectTransform handle = MakeRect(slidingArea, "Thumb");
        Stretch(handle);
        var thumb = handle.gameObject.AddComponent<Image>();
        thumb.color = Jade;
        bar.handleRect = handle;
        bar.targetGraphic = thumb;
        bar.direction = Scrollbar.Direction.BottomToTop;
        scroll.verticalScrollbar = bar;
        scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
    }

    private void RefreshLabels()
    {
        heading.text = GameText.Get("ui.encyclopedia.title", "图鉴");
        progressLabel.text = GameText.Get("ui.encyclopedia.progress", "收集进度");
        emptyLabel.text = GameText.Get("ui.encyclopedia.empty", "暂无条目");
        for (int i = 0; i < Categories.Length; i++)
            tabLabels[i].text = CategoryLabel(Categories[i]);
    }

    private void SwitchTab(EncyclopediaCategory next)
    {
        if (next == category || detail.IsVisible)
            return;

        scrollPositions[(int)category] = scroll.verticalNormalizedPosition;
        category = next;
        Populate();
        LayoutPanel();
    }

    private void Populate()
    {
        for (int i = content.childCount - 1; i >= 0; i--)
        {
            GameObject child = content.GetChild(i).gameObject;
            child.SetActive(false);
            DisposeObject(child);
        }
        sections.Clear();
        for (int i = 0; i < Categories.Length; i++)
        {
            bool selected = category == Categories[i];
            tabFaces[i].craftingStyle = selected ? MapCraftingFrameGraphic.Style.Action : MapCraftingFrameGraphic.Style.Material;
            tabFaces[i].SetState(selected, true);
            tabLabels[i].color = selected ? Gold : Ivory;
        }

        var entries = catalog != null
            ? catalog.Entries.Where(entry => entry?.Asset != null && entry.Category == category).ToArray()
            : Array.Empty<EncyclopediaCatalog.Entry>();
        int unlocked = entries.Count(IsUnlocked);
        progressCount.text = CountLabel(unlocked, entries.Length);
        progressFill.anchorMin = Vector2.zero;
        progressFill.anchorMax = new Vector2(entries.Length > 0 ? (float)unlocked / entries.Length : 0, 1);
        progressFill.offsetMin = progressFill.offsetMax = Vector2.zero;
        emptyLabel.gameObject.SetActive(entries.Length == 0);

        if (category == EncyclopediaCategory.Enemies)
            BuildEnemySections(entries);
        else
            foreach (var group in entries.GroupBy(entry => entry.Rarity).OrderBy(group => RarityOrder(group.Key)))
                BuildSection(group.Key, group.ToArray());
    }

    private void BuildEnemySections(EncyclopediaCatalog.Entry[] entries)
    {
        BuildArea("Outer", GameText.Get("ui.encyclopedia.outer", "外层"), EncyclopediaEnemyGroup.OuterStandard, EncyclopediaEnemyGroup.OuterElite);
        BuildArea("Mid", GameText.Get("ui.encyclopedia.mid", "中层"), EncyclopediaEnemyGroup.MidStandard, EncyclopediaEnemyGroup.MidElite);
        BuildArea("Inner", GameText.Get("ui.encyclopedia.inner", "内层"), EncyclopediaEnemyGroup.InnerStandard, EncyclopediaEnemyGroup.InnerElite);
        BuildArea("Core", GameText.Get("ui.encyclopedia.core", "核心"), EncyclopediaEnemyGroup.CoreStandard, EncyclopediaEnemyGroup.CoreElite);
        BuildSingle("Boss", GameText.Get("ui.encyclopedia.boss", "BOSS"), EncyclopediaEnemyGroup.Boss);
        BuildSingle("Special", GameText.Get("ui.encyclopedia.special", "特殊遭遇"), EncyclopediaEnemyGroup.Special);

        void BuildArea(string key, string label, EncyclopediaEnemyGroup standard, EncyclopediaEnemyGroup elite)
        {
            var area = entries.Where(entry => entry.EnemyGroup == standard || entry.EnemyGroup == elite).ToArray();
            if (area.Length == 0) return;
            BuildSection("EnemyArea_" + key, label, Gold, area, true);
            BuildPool(standard, GameText.Get("ui.encyclopedia.enemy_standard", "普通"), Jade);
            BuildPool(elite, GameText.Get("ui.encyclopedia.enemy_elite", "精英"), new Color32(222, 156, 112, 255));
        }
        void BuildPool(EncyclopediaEnemyGroup group, string label, Color accent)
        {
            var pool = entries.Where(entry => entry.EnemyGroup == group).ToArray();
            if (pool.Length > 0) BuildSection("EnemyPool_" + group, label, accent, pool, false, true);
        }
        void BuildSingle(string key, string label, EncyclopediaEnemyGroup group)
        {
            var pool = entries.Where(entry => entry.EnemyGroup == group).ToArray();
            if (pool.Length > 0) BuildSection("EnemyArea_" + key, label, Gold, pool);
        }
    }

    private void BuildSection(LootRarity? rarity, EncyclopediaCatalog.Entry[] entries)
    {
        Color accent = rarity.HasValue
            ? Color.Lerp(MapInventoryDetailView.GetLootRarityColor(rarity.Value), Ivory, .28f) : Gold;
        BuildSection("Section_" + rarity,
            rarity.HasValue ? MapInventoryDetailView.EncyclopediaRarityLabel(rarity.Value) : CategoryLabel(category), accent, entries);
    }

    private void BuildSection(string key, string label, Color accent, EncyclopediaCatalog.Entry[] entries,
        bool headingOnly = false, bool subgroup = false)
    {
        var section = new Section { header = MakeRect(content, key), headingOnly = headingOnly };
        sections.Add(section);
        TMP_Text name = Label(section.header, "Rarity", headingOnly ? 31 : subgroup ? 25 : 27, accent, TextAlignmentOptions.MidlineLeft, true);
        name.text = label;
        Place(name.rectTransform, subgroup ? 20 : 0, 0, 157, 37);
        RectTransform rule = MakeRect(section.header, "SectionRule");
        rule.anchorMin = new Vector2(0, .5f);
        rule.anchorMax = new Vector2(1, .5f);
        rule.offsetMin = new Vector2(subgroup ? 183 : 163, -1);
        rule.offsetMax = new Vector2(-186, 1);
        var ruleImage = rule.gameObject.AddComponent<Image>();
        ruleImage.color = new Color(accent.r, accent.g, accent.b, .54f);
        ruleImage.raycastTarget = false;
        TMP_Text count = Label(section.header, "Count", 23, Ivory, TextAlignmentOptions.MidlineRight);
        count.text = CountLabel(entries.Count(IsUnlocked), entries.Length);
        count.rectTransform.anchorMin = count.rectTransform.anchorMax = Vector2.one;
        count.rectTransform.pivot = Vector2.one;
        count.rectTransform.sizeDelta = new Vector2(180, 37);

        if (!headingOnly)
            foreach (EncyclopediaCatalog.Entry entry in entries)
                section.cards.Add(BuildCard(entry, accent));
    }

    private RectTransform BuildCard(EncyclopediaCatalog.Entry entry, Color accent)
    {
        bool unlocked = IsUnlocked(entry);
        bool textOnly = category == EncyclopediaCategory.Calamities;
        RectTransform card = MakeRect(content, "Entry_" + entry.Id);
        var frame = card.gameObject.AddComponent<MapItemDetailFrameGraphic>();
        frame.detailStyle = MapItemDetailFrameGraphic.DetailStyle.Rarity;
        frame.accent = unlocked ? accent : Color.Lerp(new Color32(26, 42, 31, 255), accent, .43f);
        frame.raycastTarget = false;
        if (unlocked)
            AddButton(card, frame, () => Inspect(entry));

        if (!unlocked)
        {
            TMP_Text locked = Label(card, "Locked", textOnly ? 22 : 43, Muted, TextAlignmentOptions.Center, true);
            locked.text = textOnly ? GameText.Get("ui.encyclopedia.locked", "未解锁") : "?";
            Stretch(locked.rectTransform, 8, 3);
            return card;
        }

        TMP_Text title = Label(card, "EntryName", 24, Ivory, TextAlignmentOptions.Center, true);
        title.text = entry.DisplayName;
        title.enableWordWrapping = true;
        if (textOnly)
        {
            Stretch(title.rectTransform, 8, 4);
            return card;
        }

        title.rectTransform.anchorMin = Vector2.zero;
        title.rectTransform.anchorMax = new Vector2(1, 0);
        title.rectTransform.offsetMin = new Vector2(8, 6);
        title.rectTransform.offsetMax = new Vector2(-8, 40);
        if (entry.Icon != null)
        {
            RectTransform iconRect = MakeRect(card, "Icon");
            iconRect.anchorMin = iconRect.anchorMax = iconRect.pivot = new Vector2(.5f, 1);
            iconRect.anchoredPosition = new Vector2(0, -6);
            iconRect.sizeDelta = new Vector2(88, 77);
            var image = iconRect.gameObject.AddComponent<Image>();
            image.sprite = entry.Icon;
            image.preserveAspect = true;
            image.raycastTarget = false;
            MapItemIconFit.Attach(image);
        }
        return card;
    }

    private void Inspect(EncyclopediaCatalog.Entry entry)
    {
        if (!IsUnlocked(entry) || detail.IsVisible)
            return;

        scroll.StopMovement();
        detail.ShowEncyclopediaEntry(entry);
        if (detail.IsVisible)
        {
            browserInput.interactable = browserInput.blocksRaycasts = false;
            detail.ApplyViewport(viewportPixels, safePixels);
        }
    }

    private void EnableBrowser()
    {
        browserInput.interactable = browserInput.blocksRaycasts = true;
    }

    public void ApplyViewport(Vector2 size, Rect safeArea)
    {
        viewportPixels = size;
        safePixels = safeArea;
        if (safeArea.width <= 0 || safeArea.height <= 0)
            safeArea = new Rect(Vector2.zero, size);
        var full = new Vector2(Mathf.Max(1, size.x), Mathf.Max(1, size.y));
        safeRoot.anchorMin = safeArea.min / full;
        safeRoot.anchorMax = safeArea.max / full;
        LayoutPanel();
        if (detail.IsVisible)
            detail.ApplyViewport(size, safeArea);
    }

    private void LayoutPanel()
    {
        Canvas.ForceUpdateCanvases();
        float width = Mathf.Min(1980, safeRoot.rect.width * .9f);
        float height = Mathf.Min(930, safeRoot.rect.height - 76);
        panel.sizeDelta = new Vector2(Mathf.Max(1, width), Mathf.Max(1, height));
        float plaqueWidth = Mathf.Min(385, width * .56f);
        Place(plaque, (width - plaqueWidth) * .5f, -26, plaqueWidth, 73);
        Place(closeRect, width - 74, 15, 53, 53);
        float tabWidth = (width - 64 - 8 * (tabs.Length - 1)) / tabs.Length;
        for (int i = 0; i < tabs.Length; i++)
            Place((RectTransform)tabs[i].transform, 32 + i * (tabWidth + 8), 68, tabWidth, 56);
        Place(progressFrame, 32, 139, width - 64, 52);
        float progressWidth = width - 64;
        Place(progressLabel.rectTransform, 16, 0, progressWidth * .24f - 16, 52);
        Place(progressCount.rectTransform, progressWidth * .67f, 0, progressWidth * .33f - 16, 52);
        float gridWidth = Mathf.Max(1, width - 90);
        Place(viewport, 32, 205, gridWidth, Mathf.Max(1, height - 232));
        Place(scrollBarRect, width - 47, 210, 9, Mathf.Max(1, height - 241));

        int columns = Mathf.Clamp(Mathf.FloorToInt((gridWidth + CardGap) / (MinCardWidth + CardGap)), 1, MaxColumns);
        float cellWidth = (gridWidth - (columns - 1) * CardGap) / columns;
        float cellHeight = category == EncyclopediaCategory.Calamities ? 58 : CardHeight;
        float y = 4;
        foreach (Section section in sections)
        {
            Place(section.header, 0, y, gridWidth, 38);
            y += 42;
            if (section.headingOnly) continue;
            for (int i = 0; i < section.cards.Count; i++)
                Place(section.cards[i], i % columns * (cellWidth + CardGap), y + i / columns * (cellHeight + CardGap), cellWidth, cellHeight);
            y += Mathf.CeilToInt((float)section.cards.Count / columns) * (cellHeight + CardGap) + 10;
        }
        content.sizeDelta = new Vector2(0, y);
        Canvas.ForceUpdateCanvases();
        scroll.StopMovement();
        scroll.verticalNormalizedPosition = scrollPositions[(int)category];
    }

    private bool IsUnlocked(EncyclopediaCatalog.Entry entry) => progress != null && progress.IsUnlocked(entry);

    private static int RarityOrder(LootRarity? rarity)
    {
        if (!rarity.HasValue)
            return 0;
        return rarity == LootRarity.Trash ? 5 : (int)LootRarity.Legendary - (int)rarity.Value;
    }

    private static string CountLabel(int current, int total) =>
        $"{current} / {total} · {(total > 0 ? 100f * current / total : 0).ToString("0.#", System.Globalization.CultureInfo.InvariantCulture)}%";

    private TMP_Text Label(Transform parent, string name, float size, Color color, TextAlignmentOptions alignment, bool bold = false)
    {
        var text = MakeRect(parent, name).gameObject.AddComponent<TextMeshProUGUI>();
        TMP_FontAsset font = catalog != null ? (bold ? catalog.HeadingFont : catalog.BodyFont) : null;
        if (font != null)
            text.font = font;
        text.fontSize = size;
        text.enableAutoSizing = true;
        text.fontSizeMin = Mathf.Min(15, size);
        text.fontSizeMax = size;
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.color = color;
        text.alignment = alignment;
        text.raycastTarget = false;
        return text;
    }

    private static Button AddButton(RectTransform rect, Graphic face, UnityEngine.Events.UnityAction action)
    {
        face.raycastTarget = true;
        var button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = face;
        button.transition = Selectable.Transition.None;
        button.onClick.AddListener(action);
        rect.gameObject.AddComponent<MapCraftingButtonFeedback>();
        return button;
    }

    private static MapCraftingFrameGraphic AddFace(RectTransform rect, MapCraftingFrameGraphic.Style style)
    {
        var face = rect.gameObject.AddComponent<MapCraftingFrameGraphic>();
        face.craftingStyle = style;
        face.raycastTarget = false;
        return face;
    }

    private static RectTransform MakeRect(Transform parent, string name)
    {
        RectTransform rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        return rect;
    }

    private static void Place(RectTransform rect, float x, float y, float width, float height)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(x, -y);
        rect.sizeDelta = new Vector2(width, height);
    }

    private static void Stretch(RectTransform rect, float x = 0, float y = 0)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(x, y);
        rect.offsetMax = new Vector2(-x, -y);
    }

    private static void DisposeObject(GameObject obj)
    {
        if (Application.isPlaying)
            Destroy(obj);
        else
            DestroyImmediate(obj);
    }
}
