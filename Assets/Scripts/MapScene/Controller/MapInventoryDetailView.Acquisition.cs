using System;
using UnityEngine.UI;

public partial class MapInventoryDetailView
{
    private RunState inventoryContext;
    public Func<bool> DismissAllowed { get; set; }

    public void ConfigureInventoryContext(RunState state)
    {
        inventoryContext = state;
        if (IsVisible && displayedItem is CraftingBlueprint_SO blueprint && descriptionText != null)
        {
            descriptionText.text = BlueprintDetailDescription(blueprint);
            if (modalRoot != null) RefreshDescriptionLayout(false);
        }
    }

    public void ShowAcquisition(RunItemAcquisitionBatch acquisition, int remaining)
    {
        if (acquisition?.Item is Relic_SO relic)
            Show(new RunRelicState { relicData = relic, amount = acquisition.Amount });
        else if (acquisition?.Item is CraftingBlueprint_SO blueprint)
            Show(new RunBlueprintState { blueprintData = blueprint, amount = acquisition.Amount });
        else { HideImmediately(); return; }
        RefreshAcquisitionPresentation(acquisition, remaining);
        if (modalRoot != null) modalRoot.GetComponent<Button>().interactable = false;
    }

    public void RefreshAcquisitionPresentation(RunItemAcquisitionBatch acquisition, int remaining)
    {
        if (headingText != null) headingText.text = acquisition.Item is Relic_SO ? GameText.Get("ui.map_inventory_detail_view.relic_acquired", "获得遗物") : GameText.Get("ui.map_inventory_detail_view.blueprint_acquired", "获得图纸");
        if (quantityText != null)
        {
            quantityText.text = GameText.Get("ui.map_inventory_detail_view.received", "本次获得 ×") + acquisition.Amount;
            quantityText.enableAutoSizing = true;
            quantityText.fontSizeMin = 16; quantityText.fontSizeMax = 26;
        }
        if (closeLabel != null) closeLabel.text = remaining > 0 ? GameText.Get("ui.map_inventory_detail_view.next_item", "下一件") : GameText.Get("ui.continue", "继续");
        ConfigureActionButton(string.Empty, false, null, remaining > 0 ? GameText.Get("ui.map_inventory_detail_view.added_to_inventory", "已收入背包 · 还有 ") + remaining + GameText.Get("ui.map_inventory_detail_view.more_to_review", " 项待查看") : GameText.Get("ui.map_inventory_detail_view.refresh_acquisition_presentation.added_to_inventory", "已收入背包"));
    }

    private void ResetPresentationLabels()
    {
        if (headingText != null) headingText.text = GameText.Get("ui.map_inventory_detail_view.item_details", "物品详情");
        if (closeLabel != null) closeLabel.text = GameText.Get("ui.close", "关闭");
        if (modalRoot != null) modalRoot.GetComponent<Button>().interactable = true;
    }

    private string BlueprintDetailDescription(CraftingBlueprint_SO blueprint)
    {
        return BlueprintRequirementPreviewService.BuildDescription(blueprint, inventoryContext);
    }
}
