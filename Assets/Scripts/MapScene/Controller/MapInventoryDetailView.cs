using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class MapInventoryDetailView : MonoBehaviour
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
        if (closeButton != null)
            closeButton.onClick.AddListener(Hide);
    }

    public void ConfigureRuntime(Image icon, TextMeshProUGUI title, TextMeshProUGUI meta, TextMeshProUGUI description, TextMeshProUGUI relicAdditionalDescription, TextMeshProUGUI actionStatus, Button action, Button close)
    {
        iconImage = icon;
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
        EnsureFontFallback(keywordFont, bodyFont);
        ApplyFont(titleText, keywordFont);
        ApplyFont(metaText, keywordFont);
        ApplyFont(descriptionText, bodyFont != null ? bodyFont : keywordFont);
        ApplyFont(relicAdditionalDescriptionText, bodyFont != null ? bodyFont : keywordFont);
        ApplyFont(actionStatusText, bodyFont != null ? bodyFont : keywordFont);

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
        if (iconImage != null)
        {
            iconImage.sprite = item.icon != null ? item.icon : fallbackSprite;
            iconImage.preserveAspect = true;
        }

        if (titleText != null)
            titleText.text = string.IsNullOrWhiteSpace(item.lootName) ? lootState.DisplayName : item.lootName;

        if (metaText != null)
        {
            string attributeName = item.attribute != null && !string.IsNullOrWhiteSpace(item.attribute.attributeName)
                ? item.attribute.attributeName
                : "\u65e0\u5c5e\u6027";
            metaText.text = $"{BuildColoredLootRarityLabel(item.rarity)} | {attributeName} | x{Mathf.Max(1, lootState.amount)}";
        }

        if (descriptionText != null)
            descriptionText.text = string.IsNullOrWhiteSpace(item.description) ? "\u6682\u65e0\u63cf\u8ff0\u3002" : item.description;

        SetRelicAdditionalDescription(string.Empty);
        ConfigureActionButton(string.Empty, false, null, string.Empty);
        gameObject.SetActive(true);
    }

    public void Show(RunWeaponState weaponState, Sprite fallbackSprite = null)
    {
        if (weaponState == null || weaponState.weaponData == null)
        {
            Hide();
            return;
        }

        Weapon_SO weapon = weaponState.weaponData;
        if (iconImage != null)
        {
            iconImage.sprite = weapon.icon != null ? weapon.icon : fallbackSprite;
            iconImage.preserveAspect = true;
        }

        if (titleText != null)
            titleText.text = string.IsNullOrWhiteSpace(weapon.weaponName) ? weaponState.DisplayName : weapon.weaponName;

        if (metaText != null)
            metaText.text = $"{BuildColoredWeaponRarityLabel(weapon.rarity)} | {GetWeaponTypeLabel(weapon.weaponType)} | x{Mathf.Max(1, weaponState.amount)}";

        if (descriptionText != null)
            descriptionText.text = string.IsNullOrWhiteSpace(weapon.description) ? "\u6682\u65e0\u63cf\u8ff0\u3002" : weapon.description;

        SetRelicAdditionalDescription(string.Empty);
        gameObject.SetActive(true);
    }

    public void Show(RunBlueprintState blueprintState, Sprite fallbackSprite = null)
    {
        if (blueprintState == null || blueprintState.blueprintData == null)
        {
            Hide();
            return;
        }

        CraftingBlueprint_SO blueprint = blueprintState.blueprintData;
        if (iconImage != null)
        {
            iconImage.sprite = blueprint.DisplayIcon != null ? blueprint.DisplayIcon : fallbackSprite;
            iconImage.preserveAspect = true;
        }

        if (titleText != null)
            titleText.text = blueprint.DisplayName;

        if (metaText != null)
        {
            string weaponName = blueprint.targetWeapon != null && !string.IsNullOrWhiteSpace(blueprint.targetWeapon.weaponName)
                ? blueprint.targetWeapon.weaponName
                : "\u672a\u914d\u7f6e\u6b66\u5668";
            metaText.text = $"{BuildColoredLootRarityLabel(blueprint.rarity)} | {weaponName} | x{Mathf.Max(1, blueprintState.amount)}";
        }

        if (descriptionText != null)
            descriptionText.text = BuildBlueprintDescription(blueprint);

        SetRelicAdditionalDescription(string.Empty);
        ConfigureActionButton(string.Empty, false, null, string.Empty);
        gameObject.SetActive(true);
    }

    public void Show(RunRelicState relicState, Sprite fallbackSprite = null)
    {
        if (relicState == null || relicState.relicData == null)
        {
            Hide();
            return;
        }

        Relic_SO relic = relicState.relicData;
        if (iconImage != null)
        {
            iconImage.sprite = relic.icon != null ? relic.icon : fallbackSprite;
            iconImage.preserveAspect = true;
        }

        if (titleText != null)
            titleText.text = relic.DisplayName;

        if (metaText != null)
            metaText.text = BuildColoredLootRarityLabel(relic.rarity);

        if (descriptionText != null)
            descriptionText.text = string.IsNullOrWhiteSpace(relic.description) ? "\u6682\u65e0\u63cf\u8ff0\u3002" : relic.description;

        SetRelicAdditionalDescription(relic.additionalDescription);
        ConfigureActionButton(string.Empty, false, null, string.Empty);
        gameObject.SetActive(true);
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
    }

    public void Hide()
    {
        SetRelicAdditionalDescription(string.Empty);
        ConfigureActionButton(string.Empty, false, null, string.Empty);
        gameObject.SetActive(false);
    }

    private void SetRelicAdditionalDescription(string value)
    {
        if (relicAdditionalDescriptionText == null)
            return;

        bool hasContent = !string.IsNullOrWhiteSpace(value);
        relicAdditionalDescriptionText.gameObject.SetActive(hasContent);
        relicAdditionalDescriptionText.text = hasContent ? value : string.Empty;
    }

    private static string BuildColoredLootRarityLabel(LootRarity rarity)
    {
        return WrapWithColor(GetLootRarityLabel(rarity), GetLootRarityColor(rarity));
    }

    private static string BuildColoredWeaponRarityLabel(Rarity rarity)
    {
        return WrapWithColor(GetWeaponRarityLabel(rarity), GetWeaponRarityColor(rarity));
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
        if (text == null || font == null)
            return;

        text.font = font;
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
