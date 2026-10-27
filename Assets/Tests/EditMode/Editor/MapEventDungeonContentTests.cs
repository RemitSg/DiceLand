using NUnit.Framework;
using UnityEditor;

public sealed class MapEventDungeonContentTests
{
    [TestCase("Assets/Data/Events/026 反复出现的门/026 反复出现的门.asset", 0, "反复出现的门")]
    [TestCase("Assets/Data/Events/033 幸运游戏（其二）/033 幸运游戏（其二）.asset", 1, "幸运迷宫")]
    public void AuthoredDungeonEntranceUsesItsExistingDungeon(string path, int index, string dungeonName)
    {
        var ev = AssetDatabase.LoadAssetAtPath<MapEvent_SO>(path);
        Assert.IsNotNull(ev);
        MapEventOptionData option = ev.GetValidOptions()[index];
        Assert.AreEqual(MapEventOptionBuiltInEffectType.StartSpecialDungeon, option.builtInEffectType);
        Assert.IsNotNull(option.specialDungeon);
        Assert.AreEqual(dungeonName, option.specialDungeon.DisplayName);
        Assert.IsNotEmpty(option.specialDungeon.GetValidEntries());
    }

    [Test]
    public void DoorRewardBagUsesTheFormalTreasureTableWithoutChangingItsRarityOrCount()
    {
        var dungeon = AssetDatabase.LoadAssetAtPath<SpecialDungeon_SO>("Assets/Data/Events/026 反复出现的门/026 反复出现的门_特殊副本.asset");
        var table = UnityEngine.Resources.Load<TreasureRewardTable_SO>("DefaultTreasureRewardTable");
        var entries = dungeon.GetValidEntries(); var bag = entries[entries.Count - 1].treasureOptions[0];
        Assert.IsTrue(bag.IsConfigured);
        Assert.AreSame(table.RelicTable, bag.relicTable);
        Assert.AreEqual(LootRarity.Uncommon, bag.relicBagRarity); Assert.AreEqual(1, bag.relicBagChoiceCount);
    }
}
