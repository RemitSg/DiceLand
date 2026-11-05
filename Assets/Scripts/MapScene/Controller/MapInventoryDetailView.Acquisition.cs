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
        if (headingText != null) headingText.text = acquisition.Item is Relic_SO ? "获得遗物" : "获得图纸";
        if (quantityText != null)
        {
            quantityText.text = "本次获得 ×" + acquisition.Amount;
            quantityText.enableAutoSizing = true;
            quantityText.fontSizeMin = 16; quantityText.fontSizeMax = 26;
        }
        if (closeLabel != null) closeLabel.text = remaining > 0 ? "下一件" : "继续";
        ConfigureActionButton(string.Empty, false, null, remaining > 0 ? "已收入背包 · 还有 " + remaining + " 项待查看" : "已收入背包");
    }

    private void ResetPresentationLabels()
    {
        if (headingText != null) headingText.text = "物品详情";
        if (closeLabel != null) closeLabel.text = "关闭";
        if (modalRoot != null) modalRoot.GetComponent<Button>().interactable = true;
    }

    private string BlueprintDetailDescription(CraftingBlueprint_SO blueprint)
    {
        return BlueprintRequirementPreviewService.BuildDescription(blueprint, inventoryContext);
    }
}
