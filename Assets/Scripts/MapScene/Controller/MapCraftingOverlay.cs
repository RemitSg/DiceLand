using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public partial class MapCraftingOverlay : MonoBehaviour
{
    public event Action Hidden;

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
            ? selectedBlueprint.targetWeapon.weaponName : "未选择图纸";
        metaText.text = hasBlueprint ? GetRarityLabel(selectedBlueprint.rarity) + " · " +
            (selectedBlueprint.targetWeapon != null ? GetWeaponTypeLabel(selectedBlueprint.targetWeapon.weaponType) : "武器") : string.Empty;
        metaBadge.gameObject.SetActive(hasBlueprint);
        descriptionText.text = hasBlueprint
            ? (string.IsNullOrWhiteSpace(selectedBlueprint.description) ? "暂无图纸描述。" : selectedBlueprint.description)
            : "获得图纸后，可在此查看成品与材料需求。";
        if (hasBlueprint && blueprintCards.Count > 1)
            descriptionText.text = selectedBlueprint.DisplayName + "\n" + descriptionText.text;
        foreach (MaterialRow row in materialRows) RemoveUi(row.rect.gameObject);
        materialRows.Clear();
        if (hasBlueprint)
        {
            int cost = RunCraftingService.Cost(currentRunState);
            if (cost > 0)
                materialRows.Add(BuildMaterialRow("金币", MapForestArt.Get("Coins"), currentRunState != null ? currentRunState.money : 0, cost));
            foreach (CraftingMaterialRequirement requirement in RunCraftingService.GetRequirements(selectedBlueprint))
                materialRows.Add(BuildMaterialRow(requirement.lootItem.lootName, requirement.lootItem.icon,
                    currentRunState != null ? currentRunState.GetLootAmount(requirement.lootItem) : 0, requirement.amount));
        }
        materialEmptyText.gameObject.SetActive(materialRows.Count == 0);
        materialEmptyText.text = hasBlueprint ? "未配置有效材料需求" : "选择图纸以查看材料";
        RefreshBlueprintCardStates();
        RefreshAvailability();
        RefreshLayout();
    }

    private void RefreshBlueprintCardStates()
    {
        foreach (BlueprintCard card in blueprintCards)
        {
            bool selected = card.blueprint == selectedBlueprint;
            bool ready = RunCraftingService.CanCraft(currentRunState, card.blueprint, out string reason);
            card.face.SetState(selected, true);
            card.label.color = selected ? Ink : Ivory;
            card.readiness.text = ready ? "可锻造" : reason != null && reason.StartsWith("金币不足") ? "缺金币"
                : reason != null && reason.StartsWith("材料不足") ? "缺材料" : "暂不可用";
            card.readiness.color = selected ? (ready ? new Color32(28, 83, 48, 255) : new Color32(117, 50, 29, 255)) : (ready ? Jade : Coral);
        }
    }

    private void RefreshAvailability()
    {
        bool canCraft = RunCraftingService.CanCraft(currentRunState, selectedBlueprint, out string reason);
        bool enabled = canCraft && craftHandler != null && !handlingAction && feedbackTime <= 0 && !closing;
        craftButton.interactable = enabled;
        craftFace.SetState(false, enabled);
        craftLabel.color = enabled ? Ivory : Muted;
        if (selectedBlueprint == null) availabilityText.text = "请选择一张图纸";
        else if (!canCraft) availabilityText.text = reason != null && reason.StartsWith("材料不足") ? "材料不足，暂无法锻造" : reason;
        else availabilityText.text = "材料齐备，可以锻造";
        availabilityText.color = canCraft ? Jade : Coral;
    }

    private void OnCraftClicked()
    {
        if (!IsVisible || closing || handlingAction || feedbackTime > 0 || craftHandler == null ||
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
            case WeaponType.Sword: return "剑";
            case WeaponType.Axe: return "斧";
            case WeaponType.Bow: return "弓";
            case WeaponType.Wand: return "法杖";
            case WeaponType.Dagger: return "匕首";
            default: return "武器";
        }
    }

    private static string GetRarityLabel(LootRarity rarity)
    {
        switch (rarity)
        {
            case LootRarity.Trash: return "劣质";
            case LootRarity.Common: return "普通";
            case LootRarity.Uncommon: return "优秀";
            case LootRarity.Rare: return "稀有";
            case LootRarity.Epic: return "史诗";
            case LootRarity.Legendary: return "传说";
            default: return "普通";
        }
    }
}
