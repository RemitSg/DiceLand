using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class MapRunSnapshot
{
    public string sceneName;
    public RunState runState;
    public List<BoardTileData> tiles = new List<BoardTileData>();
    public List<int> pendingCalamityTiers = new List<int>();
    public bool suppressOnePendingCalamityOnActionPointRestore;
    public int boardSize;
    public Vector2Int spawnCoord;
    public Vector2Int bossCoord;
    public Vector2Int selectedCoord;
    public int actionPointBase;
    public int actionPointBonus;
    public int actionPointCurrent;

    public MapRunSnapshot Clone()
    {
        MapRunSnapshot clone = new MapRunSnapshot
        {
            sceneName = sceneName,
            runState = runState != null ? runState.Clone() : null,
            boardSize = boardSize,
            spawnCoord = spawnCoord,
            bossCoord = bossCoord,
            selectedCoord = selectedCoord,
            actionPointBase = actionPointBase,
            actionPointBonus = actionPointBonus,
            actionPointCurrent = actionPointCurrent,
            suppressOnePendingCalamityOnActionPointRestore = suppressOnePendingCalamityOnActionPointRestore
        };

        if (tiles != null)
        {
            for (int i = 0; i < tiles.Count; i++)
            {
                BoardTileData tile = tiles[i];
                if (tile != null)
                    clone.tiles.Add(tile.Clone());
            }
        }

        if (pendingCalamityTiers != null)
            clone.pendingCalamityTiers = new List<int>(pendingCalamityTiers);

        return clone;
    }
}

public static class MapRunRuntime
{
    private static MapRunSnapshot currentSnapshot;

    public static bool TryGetSnapshot(string sceneName, out MapRunSnapshot snapshot)
    {
        if (currentSnapshot != null &&
            !string.IsNullOrWhiteSpace(sceneName) &&
            string.Equals(currentSnapshot.sceneName, sceneName, StringComparison.OrdinalIgnoreCase))
        {
            snapshot = currentSnapshot.Clone();
            return true;
        }

        snapshot = null;
        return false;
    }

    public static void SaveSnapshot(string sceneName, RunState runState, BoardManager boardManager, ActionPointSystem actionPointSystem, IEnumerable<int> pendingCalamityTiers = null, bool suppressOnePendingCalamityOnActionPointRestore = false)
    {
        if (runState == null || boardManager == null || actionPointSystem == null || string.IsNullOrWhiteSpace(sceneName))
            return;

        MapRunSnapshot snapshot = new MapRunSnapshot
        {
            sceneName = sceneName,
            runState = runState.Clone(),
            boardSize = boardManager.BoardSize,
            spawnCoord = boardManager.SpawnCoord,
            bossCoord = boardManager.BossCoord,
            selectedCoord = boardManager.SelectedCoord,
            actionPointBase = actionPointSystem.BasePoints,
            actionPointBonus = actionPointSystem.BonusPoints,
            actionPointCurrent = actionPointSystem.CurrentPoints,
            suppressOnePendingCalamityOnActionPointRestore = suppressOnePendingCalamityOnActionPointRestore
        };

        foreach (BoardTileData tile in boardManager.GetAllTiles())
        {
            if (tile != null)
                snapshot.tiles.Add(tile.Clone());
        }

        if (pendingCalamityTiers != null)
            snapshot.pendingCalamityTiers.AddRange(pendingCalamityTiers);

        currentSnapshot = snapshot;
    }

    public static void ApplyEncounterResult(BattleEncounterData encounter)
    {
        if (currentSnapshot == null || encounter == null)
            return;

        if (encounter.resolvedPartyMembers != null && encounter.resolvedPartyMembers.Count > 0)
        {
            currentSnapshot.runState.partyMembers = new List<RunPartyMemberState>(encounter.resolvedPartyMembers.Count);
            for (int i = 0; i < encounter.resolvedPartyMembers.Count; i++)
            {
                RunPartyMemberState member = encounter.resolvedPartyMembers[i];
                if (member != null)
                    currentSnapshot.runState.partyMembers.Add(member.Clone());
            }

            currentSnapshot.runState.SyncLegacyPlayerFieldsFromParty();
        }

        if (encounter.wasVictory && encounter.resolvedLoot != null && encounter.resolvedLoot.Count > 0)
        {
            for (int i = 0; i < encounter.resolvedLoot.Count; i++)
            {
                RunLootState loot = encounter.resolvedLoot[i];
                if (loot == null || loot.lootItem == null || loot.amount <= 0)
                    continue;

                currentSnapshot.runState.AddLoot(loot.lootItem, loot.amount);
            }
        }

        if (encounter.wasVictory && encounter.resolvedRelics != null && encounter.resolvedRelics.Count > 0)
        {
            for (int i = 0; i < encounter.resolvedRelics.Count; i++)
            {
                RunRelicState relic = encounter.resolvedRelics[i];
                if (relic == null || relic.relicData == null || relic.amount <= 0)
                    continue;

                currentSnapshot.runState.AddRelic(relic.relicData, relic.amount);
            }
        }

        if (encounter.wasVictory && encounter.pendingVictoryLootRewards != null && encounter.pendingVictoryLootRewards.Count > 0)
        {
            for (int i = 0; i < encounter.pendingVictoryLootRewards.Count; i++)
            {
                RunLootState loot = encounter.pendingVictoryLootRewards[i];
                if (loot == null || loot.lootItem == null || loot.amount <= 0)
                    continue;

                currentSnapshot.runState.AddLoot(loot.lootItem, loot.amount);
            }
        }

        if (encounter.wasVictory && encounter.pendingVictoryRelicRewards != null && encounter.pendingVictoryRelicRewards.Count > 0)
        {
            for (int i = 0; i < encounter.pendingVictoryRelicRewards.Count; i++)
            {
                RunRelicState relic = encounter.pendingVictoryRelicRewards[i];
                if (relic == null || relic.relicData == null || relic.amount <= 0)
                    continue;

                currentSnapshot.runState.AddRelic(relic.relicData, relic.amount);
            }
        }

        if (encounter.wasVictory && encounter.pendingVictoryBlueprintRewards != null && encounter.pendingVictoryBlueprintRewards.Count > 0)
        {
            for (int i = 0; i < encounter.pendingVictoryBlueprintRewards.Count; i++)
            {
                RunBlueprintState blueprint = encounter.pendingVictoryBlueprintRewards[i];
                if (blueprint == null || blueprint.blueprintData == null || blueprint.amount <= 0)
                    continue;

                currentSnapshot.runState.AddBlueprint(blueprint.blueprintData, blueprint.amount);
            }
        }

        if (encounter.wasVictory && encounter.moneyGained > 0)
            currentSnapshot.runState.AddMoney(encounter.moneyGained);

        for (int i = 0; i < currentSnapshot.tiles.Count; i++)
        {
            BoardTileData tile = currentSnapshot.tiles[i];
            if (tile == null || tile.coord != encounter.sourceCoord)
                continue;

            tile.isVisited = true;
            if (encounter.wasVictory)
            {
                tile.isResolved = true;
                if (tile.tileType == MapTileType.Battle || tile.tileType == MapTileType.Elite)
                    tile.tileType = MapTileType.Empty;
            }

            break;
        }
    }

    public static void ApplyRestResult(string sceneName, Vector2Int sourceCoord)
    {
        if (currentSnapshot == null || string.IsNullOrWhiteSpace(sceneName))
            return;

        if (!string.Equals(currentSnapshot.sceneName, sceneName, StringComparison.OrdinalIgnoreCase))
            return;

        if (currentSnapshot.runState?.partyMembers != null)
        {
            for (int i = 0; i < currentSnapshot.runState.partyMembers.Count; i++)
            {
                RunPartyMemberState member = currentSnapshot.runState.partyMembers[i];
                if (member == null)
                    continue;

                member.currentHP = Mathf.Clamp(member.maxHP, 0, Mathf.Max(0, member.maxHP));
            }

            currentSnapshot.runState.SyncLegacyPlayerFieldsFromParty();
        }

        for (int i = 0; i < currentSnapshot.tiles.Count; i++)
        {
            BoardTileData tile = currentSnapshot.tiles[i];
            if (tile == null || tile.coord != sourceCoord)
                continue;

            tile.isVisited = true;
            tile.isResolved = true;
            break;
        }
    }

    public static void ApplyRestoreActionPointsResult(string sceneName, Vector2Int sourceCoord)
    {
        if (currentSnapshot == null || string.IsNullOrWhiteSpace(sceneName))
            return;

        if (!string.Equals(currentSnapshot.sceneName, sceneName, StringComparison.OrdinalIgnoreCase))
            return;

        currentSnapshot.actionPointCurrent = Mathf.Max(0, currentSnapshot.actionPointBase + currentSnapshot.actionPointBonus);
        if (currentSnapshot.suppressOnePendingCalamityOnActionPointRestore && currentSnapshot.pendingCalamityTiers.Count > 0)
            currentSnapshot.pendingCalamityTiers.RemoveAt(0);
        currentSnapshot.suppressOnePendingCalamityOnActionPointRestore = false;

        for (int i = 0; i < currentSnapshot.tiles.Count; i++)
        {
            BoardTileData tile = currentSnapshot.tiles[i];
            if (tile == null || tile.coord != sourceCoord)
                continue;

            tile.isVisited = true;
            tile.isResolved = true;
            break;
        }
    }

    public static bool TryApplyRecruitResult(string sceneName, Vector2Int sourceCoord, CharacterData_SO characterData, int recruitCost, int maxRecruitCount, out string errorMessage)
    {
        if (currentSnapshot == null || string.IsNullOrWhiteSpace(sceneName))
        {
            errorMessage = "\u672a\u627e\u5230\u5730\u56fe\u8fdb\u5ea6\u5feb\u7167\u3002";
            return false;
        }

        if (!string.Equals(currentSnapshot.sceneName, sceneName, StringComparison.OrdinalIgnoreCase))
        {
            errorMessage = "\u5f53\u524d\u5730\u56fe\u8fdb\u5ea6\u4e0e\u4f11\u606f\u573a\u666f\u4e0d\u5339\u914d\u3002";
            return false;
        }

        if (currentSnapshot.runState == null)
        {
            errorMessage = "\u8fd0\u884c\u4e2d\u961f\u4f0d\u6570\u636e\u4e22\u5931\u3002";
            return false;
        }

        if (characterData == null)
        {
            errorMessage = "\u672a\u9009\u62e9\u53ef\u62db\u52df\u89d2\u8272\u3002";
            return false;
        }

        if (maxRecruitCount > 0 && currentSnapshot.runState.recruitedMemberCount >= maxRecruitCount)
        {
            errorMessage = "\u62db\u52df\u6b21\u6570\u5df2\u8fbe\u4e0a\u9650\uff0c\u65e0\u6cd5\u7ee7\u7eed\u62db\u52df\u3002";
            return false;
        }

        if (currentSnapshot.runState.HasCharacter(characterData))
        {
            errorMessage = "\u8be5\u89d2\u8272\u5df2\u5728\u961f\u4f0d\u4e2d\u3002";
            return false;
        }

        if (!currentSnapshot.runState.TrySpendMoney(Mathf.Max(0, recruitCost)))
        {
            errorMessage = $"\u91d1\u94b1\u4e0d\u8db3\uff0c\u9700\u8981 {Mathf.Max(0, recruitCost)} \u91d1\u5e01\u3002";
            return false;
        }

        int baseHp = Mathf.Max(1, characterData.baseHP);
        Weapon_SO initialWeapon = characterData.initialWeaponData;
        currentSnapshot.runState.AddPartyMember(new RunPartyMemberState
        {
            memberId = string.IsNullOrWhiteSpace(characterData.characterName) ? characterData.name : characterData.characterName,
            characterData = characterData,
            weaponData = initialWeapon,
            level = 1,
            experience = 0,
            maxHP = baseHp,
            currentHP = baseHp
        });

        if (initialWeapon != null)
            currentSnapshot.runState.AddWeapon(initialWeapon, 1);

        currentSnapshot.runState.recruitedMemberCount = Mathf.Max(0, currentSnapshot.runState.recruitedMemberCount + 1);

        for (int i = 0; i < currentSnapshot.tiles.Count; i++)
        {
            BoardTileData tile = currentSnapshot.tiles[i];
            if (tile == null || tile.coord != sourceCoord)
                continue;

            tile.isVisited = true;
            tile.isResolved = true;
            break;
        }

        errorMessage = string.Empty;
        return true;
    }

    public static void Clear(string sceneName = null)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            currentSnapshot = null;
            return;
        }

        if (currentSnapshot != null &&
            string.Equals(currentSnapshot.sceneName, sceneName, StringComparison.OrdinalIgnoreCase))
        {
            currentSnapshot = null;
        }
    }
}
