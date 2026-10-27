using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

public sealed class AgentAdventureTreasureTests
{
    private AgentAdventureSession session;
    private Random.State outside;
    private MapRunSnapshot global;
    private readonly List<UnityEngine.Object> temporary = new List<UnityEngine.Object>();
    private AgentAdventureMapRuntime Map => (AgentAdventureMapRuntime)typeof(AgentAdventureSession).GetField("map", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(session);
    private BoardTileData Tile => Map.Snapshot.tiles.Single(t => t.coord == Map.Snapshot.runState.playerGridPosition);
    [SetUp] public void SetUp()
    {
        outside = Random.state;
        global = (MapRunSnapshot)typeof(MapRunRuntime).GetField("currentSnapshot", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
    }
    [TearDown] public void TearDown()
    {
        session?.Dispose(); session = null;
        foreach (var obj in temporary) if (obj != null) UnityEngine.Object.DestroyImmediate(obj);
        temporary.Clear(); Random.state = outside; MapRunRuntime.RestoreSnapshot(global);
    }
    private T Temp<T>() where T : ScriptableObject { var obj = ScriptableObject.CreateInstance<T>(); temporary.Add(obj); return obj; }
    private static void Set(object value, string field, object content) => value.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(value, content);
    private static T Find<T>(Func<T, bool> filter) where T : UnityEngine.Object => AssetDatabase.FindAssets("t:" + typeof(T).Name, new[] { "Assets/Data" })
        .Select(g => AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(g))).First(filter);
    private static string Id(UnityEngine.Object value) => AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(value));
    private void Open() { session = new AgentAdventureSession(new AgentAdventureRequest { seed = 1234 }); }
    private AgentAdventureRequest Command(string op, int index = 0)
    {
        var state = session.Observe(); return new AgentAdventureRequest { sessionId = state.sessionId, revision = state.revision,
            requestId = Guid.NewGuid().ToString("N"), op = op, choiceIndex = index };
    }
    private AgentAdventureObservation Act(string op, int index = 0) => session.Act(Command(op, index)).state;
    private AgentAdventureObservation Enter(Action<BoardTileData> configure = null)
    {
        var tile = Map.LegalDestinations().First(); tile.tileType = MapTileType.Treasure; tile.isResolved = false; tile.isVisited = false;
        configure?.Invoke(tile);
        var command = Command("run_move"); command.x = tile.coord.x; command.y = tile.coord.y;
        return session.Act(command).state;
    }
    private List<Relic_SO> Relics(int count)
    {
        Relic_SO acquired = Find<Relic_SO>(r => r.relicId == "82");
        return new[] { acquired }.Concat(AssetDatabase.FindAssets("t:Relic_SO", new[] { "Assets/Data" })
            .Select(g => AssetDatabase.LoadAssetAtPath<Relic_SO>(AssetDatabase.GUIDToAssetPath(g))).Where(r => r != acquired && r.rarity == acquired.rarity)).Take(count).ToList();
    }
    private RelicTable_SO RelicTable(List<Relic_SO> relics)
    { var table = Temp<RelicTable_SO>(); Set(table, "relics", relics); return table; }
    private void ConfigureBag(List<Relic_SO> relics)
    {
        var table = Temp<TreasureRewardTable_SO>(); Set(table, "relicTable", RelicTable(relics));
        var weights = Temp<LootRarityWeightTable_SO>();
        Set(weights, "rarityWeights", new List<LootRarityWeightTable_SO.RarityWeightEntry>
        { new LootRarityWeightTable_SO.RarityWeightEntry { rarity = relics[0].rarity, weight = 1 } });
        Set(table, "relicBagRarityWeightTable", weights); Map.Configuration.treasure = table;
    }

    [Test] public void WealthIsTheRealQuotedAmountAndRetriedChoiceDoesNotGrantItTwice()
    {
        Open(); var state = Enter(); int quote = state.treasure.options.Single(o => o.kind == "Money").money;
        Assert.Greater(quote, 0); Assert.AreEqual(0, state.money); Assert.IsEmpty(state.treasure.relicChoices);
        var command = Command("run_choose_treasure", 0); var first = session.Act(command);
        Assert.AreEqual("treasure_result", first.state.phase); Assert.AreEqual(quote, first.state.money);
        Assert.AreEqual(quote, first.state.treasure.result.money); Assert.IsTrue(Tile.isResolved);
        Assert.AreEqual(JsonUtility.ToJson(first), JsonUtility.ToJson(session.Act(command)));
        Assert.Throws<ArgumentException>(() => Act("run_choose_treasure", 1));
        Assert.AreEqual("map", Act("run_continue_treasure").phase);
        Assert.IsFalse(session.Observe().legalActions.Any(a => a.op == "run_interact"));
    }

    [Test] public void KnowledgeGrantsExactlyTheDisplayedBlueprint()
    {
        Open(); var state = Enter(); var option = state.treasure.options.Single(o => o.kind == "Blueprint");
        Assert.IsTrue(option.selectable); state = Act("run_choose_treasure", option.index);
        Assert.AreEqual(option.blueprint, state.treasure.result.blueprint.id);
        Assert.AreEqual(1, state.blueprints.Single(b => b.id == option.blueprint).amount); Assert.AreEqual(0, state.money);
        Assert.IsTrue(Tile.isResolved); Assert.AreEqual("map", Act("run_continue_treasure").phase);
    }

    [Test] public void RelicBagRequiresOneChoiceBeforeResolutionAndAppliesAcquisitionOnce()
    {
        Open(); var relics = Relics(3); ConfigureBag(relics); Enter();
        int maxHp = Map.Snapshot.runState.partyMembers[0].maxHP;
        var state = Act("run_choose_treasure", 2);
        Assert.AreEqual("treasure_relic", state.phase); Assert.IsFalse(Tile.isResolved); Assert.IsEmpty(state.relics);
        Assert.AreEqual(3, state.treasure.relicChoices.Length); Assert.Throws<ArgumentException>(() => Act("run_continue_treasure"));
        int index = state.treasure.relicChoices.Single(r => r.id == Id(relics[0])).index;
        var command = Command("run_choose_treasure_relic", index); var selected = session.Act(command);
        Assert.AreEqual(JsonUtility.ToJson(selected), JsonUtility.ToJson(session.Act(command)));
        Assert.AreEqual(1, Map.Snapshot.runState.GetRelicAmount(relics[0])); Assert.AreEqual(maxHp + 3, selected.state.party.Single().maxHp);
        Assert.IsTrue(Tile.isResolved); Assert.AreEqual("treasure_result", selected.state.phase);
    }

    [Test] public void RefreshUsesTheEligibleRarityPoolExcludesOwnedAndCurrentAndConsumesOneUse()
    {
        Open(); var relics = Relics(7); Assert.AreEqual(7, relics.Count); ConfigureBag(relics);
        Map.Snapshot.runState.AddRelic(relics[6], 1); Map.Snapshot.runState.abilityRelicChoiceRefreshCount = 1;
        Enter(); var state = Act("run_choose_treasure", 2);
        string[] before = state.treasure.relicChoices.Select(r => r.id).ToArray(); Assert.IsTrue(state.treasure.canRefresh);
        Assert.IsFalse(before.Contains(Id(relics[6]))); var command = Command("run_refresh_treasure_relics");
        var refreshed = session.Act(command);
        Assert.AreEqual(JsonUtility.ToJson(refreshed), JsonUtility.ToJson(session.Act(command)));
        Assert.AreEqual(0, refreshed.state.treasure.refreshesRemaining); Assert.IsFalse(refreshed.state.treasure.canRefresh);
        Assert.IsFalse(refreshed.state.treasure.relicChoices.Any(r => before.Contains(r.id) || r.id == Id(relics[6])));
        Assert.IsFalse(Tile.isResolved); Assert.AreEqual(1, refreshed.state.relics.Sum(r => r.amount));
        Assert.Throws<ArgumentException>(() => Act("run_refresh_treasure_relics"));
    }

    [Test] public void ExhaustedRefreshPoolDoesNotOfferARefreshOrConsumeTheBonus()
    {
        Open(); ConfigureBag(Relics(3)); Map.Snapshot.runState.abilityRelicChoiceRefreshCount = 2;
        Enter(); var state = Act("run_choose_treasure", 2);
        Assert.AreEqual(2, state.treasure.refreshesRemaining); Assert.IsFalse(state.treasure.canRefresh);
        Assert.IsFalse(state.legalActions.Any(a => a.op == "run_refresh_treasure_relics"));
        Assert.Throws<ArgumentException>(() => Act("run_refresh_treasure_relics"));
        Assert.AreEqual(state.revision, session.Observe().revision);
    }

    [Test] public void AllOwnedRelicsDisableTheBagWithoutGrantingAnyReward()
    {
        Open(); var relics = Relics(3); ConfigureBag(relics);
        foreach (var relic in relics) Map.Snapshot.runState.AddRelic(relic, 1);
        var state = Enter(); Assert.IsFalse(state.treasure.options[2].selectable); Assert.IsNotEmpty(state.treasure.options[2].unavailableReason);
        Assert.IsFalse(state.legalActions.Any(a => a.op == "run_choose_treasure" && a.choiceIndex == 2));
        Assert.IsEmpty(state.treasure.relicChoices); Assert.AreEqual(0, state.money); Assert.IsFalse(Tile.isResolved);
    }

    [Test] public void ObservationDoesNotRollOrChangeInventoryRandomStreamsOrTheUserSnapshot()
    {
        MapRunRuntime.RestoreSnapshot(new MapRunSnapshot { sceneName = "TreasureUserSnapshot", runState = new RunState { money = 987 } });
        Open(); var state = Enter();
        string json = JsonUtility.ToJson(state), rng = JsonUtility.ToJson(Random.state), mapRng = JsonUtility.ToJson(Map.PreviewWithRandom(() => Random.state));
        for (int i = 0; i < 5; i++) Assert.AreEqual(json, JsonUtility.ToJson(session.Observe()));
        Assert.AreEqual(rng, JsonUtility.ToJson(Random.state)); Assert.AreEqual(mapRng, JsonUtility.ToJson(Map.PreviewWithRandom(() => Random.state)));
        Act("run_choose_treasure", 0); Assert.AreEqual(987, MapRunRuntime.TryGetSnapshot("TreasureUserSnapshot", out var untouched) ? untouched.runState.money : -1);
    }

    [Test] public void InvalidOrStaleChoicesDoNotModifyTheAdventure()
    {
        Open(); var state = Enter(); var stale = Command("run_choose_treasure"); stale.revision--;
        Assert.Throws<ArgumentException>(() => session.Act(stale)); Assert.Throws<ArgumentException>(() => Act("run_choose_treasure", 99));
        Assert.Throws<ArgumentException>(() => Act("run_choose_treasure_relic", 0));
        Assert.AreEqual(0, session.Observe().money); Assert.AreEqual(state.revision, session.Observe().revision); Assert.IsFalse(Tile.isResolved);
    }

    [Test] public void TreasureResultDefersCalamityUntilItIsAcknowledged()
    {
        Open(); Map.Points.RestoreState(6, 0, 2); var state = Enter();
        Assert.AreEqual(2, state.cycle); Assert.IsEmpty(state.calamityChoices); Act("run_choose_treasure", 0);
        Assert.AreEqual("calamity", Act("run_continue_treasure").phase);
    }

    [Test] public void AbilityTreasureTableOverrideIsUsedForTheQuoteAndTheGrant()
    {
        Open(); var table = Temp<TreasureRewardTable_SO>(); Set(table, "minMoney", 73); Set(table, "maxMoney", 73);
        Map.Snapshot.runState.abilityTreasureRewardTableOverride = table;
        var state = Enter(); Assert.AreEqual(73, state.treasure.options[0].money);
        Assert.IsFalse(state.treasure.options[1].selectable); Assert.IsFalse(state.treasure.options[2].selectable);
        Assert.AreEqual(73, Act("run_choose_treasure", 0).money);
    }

    [Test] public void SpecialDungeonDirectRelicUsesTheSameSingleVisitFlow()
    {
        Open(); var relic = Relics(1)[0]; int hp = Map.Snapshot.runState.partyMembers[0].maxHP;
        Enter(tile => { tile.sourceSpecialDungeon = Temp<SpecialDungeon_SO>(); tile.specialDungeonTreasureOptions = new List<SpecialDungeonTreasureOptionEntry>
        { new SpecialDungeonTreasureOptionEntry { rewardType = SpecialDungeonTreasureRewardType.Relic, relicData = relic } }; });
        Assert.AreEqual("Relic", session.Observe().treasure.options.Single().kind);
        var state = Act("run_choose_treasure", 0); Assert.AreEqual("treasure_result", state.phase);
        Assert.AreEqual(1, Map.Snapshot.runState.GetRelicAmount(relic)); Assert.AreEqual(hp + 3, state.party.Single().maxHp);
        Assert.IsTrue(Tile.isResolved);
    }

    [Test] public void SpecialDungeonBagUsesItsConfiguredChoiceCount()
    {
        Open(); var relics = Relics(2); var table = RelicTable(relics);
        Enter(tile => { tile.sourceSpecialDungeon = Temp<SpecialDungeon_SO>(); tile.specialDungeonTreasureOptions = new List<SpecialDungeonTreasureOptionEntry>
        { new SpecialDungeonTreasureOptionEntry { rewardType = SpecialDungeonTreasureRewardType.RelicBag, relicTable = table, relicBagRarity = relics[0].rarity, relicBagChoiceCount = 2 } }; });
        var state = Act("run_choose_treasure", 0); Assert.AreEqual(2, state.treasure.relicChoices.Length);
        Assert.IsFalse(Tile.isResolved); Act("run_choose_treasure_relic", 0); Assert.IsTrue(Tile.isResolved);
    }

    [Test] public void SharedRewardServiceRejectsForeignRelicChoiceAndResolvedTiles()
    {
        var relic = Relics(1)[0]; var entry = new BattleSettlementLootEntry { relicData = relic, amount = 1 };
        var choices = new BattleSettlementRelicChoiceData { choices = new List<BattleSettlementLootEntry> { entry }, selectedChoice = new BattleSettlementLootEntry { relicData = relic } };
        var state = new RunState(); Assert.IsFalse(TreasureRewardService.ApplyRelicChoice(state, choices).success);
        Assert.AreEqual(0, state.GetRelicAmount(relic)); choices.selectedChoice = entry;
        var tile = new BoardTileData { tileType = MapTileType.Treasure, isResolved = true };
        Assert.IsFalse(TreasureRewardService.ApplyTileRelicChoice(state, tile, choices).success); Assert.AreEqual(0, state.GetRelicAmount(relic));
    }
}
