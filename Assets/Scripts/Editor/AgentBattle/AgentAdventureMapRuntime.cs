using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

// Map primitive owned by the adventure decision driver; no HTTP policy or duplicated map rules.
public sealed class AgentAdventureMapRuntime : IDisposable
{
    private Scene scene;
    private GameObject root;
    private PlayerMapController mover;
    private TileResolver resolver;
    private Random.State randomState;
    private bool disposed;
    private bool movingToUnvisited;
    public AgentAdventureConfiguration Configuration { get; }
    public MapRunSnapshot Snapshot { get; private set; }
    public BoardManager Board { get; private set; }
    public ActionPointSystem Points { get; private set; }
    public Queue<int> PendingCalamities { get; } = new Queue<int>();
    public List<BattleSettlementRelicChoiceData> StartingChoices { get; private set; }
    public MapMovementCycleFlags CycleFlags { get; private set; } = new MapMovementCycleFlags();
    public string LastMoveMessage { get; private set; }
    public string[] ConfigurationIssues => Snapshot.tiles.Where(t => t.IsHostileTile && (t.assignedEnemyGroup == null || !t.assignedEnemyGroup.IsValid))
        .Select(t => "No valid enemy group for " + t.tileType + " " + t.dangerBand + " at " + t.coord).ToArray();

    public AgentAdventureMapRuntime(AgentAdventureConfiguration configuration, int seed, AbilityRunBonus bonus = null, long startedUnixSeconds = 0)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode before opening an adventure.");
        if (SceneManager.GetSceneByPath("Assets/Scripts/Editor/AgentBattle/AgentAdventureSandbox.unity").isLoaded)
            throw new InvalidOperationException("An adventure already owns the map sandbox.");
        Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        if (configuration.starting?.difficulty == null || configuration.enemies == null)
            throw new ArgumentException("Adventure configuration requires difficulty and enemy roster.");
        Scene previous = SceneManager.GetActiveScene();
        Random.State outside = Random.state;
        try
        {
            Random.InitState(seed);
            scene = EditorSceneManager.OpenScene("Assets/Scripts/Editor/AgentBattle/AgentAdventureSandbox.unity", OpenSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            root = new GameObject("AgentAdventureMap");
            Board = root.AddComponent<BoardManager>(); Board.ConfigureHeadlessExecution();
            Points = root.AddComponent<ActionPointSystem>();
            mover = root.AddComponent<PlayerMapController>(); mover.ConfigureHeadlessExecution();
            resolver = root.AddComponent<TileResolver>(); resolver.ConfigureMoneyRewardTable(configuration.money);
            bonus = bonus ?? new AbilityRunBonus();
            RunState state = RunBootstrapService.CreateNormalRun(configuration.starting, bonus, startedUnixSeconds, out List<BattleSettlementRelicChoiceData> choices);
            StartingChoices = choices;
            Snapshot = new MapRunSnapshot { sceneName = "AgentAdventure_" + Guid.NewGuid().ToString("N"), runState = state };
            Points.Initialize(Mathf.Max(1, RunBootstrapService.ResolveActionPointLimitForTier(configuration.starting.difficulty, 0) + bonus.startingActionPointLimitFlat));
            Board.GenerateBoard(configuration.starting.difficulty, Random.Range(int.MinValue, int.MaxValue));
            foreach (BoardTileData tile in Board.GetAllTiles())
            {
                MapBattleEncounterService.EnsureTileAssignedEnemyGroup(configuration.enemies, tile);
                if (tile.IsHostileTile) tile.previewDangerRating = MapDangerRatingService.ResolveRating(tile.tileType, tile.dangerBand, tile.assignedEnemyGroup);
            }
            Snapshot.tiles = Board.GetAllTiles().ToList(); Snapshot.boardSize = Board.BoardSize;
            Snapshot.spawnCoord = Board.SpawnCoord; Snapshot.bossCoord = Board.BossCoord;
            mover.Initialize(root.transform, Board, Points, resolver, state);
            Board.TryGetTile(Board.SpawnCoord, out BoardTileData spawn);
            spawn.isVisited = true; mover.SetSpawn(spawn); Board.SetSelectedTile(spawn.coord);
            Points.ActionPointsExhausted += OnExhausted;
            resolver.KeyCollected += OnKey;
            mover.PlayerMovedToTile += OnArrival;
            SynchronizeSnapshot(); randomState = Random.state;
        }
        catch { Dispose(); throw; }
        finally
        {
            Random.state = outside;
            if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
        }
    }

    public IEnumerable<BoardTileData> LegalDestinations()
    {
        if (disposed) throw new ObjectDisposedException(nameof(AgentAdventureMapRuntime));
        return Board.GetAllTiles().Where(t => Board.IsAdjacent(Snapshot.runState.playerGridPosition, t.coord)
            && MapMovementService.CanAttemptMove(Snapshot.runState, Points, t, MapMovementService.GetMoveCost(Snapshot.runState, t))).ToArray();
    }

    public bool TryMove(Vector2Int coord)
    {
        if (disposed) throw new ObjectDisposedException(nameof(AgentAdventureMapRuntime));
        BoardTileData tile = LegalDestinations().FirstOrDefault(t => t.coord == coord);
        if (tile == null) return false;
        Random.State outside = Random.state;
        try
        {
            Random.state = randomState;
            movingToUnvisited = !tile.isVisited;
            bool moved = MapMovementService.TryExecuteMove(new MapMovementExecutionRequest
            {
                runState = Snapshot.runState, tile = tile, moveCost = MapMovementService.GetMoveCost(Snapshot.runState, tile),
                actionPointSystem = Points, playerMapController = mover, previousCycleFlags = CycleFlags.Clone(), applyCycleFlags = flags => CycleFlags = flags
            }, out MapMovementExecutionResult result);
            if (!string.IsNullOrEmpty(result.pendingRelicStatusMessage)) LastMoveMessage = result.pendingRelicStatusMessage;
            SynchronizeSnapshot(); return moved;
        }
        finally { randomState = Random.state; Random.state = outside; }
    }

    private void OnArrival(BoardTileData tile, string message)
    {
        if (movingToUnvisited) MapMovementService.RecordUnexploredTileVisit(Snapshot.runState);
        movingToUnvisited = false;
        Board.SetSelectedTile(tile.coord); LastMoveMessage = message;
    }

    public T WithRandom<T>(Func<T> action)
    {
        if (disposed) throw new ObjectDisposedException(nameof(AgentAdventureMapRuntime));
        Random.State outside = Random.state;
        try { Random.state = randomState; return action(); }
        finally { randomState = Random.state; Random.state = outside; SynchronizeSnapshot(); }
    }
    private void OnExhausted() { MapMovementService.AdvanceExhaustedCycle(Snapshot.runState, Points, Configuration.starting.difficulty, PendingCalamities); }
    private void OnKey(BoardTileData tile) { MapMovementService.ApplyKeyCollected(Snapshot.runState, null, tile, Points); }

    public void SynchronizeSnapshot()
    {
        Snapshot.actionPointBase = Points.BasePoints; Snapshot.actionPointBonus = Points.BonusPoints; Snapshot.actionPointCurrent = Points.CurrentPoints;
        Snapshot.pendingCalamityTiers = PendingCalamities.ToList(); Snapshot.selectedCoord = Board.SelectedCoord;
        Snapshot.suppressOnePendingCalamityOnActionPointRestore = CycleFlags.suppressPendingCalamityOnNextRestActionPointRestore;
    }

    public void Dispose()
    {
        if (disposed) return; disposed = true;
        if (Points != null) Points.ActionPointsExhausted -= OnExhausted;
        if (resolver != null) resolver.KeyCollected -= OnKey;
        if (mover != null) mover.PlayerMovedToTile -= OnArrival;
        if (root != null) UnityEngine.Object.DestroyImmediate(root);
        if (scene.IsValid() && scene.isLoaded) EditorSceneManager.CloseScene(scene, true);
    }
}
