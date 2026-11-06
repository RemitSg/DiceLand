public static partial class RelicSystem
{
    public static void RestoreDiagonalMapMovement(RunState runState)
    {
        if (runState == null || runState.allowDiagonalMapMovement || runState.relicInventory == null)
            return;

        foreach (RunRelicState relic in runState.relicInventory)
        {
            if (relic == null || relic.amount <= 0 || relic.relicData == null || relic.relicData.builtInEffects == null)
                continue;

            foreach (Relic_SO.RelicBuiltInEffectEntry effect in relic.relicData.builtInEffects)
            {
                if (effect != null && effect.trigger == Relic_SO.RelicTrigger.OnRelicAcquired &&
                    effect.effectType == Relic_SO.RelicBuiltInEffectType.EnableDiagonalMapMovement && effect.chance >= 1f)
                {
                    // Recover the unlock without replaying one-time acquisition rewards.
                    runState.allowDiagonalMapMovement = true;
                    return;
                }
            }
        }
    }

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
