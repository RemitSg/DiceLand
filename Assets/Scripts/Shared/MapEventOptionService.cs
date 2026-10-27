using System.Collections.Generic;
using UnityEngine;

public sealed class MapEventOptionPreview
{
    public int requiredMoney;
    public bool isSelectable = true;
    public string additionalDetail, unavailableReason;
}

// Selection orchestration shared by the map overlay and the agent adapter.
public static class MapEventOptionService
{
    public const int MaxNestedEventDepth = 16;

    public static int RequiredMoney(MapEventOptionData option, int currentMoney)
    {
        if (option == null) return 0;
        return Mathf.Max(0, option.requiredMoney) + Mathf.FloorToInt(Mathf.Max(0, currentMoney) * (Mathf.Max(0, option.requiredMoneyPercent) / 100f));
    }

    public static MapEventOptionPreview BuildPreview(RunState state, MapEventOptionData option,
        IDictionary<string, MapEventLootDiscardPreview> cache)
    {
        var preview = new MapEventOptionPreview();
        if (state == null || option == null) return preview;
        preview.requiredMoney = RequiredMoney(option, state.money);
        MapEventLootCostPreviewData loot = MapEventLootCostService.BuildPreview(state, option, cache);
        bool enough = preview.requiredMoney <= 0 || state.money >= preview.requiredMoney;
        preview.isSelectable = loot.isSelectable && enough;
        preview.additionalDetail = loot.additionalDetail;
        preview.unavailableReason = enough ? loot.unavailableReason : $"金钱不足，需要 {preview.requiredMoney} 金钱。";
        return preview;
    }

    public static bool TryExecute(MapEventBuiltInEffectExecutionRequest request, MapEvent_SO eventData,
        RunGameManager manager, out MapEventBuiltInEffectExecutionResult result)
    {
        result = new MapEventBuiltInEffectExecutionResult();
        if (request?.runState == null || request.currentEventTile == null || request.optionData == null || eventData == null)
        { result.message = "事件执行失败。"; return false; }
        RunState state = request.runState;
        MapEventOptionData option = request.optionData;
        request.moneyBeforeSelection = Mathf.Max(0, state.money);
        int cost = request.selectionMoneyCost = RequiredMoney(option, request.moneyBeforeSelection);
        if (cost > 0 && !state.TrySpendMoney(cost))
        { result.message = $"金钱不足，需要 {cost} 金钱。"; return false; }

        bool success = MapEventBuiltInEffectExecutorService.TryExecute(request, out result);
        if (success && option.customHandler != null)
        {
            success = option.customHandler.Execute(new MapEventExecutionContext
            {
                runState = state, actionPointSystem = request.actionPointSystem, tile = request.currentEventTile,
                eventData = eventData, optionData = option, runGameManager = manager
            }, out string customMessage);
            if (!string.IsNullOrWhiteSpace(customMessage)) result.message = customMessage;
            else if (!success) result.message = "事件的自定义效果未能执行。";
        }
        if (!success)
        {
            // The formal flow refunds the selection fee; effects are not a transactional simulation.
            if (cost > 0) state.AddMoney(cost, countAsRunGain: false);
            result.success = false;
            return false;
        }

        if (result.nextEventOverride == null && option.nextEvent == null)
        {
            BoardTileData tile = request.currentEventTile;
            tile.isResolved = result.hasResolveEventOverride ? result.resolveEventOverride : option.resolveEvent;
            if (tile.isResolved)
            {
                tile.assignedEvent = null;
                if (state.settlementStats == null) state.settlementStats = new RunSettlementStats();
                state.settlementStats.eventTilesCompleted++;
            }
        }
        if (cost > 0) result.message = $"支付 {cost} 金钱。" + (string.IsNullOrWhiteSpace(result.message) ? "" : "\n" + result.message);
        result.success = true;
        return true;
    }
}
