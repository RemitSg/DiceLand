using NUnit.Framework;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class SpecialDungeonDataModelTests
{
    [Test]
    public void GetValidEntries_FiltersNullEntriesAndUnsupportedEmptyTiles()
    {
        SpecialDungeon_SO dungeon = ScriptableObject.CreateInstance<SpecialDungeon_SO>();
        dungeon.entries.Add(null);
        dungeon.entries.Add(new SpecialDungeonTileEntry { tileType = MapTileType.Empty });
        dungeon.entries.Add(new SpecialDungeonTileEntry { tileType = MapTileType.Battle });

        Assert.That(dungeon.GetValidEntries(), Has.Count.EqualTo(1));
        Assert.AreEqual(MapTileType.Battle, dungeon.GetValidEntries()[0].tileType);

        Object.DestroyImmediate(dungeon);
    }

    [Test]
    public void TreasureOption_IsConfiguredForMoneyBlueprintRelicAndRelicBag()
    {
        SpecialDungeonTreasureOptionEntry money = new SpecialDungeonTreasureOptionEntry
        {
            rewardType = SpecialDungeonTreasureRewardType.Money,
            moneyAmount = 25
        };
        CraftingBlueprint_SO blueprint = ScriptableObject.CreateInstance<CraftingBlueprint_SO>();
        Relic_SO relic = ScriptableObject.CreateInstance<Relic_SO>();
        RelicTable_SO relicTable = ScriptableObject.CreateInstance<RelicTable_SO>();
        SetRelics(relicTable, new List<Relic_SO> { relic });

        Assert.IsTrue(money.IsConfigured);
        Assert.IsTrue(new SpecialDungeonTreasureOptionEntry
        {
            rewardType = SpecialDungeonTreasureRewardType.Blueprint,
            blueprintData = blueprint
        }.IsConfigured);
        Assert.IsTrue(new SpecialDungeonTreasureOptionEntry
        {
            rewardType = SpecialDungeonTreasureRewardType.Relic,
            relicData = relic
        }.IsConfigured);
        Assert.IsTrue(new SpecialDungeonTreasureOptionEntry
        {
            rewardType = SpecialDungeonTreasureRewardType.RelicBag,
            relicTable = relicTable,
            relicBagChoiceCount = 1
        }.IsConfigured);

        Object.DestroyImmediate(relicTable);
        Object.DestroyImmediate(relic);
        Object.DestroyImmediate(blueprint);
    }

    private static void SetRelics(RelicTable_SO relicTable, List<Relic_SO> relics)
    {
        FieldInfo field = typeof(RelicTable_SO).GetField("relics", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field);
        field.SetValue(relicTable, relics);
    }
}
