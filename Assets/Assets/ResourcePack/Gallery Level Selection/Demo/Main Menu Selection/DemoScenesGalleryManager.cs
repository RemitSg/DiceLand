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
    [SerializeField] private string newGameSceneName = "MapScene";
    [SerializeField] private string continueGameSceneName = "MapScene";
    [SerializeField] private string tutorialSceneName = string.Empty;
    [SerializeField] private string optionsSceneName = string.Empty;
    [SerializeField] private string contributorsSceneName = string.Empty;

    private readonly List<MenuEntry> runtimeEntries = new List<MenuEntry>();

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

    public override void Start()
    {
        PrepareMainMenuItems();
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

        if (item.text != null)
            item.text.text = entry.label;

        Text[] textComponents = item.GetComponentsInChildren<Text>(true);
        for (int i = 0; i < textComponents.Length; i++)
        {
            Text textComponent = textComponents[i];
            if (textComponent == null)
                continue;

            textComponent.text = entry.label;
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
                return "Continue Game";
            case MenuAction.Tutorial:
                return "Open Tutorial";
            case MenuAction.Options:
                return "Open Options";
            case MenuAction.Contributors:
                return "View Contributors";
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
                return "Continue";
            case MenuAction.Tutorial:
            case MenuAction.Options:
                return "Open";
            case MenuAction.Contributors:
                return "View";
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
                ContinueGame(entry.sceneName);
                break;
            case MenuAction.Tutorial:
            case MenuAction.Options:
            case MenuAction.Contributors:
                OpenOptionalScene(entry.sceneName, entry.label);
                break;
            case MenuAction.Exit:
                ExitGame();
                break;
        }
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
