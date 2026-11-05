using System;

public partial class RunState
{
    [NonSerialized] private RunItemAcquisitionQueue itemAcquisitions;
    public RunItemAcquisitionQueue ItemAcquisitions => itemAcquisitions ?? (itemAcquisitions = new RunItemAcquisitionQueue());

    public void AddPreviewedBlueprint(CraftingBlueprint_SO blueprint, int amount = 1)
    {
        AddBlueprintCore(blueprint, amount, false);
    }

    public void AddPreviewedRelic(Relic_SO relic, int amount = 1)
    {
        AddRelicCore(relic, amount, true, amount);
    }

    public void AddRelicWithPreviewedAmount(Relic_SO relic, int amount, int previewedAmount)
    {
        AddRelicCore(relic, amount, true, Math.Max(0, previewedAmount));
    }
}
