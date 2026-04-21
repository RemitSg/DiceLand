using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public class RestSceneReforgeInteractable : MonoBehaviour
{
    private const string OutlineShaderResourcePath = "Shaders/RestSceneSolidOutline";
    private const string OutlineShaderName = "MobileGame/RestScene/SolidSpriteOutline";
    private const string OutlineWidthPropertyName = "_OutlineWidth";

    private enum ReforgeMode
    {
        Loot = 0,
        Relic = 1
    }

    [Header("Pools")]
    [SerializeField] private LootItemTable_SO lootReforgeTable;
    [SerializeField] private List<LootAttribute_SO> lootAttributeOptions = new List<LootAttribute_SO>();
    [SerializeField] private RelicTable_SO relicReforgeTable;

    [Header("Outline")]
    [SerializeField] private Color outlineColor = new Color(0.9f, 0.7f, 1f, 1f);
    [FormerlySerializedAs("outlineScale")]
    [SerializeField] private float outlineWidth = 1.25f;
    [SerializeField] private Vector3 outlineOffset = Vector3.zero;
    [SerializeField] private int outlineSortingOrderOffset = -1;

    [Header("Open Prompt")]
    [SerializeField] private Vector2 promptSize = new Vector2(760f, 360f);
    [SerializeField] private Vector2 promptOffset = Vector2.zero;
    [SerializeField] private Sprite promptBackgroundSprite;
    [SerializeField] private Color promptBackgroundColor = new Color(0f, 0f, 0f, 0.88f);
    [SerializeField] private Sprite promptConfirmButtonSprite;
    [SerializeField] private Sprite promptCancelButtonSprite;
    [SerializeField] private string promptTitleText = "重铸";
    [SerializeField] private string promptBodyText = "确定要开始重铸吗？\n每次休息访问最多可进行两次重铸。";
    [SerializeField] private string promptConfirmLabel = "确定";
    [SerializeField] private string promptCancelLabel = "取消";

    [Header("Panel")]
    [SerializeField] private Vector2 panelSize = new Vector2(1260f, 760f);
    [SerializeField] private Vector2 panelOffset = Vector2.zero;
    [SerializeField] private Sprite panelBackgroundSprite;
    [SerializeField] private Color panelBackgroundColor = new Color(0.05f, 0.06f, 0.09f, 0.94f);
    [SerializeField] private Sprite sectionBackgroundSprite;
    [SerializeField] private Color sectionBackgroundColor = new Color(0.12f, 0.14f, 0.2f, 0.92f);
    [SerializeField] private Sprite actionButtonSprite;
    [SerializeField] private Sprite selectedButtonSprite;
    [SerializeField] private Color actionButtonColor = new Color(0.32f, 0.64f, 0.95f, 1f);
    [SerializeField] private Color selectedButtonColor = new Color(0.95f, 0.74f, 0.33f, 1f);
    [SerializeField] private Color disabledButtonColor = new Color(0.38f, 0.38f, 0.38f, 0.92f);
    [SerializeField] private Color normalButtonTextColor = Color.white;
    [SerializeField] private Color selectedButtonTextColor = Color.black;

    [Header("Layout")]
    [SerializeField] private Vector2 modeButtonSize = new Vector2(210f, 62f);
    [SerializeField] private Vector2 bottomButtonSize = new Vector2(240f, 72f);
    [SerializeField] private float modeButtonSpacing = 18f;
    [SerializeField] private float listButtonHeight = 82f;
    [SerializeField] private float listButtonSpacing = 10f;
    [SerializeField] private Vector2 itemListSize = new Vector2(430f, 450f);
    [SerializeField] private Vector2 attributeListSize = new Vector2(260f, 300f);

    [Header("Text")]
    [SerializeField] private TMP_FontAsset titleFont;
    [SerializeField] private TMP_FontAsset bodyFont;
    [SerializeField] private TMP_FontAsset buttonFont;
    [SerializeField] private Color titleColor = Color.white;
    [SerializeField] private Color bodyColor = new Color(0.95f, 0.95f, 0.95f, 1f);
    [SerializeField] private Color accentColor = new Color(1f, 0.83f, 0.35f, 1f);
    [SerializeField] private Color statusColor = new Color(0.7f, 0.92f, 1f, 1f);
    [SerializeField] private Color errorColor = new Color(1f, 0.48f, 0.48f, 1f);
    [SerializeField] private int titleFontSize = 42;
    [SerializeField] private int bodyFontSize = 28;
    [SerializeField] private int buttonFontSize = 28;

    private Canvas canvas;
    private RectTransform canvasRect;
    private RectTransform promptPanelRect;
    private Image promptPanelImage;
    private Button promptConfirmButton;
    private Button promptCancelButton;
    private RectTransform mainPanelRect;
    private Image mainPanelImage;
    private Button lootModeButton;
    private Button relicModeButton;
    private Button actionButton;
    private Button secondaryButton;
    private TextMeshProUGUI titleText;
    private TextMeshProUGUI remainingText;
    private TextMeshProUGUI hintText;
    private TextMeshProUGUI itemSectionTitleText;
    private TextMeshProUGUI attributeSectionTitleText;
    private TextMeshProUGUI selectionSummaryText;
    private TextMeshProUGUI resultPreviewText;
    private TextMeshProUGUI statusText;
    private RectTransform itemListContent;
    private RectTransform attributeListContent;
    private ScrollRect itemScrollRect;
    private ScrollRect attributeScrollRect;
    private readonly List<Button> itemButtons = new List<Button>();
    private readonly List<Button> attributeButtons = new List<Button>();
    private readonly List<GameObject> spawnedItemObjects = new List<GameObject>();
    private readonly List<GameObject> spawnedAttributeObjects = new List<GameObject>();

    private SpriteRenderer targetSpriteRenderer;
    private SpriteRenderer outlineRenderer;
    private Material runtimeOutlineMaterial;
    private Collider2D interactionCollider;
    private Camera inputCamera;
    private bool mouseHovering;
    private bool touchHighlighting;
    private bool ownsModalLock;
    private bool promptVisible;
    private bool mainPanelVisible;

    private ReforgeMode currentMode = ReforgeMode.Loot;
    private LootItem_SO selectedLootA;
    private LootItem_SO selectedLootB;
    private LootAttribute_SO selectedLootAttribute;
    private Relic_SO selectedRelicA;
    private Relic_SO selectedRelicB;
    private int successfulReforgeCount;

    private void Awake()
    {
        interactionCollider = GetComponent<Collider2D>();
        inputCamera = Camera.main;
        EnsureOutlineRenderer();
        EnsureUi();
        SetPromptVisible(false);
        SetMainPanelVisible(false);
        RefreshHighlightState();
    }

    private void OnEnable()
    {
        RefreshHighlightState();
    }

    private void OnDisable()
    {
        mouseHovering = false;
        touchHighlighting = false;
        HideAllPanels(true);
        RefreshHighlightState();
    }

    private void OnDestroy()
    {
        ReleaseModalLock();
        if (runtimeOutlineMaterial == null)
            return;

        if (Application.isPlaying)
            Destroy(runtimeOutlineMaterial);
        else
            DestroyImmediate(runtimeOutlineMaterial);

        runtimeOutlineMaterial = null;
    }

    private void Update()
    {
        UpdateTouchHighlight();
        RefreshOutlineRendererVisuals();
    }

    private void OnMouseEnter()
    {
        if (RestSceneModalState.IsLocked && !RestSceneModalState.IsLockedBy(this))
            return;

        mouseHovering = true;
        RefreshHighlightState();
    }

    private void OnMouseExit()
    {
        mouseHovering = false;
        RefreshHighlightState();
    }

    private void OnMouseDown()
    {
        if (RestSceneModalState.IsLocked && !RestSceneModalState.IsLockedBy(this))
            return;

        mouseHovering = true;
        RefreshHighlightState();
    }

    private void OnMouseUpAsButton()
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;
        if (RestSceneModalState.IsLocked && !RestSceneModalState.IsLockedBy(this))
            return;
        if (!RestSceneRuntime.HasPendingVisit)
            return;

        ShowOpenPrompt();
    }

    public void ShowOpenPrompt()
    {
        if (!RestSceneModalState.TryAcquire(this))
            return;

        ownsModalLock = true;
        EnsureUi();
        SetMainPanelVisible(false);
        SetPromptVisible(true);
    }

    public void HideAllPanels()
    {
        HideAllPanels(false);
    }

    private void HideAllPanels(bool forceRelease)
    {
        promptVisible = false;
        mainPanelVisible = false;

        if (promptPanelRect != null)
            promptPanelRect.gameObject.SetActive(false);
        if (mainPanelRect != null)
            mainPanelRect.gameObject.SetActive(false);
        if (canvas != null)
            canvas.gameObject.SetActive(false);

        if (forceRelease || !HasCommittedFirstReforge())
            ReleaseModalLock();

        RefreshHighlightState();
    }

    private void HandlePromptConfirm()
    {
        if (!RestSceneRuntime.HasPendingVisit)
        {
            HideAllPanels(true);
            return;
        }

        EnsureUi();
        SetPromptVisible(false);
        SetMainPanelVisible(true);
        SetStatus(string.Empty, false);
        RefreshMainPanel();
    }

    private void HandleSecondaryButtonClicked()
    {
        if (HasCommittedFirstReforge())
        {
            RestSceneRuntime.TryConfirmReforgeExitAndReturn();
            return;
        }

        HideAllPanels(true);
    }

    private void HandleLootModeClicked()
    {
        currentMode = ReforgeMode.Loot;
        RefreshMainPanel();
    }

    private void HandleRelicModeClicked()
    {
        currentMode = ReforgeMode.Relic;
        RefreshMainPanel();
    }

    private void HandleActionButtonClicked()
    {
        if (!RestSceneRuntime.HasPendingVisit)
        {
            SetStatus("当前不在休息场景访问流程中。", true);
            return;
        }

        string resultName;
        bool succeeded = currentMode == ReforgeMode.Loot
            ? TryExecuteLootReforge(out resultName)
            : TryExecuteRelicReforge(out resultName);

        if (!succeeded)
            return;

        successfulReforgeCount = Mathf.Clamp(successfulReforgeCount + 1, 0, 2);
        ClearCurrentModeSelection();
        RefreshMainPanel();

        if (successfulReforgeCount >= 2)
        {
            RestSceneRuntime.TryConfirmReforgeExitAndReturn();
            return;
        }

        SetStatus($"重铸成功：获得 {resultName}\n还可以继续重铸 1 次，或直接离开休息场景。", false);
    }

    private bool TryExecuteLootReforge(out string resultName)
    {
        resultName = string.Empty;
        if (!ValidateLootSelection(out string errorMessage))
        {
            SetStatus(errorMessage, true);
            return false;
        }

        LootItem_SO resultLoot = RollLootReforgeResult();
        if (resultLoot == null)
        {
            SetStatus("当前没有符合条件的重铸结果。", true);
            return false;
        }

        if (!RestSceneRuntime.TryApplyLootReforge(selectedLootA, selectedLootB, resultLoot, out string applyError))
        {
            SetStatus(string.IsNullOrWhiteSpace(applyError) ? "战利品重铸失败。" : applyError, true);
            return false;
        }

        resultName = GetLootDisplayName(resultLoot);
        return true;
    }

    private bool TryExecuteRelicReforge(out string resultName)
    {
        resultName = string.Empty;
        if (!ValidateRelicSelection(out string errorMessage))
        {
            SetStatus(errorMessage, true);
            return false;
        }

        Relic_SO resultRelic = RollRelicReforgeResult();
        if (resultRelic == null)
        {
            SetStatus("当前没有可用的更高阶遗物。", true);
            return false;
        }

        if (!RestSceneRuntime.TryApplyRelicReforge(selectedRelicA, selectedRelicB, resultRelic, out string applyError))
        {
            SetStatus(string.IsNullOrWhiteSpace(applyError) ? "遗物重铸失败。" : applyError, true);
            return false;
        }

        resultName = GetRelicDisplayName(resultRelic);
        return true;
    }

    private bool ValidateLootSelection(out string errorMessage)
    {
        errorMessage = string.Empty;
        if (selectedLootA == null || selectedLootB == null)
        {
            errorMessage = "请先选择两件战利品材料。";
            return false;
        }

        if (selectedLootAttribute == null)
        {
            errorMessage = "请先选择目标属性。";
            return false;
        }

        if (selectedLootA.rarity != selectedLootB.rarity)
        {
            errorMessage = "两件战利品必须为同阶级。";
            return false;
        }

        if (!RestSceneRuntime.TryGetCurrentSnapshot(out MapRunSnapshot snapshot) || snapshot?.runState == null)
        {
            errorMessage = "未找到当前队伍背包数据。";
            return false;
        }

        int requiredAmount = selectedLootA == selectedLootB ? 2 : 1;
        if (snapshot.runState.GetLootAmount(selectedLootA) < requiredAmount)
        {
            errorMessage = $"材料不足：{GetLootDisplayName(selectedLootA)}";
            return false;
        }

        if (selectedLootA != selectedLootB && snapshot.runState.GetLootAmount(selectedLootB) < 1)
        {
            errorMessage = $"材料不足：{GetLootDisplayName(selectedLootB)}";
            return false;
        }

        return true;
    }

    private bool ValidateRelicSelection(out string errorMessage)
    {
        errorMessage = string.Empty;
        if (selectedRelicA == null || selectedRelicB == null)
        {
            errorMessage = "请先选择两件遗物材料。";
            return false;
        }

        if (selectedRelicA.rarity != selectedRelicB.rarity)
        {
            errorMessage = "两件遗物必须为同阶级。";
            return false;
        }

        if (selectedRelicA.rarity >= LootRarity.Legendary)
        {
            errorMessage = "传说遗物无法继续升阶重铸。";
            return false;
        }

        if (!RestSceneRuntime.TryGetCurrentSnapshot(out MapRunSnapshot snapshot) || snapshot?.runState == null)
        {
            errorMessage = "未找到当前队伍遗物数据。";
            return false;
        }

        int requiredAmount = selectedRelicA == selectedRelicB ? 2 : 1;
        if (snapshot.runState.GetRelicAmount(selectedRelicA) < requiredAmount)
        {
            errorMessage = $"材料不足：{GetRelicDisplayName(selectedRelicA)}";
            return false;
        }

        if (selectedRelicA != selectedRelicB && snapshot.runState.GetRelicAmount(selectedRelicB) < 1)
        {
            errorMessage = $"材料不足：{GetRelicDisplayName(selectedRelicB)}";
            return false;
        }

        return true;
    }

    private LootItem_SO RollLootReforgeResult()
    {
        if (selectedLootA == null || selectedLootB == null || selectedLootAttribute == null)
            return null;

        LootRarity rarity = selectedLootA.rarity;
        List<LootItem_SO> primaryCandidates = new List<LootItem_SO>();
        List<LootItem_SO> fallbackCandidates = new List<LootItem_SO>();
        IReadOnlyList<LootItem_SO> lootPool = GetResolvedLootReforgePool();
        for (int i = 0; i < lootPool.Count; i++)
        {
            LootItem_SO candidate = lootPool[i];
            if (candidate == null || candidate.attribute != selectedLootAttribute || candidate.rarity != rarity)
                continue;

            fallbackCandidates.Add(candidate);
            if (candidate != selectedLootA && candidate != selectedLootB)
                primaryCandidates.Add(candidate);
        }

        List<LootItem_SO> resolvedCandidates = primaryCandidates.Count > 0 ? primaryCandidates : fallbackCandidates;
        if (resolvedCandidates.Count <= 0)
            return null;

        System.Random rng = CreateDeterministicRandom("LootReforge");
        return resolvedCandidates[rng.Next(resolvedCandidates.Count)];
    }

    private Relic_SO RollRelicReforgeResult()
    {
        if (selectedRelicA == null || selectedRelicB == null)
            return null;

        int startRarityIndex = Mathf.Clamp((int)selectedRelicA.rarity + 1, 0, (int)LootRarity.Legendary);
        IReadOnlyList<Relic_SO> relicPool = GetResolvedRelicReforgePool();
        for (int rarityIndex = startRarityIndex; rarityIndex <= (int)LootRarity.Legendary; rarityIndex++)
        {
            LootRarity rarity = (LootRarity)rarityIndex;
            List<Relic_SO> candidates = new List<Relic_SO>();
            for (int i = 0; i < relicPool.Count; i++)
            {
                Relic_SO candidate = relicPool[i];
                if (candidate == null || candidate.rarity != rarity)
                    continue;

                candidates.Add(candidate);
            }

            if (candidates.Count <= 0)
                continue;

            System.Random rng = CreateDeterministicRandom("RelicReforge");
            return candidates[rng.Next(candidates.Count)];
        }

        return null;
    }

    private System.Random CreateDeterministicRandom(string salt)
    {
        unchecked
        {
            int seed = 17;
            seed = seed * 31 + successfulReforgeCount;
            seed = seed * 31 + (int)currentMode;
            seed = seed * 31 + RestSceneRuntime.SourceCoord.x;
            seed = seed * 31 + RestSceneRuntime.SourceCoord.y;
            seed = seed * 31 + (RestSceneRuntime.ReturnSceneName != null ? RestSceneRuntime.ReturnSceneName.GetHashCode() : 0);
            seed = seed * 31 + GetObjectIdentityHash(selectedLootA);
            seed = seed * 31 + GetObjectIdentityHash(selectedLootB);
            seed = seed * 31 + GetObjectIdentityHash(selectedLootAttribute);
            seed = seed * 31 + GetObjectIdentityHash(selectedRelicA);
            seed = seed * 31 + GetObjectIdentityHash(selectedRelicB);
            seed = seed * 31 + (salt != null ? salt.GetHashCode() : 0);
            return new System.Random(seed);
        }
    }

    private static int GetObjectIdentityHash(UnityEngine.Object obj)
    {
        return obj != null ? obj.name.GetHashCode() : 0;
    }

    private void RefreshMainPanel()
    {
        EnsureUi();
        ValidateSelectionsAgainstInventory();
        RefreshModeButtonStates();
        RefreshItemSection();
        RefreshAttributeSection();
        RefreshSummaryAndButtons();
    }

    private void ValidateSelectionsAgainstInventory()
    {
        if (!RestSceneRuntime.TryGetCurrentSnapshot(out MapRunSnapshot snapshot) || snapshot?.runState == null)
        {
            selectedLootA = null;
            selectedLootB = null;
            selectedRelicA = null;
            selectedRelicB = null;
            return;
        }

        RunState runState = snapshot.runState;
        if (selectedLootA != null && runState.GetLootAmount(selectedLootA) <= 0)
            selectedLootA = null;
        if (selectedLootB != null && runState.GetLootAmount(selectedLootB) <= 0)
            selectedLootB = null;
        if (selectedLootA != null && selectedLootB != null && selectedLootA == selectedLootB && runState.GetLootAmount(selectedLootA) < 2)
            selectedLootB = null;

        if (selectedRelicA != null && runState.GetRelicAmount(selectedRelicA) <= 0)
            selectedRelicA = null;
        if (selectedRelicB != null && runState.GetRelicAmount(selectedRelicB) <= 0)
            selectedRelicB = null;
        if (selectedRelicA != null && selectedRelicB != null && selectedRelicA == selectedRelicB && runState.GetRelicAmount(selectedRelicA) < 2)
            selectedRelicB = null;

        if (selectedLootA == null && selectedLootB != null)
        {
            selectedLootA = selectedLootB;
            selectedLootB = null;
        }

        if (selectedRelicA == null && selectedRelicB != null)
        {
            selectedRelicA = selectedRelicB;
            selectedRelicB = null;
        }
    }

    private void RefreshModeButtonStates()
    {
        ApplySelectionButtonVisual(lootModeButton, currentMode == ReforgeMode.Loot);
        ApplySelectionButtonVisual(relicModeButton, currentMode == ReforgeMode.Relic);
    }

    private void RefreshItemSection()
    {
        ClearSpawnedObjects(spawnedItemObjects);
        itemButtons.Clear();

        if (itemSectionTitleText != null)
            itemSectionTitleText.text = currentMode == ReforgeMode.Loot ? "选择战利品材料" : "选择遗物材料";

        if (!RestSceneRuntime.TryGetCurrentSnapshot(out MapRunSnapshot snapshot) || snapshot?.runState == null)
            return;

        if (currentMode == ReforgeMode.Loot)
        {
            List<RunLootState> items = GetEligibleLootInventory(snapshot.runState);
            for (int i = 0; i < items.Count; i++)
            {
                RunLootState stack = items[i];
                if (stack == null || stack.lootItem == null)
                    continue;

                LootItem_SO localLoot = stack.lootItem;
                Button button = CreateListButton(
                    itemListContent,
                    BuildLootButtonLabel(stack),
                    () => HandleLootSelection(localLoot));
                ApplySelectionButtonVisual(button, localLoot == selectedLootA || localLoot == selectedLootB);
                itemButtons.Add(button);
                spawnedItemObjects.Add(button.gameObject);
            }
        }
        else
        {
            List<RunRelicState> relics = GetEligibleRelicInventory(snapshot.runState);
            for (int i = 0; i < relics.Count; i++)
            {
                RunRelicState stack = relics[i];
                if (stack == null || stack.relicData == null)
                    continue;

                Relic_SO localRelic = stack.relicData;
                Button button = CreateListButton(
                    itemListContent,
                    BuildRelicButtonLabel(stack),
                    () => HandleRelicSelection(localRelic));
                ApplySelectionButtonVisual(button, localRelic == selectedRelicA || localRelic == selectedRelicB);
                itemButtons.Add(button);
                spawnedItemObjects.Add(button.gameObject);
            }
        }

        Canvas.ForceUpdateCanvases();
        if (itemScrollRect != null)
            itemScrollRect.verticalNormalizedPosition = 1f;
    }

    private void RefreshAttributeSection()
    {
        ClearSpawnedObjects(spawnedAttributeObjects);
        attributeButtons.Clear();

        bool showAttributes = currentMode == ReforgeMode.Loot;
        if (attributeSectionTitleText != null)
        {
            attributeSectionTitleText.gameObject.SetActive(showAttributes);
            attributeSectionTitleText.text = "选择目标属性";
        }

        if (attributeScrollRect != null)
            attributeScrollRect.gameObject.SetActive(showAttributes);

        if (!showAttributes)
            return;

        IReadOnlyList<LootAttribute_SO> attributes = GetResolvedAttributeOptions();
        for (int i = 0; i < attributes.Count; i++)
        {
            LootAttribute_SO attribute = attributes[i];
            if (attribute == null)
                continue;

            LootAttribute_SO localAttribute = attribute;
            Button button = CreateListButton(
                attributeListContent,
                GetLootAttributeDisplayName(attribute),
                () => HandleLootAttributeSelection(localAttribute));
            ApplySelectionButtonVisual(button, localAttribute == selectedLootAttribute);
            attributeButtons.Add(button);
            spawnedAttributeObjects.Add(button.gameObject);
        }

        Canvas.ForceUpdateCanvases();
        if (attributeScrollRect != null)
            attributeScrollRect.verticalNormalizedPosition = 1f;
    }

    private void RefreshSummaryAndButtons()
    {
        if (titleText != null)
            titleText.text = "重铸";
        if (remainingText != null)
            remainingText.text = $"剩余次数 {Mathf.Max(0, 2 - successfulReforgeCount)}/2";

        if (hintText != null)
        {
            hintText.text = currentMode == ReforgeMode.Loot
                ? "消耗两件同阶级战利品，重铸为一件指定属性的同阶随机战利品。"
                : "消耗两件同阶级遗物，重铸为一件更高阶的随机遗物。";
        }

        if (selectionSummaryText != null)
            selectionSummaryText.text = BuildSelectionSummary();
        if (resultPreviewText != null)
            resultPreviewText.text = BuildResultPreview();

        if (actionButton != null)
        {
            bool interactable = successfulReforgeCount < 2 && (currentMode == ReforgeMode.Loot
                ? CanExecuteLootReforge()
                : CanExecuteRelicReforge());
            actionButton.interactable = interactable;
            ApplyActionButtonVisual(actionButton, interactable);

            TextMeshProUGUI label = actionButton.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null)
                label.text = currentMode == ReforgeMode.Loot ? "确认重铸战利品" : "确认重铸遗物";
        }

        if (secondaryButton != null)
        {
            TextMeshProUGUI label = secondaryButton.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null)
                label.text = HasCommittedFirstReforge() ? "离开休息场景" : "关闭";
            ApplyActionButtonVisual(secondaryButton, true);
        }
    }

    private bool CanExecuteLootReforge()
    {
        return ValidateLootSelection(out _);
    }

    private bool CanExecuteRelicReforge()
    {
        return ValidateRelicSelection(out _);
    }

    private string BuildSelectionSummary()
    {
        if (currentMode == ReforgeMode.Loot)
        {
            return $"材料一：{GetLootDisplayName(selectedLootA)}\n材料二：{GetLootDisplayName(selectedLootB)}\n目标属性：{GetLootAttributeDisplayName(selectedLootAttribute)}";
        }

        return $"材料一：{GetRelicDisplayName(selectedRelicA)}\n材料二：{GetRelicDisplayName(selectedRelicB)}\n目标结果：更高阶随机遗物";
    }

    private string BuildResultPreview()
    {
        if (currentMode == ReforgeMode.Loot)
        {
            if (selectedLootA == null || selectedLootB == null || selectedLootAttribute == null)
                return "结果预览：选择完成后将显示为同阶、指定属性的随机战利品。";

            LootItem_SO preview = RollLootReforgeResult();
            return preview != null
                ? $"结果预览：可能获得 {GetLootDisplayName(preview)}"
                : "结果预览：当前条件下没有可用结果。";
        }

        if (selectedRelicA == null || selectedRelicB == null)
            return "结果预览：选择完成后将显示为更高阶随机遗物。";

        Relic_SO relicPreview = RollRelicReforgeResult();
        return relicPreview != null
            ? $"结果预览：可能获得 {GetRelicDisplayName(relicPreview)}"
            : "结果预览：当前没有更高阶遗物可供重铸。";
    }

    private void HandleLootSelection(LootItem_SO lootItem)
    {
        if (lootItem == null)
            return;

        if (!RestSceneRuntime.TryGetCurrentSnapshot(out MapRunSnapshot snapshot) || snapshot?.runState == null)
            return;

        if (selectedLootB == lootItem)
        {
            selectedLootB = null;
            RefreshMainPanel();
            return;
        }

        if (selectedLootA == null)
        {
            selectedLootA = lootItem;
            RefreshMainPanel();
            return;
        }

        if (selectedLootA == lootItem && selectedLootB == null)
        {
            if (snapshot.runState.GetLootAmount(lootItem) < 2)
            {
                SetStatus("该战利品数量不足 2，无法重复作为两份材料。", true);
                return;
            }

            selectedLootB = lootItem;
            RefreshMainPanel();
            return;
        }

        if (selectedLootA == lootItem)
        {
            selectedLootA = selectedLootB;
            selectedLootB = null;
            RefreshMainPanel();
            return;
        }

        if (lootItem.rarity != selectedLootA.rarity)
        {
            SetStatus("两件战利品必须为同阶级。", true);
            return;
        }

        selectedLootB = lootItem;
        RefreshMainPanel();
    }

    private void HandleRelicSelection(Relic_SO relicData)
    {
        if (relicData == null)
            return;

        if (!RestSceneRuntime.TryGetCurrentSnapshot(out MapRunSnapshot snapshot) || snapshot?.runState == null)
            return;

        if (selectedRelicB == relicData)
        {
            selectedRelicB = null;
            RefreshMainPanel();
            return;
        }

        if (selectedRelicA == null)
        {
            selectedRelicA = relicData;
            RefreshMainPanel();
            return;
        }

        if (selectedRelicA == relicData && selectedRelicB == null)
        {
            if (snapshot.runState.GetRelicAmount(relicData) < 2)
            {
                SetStatus("该遗物数量不足 2，无法重复作为两份材料。", true);
                return;
            }

            selectedRelicB = relicData;
            RefreshMainPanel();
            return;
        }

        if (selectedRelicA == relicData)
        {
            selectedRelicA = selectedRelicB;
            selectedRelicB = null;
            RefreshMainPanel();
            return;
        }

        if (relicData.rarity != selectedRelicA.rarity)
        {
            SetStatus("两件遗物必须为同阶级。", true);
            return;
        }

        selectedRelicB = relicData;
        RefreshMainPanel();
    }

    private void HandleLootAttributeSelection(LootAttribute_SO attribute)
    {
        selectedLootAttribute = selectedLootAttribute == attribute ? null : attribute;
        RefreshMainPanel();
    }

    private bool HasCommittedFirstReforge()
    {
        return successfulReforgeCount > 0;
    }

    private void ClearCurrentModeSelection()
    {
        if (currentMode == ReforgeMode.Loot)
        {
            selectedLootA = null;
            selectedLootB = null;
            selectedLootAttribute = null;
            return;
        }

        selectedRelicA = null;
        selectedRelicB = null;
    }

    private List<RunLootState> GetEligibleLootInventory(RunState runState)
    {
        List<RunLootState> items = new List<RunLootState>();
        if (runState == null || runState.lootInventory == null)
            return items;

        for (int i = 0; i < runState.lootInventory.Count; i++)
        {
            RunLootState stack = runState.lootInventory[i];
            if (stack == null || stack.lootItem == null || stack.amount <= 0)
                continue;

            items.Add(stack.Clone());
        }

        items.Sort((left, right) =>
        {
            int rarityCompare = left.lootItem.rarity.CompareTo(right.lootItem.rarity);
            if (rarityCompare != 0)
                return rarityCompare;

            return string.Compare(GetLootDisplayName(left.lootItem), GetLootDisplayName(right.lootItem), StringComparison.OrdinalIgnoreCase);
        });

        return items;
    }

    private List<RunRelicState> GetEligibleRelicInventory(RunState runState)
    {
        List<RunRelicState> relics = new List<RunRelicState>();
        if (runState == null || runState.relicInventory == null)
            return relics;

        for (int i = 0; i < runState.relicInventory.Count; i++)
        {
            RunRelicState stack = runState.relicInventory[i];
            if (stack == null || stack.relicData == null || stack.amount <= 0)
                continue;

            if (stack.relicData.rarity >= LootRarity.Legendary)
                continue;

            relics.Add(stack.Clone());
        }

        relics.Sort((left, right) =>
        {
            int rarityCompare = left.relicData.rarity.CompareTo(right.relicData.rarity);
            if (rarityCompare != 0)
                return rarityCompare;

            return string.Compare(GetRelicDisplayName(left.relicData), GetRelicDisplayName(right.relicData), StringComparison.OrdinalIgnoreCase);
        });

        return relics;
    }

    private IReadOnlyList<LootAttribute_SO> GetResolvedAttributeOptions()
    {
        List<LootAttribute_SO> attributes = new List<LootAttribute_SO>();
        HashSet<LootAttribute_SO> seen = new HashSet<LootAttribute_SO>();

        if (lootAttributeOptions != null)
        {
            for (int i = 0; i < lootAttributeOptions.Count; i++)
            {
                LootAttribute_SO attribute = lootAttributeOptions[i];
                if (attribute == null || !seen.Add(attribute))
                    continue;

                attributes.Add(attribute);
            }
        }

        if (attributes.Count > 0)
            return attributes;

        IReadOnlyList<LootItem_SO> lootPool = GetResolvedLootReforgePool();
        for (int i = 0; i < lootPool.Count; i++)
        {
            LootItem_SO item = lootPool[i];
            if (item == null || item.attribute == null || !seen.Add(item.attribute))
                continue;

            attributes.Add(item.attribute);
        }

        attributes.Sort((left, right) => string.Compare(GetLootAttributeDisplayName(left), GetLootAttributeDisplayName(right), StringComparison.OrdinalIgnoreCase));
        return attributes;
    }

    private IReadOnlyList<LootItem_SO> GetResolvedLootReforgePool()
    {
        return lootReforgeTable != null && lootReforgeTable.IsValid
            ? lootReforgeTable.LootItems
            : Array.Empty<LootItem_SO>();
    }

    private IReadOnlyList<Relic_SO> GetResolvedRelicReforgePool()
    {
        return relicReforgeTable != null && relicReforgeTable.IsValid
            ? relicReforgeTable.Relics
            : Array.Empty<Relic_SO>();
    }

    private void EnsureOutlineRenderer()
    {
        targetSpriteRenderer = GetComponent<SpriteRenderer>();
        if (targetSpriteRenderer == null)
            targetSpriteRenderer = GetComponentInChildren<SpriteRenderer>(true);

        if (targetSpriteRenderer == null)
            return;

        Transform outlineTransform = targetSpriteRenderer.transform.Find("HoverOutline");
        if (outlineTransform != null)
            outlineRenderer = outlineTransform.GetComponent<SpriteRenderer>();

        if (outlineRenderer == null)
        {
            GameObject outlineObject = new GameObject("HoverOutline", typeof(SpriteRenderer));
            outlineObject.transform.SetParent(targetSpriteRenderer.transform, false);
            outlineRenderer = outlineObject.GetComponent<SpriteRenderer>();
        }

        outlineRenderer.enabled = false;
        outlineRenderer.sharedMaterial = EnsureOutlineMaterial();
        RefreshOutlineRendererVisuals();
    }

    private void RefreshOutlineRendererVisuals()
    {
        if (targetSpriteRenderer == null || outlineRenderer == null)
            return;

        Material outlineMaterial = EnsureOutlineMaterial();
        outlineRenderer.sprite = targetSpriteRenderer.sprite;
        outlineRenderer.color = outlineColor;
        outlineRenderer.flipX = targetSpriteRenderer.flipX;
        outlineRenderer.flipY = targetSpriteRenderer.flipY;
        outlineRenderer.drawMode = targetSpriteRenderer.drawMode;
        outlineRenderer.size = targetSpriteRenderer.size;
        outlineRenderer.maskInteraction = targetSpriteRenderer.maskInteraction;
        outlineRenderer.sortingLayerID = targetSpriteRenderer.sortingLayerID;
        outlineRenderer.sortingOrder = targetSpriteRenderer.sortingOrder + outlineSortingOrderOffset;
        outlineRenderer.transform.localPosition = outlineOffset;
        outlineRenderer.transform.localRotation = Quaternion.identity;
        outlineRenderer.transform.localScale = Vector3.one;
        outlineRenderer.sharedMaterial = outlineMaterial;

        if (outlineMaterial != null)
            outlineMaterial.SetFloat(OutlineWidthPropertyName, Mathf.Max(0f, outlineWidth));
    }

    private Material EnsureOutlineMaterial()
    {
        if (runtimeOutlineMaterial != null)
            return runtimeOutlineMaterial;

        Shader shader = Resources.Load<Shader>(OutlineShaderResourcePath);
        if (shader == null)
            shader = Shader.Find(OutlineShaderName);

        if (shader == null)
        {
            Debug.LogWarning($"{nameof(RestSceneReforgeInteractable)} could not find outline shader '{OutlineShaderName}'.", this);
            return null;
        }

        runtimeOutlineMaterial = new Material(shader)
        {
            name = "Runtime_RestSceneReforgeOutlineMaterial"
        };
        runtimeOutlineMaterial.SetFloat(OutlineWidthPropertyName, Mathf.Max(0f, outlineWidth));
        return runtimeOutlineMaterial;
    }

    private void UpdateTouchHighlight()
    {
        bool shouldHighlight = false;
        if (interactionCollider != null && !promptVisible && !mainPanelVisible && (!RestSceneModalState.IsLocked || RestSceneModalState.IsLockedBy(this)))
        {
            inputCamera = inputCamera != null ? inputCamera : Camera.main;
            if (inputCamera != null && Input.touchCount > 0)
            {
                for (int i = 0; i < Input.touchCount; i++)
                {
                    Touch touch = Input.GetTouch(i);
                    if (touch.phase == TouchPhase.Canceled || touch.phase == TouchPhase.Ended)
                        continue;

                    Vector3 worldPoint = inputCamera.ScreenToWorldPoint(touch.position);
                    worldPoint.z = transform.position.z;
                    if (interactionCollider.OverlapPoint(worldPoint))
                    {
                        shouldHighlight = true;
                        break;
                    }
                }
            }
        }

        if (touchHighlighting == shouldHighlight)
            return;

        touchHighlighting = shouldHighlight;
        RefreshHighlightState();
    }

    private void RefreshHighlightState()
    {
        if (outlineRenderer == null)
            return;

        bool visible = (mouseHovering || touchHighlighting) && !promptVisible && !mainPanelVisible;
        outlineRenderer.enabled = visible;
    }

    private void EnsureUi()
    {
        EventSystemUtility.EnsureSingleEventSystem();
        if (canvas != null)
            return;

        GameObject canvasObject = new GameObject("RestReforgeCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);

        canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 3100;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasRect = canvasObject.GetComponent<RectTransform>();
        canvasRect.anchorMin = Vector2.zero;
        canvasRect.anchorMax = Vector2.one;
        canvasRect.offsetMin = Vector2.zero;
        canvasRect.offsetMax = Vector2.zero;

        BuildPromptPanel();
        BuildMainPanel();

        canvas.gameObject.SetActive(false);
    }

    private void BuildPromptPanel()
    {
        promptPanelImage = CreateImage("PromptPanel", canvasRect, out promptPanelRect);
        promptPanelRect.anchorMin = new Vector2(0.5f, 0.5f);
        promptPanelRect.anchorMax = new Vector2(0.5f, 0.5f);
        promptPanelRect.pivot = new Vector2(0.5f, 0.5f);

        TextMeshProUGUI promptTitle = CreateText("PromptTitle", promptPanelRect, titleFont, titleFontSize, titleColor, TextAlignmentOptions.Center);
        RectTransform promptTitleRect = promptTitle.rectTransform;
        promptTitleRect.anchorMin = new Vector2(0.5f, 1f);
        promptTitleRect.anchorMax = new Vector2(0.5f, 1f);
        promptTitleRect.pivot = new Vector2(0.5f, 1f);
        promptTitleRect.anchoredPosition = new Vector2(0f, -34f);
        promptTitleRect.sizeDelta = new Vector2(620f, 68f);
        promptTitle.text = promptTitleText;

        TextMeshProUGUI promptBody = CreateText("PromptBody", promptPanelRect, bodyFont, bodyFontSize, bodyColor, TextAlignmentOptions.Center);
        promptBody.enableWordWrapping = true;
        RectTransform promptBodyRect = promptBody.rectTransform;
        promptBodyRect.anchorMin = new Vector2(0.5f, 0.5f);
        promptBodyRect.anchorMax = new Vector2(0.5f, 0.5f);
        promptBodyRect.pivot = new Vector2(0.5f, 0.5f);
        promptBodyRect.anchoredPosition = new Vector2(0f, 20f);
        promptBodyRect.sizeDelta = new Vector2(620f, 150f);
        promptBody.text = promptBodyText;

        promptConfirmButton = CreateActionButton("PromptConfirmButton", promptPanelRect, promptConfirmButtonSprite, promptConfirmLabel, HandlePromptConfirm);
        RectTransform promptConfirmRect = promptConfirmButton.GetComponent<RectTransform>();
        promptConfirmRect.anchorMin = new Vector2(0.5f, 0f);
        promptConfirmRect.anchorMax = new Vector2(0.5f, 0f);
        promptConfirmRect.pivot = new Vector2(0.5f, 0f);

        promptCancelButton = CreateActionButton("PromptCancelButton", promptPanelRect, promptCancelButtonSprite, promptCancelLabel, () => HideAllPanels(true));
        RectTransform promptCancelRect = promptCancelButton.GetComponent<RectTransform>();
        promptCancelRect.anchorMin = new Vector2(0.5f, 0f);
        promptCancelRect.anchorMax = new Vector2(0.5f, 0f);
        promptCancelRect.pivot = new Vector2(0.5f, 0f);

        ApplyPromptLayout();
    }

    private void ApplyPromptLayout()
    {
        if (promptPanelRect == null || promptPanelImage == null)
            return;

        promptPanelRect.sizeDelta = promptSize;
        promptPanelRect.anchoredPosition = promptOffset;
        promptPanelImage.sprite = promptBackgroundSprite;
        promptPanelImage.type = promptBackgroundSprite != null ? Image.Type.Sliced : Image.Type.Simple;
        promptPanelImage.color = promptBackgroundColor;

        RectTransform confirmRect = promptConfirmButton.GetComponent<RectTransform>();
        confirmRect.sizeDelta = bottomButtonSize;
        confirmRect.anchoredPosition = new Vector2(-(bottomButtonSize.x * 0.5f + 18f), 28f);

        RectTransform cancelRect = promptCancelButton.GetComponent<RectTransform>();
        cancelRect.sizeDelta = bottomButtonSize;
        cancelRect.anchoredPosition = new Vector2(bottomButtonSize.x * 0.5f + 18f, 28f);
    }

    private void BuildMainPanel()
    {
        mainPanelImage = CreateImage("MainPanel", canvasRect, out mainPanelRect);
        mainPanelRect.anchorMin = new Vector2(0.5f, 0.5f);
        mainPanelRect.anchorMax = new Vector2(0.5f, 0.5f);
        mainPanelRect.pivot = new Vector2(0.5f, 0.5f);
        mainPanelRect.sizeDelta = panelSize;
        mainPanelRect.anchoredPosition = panelOffset;
        mainPanelImage.sprite = panelBackgroundSprite;
        mainPanelImage.type = panelBackgroundSprite != null ? Image.Type.Sliced : Image.Type.Simple;
        mainPanelImage.color = panelBackgroundColor;

        titleText = CreateText("Title", mainPanelRect, titleFont, titleFontSize, titleColor, TextAlignmentOptions.Left);
        SetAnchoredRect(titleText.rectTransform, new Vector2(30f, -24f), new Vector2(320f, 60f));

        remainingText = CreateText("RemainingText", mainPanelRect, titleFont, bodyFontSize, accentColor, TextAlignmentOptions.Right);
        SetAnchoredRect(remainingText.rectTransform, new Vector2(panelSize.x - 330f, -28f), new Vector2(300f, 48f));

        hintText = CreateText("HintText", mainPanelRect, bodyFont, bodyFontSize - 2, bodyColor, TextAlignmentOptions.Left);
        hintText.enableWordWrapping = true;
        SetAnchoredRect(hintText.rectTransform, new Vector2(30f, -94f), new Vector2(760f, 86f));

        lootModeButton = CreateActionButton("LootModeButton", mainPanelRect, actionButtonSprite, "战利品重铸", HandleLootModeClicked);
        RectTransform lootModeRect = lootModeButton.GetComponent<RectTransform>();
        lootModeRect.anchorMin = new Vector2(0f, 1f);
        lootModeRect.anchorMax = new Vector2(0f, 1f);
        lootModeRect.pivot = new Vector2(0f, 1f);
        lootModeRect.anchoredPosition = new Vector2(30f, -190f);
        lootModeRect.sizeDelta = modeButtonSize;

        relicModeButton = CreateActionButton("RelicModeButton", mainPanelRect, actionButtonSprite, "遗物重铸", HandleRelicModeClicked);
        RectTransform relicModeRect = relicModeButton.GetComponent<RectTransform>();
        relicModeRect.anchorMin = new Vector2(0f, 1f);
        relicModeRect.anchorMax = new Vector2(0f, 1f);
        relicModeRect.pivot = new Vector2(0f, 1f);
        relicModeRect.anchoredPosition = new Vector2(30f + modeButtonSize.x + modeButtonSpacing, -190f);
        relicModeRect.sizeDelta = modeButtonSize;

        itemSectionTitleText = CreateText("ItemSectionTitle", mainPanelRect, titleFont, bodyFontSize, accentColor, TextAlignmentOptions.Left);
        SetAnchoredRect(itemSectionTitleText.rectTransform, new Vector2(30f, -270f), new Vector2(360f, 42f));

        RectTransform itemSectionRect = CreateSection(mainPanelRect, "ItemListSection", new Vector2(30f, -320f), itemListSize);
        itemScrollRect = CreateScrollSection(itemSectionRect, out itemListContent);

        attributeSectionTitleText = CreateText("AttributeSectionTitle", mainPanelRect, titleFont, bodyFontSize, accentColor, TextAlignmentOptions.Left);
        SetAnchoredRect(attributeSectionTitleText.rectTransform, new Vector2(500f, -270f), new Vector2(260f, 42f));

        RectTransform attributeSectionRect = CreateSection(mainPanelRect, "AttributeSection", new Vector2(500f, -320f), attributeListSize);
        attributeScrollRect = CreateScrollSection(attributeSectionRect, out attributeListContent);

        selectionSummaryText = CreateText("SelectionSummary", mainPanelRect, bodyFont, bodyFontSize, bodyColor, TextAlignmentOptions.TopLeft);
        selectionSummaryText.enableWordWrapping = true;
        SetAnchoredRect(selectionSummaryText.rectTransform, new Vector2(790f, -270f), new Vector2(430f, 140f));

        resultPreviewText = CreateText("ResultPreview", mainPanelRect, bodyFont, bodyFontSize - 2, accentColor, TextAlignmentOptions.TopLeft);
        resultPreviewText.enableWordWrapping = true;
        SetAnchoredRect(resultPreviewText.rectTransform, new Vector2(790f, -442f), new Vector2(430f, 110f));

        statusText = CreateText("StatusText", mainPanelRect, bodyFont, bodyFontSize - 2, statusColor, TextAlignmentOptions.TopLeft);
        statusText.enableWordWrapping = true;
        SetAnchoredRect(statusText.rectTransform, new Vector2(790f, -530f), new Vector2(430f, 108f));

        actionButton = CreateActionButton("ActionButton", mainPanelRect, actionButtonSprite, "确认重铸", HandleActionButtonClicked);
        RectTransform actionRect = actionButton.GetComponent<RectTransform>();
        actionRect.anchorMin = new Vector2(1f, 0f);
        actionRect.anchorMax = new Vector2(1f, 0f);
        actionRect.pivot = new Vector2(1f, 0f);
        actionRect.anchoredPosition = new Vector2(-286f, 28f);
        actionRect.sizeDelta = bottomButtonSize;

        secondaryButton = CreateActionButton("SecondaryButton", mainPanelRect, actionButtonSprite, "关闭", HandleSecondaryButtonClicked);
        RectTransform secondaryRect = secondaryButton.GetComponent<RectTransform>();
        secondaryRect.anchorMin = new Vector2(1f, 0f);
        secondaryRect.anchorMax = new Vector2(1f, 0f);
        secondaryRect.pivot = new Vector2(1f, 0f);
        secondaryRect.anchoredPosition = new Vector2(-30f, 28f);
        secondaryRect.sizeDelta = bottomButtonSize;
    }

    private RectTransform CreateSection(RectTransform parent, string name, Vector2 anchoredPosition, Vector2 size)
    {
        Image image = CreateImage(name, parent, out RectTransform rect);
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
        image.sprite = sectionBackgroundSprite;
        image.type = sectionBackgroundSprite != null ? Image.Type.Sliced : Image.Type.Simple;
        image.color = sectionBackgroundColor;
        return rect;
    }

    private ScrollRect CreateScrollSection(RectTransform parent, out RectTransform contentRoot)
    {
        GameObject scrollObject = new GameObject("ScrollRect", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
        scrollObject.transform.SetParent(parent, false);

        RectTransform scrollRectTransform = scrollObject.GetComponent<RectTransform>();
        scrollRectTransform.anchorMin = Vector2.zero;
        scrollRectTransform.anchorMax = Vector2.one;
        scrollRectTransform.offsetMin = new Vector2(10f, 10f);
        scrollRectTransform.offsetMax = new Vector2(-10f, -10f);

        Image scrollImage = scrollObject.GetComponent<Image>();
        scrollImage.color = Color.clear;

        ScrollRect scrollRect = scrollObject.GetComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 24f;

        GameObject viewportObject = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
        viewportObject.transform.SetParent(scrollRectTransform, false);
        RectTransform viewportRect = viewportObject.GetComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = Vector2.zero;
        viewportRect.offsetMax = new Vector2(-20f, 0f);
        Image viewportImage = viewportObject.GetComponent<Image>();
        viewportImage.color = Color.clear;

        GameObject contentObject = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        contentObject.transform.SetParent(viewportRect, false);
        contentRoot = contentObject.GetComponent<RectTransform>();
        contentRoot.anchorMin = new Vector2(0f, 1f);
        contentRoot.anchorMax = new Vector2(1f, 1f);
        contentRoot.pivot = new Vector2(0.5f, 1f);
        contentRoot.anchoredPosition = Vector2.zero;
        contentRoot.sizeDelta = Vector2.zero;

        VerticalLayoutGroup layout = contentObject.GetComponent<VerticalLayoutGroup>();
        layout.spacing = listButtonSpacing;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlHeight = false;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;
        layout.padding = new RectOffset(0, 0, 0, 0);

        ContentSizeFitter fitter = contentObject.GetComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        GameObject scrollbarObject = new GameObject("Scrollbar", typeof(RectTransform), typeof(Image), typeof(Scrollbar));
        scrollbarObject.transform.SetParent(scrollRectTransform, false);
        RectTransform scrollbarRect = scrollbarObject.GetComponent<RectTransform>();
        scrollbarRect.anchorMin = new Vector2(1f, 0f);
        scrollbarRect.anchorMax = new Vector2(1f, 1f);
        scrollbarRect.pivot = new Vector2(1f, 1f);
        scrollbarRect.offsetMin = new Vector2(-18f, 0f);
        scrollbarRect.offsetMax = Vector2.zero;
        Image scrollbarImage = scrollbarObject.GetComponent<Image>();
        scrollbarImage.color = new Color(0.2f, 0.24f, 0.3f, 0.92f);

        GameObject handleObject = new GameObject("Handle", typeof(RectTransform), typeof(Image));
        handleObject.transform.SetParent(scrollbarRect, false);
        RectTransform handleRect = handleObject.GetComponent<RectTransform>();
        handleRect.anchorMin = Vector2.zero;
        handleRect.anchorMax = Vector2.one;
        handleRect.offsetMin = Vector2.zero;
        handleRect.offsetMax = Vector2.zero;
        Image handleImage = handleObject.GetComponent<Image>();
        handleImage.color = accentColor;

        Scrollbar scrollbar = scrollbarObject.GetComponent<Scrollbar>();
        scrollbar.direction = Scrollbar.Direction.BottomToTop;
        scrollbar.targetGraphic = handleImage;
        scrollbar.handleRect = handleRect;

        scrollRect.viewport = viewportRect;
        scrollRect.content = contentRoot;
        scrollRect.verticalScrollbar = scrollbar;
        scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
        return scrollRect;
    }

    private Button CreateActionButton(string name, RectTransform parent, Sprite sprite, string label, UnityEngine.Events.UnityAction onClick)
    {
        GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        Image image = buttonObject.GetComponent<Image>();
        image.sprite = sprite;
        image.type = sprite != null ? Image.Type.Sliced : Image.Type.Simple;
        image.color = actionButtonColor;

        Button button = buttonObject.GetComponent<Button>();
        button.onClick.AddListener(onClick);

        TextMeshProUGUI labelText = CreateText("Label", rect, buttonFont, buttonFontSize, normalButtonTextColor, TextAlignmentOptions.Center);
        RectTransform labelRect = labelText.rectTransform;
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
        labelText.text = label;
        return button;
    }

    private Button CreateListButton(RectTransform parent, string label, UnityEngine.Events.UnityAction onClick)
    {
        Button button = CreateActionButton($"ListButton_{parent.childCount}", parent, actionButtonSprite, label, onClick);
        LayoutElement layoutElement = button.gameObject.AddComponent<LayoutElement>();
        layoutElement.preferredHeight = listButtonHeight;
        layoutElement.minHeight = listButtonHeight;
        return button;
    }

    private void ApplySelectionButtonVisual(Button button, bool selected)
    {
        if (button == null)
            return;

        Image image = button.GetComponent<Image>();
        if (image != null)
        {
            image.sprite = selected && selectedButtonSprite != null ? selectedButtonSprite : actionButtonSprite;
            image.type = image.sprite != null ? Image.Type.Sliced : Image.Type.Simple;
            image.color = selected ? selectedButtonColor : actionButtonColor;
        }

        TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>(true);
        if (label != null)
            label.color = selected ? selectedButtonTextColor : normalButtonTextColor;
    }

    private void ApplyActionButtonVisual(Button button, bool interactable)
    {
        if (button == null)
            return;

        button.interactable = interactable;
        Image image = button.GetComponent<Image>();
        if (image != null)
            image.color = interactable ? actionButtonColor : disabledButtonColor;
    }

    private void SetPromptVisible(bool visible)
    {
        promptVisible = visible;
        if (promptPanelRect != null)
            promptPanelRect.gameObject.SetActive(visible);
        UpdateCanvasVisibility();
        RefreshHighlightState();
    }

    private void SetMainPanelVisible(bool visible)
    {
        mainPanelVisible = visible;
        if (mainPanelRect != null)
            mainPanelRect.gameObject.SetActive(visible);
        UpdateCanvasVisibility();
        RefreshHighlightState();
    }

    private void UpdateCanvasVisibility()
    {
        if (canvas != null)
            canvas.gameObject.SetActive(promptVisible || mainPanelVisible);
    }

    private void ReleaseModalLock()
    {
        if (!ownsModalLock)
            return;

        RestSceneModalState.Release(this);
        ownsModalLock = false;
    }

    private void SetStatus(string message, bool isError)
    {
        if (statusText != null)
        {
            statusText.color = isError ? errorColor : statusColor;
            statusText.text = message ?? string.Empty;
        }
    }

    private static void ClearSpawnedObjects(List<GameObject> objects)
    {
        if (objects == null)
            return;

        for (int i = 0; i < objects.Count; i++)
        {
            GameObject obj = objects[i];
            if (obj == null)
                continue;

            obj.transform.SetParent(null, false);

            if (Application.isPlaying)
                UnityEngine.Object.Destroy(obj);
            else
                UnityEngine.Object.DestroyImmediate(obj);
        }

        objects.Clear();
    }

    private static void SetAnchoredRect(RectTransform rect, Vector2 anchoredPosition, Vector2 size)
    {
        if (rect == null)
            return;

        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
    }

    private static Image CreateImage(string name, RectTransform parent, out RectTransform rect)
    {
        GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(Image));
        imageObject.transform.SetParent(parent, false);
        Image image = imageObject.GetComponent<Image>();
        rect = imageObject.GetComponent<RectTransform>();
        return image;
    }

    private static TextMeshProUGUI CreateText(string name, RectTransform parent, TMP_FontAsset font, int fontSize, Color color, TextAlignmentOptions alignment)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);
        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        if (font != null)
            text.font = font;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = alignment;
        text.raycastTarget = false;
        return text;
    }

    private static string BuildLootButtonLabel(RunLootState stack)
    {
        if (stack == null || stack.lootItem == null)
            return "未知战利品";

        return $"{GetLootDisplayName(stack.lootItem)}\n{GetRarityLabel(stack.lootItem.rarity)} | x{Mathf.Max(0, stack.amount)}";
    }

    private static string BuildRelicButtonLabel(RunRelicState stack)
    {
        if (stack == null || stack.relicData == null)
            return "未知遗物";

        return $"{GetRelicDisplayName(stack.relicData)}\n{GetRarityLabel(stack.relicData.rarity)} | x{Mathf.Max(0, stack.amount)}";
    }

    private static string GetLootDisplayName(LootItem_SO lootItem)
    {
        if (lootItem == null)
            return "未选择";

        return !string.IsNullOrWhiteSpace(lootItem.lootName) ? lootItem.lootName : lootItem.name;
    }

    private static string GetRelicDisplayName(Relic_SO relicData)
    {
        if (relicData == null)
            return "未选择";

        return !string.IsNullOrWhiteSpace(relicData.DisplayName) ? relicData.DisplayName : relicData.name;
    }

    private static string GetLootAttributeDisplayName(LootAttribute_SO attribute)
    {
        if (attribute == null)
            return "未选择";

        return !string.IsNullOrWhiteSpace(attribute.attributeName) ? attribute.attributeName : attribute.name;
    }

    private static string GetRarityLabel(LootRarity rarity)
    {
        switch (rarity)
        {
            case LootRarity.Uncommon:
                return "优秀";
            case LootRarity.Rare:
                return "稀有";
            case LootRarity.Epic:
                return "史诗";
            case LootRarity.Legendary:
                return "传说";
            case LootRarity.Common:
            default:
                return "普通";
        }
    }
}
