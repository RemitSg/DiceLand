using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class MapBattleTilePreviewServiceTests
{
    private readonly List<Object> createdObjects = new List<Object>();

    [TearDown]
    public void TearDown()
    {
        for (int i = createdObjects.Count - 1; i >= 0; i--)
        {
            if (createdObjects[i] != null)
                Object.DestroyImmediate(createdObjects[i]);
        }

        createdObjects.Clear();
    }

    [Test]
    public void BuildContext_FiltersUniqueAttributesInSourceOrderAndLimitsToThreeWithoutCountDependency()
    {
        LootAttribute_SO fire = CreateAttribute("fire", "火");
        LootAttribute_SO duplicateFire = CreateAttribute("fire", "烈火");
        LootAttribute_SO blankName = CreateAttribute("blank", "   ");
        LootAttribute_SO dark = CreateAttribute(string.Empty, "暗");
        LootAttribute_SO duplicateDark = CreateAttribute(string.Empty, "暗");
        LootAttribute_SO water = CreateAttribute("water", "水");
        LootAttribute_SO wind = CreateAttribute("wind", "风");
        EnemyGroup_SO group = CreateGroup(
            fire,
            duplicateFire,
            blankName,
            dark,
            duplicateDark,
            water,
            wind);
        BoardTileData tile = CreateHostileTile(group);

        tile.previewEnemyCount = 0;
        MapBattleTilePreviewContext withoutPreviewCount = MapBattleTilePreviewService.BuildContext(
            new RunState(), tile, 2, true);

        tile.previewEnemyCount = 999;
        MapBattleTilePreviewContext withPreviewCount = MapBattleTilePreviewService.BuildContext(
            new RunState(), tile, 2, true);

        CollectionAssert.AreEqual(new[] { "火", "暗", "水" }, withoutPreviewCount.attributeNames);
        CollectionAssert.AreEqual(withoutPreviewCount.attributeNames, withPreviewCount.attributeNames);
        Assert.AreEqual(6, withPreviewCount.dangerRating);
        Assert.AreEqual("进入战斗 · 预计 2 AP", withPreviewCount.confirmLabel);
        Assert.IsTrue(withPreviewCount.canPreview);
        Assert.IsTrue(withPreviewCount.canConfirm);
    }

    [Test]
    public void BuildContext_UsesDefaultAttributeLabelWhenGroupHasNoNamedAttributes()
    {
        EnemyGroup_SO group = CreateGroup(
            null,
            CreateAttribute("blank", "  "));

        MapBattleTilePreviewContext result = MapBattleTilePreviewService.BuildContext(
            new RunState(), CreateHostileTile(group), 2, true);

        CollectionAssert.AreEqual(new[] { "无明显倾向" }, result.attributeNames);
    }

    [Test]
    public void BuildContext_LockedBossCanPreviewButCannotConfirm()
    {
        RunState runState = new RunState { currentKeys = 1, requiredKeys = 3 };
        BoardTileData tile = new BoardTileData
        {
            tileType = MapTileType.Boss,
            previewDangerRating = 99
        };

        MapBattleTilePreviewContext result = MapBattleTilePreviewService.BuildContext(
            runState, tile, 2, true);

        Assert.IsTrue(result.canPreview);
        Assert.IsFalse(result.canConfirm);
        Assert.AreEqual(8, result.dangerRating);
        Assert.AreEqual("Boss 未解锁：1/3 钥匙", result.blockReason);
    }

    [Test]
    public void BuildContext_InsufficientMovementCannotConfirmAndReportsRequiredCost()
    {
        MapBattleTilePreviewContext result = MapBattleTilePreviewService.BuildContext(
            new RunState(), CreateHostileTile(), 3, false);

        Assert.IsTrue(result.canPreview);
        Assert.IsFalse(result.canConfirm);
        Assert.AreEqual("行动点不足，需要 3 点", result.blockReason);
    }

    [TestCase(MapTileType.Empty, false)]
    [TestCase(MapTileType.Merchant, false)]
    [TestCase(MapTileType.Battle, true)]
    [TestCase(MapTileType.Elite, true)]
    [TestCase(MapTileType.Boss, true)]
    public void BuildContext_PreviewsOnlyUnresolvedHostileTiles(MapTileType tileType, bool expectedCanPreview)
    {
        BoardTileData tile = new BoardTileData { tileType = tileType };

        MapBattleTilePreviewContext result = MapBattleTilePreviewService.BuildContext(
            new RunState(), tile, 2, true);

        Assert.AreEqual(expectedCanPreview, result.canPreview);
        Assert.AreEqual(expectedCanPreview, result.canConfirm);
    }

    [Test]
    public void BuildContext_DoesNotPreviewNullOrResolvedHostileTile()
    {
        MapBattleTilePreviewContext missing = MapBattleTilePreviewService.BuildContext(
            new RunState(), null, 2, true);
        MapBattleTilePreviewContext resolved = MapBattleTilePreviewService.BuildContext(
            new RunState(), new BoardTileData { tileType = MapTileType.Battle, isResolved = true }, 2, true);

        Assert.IsFalse(missing.canPreview);
        Assert.IsFalse(missing.canConfirm);
        Assert.IsFalse(resolved.canPreview);
        Assert.IsFalse(resolved.canConfirm);
    }

    [TestCase(MapBattleTilePreviewState.Hidden, MapBattleTilePreviewState.Entering)]
    [TestCase(MapBattleTilePreviewState.Entering, MapBattleTilePreviewState.Ready)]
    [TestCase(MapBattleTilePreviewState.Ready, MapBattleTilePreviewState.Committing)]
    [TestCase(MapBattleTilePreviewState.Ready, MapBattleTilePreviewState.Exiting)]
    [TestCase(MapBattleTilePreviewState.Exiting, MapBattleTilePreviewState.Hidden)]
    public void CanTransition_AllowsLegalTransitions(
        MapBattleTilePreviewState from,
        MapBattleTilePreviewState to)
    {
        Assert.IsTrue(MapBattleTilePreviewService.CanTransition(from, to));
    }

    [Test]
    public void CanTransition_RejectsEveryUnlistedTransition()
    {
        foreach (MapBattleTilePreviewState from in System.Enum.GetValues(typeof(MapBattleTilePreviewState)))
        {
            foreach (MapBattleTilePreviewState to in System.Enum.GetValues(typeof(MapBattleTilePreviewState)))
            {
                bool listed =
                    (from == MapBattleTilePreviewState.Hidden && to == MapBattleTilePreviewState.Entering) ||
                    (from == MapBattleTilePreviewState.Entering && to == MapBattleTilePreviewState.Ready) ||
                    (from == MapBattleTilePreviewState.Ready && to == MapBattleTilePreviewState.Committing) ||
                    (from == MapBattleTilePreviewState.Ready && to == MapBattleTilePreviewState.Exiting) ||
                    (from == MapBattleTilePreviewState.Exiting && to == MapBattleTilePreviewState.Hidden);

                if (!listed)
                    Assert.IsFalse(MapBattleTilePreviewService.CanTransition(from, to), $"{from} -> {to}");
            }
        }
    }

    private BoardTileData CreateHostileTile(EnemyGroup_SO group = null)
    {
        return new BoardTileData
        {
            tileType = MapTileType.Elite,
            previewDangerRating = 6,
            assignedEnemyGroup = group
        };
    }

    private EnemyGroup_SO CreateGroup(params LootAttribute_SO[] attributes)
    {
        EnemyGroup_SO group = ScriptableObject.CreateInstance<EnemyGroup_SO>();
        group.attributes = new List<LootAttribute_SO>(attributes);
        createdObjects.Add(group);
        return group;
    }

    private LootAttribute_SO CreateAttribute(string id, string name)
    {
        LootAttribute_SO attribute = ScriptableObject.CreateInstance<LootAttribute_SO>();
        attribute.attributeId = id;
        attribute.attributeName = name;
        createdObjects.Add(attribute);
        return attribute;
    }
}
