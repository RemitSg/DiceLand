using System.Collections.Generic;
using UnityEngine;

public static class MapEventEncounterService
{
    public static bool TryPrepareEncounter(
        RunState runState,
        BoardTileData currentEventTile,
        MapEventBuiltInEffectData effectData,
        string sourceSceneName,
        string battleSceneName,
        int seedSalt,
        out BattleEncounterData encounterData,
        out string message)
    {
        encounterData = null;
        if (runState == null)
        {
            message = "当前未找到运行时队伍数据。";
            return false;
        }

        if (!TryBuildEncounterGroup(effectData, out EnemyGroup_SO encounterGroup, out message))
            return false;

        int encounterSeed = unchecked(seedSalt ^ runState.currentCycle * 397);
        if (currentEventTile != null)
            encounterSeed ^= currentEventTile.encounterSeed;

        encounterData = new BattleEncounterData
        {
            sourceSceneName = sourceSceneName,
            targetSceneName = battleSceneName,
            encounterLabel = $"事件遭遇：{encounterGroup.groupName}",
            tileType = MapTileType.Event,
            dangerBand = currentEventTile != null ? currentEventTile.dangerBand : MapDangerBand.Outer,
            sourceCoord = currentEventTile != null ? currentEventTile.coord : runState.playerGridPosition,
            enemyGroup = encounterGroup,
            partyMembers = new List<RunPartyMemberState>(runState.GetLivingPartyMembers()),
            seed = encounterSeed,
            moneyGained = 0
        };

        AppendRelicInventory(encounterData, runState);
        AppendActiveCalamities(encounterData, runState);

        message = $"将触发遭遇战：{encounterGroup.groupName}";
        return true;
    }

    public static bool TryBuildEncounterGroup(
        MapEventBuiltInEffectData effectData,
        out EnemyGroup_SO encounterGroup,
        out string message)
    {
        encounterGroup = null;
        if (effectData == null)
        {
            message = "该事件未配置有效的遭遇敌人。";
            return false;
        }

        switch (effectData.encounterSelectionMode)
        {
            case MapEventEncounterSelectionMode.RandomEnemyGroup:
                encounterGroup = BuildEncounterGroupFromEventGroups(effectData);
                break;
            case MapEventEncounterSelectionMode.RandomEnemies:
                encounterGroup = BuildEncounterGroupFromEventEnemies(effectData);
                break;
            default:
                message = "未知事件遭遇配置。";
                return false;
        }

        if (encounterGroup == null || !encounterGroup.IsValid)
        {
            message = "该事件未配置有效的遭遇敌人。";
            return false;
        }

        message = string.Empty;
        return true;
    }

    private static EnemyGroup_SO BuildEncounterGroupFromEventGroups(MapEventBuiltInEffectData effectData)
    {
        if (effectData == null || effectData.encounterEnemyGroups == null || effectData.encounterEnemyGroups.Count == 0)
            return null;

        List<EnemyGroup_SO> candidates = new List<EnemyGroup_SO>();
        for (int i = 0; i < effectData.encounterEnemyGroups.Count; i++)
        {
            EnemyGroup_SO group = effectData.encounterEnemyGroups[i];
            if (group != null && group.IsValid)
                candidates.Add(group);
        }

        if (candidates.Count == 0)
            return null;

        int index = UnityEngine.Random.Range(0, candidates.Count);
        return candidates[index];
    }

    private static EnemyGroup_SO BuildEncounterGroupFromEventEnemies(MapEventBuiltInEffectData effectData)
    {
        if (effectData == null || effectData.encounterEnemies == null || effectData.encounterEnemies.Count == 0)
            return null;

        List<EnemyData_SO> candidates = new List<EnemyData_SO>();
        for (int i = 0; i < effectData.encounterEnemies.Count; i++)
        {
            EnemyData_SO enemy = effectData.encounterEnemies[i];
            if (enemy != null)
                candidates.Add(enemy);
        }

        if (candidates.Count == 0)
            return null;

        int minCount = Mathf.Max(1, effectData.encounterEnemyCountMin);
        int maxCount = Mathf.Max(minCount, effectData.encounterEnemyCountMax);
        int pickCount = Mathf.Min(UnityEngine.Random.Range(minCount, maxCount + 1), candidates.Count);
        if (pickCount <= 0)
            return null;

        EnemyGroup_SO runtimeGroup = ScriptableObject.CreateInstance<EnemyGroup_SO>();
        runtimeGroup.groupName = "事件遭遇";
        runtimeGroup.description = "事件触发的随机遭遇。";
        runtimeGroup.enemies = new List<EnemyData_SO>();

        for (int i = 0; i < pickCount; i++)
        {
            int selectedIndex = UnityEngine.Random.Range(0, candidates.Count);
            runtimeGroup.enemies.Add(candidates[selectedIndex]);
            candidates.RemoveAt(selectedIndex);
        }

        return runtimeGroup;
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
