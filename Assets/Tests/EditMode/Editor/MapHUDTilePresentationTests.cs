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
