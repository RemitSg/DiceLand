using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

public sealed class MapForestHudPlayModeTests
{
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private GameObject host;
    private Component hud, points;
    private object state;
    private float originalTimeScale;

    [SetUp]
    public void SetUp()
    {
        originalTimeScale = Time.timeScale;
        host = new GameObject("MapForestHudPlayTest");
        points = host.AddComponent(TypeOf("ActionPointSystem"));
        Invoke(points, "Initialize", 7);
        hud = host.AddComponent(TypeOf("MapHUD"));
        state = Activator.CreateInstance(TypeOf("RunState"));
        Set(state, "difficultyName", "Easy");
        Set(state, "money", 11);
        Set(state, "currentKeys", 1);
        Set(state, "requiredKeys", 2);
        Invoke(hud, "Refresh", state, points);
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        Time.timeScale = originalTimeScale;
        UnityEngine.Object.Destroy(host);
        yield return null;
    }

    [UnityTest]
    public IEnumerator NativeHudUpdatesSelectedTileAndMenuWithoutSpendingResources()
    {
        yield return null;
        Assert.AreEqual(RenderMode.ScreenSpaceOverlay, host.GetComponentInChildren<Canvas>().renderMode);
        Assert.AreEqual("简单", Text("Difficulty"));
        Assert.AreEqual("1/2", Text("Keys"));
        object tile = Tile("Empty", new Vector2Int(3, 5));
        Set(tile, "isVisited", true);
        Set(tile, "isResolved", true);
        Invoke(hud, "ShowTile", tile, 0, false);
        yield return null;
        Assert.AreEqual("空白", Text("TileTitle"));
        Assert.AreEqual("[3,5]", Text("TileCoordinates"));
        Assert.AreEqual("已探索", Text("TileExplored"));
        Invoke(hud, "ShowTile", Tile("Treasure", new Vector2Int(4, 5)), 2, false);
        yield return null;
        Assert.AreEqual("未探索", Text("TileExplored"));
        Assert.AreEqual("2", Text("TileMoveCost"));
        Find("InventoryButton").GetComponent<Button>().onClick.Invoke();
        Assert.IsTrue(Find("InventoryPanel").gameObject.activeSelf);
        Find("InventoryButton").GetComponent<Button>().onClick.Invoke();
        Assert.IsFalse(Find("InventoryPanel").gameObject.activeSelf);
        Assert.AreEqual(11, Get(state, "money"));
        Assert.AreEqual(7, points.GetType().GetProperty("CurrentPoints").GetValue(points));
    }

    [UnityTest]
    public IEnumerator PartyScrollAndCollapseWorkWhileTimeIsPaused()
    {
        IList members = (IList)Get(state, "partyMembers");
        for (int i = 0; i < 20; i++)
        {
            object member = Activator.CreateInstance(TypeOf("RunPartyMemberState"));
            Set(member, "memberId", "Member" + i);
            Set(member, "maxHP", 17);
            Set(member, "currentHP", 17);
            members.Add(member);
        }
        Invoke(hud, "Refresh", state, points);
        Time.timeScale = 0f;
        yield return null;
        ScrollRect scroll = Find("PartyListViewport").GetComponent<ScrollRect>();
        Canvas.ForceUpdateCanvases();
        scroll.verticalNormalizedPosition = 1f;
        float before = scroll.content.anchoredPosition.y;
        EventSystem eventSystem = host.AddComponent<EventSystem>();
        scroll.OnScroll(new PointerEventData(eventSystem) { scrollDelta = new Vector2(0f, -4f) });
        yield return null;
        Assert.Greater(scroll.content.anchoredPosition.y, before);
        Find("PartyToggleButton").GetComponent<Button>().onClick.Invoke();
        yield return new WaitForSecondsRealtime(0.5f);
        Assert.IsFalse(Find("PartyListViewport").gameObject.activeSelf);
        Find("PartyToggleButton").GetComponent<Button>().onClick.Invoke();
        yield return new WaitForSecondsRealtime(0.5f);
        Assert.IsTrue(Find("PartyListViewport").gameObject.activeSelf);
        Assert.Greater(scroll.content.rect.height, scroll.viewport.rect.height);
    }

    [UnityTest]
    public IEnumerator ActionPointsSwitchThreeMapBackdropsAndExposeSeparateMidnight()
    {
        Component board = host.AddComponent(TypeOf("BoardManager"));
        Array tiles = Array.CreateInstance(TypeOf("BoardTileData"), 1);
        tiles.SetValue(Tile("Empty", Vector2Int.zero), 0);
        Invoke(board, "LoadBoard", 7, Vector2Int.zero, Vector2Int.one, Vector2Int.zero, tiles);
        GameObject cameraObject = new GameObject("MapBackgroundTestCamera", typeof(Camera));
        cameraObject.transform.SetParent(host.transform);
        Camera camera = cameraObject.GetComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 5f;
        camera.transform.position = new Vector3(0f, 0f, -10f);
        GameObject backgroundRoot = new GameObject("BackdropRoot");
        backgroundRoot.transform.SetParent(host.transform);
        Component background = host.AddComponent(TypeOf("MapBackgroundController"));
        Set(background, "fadeToBlackDuration", 0.01f);
        Set(background, "fadeFromBlackDuration", 0.01f);
        Invoke(background, "Initialize", backgroundRoot.transform, camera, board, points);
        Assert.AreEqual("ForestMorning", backgroundRoot.transform.Find("ImageBackdrop").GetComponent<SpriteRenderer>().sprite.name);
        Invoke(points, "TrySpend", 4);
        yield return new WaitForSecondsRealtime(0.1f);
        Assert.AreEqual("ForestNoon", backgroundRoot.transform.Find("ImageBackdrop").GetComponent<SpriteRenderer>().sprite.name);
        Invoke(points, "TrySpend", 2);
        yield return new WaitForSecondsRealtime(0.1f);
        Assert.AreEqual("ForestDusk", backgroundRoot.transform.Find("ImageBackdrop").GetComponent<SpriteRenderer>().sprite.name);
        camera.aspect = 3.2f;
        yield return null;
        Assert.GreaterOrEqual(backgroundRoot.transform.Find("ImageBackdrop").GetComponent<SpriteRenderer>().bounds.size.x,
            camera.orthographicSize * 2f * camera.aspect, "Resizing the viewport must not expose empty background edges.");
        Sprite midnight = (Sprite)Invoke(background, "GetBlurredMidnightBackgroundSprite");
        Assert.IsNotNull(midnight);
        Assert.That(midnight.texture.name, Does.Contain("Midnight"));
        Component calamity = host.AddComponent(TypeOf("MapCalamityOverlay"));
        Invoke(calamity, "ConfigureBackdropSprite", midnight);
        Invoke(calamity, "Show", 1, Array.CreateInstance(TypeOf("Calamity_SO"), 0), null);
        Assert.AreSame(midnight, ((Image)Get(calamity, "backdropBackgroundImage")).sprite);
        Assert.AreEqual(1, points.GetType().GetProperty("CurrentPoints").GetValue(points));
    }

    private Transform Find(string name) { return host.GetComponentsInChildren<Transform>(true).First(t => t.name == name); }
    private string Text(string name)
    {
        Component text = Find(name).GetComponent(TypeOf("TMPro.TextMeshProUGUI"));
        return (string)text.GetType().GetProperty("text").GetValue(text);
    }
    private static Type TypeOf(string name) { return AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(name)).First(t => t != null); }
    private static object Get(object target, string name) { return target.GetType().GetField(name, Members).GetValue(target); }
    private static void Set(object target, string name, object value) { target.GetType().GetField(name, Members).SetValue(target, value); }
    private static object Invoke(object target, string name, params object[] args)
    {
        return target.GetType().GetMethods(Members).Single(m => m.Name == name && m.GetParameters().Length == args.Length).Invoke(target, args);
    }
    private static object Tile(string type, Vector2Int coord)
    {
        object tile = Activator.CreateInstance(TypeOf("BoardTileData"));
        Set(tile, "tileType", Enum.Parse(TypeOf("MapTileType"), type));
        Set(tile, "coord", coord);
        return tile;
    }
}
