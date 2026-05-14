using System;
using System.Collections.Generic;

public static partial class RelicTriggerDispatcher
{
    public static int CalculateMerchantOfferPrice(RunState runState, BoardTileData tile, MerchantOfferState offer)
    {
        return RelicMerchantService.CalculateOfferPrice(runState, tile, offer);
    }

    public static void NotifyMerchantPurchaseCompleted(RunState runState, BoardTileData tile, MerchantOfferState offer)
    {
        RelicMerchantService.NotifyPurchaseCompleted(runState, tile, offer);
    }

    public static int CalculateRestReforgeLimit(RunState runState, int baseLimit)
    {
        return RelicRestReforgeService.CalculateLimit(runState, baseLimit);
    }

    public static RelicExecutionContext ProcessMoveCost(
        RunState runState,
        RunGameManager runGameManager,
        BoardTileData tile,
        int moveCost,
        Action<string> logFeedback = null)
    {
        RelicExecutionContext context = RelicExecutionContext.CreateMove(
            runState,
            runGameManager,
            tile,
            moveCost,
            tile == null || !tile.isVisited);
        ProcessRunRelics(runState, context, logFeedback);
        return context;
    }

    public static RelicExecutionContext ProcessKeyCollected(
        RunState runState,
        RunGameManager runGameManager,
        BoardTileData tile,
        ActionPointSystem actionPointSystem,
        Action<string> logFeedback = null)
    {
        RelicExecutionContext context = RelicExecutionContext.CreateKeyCollected(
            runState,
            runGameManager,
            tile,
            actionPointSystem);
        ProcessRunRelics(runState, context, logFeedback);
        return context;
    }

    public static RelicExecutionContext ProcessCalamityApplied(
        RunState runState,
        RunGameManager runGameManager,
        Calamity_SO calamityData,
        int calamityTier,
        Action<string> logFeedback = null)
    {
        RelicExecutionContext context = RelicExecutionContext.CreateCalamityApplied(
            runState,
            runGameManager,
            calamityData,
            calamityTier);
        ProcessRunRelics(runState, context, logFeedback);
        return context;
    }

    public static RelicExecutionContext ProcessRelicAcquired(
        RunState runState,
        Relic_SO acquiredRelicData,
        int acquiredRelicAmount,
        Action<string> logFeedback = null)
    {
        RelicExecutionContext context = RelicExecutionContext.CreateRelicAcquired(
            runState,
            acquiredRelicData,
            acquiredRelicAmount);
        ProcessRunRelics(runState, context, logFeedback);
        return context;
    }

    public static RelicExecutionContext ProcessActionPointsSpent(
        RunState runState,
        RunGameManager runGameManager,
        BoardTileData tile,
        int actionPointsSpent,
        Action<string> logFeedback = null)
    {
        RelicExecutionContext context = RelicExecutionContext.CreateActionPointsSpent(
            runState,
            runGameManager,
            tile,
            actionPointsSpent);
        ProcessRunRelics(runState, context, logFeedback);
        return context;
    }

    public static RelicExecutionContext ProcessLootReforge(
        RunState runState,
        LootItem_SO firstLootMaterial,
        LootItem_SO secondLootMaterial,
        LootItem_SO resultLoot,
        IReadOnlyList<LootItem_SO> lootPool,
        Action<string> logFeedback = null)
    {
        RelicExecutionContext context = RelicExecutionContext.CreateLootReforge(
            runState,
            firstLootMaterial,
            secondLootMaterial,
            resultLoot,
            lootPool);
        ProcessRunRelics(runState, context, logFeedback);
        return context;
    }

    public static RelicExecutionContext ProcessRelicReforge(
        RunState runState,
        Relic_SO firstRelicMaterial,
        Relic_SO secondRelicMaterial,
        Relic_SO resultRelic,
        Action<string> logFeedback = null)
    {
        RelicExecutionContext context = RelicExecutionContext.CreateRelicReforge(
            runState,
            firstRelicMaterial,
            secondRelicMaterial,
            resultRelic);
        ProcessRunRelics(runState, context, logFeedback);
        return context;
    }
}
