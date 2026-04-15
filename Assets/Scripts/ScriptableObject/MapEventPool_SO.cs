using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MapEventPool", menuName = "Events/Map Event Pool")]
public class MapEventPool_SO : ScriptableObject
{
    [SerializeField] private List<MapEvent_SO> events = new List<MapEvent_SO>();

    public IReadOnlyList<MapEvent_SO> GetValidEvents()
    {
        if (events == null || events.Count == 0)
            return Array.Empty<MapEvent_SO>();

        List<MapEvent_SO> valid = new List<MapEvent_SO>(events.Count);
        for (int i = 0; i < events.Count; i++)
        {
            MapEvent_SO eventData = events[i];
            if (eventData != null)
                valid.Add(eventData);
        }

        return valid;
    }

    public MapEvent_SO GetRandomEvent(System.Random rng)
    {
        IReadOnlyList<MapEvent_SO> valid = GetValidEvents();
        if (valid.Count == 0)
            return null;

        if (rng == null)
            rng = new System.Random();

        return valid[rng.Next(0, valid.Count)];
    }
}
