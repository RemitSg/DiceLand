using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class MapMovementServiceTests
{
    private GameObject root;
    private BoardManager board;
    private ActionPointSystem points;
    private PlayerMapController controller;
    private RunState runState;
    private BoardTileData origin;
    private BoardTileData target;
    private readonly List<Object> createdObjects = new List<Object>();

    [SetUp]
    public void SetUp()
    {
        root = new GameObject("MapMovementPreviewIntegrationTest");
        createdObjects.Add(root);
        board = root.AddComponent<BoardManager>();
        points = root.AddComponent<ActionPointSystem>();
        controller = root.AddComponent<PlayerMapController>();
        origin = new BoardTileData { coord = Vector2Int.zero, tileType = MapTileType.Empty };
        target = new BoardTileData { coord = Vector2Int.right, tileType = MapTileType.Battle };
        board.LoadBoard(2, origin.coord, target.coord, origin.coord, new[] { origin, target });
        points.Initialize(5);
        runState = new RunState { playerGridPosition = origin.coord };
        controller.Initialize(root.transform, board, points, null, runState);
        controller.SetPosition(origin);
    }

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
    public void ShouldPresentPendingCalamityAfterArrival_ReturnsTrueForEmptyTile()
    {
        BoardTileData tile = new BoardTileData
        {
            tileType = MapTileType.Empty,
            isVisited = true,
            isResolved = true
        };

        Assert.IsTrue(MapMovementService.ShouldPresentPendingCalamityAfterArrival(tile));
    }

    [Test]
    public void ShouldPresentPendingCalamityAfterArrival_ReturnsFalseForOverlayTiles()
    {
        Assert.IsFalse(MapMovementService.ShouldPresentPendingCalamityAfterArrival(new BoardTileData { tileType = MapTileType.Event }));
        Assert.IsFalse(MapMovementService.ShouldPresentPendingCalamityAfterArrival(new BoardTileData { tileType = MapTileType.Merchant }));
        Assert.IsFalse(MapMovementService.ShouldPresentPendingCalamityAfterArrival(new BoardTileData { tileType = MapTileType.Forge }));
    }

    [TestCase(0, 5)]
    [TestCase(2, 3)]
    public void TryExecuteMove_ZeroOrNormalCostReturnsArrivalHandoffWithoutEmitting(int moveCost, int expectedPoints)
    {
        int callbackPoints = -1;
        controller.PlayerMovedToTile += (_, __) => callbackPoints = points.CurrentPoints;
        Assert.IsTrue(controller.BeginBattleTilePreview(target, true));

        bool moved = MapMovementService.TryExecuteMove(CreateRequest(moveCost), out MapMovementExecutionResult result);

        Assert.IsTrue(moved);
        Assert.IsTrue(result.moved);
        Assert.AreEqual(expectedPoints, points.CurrentPoints);
        Assert.IsTrue(result.shouldCompleteBattlePreviewArrival);
        Assert.AreEqual(-1, callbackPoints);
        Assert.IsTrue(controller.IsPreviewCommitPending);

        Assert.IsTrue(controller.CompletePendingBattlePreviewArrival());
        Assert.AreEqual(expectedPoints, callbackPoints);
        Assert.IsFalse(controller.IsPreviewCommitPending);
    }

    [Test]
    public void TryExecuteMove_OverflowReturnsHandoffThenArrivalObservesFinalActionPoints()
    {
        points.RestoreState(1, 0, 1);
        int exhaustedCount = 0;
        points.ActionPointsExhausted += () =>
        {
            exhaustedCount++;
            points.AdvanceCycle(0);
        };
        int callbackPoints = -1;
        int callbackExhaustions = -1;
        controller.PlayerMovedToTile += (_, __) =>
        {
            callbackPoints = points.CurrentPoints;
            callbackExhaustions = exhaustedCount;
        };
        Assert.IsTrue(controller.BeginBattleTilePreview(target, true));

        bool moved = MapMovementService.TryExecuteMove(CreateRequest(2), out MapMovementExecutionResult result);

        Assert.IsTrue(moved);
        Assert.IsTrue(result.moved);
        Assert.IsTrue(result.shouldRefreshHudWithEmptyStatus);
        Assert.IsTrue(result.shouldCompleteBattlePreviewArrival);
        Assert.AreEqual(2, exhaustedCount);
        Assert.AreEqual(1, points.CurrentPoints);
        Assert.AreEqual(-1, callbackPoints);
        Assert.AreEqual(-1, callbackExhaustions);
        Assert.IsTrue(controller.IsPreviewCommitPending);

        Assert.IsTrue(controller.CompletePendingBattlePreviewArrival());
        Assert.AreEqual(1, callbackPoints);
        Assert.AreEqual(2, callbackExhaustions);
        Assert.IsFalse(controller.IsPreviewCommitPending);
    }

    [Test]
    public void TryExecuteMove_OverflowSecondSpendFailureLeavesPawnAndCoordinateAtOrigin()
    {
        points.RestoreState(1, 0, 1);
        int callbackCount = 0;
        controller.PlayerMovedToTile += (_, __) => callbackCount++;
        Assert.IsTrue(controller.BeginBattleTilePreview(target, true));

        bool moved = MapMovementService.TryExecuteMove(CreateRequest(2), out MapMovementExecutionResult result);

        Assert.IsFalse(moved);
        Assert.IsFalse(result.moved);
        Assert.IsFalse(result.shouldCompleteBattlePreviewArrival);
        Assert.AreEqual(origin.coord, runState.playerGridPosition);
        Assert.AreEqual(World(origin), controller.PawnWorldPosition);
        Assert.AreEqual(0, points.CurrentPoints);
        Assert.AreEqual(0, callbackCount);
        Assert.IsFalse(controller.IsPreviewCommitPending);
        Assert.IsFalse(controller.IsBattlePreviewActive);
    }

    [Test]
    public void TryExecuteMove_IgnoreCostReturnsFeedbackBeforeArrivalHandoff()
    {
        Relic_SO relic = ScriptableObject.CreateInstance<Relic_SO>();
        createdObjects.Add(relic);
        relic.relicId = "ignore-preview-move";
        relic.relicName = relic.relicId;
        relic.builtInEffects = new List<Relic_SO.RelicBuiltInEffectEntry>
        {
            new Relic_SO.RelicBuiltInEffectEntry
            {
                trigger = Relic_SO.RelicTrigger.OnMoveCostAboutToSpend,
                effectType = Relic_SO.RelicBuiltInEffectType.ChanceToIgnoreMoveCost,
                chance = 1f
            }
        };
        runState.AddRelic(relic, 1);
        string publishedFeedback = null;
        string observedFeedback = null;
        controller.PlayerMovedToTile += (_, __) => observedFeedback = publishedFeedback;
        Assert.IsTrue(controller.BeginBattleTilePreview(target, true));

        bool moved = MapMovementService.TryExecuteMove(CreateRequest(2), out MapMovementExecutionResult result);

        Assert.IsTrue(moved);
        Assert.IsTrue(result.moved);
        Assert.AreEqual(5, points.CurrentPoints);
        Assert.IsTrue(result.shouldCompleteBattlePreviewArrival);
        Assert.IsNotEmpty(result.pendingRelicStatusMessage);
        Assert.IsNull(observedFeedback);
        Assert.IsTrue(controller.IsPreviewCommitPending);

        publishedFeedback = result.pendingRelicStatusMessage;
        Assert.IsTrue(controller.CompletePendingBattlePreviewArrival());
        Assert.AreEqual(publishedFeedback, observedFeedback);
        Assert.IsFalse(controller.IsPreviewCommitPending);
    }

    private MapMovementExecutionRequest CreateRequest(int moveCost)
    {
        return new MapMovementExecutionRequest
        {
            runState = runState,
            tile = target,
            moveCost = moveCost,
            actionPointSystem = points,
            playerMapController = controller,
            previousCycleFlags = new MapMovementCycleFlags(),
            applyCycleFlags = _ => { }
        };
    }

    private Vector3 World(BoardTileData tile)
    {
        return board.GridToWorld(tile.coord) + new Vector3(0f, 0f, -0.2f);
    }
}
