using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class MapCraftingOverlay : MonoBehaviour
{
    public event Action Hidden;

    [Header("Font")]
    [SerializeField] private TMP_FontAsset keywordFont;
    [SerializeField] private TMP_FontAsset bodyFont;
    [SerializeField] private int titleFontSize = 34;
    [SerializeField] private int bodyFontSize = 24;
    [SerializeField] private int buttonFontSize = 24;

    [Header("Panel")]
    [SerializeField] private Vector2 panelSize = new Vector2(900f, 560f);
    [SerializeField] private Sprite panelBackgroundSprite;
    [SerializeField] private Color panelBackgroundColor = new Color(0.08f, 0.1f, 0.14f, 0.92f);
    [SerializeField] private Vector2 panelOffset = Vector2.zero;

    [Header("Blueprint List")]
    [SerializeField] private Vector2 blueprintButtonSize = new Vector2(240f, 64f);
    [SerializeField] private float blueprintButtonSpacing = 12f;
    [SerializeField] private Sprite blueprintButtonSprite;
    [SerializeField] private Sprite selectedBlueprintButtonSprite;
    [SerializeField] private Color blueprintButtonColor = new Color(0.18f, 0.16f, 0.12f, 0.96f);
    [SerializeField] private Color selectedBlueprintButtonColor = new Color(1f, 0.83f, 0.35f, 1f);
    [SerializeField] private Color blueprintButtonTextColor = Color.white;
    [SerializeField] private Color selectedBlueprintButtonTextColor = Color.black;

    [Header("Buttons")]
    [SerializeField] private Sprite actionButtonSprite;
    [SerializeField] private Vector2 actionButtonSize = new Vector2(180f, 56f);
    [SerializeField] private Color actionButtonColor = new Color(1f, 0.83f, 0.35f, 1f);
    [SerializeField] private Color disabledButtonColor = new Color(0.4f, 0.4f, 0.4f, 0.9f);
    [SerializeField] private Color actionButtonTextColor = Color.black;
    [SerializeField] private string craftButtonLabel = "锻造";
    [SerializeField] private string closeButtonLabel = "关闭";

    [Header("Detail")]
    [SerializeField] private Vector2 detailOffset = new Vector2(290f, -72f);
    [SerializeField] private Vector2 detailSize = new Vector2(540f, 340f);
    [SerializeField] private Vector2 detailIconSize = new Vector2(96f, 96f);
    [SerializeField] private Color titleColor = Color.white;
    [SerializeField] private Color bodyColor = new Color(0.92f, 0.96f, 1f, 1f);
    [SerializeField] private Color accentColor = new Color(1f, 0.83f, 0.35f, 1f);
    [SerializeField] private Color enoughMaterialColor = new Color(0.58f, 1f, 0.58f, 1f);
    [SerializeField] private Color lackingMaterialColor = new Color(1f, 0.48f, 0.48f, 1f);

    private Canvas canvas;
    private RectTransform root;
    private RectTransform panelRect;
    private Image panelImage;
    private RectTransform listRoot;
    private TextMeshProUGUI titleText;
    private TextMeshProUGUI weaponNameText;
    private TextMeshProUGUI metaText;
    private TextMeshProUGUI descriptionText;
    private TextMeshProUGUI materialsText;
    private TextMeshProUGUI statusText;
    private Image iconImage;
    private Button craftButton;
    private Button closeButton;
    private readonly List<Button> blueprintButtons = new List<Button>();
    private readonly List<GameObject> spawnedBlueprintObjects = new List<GameObject>();

    private RunState currentRunState;
    private CraftingBlueprint_SO selectedBlueprint;
    private Func<CraftingBlueprint_SO, string> craftHandler;

    public bool IsVisible =>
        panelRect != null &&
        panelRect.gameObject.activeInHierarchy;

    public void ConfigureFonts(TMP_FontAsset keyword, TMP_FontAsset body)
    {
        if (keyword != null)
            keywordFont = keyword;
        if (body != null)
            bodyFont = body;

        ApplyFonts();
    }

    public void Show(RunState runState, Func<CraftingBlueprint_SO, string> onCraft)
    {
        EnsureUi();
        currentRunState = runState;
        craftHandler = onCraft;
        gameObject.SetActive(true);

        RefreshBlueprintList();
        SelectBlueprint(selectedBlueprint != null ? selectedBlueprint : GetFirstBlueprint());
    }

    public void Hide()
    {
        if (panelRect != null)
            panelRect.gameObject.SetActive(false);
        Hidden?.Invoke();
    }

    private void EnsureUi()
    {
        if (canvas != null)
        {
            panelRect.gameObject.SetActive(true);
            return;
        }

        GameObject canvasGo = new GameObject("CraftingOverlayCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.transform.SetParent(transform, false);
        canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9050;

        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        root = canvasGo.GetComponent<RectTransform>();
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.offsetMin = Vector2.zero;
        root.offsetMax = Vector2.zero;

        panelRect = CreatePanel(root, "Panel", new Vector2(0.5f, 0.5f), panelOffset, panelSize);
        panelImage = panelRect.GetComponent<Image>();
        ApplyPanelVisual();

        titleText = CreateText(panelRect, "Title", new Vector2(26f, -18f), new Vector2(320f, 42f), titleFontSize, titleColor, "锻造", true);

        listRoot = new GameObject("BlueprintList", typeof(RectTransform)).GetComponent<RectTransform>();
        listRoot.SetParent(panelRect, false);
        listRoot.anchorMin = new Vector2(0f, 1f);
        listRoot.anchorMax = new Vector2(0f, 1f);
        listRoot.pivot = new Vector2(0f, 1f);
        listRoot.anchoredPosition = new Vector2(24f, -78f);
        listRoot.sizeDelta = new Vector2(blueprintButtonSize.x, 420f);

        RectTransform detailRoot = new GameObject("DetailRoot", typeof(RectTransform)).GetComponent<RectTransform>();
        detailRoot.SetParent(panelRect, false);
        detailRoot.anchorMin = new Vector2(0f, 1f);
        detailRoot.anchorMax = new Vector2(0f, 1f);
        detailRoot.pivot = new Vector2(0f, 1f);
        detailRoot.anchoredPosition = detailOffset;
        detailRoot.sizeDelta = detailSize;

        iconImage = BuildIcon(detailRoot);
        weaponNameText = CreateText(detailRoot, "WeaponName", new Vector2(122f, -8f), new Vector2(390f, 42f), titleFontSize - 4, titleColor, "未选择图纸", true);
        metaText = CreateText(detailRoot, "Meta", new Vector2(122f, -56f), new Vector2(390f, 34f), bodyFontSize, accentColor, string.Empty, true);
        descriptionText = CreateText(detailRoot, "Description", new Vector2(0f, -128f), new Vector2(detailSize.x - 12f, 84f), bodyFontSize, bodyColor, string.Empty);
        descriptionText.enableWordWrapping = true;
        descriptionText.overflowMode = TextOverflowModes.Overflow;
        materialsText = CreateText(detailRoot, "Materials", new Vector2(0f, -226f), new Vector2(detailSize.x - 12f, 108f), bodyFontSize, bodyColor, string.Empty);
        materialsText.enableWordWrapping = true;
        materialsText.overflowMode = TextOverflowModes.Overflow;
        statusText = CreateText(detailRoot, "Status", new Vector2(0f, -340f), new Vector2(detailSize.x - 12f, 36f), bodyFontSize - 2, accentColor, string.Empty);

        craftButton = BuildActionButton(panelRect, "CraftButton", new Vector2(-208f, 24f), craftButtonLabel, OnCraftClicked);
        closeButton = BuildActionButton(panelRect, "CloseButton", new Vector2(-24f, 24f), closeButtonLabel, Hide);

        ApplyFonts();
        panelRect.gameObject.SetActive(true);
    }

    private void RefreshBlueprintList()
    {
        for (int i = 0; i < spawnedBlueprintObjects.Count; i++)
            Destroy(spawnedBlueprintObjects[i]);
        spawnedBlueprintObjects.Clear();
        blueprintButtons.Clear();

        if (currentRunState?.blueprintInventory == null)
            return;

        float y = 0f;
        for (int i = 0; i < currentRunState.blueprintInventory.Count; i++)
        {
            RunBlueprintState blueprintState = currentRunState.blueprintInventory[i];
            if (blueprintState == null || blueprintState.blueprintData == null || blueprintState.amount <= 0)
                continue;

            Button button = BuildBlueprintButton(blueprintState, new Vector2(0f, -y));
            blueprintButtons.Add(button);
            spawnedBlueprintObjects.Add(button.gameObject);
            y += blueprintButtonSize.y + blueprintButtonSpacing;
        }
    }

    private void SelectBlueprint(CraftingBlueprint_SO blueprint)
    {
        selectedBlueprint = blueprint;
        RefreshBlueprintButtonVisuals();
        RefreshDetail();
    }

    private void RefreshDetail()
    {
        if (selectedBlueprint == null)
        {
            if (iconImage != null)
                iconImage.sprite = null;
            if (weaponNameText != null)
                weaponNameText.text = "未选择图纸";
            if (metaText != null)
                metaText.text = string.Empty;
            if (descriptionText != null)
                descriptionText.text = "请选择一张图纸来查看可锻造武器与材料需求。";
            if (materialsText != null)
                materialsText.text = string.Empty;
            SetCraftButtonInteractable(false);
            return;
        }

        if (iconImage != null)
        {
            iconImage.sprite = selectedBlueprint.DisplayIcon;
            iconImage.preserveAspect = true;
        }

        if (weaponNameText != null)
            weaponNameText.text = selectedBlueprint.targetWeapon != null ? selectedBlueprint.targetWeapon.weaponName : selectedBlueprint.DisplayName;

        if (metaText != null)
        {
            string rarity = GetRarityLabel(selectedBlueprint.rarity);
            string weaponType = selectedBlueprint.targetWeapon != null ? selectedBlueprint.targetWeapon.weaponType.ToString() : "武器";
            metaText.text = $"{rarity} | {weaponType}";
        }

        if (descriptionText != null)
            descriptionText.text = string.IsNullOrWhiteSpace(selectedBlueprint.description) ? "暂无图纸描述。" : selectedBlueprint.description;

        if (materialsText != null)
            materialsText.text = BuildMaterialDescription(selectedBlueprint);

        SetCraftButtonInteractable(CanCraft(selectedBlueprint));
    }

    private string BuildMaterialDescription(CraftingBlueprint_SO blueprint)
    {
        if (blueprint == null || blueprint.materialRequirements == null || blueprint.materialRequirements.Count == 0)
            return "未配置材料需求。";

        StringBuilder builder = new StringBuilder();
        int forgeCost = CalamitySystem.ApplyForgeCostModifiers(0, currentRunState != null ? currentRunState.activeCalamities : null);
        if (forgeCost > 0)
        {
            Color moneyColor = currentRunState != null && currentRunState.money >= forgeCost ? enoughMaterialColor : lackingMaterialColor;
            builder.Append("锻造费用：<color=#");
            builder.Append(ColorUtility.ToHtmlStringRGB(moneyColor));
            builder.Append(">");
            builder.Append(forgeCost);
            builder.AppendLine(" 金币</color>");
        }

        builder.AppendLine("所需材料：");

        for (int i = 0; i < blueprint.materialRequirements.Count; i++)
        {
            CraftingMaterialRequirement requirement = blueprint.materialRequirements[i];
            if (requirement == null || requirement.lootItem == null)
                continue;

            int currentAmount = currentRunState != null ? currentRunState.GetLootAmount(requirement.lootItem) : 0;
            Color displayColor = currentAmount >= requirement.amount ? enoughMaterialColor : lackingMaterialColor;
            builder.Append("<color=#");
            builder.Append(ColorUtility.ToHtmlStringRGB(displayColor));
            builder.Append(">");
            builder.Append(requirement.lootItem.lootName);
            builder.Append(" ");
            builder.Append(currentAmount);
            builder.Append(" / ");
            builder.Append(requirement.amount);
            builder.AppendLine("</color>");
        }

        return builder.ToString().TrimEnd();
    }

    private bool CanCraft(CraftingBlueprint_SO blueprint)
    {
        if (blueprint == null || currentRunState == null || blueprint.targetWeapon == null || blueprint.materialRequirements == null)
            return false;

        int forgeCost = CalamitySystem.ApplyForgeCostModifiers(0, currentRunState.activeCalamities);
        if (currentRunState.money < forgeCost)
            return false;

        for (int i = 0; i < blueprint.materialRequirements.Count; i++)
        {
            CraftingMaterialRequirement requirement = blueprint.materialRequirements[i];
            if (requirement == null || requirement.lootItem == null)
                continue;

            if (currentRunState.GetLootAmount(requirement.lootItem) < requirement.amount)
                return false;
        }

        return blueprint.materialRequirements.Count > 0;
    }

    private void OnCraftClicked()
    {
        if (selectedBlueprint == null || craftHandler == null)
            return;

        string result = craftHandler.Invoke(selectedBlueprint);
        if (statusText != null)
            statusText.text = result;

        RefreshDetail();
    }

    private CraftingBlueprint_SO GetFirstBlueprint()
    {
        if (currentRunState?.blueprintInventory == null)
            return null;

        for (int i = 0; i < currentRunState.blueprintInventory.Count; i++)
        {
            RunBlueprintState state = currentRunState.blueprintInventory[i];
            if (state != null && state.blueprintData != null && state.amount > 0)
                return state.blueprintData;
        }

        return null;
    }

    private Button BuildBlueprintButton(RunBlueprintState blueprintState, Vector2 anchoredPosition)
    {
        GameObject buttonGo = new GameObject($"Blueprint_{blueprintState.DisplayName}", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonGo.transform.SetParent(listRoot, false);

        RectTransform rect = buttonGo.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = blueprintButtonSize;

        Image image = buttonGo.GetComponent<Image>();
        image.color = blueprintButtonColor;
        image.sprite = blueprintButtonSprite;
        image.type = Image.Type.Sliced;

        TextMeshProUGUI label = CreateText(rect, "Label", new Vector2(14f, -10f), new Vector2(blueprintButtonSize.x - 24f, blueprintButtonSize.y - 20f), buttonFontSize, blueprintButtonTextColor, blueprintState.DisplayName, true);
        label.enableWordWrapping = true;

        Button button = buttonGo.GetComponent<Button>();
        button.onClick.AddListener(() => SelectBlueprint(blueprintState.blueprintData));
        return button;
    }

    private void RefreshBlueprintButtonVisuals()
    {
        for (int i = 0; i < blueprintButtons.Count; i++)
        {
            Button button = blueprintButtons[i];
            if (button == null)
                continue;

            bool isSelected = false;
            RunBlueprintState state = null;
            if (currentRunState?.blueprintInventory != null)
            {
                for (int j = 0; j < currentRunState.blueprintInventory.Count; j++)
                {
                    if (button.gameObject.name == $"Blueprint_{currentRunState.blueprintInventory[j].DisplayName}")
                    {
                        state = currentRunState.blueprintInventory[j];
                        break;
                    }
                }
            }

            if (state != null && state.blueprintData == selectedBlueprint)
                isSelected = true;

            Image image = button.GetComponent<Image>();
            if (image != null)
            {
                image.color = isSelected ? selectedBlueprintButtonColor : blueprintButtonColor;
                image.sprite = isSelected && selectedBlueprintButtonSprite != null ? selectedBlueprintButtonSprite : blueprintButtonSprite;
            }

            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
                label.color = isSelected ? selectedBlueprintButtonTextColor : blueprintButtonTextColor;
        }
    }

    private void SetCraftButtonInteractable(bool interactable)
    {
        if (craftButton == null)
            return;

        craftButton.interactable = interactable;
        Image image = craftButton.GetComponent<Image>();
        if (image != null)
            image.color = interactable ? actionButtonColor : disabledButtonColor;
    }

    private void ApplyFonts()
    {
        TMP_FontAsset keyword = keywordFont != null ? keywordFont : bodyFont;
        TMP_FontAsset body = bodyFont != null ? bodyFont : keywordFont;

        foreach (TMP_Text text in GetComponentsInChildren<TMP_Text>(true))
        {
            if (text == null)
                continue;

            text.font = body;
            if (text == titleText || text == weaponNameText || text == metaText)
                text.font = keyword;
            text.UpdateMeshPadding();
            text.SetAllDirty();
        }
    }

    private void ApplyPanelVisual()
    {
        if (panelImage == null)
            return;

        if (panelBackgroundSprite != null)
        {
            panelImage.sprite = panelBackgroundSprite;
            panelImage.type = Image.Type.Sliced;
            panelImage.color = Color.white;
        }
        else
        {
            panelImage.sprite = null;
            panelImage.type = Image.Type.Simple;
            panelImage.color = panelBackgroundColor;
        }
    }

    private RectTransform CreatePanel(Transform parent, string name, Vector2 pivot, Vector2 anchoredPosition, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
        return rect;
    }

    private Image BuildIcon(RectTransform parent)
    {
        GameObject iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconGo.transform.SetParent(parent, false);

        RectTransform rect = iconGo.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(0f, -6f);
        rect.sizeDelta = detailIconSize;

        Image image = iconGo.GetComponent<Image>();
        image.preserveAspect = true;
        return image;
    }

    private Button BuildActionButton(RectTransform parent, string name, Vector2 anchoredPosition, string label, UnityEngine.Events.UnityAction onClick)
    {
        GameObject buttonGo = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonGo.transform.SetParent(parent, false);

        RectTransform rect = buttonGo.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(1f, 0f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = actionButtonSize;

        Image image = buttonGo.GetComponent<Image>();
        image.color = actionButtonColor;
        image.sprite = actionButtonSprite;
        image.type = Image.Type.Sliced;

        TextMeshProUGUI text = CreateText(rect, "Label", Vector2.zero, actionButtonSize, buttonFontSize, actionButtonTextColor, label, true);
        text.rectTransform.anchorMin = Vector2.zero;
        text.rectTransform.anchorMax = Vector2.one;
        text.rectTransform.offsetMin = Vector2.zero;
        text.rectTransform.offsetMax = Vector2.zero;
        text.alignment = TextAlignmentOptions.Center;

        Button button = buttonGo.GetComponent<Button>();
        button.onClick.AddListener(onClick);
        return button;
    }

    private TextMeshProUGUI CreateText(RectTransform parent, string name, Vector2 anchoredPosition, Vector2 size, int fontSize, Color color, string value, bool useKeywordFont = false)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        TextMeshProUGUI text = go.GetComponent<TextMeshProUGUI>();
        text.font = useKeywordFont && keywordFont != null ? keywordFont : (bodyFont != null ? bodyFont : keywordFont);
        text.fontSize = fontSize;
        text.color = color;
        text.text = value;
        text.alignment = TextAlignmentOptions.TopLeft;
        text.enableWordWrapping = false;
        return text;
    }

    private static string GetRarityLabel(LootRarity rarity)
    {
        switch (rarity)
        {
            case LootRarity.Trash:
                return "Trash";
            case LootRarity.Common:
                return "普通";
            case LootRarity.Uncommon:
                return "优秀";
            case LootRarity.Rare:
                return "稀有";
            case LootRarity.Epic:
                return "史诗";
            case LootRarity.Legendary:
                return "传说";
            default:
                return rarity.ToString();
        }
    }
}


