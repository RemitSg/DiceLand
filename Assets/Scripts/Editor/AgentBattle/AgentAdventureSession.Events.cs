using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public sealed partial class AgentAdventureSession
{
    private MapEvent_SO currentEvent, nextEvent;
    private int eventDepth;
    private string eventResultContent, eventResultMessage;
    private readonly Dictionary<string, MapEventLootDiscardPreview> eventDiscardCache = new Dictionary<string, MapEventLootDiscardPreview>();
    private AgentAdventureEventOption[] eventOptions = Array.Empty<AgentAdventureEventOption>();
    private BattleEncounterData pendingEventEncounter;
    private SpecialDungeon_SO pendingEventDungeon;

    private bool OpenEventLocation(BoardTileData tile)
    {
        if (tile.tileType != MapTileType.Event || tile.isResolved) return false;
        tile.isVisited = true;
        map.WithRandom(() => { MapEventAssignmentService.EnsureTileAssignedEvent(map.Configuration.events, map.Snapshot.runState, tile); return true; });
        if (tile.assignedEvent == null)
        { message = "当前未配置可触发事件。"; return false; }
        currentEvent = tile.assignedEvent; nextEvent = null; eventDepth = 0;
        eventResultContent = eventResultMessage = null;
        eventDiscardCache.Clear();
        phase = "event";
        FreezeEventOptions();
        message = "触发事件：" + currentEvent.DisplayName;
        return true;
    }

    private void FreezeEventOptions()
    {
        // Like the overlay, roll discard previews once when presenting the options, never during Observe.
        eventOptions = map.WithRandom(() => currentEvent.GetValidOptions().Select((option, index) =>
        {
            MapEventOptionPreview preview = MapEventOptionService.BuildPreview(map.Snapshot.runState, option, eventDiscardCache);
            bool supported = option.customHandler == null;
            return new AgentAdventureEventOption { index = index, id = option.optionId, label = option.DisplayLabel,
                consequence = option.consequenceDescription, requiredMoney = preview.requiredMoney, additionalDetail = preview.additionalDetail,
                nextEvent = option.nextEvent != null ? option.nextEvent.DisplayName : null,
                selectable = preview.isSelectable && supported,
                unavailableReason = supported ? preview.unavailableReason : "This custom event handler requires a headless adapter." };
        }).ToArray());
    }

    private AgentAdventureEvent DescribeEvent()
    {
        if (phase != "event" && phase != "event_result" && phase != "dungeon_pending") return null;
        return new AgentAdventureEvent { id = AssetId(currentEvent), name = currentEvent != null ? currentEvent.DisplayName : "",
            content = currentEvent != null ? currentEvent.eventContent : "", depth = eventDepth,
            options = phase == "event" ? eventOptions : Array.Empty<AgentAdventureEventOption>(),
            resultContent = eventResultContent, resultMessage = eventResultMessage, nextEvent = AssetId(nextEvent),
            hasPendingBattle = pendingEventEncounter != null, pendingDungeon = AssetId(pendingEventDungeon),
            blockedReason = nextEvent != null && eventDepth >= MapEventOptionService.MaxNestedEventDepth ? "事件嵌套层数过深，请检查事件配置。" : null };
    }

    private IEnumerable<AgentAdventureAction> EventActions()
    {
        if (phase == "event")
            foreach (var option in eventOptions.Where(o => o.selectable))
                yield return new AgentAdventureAction { op = "run_choose_event", choiceIndex = option.index };
        else if (phase == "event_result" && (nextEvent == null || eventDepth < MapEventOptionService.MaxNestedEventDepth))
            yield return new AgentAdventureAction { op = "run_continue_event" };
    }

    private void ChooseEvent(int index)
    {
        MapEventOptionData option = currentEvent.GetValidOptions()[index];
        map.WithRandom(() =>
        {
            bool success = MapEventOptionService.TryExecute(new MapEventBuiltInEffectExecutionRequest
            {
                runState = map.Snapshot.runState, actionPointSystem = map.Points, currentEventTile = CurrentTile, optionData = option,
                levelTable = map.Configuration.levels, sourceSceneName = map.Snapshot.sceneName, battleSceneName = "AgentBattleSandbox",
                encounterSeedSalt = UnityEngine.Random.Range(int.MinValue, int.MaxValue), randomLootDiscardPreviewCache = eventDiscardCache
            }, currentEvent, null, out MapEventBuiltInEffectExecutionResult result);
            ReleasePendingEventEncounter();
            pendingEventEncounter = result.pendingEncounterData; pendingEventDungeon = result.pendingSpecialDungeon;
            if (!success) throw new InvalidOperationException(result.message);
            eventResultMessage = message = result.message;
            eventResultContent = !string.IsNullOrWhiteSpace(result.resultContent) ? result.resultContent
                : !string.IsNullOrWhiteSpace(option.resultContent) ? option.resultContent
                : !string.IsNullOrWhiteSpace(result.message) ? result.message : option.consequenceDescription;
            nextEvent = result.nextEventOverride != null ? result.nextEventOverride : option.nextEvent;
            return true;
        });
        phase = "event_result";
    }

    private void ContinueEvent()
    {
        if (nextEvent != null)
        {
            currentEvent = nextEvent; nextEvent = null; eventDepth++;
            eventResultContent = eventResultMessage = null; phase = "event"; FreezeEventOptions();
            message = currentEvent.DisplayName;
            return;
        }
        eventDiscardCache.Clear(); eventOptions = Array.Empty<AgentAdventureEventOption>();
        if (pendingEventEncounter != null)
        {
            bool ownsGroup = !EditorUtility.IsPersistent(pendingEventEncounter.enemyGroup);
            bridge = new AgentAdventureBattleBridge(map.Snapshot, pendingEventEncounter, map.Configuration.levels, ownsGroup, maxRounds);
            pendingEventEncounter = null;
            phase = "battle"; return;
        }
        if (pendingEventDungeon != null)
        {
            SpecialDungeon_SO dungeon = pendingEventDungeon; pendingEventDungeon = null;
            if (map.IsInSpecialDungeon) message = "当前已经在特殊副本中，无法重复进入。";
            else if (!map.EnterSpecialDungeon(dungeon)) throw new InvalidOperationException("Cannot enter the special dungeon.");
            else message = "进入特殊副本：" + dungeon.DisplayName + "。副本内移动不消耗行动点。";
            phase = "map"; ClearDeferrals(); PresentCalamity(); return;
        }
        phase = "map"; ClearDeferrals(); PresentCalamity();
    }

    private void ReleasePendingEventEncounter()
    {
        if (pendingEventEncounter?.enemyGroup != null && !EditorUtility.IsPersistent(pendingEventEncounter.enemyGroup))
            UnityEngine.Object.DestroyImmediate(pendingEventEncounter.enemyGroup);
        pendingEventEncounter = null;
    }
}
