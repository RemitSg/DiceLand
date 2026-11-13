using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class MistCollectorEventTests
{
    private const string EventPath = "Assets/Data/Events/035 收雾人/035 收雾人.asset";
    private const string RelicPath = "Assets/Data/Relics/162 瓶中雾/瓶中雾.asset";
    private MapEvent_SO eventData;
    private Relic_SO mist;
    private GameObject host;
    private ActionPointSystem points;
    private Random.State random;

    [SetUp]
    public void SetUp()
    {
        random = Random.state;
        eventData = AssetDatabase.LoadAssetAtPath<MapEvent_SO>(EventPath);
        mist = AssetDatabase.LoadAssetAtPath<Relic_SO>(RelicPath);
        host = new GameObject("Mist Collector test");
        points = host.AddComponent<ActionPointSystem>();
        points.Initialize(6);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(host);
        Random.state = random;
    }

    [Test]
    public void EventAndFixedRewardAreAvailableThroughProductionAssetsAndSaveCatalog()
    {
        var pool = AssetDatabase.LoadAssetAtPath<MapEventPool_SO>("Assets/Data/Events/MapEventPool.asset");
        Assert.AreSame(eventData, pool.GetValidEvents().Single(item => item.eventId == "035"));
        Assert.IsNotNull(MapEventSceneArt.Resolve(eventData));
        Assert.IsNotNull(mist.icon);
        Assert.AreEqual("162", mist.relicId);
        Assert.AreEqual(LootRarity.Common, mist.rarity);
        Assert.IsTrue(eventData.options.All(option => option.resolveEvent));

        var catalog = RunSaveAssetCatalogBuilder.Build(new RunSaveAssetCatalogBuildRequest { eventPool = pool });
        Assert.AreSame(mist, catalog.ResolveRelic(mist.relicId));
        Assert.AreSame(eventData, catalog.ResolveEvent(eventData.eventId));
        var encyclopedia = Resources.Load<EncyclopediaCatalog>("MainMenu/EncyclopediaCatalog");
        Assert.IsTrue(encyclopedia.Entries.Any(entry => entry.Asset == mist));
    }

    [TestCase(0, 4)]
    [TestCase(1, 5)]
    [TestCase(2, 6)]
    [TestCase(3, 6)]
    public void HelpingPaysTheAdjustedCostBeforeGrantingBottledMist(int owned, int expectedPoints)
    {
        var run = new RunState();
        run.AddRelic(mist, owned);
        var request = Request(run, 0);
        var preview = MapEventOptionService.BuildPreview(run, request.optionData, null, request.currentEventTile);
        Assert.AreEqual(6, points.CurrentPoints);
        if (owned == 0) Assert.IsEmpty(preview.additionalDetail);
        else StringAssert.Contains($"2 → {6 - expectedPoints}", preview.additionalDetail);

        Assert.IsTrue(MapEventOptionService.TryExecute(request, eventData, null, out var result), result.message);
        Assert.AreEqual(expectedPoints, points.CurrentPoints);
        Assert.AreEqual(owned + 1, run.GetRelicAmount(mist));
        Assert.IsTrue(request.currentEventTile.isResolved);
        Assert.AreEqual(1, run.settlementStats.eventTilesCompleted);
    }

    [Test]
    public void EventLossDoesNotDiscountMovementAndUsesTheNormalCycleOnExhaustion()
    {
        var run = new RunState();
        run.AddRelic(mist);
        var request = Request(run, 0);
        Assert.AreEqual(2, RelicTriggerDispatcher.ProcessMoveCost(run, null, request.currentEventTile, 2).moveCost);
        Assert.AreEqual(2, RelicTriggerDispatcher.CalculateEventActionPointLoss(run, new BoardTileData { tileType = MapTileType.Rest }, 2));
        var pending = new Queue<int>();
        points.ActionPointsExhausted += () => MapMovementService.AdvanceExhaustedCycle(run, points, null, pending);
        points.RestoreState(6, 0, 1);

        Assert.IsTrue(MapEventOptionService.TryExecute(request, eventData, null, out _));
        Assert.AreEqual(1, pending.Count);
        Assert.AreEqual(points.CurrentMaxPoints, points.CurrentPoints);
    }

    [TestCase(3, 4)]
    [TestCase(6, 6)]
    public void LeavingDiscardsOnePreviewedLootAndRestoresOnePointUpToTheCap(int before, int expected)
    {
        var run = new RunState();
        var loot = AssetDatabase.LoadAssetAtPath<LootItem_SO>("Assets/Data/Loots/LootItems/000 史莱姆粘液/SlimeMucus.asset");
        run.AddLoot(loot, 3);
        run.AddRelic(mist);
        points.RestoreState(6, 0, before);
        var request = Request(run, 1);
        var preview = MapEventOptionService.BuildPreview(run, request.optionData, request.randomLootDiscardPreviewCache, request.currentEventTile);
        Assert.IsTrue(preview.isSelectable);
        StringAssert.Contains(loot.LocalizedName, preview.additionalDetail);

        Assert.IsTrue(MapEventOptionService.TryExecute(request, eventData, null, out var result), result.message);
        Assert.AreEqual(expected, points.CurrentPoints);
        Assert.AreEqual(2, run.GetLootAmount(loot));
        Assert.AreEqual(1, run.GetRelicAmount(mist));
        Assert.IsTrue(request.currentEventTile.isResolved);
    }

    [Test]
    public void LeavingWithNoLootCannotRestoreActionPoints()
    {
        var run = new RunState();
        points.RestoreState(6, 0, 3);
        var request = Request(run, 1);
        Assert.IsFalse(MapEventOptionService.BuildPreview(run, request.optionData, null, request.currentEventTile).isSelectable);
        Assert.IsFalse(MapEventOptionService.TryExecute(request, eventData, null, out _));
        Assert.AreEqual(3, points.CurrentPoints);
        Assert.IsFalse(request.currentEventTile.isResolved);
    }

    private MapEventBuiltInEffectExecutionRequest Request(RunState run, int index) => new MapEventBuiltInEffectExecutionRequest
    {
        runState = run,
        actionPointSystem = points,
        currentEventTile = new BoardTileData { tileType = MapTileType.Event, assignedEvent = eventData },
        optionData = eventData.options[index],
        randomLootDiscardPreviewCache = new Dictionary<string, MapEventLootDiscardPreview>()
    };
}
