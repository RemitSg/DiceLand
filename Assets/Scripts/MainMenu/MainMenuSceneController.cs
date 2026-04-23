using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DefaultExecutionOrder(-1000)]
[DisallowMultipleComponent]
public class MainMenuSceneController : MonoBehaviour
{
    [Serializable]
    private sealed class DemoTestPartyMemberDefinition
    {
        public string memberId = "DemoMember";
        public CharacterData_SO characterData;
        public Weapon_SO equippedWeapon;
        [Min(1)] public int startingLevel = 1;
        [Min(0)] public int startingExperience;
        [Min(0)] public int startingMaxHP;
        [Min(0)] public int startingCurrentHP;
    }

    private enum MenuAction
    {
        None = 0,
        NewGame = 1,
        Continue = 2,
        Tutorial = 3,
        Contributors = 4,
        Settings = 5,
        Exit = 6,
        DemoTest = 7
    }

    [Header("Difficulty Configs")]
    [SerializeField] private DifficultyConfig_SO[] difficultyConfigs = Array.Empty<DifficultyConfig_SO>();

    [Header("Scene Names")]
    [SerializeField] private string mapSceneName = "MapScene";
    [SerializeField] private string demoTestSceneName = "SampleScene";

    [Header("Labels")]
    [SerializeField] private string newGameLabel = "NewGame";
    [SerializeField] private string continueLabel = "Continue";
    [SerializeField] private string tutorialLabel = "Tutorial";
    [SerializeField] private string contributorsLabel = "Contributors";
    [SerializeField] private string settingsLabel = "Settings";
    [SerializeField] private string exitLabel = "Exit";
    [SerializeField] private string demoTestLabel = "DemoTest";
    [SerializeField] private Color disabledContinueColor = new Color(0.55f, 0.55f, 0.55f, 0.9f);
    [SerializeField] private string noSaveSuffix = " (No Save)";

    [Header("Tutorial")]
    [SerializeField] private TutorialMenuCatalog_SO tutorialCatalog;
    [SerializeField] private string tutorialCatalogResourcesPath = "TutorialMenuCatalog";

    [Header("Contributors")]
    [SerializeField] private MainMenuContributorsOverlay.DisplayConfig contributorsConfig = new MainMenuContributorsOverlay.DisplayConfig();

    [Header("DemoTest")]
    [SerializeField] private string demoTestEncounterLabel = "Demo Battle Test";
    [SerializeField] private EnemyGroup_SO demoTestEnemyGroup;
    [SerializeField] private List<DemoTestPartyMemberDefinition> demoTestPartyMembers = new List<DemoTestPartyMemberDefinition>();

    [Header("Fonts")]
    [SerializeField] private Font settingsTitleFont;
    [SerializeField] private Font settingsBodyFont;
    [SerializeField] private Font settingsLabelFont;
    [SerializeField] private Font settingsValueFont;
    [SerializeField] private Font settingsToggleStateFont;
    [SerializeField] private Font settingsDropdownFont;
    [SerializeField] private Font settingsButtonFont;
    [SerializeField] private Font contributorsTitleFont;
    [SerializeField] private Font contributorsBodyFont;

    private GalleryLevelSelectionManager mainMenuGallery;
    private GalleryLevelSelectionManager difficultyGallery;
    private MainMenuTutorialOverlay tutorialOverlay;
    private MainMenuContributorsOverlay contributorsOverlay;
    private MainMenuSettingsOverlay settingsOverlay;
    private readonly Dictionary<GalleryLevelView, MenuAction> menuActions = new Dictionary<GalleryLevelView, MenuAction>();
    private readonly Dictionary<GalleryLevelView, DifficultyConfig_SO> difficultyLookup = new Dictionary<GalleryLevelView, DifficultyConfig_SO>();
    private int lastCenteredMainMenuIndex = -1;
    private MenuAction lastCenteredMainMenuAction = MenuAction.None;
    private bool suppressSettingsAutoShowUntilFocusLeaves;
    private bool galleryInputEnabled = true;
    private bool wasSettingsOverlayVisible;

    private void Awake()
    {
        if (!IsMainMenuScene())
            return;

        mainMenuGallery = GetComponent<GalleryLevelSelectionManager>();
        if (mainMenuGallery == null)
        {
            Debug.LogWarning("[MainMenuSceneController] Missing GalleryLevelSelectionManager on main menu root.");
            enabled = false;
            return;
        }

        contributorsOverlay = GetComponent<MainMenuContributorsOverlay>();
        if (contributorsOverlay == null)
            contributorsOverlay = gameObject.AddComponent<MainMenuContributorsOverlay>();

        tutorialOverlay = GetComponent<MainMenuTutorialOverlay>();
        if (tutorialOverlay == null)
            tutorialOverlay = gameObject.AddComponent<MainMenuTutorialOverlay>();

        settingsOverlay = GetComponent<MainMenuSettingsOverlay>();
        if (settingsOverlay == null)
            settingsOverlay = gameObject.AddComponent<MainMenuSettingsOverlay>();

        contributorsOverlay.ConfigureFonts(contributorsTitleFont, contributorsBodyFont);
        settingsOverlay.ConfigureFonts(
            settingsTitleFont,
            settingsBodyFont,
            settingsLabelFont,
            settingsValueFont,
            settingsToggleStateFont,
            settingsDropdownFont,
            settingsButtonFont);

        ApplyMainMenuSettings();
        ConfigureMainMenuItems();
        ConfigureDifficultyMenu();
    }

    private void Start()
    {
        if (!enabled || mainMenuGallery == null)
            return;

        mainMenuGallery.OnItemsClickedEvent += HandleMainMenuItemClicked;
        if (difficultyGallery != null)
            difficultyGallery.OnItemsClickedEvent += HandleDifficultyItemClicked;

        RefreshContinueState();
    }

    private void OnDestroy()
    {
        if (mainMenuGallery != null)
            mainMenuGallery.OnItemsClickedEvent -= HandleMainMenuItemClicked;
        if (difficultyGallery != null)
            difficultyGallery.OnItemsClickedEvent -= HandleDifficultyItemClicked;
    }

    private void Update()
    {
        if (!enabled)
            return;

        bool tutorialVisibleAtStart = tutorialOverlay != null && tutorialOverlay.IsVisible;
        bool contributorsVisibleAtStart = contributorsOverlay != null && contributorsOverlay.IsVisible;
        bool settingsVisibleAtStart = settingsOverlay != null && settingsOverlay.IsVisible;

        SetGalleryInputEnabled(!(tutorialVisibleAtStart || contributorsVisibleAtStart || settingsVisibleAtStart));

        if (!tutorialVisibleAtStart && !contributorsVisibleAtStart && !settingsVisibleAtStart)
            HandleFocusedMenuOverlayState();

        if (!Input.GetKeyDown(KeyCode.Escape))
        {
            TrackOverlayVisibilityState();
            return;
        }

        if (tutorialVisibleAtStart)
        {
            tutorialOverlay.Hide();
            TrackOverlayVisibilityState();
            return;
        }

        if (contributorsVisibleAtStart)
        {
            contributorsOverlay.Hide();
            TrackOverlayVisibilityState();
            return;
        }

        if (settingsVisibleAtStart)
        {
            settingsOverlay.Hide();
            if (lastCenteredMainMenuAction == MenuAction.Settings)
                suppressSettingsAutoShowUntilFocusLeaves = true;
            TrackOverlayVisibilityState();
            return;
        }

        TrackOverlayVisibilityState();
    }

    private bool IsMainMenuScene()
    {
        return SceneManager.GetActiveScene().name.Equals("Menu Selection", StringComparison.OrdinalIgnoreCase);
    }

    private void ApplyMainMenuSettings()
    {
        MainMenuSettingsData settings = MainMenuSettingsPersistence.Load();
        MainMenuSettingsPersistence.Apply(settings);
    }

    private void HandleFocusedMenuOverlayState()
    {
        if (mainMenuGallery == null)
            return;

        int centeredIndex = mainMenuGallery.GetCenterFixedItemIndex();
        if (centeredIndex < 0 || centeredIndex >= mainMenuGallery.items.Length)
            return;

        GalleryLevelView centeredItem = mainMenuGallery.items[centeredIndex];
        MenuAction centeredAction = MenuAction.None;
        if (centeredItem != null && menuActions.TryGetValue(centeredItem, out MenuAction resolvedAction))
            centeredAction = resolvedAction;

        if (centeredIndex == lastCenteredMainMenuIndex && centeredAction == lastCenteredMainMenuAction)
            return;

        MenuAction previousAction = lastCenteredMainMenuAction;
        lastCenteredMainMenuIndex = centeredIndex;
        lastCenteredMainMenuAction = centeredAction;

        if (lastCenteredMainMenuAction != MenuAction.Settings)
            suppressSettingsAutoShowUntilFocusLeaves = false;

        if (previousAction == MenuAction.Settings && lastCenteredMainMenuAction != MenuAction.Settings && settingsOverlay != null && settingsOverlay.IsVisible)
            settingsOverlay.Hide();

        if (lastCenteredMainMenuAction == MenuAction.Settings && !suppressSettingsAutoShowUntilFocusLeaves)
            ShowSettingsOverlay();
    }

    private void ConfigureMainMenuItems()
    {
        menuActions.Clear();
        List<GalleryLevelView> sourceItems = BuildMainMenuItemsWithTutorialEntry();
        List<GalleryLevelView> filteredItems = new List<GalleryLevelView>();
        GalleryLevelView newGameItem = null;

        for (int i = 0; i < sourceItems.Count; i++)
        {
            GalleryLevelView item = sourceItems[i];
            if (item == null)
                continue;

            string identity = GetMenuIdentity(item);
            MenuAction action = ResolveMenuAction(identity);
            if (action == MenuAction.None)
            {
                item.gameObject.SetActive(false);
                continue;
            }

            filteredItems.Add(item);
            menuActions[item] = action;
            item.levelName = GetDisplayLabel(action);
            if (item.text != null)
                item.text.text = item.levelName;

            DemoGalleryMenuItem demoItem = item as DemoGalleryMenuItem;
            if (action == MenuAction.NewGame)
            {
                newGameItem = item;
            }
            else if (demoItem != null)
            {
                if (demoItem.contentToShow != null)
                    demoItem.contentToShow.SetActive(false);
                demoItem.contentToShow = null;
                if (demoItem.contentMask != null)
                {
                    Color color = demoItem.contentMask.color;
                    color.a = 0f;
                    demoItem.contentMask.color = color;
                }
            }
        }

        mainMenuGallery.items = filteredItems.ToArray();

        if (newGameItem is DemoGalleryMenuItem newGameDemoItem && newGameDemoItem.contentToShow != null)
            difficultyGallery = newGameDemoItem.contentToShow.GetComponentInChildren<GalleryLevelSelectionManager>(true);
    }

    private void ConfigureDifficultyMenu()
    {
        difficultyLookup.Clear();
        if (difficultyGallery == null)
            return;

        List<DifficultyConfig_SO> validConfigs = BuildDifficultyList();
        if (validConfigs.Count == 0)
            return;

        List<GalleryLevelView> filteredItems = new List<GalleryLevelView>();
        int count = Mathf.Min(validConfigs.Count, difficultyGallery.items.Length);
        for (int i = 0; i < difficultyGallery.items.Length; i++)
        {
            GalleryLevelView item = difficultyGallery.items[i];
            if (item == null)
                continue;

            if (i >= count)
            {
                item.gameObject.SetActive(false);
                continue;
            }

            DifficultyConfig_SO config = validConfigs[i];
            filteredItems.Add(item);
            difficultyLookup[item] = config;
            item.gameObject.SetActive(true);
            item.levelName = config.difficultyName;
            if (item.text != null)
                item.text.text = config.difficultyName;

            if (item is DemoGalleryMenuItem demoItem)
            {
                if (demoItem.contentToShow != null)
                    demoItem.contentToShow.SetActive(false);
                demoItem.contentToShow = null;
                if (demoItem.contentMask != null)
                {
                    Color color = demoItem.contentMask.color;
                    color.a = 0f;
                    demoItem.contentMask.color = color;
                }
            }
        }

        difficultyGallery.items = filteredItems.ToArray();
    }

    private List<DifficultyConfig_SO> BuildDifficultyList()
    {
        List<DifficultyConfig_SO> validConfigs = new List<DifficultyConfig_SO>();
        for (int i = 0; i < difficultyConfigs.Length; i++)
        {
            DifficultyConfig_SO config = difficultyConfigs[i];
            if (config != null && !validConfigs.Contains(config))
                validConfigs.Add(config);
        }

        validConfigs.Sort(CompareDifficultyConfigOrder);
        return validConfigs;
    }

    private static int CompareDifficultyConfigOrder(DifficultyConfig_SO a, DifficultyConfig_SO b)
    {
        return GetDifficultySortKey(a).CompareTo(GetDifficultySortKey(b));
    }

    private static int GetDifficultySortKey(DifficultyConfig_SO config)
    {
        if (config == null)
            return int.MaxValue;

        string name = config.difficultyName ?? string.Empty;
        if (name.Equals("Easy", StringComparison.OrdinalIgnoreCase))
            return 0;
        if (name.Equals("Normal", StringComparison.OrdinalIgnoreCase))
            return 1;
        if (name.Equals("Hard", StringComparison.OrdinalIgnoreCase))
            return 2;
        if (name.Equals("Extreme", StringComparison.OrdinalIgnoreCase))
            return 3;

        return 10 + Mathf.Max(0, config.boardSize);
    }

    private void RefreshContinueState()
    {
        GalleryLevelView continueItem = FindItemByAction(MenuAction.Continue);
        if (continueItem == null)
            return;

        bool hasSave = MainMenuRunSaveSystem.HasSavedRun();
        string label = continueLabel;
        if (!hasSave)
            label += noSaveSuffix;

        if (continueItem.text != null)
            continueItem.text.text = label;

        continueItem.levelName = continueLabel;

        Collider2D collider = continueItem.GetComponent<Collider2D>();
        if (collider != null)
            collider.enabled = hasSave;

        if (continueItem.text != null)
            continueItem.text.color = hasSave ? Color.white : disabledContinueColor;
    }

    private void HandleMainMenuItemClicked(int index, bool isCenter)
    {
        if (!isCenter || index < 0 || index >= mainMenuGallery.items.Length)
            return;

        GalleryLevelView item = mainMenuGallery.items[index];
        if (item == null || !menuActions.TryGetValue(item, out MenuAction action))
            return;

        switch (action)
        {
            case MenuAction.NewGame:
                MainMenuRuntime.ClearContinueRequest();
                MainMenuRuntime.ClearTutorialRequest();
                break;

            case MenuAction.Continue:
                HandleContinueClicked();
                break;

            case MenuAction.Tutorial:
                ShowTutorialOverlay();
                break;

            case MenuAction.Contributors:
                if (settingsOverlay != null && settingsOverlay.IsVisible)
                    settingsOverlay.Hide();
                contributorsOverlay.Show(contributorsConfig);
                SetGalleryInputEnabled(false);
                break;

            case MenuAction.Settings:
                ShowSettingsOverlay();
                break;

            case MenuAction.Exit:
                HandleExitClicked();
                break;

            case MenuAction.DemoTest:
                HandleDemoTestClicked();
                break;
        }
    }

    private void HandleDifficultyItemClicked(int index, bool isCenter)
    {
        if (!isCenter || difficultyGallery == null || index < 0 || index >= difficultyGallery.items.Length)
            return;

        GalleryLevelView item = difficultyGallery.items[index];
        if (item == null || !difficultyLookup.TryGetValue(item, out DifficultyConfig_SO config) || config == null)
            return;

        StartNewGame(config);
    }

    private void HandleContinueClicked()
    {
        MainMenuRuntime.ClearTutorialRequest();

        if (!MainMenuRunSaveSystem.HasSavedRun())
        {
            RefreshContinueState();
            return;
        }

        if (MainMenuRunSaveSystem.TryReadMetadata(out MainMenuRunSaveMetadata metadata))
        {
            DifficultyConfig_SO matchingConfig = FindDifficultyByName(metadata != null ? metadata.difficultyName : string.Empty);
            if (matchingConfig != null)
                MainMenuRuntime.SetSelectedDifficulty(matchingConfig);
        }

        MainMenuRuntime.RequestContinueFromSave();
        LoadMapScene();
    }

    private void StartNewGame(DifficultyConfig_SO config)
    {
        MainMenuRuntime.ClearContinueRequest();
        MainMenuRuntime.ClearTutorialRequest();
        MainMenuRuntime.SetSelectedDifficulty(config);
        MainMenuRunSaveSystem.DeleteSavedRun();
        MapRunRuntime.Clear();
        BattleEncounterRuntime.Clear();
        RestSceneRuntime.Clear();
        LoadMapScene();
    }

    private void HandleDemoTestClicked()
    {
        if (demoTestEnemyGroup == null || !demoTestEnemyGroup.IsValid)
        {
            Debug.LogWarning("[MainMenuSceneController] DemoTest requires a valid EnemyGroup_SO.");
            return;
        }

        List<RunPartyMemberState> partyMembers = BuildDemoTestPartyMembers();
        if (partyMembers.Count == 0)
        {
            Debug.LogWarning("[MainMenuSceneController] DemoTest requires at least one configured ally.");
            return;
        }

        MainMenuRuntime.ClearContinueRequest();
        MainMenuRuntime.ClearTutorialRequest();
        MainMenuRuntime.ClearSelectedDifficulty();
        BattleEncounterRuntime.Clear();

        BattleEncounterRuntime.SetEncounter(new BattleEncounterData
        {
            sourceSceneName = SceneManager.GetActiveScene().name,
            targetSceneName = demoTestSceneName,
            encounterLabel = string.IsNullOrWhiteSpace(demoTestEncounterLabel) ? "Demo Battle Test" : demoTestEncounterLabel,
            tileType = MapTileType.Battle,
            dangerBand = MapDangerBand.Outer,
            sourceCoord = new Vector2Int(-1, -1),
            enemyGroup = demoTestEnemyGroup,
            partyMembers = partyMembers,
            relicInventory = new List<RunRelicState>(),
            activeCalamities = new List<RunCalamityState>(),
            seed = UnityEngine.Random.Range(int.MinValue, int.MaxValue),
            moneyGained = 0
        });

        if (SceneCurtainTransitionController.Instance != null)
            SceneCurtainTransitionController.Instance.TransitionToScene(demoTestSceneName);
        else
            SceneManager.LoadScene(demoTestSceneName);
    }

    private List<RunPartyMemberState> BuildDemoTestPartyMembers()
    {
        List<RunPartyMemberState> result = new List<RunPartyMemberState>();
        for (int i = 0; i < demoTestPartyMembers.Count; i++)
        {
            DemoTestPartyMemberDefinition definition = demoTestPartyMembers[i];
            if (definition == null || definition.characterData == null)
                continue;

            Weapon_SO resolvedWeapon = definition.equippedWeapon != null
                ? definition.equippedWeapon
                : definition.characterData.initialWeaponData;
            int resolvedMaxHp = definition.startingMaxHP > 0
                ? Mathf.Max(1, definition.startingMaxHP)
                : Mathf.Max(1, definition.characterData.baseHP);
            int resolvedCurrentHp = definition.startingCurrentHP > 0
                ? Mathf.Clamp(definition.startingCurrentHP, 0, resolvedMaxHp)
                : resolvedMaxHp;

            result.Add(new RunPartyMemberState
            {
                memberId = string.IsNullOrWhiteSpace(definition.memberId)
                    ? (string.IsNullOrWhiteSpace(definition.characterData.characterName) ? $"DemoMember{i + 1}" : definition.characterData.characterName)
                    : definition.memberId,
                characterData = definition.characterData,
                weaponData = resolvedWeapon,
                level = Mathf.Max(1, definition.startingLevel),
                experience = Mathf.Max(0, definition.startingExperience),
                maxHP = resolvedMaxHp,
                currentHP = resolvedCurrentHp
            });
        }

        return result;
    }

    private void LoadMapScene()
    {
        if (SceneCurtainTransitionController.Instance != null)
            SceneCurtainTransitionController.Instance.TransitionToScene(mapSceneName);
        else
            SceneManager.LoadScene(mapSceneName);
    }

    private void HandleSettingsSaved(MainMenuSettingsData data)
    {
        MainMenuSettingsPersistence.Save(data);
        MainMenuSettingsPersistence.Apply(data);
    }

    private void ShowSettingsOverlay()
    {
        if (settingsOverlay == null)
            return;

        if (settingsOverlay.IsVisible)
            return;

        suppressSettingsAutoShowUntilFocusLeaves = false;
        settingsOverlay.Show(MainMenuSettingsPersistence.Current, HandleSettingsSaved);
        SetGalleryInputEnabled(false);
    }

    private void ShowTutorialOverlay()
    {
        if (tutorialOverlay == null || tutorialOverlay.IsVisible)
            return;

        if (contributorsOverlay != null && contributorsOverlay.IsVisible)
            contributorsOverlay.Hide();
        if (settingsOverlay != null && settingsOverlay.IsVisible)
            settingsOverlay.Hide();

        tutorialOverlay.Show(ResolveTutorialCatalog(), StartTutorialFromMenu, TrackOverlayVisibilityState);
        SetGalleryInputEnabled(false);
    }

    private void StartTutorialFromMenu()
    {
        PrepareTutorialRun();
        LoadMapScene();
    }

    private void PrepareTutorialRun()
    {
        MainMenuRuntime.ClearContinueRequest();
        MainMenuRuntime.ClearSelectedDifficulty();
        MainMenuRuntime.RequestTutorialStart(SceneManager.GetActiveScene().name);
        MapRunRuntime.Clear();
        BattleEncounterRuntime.Clear();
        RestSceneRuntime.Clear();
    }

    private TutorialMenuCatalog_SO ResolveTutorialCatalog()
    {
        if (tutorialCatalog != null)
            return tutorialCatalog;

        if (string.IsNullOrWhiteSpace(tutorialCatalogResourcesPath))
            return null;

        tutorialCatalog = Resources.Load<TutorialMenuCatalog_SO>(tutorialCatalogResourcesPath);
        return tutorialCatalog;
    }

    private void TrackOverlayVisibilityState()
    {
        bool settingsVisible = settingsOverlay != null && settingsOverlay.IsVisible;
        if (wasSettingsOverlayVisible && !settingsVisible && lastCenteredMainMenuAction == MenuAction.Settings)
            suppressSettingsAutoShowUntilFocusLeaves = true;

        wasSettingsOverlayVisible = settingsVisible;
        SetGalleryInputEnabled(!IsBlockingOverlayVisible());
    }

    private bool IsBlockingOverlayVisible()
    {
        return (tutorialOverlay != null && tutorialOverlay.IsVisible)
            || (contributorsOverlay != null && contributorsOverlay.IsVisible)
            || (settingsOverlay != null && settingsOverlay.IsVisible);
    }

    private void SetGalleryInputEnabled(bool enabled)
    {
        if (galleryInputEnabled == enabled)
            return;

        galleryInputEnabled = enabled;

        if (mainMenuGallery != null)
            mainMenuGallery.enabled = enabled;

        if (difficultyGallery != null)
            difficultyGallery.enabled = enabled;
    }

    private static void HandleExitClicked()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private DifficultyConfig_SO FindDifficultyByName(string difficultyName)
    {
        if (string.IsNullOrWhiteSpace(difficultyName))
            return null;

        for (int i = 0; i < difficultyConfigs.Length; i++)
        {
            DifficultyConfig_SO config = difficultyConfigs[i];
            if (config != null && string.Equals(config.difficultyName, difficultyName, StringComparison.OrdinalIgnoreCase))
                return config;
        }

        return null;
    }

    private GalleryLevelView FindItemByAction(MenuAction action)
    {
        foreach (KeyValuePair<GalleryLevelView, MenuAction> pair in menuActions)
        {
            if (pair.Value == action)
                return pair.Key;
        }

        return null;
    }

    private static string GetMenuIdentity(GalleryLevelView item)
    {
        if (item == null)
            return string.Empty;

        if (!string.IsNullOrWhiteSpace(item.levelName))
            return item.levelName;

        if (!string.IsNullOrWhiteSpace(item.gameObject.name))
            return item.gameObject.name;

        return string.Empty;
    }

    private static MenuAction ResolveMenuAction(string value)
    {
        string normalized = (value ?? string.Empty).Trim().ToLowerInvariant();
        switch (normalized)
        {
            case "solo play":
            case "newgame":
            case "new game":
                return MenuAction.NewGame;

            case "load":
            case "continue":
                return MenuAction.Continue;

            case "tutorial":
            case "guide":
                return MenuAction.Tutorial;

            case "credits":
            case "contributors":
                return MenuAction.Contributors;

            case "options":
            case "settings":
                return MenuAction.Settings;

            case "exit":
                return MenuAction.Exit;

            case "demo":
            case "demotest":
            case "demo test":
                return MenuAction.DemoTest;

            default:
                return MenuAction.None;
        }
    }

    private string GetDisplayLabel(MenuAction action)
    {
        switch (action)
        {
            case MenuAction.NewGame:
                return newGameLabel;
            case MenuAction.Continue:
                return continueLabel;
            case MenuAction.Tutorial:
                return tutorialLabel;
            case MenuAction.Contributors:
                return contributorsLabel;
            case MenuAction.Settings:
                return settingsLabel;
            case MenuAction.Exit:
                return exitLabel;
            case MenuAction.DemoTest:
                return demoTestLabel;
            default:
                return string.Empty;
        }
    }

    private List<GalleryLevelView> BuildMainMenuItemsWithTutorialEntry()
    {
        List<GalleryLevelView> items = new List<GalleryLevelView>();
        if (mainMenuGallery == null || mainMenuGallery.items == null)
            return items;

        for (int i = 0; i < mainMenuGallery.items.Length; i++)
        {
            GalleryLevelView item = mainMenuGallery.items[i];
            if (item != null)
                items.Add(item);
        }

        EnsureTutorialMenuItem(items);
        return items;
    }

    private void EnsureTutorialMenuItem(List<GalleryLevelView> items)
    {
        for (int i = 0; i < items.Count; i++)
        {
            if (ResolveMenuAction(GetMenuIdentity(items[i])) == MenuAction.Tutorial)
                return;
        }

        GalleryLevelView prototype = FindTutorialPrototype(items);
        if (prototype == null)
            return;

        GalleryLevelView tutorialItem = CreateRuntimeTutorialMenuItem(prototype);
        int insertIndex = DetermineTutorialInsertIndex(items);
        items.Insert(insertIndex, tutorialItem);
    }

    private GalleryLevelView CreateRuntimeTutorialMenuItem(GalleryLevelView prototype)
    {
        GameObject runtimeClone = Instantiate(prototype.gameObject, prototype.transform.parent, false);
        runtimeClone.name = tutorialLabel;
        runtimeClone.SetActive(true);

        GalleryLevelView tutorialItem = runtimeClone.GetComponent<GalleryLevelView>();
        if (tutorialItem == null)
            tutorialItem = runtimeClone.AddComponent<GalleryLevelView>();

        tutorialItem.manager = mainMenuGallery;
        tutorialItem.levelName = tutorialLabel;
        if (tutorialItem.text != null)
            tutorialItem.text.text = tutorialLabel;

        if (tutorialItem is DemoGalleryMenuItem demoItem)
        {
            if (demoItem.contentToShow != null)
                demoItem.contentToShow.SetActive(false);
            demoItem.contentToShow = null;
            if (demoItem.contentMask != null)
            {
                Color color = demoItem.contentMask.color;
                color.a = 0f;
                demoItem.contentMask.color = color;
            }
        }

        return tutorialItem;
    }

    private static GalleryLevelView FindTutorialPrototype(IReadOnlyList<GalleryLevelView> items)
    {
        for (int i = 0; i < items.Count; i++)
        {
            GalleryLevelView item = items[i];
            if (item != null && ResolveMenuAction(GetMenuIdentity(item)) == MenuAction.DemoTest)
                return item;
        }

        for (int i = 0; i < items.Count; i++)
        {
            if (items[i] != null)
                return items[i];
        }

        return null;
    }

    private static int DetermineTutorialInsertIndex(IReadOnlyList<GalleryLevelView> items)
    {
        for (int i = 0; i < items.Count; i++)
        {
            MenuAction action = ResolveMenuAction(GetMenuIdentity(items[i]));
            if (action == MenuAction.Continue)
                return i + 1;

            if (action == MenuAction.Contributors || action == MenuAction.Settings)
                return i;
        }

        return items.Count;
    }
}
