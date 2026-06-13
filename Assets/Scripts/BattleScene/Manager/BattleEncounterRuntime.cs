using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class BattleEncounterResolutionOverride
{
    public bool forceVictory;
    public string subtitleOverride;
    public List<LootTable_SO> lootTables = new List<LootTable_SO>();
}

[Serializable]
public sealed class BattleEncounterData
{
    public string sourceSceneName;
    public string targetSceneName;
    public string encounterLabel;
    public string tutorialCompletionSceneName;
    public TutorialBattleStageId tutorialBattleStageId;
    public MapTileType tileType;
    public MapDangerBand dangerBand;
    public Vector2Int sourceCoord;
    public EnemyGroup_SO enemyGroup;
    public RelicTable_SO eliteRelicChoiceTable;
    public TutorialBattleDefinition_SO tutorialBattleDefinition;
    public List<RunPartyMemberState> partyMembers = new List<RunPartyMemberState>();
    public List<RunPartyMemberState> resolvedPartyMembers = new List<RunPartyMemberState>();
    public List<RunLootState> resolvedLoot = new List<RunLootState>();
    public List<RunRelicState> resolvedRelics = new List<RunRelicState>();
    public List<RunLootState> pendingVictoryLootRewards = new List<RunLootState>();
    public List<RunRelicState> pendingVictoryRelicRewards = new List<RunRelicState>();
    public List<RunBlueprintState> pendingVictoryBlueprintRewards = new List<RunBlueprintState>();
    public List<RunWeaponState> pendingVictoryWeaponRewards = new List<RunWeaponState>();
    public List<RunRelicState> relicInventory = new List<RunRelicState>();
    public List<RunCalamityState> activeCalamities = new List<RunCalamityState>();
    public List<PendingBattleStatusGrant> pendingBattleStartAllyStatusGrants = new List<PendingBattleStatusGrant>();
    public BattleEncounterResolutionOverride resolutionOverride;
    public int seed;
    public bool wasVictory;
    public int experienceGained;
    public int configuredExperienceReward = -1;
    public int moneyGained;
    public bool isTutorialEncounter;
    public bool isSpecialDungeonEncounter;
    public bool isEventEncounter;
    public bool returnToMainMenuOnVictory;
    public bool restartBattleOnDefeat = true;
    public bool returnToMapOnDefeat;
    [NonSerialized] public bool battleStartAllyStatusGrantsApplied;
    [NonSerialized] private HashSet<string> roundRelicProcKeys;
    [NonSerialized] private HashSet<string> battleRelicProcKeys;

    public IReadOnlyList<EnemyData_SO> Enemies =>
        enemyGroup != null ? enemyGroup.GetEnemies() : Array.Empty<EnemyData_SO>();

    public IReadOnlyList<RunPartyMemberState> Allies => partyMembers ?? (IReadOnlyList<RunPartyMemberState>)Array.Empty<RunPartyMemberState>();

    public bool HasEnemies => enemyGroup != null && enemyGroup.IsValid;
    public bool HasAllies => partyMembers != null && partyMembers.Count > 0;

    public void ResetRoundRelicProcKeys()
    {
        if (roundRelicProcKeys == null)
            roundRelicProcKeys = new HashSet<string>(StringComparer.Ordinal);
        else
            roundRelicProcKeys.Clear();
    }

    public bool TryRegisterRoundRelicProc(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return true;

        if (roundRelicProcKeys == null)
            roundRelicProcKeys = new HashSet<string>(StringComparer.Ordinal);

        return roundRelicProcKeys.Add(key);
    }

    public bool TryRegisterBattleRelicProc(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return true;

        if (battleRelicProcKeys == null)
            battleRelicProcKeys = new HashSet<string>(StringComparer.Ordinal);

        return battleRelicProcKeys.Add(key);
    }
}

public static class BattleEncounterRuntime
{
    public static BattleEncounterData CurrentEncounter { get; private set; }
    public static BattleEncounterData ActiveEncounter { get; private set; }

    public static bool HasEncounter => CurrentEncounter != null && CurrentEncounter.HasEnemies;

    public static void SetEncounter(BattleEncounterData encounterData)
    {
        CurrentEncounter = encounterData;
    }

    public static BattleEncounterData ConsumeEncounter()
    {
        BattleEncounterData encounterData = CurrentEncounter;
        ActiveEncounter = encounterData;
        CurrentEncounter = null;
        return encounterData;
    }

    public static void CompleteActiveEncounter()
    {
        ActiveEncounter = null;
    }

    public static void RequeueActiveEncounter()
    {
        if (ActiveEncounter != null)
            CurrentEncounter = ActiveEncounter;
    }

    public static void Clear()
    {
        CurrentEncounter = null;
        ActiveEncounter = null;
    }
}
