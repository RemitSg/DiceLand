using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public sealed partial class AgentAdventureSession
{
    private List<CharacterData_SO> recruitCandidates = new List<CharacterData_SO>();
    private bool OpenRestLocation(BoardTileData tile)
    {
        if (tile.tileType != MapTileType.Rest || tile.isResolved) return false;
        tile.isVisited = true; phase = "rest";
        // Snapshot.sceneName identifies the isolated owner; recruit rolls use the formal map's logical name.
        recruitCandidates = RunRestService.GetRecruitCandidates(map.Snapshot.runState, tile.coord, "MapScene",
            map.Configuration.starting.recruitRoster, map.Configuration.starting.difficulty);
        message = "选择恢复生命、恢复行动点或招募角色。";
        return true;
    }

    private bool CanRecruit(CharacterData_SO character)
    {
        return map.PreviewWithRandom(() =>
        {
            MapRunSnapshot copy = map.Snapshot.Clone();
            return MapRunRuntime.TryApplyRecruitResult(copy, copy.sceneName, CurrentTile.coord, character,
                RunRestService.RecruitCost(copy.runState, map.Configuration.starting.difficulty),
                Mathf.Max(1, map.Configuration.starting.difficulty.maxRecruitCount), out _);
        });
    }

    private AgentAdventureRest DescribeRest()
    {
        if (phase != "rest") return null;
        RunState state = map.Snapshot.runState;
        return new AgentAdventureRest
        {
            healPercent = CalamitySystem.ResolveRestHealPercent(100, state.activeCalamities),
            actionPointRestorePercent = CalamitySystem.ResolveRestActionPointRestorePercent(100, state.activeCalamities),
            recruitCost = RunRestService.RecruitCost(state, map.Configuration.starting.difficulty), recruitedCount = state.recruitedMemberCount,
            maxRecruitCount = Mathf.Max(1, map.Configuration.starting.difficulty.maxRecruitCount),
            candidates = recruitCandidates.Select((c,i) => new AgentAdventureRecruit { index = i, id = AssetId(c), name = c.characterName,
                description = c.basicDescription, hp = c.baseHP, weapon = AssetId(c.initialWeaponData), canRecruit = CanRecruit(c) }).ToArray()
        };
    }

    private IEnumerable<AgentAdventureAction> RestActions()
    {
        yield return new AgentAdventureAction { op = "run_rest_heal" };
        yield return new AgentAdventureAction { op = "run_rest_restore_ap" };
        for (int i = 0; i < recruitCandidates.Count; i++)
            if (CanRecruit(recruitCandidates[i])) yield return new AgentAdventureAction { op = "run_recruit", choiceIndex = i };
    }

    private IEnumerable<AgentAdventureAction> EquipmentActions()
    {
        RunState state = map.Snapshot.runState;
        foreach (Weapon_SO weapon in state.weaponInventory.Where(w => w?.weaponData != null && w.amount > 0).Select(w => w.weaponData).Distinct())
            if (state.Clone().TryEquipWeapon(weapon, out _, out _)) yield return new AgentAdventureAction { op = "run_equip", assetId = AssetId(weapon) };
    }

    private void ExecuteRestAction(AgentAdventureRequest request)
    {
        map.WithRandom(() =>
        {
            MapRunSnapshot snapshot = map.Snapshot;
            if (request.op == "run_rest_heal")
            {
                MapRunRuntime.ApplyRestResult(snapshot, snapshot.sceneName, CurrentTile.coord); message = "休息完成。";
            }
            else if (request.op == "run_rest_restore_ap")
            {
                MapRunRuntime.ApplyRestoreActionPointsResult(snapshot, snapshot.sceneName, CurrentTile.coord); message = "行动点恢复完成。";
            }
            else
            {
                CharacterData_SO character = recruitCandidates[request.choiceIndex];
                if (!MapRunRuntime.TryApplyRecruitResult(snapshot, snapshot.sceneName, CurrentTile.coord, character,
                    RunRestService.RecruitCost(snapshot.runState, map.Configuration.starting.difficulty),
                    Mathf.Max(1, map.Configuration.starting.difficulty.maxRecruitCount), out message)) throw new InvalidOperationException(message);
                message = "招募成功：" + character.characterName;
            }
            map.ApplySnapshotTransientState();
            return true;
        });
        recruitCandidates.Clear();
        map.CycleFlags.suppressPendingCalamityOnNextRestActionPointRestore = false;
        phase = "map"; ClearDeferrals(); PresentCalamity();
    }
}
