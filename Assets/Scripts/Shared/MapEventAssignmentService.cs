using System.Collections.Generic;
using UnityEngine;

public static class MapEventAssignmentService
{
    private const int EventSeedSalt = 0x53A4E71;

    public static void EnsureTileAssignedEvent(
        MapEventPool_SO eventPool,
        RunState runState,
        BoardTileData tile)
    {
        if (tile == null || tile.tileType != MapTileType.Event)
            return;
        if (tile.assignedEvent != null)
            return;

        MapEvent_SO selectedEvent = SelectEventForTile(eventPool, runState, tile);
        if (selectedEvent == null)
            return;

        tile.assignedEvent = selectedEvent;
        runState?.MarkEventDrawn(selectedEvent);
    }

    public static MapEvent_SO SelectEventForTile(
        MapEventPool_SO eventPool,
        RunState runState,
        BoardTileData tile,
        System.Random rng = null)
    {
        if (eventPool == null || tile == null)
            return null;

        List<MapEvent_SO> availableEvents = BuildAvailableEvents(eventPool, runState);
        if (availableEvents.Count == 0)
            return null;

        System.Random resolvedRng = rng ?? new System.Random(tile.encounterSeed ^ EventSeedSalt);
        return availableEvents[resolvedRng.Next(0, availableEvents.Count)];
    }

    public static List<MapEvent_SO> BuildAvailableEvents(MapEventPool_SO eventPool, RunState runState)
    {
        List<MapEvent_SO> availableEvents = new List<MapEvent_SO>();
        if (eventPool == null)
            return availableEvents;

        IReadOnlyList<MapEvent_SO> validEvents = eventPool.GetValidEvents();
        if (validEvents == null || validEvents.Count == 0)
            return availableEvents;

        for (int i = 0; i < validEvents.Count; i++)
        {
            MapEvent_SO eventData = validEvents[i];
            if (eventData == null)
                continue;
            if (runState != null && runState.HasDrawnEvent(eventData))
                continue;

            availableEvents.Add(eventData);
        }

        return availableEvents;
    }
}
