using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public partial class MapCraftingOverlay : MonoBehaviour
{
    public event Action Hidden;
    public event Action<string> WeaponFused;

    [Header("Fonts")]
    [SerializeField] private TMP_FontAsset keywordFont;
    [SerializeField] private TMP_FontAsset bodyFont;
    [SerializeField] private int titleFontSize = 56;
    [SerializeField] private int bodyFontSize = 28;
    [SerializeField] private int buttonFontSize = 34;

    [Header("Panel")]
    [SerializeField] private Vector2 panelSize = new Vector2(1480, 840);
    [SerializeField] private Vector2 panelOffset;
    [SerializeField] private string craftButtonLabel = "锻造";
    [SerializeField] private string closeButtonLabel = "关闭";

    private RunState currentRunState;
    private CraftingBlueprint_SO selectedBlueprint;
    private Func<CraftingBlueprint_SO, string> craftHandler;
    private bool handlingAction;
    private readonly List<BlueprintCard> blueprintCards = new List<BlueprintCard>();
    private readonly List<MaterialRow> materialRows = new List<MaterialRow>();

    public bool IsVisible => canvas != null && canvas.gameObject.activeInHierarchy;

    public void ConfigureFonts(TMP_FontAsset keyword, TMP_FontAsset body)
    {
        if (keyword != null) keywordFont = keyword;
        if (body != null) bodyFont = body;
        ApplyFonts();
        if (canvas != null) RefreshLayout();
    }

    public void Show(RunState runState, Func<CraftingBlueprint_SO, string> onCraft)
    {
        EnsureUi();
        currentRunState = runState;
        craftHandler = onCraft;
        gameObject.SetActive(true);
        canvas.gameObject.SetActive(true);
        closing = false;
        handlingAction = false;
        feedbackTime = 0;
        panelGroup.interactable = true;
        statusText.text = string.Empty;
        ResetFusion();
        ShowFusionTab(false);
        RefreshBlueprintList();
        SelectBlueprint(ResolveSelection(selectedBlueprint), false);
        ApplyViewport(new Vector2(Screen.width, Screen.height), Screen.safeArea);
        ResetScroll(blueprintScroll);
        openProgress = Application.isPlaying ? 0 : 1;
        ApplyMotion();
    }

    // Keep the backdrop until the closing animation ends to prevent click-through.
    public void Hide()
    {
        if (!IsVisible || closing) return;
        closing = true;
        panelGroup.interactable = false;
        if (!Application.isPlaying) FinishHide();
    }

    private void FinishHide()
    {
        canvas.gameObject.SetActive(false);
        closing = false;
        craftHandler = null;
        ResetFusion();
        currentRunState = null;
        Hidden?.Invoke();
    }

    private CraftingBlueprint_SO ResolveSelection(CraftingBlueprint_SO preferred)
    {
        if (currentRunState?.blueprintInventory == null) return null;
        CraftingBlueprint_SO first = null;
        foreach (RunBlueprintState entry in currentRunState.blueprintInventory)
        {
            if (entry?.blueprintData == null || entry.amount <= 0) continue;
            if (entry.blueprintData == preferred) return preferred;
            if (first == null) first = entry.blueprintData;
        }
        return first;
    }

    private void RefreshBlueprintList()
    {
        foreach (BlueprintCard card in blueprintCards) RemoveUi(card.button.gameObject);
        blueprintCards.Clear();
        var seen = new HashSet<CraftingBlueprint_SO>();
        if (currentRunState?.blueprintInventory != null)
            foreach (RunBlueprintState entry in currentRunState.blueprintInventory)
                if (entry?.blueprintData != null && entry.amount > 0 && seen.Add(entry.blueprintData))
                    blueprintCards.Add(BuildBlueprintCard(entry.blueprintData));
        emptyText.gameObject.SetActive(blueprintCards.Count == 0);
    }

    private void SelectBlueprint(CraftingBlueprint_SO blueprint, bool animate = true)
    {
        bool changed = selectedBlueprint != blueprint;
        selectedBlueprint = blueprint;
        statusText.text = string.Empty;
        RefreshDetail();
        if (animate && changed) selectionTime = 0.24f;
        ResetScroll(materialScroll);
        ResetScroll(descriptionScroll);
    }

    private void RefreshDetail()
    {
        bool hasBlueprint = selectedBlueprint != null;
        SetIcon(blueprintImage, hasBlueprint ? selectedBlueprint.DisplayIcon : null);
        SetIcon(iconImage, hasBlueprint && selectedBlueprint.targetWeapon != null ? selectedBlueprint.targetWeapon.icon : null);
        weaponNameText.text = hasBlueprint && selectedBlueprint.targetWeapon != null
            ? selectedBlueprint.targetWeapon.LocalizedName : GameText.Get("ui.map_crafting.no_blueprint_selected", "未选择图纸");
        metaText.text = hasBlueprint ? GetRarityLabel(selectedBlueprint.rarity) + " · " +
            (selectedBlueprint.targetWeapon != null ? GetWeaponTypeLabel(selectedBlueprint.targetWeapon.weaponType) : GameText.Get("ui.map_crafting.weapon", "武器")) : string.Empty;
        metaBadge.gameObject.SetActive(hasBlueprint);
        descriptionText.text = hasBlueprint
            ? (string.IsNullOrWhiteSpace(selectedBlueprint.description) ? GameText.Get("ui.map_crafting.no_blueprint_description_available", "暂无图纸描述。") : selectedBlueprint.LocalizedDescription)
            : GameText.Get("ui.map_crafting.once_you_obtain_a_blueprint_you_can_preview", "获得图纸后，可在此查看成品与材料需求。");
        if (hasBlueprint && blueprintCards.Count > 1)
            descriptionText.text = selectedBlueprint.LocalizedName + "\n" + descriptionText.text;
        foreach (MaterialRow row in materialRows) RemoveUi(row.rect.gameObject);
        materialRows.Clear();
        if (hasBlueprint)
        {
            int cost = RunCraftingService.Cost(currentRunState);
            if (cost > 0)
                materialRows.Add(BuildMaterialRow(GameText.Get("term.gold", "金币"), MapForestArt.Get("Coins"), currentRunState != null ? currentRunState.money : 0, cost));
            foreach (CraftingMaterialRequirement requirement in RunCraftingService.GetRequirements(selectedBlueprint))
                materialRows.Add(BuildMaterialRow(requirement.lootItem.LocalizedName, requirement.lootItem.icon,
                    currentRunState != null ? currentRunState.GetLootAmount(requirement.lootItem) : 0, requirement.amount));
        }
        materialEmptyText.gameObject.SetActive(materialRows.Count == 0);
        materialEmptyText.text = hasBlueprint ? GameText.Get("ui.map_crafting.no_valid_material_requirements", "未配置有效材料需求") : GameText.Get("ui.map_crafting.select_a_blueprint_to_see_its_materials", "选择图纸以查看材料");
        RefreshBlueprintCardStates();
        RefreshAvailability();
        RefreshLayout();
    }

    private void RefreshBlueprintCardStates()
    {
        foreach (BlueprintCard card in blueprintCards)
        {
            bool selected = card.blueprint == selectedBlueprint;
            var availability = RunCraftingService.CheckAvailability(currentRunState, card.blueprint, out _);
            bool ready = availability == RunCraftingService.Availability.Ready;
            card.face.SetState(selected, true);
            card.label.color = selected ? Ink : Ivory;
            card.readiness.text = ready ? GameText.Get("ui.map_crafting.ready_to_forge", "可锻造") : availability == RunCraftingService.Availability.InsufficientGold ? GameText.Get("ui.map_crafting.need_gold", "缺金币")
                : availability == RunCraftingService.Availability.InsufficientMaterials ? GameText.Get("ui.map_crafting.need_materials", "缺材料") : GameText.Get("ui.map_crafting.unavailable", "暂不可用");
            card.readiness.color = selected ? (ready ? new Color32(28, 83, 48, 255) : new Color32(117, 50, 29, 255)) : (ready ? Jade : Coral);
        }
    }

    private void RefreshAvailability()
    {
        var availability = RunCraftingService.CheckAvailability(currentRunState, selectedBlueprint, out string reason);
        bool canCraft = availability == RunCraftingService.Availability.Ready;
        bool enabled = canCraft && craftHandler != null && !handlingAction && feedbackTime <= 0 && !closing;
        craftButton.interactable = enabled;
        craftFace.SetState(false, enabled);
        craftLabel.color = enabled ? Ivory : Muted;
        if (selectedBlueprint == null) availabilityText.text = GameText.Get("ui.map_crafting.select_a_blueprint", "请选择一张图纸");
        else if (!canCraft) availabilityText.text = availability == RunCraftingService.Availability.InsufficientMaterials ? GameText.Get("ui.map_crafting.you_need_more_materials_to_forge_this_weapon", "材料不足，暂无法锻造") : reason;
        else availabilityText.text = GameText.Get("ui.map_crafting.all_materials_ready_you_can_forge_this_weapon", "材料齐备，可以锻造");
        availabilityText.color = canCraft ? Jade : Coral;
    }

    private void OnCraftClicked()
    {
        if (!IsVisible || showingFusion || closing || handlingAction || feedbackTime > 0 || craftHandler == null ||
            !RunCraftingService.CanCraft(currentRunState, selectedBlueprint, out _)) return;
        handlingAction = true;
        try
        {
            Weapon_SO weapon = selectedBlueprint.targetWeapon;
            int before = currentRunState.GetWeaponAmount(weapon);
            string result;
            using (MapFeedbackOrigin.FromText(availabilityText, (RectTransform)craftButton.transform))
                result = craftHandler(selectedBlueprint);
            bool success = currentRunState.GetWeaponAmount(weapon) > before;
            feedbackTime = success ? 0.72f : 0.20f;
            forgeBurst = success;
            RefreshDetail();
            statusText.text = result ?? string.Empty;
            statusText.color = success ? Jade : Coral;
        }
        finally
        {
            handlingAction = false;
            RefreshAvailability();
        }
    }

    private static void RemoveUi(GameObject go)
    {
        go.SetActive(false);
        if (Application.isPlaying) Destroy(go); else DestroyImmediate(go);
    }

    private static string GetWeaponTypeLabel(WeaponType type)
    {
        switch (type)
        {
            case WeaponType.Sword: return GameText.Get("ui.map_crafting.sword", "剑");
            case WeaponType.Axe: return GameText.Get("ui.map_crafting.axe", "斧");
            case WeaponType.Bow: return GameText.Get("ui.map_crafting.bow", "弓");
            case WeaponType.Wand: return GameText.Get("ui.map_crafting.staff", "法杖");
            case WeaponType.Dagger: return GameText.Get("ui.map_crafting.dagger", "匕首");
            default: return GameText.Get("ui.map_crafting.weapon", "武器");
        }
    }

    private static string GetRarityLabel(LootRarity rarity)
    {
        switch (rarity)
        {
            case LootRarity.Trash: return GameText.Get("ui.map_crafting.poor", "劣质");
            case LootRarity.Common: return GameText.Get("ui.difficulty.normal", "普通");
            case LootRarity.Uncommon: return GameText.Get("ui.map_crafting.fine", "优秀");
            case LootRarity.Rare: return GameText.Get("battle.settlement.rare", "稀有");
            case LootRarity.Epic: return GameText.Get("battle.settlement.epic", "史诗");
            case LootRarity.Legendary: return GameText.Get("battle.settlement.legendary", "传说");
            default: return GameText.Get("ui.difficulty.normal", "普通");
        }
    }
}
