using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class MapBattleEncounterBuildRequest
{
    public RunState runState;
    public BoardTileData tile;
    public EnemyGroup_SO encounterGroup;
    public MoneyRewardTable_SO moneyRewardTable;
    public RelicTable_SO eliteRelicChoiceTable;
    public string sourceSceneName;
    public string battleSceneName;
    public string tutorialCompletionSceneName;
    public TutorialBattleStageId tutorialBattleStageId;
    public TutorialBattleDefinition_SO tutorialBattleDefinition;
    public List<PendingBattleStatusGrant> pendingBattleStartAllyStatusGrants;
    public bool isTutorialEncounter;
    public bool returnToMainMenuOnVictory;
    public bool restartBattleOnDefeat = true;
}

public static class MapBattleEncounterService
{
    public static EnemyGroup_SO SelectEnemyGroupForTile(
        EnemyRoster_SO enemyRoster,
        BoardTileData tile)
    {
        if (tile == null || enemyRoster == null)
            return null;

        System.Random rng = new System.Random(tile.encounterSeed);
        switch (tile.tileType)
        {
            case MapTileType.Elite:
                return enemyRoster.GetRandomGroup(tile.dangerBand, EnemyPoolKind.Elite, rng);
            case MapTileType.Boss:
                return enemyRoster.GetRandomBossGroup(rng);
            case MapTileType.Battle:
            default:
                return enemyRoster.GetRandomGroup(tile.dangerBand, EnemyPoolKind.Standard, rng);
        }
    }

    public static void EnsureTileAssignedEnemyGroup(
        EnemyRoster_SO enemyRoster,
        BoardTileData tile)
    {
        if (tile == null || !tile.IsHostileTile)
            return;
        if (tile.assignedEnemyGroup != null && tile.assignedEnemyGroup.IsValid)
            return;

        tile.assignedEnemyGroup = SelectEnemyGroupForTile(enemyRoster, tile);
    }

    public static BattleEncounterData BuildEncounter(MapBattleEncounterBuildRequest request)
    {
        if (request == null || request.tile == null || request.runState == null || request.encounterGroup == null)
            return null;

        BoardTileData tile = request.tile;
        BattleEncounterData encounterData = new BattleEncounterData
        {
            sourceSceneName = string.IsNullOrWhiteSpace(request.sourceSceneName)
                ? SceneManager.GetActiveScene().name
                : request.sourceSceneName,
            targetSceneName = request.battleSceneName,
            tutorialCompletionSceneName = request.tutorialCompletionSceneName,
            encounterLabel = $"{tile.tileType} [{tile.coord.x},{tile.coord.y}]",
            tutorialBattleStageId = request.tutorialBattleStageId,
            tileType = tile.tileType,
            dangerBand = tile.dangerBand,
            sourceCoord = tile.coord,
            enemyGroup = request.encounterGroup,
            eliteRelicChoiceTable = request.eliteRelicChoiceTable,
            tutorialBattleDefinition = request.tutorialBattleDefinition,
            partyMembers = new List<RunPartyMemberState>(request.runState.GetLivingPartyMembers()),
            relicInventory = new List<RunRelicState>(),
            activeCalamities = new List<RunCalamityState>(),
            pendingBattleStartAllyStatusGrants = request.pendingBattleStartAllyStatusGrants ?? new List<PendingBattleStatusGrant>(),
            seed = tile.encounterSeed,
            moneyGained = request.moneyRewardTable != null
                ? request.moneyRewardTable.RollReward(tile.tileType, tile.dangerBand, tile.encounterSeed)
                : 0,
            isTutorialEncounter = request.isTutorialEncounter,
            returnToMainMenuOnVictory = request.returnToMainMenuOnVictory,
            restartBattleOnDefeat = request.restartBattleOnDefeat
        };

        AppendRelicInventory(encounterData, request.runState);
        AppendActiveCalamities(encounterData, request.runState);
        return encounterData;
    }

    private static void AppendRelicInventory(BattleEncounterData encounterData, RunState runState)
    {
        if (encounterData == null || runState == null || runState.relicInventory == null)
            return;

        for (int i = 0; i < runState.relicInventory.Count; i++)
        {
            RunRelicState relicState = runState.relicInventory[i];
            if (relicState != null)
                encounterData.relicInventory.Add(relicState.Clone());
        }
    }

    private static void AppendActiveCalamities(BattleEncounterData encounterData, RunState runState)
    {
        if (encounterData == null || runState == null || runState.activeCalamities == null)
            return;

        for (int i = 0; i < runState.activeCalamities.Count; i++)
        {
            RunCalamityState calamityState = runState.activeCalamities[i];
            if (calamityState != null)
                encounterData.activeCalamities.Add(calamityState.Clone());
        }
    }
}
