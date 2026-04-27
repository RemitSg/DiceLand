using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

public class MapEventLootCostServiceTests
{
    private LootItem_SO loot;
    private LootItem_SO otherLoot;

    [TearDown]
    public void TearDown()
    {
        if (loot != null)
            Object.DestroyImmediate(loot);
        if (otherLoot != null)
            Object.DestroyImmediate(otherLoot);
    }

    [Test]
    public void BuildPreview_RandomLootDiscardShowsCachedDiscardDetail()
    {
        loot = CreateLoot("Amber");
        RunState runState = new RunState();
        runState.AddLoot(loot, 4);
        MapEventOptionData option = new MapEventOptionData
        {
            optionId = "Trade",
            builtInEffectType = MapEventOptionBuiltInEffectType.LoseOwnedRandomLoot,
            randomOwnedLootAmountMin = 2,
            randomOwnedLootAmountMax = 2
        };
        Dictionary<string, MapEventLootDiscardPreview> cache = new Dictionary<string, MapEventLootDiscardPreview>();

        MapEventLootCostPreviewData preview = MapEventLootCostService.BuildPreview(runState, option, cache);

        Assert.IsTrue(preview.isSelectable);
        Assert.AreEqual("将随机丢弃：Amber x2", preview.additionalDetail);
        Assert.AreEqual(1, cache.Count);
        Assert.AreSame(loot, cache["Trade::0"].lootItem);
        Assert.AreEqual(2, cache["Trade::0"].amount);
    }

    [Test]
    public void BuildPreview_SpecificLootMissingMarksOptionUnavailable()
    {
        loot = CreateLoot("Amber");
        RunState runState = new RunState();
        runState.AddLoot(loot, 1);
        MapEventOptionData option = new MapEventOptionData
        {
            builtInEffectType = MapEventOptionBuiltInEffectType.LoseSpecificLoot,
            lootRewards =
            {
                new MapEventLootRewardEntry
                {
                    lootItem = loot,
                    amountMin = 2,
                    amountMax = 2
                }
            }
        };

        MapEventLootCostPreviewData preview = MapEventLootCostService.BuildPreview(runState, option, null);

        Assert.IsFalse(preview.isSelectable);
        Assert.AreEqual("背包中缺少所需的指定战利品。", preview.unavailableReason);
        Assert.AreEqual("将失去：Amber x2", preview.additionalDetail);
    }

    [Test]
    public void TryExecuteRandomLootDiscard_ConsumesCachedPreviewAndClearsCache()
    {
        loot = CreateLoot("Amber");
        RunState runState = new RunState();
        runState.AddLoot(loot, 5);
        MapEventOptionData option = new MapEventOptionData { optionId = "Trade" };
        MapEventBuiltInEffectData effect = new MapEventBuiltInEffectData
        {
            effectType = MapEventOptionBuiltInEffectType.LoseOwnedRandomLoot,
            randomOwnedLootAmountMin = 2,
            randomOwnedLootAmountMax = 2
        };
        Dictionary<string, MapEventLootDiscardPreview> cache = new Dictionary<string, MapEventLootDiscardPreview>
        {
            ["Trade::3"] = new MapEventLootDiscardPreview { lootItem = loot, amount = 2 }
        };

        bool result = MapEventLootCostService.TryExecuteRandomLootDiscard(runState, option, 3, effect, cache, out string message);

        Assert.IsTrue(result);
        Assert.AreEqual(3, runState.GetLootAmount(loot));
        Assert.IsFalse(cache.ContainsKey("Trade::3"));
        Assert.AreEqual("失去战利品：Amber x2。", message);
    }

    [Test]
    public void TryExecuteSpecificLootDiscard_ValidatesBeforeConsumingAnything()
    {
        loot = CreateLoot("Amber");
        otherLoot = CreateLoot("Bone");
        RunState runState = new RunState();
        runState.AddLoot(loot, 2);
        runState.AddLoot(otherLoot, 1);
        MapEventBuiltInEffectData effect = new MapEventBuiltInEffectData
        {
            effectType = MapEventOptionBuiltInEffectType.LoseSpecificLoot,
            lootRewards =
            {
                new MapEventLootRewardEntry { lootItem = loot, amountMin = 2, amountMax = 2 },
                new MapEventLootRewardEntry { lootItem = otherLoot, amountMin = 2, amountMax = 2 }
            }
        };

        bool result = MapEventLootCostService.TryExecuteSpecificLootDiscard(runState, effect, out string message);

        Assert.IsFalse(result);
        Assert.AreEqual(2, runState.GetLootAmount(loot));
        Assert.AreEqual(1, runState.GetLootAmount(otherLoot));
        Assert.AreEqual("背包中缺少 Bone x2。", message);
    }

    private static LootItem_SO CreateLoot(string lootName)
    {
        LootItem_SO item = ScriptableObject.CreateInstance<LootItem_SO>();
        item.lootName = lootName;
        return item;
    }
}
