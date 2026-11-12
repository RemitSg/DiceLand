using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public sealed class MapEventInventoryReforgeTests
{
    private const string EventPath = "Assets/Data/Events/034 矮人铁匠/034 矮人铁匠.asset";
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private readonly List<Object> temporary = new List<Object>();
    private Random.State random;

    [SetUp] public void SetUp() => random = Random.state;

    [TearDown] public void TearDown()
    {
        foreach (Object item in temporary) Object.DestroyImmediate(item);
        temporary.Clear();
        Random.state = random;
    }

    [Test]
    public void AuthoredEventUsesThreeRealTablesAndHasAnIllustration()
    {
        var data = AssetDatabase.LoadAssetAtPath<MapEvent_SO>(EventPath);
        var pool = AssetDatabase.LoadAssetAtPath<MapEventPool_SO>("Assets/Data/Events/MapEventPool.asset");
        Assert.AreEqual(data, pool.GetValidEvents().Single(entry => entry.eventId == "034"));
        Assert.AreEqual("矮人铁匠", data.eventName);
        Assert.AreEqual(3, data.options.Count);
        Assert.Greater(data.options[0].relicTable.Relics.Count, 1);
        Assert.Greater(data.options[1].blueprintTable.Blueprints.Count, 1);
        Assert.Greater(data.options[2].lootReforgeTable.LootItems.Count, 1);
        Assert.IsTrue(data.options.All(option => option.resolveEvent && option.requiredMoney == 0 && option.requiredMoneyPercent == 0));
        Assert.IsNotNull(Resources.Load<Sprite>("EventUI/Scenes/034"));

        var catalog = RunSaveAssetCatalogBuilder.Build(new RunSaveAssetCatalogBuildRequest { eventPool = pool });
        foreach (LootItem_SO item in data.options[2].lootReforgeTable.LootItems.Where(item => item != null))
            Assert.AreSame(item, catalog.ResolveLoot(MainMenuRunSaveSystem.GetLootId(item)));
    }

    [TestCase(MapEventOptionBuiltInEffectType.ReforgeSelectedRelicSameRarity)]
    [TestCase(MapEventOptionBuiltInEffectType.ReforgeSelectedBlueprintSameRarity)]
    [TestCase(MapEventOptionBuiltInEffectType.ReforgeSelectedLootSameRarity)]
    public void TradesOneOwnedItemForADifferentItemOfTheSameRarityOnlyOnce(MapEventOptionBuiltInEffectType effect)
    {
        var state = new RunState { money = -12 };
        MapEvent_SO data = Create<MapEvent_SO>();
        var option = new MapEventOptionData { builtInEffectType = effect };
        data.options.Add(option);
        ScriptableObject source, replacement, otherRarity;
        System.Func<ScriptableObject, int> count;
        switch (effect)
        {
            case MapEventOptionBuiltInEffectType.ReforgeSelectedRelicSameRarity:
                var relic = Create<Relic_SO>(); relic.rarity = LootRarity.Rare;
                var otherRelic = Create<Relic_SO>(); otherRelic.rarity = LootRarity.Rare;
                var commonRelic = Create<Relic_SO>(); commonRelic.rarity = LootRarity.Common;
                option.relicTable = Create<RelicTable_SO>();
                SetList(option.relicTable, "relics", relic, otherRelic, commonRelic);
                state.AddRelic(relic, 2, processAcquireEffects: false);
                source = relic; replacement = otherRelic; otherRarity = commonRelic;
                count = item => state.GetRelicAmount((Relic_SO)item);
                break;
            case MapEventOptionBuiltInEffectType.ReforgeSelectedBlueprintSameRarity:
                var blueprint = Create<CraftingBlueprint_SO>(); blueprint.rarity = LootRarity.Rare;
                var otherBlueprint = Create<CraftingBlueprint_SO>(); otherBlueprint.rarity = LootRarity.Rare;
                var commonBlueprint = Create<CraftingBlueprint_SO>(); commonBlueprint.rarity = LootRarity.Common;
                option.blueprintTable = Create<BlueprintTable_SO>();
                SetList(option.blueprintTable, "blueprints", blueprint, otherBlueprint, commonBlueprint);
                state.AddBlueprint(blueprint, 2);
                source = blueprint; replacement = otherBlueprint; otherRarity = commonBlueprint;
                count = item => state.GetBlueprintAmount((CraftingBlueprint_SO)item);
                break;
            default:
                var loot = Create<LootItem_SO>(); loot.rarity = LootRarity.Rare;
                var otherLoot = Create<LootItem_SO>(); otherLoot.rarity = LootRarity.Rare;
                var commonLoot = Create<LootItem_SO>(); commonLoot.rarity = LootRarity.Common;
                option.lootReforgeTable = Create<LootItemTable_SO>();
                SetList(option.lootReforgeTable, "lootItems", loot, otherLoot, commonLoot);
                state.AddLoot(loot, 2);
                source = loot; replacement = otherLoot; otherRarity = commonLoot;
                count = item => state.GetLootAmount((LootItem_SO)item);
                break;
        }

        var tile = new BoardTileData { tileType = MapTileType.Event, assignedEvent = data };
        var request = new MapEventBuiltInEffectExecutionRequest { runState = state, currentEventTile = tile, optionData = option };
        CollectionAssert.AreEqual(new[] { source }, MapEventInventoryReforgeService.GetEligibleMaterials(state, option));
        Assert.IsFalse(MapEventOptionService.TryExecute(request, data, null, out _));
        request.selectedInventoryItem = replacement;
        Assert.IsFalse(MapEventOptionService.TryExecute(request, data, null, out _));
        Assert.AreEqual(2, count(source));
        Assert.IsFalse(tile.isResolved);

        request.selectedInventoryItem = source;
        Assert.IsTrue(MapEventOptionService.TryExecute(request, data, null, out var result), result.message);
        Assert.IsTrue(tile.isResolved);
        Assert.AreEqual(1, state.settlementStats.eventTilesCompleted);
        Assert.IsFalse(MapEventOptionService.TryExecute(request, data, null, out _));
        Assert.AreEqual(1, count(source));
        Assert.AreEqual(1, count(replacement));
        Assert.AreEqual(0, count(otherRarity));
        Assert.AreEqual(-12, state.money);
    }

    [Test]
    public void BackpackSelectionCanBeCancelledAndOnlyConfirmConsumesTheBlueprint()
    {
        var state = new RunState();
        var data = AssetDatabase.LoadAssetAtPath<MapEvent_SO>(EventPath);
        var table = data.options[1].blueprintTable.Blueprints;
        var source = table.First(item => item != null && table.Any(other => other != null && other != item && other.rarity == item.rarity));
        state.AddBlueprint(source);

        var host = new GameObject("Reforge event regression"); temporary.Add(host);
        var hud = host.AddComponent<MapHUD>(); hud.InitializeInventoryOnly(); hud.Refresh(state, null);
        var overlay = host.AddComponent<MapEventOverlay>(); overlay.ConfigureInventory(hud);
        var art = MapTreasureArt.Load(); overlay.ConfigureFonts(art.headingFont, art.bodyFont);
        var tile = new BoardTileData { tileType = MapTileType.Event, assignedEvent = data };
        overlay.Show(tile, data, (BoardTileData t, MapEvent_SO e, MapEventOptionData option, out string message, out string story, out MapEvent_SO next) =>
        {
            var selected = (ScriptableObject)typeof(MapEventOverlay).GetProperty("SelectedReforgeItem", Private).GetValue(overlay);
            bool success = MapEventOptionService.TryExecute(new MapEventBuiltInEffectExecutionRequest
                { runState = state, currentEventTile = t, optionData = option, selectedInventoryItem = selected }, e, null, out var result);
            message = result.message; story = result.resultContent; next = result.nextEventOverride;
            return success;
        }, (t, e, option) => new MapEventOverlay.EventOptionPreviewData
            { isSelectable = MapEventOptionService.BuildPreview(state, option, null).isSelectable });

        Button FindButton(string name) => host.GetComponentsInChildren<Button>(true).Single(button => button.name == name);
        var confirm = FindButton("ConfirmButton");
        confirm.onClick.Invoke();
        Assert.IsTrue(hud.IsInventoryVisible);
        Assert.IsTrue(FindButton("BlueprintTab").interactable);
        Assert.IsFalse(FindButton("RelicTab").interactable);
        hud.CloseInventoryPanel();
        Assert.IsFalse(hud.IsInventoryVisible);
        Assert.AreEqual(1, state.GetBlueprintAmount(source));

        confirm.onClick.Invoke();
        FindButton("Slot_0").onClick.Invoke();
        var select = FindButton("ActionButton");
        Assert.IsTrue(select.IsInteractable());
        select.onClick.Invoke();
        Assert.IsFalse(hud.IsInventoryVisible);
        Assert.AreEqual(1, state.GetBlueprintAmount(source));
        Assert.IsFalse(tile.isResolved);
        StringAssert.Contains(source.LocalizedName, host.GetComponentsInChildren<TMP_Text>(true).Single(t => t.name == "ConsequenceText").text);

        confirm.onClick.Invoke();
        Assert.IsTrue(tile.isResolved);
        Assert.AreEqual(0, state.GetBlueprintAmount(source));
        Assert.AreEqual(1, state.blueprintInventory.Count);
        Assert.AreEqual(source.rarity, state.blueprintInventory.Single().blueprintData.rarity);
    }

    private T Create<T>() where T : ScriptableObject
    {
        T item = ScriptableObject.CreateInstance<T>();
        temporary.Add(item);
        return item;
    }

    private static void SetList<T>(ScriptableObject asset, string fieldName, params T[] items) =>
        asset.GetType().GetField(fieldName, Private).SetValue(asset, items.ToList());
}
