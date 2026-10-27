using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Decision adapter. Gameplay changes remain in the existing run, map and battle services.
public sealed partial class AgentAdventureSession : IDisposable
{
    private readonly string id = Guid.NewGuid().ToString("N");
    private readonly int seed, maxRounds;
    private readonly Dictionary<string, string> bodies = new Dictionary<string, string>();
    private readonly Dictionary<string, string> responses = new Dictionary<string, string>();
    private readonly Queue<string> recent = new Queue<string>();
    private AgentAdventureMapRuntime map;
    private AgentAdventureBattleBridge bridge;
    private List<Calamity_SO> choices = new List<Calamity_SO>();
    private int choiceTier, revision;
    private string phase = "map", outcome, message;
    private bool disposed;
    public string Id => id;

    public AgentAdventureSession(AgentAdventureRequest request)
    {
        if (request.maxRounds < 1 || request.maxRounds > 1000) throw new ArgumentException("maxRounds must be between 1 and 1000.");
        if (BattleEncounterRuntime.ActiveEncounter != null || BattleEncounterRuntime.CurrentEncounter != null || TutorialBattleRuntime.IsActive)
            throw new InvalidOperationException("Close the existing battle before opening an adventure.");
        if (UnityEngine.Object.FindObjectsOfType<BattleManager>(true).Length != 0 || UnityEngine.Object.FindObjectsOfType<BattleAllyController>(true).Length != 0
            || UnityEngine.Object.FindObjectsOfType<BattleEnemyController>(true).Length != 0)
            throw new InvalidOperationException("An existing battle scene owns the runtime.");
        seed = request.seed; maxRounds = request.maxRounds;
        map = new AgentAdventureMapRuntime(AgentAdventureConfiguration.Load(), seed);
    }

    public AgentAdventureObservation Observe()
    {
        if (disposed) throw new ObjectDisposedException(nameof(AgentAdventureSession));
        map.SynchronizeSnapshot(); RunState state = map.Snapshot.runState;
        return new AgentAdventureObservation
        {
            sessionId = id, revision = revision, seed = seed, phase = phase, outcome = outcome, message = message,
            difficulty = state.difficultyName, x = state.playerGridPosition.x, y = state.playerGridPosition.y, boardSize = map.Board.BoardSize,
            actionPoints = map.Points.CurrentPoints, maxActionPoints = map.Points.CurrentMaxPoints, cycle = state.currentCycle,
            calamityTier = state.calamityTier, keys = state.currentKeys, requiredKeys = state.requiredKeys, money = state.money,
            configurationIssues = map.ConfigurationIssues,
            tiles = map.Snapshot.tiles.Select(t => new AgentAdventureTile { x = t.coord.x, y = t.coord.y, type = t.tileType.ToString(), dangerBand = t.dangerBand.ToString(),
                danger = t.previewDangerRating, visited = t.isVisited, resolved = t.isResolved, encounterAvailable = t.assignedEnemyGroup != null && t.assignedEnemyGroup.IsValid,
                moveCost = MapMovementService.GetMoveCost(state, t) }).ToArray(),
            party = state.partyMembers.Select(m => new AgentAdventurePartyMember { id = m.memberId, name = m.characterData != null ? m.characterData.characterName : m.memberId,
                character = AssetId(m.characterData), weapon = AssetId(m.weaponData), hp = m.currentHP, maxHp = m.maxHP, level = m.level, experience = m.experience }).ToArray(),
            loot = state.lootInventory.Where(s => s?.lootItem != null).Select(s => Item(s.lootItem, s.lootItem.lootName, s.amount)).ToArray(),
            relics = state.relicInventory.Where(s => s?.relicData != null).Select(s => Item(s.relicData, s.relicData.DisplayName, s.amount)).ToArray(),
            weapons = state.weaponInventory.Where(s => s?.weaponData != null).Select(s => Item(s.weaponData, s.weaponData.weaponName, s.amount)).ToArray(),
            blueprints = state.blueprintInventory.Where(s => s?.blueprintData != null).Select(s => Item(s.blueprintData, s.blueprintData.name, s.amount)).ToArray(),
            calamityChoices = choices.Select(c => new AgentBattleAsset { id = AssetId(c), name = c.DisplayName, description = c.description }).ToArray(),
            activeCalamities = state.activeCalamities.Where(c => c?.calamityData != null).Select(c => new AgentBattleAsset { id = AssetId(c.calamityData), name = c.calamityData.DisplayName, description = c.calamityData.description }).ToArray(),
            battle = bridge != null ? bridge.Battle.Observe() : null, legalActions = LegalActions().ToArray(),
            merchantOffers = DescribeMerchant(), merchantRefreshCost = phase == "merchant" ? MerchantStockService.RefreshCost(state, CurrentTile, map.Configuration.starting.difficulty) : 0,
            recipes = DescribeRecipes(), rest = DescribeRest()
        };
    }

    public AgentAdventureResponse Act(AgentAdventureRequest request)
    {
        if (disposed) throw new ObjectDisposedException(nameof(AgentAdventureSession));
        if (request.sessionId != id || string.IsNullOrWhiteSpace(request.requestId)) throw new ArgumentException("A matching sessionId and unique requestId are required.");
        string body = JsonUtility.ToJson(request);
        if (bodies.TryGetValue(request.requestId, out string old))
        {
            if (old != body) throw new ArgumentException("requestId was already used with different parameters.");
            if (!responses.TryGetValue(request.requestId, out string cached)) throw new ArgumentException("The cached response expired; observe the current state.");
            return JsonUtility.FromJson<AgentAdventureResponse>(cached);
        }
        if (request.revision != revision) throw new ArgumentException("Stale revision; observe the adventure.");
        if (bodies.Count >= 10000) throw new ArgumentException("Adventure command limit reached.");
        if (!LegalActions().Any(a => Matches(a, request))) throw new ArgumentException("Illegal action; select from legalActions.");
        try
        {
            switch (request.op)
            {
                case "run_move":
                    map.Board.TryGetTile(new Vector2Int(request.x, request.y), out BoardTileData destination);
                    bool visitedEmpty = destination.isVisited && destination.isResolved && destination.tileType == MapTileType.Empty;
                    if (!map.TryMove(destination.coord)) throw new InvalidOperationException("The map rejected this move.");
                    message = map.LastMoveMessage;
                    if (visitedEmpty) map.WithRandom(() => MapMovementService.TryTriggerVisitedEmptyTileAmbush(map.Snapshot.runState, map.Board, map.Configuration.enemies, destination));
                    EnterLocation(destination); break;
                case "run_interact":
                    map.Board.TryGetTile(map.Snapshot.runState.playerGridPosition, out BoardTileData current); EnterLocation(current); break;
                case "run_battle":
                    AgentBattleObservation before = bridge.Battle.Observe(); AgentBattleLegalAction action = request.battleAction;
                    bridge.Battle.Act(new AgentBattleRequest { op = action.op, actorId = action.actorId, buttonIndex = action.buttonIndex, skillIndex = action.skillIndex,
                        targetId = action.targetId, targetButtonIndex = action.targetButtonIndex, choiceIndex = action.choiceIndex,
                        sessionId = before.sessionId, revision = before.revision, requestId = request.requestId });
                    break;
                case "run_return": ReturnFromBattle(); break;
                case "run_choose_calamity": ChooseCalamity(request.choiceIndex); break;
                case "run_buy": case "run_refresh_shop": case "run_craft": ExecuteEconomyAction(request); break;
                case "run_rest_heal": case "run_rest_restore_ap": case "run_recruit": ExecuteRestAction(request); break;
                case "run_equip":
                    Weapon_SO equip = map.Snapshot.runState.weaponInventory.Select(w => w.weaponData).First(w => w != null && AssetId(w) == request.assetId);
                    if (!map.Snapshot.runState.TryEquipWeapon(equip, out RunPartyMemberState owner, out message)) throw new InvalidOperationException(message);
                    message = owner.DisplayName + " 装备了 " + equip.weaponName; break;
                case "run_leave_location":
                    if (phase == "merchant") RelicMerchantService.ResetVisitState(CurrentTile);
                    phase = "map"; ClearDeferrals(); PresentCalamity(); break;
            }
            revision++;
        }
        catch { phase = "faulted"; outcome = "execution_error"; throw; }
        var response = new AgentAdventureResponse { success = true, state = Observe() };
        bodies.Add(request.requestId, body); responses.Add(request.requestId, JsonUtility.ToJson(response)); recent.Enqueue(request.requestId);
        while (recent.Count > 32) responses.Remove(recent.Dequeue());
        return response;
    }

    private IEnumerable<AgentAdventureAction> LegalActions()
    {
        if (phase == "map")
        {
            foreach (BoardTileData tile in map.LegalDestinations()) yield return new AgentAdventureAction { op = "run_move", x = tile.coord.x, y = tile.coord.y };
            map.Board.TryGetTile(map.Snapshot.runState.playerGridPosition, out BoardTileData tileHere);
            if (CanInteract(tileHere)) yield return new AgentAdventureAction { op = "run_interact" };
            foreach (AgentAdventureAction equip in EquipmentActions()) yield return equip;
        }
        else if (phase == "battle")
        {
            foreach (AgentBattleLegalAction action in bridge.Battle.Observe().legalActions) yield return new AgentAdventureAction { op = "run_battle", battleAction = action };
            if (bridge.Battle.ResultReady) yield return new AgentAdventureAction { op = "run_return" };
        }
        else if (phase == "calamity")
            for (int i = 0; i < choices.Count; i++) yield return new AgentAdventureAction { op = "run_choose_calamity", choiceIndex = i };
        else if (phase == "rest")
            foreach (AgentAdventureAction action in RestActions()) yield return action;
        else if (phase == "merchant" || phase == "forge")
        {
            foreach (AgentAdventureAction action in EconomyActions()) yield return action;
            yield return new AgentAdventureAction { op = "run_leave_location" };
        }
    }

    private bool CanInteract(BoardTileData tile)
    {
        if (tile == null) return false;
        if (tile.IsHostileTile) return !tile.isResolved && tile.assignedEnemyGroup != null && tile.assignedEnemyGroup.IsValid
            && (tile.tileType != MapTileType.Boss || map.Snapshot.runState.IsBossUnlocked);
        return tile.tileType == MapTileType.Merchant || tile.tileType == MapTileType.Forge || (tile.tileType == MapTileType.Rest && !tile.isResolved)
            || (!tile.isResolved && (tile.tileType == MapTileType.Event || tile.tileType == MapTileType.Treasure));
    }

    private void EnterLocation(BoardTileData tile)
    {
        phase = "map";
        if (tile.IsHostileTile)
        {
            if (tile.tileType == MapTileType.Boss && !map.Snapshot.runState.IsBossUnlocked)
                message = "Boss is locked until the required keys are collected.";
            else if (tile.assignedEnemyGroup == null || !tile.assignedEnemyGroup.IsValid)
                message = "This tile has no configured enemy group; it has not been cleared.";
            else
            {
                bridge = new AgentAdventureBattleBridge(map.Snapshot, tile, tile.assignedEnemyGroup, map.Configuration.starting.difficulty,
                    map.Configuration.money, map.Configuration.eliteRelics, map.Configuration.levels, maxRounds);
                tile.isVisited = true; phase = "battle"; return;
            }
        }
        else if (CanInteract(tile))
        {
            if (OpenEconomyLocation(tile)) return;
            if (OpenRestLocation(tile)) return;
            phase = tile.tileType.ToString().ToLowerInvariant();
            message = "This location requires the next adventure feature batch; no placeholder rewards have been applied.";
            return;
        }
        ClearDeferrals(); PresentCalamity();
    }

    private void ReturnFromBattle()
    {
        BattleEncounterData encounter = bridge.Battle.Encounter;
        bridge.ApplyResult(); bridge.Dispose(); bridge = null;
        ClearDeferrals();
        if (encounter.wasVictory && encounter.tileType == MapTileType.Boss) { phase = "victory_pending_settlement"; outcome = "victory"; return; }
        if (!encounter.wasVictory && !encounter.returnToMapOnDefeat) { phase = "defeat_pending_settlement"; outcome = "defeat"; return; }
        phase = "map"; PresentCalamity();
    }

    private void PresentCalamity()
    {
        while (phase == "map" && map.PendingCalamities.Count > 0)
        {
            choiceTier = Math.Max(1, map.PendingCalamities.Dequeue());
            choices = map.WithRandom(() => CalamitySelectionService.RollChoices(map.Configuration.calamities, map.Snapshot.runState, choiceTier, 2,
                new System.Random(UnityEngine.Random.Range(int.MinValue, int.MaxValue))));
            if (choices.Count == 0) continue;
            CalamitySelectionService.MarkChoicesDrawn(map.Snapshot.runState, choices); phase = "calamity";
        }
        map.SynchronizeSnapshot();
    }

    private void ChooseCalamity(int index)
    {
        Calamity_SO selected = choices[index];
        map.WithRandom(() =>
        {
            CalamitySelectionService.ApplyChosenCalamity(map.Snapshot.runState, selected, choiceTier);
            var feedback = CalamitySelectionService.ApplyBuiltInMapEffects(selected, map.Snapshot.runState, map.Board, map.Configuration.enemies);
            selected.customEffectHandler?.OnCalamityApplied(map.Snapshot.runState, null);
            RelicExecutionContext context = RelicTriggerDispatcher.ProcessCalamityApplied(map.Snapshot.runState, null, selected, choiceTier);
            message = selected.DisplayName + "\n" + string.Join("\n", feedback.Concat(context.feedbackMessages ?? new List<string>()));
            return true;
        });
        choices.Clear(); phase = "map"; PresentCalamity();
    }
    private void ClearDeferrals() { map.CycleFlags.deferPendingCalamityUntilMapResume = false; map.CycleFlags.deferPendingCalamityUntilOverlayClosed = false; }
    private static string AssetId(UnityEngine.Object value) => value != null ? AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(value)) : "";
    private static AgentBattleRewardItem Item(UnityEngine.Object value, string name, int amount) => new AgentBattleRewardItem { id = AssetId(value), name = name, amount = amount };
    private static bool Matches(AgentAdventureAction a, AgentAdventureRequest r)
    {
        if (a.op != r.op) return false;
        if (a.op == "run_move") return a.x == r.x && a.y == r.y;
        if (a.op == "run_choose_calamity") return a.choiceIndex == r.choiceIndex;
        if (a.op == "run_buy" || a.op == "run_recruit") return a.choiceIndex == r.choiceIndex;
        if (a.op == "run_craft" || a.op == "run_equip") return a.assetId == r.assetId;
        if (a.op == "run_battle") return JsonUtility.ToJson(a.battleAction) == JsonUtility.ToJson(r.battleAction);
        return true;
    }
    public void Dispose() { if (disposed) return; disposed = true; bridge?.Dispose(); bridge = null; map?.Dispose(); map = null; }
}
