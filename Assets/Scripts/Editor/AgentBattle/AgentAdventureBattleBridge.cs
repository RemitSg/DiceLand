using System;
using System.Linq;

// An adventure owns its snapshot. The global UI snapshot is never replaced by agent play.
public sealed class AgentAdventureBattleBridge : IDisposable
{
    public AgentBattleSession Battle { get; private set; }
    public MapRunSnapshot Snapshot { get; }

    public AgentAdventureBattleBridge(MapRunSnapshot snapshot, BoardTileData tile, EnemyGroup_SO group,
        DifficultyConfig_SO difficulty, MoneyRewardTable_SO money, RelicTable_SO eliteRelics, LevelTable_SO levels, int maxRounds = 1000)
    {
        if (snapshot?.runState == null || tile == null || group == null || !group.IsValid)
            throw new ArgumentException("A live adventure, tile and enemy group are required.");
        if (tile.tileType == MapTileType.Boss && !snapshot.runState.IsBossUnlocked)
            throw new ArgumentException("Boss is locked until the required keys are collected.");
        if (snapshot.tiles == null || !snapshot.tiles.Contains(tile))
            throw new ArgumentException("The battle tile does not belong to this adventure.");
        if (!tile.IsHostileTile || tile.isResolved || !snapshot.runState.GetLivingPartyMembers().Any())
            throw new ArgumentException("An unresolved hostile tile and a living party are required.");
        Snapshot = snapshot;
        RunState encounterState = snapshot.runState.Clone();
        BattleEncounterData encounter = MapBattleEncounterService.BuildEncounter(new MapBattleEncounterBuildRequest
        {
            runState = encounterState, tile = tile, encounterGroup = group, difficultyConfig = difficulty,
            moneyRewardTable = money, eliteRelicChoiceTable = eliteRelics, sourceSceneName = snapshot.sceneName,
            battleSceneName = "AgentBattleSandbox", pendingBattleStartAllyStatusGrants = encounterState.ConsumePendingNextBattleAllyStatusGrants(),
            restartBattleOnDefeat = false
        });
        Battle = new AgentBattleSession(new AgentBattleRequest { op = "create", seed = tile.encounterSeed, maxRounds = maxRounds }, encounter, levels);
        snapshot.runState.ConsumePendingNextBattleAllyStatusGrants();
    }

    public void ApplyResult() { Battle.ApplyAdventureResult(Snapshot); }
    public void Dispose() { Battle?.Dispose(); Battle = null; }
}
