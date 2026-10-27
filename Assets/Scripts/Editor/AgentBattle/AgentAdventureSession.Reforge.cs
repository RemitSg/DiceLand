using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public sealed partial class AgentAdventureSession
{
    private int reforgeMode, reforgeCompleted;
    private LootItem_SO reforgeLootA, reforgeLootB;
    private LootAttribute_SO reforgeAttribute;
    private Relic_SO reforgeRelicA, reforgeRelicB;
    private AgentBattleRewardItem lastReforgeResult;
    private RunReforgeDefinition ReforgeDefinition => map.Configuration.reforge;
    private UnityEngine.Object ReforgeFirst => reforgeMode == 0 ? (UnityEngine.Object)reforgeLootA : reforgeRelicA;
    private UnityEngine.Object ReforgeSecond => reforgeMode == 0 ? (UnityEngine.Object)reforgeLootB : reforgeRelicB;
    private int ReforgeLimit => map.PreviewWithRandom(() => RunRestService.ReforgeLimit(map.Snapshot.runState.Clone()));

    private void ResetReforgeVisit()
    {
        reforgeMode = 0; reforgeCompleted = 0; lastReforgeResult = null;
        reforgeLootA = reforgeLootB = null; reforgeAttribute = null; reforgeRelicA = reforgeRelicB = null;
    }
    private void ClearReforgeSelection()
    {
        if (reforgeMode == 0) { reforgeLootA = reforgeLootB = null; reforgeAttribute = null; }
        else reforgeRelicA = reforgeRelicB = null;
    }
    private System.Random ReforgeRandom(string salt) => RunRestService.ReforgeRandom(reforgeCompleted, reforgeMode, CurrentTile.coord, "MapScene",
        reforgeLootA, reforgeLootB, reforgeAttribute, reforgeRelicA, reforgeRelicB, salt);
    private LootItem_SO RolledLoot => RunRestService.RollLootReforge(ReforgeDefinition, reforgeLootA, reforgeLootB, reforgeAttribute, ReforgeRandom("LootReforge"));
    private Relic_SO RolledRelic => RunRestService.RollRelicReforge(ReforgeDefinition, reforgeRelicA, reforgeRelicB, ReforgeRandom("RelicReforge"));

    private bool CanConfirmReforge()
    {
        return map.PreviewWithRandom(() =>
        {
            RunState state = map.Snapshot.runState.Clone();
            if (reforgeCompleted >= RunRestService.ReforgeLimit(state)) return false;
            return reforgeMode == 0
                ? RunRestService.ValidateLootReforge(state, reforgeLootA, reforgeLootB, reforgeAttribute, out _) && RolledLoot != null
                : RunRestService.ValidateRelicReforge(state, reforgeRelicA, reforgeRelicB, out _) && RolledRelic != null;
        });
    }
    private UnityEngine.Object[] ReforgeMaterialObjects()
    {
        RunState state = map.Snapshot.runState;
        return reforgeMode == 0
            ? state.lootInventory.Where(s => s?.lootItem != null && s.amount > 0).Select(s => (UnityEngine.Object)s.lootItem).Distinct().ToArray()
            : state.relicInventory.Where(s => s?.relicData != null && s.amount > 0 && s.relicData.rarity < LootRarity.Legendary).Select(s => (UnityEngine.Object)s.relicData).Distinct().ToArray();
    }
    private bool CanSelectSecond(UnityEngine.Object value)
    {
        if (ReforgeFirst == null) return false;
        if (reforgeMode == 0)
        {
            var item = (LootItem_SO)value;
            return item.rarity == reforgeLootA.rarity && map.Snapshot.runState.GetLootAmount(item) >= (item == reforgeLootA ? 2 : 1);
        }
        var relic = (Relic_SO)value;
        return relic.rarity == reforgeRelicA.rarity && map.Snapshot.runState.GetRelicAmount(relic) >= (relic == reforgeRelicA ? 2 : 1);
    }

    private AgentAdventureReforge DescribeReforge()
    {
        if (phase != "reforge") return null;
        int limit = ReforgeLimit;
        var candidates = RunRestService.LootReforgeCandidates(ReforgeDefinition, reforgeLootA, reforgeLootB, reforgeAttribute);
        Relic_SO preview = reforgeMode == 1 ? RolledRelic : null;
        return new AgentAdventureReforge
        {
            mode = reforgeMode == 0 ? "loot" : "relic", completed = reforgeCompleted, limit = limit, remaining = Math.Max(0, limit - reforgeCompleted),
            committed = reforgeCompleted > 0, canConfirm = CanConfirmReforge(), first = AssetId(ReforgeFirst), second = AssetId(ReforgeSecond), attribute = AssetId(reforgeAttribute),
            stage = ReforgeFirst == null ? "first" : ReforgeSecond == null ? "second" : reforgeMode == 0 && reforgeAttribute == null ? "attribute" : "confirm",
            materials = ReforgeMaterialObjects().Select(value =>
            {
                LootItem_SO loot = value as LootItem_SO; Relic_SO relic = value as Relic_SO;
                return new AgentAdventureReforgeMaterial { id = AssetId(value), name = loot != null ? loot.lootName : relic.DisplayName,
                    description = loot != null ? loot.description : relic.description, rarity = (loot != null ? loot.rarity : relic.rarity).ToString(),
                    attribute = loot != null ? AssetId(loot.attribute) : "", amount = loot != null ? map.Snapshot.runState.GetLootAmount(loot) : map.Snapshot.runState.GetRelicAmount(relic) };
            }).ToArray(),
            attributes = RunRestService.ReforgeAttributes(ReforgeDefinition).Select(a => new AgentBattleAsset { id = AssetId(a), name = RunRestService.AttributeName(a), description = a.description }).ToArray(),
            possibleResults = reforgeMode == 0 ? candidates.Select(c => Item(c, c.lootName, 1)).ToArray() : new AgentBattleRewardItem[0],
            resultPreview = preview != null ? Item(preview, preview.DisplayName, 1) : null
        };
    }

    private IEnumerable<AgentAdventureAction> ReforgeActions()
    {
        yield return new AgentAdventureAction { op = reforgeCompleted > 0 ? "run_reforge_finish" : "run_reforge_cancel" };
        yield return new AgentAdventureAction { op = reforgeMode == 0 ? "run_reforge_relic" : "run_reforge_loot" };
        if (ReforgeFirst != null || ReforgeSecond != null || (reforgeMode == 0 && reforgeAttribute != null)) yield return new AgentAdventureAction { op = "run_reforge_reset" };
        if (reforgeCompleted >= ReforgeLimit) yield break;
        foreach (var value in ReforgeMaterialObjects())
        {
            if (value != ReforgeFirst) yield return new AgentAdventureAction { op = "run_reforge_first", assetId = AssetId(value) };
            if (value != ReforgeSecond && CanSelectSecond(value)) yield return new AgentAdventureAction { op = "run_reforge_second", assetId = AssetId(value) };
        }
        if (reforgeMode == 0 && reforgeLootA != null && reforgeLootB != null)
            foreach (var attribute in RunRestService.ReforgeAttributes(ReforgeDefinition))
                if (attribute != reforgeAttribute && RunRestService.LootReforgeCandidates(ReforgeDefinition, reforgeLootA, reforgeLootB, attribute).Count > 0)
                    yield return new AgentAdventureAction { op = "run_reforge_attribute", assetId = AssetId(attribute) };
        if (CanConfirmReforge()) yield return new AgentAdventureAction { op = "run_reforge_confirm" };
    }

    private void ExecuteReforgeAction(AgentAdventureRequest request)
    {
        switch (request.op)
        {
            case "run_open_reforge": phase = "reforge"; return;
            case "run_reforge_cancel": phase = "rest"; return;
            case "run_reforge_loot": reforgeMode = 0; return;
            case "run_reforge_relic": reforgeMode = 1; return;
            case "run_reforge_reset": ClearReforgeSelection(); return;
            case "run_reforge_first":
                if (reforgeMode == 0) { reforgeLootA = (LootItem_SO)ReforgeMaterialObjects().Single(v => AssetId(v) == request.assetId); reforgeLootB = null; reforgeAttribute = null; }
                else { reforgeRelicA = (Relic_SO)ReforgeMaterialObjects().Single(v => AssetId(v) == request.assetId); reforgeRelicB = null; }
                return;
            case "run_reforge_second":
                if (reforgeMode == 0) { reforgeLootB = (LootItem_SO)ReforgeMaterialObjects().Single(v => AssetId(v) == request.assetId); reforgeAttribute = null; }
                else reforgeRelicB = (Relic_SO)ReforgeMaterialObjects().Single(v => AssetId(v) == request.assetId);
                return;
            case "run_reforge_attribute": reforgeAttribute = RunRestService.ReforgeAttributes(ReforgeDefinition).Single(a => AssetId(a) == request.assetId); return;
            case "run_reforge_finish": FinishReforgeVisit(); return;
            case "run_reforge_confirm":
                map.WithRandom(() =>
                {
                    MapRunSnapshot snapshot = map.Snapshot;
                    if (reforgeMode == 0)
                    {
                        LootItem_SO result = RolledLoot;
                        if (!MapRunRuntime.TryApplyLootReforgeResult(snapshot, snapshot.sceneName, reforgeLootA, reforgeLootB, result, ReforgeDefinition.LootPool, out message))
                            throw new InvalidOperationException(message);
                        lastReforgeResult = Item(result, result.lootName, 1);
                    }
                    else
                    {
                        Relic_SO result = RolledRelic;
                        if (!MapRunRuntime.TryApplyRelicReforgeResult(snapshot, snapshot.sceneName, reforgeRelicA, reforgeRelicB, result, out message))
                            throw new InvalidOperationException(message);
                        lastReforgeResult = Item(result, result.DisplayName, 1);
                    }
                    reforgeCompleted = RunRestService.AdvanceReforgeCount(reforgeCompleted, RunRestService.ReforgeLimit(snapshot.runState));
                    message = "重铸成功：" + lastReforgeResult.name; ClearReforgeSelection(); return true;
                });
                if (reforgeCompleted >= ReforgeLimit) FinishReforgeVisit();
                return;
        }
    }

    private void FinishReforgeVisit()
    {
        map.WithRandom(() =>
        {
            MapRunRuntime.ApplyRestVisitConsumedResult(map.Snapshot, map.Snapshot.sceneName, CurrentTile.coord);
            map.ApplySnapshotTransientState(); return true;
        });
        CompleteRestReturn();
    }
}
