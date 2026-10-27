using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class RunRestReforgeServiceTests
{
    private readonly List<UnityEngine.Object> owned = new List<UnityEngine.Object>();
    private T Make<T>(string name = "") where T : ScriptableObject { var v = ScriptableObject.CreateInstance<T>(); v.name = name; owned.Add(v); return v; }
    [TearDown] public void TearDown() { foreach (var v in owned) UnityEngine.Object.DestroyImmediate(v); owned.Clear(); }
    private LootItem_SO Loot(string name, LootRarity rarity, LootAttribute_SO attribute)
    { var v = Make<LootItem_SO>(name); v.rarity = rarity; v.attribute = attribute; return v; }
    private Relic_SO Relic(string name, LootRarity rarity) { var v = Make<Relic_SO>(name); v.rarity = rarity; return v; }
    private RunReforgeDefinition Definition(LootItem_SO[] loot = null, Relic_SO[] relics = null)
    {
        var lootTable = Make<LootItemTable_SO>(); var relicTable = Make<RelicTable_SO>();
        typeof(LootItemTable_SO).GetField("lootItems", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(lootTable, new List<LootItem_SO>(loot ?? Array.Empty<LootItem_SO>()));
        typeof(RelicTable_SO).GetField("relics", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(relicTable, new List<Relic_SO>(relics ?? Array.Empty<Relic_SO>()));
        return new RunReforgeDefinition { lootTable = lootTable, relicTable = relicTable };
    }

    [Test] public void SameStackRequiresTwoItemsAndLegendaryRelicsCannotBeUpgraded()
    {
        var attribute = Make<LootAttribute_SO>(); var loot = Loot("A", LootRarity.Common, attribute); var relic = Relic("R", LootRarity.Common); var state = new RunState();
        state.AddLoot(loot,1); state.AddRelic(relic);
        Assert.IsFalse(RunRestService.ValidateLootReforge(state, loot, loot, attribute, out _));
        Assert.IsFalse(RunRestService.ValidateRelicReforge(state, relic, relic, out _));
        state.AddLoot(loot,1); state.AddRelic(relic);
        Assert.IsTrue(RunRestService.ValidateLootReforge(state, loot, loot, attribute, out _));
        Assert.IsTrue(RunRestService.ValidateRelicReforge(state, relic, relic, out _));
        relic.rarity = LootRarity.Legendary;
        Assert.IsFalse(RunRestService.ValidateRelicReforge(state, relic, relic, out _));
    }

    [Test] public void LootPoolPreservesMatchingMaterialsAndFiltersRarityAndAttribute()
    {
        var blood = Make<LootAttribute_SO>(); var fire = Make<LootAttribute_SO>();
        var a = Loot("A", LootRarity.Common, blood); var b = Loot("B", LootRarity.Common, blood);
        var definition = Definition(new[] { a, b, Loot("C", LootRarity.Common, fire), Loot("D", LootRarity.Rare, blood) });
        CollectionAssert.AreEqual(new[] { a, b }, RunRestService.LootReforgeCandidates(definition, a, b, blood));
        Assert.AreSame(RunRestService.RollLootReforge(definition, a, b, blood, new System.Random(5)), RunRestService.RollLootReforge(definition, a, b, blood, new System.Random(5)));
        Assert.IsEmpty(RunRestService.LootReforgeCandidates(definition, a, b, Make<LootAttribute_SO>()));
    }

    [Test] public void RelicRollUsesTheNearestAvailableHigherTierAndReturnsNullIfExhausted()
    {
        var common = Relic("Common", LootRarity.Common); var rare = Relic("Rare", LootRarity.Rare); var legendary = Relic("Legendary", LootRarity.Legendary);
        var definition = Definition(relics: new[] { rare, legendary });
        Assert.AreSame(rare, RunRestService.RollRelicReforge(definition, common, common, new System.Random(1)));
        Assert.AreSame(legendary, RunRestService.RollRelicReforge(definition, rare, rare, new System.Random(1)));
        Assert.IsNull(RunRestService.RollRelicReforge(definition, legendary, legendary, new System.Random(1)));
    }

    [Test] public void FormalUiAndSharedServiceRollTheSameSelectionAndSeed()
    {
        var blood = Make<LootAttribute_SO>("Blood"); var a = Loot("A", LootRarity.Common, blood); var b = Loot("B", LootRarity.Common, blood);
        var definition = Definition(new[] { a, b });
        var go = new GameObject("ReforgeUiDataTest"); go.SetActive(false); go.AddComponent<BoxCollider2D>(); owned.Add(go);
        var ui = go.AddComponent<RestSceneReforgeInteractable>();
        Action<string,object> set = (name,value) => typeof(RestSceneReforgeInteractable).GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(ui,value);
        set("lootReforgeTable",definition.lootTable); set("selectedLootA",a); set("selectedLootB",b); set("selectedLootAttribute",blood); set("successfulReforgeCount",1);
        var method = typeof(RestSceneReforgeInteractable).GetMethod("RollLootReforgeResult",BindingFlags.Instance|BindingFlags.NonPublic);
        var expected = RunRestService.RollLootReforge(definition,a,b,blood,RunRestService.ReforgeRandom(1,0,RestSceneRuntime.SourceCoord,RestSceneRuntime.ReturnSceneName,a,b,blood,null,null,"LootReforge"));
        Assert.AreSame(expected, method.Invoke(ui,null));
    }

    [Test] public void ReforgeLimitIncludesRelicBonusesAndTheSuccessfulCountIsBounded()
    {
        var relic = Relic("More",LootRarity.Legendary);
        relic.builtInEffects.Add(new Relic_SO.RelicBuiltInEffectEntry { trigger = Relic_SO.RelicTrigger.OnRestReforgeLimitCalculated, effectType = Relic_SO.RelicBuiltInEffectType.IncreaseRestReforgeLimit, amount = 1 });
        var state = new RunState(); state.AddRelic(relic);
        Assert.AreEqual(3,RunRestService.ReforgeLimit(state)); Assert.AreEqual(2,RunRestService.ReforgeLimit(null));
        Assert.AreEqual(3,RunRestService.AdvanceReforgeCount(2,3)); Assert.AreEqual(3,RunRestService.AdvanceReforgeCount(3,3));
    }
}
