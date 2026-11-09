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
    [NonSerialized] public SpecialDungeonRuntimeContext specialDungeonContext;

    public MapRunSnapshot Clone()
    {
        return Clone(includeSpecialDungeonContext: true);
    }

    public MapRunSnapshot CloneWithoutSpecialDungeonContext()
    {
        return Clone(includeSpecialDungeonContext: false);
    }

    private MapRunSnapshot Clone(bool includeSpecialDungeonContext)
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
            suppressOnePendingCalamityOnActionPointRestore = suppressOnePendingCalamityOnActionPointRestore,
            specialDungeonContext = includeSpecialDungeonContext && specialDungeonContext != null
                ? specialDungeonContext.Clone()
                : null
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

    // Inventory may equip weapons during a rest visit. Bind that UI to the owned run rather
    // than a TryGetSnapshot clone, so equipment survives the return to the map.
    internal static RunState GetRunStateForInventory(string sceneName)
    {
        return currentSnapshot != null && string.Equals(currentSnapshot.sceneName, sceneName, StringComparison.OrdinalIgnoreCase)
            ? currentSnapshot.runState : null;
    }

    internal static RunState GetRunStateForAcquisitionDisplay(string sceneName)
    {
        return currentSnapshot != null && string.Equals(currentSnapshot.sceneName, sceneName, StringComparison.OrdinalIgnoreCase)
            ? currentSnapshot.runState : null;
    }

    public static bool TryGetMoney(string sceneName, out int money)
    {
        bool available = currentSnapshot?.runState != null && !string.IsNullOrWhiteSpace(sceneName) &&
            string.Equals(currentSnapshot.sceneName, sceneName, StringComparison.OrdinalIgnoreCase);
        money = available ? currentSnapshot.runState.money : 0;
        return available;
    }

    public static bool TryGetSnapshot(string sceneName, out MapRunSnapshot snapshot)
    {
        if (currentSnapshot != null &&
            !string.IsNullOrWhiteSpace(sceneName) &&
            string.Equals(currentSnapshot.sceneName, sceneName, StringComparison.OrdinalIgnoreCase))
        {
            snapshot = currentSnapshot.Clone();
            RelicSystem.RestoreDiagonalMapMovement(snapshot.runState);
            return true;
        }

        snapshot = null;
        return false;
    }

    public static void SaveSnapshot(
        string sceneName,
        RunState runState,
        BoardManager boardManager,
        ActionPointSystem actionPointSystem,
        IEnumerable<int> pendingCalamityTiers = null,
        bool suppressOnePendingCalamityOnActionPointRestore = false,
        SpecialDungeonRuntimeContext specialDungeonContext = null)
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
            suppressOnePendingCalamityOnActionPointRestore = suppressOnePendingCalamityOnActionPointRestore,
            specialDungeonContext = specialDungeonContext != null ? specialDungeonContext.Clone() : null
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

    public static void RestoreSnapshot(MapRunSnapshot snapshot)
    {
        currentSnapshot = snapshot != null ? snapshot.Clone() : null;
    }

    public static void ApplyEncounterResult(BattleEncounterData encounter)
    {
        ApplyEncounterResult(currentSnapshot, encounter);
    }

    private static List<RunPartyMemberState> MergeResolvedPartyMembers(
        List<RunPartyMemberState> originalParty, List<RunPartyMemberState> resolvedParty)
    {
        List<RunPartyMemberState> merged = new List<RunPartyMemberState>();
        bool[] consumed = new bool[resolvedParty.Count];
        // Only living members enter battle. Settlement is a partial update to
        // the roster, so absent members (including the dead leader) keep order.
        if (originalParty != null)
        {
            foreach (RunPartyMemberState original in originalParty)
            {
                if (original == null || original.isTemporaryBattleGuest)
                    continue;

                RunPartyMemberState result = original;
                for (int i = 0; i < resolvedParty.Count; i++)
                {
                    RunPartyMemberState candidate = resolvedParty[i];
                    if (consumed[i] || candidate == null || candidate.isTemporaryBattleGuest)
                        continue;

                    bool sameMember = !string.IsNullOrWhiteSpace(original.memberId)
                        ? string.Equals(original.memberId, candidate.memberId, System.StringComparison.Ordinal)
                        : original.characterData == candidate.characterData && original.weaponData == candidate.weaponData;
                    if (!sameMember)
                        continue;

                    result = candidate;
                    consumed[i] = true;
                    break;
                }

                merged.Add(result.Clone());
            }
        }

        for (int i = 0; i < resolvedParty.Count; i++)
        {
            RunPartyMemberState member = resolvedParty[i];
            if (!consumed[i] && member != null && !member.isTemporaryBattleGuest)
                merged.Add(member.Clone());
        }

        return merged;
    }

    public static void ApplyEncounterResult(MapRunSnapshot snapshot, BattleEncounterData encounter)
    {
        if (snapshot == null || encounter == null)
            return;

        if (encounter.resolvedPartyMembers != null && encounter.resolvedPartyMembers.Count > 0)
        {
            snapshot.runState.partyMembers = MergeResolvedPartyMembers(
                snapshot.runState.partyMembers, encounter.resolvedPartyMembers);

            if (encounter.wasVictory && encounter.isTutorialEncounter)
            {
                for (int i = 0; i < snapshot.runState.partyMembers.Count; i++)
                {
                    RunPartyMemberState member = snapshot.runState.partyMembers[i];
                    if (member == null)
                        continue;

                    member.currentHP = Mathf.Clamp(member.maxHP, 0, Mathf.Max(0, member.maxHP));
                }
            }

            snapshot.runState.SyncLegacyPlayerFieldsFromParty();
        }

        if (encounter.wasVictory && encounter.resolvedLoot != null && encounter.resolvedLoot.Count > 0)
        {
            for (int i = 0; i < encounter.resolvedLoot.Count; i++)
            {
                RunLootState loot = encounter.resolvedLoot[i];
                if (loot == null || loot.lootItem == null || loot.amount <= 0)
                    continue;

                snapshot.runState.AddLoot(loot.lootItem, loot.amount);
            }
        }

        if (encounter.wasVictory && encounter.resolvedRelics != null && encounter.resolvedRelics.Count > 0)
        {
            for (int i = 0; i < encounter.resolvedRelics.Count; i++)
            {
                RunRelicState relic = encounter.resolvedRelics[i];
                if (relic == null || relic.relicData == null || relic.amount <= 0)
                    continue;

                int previewed = 0;
                if (encounter.previewedRelicRewards != null)
                    foreach (RunRelicState known in encounter.previewedRelicRewards)
                        if (known?.relicData == relic.relicData) previewed += Mathf.Max(0, known.amount);
                snapshot.runState.AddRelicWithPreviewedAmount(relic.relicData, relic.amount, previewed);
            }
        }

        if (encounter.wasVictory && encounter.pendingVictoryLootRewards != null && encounter.pendingVictoryLootRewards.Count > 0)
        {
            for (int i = 0; i < encounter.pendingVictoryLootRewards.Count; i++)
            {
                RunLootState loot = encounter.pendingVictoryLootRewards[i];
                if (loot == null || loot.lootItem == null || loot.amount <= 0)
                    continue;

                snapshot.runState.AddLoot(loot.lootItem, loot.amount);
            }
        }

        if (encounter.wasVictory && encounter.pendingVictoryRelicRewards != null && encounter.pendingVictoryRelicRewards.Count > 0)
        {
            for (int i = 0; i < encounter.pendingVictoryRelicRewards.Count; i++)
            {
                RunRelicState relic = encounter.pendingVictoryRelicRewards[i];
                if (relic == null || relic.relicData == null || relic.amount <= 0)
                    continue;

                snapshot.runState.AddRelic(relic.relicData, relic.amount);
            }
        }

        if (encounter.wasVictory && encounter.pendingVictoryBlueprintRewards != null && encounter.pendingVictoryBlueprintRewards.Count > 0)
        {
            for (int i = 0; i < encounter.pendingVictoryBlueprintRewards.Count; i++)
            {
                RunBlueprintState blueprint = encounter.pendingVictoryBlueprintRewards[i];
                if (blueprint == null || blueprint.blueprintData == null || blueprint.amount <= 0)
                    continue;

                snapshot.runState.AddBlueprint(blueprint.blueprintData, blueprint.amount);
            }
        }

        if (encounter.wasVictory && encounter.pendingVictoryWeaponRewards != null && encounter.pendingVictoryWeaponRewards.Count > 0)
        {
            for (int i = 0; i < encounter.pendingVictoryWeaponRewards.Count; i++)
            {
                RunWeaponState weapon = encounter.pendingVictoryWeaponRewards[i];
                if (weapon == null || weapon.weaponData == null || weapon.amount <= 0)
                    continue;

                snapshot.runState.AddWeapon(weapon.weaponData, weapon.amount);
            }
        }

        if (encounter.wasVictory && encounter.moneyGained > 0)
            snapshot.runState.AddMoney(encounter.moneyGained);

        if (encounter.wasVictory)
        {
            if (snapshot.runState.settlementStats == null)
                snapshot.runState.settlementStats = new RunSettlementStats();

            snapshot.runState.settlementStats.IncrementBattle(encounter.tileType, encounter.dangerBand);
            if (encounter.isEventEncounter)
                snapshot.runState.settlementStats.eventBattlesCompleted++;
        }

        SyncConsumedBattleRelics(snapshot, encounter);

        for (int i = 0; i < snapshot.tiles.Count; i++)
        {
            BoardTileData tile = snapshot.tiles[i];
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

    private static void SyncConsumedBattleRelics(MapRunSnapshot snapshot, BattleEncounterData encounter)
    {
        if (snapshot?.runState == null || encounter == null || encounter.relicInventory == null)
            return;

        for (int i = 0; i < encounter.relicInventory.Count; i++)
        {
            RunRelicState battleRelic = encounter.relicInventory[i];
            if (battleRelic == null || battleRelic.relicData == null || battleRelic.amount > 0)
                continue;

            int ownedAmount = snapshot.runState.GetRelicAmount(battleRelic.relicData);
            if (ownedAmount > 0)
                snapshot.runState.TryConsumeRelic(battleRelic.relicData, ownedAmount);
        }
    }

    public static void ApplyRestResult(string sceneName, Vector2Int sourceCoord)
    {
        ApplyRestResult(currentSnapshot, sceneName, sourceCoord);
    }

    public static void ApplyRestResult(MapRunSnapshot snapshot, string sceneName, Vector2Int sourceCoord)
    {
        if (snapshot == null || string.IsNullOrWhiteSpace(sceneName))
            return;

        if (!string.Equals(snapshot.sceneName, sceneName, StringComparison.OrdinalIgnoreCase))
            return;

        if (snapshot.runState?.partyMembers != null)
        {
            for (int i = 0; i < snapshot.runState.partyMembers.Count; i++)
            {
                RunPartyMemberState member = snapshot.runState.partyMembers[i];
                if (member == null)
                    continue;

                int maxHP = Mathf.Max(0, member.maxHP);
                int healPercent = CalamitySystem.ResolveRestHealPercent(100, snapshot.runState.activeCalamities);
                int healAmount = Mathf.CeilToInt(maxHP * (healPercent / 100f));
                member.currentHP = Mathf.Clamp(member.currentHP + healAmount, 0, maxHP);
            }

            snapshot.runState.SyncLegacyPlayerFieldsFromParty();
        }

        RelicTriggerDispatcher.ProcessRestHealCompleted(snapshot.runState);

        for (int i = 0; i < snapshot.tiles.Count; i++)
        {
            BoardTileData tile = snapshot.tiles[i];
            if (tile == null || tile.coord != sourceCoord)
                continue;

            tile.isVisited = true;
            tile.isResolved = true;
            break;
        }
    }

    public static void ApplyRestoreActionPointsResult(string sceneName, Vector2Int sourceCoord)
    {
        ApplyRestoreActionPointsResult(currentSnapshot, sceneName, sourceCoord);
    }

    public static void ApplyRestoreActionPointsResult(MapRunSnapshot snapshot, string sceneName, Vector2Int sourceCoord)
    {
        if (snapshot == null || string.IsNullOrWhiteSpace(sceneName))
            return;

        if (!string.Equals(snapshot.sceneName, sceneName, StringComparison.OrdinalIgnoreCase))
            return;

        int maxActionPoints = Mathf.Max(0, snapshot.actionPointBase + snapshot.actionPointBonus);
        int restorePercent = CalamitySystem.ResolveRestActionPointRestorePercent(100, snapshot.runState != null ? snapshot.runState.activeCalamities : null);
        int restoreAmount = Mathf.CeilToInt(maxActionPoints * (restorePercent / 100f));
        snapshot.actionPointCurrent = Mathf.Clamp(snapshot.actionPointCurrent + restoreAmount, 0, maxActionPoints);
        if (snapshot.suppressOnePendingCalamityOnActionPointRestore && snapshot.pendingCalamityTiers.Count > 0)
            snapshot.pendingCalamityTiers.RemoveAt(0);
        snapshot.suppressOnePendingCalamityOnActionPointRestore = false;
        RelicTriggerDispatcher.ProcessRestActionPointRestored(snapshot.runState);

        for (int i = 0; i < snapshot.tiles.Count; i++)
        {
            BoardTileData tile = snapshot.tiles[i];
            if (tile == null || tile.coord != sourceCoord)
                continue;

            tile.isVisited = true;
            tile.isResolved = true;
            break;
        }
    }

    public static bool TryApplyRecruitResult(string sceneName, Vector2Int sourceCoord, CharacterData_SO characterData, int recruitCost, int maxRecruitCount, out string errorMessage)
    {
        return TryApplyRecruitResult(currentSnapshot, sceneName, sourceCoord, characterData, recruitCost, maxRecruitCount, out errorMessage);
    }

    public static bool TryApplyRecruitResult(MapRunSnapshot snapshot, string sceneName, Vector2Int sourceCoord, CharacterData_SO characterData, int recruitCost, int maxRecruitCount, out string errorMessage)
    {
        if (snapshot == null || string.IsNullOrWhiteSpace(sceneName))
        {
            errorMessage = GameText.Get("rest.reforge.missing_snapshot", "未找到地图进度快照。");
            return false;
        }

        if (!string.Equals(snapshot.sceneName, sceneName, StringComparison.OrdinalIgnoreCase))
        {
            errorMessage = GameText.Get("rest.reforge.snapshot_mismatch", "当前地图进度与休息场景不匹配。");
            return false;
        }

        if (snapshot.runState == null)
        {
            errorMessage = GameText.Get("rest.reforge.missing_party", "运行中队伍数据丢失。");
            return false;
        }

        if (snapshot.runState.recruitmentDisabled)
        {
            errorMessage = GameText.Get("ui.rest_scene_runtime.a_relic_is_preventing_recruitment", "当前遗物效果禁止招募。");
            return false;
        }

        if (characterData == null)
        {
            errorMessage = GameText.Get("rest.recruit.invalid_candidate", "未选择可招募角色。");
            return false;
        }

        if (maxRecruitCount > 0 && snapshot.runState.recruitedMemberCount >= maxRecruitCount)
        {
            errorMessage = GameText.Get("rest.recruit.limit", "招募次数已达上限，无法继续招募。");
            return false;
        }

        if (snapshot.runState.HasCharacter(characterData))
        {
            errorMessage = GameText.Get("rest.recruit.already_member", "该角色已在队伍中。");
            return false;
        }

        if (!snapshot.runState.TrySpendMoney(Mathf.Max(0, recruitCost)))
        {
            errorMessage = GameText.Format("rest.recruit.need_gold", "金钱不足，需要 {max} 金币。", ("max", Mathf.Max(0, recruitCost)));
            return false;
        }

        int baseHp = Mathf.Max(1, characterData.baseHP);
        Weapon_SO initialWeapon = characterData.initialWeaponData;
        snapshot.runState.AddPartyMember(new RunPartyMemberState
        {
            memberId = characterData.CharacterId,
            characterData = characterData,
            weaponData = initialWeapon,
            level = 1,
            experience = 0,
            preCalamityMaxHP = baseHp,
            maxHP = baseHp,
            currentHP = baseHp
        });
        RunPartyMemberMaxHpService.RecalculateParty(snapshot.runState);

        if (initialWeapon != null)
            snapshot.runState.AddWeapon(initialWeapon, 1);

        snapshot.runState.recruitedMemberCount = Mathf.Max(0, snapshot.runState.recruitedMemberCount + 1);

        for (int i = 0; i < snapshot.tiles.Count; i++)
        {
            BoardTileData tile = snapshot.tiles[i];
            if (tile == null || tile.coord != sourceCoord)
                continue;

            tile.isVisited = true;
            tile.isResolved = true;
            break;
        }

        errorMessage = string.Empty;
        return true;
    }

    public static bool TryApplyLootReforgeResult(
        string sceneName,
        LootItem_SO firstLoot,
        LootItem_SO secondLoot,
        LootItem_SO resultLoot,
        IReadOnlyList<LootItem_SO> lootReforgePool,
        out string errorMessage)
    {
        return TryApplyLootReforgeResult(currentSnapshot, sceneName, firstLoot, secondLoot, resultLoot, lootReforgePool, out errorMessage);
    }

    public static bool TryApplyLootReforgeResult(MapRunSnapshot snapshot,
        string sceneName,
        LootItem_SO firstLoot,
        LootItem_SO secondLoot,
        LootItem_SO resultLoot,
        IReadOnlyList<LootItem_SO> lootReforgePool,
        out string errorMessage)
    {
        if (snapshot == null || string.IsNullOrWhiteSpace(sceneName))
        {
            errorMessage = GameText.Get("rest.reforge.missing_snapshot", "未找到地图进度快照。");
            return false;
        }

        if (!string.Equals(snapshot.sceneName, sceneName, StringComparison.OrdinalIgnoreCase))
        {
            errorMessage = GameText.Get("rest.reforge.snapshot_mismatch", "当前地图进度与休息场景不匹配。");
            return false;
        }

        if (snapshot.runState == null)
        {
            errorMessage = GameText.Get("rest.reforge.missing_party", "运行中的队伍数据丢失。");
            return false;
        }

        if (firstLoot == null || secondLoot == null || resultLoot == null)
        {
            errorMessage = GameText.Get("rest.reforge.invalid_materials", "重铸材料或结果未配置。");
            return false;
        }

        if (firstLoot.rarity != secondLoot.rarity)
        {
            errorMessage = GameText.Get("rest.reforge.both_loot_items_must_have_the_same_rarity", "两件战利品必须为同阶级。");
            return false;
        }

        if (resultLoot.rarity != firstLoot.rarity)
        {
            errorMessage = GameText.Get("rest.reforge.loot_rarity_mismatch", "目标战利品的阶级与材料不匹配。");
            return false;
        }

        int requiredFirstAmount = firstLoot == secondLoot ? 2 : 1;
        if (snapshot.runState.GetLootAmount(firstLoot) < requiredFirstAmount)
        {
            errorMessage = GameText.Format("rest.reforge.material_missing", "材料不足：{material}", ("material", GetLootDisplayName(firstLoot)));
            return false;
        }

        if (firstLoot != secondLoot && snapshot.runState.GetLootAmount(secondLoot) < 1)
        {
            errorMessage = GameText.Format("rest.reforge.material_missing", "材料不足：{material}", ("material", GetLootDisplayName(secondLoot)));
            return false;
        }

        RelicExecutionContext lootReforgeContext = RelicTriggerDispatcher.ProcessLootReforge(
            snapshot.runState,
            firstLoot,
            secondLoot,
            resultLoot,
            lootReforgePool);

        if (!snapshot.runState.TryConsumeLoot(firstLoot, 1))
        {
            errorMessage = GameText.Format("rest.reforge.material_unavailable", "无法消耗材料：{material}", ("material", GetLootDisplayName(firstLoot)));
            return false;
        }

        if (!snapshot.runState.TryConsumeLoot(secondLoot, 1))
        {
            snapshot.runState.AddLoot(firstLoot, 1);
            errorMessage = GameText.Format("rest.reforge.material_unavailable", "无法消耗材料：{material}", ("material", GetLootDisplayName(secondLoot)));
            return false;
        }

        snapshot.runState.AddLoot(resultLoot, 1);
        if (lootReforgeContext.bonusLootReforgeRewards != null)
        {
            for (int bonusIndex = 0; bonusIndex < lootReforgeContext.bonusLootReforgeRewards.Count; bonusIndex++)
            {
                LootItem_SO bonusLoot = lootReforgeContext.bonusLootReforgeRewards[bonusIndex];
                if (bonusLoot != null)
                    snapshot.runState.AddLoot(bonusLoot, 1);
            }
        }
        errorMessage = string.Empty;
        RelicTriggerDispatcher.ProcessRestReforgeCompleted(snapshot.runState);
        return true;
    }

    public static bool TryApplyRelicReforgeResult(
        string sceneName,
        Relic_SO firstRelic,
        Relic_SO secondRelic,
        Relic_SO resultRelic,
        out string errorMessage)
    {
        return TryApplyRelicReforgeResult(currentSnapshot, sceneName, firstRelic, secondRelic, resultRelic, out errorMessage);
    }

    public static bool TryApplyRelicReforgeResult(MapRunSnapshot snapshot,
        string sceneName,
        Relic_SO firstRelic,
        Relic_SO secondRelic,
        Relic_SO resultRelic,
        out string errorMessage)
    {
        if (snapshot == null || string.IsNullOrWhiteSpace(sceneName))
        {
            errorMessage = GameText.Get("rest.reforge.missing_snapshot", "未找到地图进度快照。");
            return false;
        }

        if (!string.Equals(snapshot.sceneName, sceneName, StringComparison.OrdinalIgnoreCase))
        {
            errorMessage = GameText.Get("rest.reforge.snapshot_mismatch", "当前地图进度与休息场景不匹配。");
            return false;
        }

        if (snapshot.runState == null)
        {
            errorMessage = GameText.Get("rest.reforge.missing_party", "运行中的队伍数据丢失。");
            return false;
        }

        if (firstRelic == null || secondRelic == null || resultRelic == null)
        {
            errorMessage = GameText.Get("rest.reforge.invalid_relics", "重铸遗物或结果未配置。");
            return false;
        }

        if (firstRelic.rarity != secondRelic.rarity)
        {
            errorMessage = GameText.Get("rest.reforge.both_relics_must_have_the_same_rarity", "两件遗物必须为同阶级。");
            return false;
        }

        bool isFixedReforgeResult = RelicFixedReforgeService.IsFixedResult(
            snapshot.runState,
            firstRelic,
            secondRelic,
            resultRelic);

        if (!isFixedReforgeResult && resultRelic.rarity <= firstRelic.rarity)
        {
            errorMessage = GameText.Get("rest.reforge.relic_rarity_mismatch", "结果遗物必须高于材料阶级。");
            return false;
        }

        int requiredFirstAmount = firstRelic == secondRelic ? 2 : 1;
        if (snapshot.runState.GetRelicAmount(firstRelic) < requiredFirstAmount)
        {
            errorMessage = GameText.Format("rest.reforge.material_missing", "材料不足：{material}", ("material", GetRelicDisplayName(firstRelic)));
            return false;
        }

        if (firstRelic != secondRelic && snapshot.runState.GetRelicAmount(secondRelic) < 1)
        {
            errorMessage = GameText.Format("rest.reforge.material_missing", "材料不足：{material}", ("material", GetRelicDisplayName(secondRelic)));
            return false;
        }

        RelicExecutionContext reforgeContext = RelicTriggerDispatcher.ProcessRelicReforge(
            snapshot.runState,
            firstRelic,
            secondRelic,
            resultRelic);

        Relic_SO preservedMaterial = null;
        if (reforgeContext.preservedReforgeMaterialSlot == 0)
            preservedMaterial = firstRelic;
        else if (reforgeContext.preservedReforgeMaterialSlot == 1)
            preservedMaterial = secondRelic;

        if (!snapshot.runState.TryConsumeRelic(firstRelic, 1))
        {
            errorMessage = GameText.Format("rest.reforge.material_unavailable", "无法消耗材料：{material}", ("material", GetRelicDisplayName(firstRelic)));
            return false;
        }

        if (!snapshot.runState.TryConsumeRelic(secondRelic, 1))
        {
            snapshot.runState.AddPreviewedRelic(firstRelic, 1);
            errorMessage = GameText.Format("rest.reforge.material_unavailable", "无法消耗材料：{material}", ("material", GetRelicDisplayName(secondRelic)));
            return false;
        }

        snapshot.runState.AddRelic(resultRelic, 1);
        if (preservedMaterial != null)
            snapshot.runState.AddPreviewedRelic(preservedMaterial, 1);
        errorMessage = string.Empty;
        RelicTriggerDispatcher.ProcessRestReforgeCompleted(snapshot.runState);
        return true;
    }

    public static void ApplyRestVisitConsumedResult(string sceneName, Vector2Int sourceCoord)
    {
        ApplyRestVisitConsumedResult(currentSnapshot, sceneName, sourceCoord);
    }

    public static void ApplyRestVisitConsumedResult(MapRunSnapshot snapshot, string sceneName, Vector2Int sourceCoord)
    {
        if (snapshot == null || string.IsNullOrWhiteSpace(sceneName))
            return;

        if (!string.Equals(snapshot.sceneName, sceneName, StringComparison.OrdinalIgnoreCase))
            return;

        for (int i = 0; i < snapshot.tiles.Count; i++)
        {
            BoardTileData tile = snapshot.tiles[i];
            if (tile == null || tile.coord != sourceCoord)
                continue;

            tile.isVisited = true;
            tile.isResolved = true;
            break;
        }
    }

    private static string GetLootDisplayName(LootItem_SO lootItem)
    {
        if (lootItem == null)
            return GameText.Get("relic.feedback.get_loot_display_name", "战利品");

        return lootItem.LocalizedName;
    }

    private static string GetRelicDisplayName(Relic_SO relicData)
    {
        if (relicData == null)
            return GameText.Get("relic.feedback.get_relic_display_name", "遗物");

        return !string.IsNullOrWhiteSpace(relicData.DisplayName) ? relicData.LocalizedName : relicData.name;
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
