using System.Collections.Generic;
using UnityEngine;

public sealed partial class RelicExecutionContext
{
    public static RelicExecutionContext CreateMove(RunState runState, RunGameManager runGameManager, BoardTileData tile, int moveCost, bool isDestinationUnvisited)
    {
        return new RelicExecutionContext
        {
            trigger = Relic_SO.RelicTrigger.OnMoveCostAboutToSpend,
            runState = runState,
            runGameManager = runGameManager,
            tile = tile,
            moveCost = moveCost,
            isDestinationUnvisited = isDestinationUnvisited,
            feedbackMessages = new List<string>()
        };
    }

    public static RelicExecutionContext CreateKeyCollected(
        RunState runState,
        RunGameManager runGameManager,
        BoardTileData tile,
        ActionPointSystem actionPointSystem)
    {
        return new RelicExecutionContext
        {
            trigger = Relic_SO.RelicTrigger.OnKeyCollected,
            runState = runState,
            runGameManager = runGameManager,
            tile = tile,
            actionPointSystem = actionPointSystem,
            feedbackMessages = new List<string>()
        };
    }

    public static RelicExecutionContext CreateCalamityApplied(
        RunState runState,
        RunGameManager runGameManager,
        Calamity_SO calamityData,
        int calamityTier)
    {
        return new RelicExecutionContext
        {
            trigger = Relic_SO.RelicTrigger.OnCalamityApplied,
            runState = runState,
            runGameManager = runGameManager,
            calamityData = calamityData,
            calamityTier = Mathf.Max(0, calamityTier),
            feedbackMessages = new List<string>()
        };
    }

    public static RelicExecutionContext CreateBattleLootRewardGranted(
        RunState runState,
        LootItem_SO grantedBattleLoot,
        IReadOnlyList<LootItem_SO> battleLootPool)
    {
        return new RelicExecutionContext
        {
            trigger = Relic_SO.RelicTrigger.OnBattleLootRewardGranted,
            runState = runState,
            grantedBattleLoot = grantedBattleLoot,
            battleLootPool = battleLootPool,
            bonusBattleLootRewards = new List<LootItem_SO>(),
            feedbackMessages = new List<string>()
        };
    }

    public static RelicExecutionContext CreateMerchantPriceCalculation(
        RunState runState,
        MerchantOfferState merchantOffer,
        int merchantBasePrice)
    {
        return new RelicExecutionContext
        {
            trigger = Relic_SO.RelicTrigger.OnMerchantPriceCalculated,
            runState = runState,
            merchantOffer = merchantOffer,
            merchantBasePrice = Mathf.Max(0, merchantBasePrice),
            merchantPriceMultiplier = 1f,
            feedbackMessages = new List<string>()
        };
    }

    public static RelicExecutionContext CreateMerchantPriceCalculation(
        RunState runState,
        BoardTileData tile,
        MerchantOfferState merchantOffer,
        int merchantBasePrice)
    {
        return new RelicExecutionContext
        {
            trigger = Relic_SO.RelicTrigger.OnMerchantPriceCalculated,
            runState = runState,
            tile = tile,
            merchantOffer = merchantOffer,
            merchantBasePrice = Mathf.Max(0, merchantBasePrice),
            merchantPriceMultiplier = 1f,
            feedbackMessages = new List<string>()
        };
    }

    public static RelicExecutionContext CreateMerchantPurchaseCompleted(
        RunState runState,
        BoardTileData tile,
        MerchantOfferState merchantOffer)
    {
        return new RelicExecutionContext
        {
            trigger = Relic_SO.RelicTrigger.OnMerchantPurchaseCompleted,
            runState = runState,
            tile = tile,
            merchantOffer = merchantOffer,
            feedbackMessages = new List<string>()
        };
    }

    public static RelicExecutionContext CreateMerchantStockCountCalculation(
        RunState runState,
        BoardTileData tile,
        int baseTargetCount)
    {
        return new RelicExecutionContext
        {
            trigger = Relic_SO.RelicTrigger.OnMerchantStockCountCalculated,
            runState = runState,
            tile = tile,
            merchantStockTargetCount = Mathf.Max(0, baseTargetCount),
            feedbackMessages = new List<string>()
        };
    }

    public static RelicExecutionContext CreateMerchantPurchaseAffordabilityCheck(
        RunState runState,
        BoardTileData tile,
        MerchantOfferState merchantOffer,
        int merchantBasePrice)
    {
        return new RelicExecutionContext
        {
            trigger = Relic_SO.RelicTrigger.OnMerchantPurchaseAffordabilityChecked,
            runState = runState,
            tile = tile,
            merchantOffer = merchantOffer,
            merchantBasePrice = Mathf.Max(0, merchantBasePrice),
            feedbackMessages = new List<string>()
        };
    }

    public static RelicExecutionContext CreateRelicAcquired(
        RunState runState,
        Relic_SO acquiredRelicData,
        int acquiredRelicAmount)
    {
        return new RelicExecutionContext
        {
            trigger = Relic_SO.RelicTrigger.OnRelicAcquired,
            runState = runState,
            acquiredRelicData = acquiredRelicData,
            acquiredRelicAmount = Mathf.Max(0, acquiredRelicAmount),
            feedbackMessages = new List<string>()
        };
    }

    public static RelicExecutionContext CreateActionPointsSpent(
        RunState runState,
        RunGameManager runGameManager,
        BoardTileData tile,
        int actionPointsSpent)
    {
        return new RelicExecutionContext
        {
            trigger = Relic_SO.RelicTrigger.OnActionPointsSpent,
            runState = runState,
            runGameManager = runGameManager,
            tile = tile,
            actionPointsSpent = Mathf.Max(0, actionPointsSpent),
            feedbackMessages = new List<string>()
        };
    }

    public static RelicExecutionContext CreateLootReforge(
        RunState runState,
        LootItem_SO firstLootMaterial,
        LootItem_SO secondLootMaterial,
        LootItem_SO resultLoot,
        IReadOnlyList<LootItem_SO> lootPool)
    {
        return new RelicExecutionContext
        {
            trigger = Relic_SO.RelicTrigger.OnLootReforge,
            runState = runState,
            firstReforgeLootMaterial = firstLootMaterial,
            secondReforgeLootMaterial = secondLootMaterial,
            reforgeResultLoot = resultLoot,
            lootReforgePool = lootPool,
            bonusLootReforgeRewards = new List<LootItem_SO>(),
            feedbackMessages = new List<string>()
        };
    }

    public static RelicExecutionContext CreateRelicReforge(
        RunState runState,
        Relic_SO firstRelicMaterial,
        Relic_SO secondRelicMaterial,
        Relic_SO resultRelic)
    {
        return new RelicExecutionContext
        {
            trigger = Relic_SO.RelicTrigger.OnRelicReforge,
            runState = runState,
            firstReforgeRelicMaterial = firstRelicMaterial,
            secondReforgeRelicMaterial = secondRelicMaterial,
            reforgeResultRelic = resultRelic,
            preservedReforgeMaterialSlot = -1,
            feedbackMessages = new List<string>()
        };
    }

    public static RelicExecutionContext CreateRestReforgeLimitCalculation(
        RunState runState,
        int baseRestReforgeCount)
    {
        return new RelicExecutionContext
        {
            trigger = Relic_SO.RelicTrigger.OnRestReforgeLimitCalculated,
            runState = runState,
            baseRestReforgeCount = Mathf.Max(0, baseRestReforgeCount),
            feedbackMessages = new List<string>()
        };
    }

    public static RelicExecutionContext CreateRestActionPointRestored(RunState runState)
    {
        return new RelicExecutionContext
        {
            trigger = Relic_SO.RelicTrigger.OnRestActionPointRestored,
            runState = runState,
            feedbackMessages = new List<string>()
        };
    }

    public static RelicExecutionContext CreateRestReforgeCompleted(RunState runState)
    {
        return new RelicExecutionContext
        {
            trigger = Relic_SO.RelicTrigger.OnRestReforgeCompleted,
            runState = runState,
            feedbackMessages = new List<string>()
        };
    }

    public static RelicExecutionContext CreateRestHealCompleted(RunState runState)
    {
        return new RelicExecutionContext
        {
            trigger = Relic_SO.RelicTrigger.OnRestHealCompleted,
            runState = runState,
            feedbackMessages = new List<string>()
        };
    }
}
