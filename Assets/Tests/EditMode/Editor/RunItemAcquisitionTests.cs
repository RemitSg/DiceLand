using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

public sealed class RunItemAcquisitionTests
{
    private readonly List<Object> owned = new List<Object>();
    private T Make<T>() where T : ScriptableObject { T value = ScriptableObject.CreateInstance<T>(); owned.Add(value); return value; }
    [TearDown] public void TearDown() { foreach (Object value in owned) Object.DestroyImmediate(value); owned.Clear(); }

    [Test]
    public void GrantsAreCommittedBeforeDisplayAndAcknowledgementDoesNotGrantAgain()
    {
        var state = new RunState();
        Relic_SO relic = Make<Relic_SO>(); CraftingBlueprint_SO blueprint = Make<CraftingBlueprint_SO>();
        state.AddRelic(relic, 2); state.AddBlueprint(blueprint, 3);
        Assert.AreEqual(2, state.GetRelicAmount(relic)); Assert.AreEqual(3, state.GetBlueprintAmount(blueprint));
        Assert.IsTrue(state.ItemAcquisitions.TryGetNext(out RunItemAcquisitionBatch first));
        Assert.AreSame(relic, first.Item); Assert.AreEqual(2, first.Amount);
        Assert.AreEqual(1, state.ItemAcquisitions.CountAfter(first));
        state.ItemAcquisitions.Acknowledge(first); state.ItemAcquisitions.Acknowledge(first);
        Assert.IsTrue(state.ItemAcquisitions.TryGetNext(out RunItemAcquisitionBatch second));
        Assert.AreSame(blueprint, second.Item); Assert.AreEqual(3, second.Amount);
        state.ItemAcquisitions.Acknowledge(second);
        Assert.AreEqual(0, state.ItemAcquisitions.PendingCount);
        Assert.AreEqual(2, state.GetRelicAmount(relic)); Assert.AreEqual(3, state.GetBlueprintAmount(blueprint));
    }

    [Test]
    public void ConsecutiveStacksBatchButNewGrantsCannotBeAcknowledgedByAnOldOpenPage()
    {
        var state = new RunState(); Relic_SO relic = Make<Relic_SO>();
        state.AddRelic(relic, 2); state.AddRelic(relic, 3);
        state.ItemAcquisitions.TryGetNext(out RunItemAcquisitionBatch showing);
        Assert.AreEqual(5, showing.Amount);
        state.AddRelic(relic);
        Assert.AreEqual(1, state.ItemAcquisitions.CountAfter(showing));
        state.ItemAcquisitions.Acknowledge(showing);
        Assert.IsTrue(state.ItemAcquisitions.TryGetNext(out RunItemAcquisitionBatch next));
        Assert.AreEqual(1, next.Amount); Assert.AreEqual(6, state.GetRelicAmount(relic));
    }

    [Test]
    public void SnapshotAcknowledgementsAreSharedButHypotheticalNewGrantsAreIsolated()
    {
        var state = new RunState(); Relic_SO relic = Make<Relic_SO>();
        state.AddRelic(relic);
        RunState snapshot = state.Clone();
        snapshot.AddRelic(relic, 2);
        Assert.AreEqual(1, state.GetRelicAmount(relic)); Assert.AreEqual(3, snapshot.GetRelicAmount(relic));
        Assert.AreEqual(1, state.ItemAcquisitions.PendingCount);
        state.ItemAcquisitions.TryGetNext(out RunItemAcquisitionBatch original);
        state.ItemAcquisitions.Acknowledge(original);
        Assert.IsTrue(snapshot.ItemAcquisitions.TryGetNext(out RunItemAcquisitionBatch fresh));
        Assert.AreEqual(2, fresh.Amount);
    }

    [Test]
    public void ExistingSaveFieldsRemainSerializableWithoutReannouncingLoadedInventory()
    {
        var state = new RunState(); Relic_SO relic = Make<Relic_SO>(); state.AddRelic(relic);
        string json = JsonUtility.ToJson(state);
        Assert.That(json, Does.Not.Contain("itemAcquisitions"));
        RunState restored = JsonUtility.FromJson<RunState>(json);
        Assert.AreEqual(1, restored.GetRelicAmount(relic));
        Assert.AreEqual(0, restored.ItemAcquisitions.PendingCount);
    }

    [Test]
    public void InvalidGrantsAndInventoryRestorationDoNotCreateReceipts()
    {
        var state = new RunState(); Relic_SO relic = Make<Relic_SO>(); CraftingBlueprint_SO blueprint = Make<CraftingBlueprint_SO>();
        state.AddRelic(null); state.AddBlueprint(null); state.AddRelic(relic, 0); state.AddBlueprint(blueprint, -1);
        state.AddRelic(relic, 1, false); state.AddPreviewedBlueprint(blueprint, 2);
        Assert.AreEqual(0, state.ItemAcquisitions.PendingCount);
        Assert.AreEqual(1, state.GetRelicAmount(relic)); Assert.AreEqual(2, state.GetBlueprintAmount(blueprint));
    }

    [Test]
    public void AssetIdentityIsPreservedEvenForIdenticalNamesAndIds()
    {
        var state = new RunState(); Relic_SO a = Make<Relic_SO>(), b = Make<Relic_SO>();
        a.relicName = b.relicName = "同名遗物";
        state.AddRelic(a); state.AddRelic(b);
        state.ItemAcquisitions.TryGetNext(out RunItemAcquisitionBatch first);
        Assert.AreSame(a, first.Item); Assert.AreEqual(1, state.ItemAcquisitions.CountAfter(first));
        state.ItemAcquisitions.Acknowledge(first);
        state.ItemAcquisitions.TryGetNext(out RunItemAcquisitionBatch second); Assert.AreSame(b, second.Item);
    }

    [Test]
    public void TreasureBlueprintsAndChosenRelicsSkipOnlyTheirOwnAlreadyReadRewards()
    {
        var state = new RunState(); CraftingBlueprint_SO blueprint = Make<CraftingBlueprint_SO>();
        TreasureRewardResult result = TreasureRewardService.ApplyChoice(state, null,
            new TreasureRewardChoice { rewardType = TreasureRewardType.Knowledge, blueprint = blueprint, isAvailable = true }, 17);
        Assert.IsTrue(result.success); Assert.AreEqual(1, state.GetBlueprintAmount(blueprint));
        Relic_SO parent = Make<Relic_SO>(), bonus = Make<Relic_SO>();
        RelicTable_SO table = Make<RelicTable_SO>();
        typeof(RelicTable_SO).GetField("relics", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(table, new List<Relic_SO> { bonus });
        parent.builtInEffects.Add(new Relic_SO.RelicBuiltInEffectEntry
        {
            trigger = Relic_SO.RelicTrigger.OnRelicAcquired,
            effectType = Relic_SO.RelicBuiltInEffectType.GrantRandomRelicsOnAcquire,
            amount = 1, relicTable = table
        });
        var selected = new BattleSettlementLootEntry { relicData = parent, amount = 1 };
        var choice = new BattleSettlementRelicChoiceData(); choice.choices.Add(selected); choice.selectedChoice = selected;
        Assert.IsTrue(TreasureRewardService.ApplyRelicChoice(state, choice).success);
        Assert.AreEqual(1, state.GetRelicAmount(parent)); Assert.AreEqual(1, state.GetRelicAmount(bonus));
        Assert.IsTrue(state.ItemAcquisitions.TryGetNext(out RunItemAcquisitionBatch gift));
        Assert.AreSame(bonus, gift.Item); Assert.AreEqual(1, state.ItemAcquisitions.PendingCount);
    }

    [Test]
    public void BattleChoiceDoesNotHideAnUnseenDropOfTheSameRelic()
    {
        Relic_SO relic = Make<Relic_SO>();
        var encounter = new BattleEncounterData { wasVictory = true };
        encounter.resolvedRelics.Add(new RunRelicState { relicData = relic, amount = 2 });
        var entry = new BattleSettlementLootEntry { relicData = relic, amount = 1 };
        var choice = new BattleSettlementRelicChoiceData(); choice.choices.Add(entry); choice.selectedChoice = entry;
        Assert.IsTrue(BattleRelicChoiceSelectionService.TryApply(encounter, choice, out _));
        Assert.IsTrue(BattleRelicChoiceSelectionService.TryApply(encounter, choice, out _));
        var snapshot = new MapRunSnapshot { runState = new RunState() };
        MapRunRuntime.ApplyEncounterResult(snapshot, encounter);
        Assert.AreEqual(3, snapshot.runState.GetRelicAmount(relic));
        snapshot.runState.ItemAcquisitions.TryGetNext(out RunItemAcquisitionBatch unknown);
        Assert.AreSame(relic, unknown.Item); Assert.AreEqual(2, unknown.Amount);
    }

    [Test]
    public void EventRewardsCreateReceiptsAndPendingVictoryBlueprintsAreNotLost()
    {
        Relic_SO relic = Make<Relic_SO>();
        var state = new RunState();
        var effect = new MapEventBuiltInEffectData { effectType = MapEventOptionBuiltInEffectType.GainRelics };
        effect.relicRewards.Add(new MapEventRelicRewardEntry { relicData = relic, amount = 2 });
        Assert.IsTrue(MapEventRewardService.TryExecuteRelicReward(state, effect, out _));
        state.ItemAcquisitions.TryGetNext(out RunItemAcquisitionBatch reward);
        Assert.AreSame(relic, reward.Item); Assert.AreEqual(2, reward.Amount);
        CraftingBlueprint_SO blueprint = Make<CraftingBlueprint_SO>();
        var encounter = new BattleEncounterData { wasVictory = true };
        encounter.pendingVictoryBlueprintRewards.Add(new RunBlueprintState { blueprintData = blueprint, amount = 2 });
        var snapshot = new MapRunSnapshot { runState = new RunState() };
        MapRunRuntime.ApplyEncounterResult(snapshot, encounter);
        snapshot.runState.ItemAcquisitions.TryGetNext(out RunItemAcquisitionBatch deferred);
        Assert.AreSame(blueprint, deferred.Item); Assert.AreEqual(2, deferred.Amount);
    }
}
