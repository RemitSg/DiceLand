using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class DifficultySelectionGalleryManager : GalleryLevelSelectionManager
{
    [SerializeField] private DifficultyConfig_SO easyDifficultyConfig;
    [SerializeField] private DifficultyConfig_SO mediumDifficultyConfig;
    [SerializeField] private DifficultyConfig_SO hardDifficultyConfig;
    [SerializeField] private DifficultyConfig_SO extremeDifficultyConfig;
    [SerializeField] private string mapSceneName = "MapScene";

    private readonly List<DifficultyEntry> runtimeEntries = new List<DifficultyEntry>(4);

    private struct DifficultyEntry
    {
        public string label;
        public DifficultyConfig_SO config;

        public DifficultyEntry(string label, DifficultyConfig_SO config)
        {
            this.label = label;
            this.config = config;
        }
    }

    public override void Start()
    {
        PrepareDifficultyItems();
        base.Start();
    }

    public override void OnItemsClicked(int index, bool isCenter)
    {
        base.OnItemsClicked(index, isCenter);

        if (!isCenter)
            return;

        if (index < 0 || index >= runtimeEntries.Count)
            return;

        DifficultyEntry entry = runtimeEntries[index];
        if (entry.config == null)
        {
            Debug.LogWarning($"[DifficultySelectionGalleryManager] Difficulty entry '{entry.label}' has no config assigned.");
            return;
        }

        MainMenuRuntime.SetSelectedDifficulty(entry.config);
        MapRunRuntime.Clear(mapSceneName);
        RestSceneRuntime.Clear();
        BattleEncounterRuntime.Clear();
        TransitionToMapScene();
    }

    private void PrepareDifficultyItems()
    {
        runtimeEntries.Clear();
        runtimeEntries.Add(new DifficultyEntry("Easy", easyDifficultyConfig));
        runtimeEntries.Add(new DifficultyEntry("Medium", mediumDifficultyConfig));
        runtimeEntries.Add(new DifficultyEntry("Hard", hardDifficultyConfig));
        runtimeEntries.Add(new DifficultyEntry("Extreme", extremeDifficultyConfig));

        RemoveInvalidItems();
        EnsureMenuItemCapacity(runtimeEntries.Count);

        GalleryLevelView[] existingItems = items ?? new GalleryLevelView[0];
        GalleryLevelView[] difficultyItems = new GalleryLevelView[runtimeEntries.Count];
        for (int i = 0; i < runtimeEntries.Count; i++)
        {
            GalleryLevelView item = existingItems[i];
            difficultyItems[i] = item;
            ApplyDifficultyEntryToItem(item, runtimeEntries[i], i);
        }

        for (int i = runtimeEntries.Count; i < existingItems.Length; i++)
        {
            if (existingItems[i] != null)
                existingItems[i].gameObject.SetActive(false);
        }

        items = difficultyItems;
        autoGenerateItemsCount = 0;
        start_level = 0;
        activeItems = Mathf.Clamp(activeItems, 4, Mathf.Max(4, items.Length));
        infiniteLoop = false;

        UpdateContentPanelTexts();
    }

    private void RemoveInvalidItems()
    {
        if (items == null || items.Length == 0)
        {
            items = new GalleryLevelView[0];
            return;
        }

        List<GalleryLevelView> validItems = new List<GalleryLevelView>(items.Length);
        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] != null)
                validItems.Add(items[i]);
        }

        items = validItems.ToArray();
    }

    private void EnsureMenuItemCapacity(int requiredCount)
    {
        RemoveInvalidItems();

        if (items.Length == 0)
        {
            Debug.LogWarning("[DifficultySelectionGalleryManager] No submenu items were found in the scene.");
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
            clonedObject.name = $"Difficulty Item {expandedItems.Count + 1}";
            GalleryLevelView clonedView = clonedObject.GetComponent<GalleryLevelView>();
            if (clonedView == null)
                clonedView = clonedObject.AddComponent<GalleryLevelView>();

            clonedView.manager = this;
            expandedItems.Add(clonedView);
        }

        items = expandedItems.ToArray();
    }

    private void ApplyDifficultyEntryToItem(GalleryLevelView item, DifficultyEntry entry, int index)
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
    }

    private void UpdateContentPanelTexts()
    {
        Transform parentTransform = transform.parent;
        if (parentTransform == null)
            return;

        HashSet<Text> itemTexts = new HashSet<Text>();
        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] == null)
                continue;

            Text[] childTexts = items[i].GetComponentsInChildren<Text>(true);
            for (int j = 0; j < childTexts.Length; j++)
            {
                if (childTexts[j] != null)
                    itemTexts.Add(childTexts[j]);
            }
        }

        Text[] candidateTexts = parentTransform.GetComponentsInChildren<Text>(true)
            .Where(textComponent => textComponent != null && !itemTexts.Contains(textComponent))
            .OrderByDescending(textComponent => textComponent.rectTransform.position.y)
            .ToArray();

        if (candidateTexts.Length >= 1)
            candidateTexts[0].text = "Select Difficulty";

        if (candidateTexts.Length >= 2)
            candidateTexts[candidateTexts.Length - 1].text = "Start";
    }

    private void TransitionToMapScene()
    {
        if (string.IsNullOrWhiteSpace(mapSceneName))
        {
            Debug.LogWarning("[DifficultySelectionGalleryManager] Map scene name is empty.");
            return;
        }

        if (SceneCurtainTransitionController.Instance != null)
        {
            SceneCurtainTransitionController.Instance.TransitionToScene(mapSceneName);
            return;
        }

        SceneManager.LoadScene(mapSceneName);
    }
}
