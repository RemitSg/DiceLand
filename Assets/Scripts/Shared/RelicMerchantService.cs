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
        int relicModifiedPrice = Mathf.Max(0, Mathf.FloorToInt((basePrice * multiplier) + 0.5f));
        return CalamitySystem.ApplyMerchantPriceModifiers(relicModifiedPrice, runState.activeCalamities);
    }

    public static int CalculateMerchantStockTargetCount(RunState runState, BoardTileData tile, int baseTargetCount)
    {
        int targetCount = Mathf.Max(0, baseTargetCount);
        if (runState == null)
            return targetCount;

        RelicExecutionContext context = RelicExecutionContext.CreateMerchantStockCountCalculation(runState, tile, targetCount);
        RelicSystem.Process(runState, Relic_SO.RelicTrigger.OnMerchantStockCountCalculated, context);
        return Mathf.Max(0, context.merchantStockTargetCount);
    }

    public static bool CanPurchaseWithPositiveMoneyOverride(RunState runState, BoardTileData tile, MerchantOfferState offer, int price)
    {
        if (runState == null || offer == null || runState.money <= 0)
            return false;

        RelicExecutionContext context = RelicExecutionContext.CreateMerchantPurchaseAffordabilityCheck(runState, tile, offer, price);
        RelicSystem.Process(runState, Relic_SO.RelicTrigger.OnMerchantPurchaseAffordabilityChecked, context);
        return context.allowMerchantPurchaseWithPositiveMoney;
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
