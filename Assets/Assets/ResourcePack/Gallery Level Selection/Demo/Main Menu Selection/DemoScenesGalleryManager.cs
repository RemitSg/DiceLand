using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class DemoScenesGalleryManager : GalleryLevelSelectionManager
{
    [System.Serializable]
    public class ContributorsRollSettings
    {
        public string confirmTitle = "View Contributors?";
        public string confirmActionLabel = "Yes";
        public string rollTitle = "Contributors";
        [TextArea(2, 8)] public string openingText = "A small game by people who care a lot.";
        [TextArea(10, 40)] public string creditsText = "Director\nName\n\nProgramming\nName\n\nArt\nName\n\nMusic\nName";
        [TextArea(2, 8)] public string endingText = "Thank you for playing.";
        [Min(0f)] public float fadeDuration = 1.2f;
        [Min(0f)] public float startDelay = 0.6f;
        [Min(1f)] public float scrollSpeed = 90f;
        [Min(0f)] public float endDelay = 1.5f;
        [Min(200f)] public float contentWidth = 900f;
        [Min(0f)] public float horizontalPadding = 120f;
        [Min(0f)] public float verticalPadding = 180f;
        public Color backgroundColor = new Color(0f, 0f, 0f, 1f);
        public Color textColor = Color.white;
        [Min(16f)] public float titleFontSize = 60f;
        [Min(12f)] public float bodyFontSize = 36f;
    }

    [SerializeField] private string newGameSceneName = "MapScene";
    [SerializeField] private string continueGameSceneName = "MapScene";
    [SerializeField] private string tutorialSceneName = string.Empty;
    [SerializeField] private string optionsSceneName = string.Empty;
    [SerializeField] private string contributorsSceneName = string.Empty;
    [SerializeField] private GameObject settingsPanelPrefab;
    [SerializeField] private ContributorsRollSettings contributorsRollSettings = new ContributorsRollSettings();

    private readonly List<MenuEntry> runtimeEntries = new List<MenuEntry>();
    private MainMenuContributorsRollController contributorsRollController;

    private enum MenuAction
    {
        NewGame,
        ContinueGame,
        Tutorial,
        Options,
        Contributors,
        Exit
    }

    private struct MenuEntry
    {
        public string label;
        public MenuAction action;
        public string sceneName;

        public MenuEntry(string label, MenuAction action, string sceneName)
        {
            this.label = label;
            this.action = action;
            this.sceneName = sceneName;
        }
    }

    private bool HasContinueSnapshot()
    {
        if (string.IsNullOrWhiteSpace(continueGameSceneName))
            return false;

        return MapRunRuntime.TryGetSnapshot(continueGameSceneName, out _);
    }

    public override void Start()
    {
        PrepareMainMenuItems();
        EnsureOptionsSettingsPanel();
        EnsureContributorsRollController();
        base.Start();
    }

    public override void OnItemsClicked(int index, bool isCenter)
    {
        base.OnItemsClicked(index, isCenter);

        if (!isCenter)
            return;

        if (index < 0 || index >= runtimeEntries.Count)
            return;

        HandleMenuEntry(runtimeEntries[index]);
    }

    private void PrepareMainMenuItems()
    {
        runtimeEntries.Clear();
        runtimeEntries.Add(new MenuEntry("New Game", MenuAction.NewGame, newGameSceneName));
        runtimeEntries.Add(new MenuEntry("Continue Game", MenuAction.ContinueGame, continueGameSceneName));
        runtimeEntries.Add(new MenuEntry("Tutorial", MenuAction.Tutorial, tutorialSceneName));
        runtimeEntries.Add(new MenuEntry("Options", MenuAction.Options, optionsSceneName));
        runtimeEntries.Add(new MenuEntry("Contributors", MenuAction.Contributors, contributorsSceneName));
        runtimeEntries.Add(new MenuEntry("Exit", MenuAction.Exit, string.Empty));

        RemoveInvalidMenuItems();
        EnsureMenuItemCapacity(runtimeEntries.Count);

        GalleryLevelView[] existingItems = items ?? new GalleryLevelView[0];
        GalleryLevelView[] mainMenuItems = new GalleryLevelView[runtimeEntries.Count];
        for (int i = 0; i < runtimeEntries.Count; i++)
        {
            GalleryLevelView item = existingItems[i];
            mainMenuItems[i] = item;
            ApplyMenuEntryToItem(item, runtimeEntries[i], i);
        }

        for (int i = runtimeEntries.Count; i < existingItems.Length; i++)
        {
            if (existingItems[i] != null)
                existingItems[i].gameObject.SetActive(false);
        }

        items = mainMenuItems;
        autoGenerateItemsCount = 0;
        start_level = 0;
        activeItems = Mathf.Clamp(activeItems, 4, Mathf.Max(4, items.Length));
        infiniteLoop = items.Length >= 4;
    }

    private void RemoveInvalidMenuItems()
    {
        if (items == null || items.Length == 0)
        {
            items = new GalleryLevelView[0];
            return;
        }

        List<GalleryLevelView> validItems = new List<GalleryLevelView>(items.Length);
        for (int i = 0; i < items.Length; i++)
        {
            GalleryLevelView item = items[i];
            if (item == null)
                continue;

            validItems.Add(item);
        }

        items = validItems.ToArray();
    }

    private void EnsureMenuItemCapacity(int requiredCount)
    {
        RemoveInvalidMenuItems();

        if (items.Length == 0)
        {
            Debug.LogWarning("[DemoScenesGalleryManager] No menu items were found in the scene.");
            return;
        }

        if (items.Length >= requiredCount)
            return;

        List<GalleryLevelView> expandedItems = new List<GalleryLevelView>(items);
        GalleryLevelView template = items[items.Length - 1];
        Transform parent = template != null && template.transform.parent != null
            ? template.transform.parent
            : itemsContainer;

        while (expandedItems.Count < requiredCount)
        {
            GameObject clonedObject = Instantiate(template.gameObject, parent, false);
            clonedObject.name = $"Main Menu Item {expandedItems.Count + 1}";
            GalleryLevelView clonedView = clonedObject.GetComponent<GalleryLevelView>();
            if (clonedView == null)
                clonedView = clonedObject.AddComponent<GalleryLevelView>();

            clonedView.manager = this;
            expandedItems.Add(clonedView);
        }

        items = expandedItems.ToArray();
    }

    private void ApplyMenuEntryToItem(GalleryLevelView item, MenuEntry entry, int index)
    {
        if (item == null)
            return;

        item.manager = this;
        item.index = index;
        item.levelName = entry.label;
        item.gameObject.name = entry.label;
        item.gameObject.SetActive(true);

        bool continueAvailable = entry.action != MenuAction.ContinueGame || HasContinueSnapshot();

        if (item.text != null)
        {
            item.text.text = entry.label;
            item.text.color = continueAvailable
                ? new Color(item.text.color.r, item.text.color.g, item.text.color.b, 1f)
                : new Color(item.text.color.r, item.text.color.g, item.text.color.b, 0.45f);
        }

        Text[] textComponents = item.GetComponentsInChildren<Text>(true);
        for (int i = 0; i < textComponents.Length; i++)
        {
            Text textComponent = textComponents[i];
            if (textComponent == null)
                continue;

            textComponent.text = entry.label;
            textComponent.color = continueAvailable
                ? new Color(textComponent.color.r, textComponent.color.g, textComponent.color.b, 1f)
                : new Color(textComponent.color.r, textComponent.color.g, textComponent.color.b, 0.45f);
        }

        if (item is DemoGalleryMenuItem demoItem)
            ApplyMenuEntryToContent(demoItem, entry);
    }

    private void ApplyMenuEntryToContent(DemoGalleryMenuItem item, MenuEntry entry)
    {
        if (item == null || item.contentToShow == null)
            return;

        Text[] contentTexts = item.contentToShow.GetComponentsInChildren<Text>(true);
        if (contentTexts == null || contentTexts.Length == 0)
            return;

        Text[] orderedTexts = contentTexts
            .Where(textComponent => textComponent != null)
            .OrderByDescending(textComponent => textComponent.rectTransform.position.y)
            .ToArray();

        if (orderedTexts.Length >= 1)
            orderedTexts[0].text = GetContentTitle(entry);

        if (orderedTexts.Length >= 2)
            orderedTexts[orderedTexts.Length - 1].text = GetContentActionLabel(entry);
    }

    private string GetContentTitle(MenuEntry entry)
    {
        switch (entry.action)
        {
            case MenuAction.NewGame:
                return "Select Difficulty";
            case MenuAction.ContinueGame:
                return HasContinueSnapshot() ? "Continue Game" : "No Saved Game";
            case MenuAction.Tutorial:
                return "Open Tutorial";
            case MenuAction.Options:
                return "Settings";
            case MenuAction.Contributors:
                return string.IsNullOrWhiteSpace(contributorsRollSettings.confirmTitle)
                    ? "View Contributors?"
                    : contributorsRollSettings.confirmTitle;
            case MenuAction.Exit:
                return "Exit Game";
            default:
                return entry.label;
        }
    }

    private string GetContentActionLabel(MenuEntry entry)
    {
        switch (entry.action)
        {
            case MenuAction.NewGame:
                return "Choose";
            case MenuAction.ContinueGame:
                return HasContinueSnapshot() ? "Continue" : "Unavailable";
            case MenuAction.Tutorial:
                return "Open";
            case MenuAction.Options:
                return "Adjust";
            case MenuAction.Contributors:
                return string.IsNullOrWhiteSpace(contributorsRollSettings.confirmActionLabel)
                    ? "Yes"
                    : contributorsRollSettings.confirmActionLabel;
            case MenuAction.Exit:
                return "Exit";
            default:
                return entry.label;
        }
    }

    private void HandleMenuEntry(MenuEntry entry)
    {
        switch (entry.action)
        {
            case MenuAction.NewGame:
                return;
            case MenuAction.ContinueGame:
                if (!HasContinueSnapshot())
                    return;
                ContinueGame(entry.sceneName);
                break;
            case MenuAction.Tutorial:
                OpenOptionalScene(entry.sceneName, entry.label);
                break;
            case MenuAction.Contributors:
                PlayContributorsRoll();
                break;
            case MenuAction.Options:
                return;
            case MenuAction.Exit:
                ExitGame();
                break;
        }
    }

    private void EnsureOptionsSettingsPanel()
    {
        if (settingsPanelPrefab == null)
            return;

        for (int i = 0; i < runtimeEntries.Count && i < items.Length; i++)
        {
            if (runtimeEntries[i].action != MenuAction.Options)
                continue;

            if (!(items[i] is DemoGalleryMenuItem demoItem) || demoItem.contentToShow == null)
                return;

            MainMenuSettingsPanelController controller = demoItem.contentToShow.GetComponentInChildren<MainMenuSettingsPanelController>(true);
            if (controller == null)
            {
                for (int childIndex = 0; childIndex < demoItem.contentToShow.transform.childCount; childIndex++)
                    demoItem.contentToShow.transform.GetChild(childIndex).gameObject.SetActive(false);

                GameObject instance = Instantiate(settingsPanelPrefab, demoItem.contentToShow.transform, false);
                instance.name = "Settings Panel";

                RectTransform instanceRect = instance.transform as RectTransform;
                if (instanceRect != null)
                {
                    instanceRect.anchorMin = Vector2.zero;
                    instanceRect.anchorMax = Vector2.one;
                    instanceRect.offsetMin = Vector2.zero;
                    instanceRect.offsetMax = Vector2.zero;
                    instanceRect.anchoredPosition = Vector2.zero;
                    instanceRect.localScale = Vector3.one;
                }

                controller = instance.GetComponent<MainMenuSettingsPanelController>();
                if (controller == null)
                    controller = instance.AddComponent<MainMenuSettingsPanelController>();
            }

            controller.Initialize(this);
            return;
        }
    }

    private void EnsureContributorsRollController()
    {
        if (contributorsRollController != null)
            return;

        contributorsRollController = GetComponentInChildren<MainMenuContributorsRollController>(true);
        if (contributorsRollController != null)
            return;

        GameObject controllerObject = new GameObject("Contributors Roll Controller", typeof(RectTransform));
        controllerObject.transform.SetParent(transform, false);

        RectTransform rectTransform = controllerObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
        rectTransform.localScale = Vector3.one;

        contributorsRollController = controllerObject.AddComponent<MainMenuContributorsRollController>();
        contributorsRollController.Initialize(this);
    }

    private void PlayContributorsRoll()
    {
        EnsureContributorsRollController();
        if (contributorsRollController == null)
            return;

        contributorsRollController.Play(contributorsRollSettings);
    }

    private void StartNewGame(string sceneName)
    {
        MapRunRuntime.Clear();
        RestSceneRuntime.Clear();
        BattleEncounterRuntime.Clear();
        TransitionToScene(sceneName);
    }

    private void ContinueGame(string sceneName)
    {
        MainMenuRuntime.ClearSelectedDifficulty();
        TransitionToScene(sceneName);
    }

    private void OpenOptionalScene(string sceneName, string entryLabel)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.Log($"[DemoScenesGalleryManager] '{entryLabel}' is reserved, but no target scene is configured yet.");
            return;
        }

        TransitionToScene(sceneName);
    }

    private void TransitionToScene(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogWarning("[DemoScenesGalleryManager] Target scene name is empty.");
            return;
        }

        SceneCurtainTransitionController.Instance.TransitionToScene(sceneName);
    }

    private void ExitGame()
    {
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
