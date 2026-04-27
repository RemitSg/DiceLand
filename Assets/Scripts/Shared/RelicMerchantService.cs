using UnityEngine;

public static class RelicMerchantService
{
    public static int CalculateOfferPrice(RunState runState, MerchantOfferState offer)
    {
        return CalculateOfferPrice(runState, null, offer);
    }

    public static int CalculateOfferPrice(RunState runState, BoardTileData tile, MerchantOfferState offer)
    {
        int basePrice = Mathf.Max(0, offer != null ? offer.price : 0);
        if (runState == null || offer == null)
            return basePrice;

        RelicExecutionContext context = RelicExecutionContext.CreateMerchantPriceCalculation(runState, tile, offer, basePrice);
        RelicSystem.Process(runState, Relic_SO.RelicTrigger.OnMerchantPriceCalculated, context);

        float multiplier = Mathf.Max(0f, 1f - (Mathf.Max(0, context.merchantDiscountPercent) / 100f));
        multiplier *= Mathf.Max(0f, context.merchantPriceMultiplier);
        return Mathf.Max(0, Mathf.FloorToInt((basePrice * multiplier) + 0.5f));
    }

    public static void NotifyPurchaseCompleted(RunState runState, BoardTileData tile, MerchantOfferState offer)
    {
        if (runState == null || tile == null || offer == null)
            return;

        RelicExecutionContext context = RelicExecutionContext.CreateMerchantPurchaseCompleted(runState, tile, offer);
        RelicSystem.Process(runState, Relic_SO.RelicTrigger.OnMerchantPurchaseCompleted, context);
    }

    public static void ResetVisitState(BoardTileData tile)
    {
        if (tile == null)
            return;

        tile.merchantPurchaseDiscountStacks = 0;
    }
}
