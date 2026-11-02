using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class MapForestHudLayoutTests
{
    private GameObject host;
    private MapHUD hud;

    [SetUp]
    public void SetUp()
    {
        host = new GameObject("MapForestLayoutTest");
        hud = host.AddComponent<MapHUD>();
        hud.Initialize();
    }

    [TearDown]
    public void TearDown() { Object.DestroyImmediate(host); }

    [TestCase(2340, 1080, 0)]
    [TestCase(1920, 1080, 0)]
    [TestCase(1600, 1200, 0)]
    [TestCase(2400, 1080, 90)]
    public void ResourceCellsStaySeparateAndInsideSafeArea(int width, int height, int sideInset)
    {
        Canvas canvas = Layout(width, height, sideInset);
        Rect keys = Bounds("KeysCell"), difficulty = Bounds("DifficultyCell");
        Assert.IsFalse(keys.Overlaps(difficulty), "Difficulty must not cover the key cell.");
        AssertContains(difficulty, Bounds("Difficulty"));
        AssertContains(keys, Bounds("Keys"));
        Rect screen = Bounds((RectTransform)canvas.transform);
        Rect resources = Bounds("TopLeftPanel");
        Rect detail = Bounds("TilePanel");
        AssertContains(screen, resources);
        AssertContains(screen, detail);
        float safeLeft = -1080f * width / height * 0.5f + sideInset * 1080f / height;
        Assert.GreaterOrEqual(resources.xMin, safeLeft - 0.01f);
    }

    [TestCase("Easy", "简单")]
    [TestCase("Medium", "普通")]
    [TestCase("Hard", "困难")]
    [TestCase("Extreme", "极难")]
    public void DifficultyUsesChineseLabelAndKeepsKeys(string difficulty, string expected)
    {
        hud.Refresh(new RunState { difficultyName = difficulty, currentKeys = 1, requiredKeys = 2 }, null);
        Assert.AreEqual(expected, Text("Difficulty").text);
        Assert.AreEqual("1/2", Text("Keys").text);
    }

    [TestCase("ActionPointLabel")]
    [TestCase("ActionPoints")]
    [TestCase("CalamityLabel")]
    [TestCase("Calamity")]
    [TestCase("MoneyLabel")]
    [TestCase("Keys")]
    [TestCase("TileTitle")]
    [TestCase("TileCoordinates")]
    [TestCase("TileExplored")]
    [TestCase("TileResolved")]
    [TestCase("MoveCostLabel")]
    [TestCase("TileMoveCost")]
    public void ProductionFontKeepsLabelsVisibleWithinTheirBoxes(string name)
    {
        TMP_FontAsset font = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            UnityEditor.AssetDatabase.GUIDToAssetPath("c6e7143cd2df45b45a784ef8c61a210f"));
        Assert.IsNotNull(font);
        hud.ConfigureFonts(font, font, font);
        hud.Refresh(new RunState { difficultyName = "Easy", currentKeys = 1, requiredKeys = 2 }, null);
        hud.ShowTile(new BoardTileData { coord = new Vector2Int(3, 5), tileType = MapTileType.Empty, isVisited = true, isResolved = true }, 0);
        Layout(2340, 1080, 0);
        TextMeshProUGUI text = Text(name);
        text.ForceMeshUpdate(true, true);
        Assert.IsFalse(text.isTextTruncated, name + " must not disappear because the font line metrics exceed the box height.");
        Assert.Greater(text.textInfo.characterInfo.Take(text.textInfo.characterCount).Count(c => c.isVisible && c.character != '\u2026'), 0);
    }

    [Test]
    public void SelectionReplacesStatusCostAndDescriptionWithoutStaleValues()
    {
        hud.ShowTile(new BoardTileData { coord = new Vector2Int(3, 5), tileType = MapTileType.Empty, isVisited = true, isResolved = true }, 0);
        Assert.AreEqual("已探索", Text("TileExplored").text);
        Assert.AreEqual("已解决", Text("TileResolved").text);
        hud.ShowTile(new BoardTileData { coord = new Vector2Int(4, 5), tileType = MapTileType.Treasure }, 2);
        Assert.AreEqual("[4,5]", Text("TileCoordinates").text);
        Assert.AreEqual("未探索", Text("TileExplored").text);
        Assert.AreEqual("未解决", Text("TileResolved").text);
        Assert.AreEqual("2", Text("TileMoveCost").text);
        Assert.That(Text("TileBody").text, Does.Not.Contain("无特殊效果"));
    }

    [Test]
    public void NoSelectionHidesBadgesAndCost()
    {
        hud.ShowTile(new BoardTileData { tileType = MapTileType.Rest }, 1);
        hud.ShowTile(null);
        Assert.IsFalse(Find("ExploredBadge").gameObject.activeSelf);
        Assert.IsFalse(Find("ResolvedBadge").gameObject.activeSelf);
        Assert.IsFalse(Find("MoveCostStrip").gameObject.activeSelf);
        Assert.That(Text("TileBody").text, Does.Contain("点击任意格子"));
    }

    [Test]
    public void LargePartyFitsAboveFooterAndCanScrollToLaterMembers()
    {
        Layout(2340, 1080, 0);
        RunState state = new RunState();
        for (int i = 0; i < 6; i++) state.partyMembers.Add(new RunPartyMemberState { memberId = "Member" + i, maxHP = 17, currentHP = 17 });
        hud.Refresh(state, null);
        Layout(2340, 1080, 0);
        ScrollRect scroll = Find("PartyListViewport").GetComponent<ScrollRect>();
        Assert.Greater(scroll.content.rect.height, scroll.viewport.rect.height);
        Assert.IsTrue(scroll.vertical);
        Assert.Greater(Bounds("PartyPanel").yMin, Bounds("MapInteractionHint").yMax);
        Assert.IsFalse(Bounds("HpText").Overlaps(Bounds("Name")));
    }

    [Test]
    public void LongHostileDetailsRemainInScrollableDescription()
    {
        Layout(2340, 1080, 0);
        string tag = string.Concat(Enumerable.Repeat("森林守卫与远处遗迹 ", 30));
        hud.ShowTile(new BoardTileData { tileType = MapTileType.Battle, previewTag = tag, previewDangerRating = 6, dangerBand = MapDangerBand.Outer }, 1, true);
        Assert.That(Text("TileBody").text, Does.Contain(tag));
        Assert.That(Text("TileBody").text, Does.Contain("危险度"));
        Assert.That(Text("TileBody").text, Does.Contain("区域"));
        ScrollRect scroll = Find("TileDescriptionViewport").GetComponent<ScrollRect>();
        Assert.Greater(scroll.content.rect.height, scroll.viewport.rect.height);
        AssertContains(Bounds("TilePanel"), Bounds("TileDescriptionViewport"));
    }

    [Test]
    public void PackagedArtProvidesEveryTimeOfDayAndSixteenSprites()
    {
        MapForestArt.Reload();
        Assert.AreEqual(16, Resources.LoadAll<Sprite>("MapUI/MapAtlas").Length);
        foreach (MapBackgroundPhase phase in System.Enum.GetValues(typeof(MapBackgroundPhase)))
            Assert.IsNotNull(MapForestArt.Background(phase), phase.ToString());
        Assert.IsNotNull(MapForestArt.Get("PanelFrame"));
        Assert.IsNotNull(MapForestArt.Icon(MapTileType.Treasure));
    }

    [Test]
    public void PawnFrameStaysInsideItsTileAfterRepeatedPositioning()
    {
        BoardManager board = host.AddComponent<BoardManager>();
        BoardTileData tile = new BoardTileData { coord = Vector2Int.zero, tileType = MapTileType.Empty };
        board.LoadBoard(1, Vector2Int.zero, Vector2Int.zero, Vector2Int.zero, new[] { tile });
        PlayerMapController player = host.AddComponent<PlayerMapController>();
        player.Initialize(host.transform, board, null, null, new RunState());
        player.SetSpawn(tile);
        player.SetPosition(tile);
        SpriteRenderer frame = host.transform.Find("PlayerMapPawn/VisualRoot/Outline").GetComponent<SpriteRenderer>();
        Assert.LessOrEqual(frame.bounds.size.x, board.TileVisualSize.x);
        Assert.LessOrEqual(frame.bounds.size.y, board.TileVisualSize.y);
    }

    [Test]
    public void DetailGlyphsHaveCanvasRenderersAndVisibleMeshes()
    {
        foreach (MapHudGlyph glyph in host.GetComponentsInChildren<MapHudGlyph>())
        {
            Assert.IsNotNull(glyph.GetComponent<CanvasRenderer>(), glyph.name);
            using (VertexHelper vertices = new VertexHelper())
            {
                typeof(MapHudGlyph).GetMethod("OnPopulateMesh", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.DeclaredOnly)
                    .Invoke(glyph, new object[] { vertices });
                Assert.Greater(vertices.currentVertCount, 0, glyph.name);
            }
            Assert.IsFalse(glyph.raycastTarget);
        }
    }

    [TestCase(7, 2340, 1080)]
    [TestCase(9, 1920, 1080)]
    [TestCase(11, 1600, 1200)]
    [TestCase(13, 2400, 1080)]
    public void OverviewKeepsEntireBoardInTheSpaceBetweenSidePanels(int size, int width, int height)
    {
        float halfBoard = (size - 1) * 0.675f + 0.56f;
        float overview = MapViewController.CalculateOverviewSize(halfBoard, 2.8f, width / (float)height);
        float screenFraction = halfBoard / (overview * width / height);
        Assert.LessOrEqual(screenFraction, 0.421f);
        Assert.GreaterOrEqual(overview, halfBoard);
    }

    [TestCase(7, 7, MapBackgroundPhase.Morning)]
    [TestCase(3, 7, MapBackgroundPhase.Noon)]
    [TestCase(1, 7, MapBackgroundPhase.Dusk)]
    public void ActionPointThresholdsRemainUnchanged(int remaining, int max, MapBackgroundPhase phase)
    {
        Assert.AreEqual(phase, MapBackgroundController.ResolveActionPointBackgroundPhase(remaining, max));
    }

    private Canvas Layout(int width, int height, int sideInset)
    {
        Canvas canvas = host.GetComponentInChildren<Canvas>();
        canvas.GetComponent<CanvasScaler>().enabled = false;
        canvas.renderMode = RenderMode.WorldSpace;
        RectTransform rect = (RectTransform)canvas.transform;
        rect.sizeDelta = new Vector2(1080f * width / height, 1080f);
        rect.position = Vector3.zero;
        hud.ApplyViewport(new Vector2(width, height), new Rect(sideInset, 0f, width - sideInset * 2f, height));
        Canvas.ForceUpdateCanvases();
        return canvas;
    }

    private RectTransform Find(string name) { return host.GetComponentsInChildren<RectTransform>(true).First(t => t.name == name); }
    private TextMeshProUGUI Text(string name) { return Find(name).GetComponent<TextMeshProUGUI>(); }
    private Rect Bounds(string name) { return Bounds(Find(name)); }
    private static Rect Bounds(RectTransform rect)
    {
        Vector3[] corners = new Vector3[4]; rect.GetWorldCorners(corners);
        return Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
    }
    private static void AssertContains(Rect outer, Rect inner)
    {
        Assert.GreaterOrEqual(inner.xMin, outer.xMin - 0.02f);
        Assert.GreaterOrEqual(inner.yMin, outer.yMin - 0.02f);
        Assert.LessOrEqual(inner.xMax, outer.xMax + 0.02f);
        Assert.LessOrEqual(inner.yMax, outer.yMax + 0.02f);
    }
}
