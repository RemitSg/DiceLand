using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MenuAction = MainMenuSceneController.MenuAction;

/// <summary>Forest title screen. All progression and scene changes belong to the existing controller.</summary>
[DisallowMultipleComponent]
[ExecuteAlways]
public sealed class MainMenuForestView : MonoBehaviour
{
    private static readonly Color Ivory = new Color(0.97f, 0.92f, 0.77f);
    private static readonly Color Gold = new Color(0.78f, 0.61f, 0.33f);
    private static readonly Color Ink = new Color(0.21f, 0.12f, 0.045f);
    private readonly Dictionary<MenuAction, Button> buttons = new Dictionary<MenuAction, Button>();
    private readonly List<Button> difficultyButtons = new List<Button>();
    private readonly List<DifficultyConfig_SO> difficulties = new List<DifficultyConfig_SO>();
    private readonly List<MenuEntryVisual> primaryEntries = new List<MenuEntryVisual>();
    private Canvas canvas;
    private CanvasGroup inputGroup;
    private RectTransform safeRoot, composition, background;
    private GameObject menuPage, difficultyPage;
    private Text continueStatus, saveWarning;
    private Button startButton;
    private Font menuFont;
    private Sprite parchmentFrame;
    private Action<MenuAction> onAction;
    private Action<DifficultyConfig_SO> onStart;
    private DifficultyConfig_SO selectedDifficulty;
    private bool canContinue, inputEnabled = true, launching;
    private Vector2 lastViewport;
    private Rect lastSafeArea;
    private RectTransform selectionFrame;
    private CanvasGroup menuGroup, difficultyGroup;
    private MenuAction highlightedAction = MenuAction.NewGame;
    private MenuAction pressedAction;
    private Vector2 highlightVelocity;
    private float pressScale = 1f, pressVelocity;
    private float difficultyProgress, difficultyVelocity, externalProgress, externalVelocity;
    private bool wantsDifficulty, externalPageVisible;

    private sealed class MenuEntryVisual
    {
        public MenuAction action;
        public RectTransform rect;
        public Text label;
        public MainMenuOrnament icon;
        public Shadow shadow;
        public Vector2 labelPosition, iconPosition;
    }

    public Canvas Canvas => canvas;
    public bool IsDifficultyVisible => wantsDifficulty;
    public bool IsPageTransitioning => difficultyProgress != (wantsDifficulty ? 1f : 0f);

    public void Build(Font font, IDictionary<MenuAction, string> labels,
        IList<DifficultyConfig_SO> configurations, Action<MenuAction> action, Action<DifficultyConfig_SO> start)
    {
        if (canvas != null)
            return;
        menuFont = font != null ? font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        Sprite[] frames = Resources.LoadAll<Sprite>("MainMenu/GildedButton");
        parchmentFrame = frames.Length > 0 ? frames[0] : null;
        onAction = action;
        onStart = start;
        if (configurations != null)
            foreach (DifficultyConfig_SO config in configurations)
                if (config != null && !difficulties.Contains(config)) difficulties.Add(config);

        GameObject root = new GameObject("MainMenuForestCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, gameObject.scene);
        canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(2335f, 1080f);
        scaler.matchWidthOrHeight = 1f;
        Image blocker = root.AddComponent<Image>();
        blocker.color = new Color(0.012f, 0.025f, 0.03f);

        background = Rect("ForestJourney", root.transform, Vector2.zero, Vector2.zero);
        RawImage art = background.gameObject.AddComponent<RawImage>();
        art.texture = Resources.Load<Texture2D>("MainMenu/ForestJourney");
        art.raycastTarget = false;
        background.anchorMin = background.anchorMax = new Vector2(0f, 0.5f);
        background.pivot = new Vector2(0f, 0.5f);

        RectTransform shade = Rect("MenuShade", root.transform, Vector2.zero, Vector2.zero);
        Stretch(shade);
        MainMenuOrnament shadeGraphic = shade.gameObject.AddComponent<MainMenuOrnament>();
        shadeGraphic.kind = MainMenuOrnament.Shape.Shade;
        shadeGraphic.raycastTarget = false;
        RectTransform motes = Rect("ForestMotes", root.transform, Vector2.zero, Vector2.zero);
        Stretch(motes);
        MainMenuOrnament particles = motes.gameObject.AddComponent<MainMenuOrnament>();
        particles.kind = MainMenuOrnament.Shape.Motes;
        particles.raycastTarget = false;

        safeRoot = Rect("SafeArea", root.transform, Vector2.zero, Vector2.zero);
        Stretch(safeRoot);
        inputGroup = safeRoot.gameObject.AddComponent<CanvasGroup>();
        composition = Rect("Composition", safeRoot, Vector2.zero, new Vector2(900f, 1080f));
        composition.anchorMin = composition.anchorMax = new Vector2(0f, 0.5f);
        composition.pivot = new Vector2(0f, 0.5f);

        RawImage logo = Rect("DiceLandLogo", composition, new Vector2(48f, 32f), new Vector2(684f, 254f)).gameObject.AddComponent<RawImage>();
        logo.texture = Resources.Load<Texture2D>("MainMenu/DiceLandLogo");
        logo.raycastTarget = false;

        menuPage = Rect("Menu", composition, Vector2.zero, new Vector2(760f, 1080f)).gameObject;
        menuGroup = menuPage.AddComponent<CanvasGroup>();
        selectionFrame = Rect("SelectionFrame", menuPage.transform, new Vector2(84f, 342f), new Vector2(636f, 108f));
        Graphic selectionGraphic;
        if (parchmentFrame != null)
        {
            Image frame = selectionFrame.gameObject.AddComponent<Image>();
            frame.sprite = parchmentFrame;
            frame.type = Image.Type.Sliced;
            frame.pixelsPerUnitMultiplier = 3f;
            selectionGraphic = frame;
        }
        else
        {
            MainMenuOrnament frame = selectionFrame.gameObject.AddComponent<MainMenuOrnament>();
            frame.kind = MainMenuOrnament.Shape.Parchment;
            selectionGraphic = frame;
        }
        selectionGraphic.raycastTarget = false;
        MenuAction[] primary = { MenuAction.NewGame, MenuAction.Continue, MenuAction.Ability, MenuAction.Tutorial, MenuAction.Settings };
        MainMenuOrnament.Shape[] icons = { MainMenuOrnament.Shape.Diamond, MainMenuOrnament.Shape.Book,
            MainMenuOrnament.Shape.Ability, MainMenuOrnament.Shape.Book, MainMenuOrnament.Shape.Settings };
        for (int i = 0; i < primary.Length; i++)
        {
            MenuAction id = primary[i];
            Button button = MenuButton(id.ToString(), menuPage.transform, new Vector2(84f, 342f + i * 112f),
                new Vector2(636f, 108f), Label(labels, id), icons[i], false, 58);
            button.transition = Selectable.Transition.None;
            button.targetGraphic.color = Color.clear;
            Text label = button.transform.Find("Label").GetComponent<Text>();
            label.fontStyle = FontStyle.Bold;
            MainMenuOrnament icon = button.transform.Find("Icon").GetComponent<MainMenuOrnament>();
            primaryEntries.Add(new MenuEntryVisual { action = id, rect = (RectTransform)button.transform,
                label = label, icon = icon, shadow = label.GetComponent<Shadow>(),
                labelPosition = label.rectTransform.anchoredPosition, iconPosition = icon.rectTransform.anchoredPosition });
            MainMenuHoverTarget hover = button.gameObject.AddComponent<MainMenuHoverTarget>();
            hover.owner = this;
            hover.action = id;
            buttons[id] = button;
            button.onClick.AddListener(() => Dispatch(id));
        }
        continueStatus = Text("SaveStatus", buttons[MenuAction.Continue].transform, new Vector2(408f, 16f), new Vector2(210f, 76f),
            "暂无存档", 28, new Color(0.64f, 0.64f, 0.58f));
        buttons[MenuAction.Continue].transform.Find("Label").GetComponent<RectTransform>().sizeDelta = new Vector2(274f, 90f);

        AddUtility(MenuAction.Contributors, new Vector2(88f, 960f), new Vector2(310f, 72f), MainMenuOrnament.Shape.People, labels);
        Text("UtilityDivider", menuPage.transform, new Vector2(406f, 970f), new Vector2(24f, 50f), "|", 30, Gold, TextAnchor.MiddleCenter);
        AddUtility(MenuAction.Exit, new Vector2(468f, 960f), new Vector2(218f, 72f), MainMenuOrnament.Shape.Exit, labels);

        Button demo = MenuButton("DemoTest", safeRoot, Vector2.zero, new Vector2(250f, 68f), Label(labels, MenuAction.DemoTest),
            MainMenuOrnament.Shape.Swords, false, 30, false);
        RectTransform demoRect = (RectTransform)demo.transform;
        demoRect.anchorMin = demoRect.anchorMax = new Vector2(1f, 0f);
        demoRect.pivot = new Vector2(1f, 0f);
        demoRect.anchoredPosition = new Vector2(-44f, 24f);
        buttons[MenuAction.DemoTest] = demo;
        demo.onClick.AddListener(() => Dispatch(MenuAction.DemoTest));

        BuildDifficultyPage();
        ApplyPagePose();
        RefreshHighlight();
        SetContinueAvailable(false);
        ApplyLayout(new Vector2(Screen.width, Screen.height), Screen.safeArea);
    }

    public Button GetButton(MenuAction action) => buttons.TryGetValue(action, out Button button) ? button : null;

    public void RefreshLanguage(Func<MenuAction, string> label)
    {
        foreach (var entry in buttons)
            entry.Value.transform.Find("Label").GetComponent<Text>().text = label(entry.Key);

        Transform page = difficultyPage.transform;
        page.Find("Title").GetComponent<Text>().text = GameText.Get("ui.difficulty.title", "选择难度");
        page.Find("StartAdventure/Label").GetComponent<Text>().text = GameText.Get("ui.difficulty.start", "开始冒险");
        page.Find("Back/Label").GetComponent<Text>().text = GameText.Get("ui.back", "返回");
        Transform missing = page.Find("MissingConfiguration");
        if (missing != null) missing.GetComponent<Text>().text = GameText.Get("ui.difficulty.missing", "暂无可用难度");
        for (int i = 0; i < difficultyButtons.Count; i++)
        {
            Transform row = difficultyButtons[i].transform;
            row.Find("Label").GetComponent<Text>().text = DifficultyLabel(difficulties[i]);
            row.Find("Details").GetComponent<Text>().text = DifficultyDetails(difficulties[i]);
        }
        SetContinueAvailable(canContinue);
    }

    public void SetContinueAvailable(bool available)
    {
        canContinue = available;
        Button button = GetButton(MenuAction.Continue);
        if (button == null) return;
        button.interactable = available;
        if (!available && highlightedAction == MenuAction.Continue) highlightedAction = MenuAction.NewGame;
        RefreshHighlight();
        continueStatus.text = available ? string.Empty : GameText.Get("ui.menu.no_save", "暂无存档");
        if (saveWarning != null)
            saveWarning.text = available ? GameText.Get("ui.menu.overwrite_save", "开始新游戏将覆盖当前冒险存档") : string.Empty;
    }

    public void SetInputEnabled(bool value)
    {
        inputEnabled = value;
        if (inputGroup == null) return;
        inputGroup.interactable = value && !launching && !externalPageVisible;
        inputGroup.blocksRaycasts = value && !launching && !externalPageVisible;
        if (!value) pressedAction = MenuAction.None;
    }

    public void Hover(MenuAction action)
    {
        if (!CanUseMenu() || (action == MenuAction.Continue && !canContinue)) return;
        foreach (MenuEntryVisual entry in primaryEntries)
            if (entry.action == action) { highlightedAction = action; return; }
    }

    public void LeaveHover(MenuAction action)
    {
        // Keep the last focus while crossing the gaps between rows.
        if (pressedAction == action) pressedAction = MenuAction.None;
    }

    public void SetPressed(MenuAction action, bool pressed)
    {
        if (pressed && CanUseMenu()) { Hover(action); pressedAction = action; }
        else if (pressedAction == action) pressedAction = MenuAction.None;
    }

    public void SetExternalPageVisible(bool visible)
    {
        if (externalPageVisible == visible) return;
        externalPageVisible = visible;
        pressedAction = MenuAction.None;
        SetInputEnabled(inputEnabled);
        if (!Application.isPlaying)
        {
            externalProgress = visible ? 1f : 0f;
            ApplyExternalPose();
        }
    }

    public void ShowDifficulty()
    {
        if (!CanUseMenu()) return;
        wantsDifficulty = true;
        pressedAction = MenuAction.None;
        GetButton(MenuAction.DemoTest).gameObject.SetActive(false);
        difficultyPage.SetActive(true);
        SelectDifficulty(difficulties.Count > 0 ? difficulties[0] : null);
        if (!Application.isPlaying) difficultyProgress = 1f;
        ApplyPagePose();
    }

    public void HideDifficulty()
    {
        if (launching || difficultyPage == null) return;
        wantsDifficulty = false;
        menuPage.SetActive(true);
        if (!Application.isPlaying) difficultyProgress = 0f;
        ApplyPagePose();
    }

    public void ApplyLayout(Vector2 viewport, Rect safePixels)
    {
        if (safeRoot == null || viewport.x <= 0f || viewport.y <= 0f) return;
        if (safePixels.width <= 0f || safePixels.height <= 0f) safePixels = new Rect(Vector2.zero, viewport);
        lastViewport = viewport;
        lastSafeArea = safePixels;
        safeRoot.anchorMin = new Vector2(safePixels.xMin / viewport.x, safePixels.yMin / viewport.y);
        safeRoot.anchorMax = new Vector2(safePixels.xMax / viewport.x, safePixels.yMax / viewport.y);
        safeRoot.offsetMin = safeRoot.offsetMax = Vector2.zero;
        float safeWidth = 1080f * safePixels.width / viewport.y;
        float safeHeight = 1080f * safePixels.height / viewport.y;
        float scale = Mathf.Min(1f, safeWidth / 1760f, safeHeight / 1080f);
        composition.localScale = Vector3.one * scale;
        ApplyExternalPose();
        RawImage art = background.GetComponent<RawImage>();
        float aspect = art.texture != null ? (float)art.texture.width / art.texture.height : 2.162f;
        float width = Mathf.Max(1080f * viewport.x / viewport.y, 1080f * aspect);
        background.sizeDelta = new Vector2(width, width / aspect);
        background.anchoredPosition = Vector2.zero;
    }

    private void LateUpdate()
    {
        if (!Application.isPlaying) return;
        Vector2 viewport = new Vector2(Screen.width, Screen.height);
        if (canvas != null && (viewport != lastViewport || Screen.safeArea != lastSafeArea))
            ApplyLayout(viewport, Screen.safeArea);
        TickMotion(Time.unscaledDeltaTime);
    }

    private bool CanUseMenu() => inputEnabled && !launching && !externalPageVisible && !wantsDifficulty && !IsPageTransitioning;

    private bool CanUseDifficulty() => inputEnabled && !launching && !externalPageVisible && wantsDifficulty && !IsPageTransitioning;

    private void TickMotion(float deltaTime)
    {
        if (canvas == null || deltaTime <= 0f) return;
        float dt = Mathf.Min(deltaTime, 0.05f);
        foreach (MenuEntryVisual entry in primaryEntries)
            if (entry.action == highlightedAction)
            {
                // Retain velocity when the pointer changes direction mid-flight.
                selectionFrame.anchoredPosition = Vector2.SmoothDamp(selectionFrame.anchoredPosition,
                    entry.rect.anchoredPosition, ref highlightVelocity, 0.075f, Mathf.Infinity, dt);
                if ((selectionFrame.anchoredPosition - entry.rect.anchoredPosition).sqrMagnitude < 0.04f)
                { selectionFrame.anchoredPosition = entry.rect.anchoredPosition; highlightVelocity = Vector2.zero; }
                break;
            }
        pressScale = Mathf.SmoothDamp(pressScale, pressedAction == highlightedAction ? 0.985f : 1f,
            ref pressVelocity, 0.045f, Mathf.Infinity, dt);
        selectionFrame.localScale = new Vector3(pressScale, pressScale, 1f);
        RefreshHighlight();
        float target = wantsDifficulty ? 1f : 0f;
        difficultyProgress = Mathf.SmoothDamp(difficultyProgress, target, ref difficultyVelocity, 0.07f, Mathf.Infinity, dt);
        if (Mathf.Abs(difficultyProgress - target) < 0.001f) { difficultyProgress = target; difficultyVelocity = 0f; }
        ApplyPagePose();
        target = externalPageVisible ? 1f : 0f;
        externalProgress = Mathf.SmoothDamp(externalProgress, target, ref externalVelocity, 0.075f, Mathf.Infinity, dt);
        if (Mathf.Abs(externalProgress - target) < 0.001f) { externalProgress = target; externalVelocity = 0f; }
        ApplyExternalPose();
    }

    private void RefreshHighlight()
    {
        if (selectionFrame == null) return;
        foreach (MenuEntryVisual entry in primaryEntries)
        {
            bool available = entry.action != MenuAction.Continue || canContinue;
            float coverage = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(1f -
                Mathf.Abs(selectionFrame.anchoredPosition.y - entry.rect.anchoredPosition.y) / 72f));
            entry.label.color = available ? Color.Lerp(Ivory, Ink, coverage) : new Color(0.57f, 0.58f, 0.55f);
            entry.icon.color = available ? Color.Lerp(Gold, new Color(0.52f, 0.31f, 0.10f), coverage) : new Color(0.45f, 0.43f, 0.35f);
            entry.label.rectTransform.anchoredPosition = entry.labelPosition + Vector2.right * (6f * coverage);
            entry.icon.rectTransform.anchoredPosition = entry.iconPosition + Vector2.right * (4f * coverage);
            if (entry.shadow != null) entry.shadow.effectColor = new Color(0f, 0.015f, 0.018f, 0.8f * (1f - coverage));
        }
    }

    private void ApplyPagePose()
    {
        if (menuGroup == null || difficultyGroup == null) return;
        float blend = Mathf.SmoothStep(0f, 1f, difficultyProgress);
        menuGroup.alpha = 1f - blend;
        difficultyGroup.alpha = blend;
        ((RectTransform)menuPage.transform).anchoredPosition = Vector2.left * (32f * blend);
        ((RectTransform)difficultyPage.transform).anchoredPosition = Vector2.right * (44f * (1f - blend));
        menuPage.SetActive(difficultyProgress < 1f || !wantsDifficulty);
        difficultyPage.SetActive(difficultyProgress > 0f || wantsDifficulty);
        menuGroup.interactable = menuGroup.blocksRaycasts = !wantsDifficulty && !IsPageTransitioning;
        difficultyGroup.interactable = difficultyGroup.blocksRaycasts = wantsDifficulty && !IsPageTransitioning;
        GetButton(MenuAction.DemoTest).gameObject.SetActive(!wantsDifficulty && !IsPageTransitioning);
    }

    private void ApplyExternalPose()
    {
        if (inputGroup == null || composition == null) return;
        inputGroup.alpha = 1f - externalProgress;
        composition.anchoredPosition = Vector2.left * (32f * externalProgress);
    }

    private void Dispatch(MenuAction action)
    {
        if (!CanUseMenu() || (action == MenuAction.Continue && !canContinue)) return;
        Hover(action);
        pressedAction = MenuAction.None;
        onAction?.Invoke(action);
    }

    private void BuildDifficultyPage()
    {
        difficultyPage = Rect("Difficulty", composition, Vector2.zero, new Vector2(900f, 1080f)).gameObject;
        difficultyGroup = difficultyPage.AddComponent<CanvasGroup>();
        Text("Title", difficultyPage.transform, new Vector2(90f, 321f), new Vector2(710f, 92f), GameText.Get("ui.difficulty.title", "选择难度"), 60, Ivory);
        Ornament("TitleRule", difficultyPage.transform, new Vector2(84f, 416f), new Vector2(700f, 12f), MainMenuOrnament.Shape.Divider, Gold);
        float rowHeight = Mathf.Min(98f, 396f / Mathf.Max(1, difficulties.Count));
        for (int i = 0; i < difficulties.Count; i++)
        {
            DifficultyConfig_SO config = difficulties[i];
            Button row = MenuButton("Difficulty_" + i, difficultyPage.transform, new Vector2(84f, 443f + i * rowHeight),
                new Vector2(700f, rowHeight - 6f), DifficultyLabel(config), MainMenuOrnament.Shape.Diamond, false, 42);
            row.transform.Find("Label").GetComponent<RectTransform>().sizeDelta = new Vector2(256f, rowHeight - 10f);
            Text("Details", row.transform, new Vector2(342f, 8f), new Vector2(344f, rowHeight - 18f),
                DifficultyDetails(config), 27, Ivory);
            row.onClick.AddListener(() => { if (CanUseDifficulty()) SelectDifficulty(config); });
            difficultyButtons.Add(row);
        }
        if (difficulties.Count == 0)
            Text("MissingConfiguration", difficultyPage.transform, new Vector2(90f, 520f), new Vector2(700f, 110f), GameText.Get("ui.difficulty.missing", "暂无可用难度"), 38, Ivory);
        saveWarning = Text("SaveWarning", difficultyPage.transform, new Vector2(96f, 854f), new Vector2(710f, 48f), string.Empty, 27, Gold);
        startButton = MenuButton("StartAdventure", difficultyPage.transform, new Vector2(84f, 918f), new Vector2(430f, 106f),
            GameText.Get("ui.difficulty.start", "开始冒险"), MainMenuOrnament.Shape.Diamond, true, 48);
        startButton.onClick.AddListener(() =>
        {
            if (!CanUseDifficulty() || selectedDifficulty == null) return;
            launching = true;
            SetInputEnabled(false);
            onStart?.Invoke(selectedDifficulty);
        });
        Button back = MenuButton("Back", difficultyPage.transform, new Vector2(540f, 928f), new Vector2(245f, 86f),
            GameText.Get("ui.back", "返回"), MainMenuOrnament.Shape.Exit, false, 38, false);
        back.onClick.AddListener(() => { if (inputEnabled) HideDifficulty(); });
        difficultyPage.SetActive(false);
    }

    private void SelectDifficulty(DifficultyConfig_SO config)
    {
        selectedDifficulty = config;
        startButton.interactable = config != null;
        for (int i = 0; i < difficultyButtons.Count; i++)
        {
            bool selected = difficulties[i] == config;
            ColorBlock colors = difficultyButtons[i].colors;
            colors.normalColor = selected ? new Color(0.78f, 0.59f, 0.28f, 0.22f) : Color.clear;
            difficultyButtons[i].colors = colors;
            difficultyButtons[i].transform.Find("Icon").GetComponent<MainMenuOrnament>().color = selected ? new Color(1f, 0.82f, 0.40f) : new Color(0.55f, 0.46f, 0.3f);
        }
    }

    private static string DifficultyLabel(DifficultyConfig_SO config)
    {
        switch ((config.difficultyName ?? string.Empty).ToLowerInvariant())
        {
            case "easy": return GameText.Get("ui.difficulty.easy", "简单");
            case "normal": case "medium": return GameText.Get("ui.difficulty.normal", "普通");
            case "hard": return GameText.Get("ui.difficulty.hard", "困难");
            case "extreme": return GameText.Get("ui.difficulty.extreme", "极难");
            default: return config.difficultyName;
        }
    }

    private static string DifficultyDetails(DifficultyConfig_SO config)
    {
        return GameText.Format("ui.difficulty.details", "{size}×{size} 棋盘 · {points} 行动点",
            ("size", config.BoardSize), ("points", config.startingActionPoints));
    }

    private void AddUtility(MenuAction id, Vector2 position, Vector2 size, MainMenuOrnament.Shape icon, IDictionary<MenuAction, string> labels)
    {
        Button button = MenuButton(id.ToString(), menuPage.transform, position, size, Label(labels, id), icon, false, 36, false);
        buttons[id] = button;
        button.onClick.AddListener(() => Dispatch(id));
    }

    private Button MenuButton(string name, Transform parent, Vector2 position, Vector2 size, string label,
        MainMenuOrnament.Shape icon, bool primary, int fontSize, bool divider = true)
    {
        RectTransform rect = Rect(name, parent, position, size);
        Graphic target;
        if (primary && parchmentFrame != null)
        {
            Image frame = rect.gameObject.AddComponent<Image>();
            frame.sprite = parchmentFrame;
            frame.type = Image.Type.Sliced;
            frame.pixelsPerUnitMultiplier = 3f;
            target = frame;
        }
        else if (primary)
        {
            MainMenuOrnament frame = rect.gameObject.AddComponent<MainMenuOrnament>();
            frame.kind = MainMenuOrnament.Shape.Parchment;
            target = frame;
        }
        else
        {
            Image fill = rect.gameObject.AddComponent<Image>();
            fill.color = Color.white;
            target = fill;
        }
        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = target;
        ColorBlock colors = button.colors;
        colors.normalColor = primary ? Color.white : Color.clear;
        colors.highlightedColor = primary ? new Color(1f, 0.96f, 0.84f) : new Color(0.82f, 0.66f, 0.34f, 0.16f);
        colors.pressedColor = primary ? new Color(0.83f, 0.73f, 0.53f) : new Color(0.82f, 0.66f, 0.34f, 0.28f);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = primary ? new Color(0.5f, 0.5f, 0.5f) : Color.clear;
        colors.fadeDuration = 0.14f;
        button.colors = colors;
        // Pointer/touch navigation avoids lingering selection opening a second panel on Enter.
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        bool large = size.y >= 90f;
        float iconSize = large ? 54f : 36f;
        float iconX = large ? 40f : 9f;
        Ornament("Icon", rect, new Vector2(iconX, (size.y - iconSize) * 0.5f), new Vector2(iconSize, iconSize), icon, primary ? new Color(0.52f, 0.31f, 0.10f) : Gold);
        float labelX = large ? 126f : 63f;
        Text("Label", rect, new Vector2(labelX, 8f), new Vector2(size.x - labelX - 22f, size.y - 16f), label, fontSize, primary ? Ink : Ivory);
        if (!primary && divider)
            Ornament("Divider", rect, new Vector2(0f, size.y - 7f), new Vector2(size.x, 12f), MainMenuOrnament.Shape.Divider, new Color(Gold.r, Gold.g, Gold.b, 0.82f));
        return button;
    }

    private Text Text(string name, Transform parent, Vector2 position, Vector2 size, string value, int fontSize, Color tint, TextAnchor align = TextAnchor.MiddleLeft)
    {
        Text text = Rect(name, parent, position, size).gameObject.AddComponent<ResourceKeywordLegacyText>();
        text.font = menuFont;
        text.fontSize = fontSize;
        text.text = value;
        text.alignment = align;
        text.color = tint;
        text.raycastTarget = false;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        if (tint != Ink)
        {
            Shadow shadow = text.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0.015f, 0.018f, 0.8f);
            shadow.effectDistance = new Vector2(1f, -2f);
        }
        return text;
    }

    private static void Ornament(string name, Transform parent, Vector2 position, Vector2 size, MainMenuOrnament.Shape shape, Color tint)
    {
        RectTransform rect = Rect(name, parent, position, size);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(position.x + size.x * 0.5f, -position.y - size.y * 0.5f);
        MainMenuOrnament graphic = rect.gameObject.AddComponent<MainMenuOrnament>();
        graphic.kind = shape;
        graphic.color = tint;
        graphic.raycastTarget = false;
    }

    private static string Label(IDictionary<MenuAction, string> labels, MenuAction id) => labels != null && labels.TryGetValue(id, out string text) ? text : id.ToString();

    private static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
    {
        RectTransform rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(position.x, -position.y);
        rect.sizeDelta = size;
        return rect;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    private void OnDestroy()
    {
        if (canvas == null) return;
        if (Application.isPlaying) Destroy(canvas.gameObject);
        else DestroyImmediate(canvas.gameObject);
    }
}
