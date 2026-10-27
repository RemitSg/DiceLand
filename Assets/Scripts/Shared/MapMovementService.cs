using System.Collections.Generic;
using UnityEngine;

public sealed class MapMovementCycleFlags
{
    public bool deferPendingCalamityUntilMapResume;
    public bool deferPendingCalamityUntilOverlayClosed;
    public bool suppressPendingCalamityOnNextRestActionPointRestore;

    public MapMovementCycleFlags Clone()
    {
        return new MapMovementCycleFlags
        {
            deferPendingCalamityUntilMapResume = deferPendingCalamityUntilMapResume,
            deferPendingCalamityUntilOverlayClosed = deferPendingCalamityUntilOverlayClosed,
            suppressPendingCalamityOnNextRestActionPointRestore = suppressPendingCalamityOnNextRestActionPointRestore
        };
    }
}

public sealed class MapMovementExecutionRequest
{
    public RunState runState;
    public RunGameManager runGameManager;
    public BoardTileData tile;
    public int moveCost;
    public ActionPointSystem actionPointSystem;
    public PlayerMapController playerMapController;
    public MapMovementCycleFlags previousCycleFlags;
    public System.Action<MapMovementCycleFlags> applyCycleFlags;
}

public sealed class MapMovementExecutionResult
{
    public bool moved;
    public bool shouldRefreshHudWithEmptyStatus;
    public string pendingRelicStatusMessage;
}

public static class MapMovementService
{
    public static void RecordUnexploredTileVisit(RunState state)
    {
        if (state.settlementStats == null) state.settlementStats = new RunSettlementStats();
        state.settlementStats.unexploredTilesVisited++;
    }

    public static bool TryTriggerVisitedEmptyTileAmbush(RunState runState, BoardManager boardManager, EnemyRoster_SO enemyRoster, BoardTileData tile)
    {
        if (runState == null
            || boardManager == null
            || enemyRoster == null
            || tile == null
            || tile.tileType != MapTileType.Empty
            || !tile.isVisited
            || !tile.isResolved)
        {
            return false;
        }

        int ambushChance = CalamitySystem.ResolveVisitedEmptyTileAmbushChance(runState.activeCalamities);
        if (ambushChance <= 0 || UnityEngine.Random.Range(0, 100) >= ambushChance)
            return false;

        tile.tileType = MapTileType.Battle;
        tile.isResolved = false;
        tile.assignedEnemyGroup = null;
        tile.assignedEvent = null;
        tile.previewTag = string.Empty;
        tile.encounterSeed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
        MapBattleEncounterService.EnsureTileAssignedEnemyGroup(enemyRoster, tile);
        tile.previewDangerRating = MapDangerRatingService.ResolveRating(tile.tileType, tile.dangerBand, tile.assignedEnemyGroup);

        return true;
    }

    public static void AdvanceExhaustedCycle(RunState state, ActionPointSystem points, DifficultyConfig_SO config, Queue<int> pendingTiers)
    {
        if (state == null || points == null) return;
        state.calamityTier++;
        state.currentCycle++;
        points.SetBasePoints(RunBootstrapService.ResolveActionPointLimitForTier(config, state.calamityTier));
        points.AdvanceCycle(0);
        pendingTiers.Enqueue(Mathf.Max(1, state.calamityTier));
    }

    public static RelicExecutionContext ApplyKeyCollected(RunState state, RunGameManager manager, BoardTileData tile, ActionPointSystem points)
    {
        if (state.settlementStats == null) state.settlementStats = new RunSettlementStats();
        state.settlementStats.keysCollected++;
        return RelicTriggerDispatcher.ProcessKeyCollected(state, manager, tile, points);
    }

    public static int GetMoveCost(RunState runState, BoardTileData tile)
    {
        if (tile == null || runState == null)
            return 0;

        if (tile.coord == runState.playerGridPosition)
            return 0;

        return tile.isVisited ? 1 : 2;
    }

    public static bool CanAttemptMove(
        RunState runState,
        ActionPointSystem actionPointSystem,
        BoardTileData tile,
        int moveCost)
    {
        if (actionPointSystem == null)
            return false;

        if (actionPointSystem.CanSpend(moveCost))
            return true;

        if (moveCost == 2 && actionPointSystem.CurrentPoints == 1)
            return true;

        return CanPotentiallyIgnoreMoveCostViaRelic(runState, tile);
    }

    public static bool CanPotentiallyIgnoreMoveCostViaRelic(RunState runState, BoardTileData tile)
    {
        if (runState == null || tile == null || runState.relicInventory == null)
            return false;

        bool requireUnvisited = !tile.isVisited;
        for (int i = 0; i < runState.relicInventory.Count; i++)
        {
            RunRelicState relicState = runState.relicInventory[i];
            if (relicState == null || relicState.relicData == null || relicState.amount <= 0)
                continue;

            List<Relic_SO.RelicBuiltInEffectEntry> effects = relicState.relicData.builtInEffects;
            if (effects == null)
                continue;

            for (int effectIndex = 0; effectIndex < effects.Count; effectIndex++)
            {
                Relic_SO.RelicBuiltInEffectEntry effect = effects[effectIndex];
                if (effect == null)
                    continue;
                if (effect.trigger != Relic_SO.RelicTrigger.OnMoveCostAboutToSpend)
                    continue;

                bool canIgnoreMoveCost =
                    effect.effectType == Relic_SO.RelicBuiltInEffectType.ChanceToIgnoreMoveCost &&
                    (!effect.requireUnvisitedTile || requireUnvisited);

                if (!canIgnoreMoveCost &&
                    effect.effectType == Relic_SO.RelicBuiltInEffectType.IgnoreMoveCostWhenDestinationVisited &&
                    !requireUnvisited)
                {
                    canIgnoreMoveCost = true;
                }

                if (!canIgnoreMoveCost &&
                    effect.effectType == Relic_SO.RelicBuiltInEffectType.IgnoreMoveCostWhenDestinationUnvisitedRestTile &&
                    requireUnvisited &&
                    tile.tileType == MapTileType.Rest)
                {
                    canIgnoreMoveCost = true;
                }

                if (!canIgnoreMoveCost)
                    continue;
                if (effect.chance <= 0f)
                    continue;

                return true;
            }
        }

        return false;
    }

    public static bool WillSpendExhaustCurrentCycle(ActionPointSystem actionPointSystem, int moveCost)
    {
        if (actionPointSystem == null)
            return false;

        int safeMoveCost = Mathf.Max(0, moveCost);
        if (safeMoveCost <= 0)
            return false;

        return actionPointSystem.CurrentPoints <= safeMoveCost;
    }

    public static bool ShouldPresentPendingCalamityAfterArrival(BoardTileData tile)
    {
        if (tile == null || tile.IsHostileTile)
            return false;

        switch (tile.tileType)
        {
            case MapTileType.Event:
            case MapTileType.Merchant:
            case MapTileType.Forge:
            case MapTileType.Rest:
            case MapTileType.Treasure:
            case MapTileType.SpecialDungeonExit:
                return false;
            default:
                return true;
        }
    }

    public static void SpendActionPointsWithOverflow(ActionPointSystem actionPointSystem, int amount)
    {
        if (actionPointSystem == null)
            return;

        int remaining = Mathf.Max(0, amount);
        int guard = 0;
        while (remaining > 0 && guard < 10000)
        {
            guard++;
            int currentPoints = actionPointSystem.CurrentPoints;
            if (currentPoints <= 0)
                break;

            int spendAmount = Mathf.Min(currentPoints, remaining);
            if (!actionPointSystem.TrySpend(spendAmount))
                break;

            remaining -= spendAmount;
        }
    }

    public static bool TryExecuteMove(
        MapMovementExecutionRequest request,
        out MapMovementExecutionResult result)
    {
        bool moved = TryExecuteMoveCore(request, out result);
        // Formal arrival occurs after cost/relic callbacks, when the animation ends.
        // Keep that order when the driver skips the animation.
        if (moved) request.playerMapController.FlushHeadlessArrival();
        return moved;
    }

    private static bool TryExecuteMoveCore(
        MapMovementExecutionRequest request,
        out MapMovementExecutionResult result)
    {
        result = new MapMovementExecutionResult();
        if (request == null ||
            request.tile == null ||
            request.playerMapController == null ||
            request.actionPointSystem == null)
        {
            return false;
        }

        BoardTileData tile = request.tile;
        int moveCost = Mathf.Max(0, request.moveCost);
        bool exhaustsCurrentCycle = WillSpendExhaustCurrentCycle(request.actionPointSystem, moveCost);
        MapMovementCycleFlags nextFlags = BuildCycleFlags(request.actionPointSystem, tile, moveCost, exhaustsCurrentCycle);

        RelicExecutionContext relicContext = RelicTriggerDispatcher.ProcessMoveCost(
            request.runState,
            request.runGameManager,
            tile,
            moveCost);

        if (relicContext.ignoreMoveCost)
        {
            bool movedWithoutCost = request.playerMapController.TryMoveTo(tile, 0);
            result.moved = movedWithoutCost;
            if (movedWithoutCost && relicContext.feedbackMessages != null && relicContext.feedbackMessages.Count > 0)
                result.pendingRelicStatusMessage = string.Join("\n", relicContext.feedbackMessages);
            return movedWithoutCost;
        }

        ApplyFlags(request, nextFlags);

        if (moveCost <= 0)
        {
            bool moved = request.playerMapController.TryMoveTo(tile, 0);
            result.moved = moved;
            return moved;
        }

        if (request.actionPointSystem.CanSpend(moveCost))
        {
            bool moved = request.playerMapController.TryMoveTo(tile, moveCost);
            if (!moved)
                RestoreFlags(request);

            if (moved && moveCost > 0)
                RelicTriggerDispatcher.ProcessActionPointsSpent(request.runState, request.runGameManager, tile, moveCost);

            result.moved = moved;
            return moved;
        }

        if (moveCost != 2 || request.actionPointSystem.CurrentPoints != 1)
        {
            RestoreFlags(request);
            return false;
        }

        if (!request.actionPointSystem.TrySpend(1))
        {
            RestoreFlags(request);
            return false;
        }

        if (!request.playerMapController.TryMoveTo(tile, 0))
        {
            RestoreFlags(request);
            return false;
        }

        bool spentOverflowPoint = request.actionPointSystem.TrySpend(1);
        if (spentOverflowPoint)
        {
            RelicTriggerDispatcher.ProcessActionPointsSpent(request.runState, request.runGameManager, tile, 2);
            result.moved = true;
            result.shouldRefreshHudWithEmptyStatus = true;
        }
        else
        {
            RestoreFlags(request);
        }

        return spentOverflowPoint;
    }

    private static MapMovementCycleFlags BuildCycleFlags(
        ActionPointSystem actionPointSystem,
        BoardTileData tile,
        int moveCost,
        bool exhaustsCurrentCycle)
    {
        return new MapMovementCycleFlags
        {
            deferPendingCalamityUntilMapResume = exhaustsCurrentCycle && (tile.IsHostileTile || tile.tileType == MapTileType.Rest),
            deferPendingCalamityUntilOverlayClosed = exhaustsCurrentCycle && (tile.tileType == MapTileType.Event || tile.tileType == MapTileType.Merchant || tile.tileType == MapTileType.Forge),
            suppressPendingCalamityOnNextRestActionPointRestore = tile.tileType == MapTileType.Rest && exhaustsCurrentCycle && actionPointSystem != null && actionPointSystem.CanSpend(moveCost)
        };
    }

    private static void ApplyFlags(MapMovementExecutionRequest request, MapMovementCycleFlags flags)
    {
        request.applyCycleFlags?.Invoke(flags);
    }

    private static void RestoreFlags(MapMovementExecutionRequest request)
    {
        if (request.previousCycleFlags != null)
            request.applyCycleFlags?.Invoke(request.previousCycleFlags);
    }
}
