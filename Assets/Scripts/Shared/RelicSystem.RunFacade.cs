public static partial class RelicSystem
{
    public static int CalculateMerchantOfferPrice(RunState runState, MerchantOfferState offer)
    {
        return RelicMerchantService.CalculateOfferPrice(runState, null, offer);
    }

    public static int CalculateMerchantOfferPrice(RunState runState, BoardTileData tile, MerchantOfferState offer)
    {
        return RelicMerchantService.CalculateOfferPrice(runState, tile, offer);
    }

    public static void NotifyMerchantPurchaseCompleted(RunState runState, BoardTileData tile, MerchantOfferState offer)
    {
        RelicMerchantService.NotifyPurchaseCompleted(runState, tile, offer);
    }

    public static void ResetMerchantVisitState(BoardTileData tile)
    {
        RelicMerchantService.ResetVisitState(tile);
    }

    public static int CalculateRestReforgeLimit(RunState runState, int baseLimit)
    {
        return RelicRestReforgeService.CalculateLimit(runState, baseLimit);
    }
}
