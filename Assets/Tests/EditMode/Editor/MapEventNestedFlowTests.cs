using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class MapEventNestedFlowTests
{
    private readonly List<Object> createdObjects = new List<Object>();

    [TearDown]
    public void TearDown()
    {
        for (int i = createdObjects.Count - 1; i >= 0; i--)
        {
            if (createdObjects[i] != null)
                Object.DestroyImmediate(createdObjects[i]);
        }

        createdObjects.Clear();
    }

    [Test]
    public void MapEventOption_CanReferenceFollowUpEvent()
    {
        MapEvent_SO parent = CreateEvent("parent");
        MapEvent_SO child = CreateEvent("child");
        parent.options.Add(new MapEventOptionData
        {
            optionId = "continue",
            optionLabel = "Continue",
            nextEvent = child
        });

        IReadOnlyList<MapEventOptionData> options = parent.GetValidOptions();

        Assert.AreSame(child, options[0].nextEvent);
    }

    [Test]
    public void SaveAssetCatalog_IncludesNestedFollowUpEvents()
    {
        MapEvent_SO parent = CreateEvent("parent");
        MapEvent_SO child = CreateEvent("child");
        parent.options.Add(new MapEventOptionData
        {
            optionId = "continue",
            nextEvent = child
        });
        BoardTileData tile = new BoardTileData
        {
            tileType = MapTileType.Event,
            assignedEvent = parent
        };

        MainMenuSaveAssetCatalog catalog = RunSaveAssetCatalogBuilder.Build(
            new RunSaveAssetCatalogBuildRequest
            {
                boardTiles = new List<BoardTileData> { tile }
            });

        Assert.AreSame(parent, catalog.ResolveEvent("parent"));
        Assert.AreSame(child, catalog.ResolveEvent("child"));
    }

    private MapEvent_SO CreateEvent(string id)
    {
        MapEvent_SO eventData = ScriptableObject.CreateInstance<MapEvent_SO>();
        eventData.eventId = id;
        eventData.eventName = id;
        createdObjects.Add(eventData);
        return eventData;
    }
}
