using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class RunFlowAssignmentServiceTests
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
    public void RollChoices_ExcludesDrawnCalamitiesAndCanMarkPresentedChoices()
    {
        Calamity_SO skipped = CreateCalamity("skipped");
        Calamity_SO available = CreateCalamity("available");
        CalamityPool_SO pool = CreateCalamityPool(skipped, available);
        RunState runState = new RunState();
        runState.MarkCalamityDrawn(skipped);

        List<Calamity_SO> choices = CalamitySelectionService.RollChoices(
            pool,
            runState,
            calamityTier: 1,
            choiceCount: 2,
            rng: new System.Random(1));
        CalamitySelectionService.MarkChoicesDrawn(runState, choices);

        Assert.AreEqual(1, choices.Count);
        Assert.AreSame(available, choices[0]);
        Assert.IsTrue(runState.HasDrawnCalamity(available));
    }

    [Test]
    public void ApplyChosenCalamity_AddsSelectedCalamityAtRequestedTier()
    {
        Calamity_SO calamity = CreateCalamity("storm");
        RunState runState = new RunState();

        CalamitySelectionService.ApplyChosenCalamity(runState, calamity, 3);

        Assert.AreEqual(1, runState.activeCalamities.Count);
        Assert.AreSame(calamity, runState.activeCalamities[0].calamityData);
        Assert.AreEqual(3, runState.activeCalamities[0].acquiredAtTier);
    }

    [Test]
    public void EnsureTileAssignedEvent_AssignsUndrawnEventAndMarksItDrawn()
    {
        MapEvent_SO skipped = CreateMapEvent("skipped");
        MapEvent_SO available = CreateMapEvent("available");
        MapEventPool_SO pool = CreateMapEventPool(skipped, available);
        RunState runState = new RunState();
        runState.MarkEventDrawn(skipped);
        BoardTileData tile = new BoardTileData
        {
            tileType = MapTileType.Event,
            encounterSeed = 9
        };

        MapEventAssignmentService.EnsureTileAssignedEvent(pool, runState, tile);

        Assert.AreSame(available, tile.assignedEvent);
        Assert.IsTrue(runState.HasDrawnEvent(available));
    }

    [Test]
    public void EnsureTileAssignedEvent_DoesNotOverwriteExistingOrNonEventTiles()
    {
        MapEvent_SO existing = CreateMapEvent("existing");
        MapEvent_SO replacement = CreateMapEvent("replacement");
        MapEventPool_SO pool = CreateMapEventPool(replacement);
        RunState runState = new RunState();
        BoardTileData assignedTile = new BoardTileData
        {
            tileType = MapTileType.Event,
            assignedEvent = existing,
            encounterSeed = 3
        };
        BoardTileData battleTile = new BoardTileData
        {
            tileType = MapTileType.Battle,
            encounterSeed = 4
        };

        MapEventAssignmentService.EnsureTileAssignedEvent(pool, runState, assignedTile);
        MapEventAssignmentService.EnsureTileAssignedEvent(pool, runState, battleTile);

        Assert.AreSame(existing, assignedTile.assignedEvent);
        Assert.IsNull(battleTile.assignedEvent);
        Assert.IsFalse(runState.HasDrawnEvent(replacement));
    }

    private Calamity_SO CreateCalamity(string id)
    {
        Calamity_SO calamity = ScriptableObject.CreateInstance<Calamity_SO>();
        calamity.calamityId = id;
        calamity.calamityName = id;
        createdObjects.Add(calamity);
        return calamity;
    }

    private CalamityPool_SO CreateCalamityPool(params Calamity_SO[] calamities)
    {
        CalamityPool_SO pool = ScriptableObject.CreateInstance<CalamityPool_SO>();
        createdObjects.Add(pool);

        CalamityPool_SO.CalamityTierEntry tierEntry = new CalamityPool_SO.CalamityTierEntry
        {
            calamityTier = 1,
            calamities = new List<Calamity_SO>(calamities)
        };
        SetPrivateField(pool, "calamityTiers", new List<CalamityPool_SO.CalamityTierEntry> { tierEntry });
        return pool;
    }

    private MapEvent_SO CreateMapEvent(string id)
    {
        MapEvent_SO eventData = ScriptableObject.CreateInstance<MapEvent_SO>();
        eventData.eventId = id;
        eventData.eventName = id;
        createdObjects.Add(eventData);
        return eventData;
    }

    private MapEventPool_SO CreateMapEventPool(params MapEvent_SO[] events)
    {
        MapEventPool_SO pool = ScriptableObject.CreateInstance<MapEventPool_SO>();
        createdObjects.Add(pool);
        SetPrivateField(pool, "events", new List<MapEvent_SO>(events));
        return pool;
    }

    private static void SetPrivateField<TTarget, TValue>(TTarget target, string fieldName, TValue value)
    {
        FieldInfo field = typeof(TTarget).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field);
        field.SetValue(target, value);
    }
}
