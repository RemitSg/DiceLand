using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

public sealed class AgentAdventureEconomyTests
{
    private AgentAdventureSession session;
    private readonly List<ScriptableObject> temporary = new List<ScriptableObject>();
    private Random.State outside;
    private AgentAdventureMapRuntime Map => (AgentAdventureMapRuntime)typeof(AgentAdventureSession).GetField("map", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(session);
    [SetUp] public void SetUp() { outside = Random.state; session = new AgentAdventureSession(new AgentAdventureRequest { seed = 1234 }); }
    [TearDown] public void TearDown() { session?.Dispose(); foreach (var o in temporary) UnityEngine.Object.DestroyImmediate(o); temporary.Clear(); Random.state = outside; }
    private AgentAdventureRequest Command(AgentAdventureAction a)
    {
        var s = session.Observe(); return new AgentAdventureRequest { op = a.op, assetId = a.assetId, choiceIndex = a.choiceIndex, x = a.x, y = a.y,
            sessionId = s.sessionId, revision = s.revision, requestId = Guid.NewGuid().ToString("N") };
    }
    private AgentAdventureObservation Act(AgentAdventureAction a) => session.Act(Command(a)).state;
    private AgentAdventureObservation Enter(MapTileType type)
    {
        // Directed adapter tests inject a location and resources; HTTP normal-content evidence is separate.
        BoardTileData tile = Map.LegalDestinations().First(); tile.tileType = type; tile.assignedEnemyGroup = null; tile.isResolved = false;
        Map.Snapshot.runState.money = 1000;
        return Act(new AgentAdventureAction { op = "run_move", x = tile.coord.x, y = tile.coord.y });
    }
    private CraftingBlueprint_SO Recipe()
    {
        return AssetDatabase.FindAssets("t:CraftingBlueprint_SO").Select(g => AssetDatabase.LoadAssetAtPath<CraftingBlueprint_SO>(AssetDatabase.GUIDToAssetPath(g)))
            .First(b => b != null && b.targetWeapon != null && RunCraftingService.GetRequirements(b).Count > 0);
    }

    [Test] public void MerchantStock_PersistsAfterClosingAndReopeningWithoutAnExtraMoveCost()
    {
        var state = Enter(MapTileType.Merchant); Assert.IsNotEmpty(state.merchantOffers);
        string before = string.Join("|", state.merchantOffers.Select(o => o.id + ":" + o.price + ":" + o.soldOut)); int points = state.actionPoints;
        Act(state.legalActions.Single(a => a.op == "run_leave_location"));
        state = Act(session.Observe().legalActions.Single(a => a.op == "run_interact"));
        Assert.AreEqual(before, string.Join("|", state.merchantOffers.Select(o => o.id + ":" + o.price + ":" + o.soldOut))); Assert.AreEqual(points, state.actionPoints);
    }

    [Test] public void Purchase_UsesQuotedPriceAndRetriesDoNotBuyTwice()
    {
        var state = Enter(MapTileType.Merchant);
        var action = state.legalActions.First(a => a.op == "run_buy"); var offer = state.merchantOffers.Single(o => o.index == action.choiceIndex);
        var command = Command(action); var first = session.Act(command);
        Assert.AreEqual(state.money - offer.price, first.state.money);
        Assert.IsTrue(first.state.merchantOffers.Single(o => o.index == offer.index).soldOut);
        Assert.AreEqual(JsonUtility.ToJson(first), JsonUtility.ToJson(session.Act(command)));
        Assert.IsFalse(first.state.legalActions.Any(a => a.op == "run_buy" && a.choiceIndex == offer.index));
        Assert.Throws<ArgumentException>(() => Act(new AgentAdventureAction { op = "run_buy", choiceIndex = offer.index }));
    }

    [Test] public void Refresh_ChargesOnceReplacesStockAndAdvancesCostTier()
    {
        var state = Enter(MapTileType.Merchant); int cost = state.merchantRefreshCost;
        string[] old = state.merchantOffers.Select(o => o.id).ToArray();
        var request = Command(state.legalActions.Single(a => a.op == "run_refresh_shop"));
        var refreshed = session.Act(request);
        Assert.AreEqual(state.money - cost, refreshed.state.money);
        Assert.AreEqual(1, Map.Snapshot.tiles.Single(t => t.coord == Map.Snapshot.runState.playerGridPosition).merchantRefreshCount);
        Assert.IsFalse(old.SequenceEqual(refreshed.state.merchantOffers.Select(o => o.id)));
        Assert.AreEqual(JsonUtility.ToJson(refreshed), JsonUtility.ToJson(session.Act(request)));
    }

    [Test] public void RepeatedObservations_WithChanceBasedDiscounts_DoNotChangeStateOrRng()
    {
        Enter(MapTileType.Merchant);
        var relic = ScriptableObject.CreateInstance<Relic_SO>(); temporary.Add(relic);
        relic.builtInEffects.Add(new Relic_SO.RelicBuiltInEffectEntry { trigger = Relic_SO.RelicTrigger.OnMerchantPriceCalculated,
            effectType = Relic_SO.RelicBuiltInEffectType.ReduceMerchantPricePercent, amount = 50, chance = 0.5f });
        Map.Snapshot.runState.AddRelic(relic);
        string before = JsonUtility.ToJson(Map.Snapshot); string rng = JsonUtility.ToJson(Random.state); string observation = JsonUtility.ToJson(session.Observe());
        for (int i = 0; i < 10; i++) Assert.AreEqual(observation, JsonUtility.ToJson(session.Observe()));
        Assert.AreEqual(before, JsonUtility.ToJson(Map.Snapshot)); Assert.AreEqual(rng, JsonUtility.ToJson(Random.state));
        var state = session.Observe(); var action = state.legalActions.First(a => a.op == "run_buy");
        int price = state.merchantOffers.Single(o => o.index == action.choiceIndex).price;
        Assert.AreEqual(state.money - price, Act(action).money);
    }

    [Test] public void Forge_CraftsOnlyOwnedAffordableRecipesAndRejectsDuplicateRetryConsumption()
    {
        CraftingBlueprint_SO blueprint = Recipe(); Map.Snapshot.runState.AddBlueprint(blueprint);
        foreach (var r in RunCraftingService.GetRequirements(blueprint)) Map.Snapshot.runState.AddLoot(r.lootItem, r.amount);
        var state = Enter(MapTileType.Forge); var action = state.legalActions.Single(a => a.op == "run_craft" && a.assetId == AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(blueprint)));
        int weapons = Map.Snapshot.runState.GetWeaponAmount(blueprint.targetWeapon);
        var command = Command(action); var crafted = session.Act(command);
        Assert.AreEqual(weapons + 1, Map.Snapshot.runState.GetWeaponAmount(blueprint.targetWeapon));
        Assert.AreEqual(1, Map.Snapshot.runState.settlementStats.craftedWeaponCount);
        Assert.AreEqual(JsonUtility.ToJson(crafted), JsonUtility.ToJson(session.Act(command)));
        Assert.IsFalse(crafted.state.legalActions.Any(a => a.op == "run_craft" && a.assetId == action.assetId));
        Assert.Throws<ArgumentException>(() => Act(action));
    }

    [Test] public void InsufficientMoneyOrMaterials_AreNotListedAsLegalActions()
    {
        var state = Enter(MapTileType.Merchant); Map.Snapshot.runState.money = -1; state = session.Observe();
        Assert.IsFalse(state.legalActions.Any(a => a.op == "run_buy" || a.op == "run_refresh_shop"));
        Act(state.legalActions.Single(a => a.op == "run_leave_location"));
        var tile = Map.Snapshot.tiles.Single(t => t.coord == Map.Snapshot.runState.playerGridPosition); tile.tileType = MapTileType.Forge;
        Map.Snapshot.runState.AddBlueprint(Recipe());
        state = Act(new AgentAdventureAction { op = "run_interact" });
        Assert.IsFalse(state.legalActions.Any(a => a.op == "run_craft"));
    }
}
