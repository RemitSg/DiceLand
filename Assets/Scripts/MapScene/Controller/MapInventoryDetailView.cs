using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public partial class MapInventoryDetailView : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI metaText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI relicAdditionalDescriptionText;
    [SerializeField] private TextMeshProUGUI actionStatusText;
    [SerializeField] private Button actionButton;
    [SerializeField] private Button closeButton;

    private void Awake()
    {
        MapItemIconFit.Attach(iconImage);
        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(Hide);
            closeButton.onClick.AddListener(Hide);
        }
    }

    public void ConfigureRuntime(Image icon, TextMeshProUGUI title, TextMeshProUGUI meta, TextMeshProUGUI description, TextMeshProUGUI relicAdditionalDescription, TextMeshProUGUI actionStatus, Button action, Button close)
    {
        iconImage = icon;
        MapItemIconFit.Attach(iconImage);
        titleText = title;
        metaText = meta;
        descriptionText = description;
        relicAdditionalDescriptionText = relicAdditionalDescription;
        actionStatusText = actionStatus;
        actionButton = action;

        if (closeButton != null)
            closeButton.onClick.RemoveListener(Hide);

        closeButton = close;
        if (closeButton != null)
            closeButton.onClick.AddListener(Hide);
    }

    public void ConfigureFonts(TMP_FontAsset keywordFont, TMP_FontAsset bodyFont)
    {
        detailHeadingFont = keywordFont;
        detailBodyFont = bodyFont != null ? bodyFont : keywordFont;
        EnsureFontFallback(keywordFont, bodyFont);
        ApplyFont(titleText, keywordFont);
        ApplyFont(metaText, keywordFont);
        ApplyFont(descriptionText, bodyFont != null ? bodyFont : keywordFont);
        ApplyFont(relicAdditionalDescriptionText, bodyFont != null ? bodyFont : keywordFont);
        ApplyFont(actionStatusText, bodyFont != null ? bodyFont : keywordFont);
        ApplyPresentationFonts();

        if (actionButton != null)
        {
            TMP_Text[] actionTexts = actionButton.GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < actionTexts.Length; i++)
                ApplyFont(actionTexts[i], keywordFont);
        }

        if (closeButton != null)
        {
            TMP_Text[] closeTexts = closeButton.GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < closeTexts.Length; i++)
                ApplyFont(closeTexts[i], keywordFont);
        }
    }

    public void Show(RunLootState lootState, Sprite fallbackSprite = null)
    {
        if (lootState == null || lootState.lootItem == null)
        {
            Hide();
            return;
        }

        LootItem_SO item = lootState.lootItem;
        string attribute = item.attribute != null && !string.IsNullOrWhiteSpace(item.attribute.attributeName)
            ? item.attribute.attributeName : "无属性";
        PresentItem(item, item.icon != null ? item.icon : fallbackSprite,
            string.IsNullOrWhiteSpace(item.lootName) ? lootState.DisplayName : item.lootName,
            GetLootRarityLabel(item.rarity), GetLootRarityColor(item.rarity), attribute, lootState.amount,
            item.description, string.Empty);
    }

    public void Show(RunWeaponState weaponState, Sprite fallbackSprite = null)
    {
        if (weaponState == null || weaponState.weaponData == null)
        {
            Hide();
            return;
        }

        Weapon_SO weapon = weaponState.weaponData;
        PresentItem(weapon, weapon.icon != null ? weapon.icon : fallbackSprite,
            string.IsNullOrWhiteSpace(weapon.weaponName) ? weaponState.DisplayName : weapon.weaponName,
            GetWeaponRarityLabel(weapon.rarity), GetWeaponRarityColor(weapon.rarity), GetWeaponTypeLabel(weapon.weaponType),
            weaponState.amount, weapon.description, string.Empty);
    }

    public void Show(RunBlueprintState blueprintState, Sprite fallbackSprite = null)
    {
        if (blueprintState == null || blueprintState.blueprintData == null)
        {
            Hide();
            return;
        }

        CraftingBlueprint_SO blueprint = blueprintState.blueprintData;
        string target = blueprint.targetWeapon != null && !string.IsNullOrWhiteSpace(blueprint.targetWeapon.weaponName)
            ? blueprint.targetWeapon.weaponName : "未配置武器";
        PresentItem(blueprint, blueprint.DisplayIcon != null ? blueprint.DisplayIcon : fallbackSprite, blueprint.DisplayName,
            GetLootRarityLabel(blueprint.rarity), GetLootRarityColor(blueprint.rarity), "图纸", blueprintState.amount,
            "锻造成品：" + target + "\n\n" + BuildBlueprintDescription(blueprint), string.Empty);
    }

    public void Show(RunRelicState relicState, Sprite fallbackSprite = null)
    {
        if (relicState == null || relicState.relicData == null)
        {
            Hide();
            return;
        }

        Relic_SO relic = relicState.relicData;
        PresentItem(relic, relic.icon != null ? relic.icon : fallbackSprite, relic.DisplayName,
            GetLootRarityLabel(relic.rarity), GetLootRarityColor(relic.rarity), "遗物", relicState.amount,
            relic.description, relic.additionalDescription);
    }

    private void PresentItem(Object item, Sprite icon, string title, string rarity, Color rarityColor,
        string category, int amount, string description, string additional)
    {
        bool changed = displayedItem != item;
        if (changed) ConfigureActionButton(string.Empty, false, null, string.Empty);
        displayedItem = item;
        if (iconImage != null)
        {
            iconImage.sprite = icon;
            iconImage.enabled = icon != null;
            iconImage.preserveAspect = true;
        }
        if (titleText != null) titleText.text = title;
        if (metaText != null)
            metaText.text = modalRoot != null ? WrapWithColor(rarity, Color.Lerp(rarityColor, Color.white, 0.22f))
                : $"{WrapWithColor(rarity, rarityColor)} | {category} | x{Mathf.Max(1, amount)}";
        if (categoryText != null) categoryText.text = category;
        if (quantityText != null) quantityText.text = "数量 ×" + Mathf.Max(1, amount);
        if (rarityFace != null) { rarityFace.accent = rarityColor; rarityFace.SetVerticesDirty(); }
        if (categoryFace != null) { categoryFace.electricElement = category == "电"; categoryFace.SetVerticesDirty(); }
        if (descriptionText != null) descriptionText.text = string.IsNullOrWhiteSpace(description) ? "暂无描述。" : description;
        SetRelicAdditionalDescription(additional);
        BeginPresentation(changed);
    }

    public void ConfigureActionButton(string label, bool interactable, UnityEngine.Events.UnityAction onClick, string status)
    {
        if (actionButton != null)
        {
            bool showButton = !string.IsNullOrWhiteSpace(label);
            actionButton.gameObject.SetActive(showButton);
            actionButton.interactable = interactable;
            actionButton.onClick.RemoveAllListeners();
            if (showButton && onClick != null)
                actionButton.onClick.AddListener(onClick);

            TMP_Text buttonLabel = actionButton.GetComponentInChildren<TMP_Text>(true);
            if (buttonLabel != null)
                buttonLabel.text = label ?? string.Empty;
        }

        if (actionStatusText != null)
            actionStatusText.text = status ?? string.Empty;
        if (actionFace != null) actionFace.SetState(false, interactable);
        if (modalRoot != null) RefreshDescriptionLayout(false);
    }

    public void Hide()
    {
        if (!IsVisible || closing) return;
        if (modalRoot == null || !Application.isPlaying || !gameObject.activeInHierarchy) HideImmediately();
        else
        {
            closing = true;
            panelGroup.interactable = false;
            if (descriptionScroll != null) descriptionScroll.StopMovement();
        }
    }

    private void SetRelicAdditionalDescription(string value)
    {
        if (relicAdditionalDescriptionText == null)
            return;

        bool hasContent = !string.IsNullOrWhiteSpace(value);
        relicAdditionalDescriptionText.gameObject.SetActive(hasContent);
        relicAdditionalDescriptionText.text = hasContent ? value : string.Empty;
    }

    private static string WrapWithColor(string value, Color color)
    {
        return $"<color=#{ColorUtility.ToHtmlStringRGB(color)}>{value}</color>";
    }

    private static string GetLootRarityLabel(LootRarity rarity)
    {
        switch (rarity)
        {
            case LootRarity.Trash:
                return "Trash";
            case LootRarity.Common:
                return "\u666e\u901a";
            case LootRarity.Uncommon:
                return "\u4f18\u79c0";
            case LootRarity.Rare:
                return "\u7a00\u6709";
            case LootRarity.Epic:
                return "\u53f2\u8bd7";
            case LootRarity.Legendary:
                return "\u4f20\u8bf4";
            default:
                return rarity.ToString();
        }
    }

    private static string GetWeaponRarityLabel(Rarity rarity)
    {
        switch (rarity)
        {
            case Rarity.Common:
                return "\u666e\u901a";
            case Rarity.Uncommon:
                return "\u4f18\u79c0";
            case Rarity.Rare:
                return "\u7a00\u6709";
            case Rarity.Epic:
                return "\u53f2\u8bd7";
            case Rarity.Legendary:
                return "\u4f20\u8bf4";
            default:
                return rarity.ToString();
        }
    }

    private static string GetWeaponTypeLabel(WeaponType weaponType)
    {
        switch (weaponType)
        {
            case WeaponType.Sword:
                return "\u5251";
            case WeaponType.Axe:
                return "\u65a7";
            case WeaponType.Bow:
                return "\u5f13";
            case WeaponType.Wand:
                return "\u6756";
            case WeaponType.Dagger:
                return "\u5315\u9996";
            default:
                return weaponType.ToString();
        }
    }

    private static Color GetLootRarityColor(LootRarity rarity)
    {
        switch (rarity)
        {
            case LootRarity.Trash:
                return new Color32(126, 83, 48, 255);
            case LootRarity.Common:
                return new Color32(128, 128, 128, 255);
            case LootRarity.Uncommon:
                return new Color32(56, 178, 86, 255);
            case LootRarity.Rare:
                return new Color32(64, 126, 255, 255);
            case LootRarity.Epic:
                return new Color32(163, 92, 255, 255);
            case LootRarity.Legendary:
                return new Color32(230, 180, 44, 255);
            default:
                return Color.white;
        }
    }

    private static Color GetWeaponRarityColor(Rarity rarity)
    {
        switch (rarity)
        {
            case Rarity.Common:
                return new Color32(128, 128, 128, 255);
            case Rarity.Uncommon:
                return new Color32(56, 178, 86, 255);
            case Rarity.Rare:
                return new Color32(64, 126, 255, 255);
            case Rarity.Epic:
                return new Color32(163, 92, 255, 255);
            case Rarity.Legendary:
                return new Color32(230, 180, 44, 255);
            default:
                return Color.white;
        }
    }

    private static string BuildBlueprintDescription(CraftingBlueprint_SO blueprint)
    {
        if (blueprint == null)
            return "\u6682\u65e0\u56fe\u7eb8\u4fe1\u606f\u3002";

        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        if (!string.IsNullOrWhiteSpace(blueprint.description))
            builder.AppendLine(blueprint.description);
        else
            builder.AppendLine("\u6682\u65e0\u56fe\u7eb8\u63cf\u8ff0\u3002");

        if (blueprint.materialRequirements != null && blueprint.materialRequirements.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("\u6240\u9700\u6750\u6599\uff1a");
            for (int i = 0; i < blueprint.materialRequirements.Count; i++)
            {
                CraftingMaterialRequirement requirement = blueprint.materialRequirements[i];
                if (requirement == null || requirement.lootItem == null)
                    continue;

                builder.Append(" - ");
                builder.Append(requirement.lootItem.lootName);
                builder.Append(" x");
                builder.AppendLine(requirement.amount.ToString());
            }
        }

        return builder.ToString().TrimEnd();
    }

    private static void ApplyFont(TMP_Text text, TMP_FontAsset font)
    {
        if (text == null)
            return;

        ResourceKeywordText.Attach(text);
        if (font == null)
            return;

        text.font = font;
        text.fontSharedMaterial = font.material;
        text.UpdateMeshPadding();
        text.SetAllDirty();
    }

    private static void EnsureFontFallback(TMP_FontAsset primaryFont, TMP_FontAsset fallbackFont)
    {
        if (primaryFont == null || fallbackFont == null || primaryFont == fallbackFont)
            return;

        if (primaryFont.fallbackFontAssetTable == null)
            primaryFont.fallbackFontAssetTable = new List<TMP_FontAsset>();

        if (!primaryFont.fallbackFontAssetTable.Contains(fallbackFont))
            primaryFont.fallbackFontAssetTable.Add(fallbackFont);
    }
}
