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
            ? item.attribute.LocalizedName : GameText.Get("ui.map_inventory_detail_view.no_attribute", "无属性");
        PresentItem(item, item.icon != null ? item.icon : fallbackSprite,
            lootState.DisplayName,
            GetLootRarityLabel(item.rarity), GetLootRarityColor(item.rarity), attribute, lootState.amount,
            item.LocalizedDescription, string.Empty);
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
            weaponState.DisplayName,
            GetWeaponRarityLabel(weapon.rarity), GetWeaponRarityColor(weapon.rarity), GetWeaponTypeLabel(weapon.weaponType),
            weaponState.amount, weapon.LocalizedDescription, string.Empty);
    }

    public void Show(RunBlueprintState blueprintState, Sprite fallbackSprite = null)
    {
        if (blueprintState == null || blueprintState.blueprintData == null)
        {
            Hide();
            return;
        }

        CraftingBlueprint_SO blueprint = blueprintState.blueprintData;
        PresentItem(blueprint, blueprint.DisplayIcon != null ? blueprint.DisplayIcon : fallbackSprite, blueprint.LocalizedName,
            GetLootRarityLabel(blueprint.rarity), GetLootRarityColor(blueprint.rarity), GameText.Get("ui.map_hud.blueprints", "图纸"), blueprintState.amount,
            BlueprintDetailDescription(blueprint), string.Empty);
    }

    public void Show(RunRelicState relicState, Sprite fallbackSprite = null)
    {
        if (relicState == null || relicState.relicData == null)
        {
            Hide();
            return;
        }

        Relic_SO relic = relicState.relicData;
        PresentItem(relic, relic.icon != null ? relic.icon : fallbackSprite, relic.LocalizedName,
            GetLootRarityLabel(relic.rarity), GetLootRarityColor(relic.rarity), GameText.Get("ui.map_hud.relics", "遗物"), relicState.amount,
            relic.LocalizedDescription, relic.LocalizedNotes);
    }

    private void PresentItem(Object item, Sprite icon, string title, string rarity, Color rarityColor,
        string category, int amount, string description, string additional)
    {
        PresentItemCore(item, icon, title, rarity, rarityColor, category, amount, description, additional, false);
    }

    private void PresentItemCore(Object item, Sprite icon, string title, string rarity, Color rarityColor,
        string category, int amount, string description, string additional, bool isEncyclopedia)
    {
        encyclopediaEntry = isEncyclopedia;
        if (!isEncyclopedia && modalRoot != null)
        {
            encyclopediaTextOnly = false;
            quantityText.transform.parent.gameObject.SetActive(true);
            displayEngraving.gameObject.SetActive(true);
            rarityFace.gameObject.SetActive(true);
        }
        ResetPresentationLabels();
        category = ResourceKeywordText.AttributeLabel(category);
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
        if (quantityText != null) quantityText.text = GameText.Get("ui.map_inventory_detail_view.quantity", "数量 ×") + Mathf.Max(1, amount);
        if (rarityFace != null) { rarityFace.accent = rarityColor; rarityFace.SetVerticesDirty(); }
        if (categoryFace != null) { categoryFace.electricElement = item is LootItem_SO loot && loot.attribute != null
            && loot.attribute.attributeId == LootAttribute_SO.ElectricityId; categoryFace.SetVerticesDirty(); }
        if (item is EnemyData_SO enemy && !string.IsNullOrWhiteSpace(enemy.LocalizedDescription))
        {
            string narrative = enemy.LocalizedDescription;
            description = description?.Replace(narrative, "<noparse>" + narrative + "</noparse>");
        }
        if (descriptionText != null) descriptionText.text = string.IsNullOrWhiteSpace(description) ? GameText.Get("ui.no_description", "暂无描述。") : description;
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
        if (DismissAllowed != null && !DismissAllowed()) return;
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
                return GameText.Get("ui.difficulty.normal", "普通");
            case LootRarity.Uncommon:
                return GameText.Get("ui.map_crafting.fine", "优秀");
            case LootRarity.Rare:
                return GameText.Get("battle.settlement.rare", "稀有");
            case LootRarity.Epic:
                return GameText.Get("battle.settlement.epic", "史诗");
            case LootRarity.Legendary:
                return GameText.Get("battle.settlement.legendary", "传说");
            default:
                return rarity.ToString();
        }
    }

    private static string GetWeaponRarityLabel(Rarity rarity)
    {
        switch (rarity)
        {
            case Rarity.Common:
                return GameText.Get("ui.difficulty.normal", "普通");
            case Rarity.Uncommon:
                return GameText.Get("ui.map_crafting.fine", "优秀");
            case Rarity.Rare:
                return GameText.Get("battle.settlement.rare", "稀有");
            case Rarity.Epic:
                return GameText.Get("battle.settlement.epic", "史诗");
            case Rarity.Legendary:
                return GameText.Get("battle.settlement.legendary", "传说");
            default:
                return rarity.ToString();
        }
    }

    private static string GetWeaponTypeLabel(WeaponType weaponType)
    {
        switch (weaponType)
        {
            case WeaponType.Sword:
                return GameText.Get("ui.map_crafting.sword", "剑");
            case WeaponType.Axe:
                return GameText.Get("ui.map_crafting.axe", "斧");
            case WeaponType.Bow:
                return GameText.Get("ui.map_crafting.bow", "弓");
            case WeaponType.Wand:
                return GameText.Get("ui.map_inventory_detail_view.staff", "杖");
            case WeaponType.Dagger:
                return GameText.Get("ui.map_crafting.dagger", "匕首");
            default:
                return weaponType.ToString();
        }
    }

    internal static Color GetLootRarityColor(LootRarity rarity)
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
