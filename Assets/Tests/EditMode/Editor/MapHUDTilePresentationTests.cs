using NUnit.Framework;
using TMPro;
using UnityEngine;

public class MapHUDTilePresentationTests
{
    [Test]
    public void ShowTile_DisplaysTreasureTileLabelAndDescription()
    {
        GameObject go = new GameObject("MapHUD");
        try
        {
            MapHUD hud = go.AddComponent<MapHUD>();
            BoardTileData tile = new BoardTileData
            {
                coord = new Vector2Int(1, 6),
                tileType = MapTileType.Treasure,
                isVisited = false,
                isResolved = false
            };

            hud.ShowTile(tile, 2);

            TextMeshProUGUI title = FindText(go.transform, "TileTitle");
            TextMeshProUGUI body = FindText(go.transform, "TileBody");
            Assert.AreEqual("[1,6] 宝藏", title.text);
            Assert.That(body.text, Does.Contain("未探索 | 未解决"));
            Assert.That(body.text, Does.Contain("移动消耗 2 行动点"));
            Assert.That(body.text, Does.Contain("宝藏格"));
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void ShowTile_SpecialDungeonExitDisplaysExitLabel()
    {
        GameObject go = new GameObject("MapHUD");
        try
        {
            MapHUD hud = go.AddComponent<MapHUD>();
            BoardTileData tile = new BoardTileData
            {
                coord = new Vector2Int(0, 1),
                tileType = MapTileType.SpecialDungeonExit
            };

            hud.ShowTile(tile, 0);

            TextMeshProUGUI title = FindText(go.transform, "TileTitle");
            TextMeshProUGUI body = FindText(go.transform, "TileBody");
            Assert.AreEqual("[0,1] 副本出口", title.text);
            Assert.That(body.text, Does.Contain("离开副本"));
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void ShowTile_SpecialDungeonBattleHidesRegionLabelButKeepsDanger()
    {
        GameObject go = new GameObject("MapHUD");
        SpecialDungeon_SO dungeon = ScriptableObject.CreateInstance<SpecialDungeon_SO>();
        try
        {
            MapHUD hud = go.AddComponent<MapHUD>();
            BoardTileData tile = new BoardTileData
            {
                coord = new Vector2Int(2, 1),
                tileType = MapTileType.Battle,
                dangerBand = MapDangerBand.Core,
                previewDangerRating = 6,
                sourceSpecialDungeon = dungeon
            };

            hud.ShowTile(tile, 0);

            TextMeshProUGUI body = FindText(go.transform, "TileBody");
            Assert.That(body.text, Does.Contain(MapDangerRatingService.GetRatingPhrase(6)));
            Assert.That(body.text, Does.Contain(MapDangerRatingService.GetRatingColorHex(6)));
            Assert.That(body.text, Does.Contain("危险度"));
            Assert.That(body.text, Does.Not.Contain("区域"));
            Assert.That(body.text, Does.Not.Contain("核心"));
        }
        finally
        {
            Object.DestroyImmediate(dungeon);
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void GetTileIcon_SpecialDungeonExitFallsBackToDefaultSprite()
    {
        GameObject go = new GameObject("BoardManager");
        try
        {
            BoardManager boardManager = go.AddComponent<BoardManager>();
            boardManager.Initialize();

            Sprite icon = boardManager.GetTileIcon(new BoardTileData
            {
                tileType = MapTileType.SpecialDungeonExit
            });

            Assert.IsNotNull(icon);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void ResolveTile_SpecialDungeonExitReturnsExitMessage()
    {
        GameObject go = new GameObject("TileResolver");
        try
        {
            TileResolver resolver = go.AddComponent<TileResolver>();
            BoardTileData tile = new BoardTileData
            {
                tileType = MapTileType.SpecialDungeonExit
            };

            string message = resolver.ResolveTile(tile, new RunState());

            Assert.IsTrue(tile.isVisited);
            Assert.That(message, Does.Contain("离开特殊副本"));
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    private static TextMeshProUGUI FindText(Transform root, string name)
    {
        TextMeshProUGUI[] texts = root.GetComponentsInChildren<TextMeshProUGUI>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            if (texts[i].name == name)
                return texts[i];
        }

        Assert.Fail($"Missing text named {name}.");
        return null;
    }
}
