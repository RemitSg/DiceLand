using System;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public sealed class TutorialForestViewTests
{
    private GameObject host;
    private Camera camera;
    private RenderTexture target;
    private float oldScale;
    private Vector2 pixels;
    private Rect safe;

    [SetUp]
    public void SetUp()
    {
        oldScale = Time.timeScale;
        host = new GameObject("TutorialForestTests");
        camera = new GameObject("TutorialTestCamera", typeof(Camera)).GetComponent<Camera>(); camera.transform.SetParent(host.transform);
        camera.transform.position = new Vector3(0, 0, -10); camera.orthographic = true; camera.orthographicSize = 5;
    }

    [TearDown]
    public void TearDown()
    {
        foreach (Camera item in host.GetComponentsInChildren<Camera>(true)) item.targetTexture = null;
        if (target != null) Object.DestroyImmediate(target);
        Object.DestroyImmediate(host); Time.timeScale = oldScale;
    }

    private void Viewport(int width, int height, Rect rect)
    {
        camera.targetTexture = null; if (target != null) Object.DestroyImmediate(target);
        target = new RenderTexture(width, height, 0); camera.targetTexture = target; camera.aspect = width / (float)height;
        pixels = new Vector2(width, height); safe = rect;
    }

    [TestCase(1920, 1080, 0, 0, 1920, 1080)]
    [TestCase(2640, 1216, 105, 15, 2450, 1170)]
    [TestCase(1600, 1200, 0, 40, 1600, 1120)]
    [TestCase(1080, 2400, 48, 100, 960, 2140)]
    public void FocusTracksRealHudFieldsAndKeepsTheCardInsideSafeArea(int width, int height, int x, int y, int sw, int sh)
    {
        Viewport(width, height, new Rect(x, y, sw, sh));
        MapHUD hud = MakeHud();
        var guide = host.AddComponent<MapTutorialGuideOverlay>();
        Configure(guide);
        Rect? ap = Resolve(hud, MapTutorialFocusPart.ActionPoints);
        Assert.IsTrue(ap.HasValue);
        Assert.IsTrue(ap.Value.Contains(Center(Find("ActionPoints"))));
        Assert.IsFalse(ap.Value.Contains(Center(Find("CalamityCell"))));
        guide.ShowFocused("行动点", TutorialForestVisualTools.ActionBody, null, "继续", () => Resolve(hud, MapTutorialFocusPart.ActionPoints), () => Resolve(hud, MapTutorialFocusPart.Resources));
        Fit(guide.Presentation);
        AssertFocus(guide.Presentation, ap.Value);
        AssertSafe(guide.Presentation);
        Rect? header = Resolve(hud, MapTutorialFocusPart.TileHeader);
        Assert.IsTrue(header.HasValue);
        Assert.IsTrue(header.Value.Contains(Center(Find("TileTitle"))));
        Assert.IsTrue(header.Value.Contains(Center(Find("CoordinateBadge"))));
        Assert.IsFalse(header.Value.Contains(Center(Find("TileIconBase"))));
        Assert.IsFalse(header.Value.Contains(Center(Find("ExploredBadge"))));
        guide.ShowFocused("坐标与类型", TutorialForestVisualTools.HeaderBody, null, "继续", () => Resolve(hud, MapTutorialFocusPart.TileHeader), () => Resolve(hud, MapTutorialFocusPart.TilePanel), "格子详情");
        Fit(guide.Presentation);
        AssertFocus(guide.Presentation, header.Value); AssertSafe(guide.Presentation);
        Assert.IsFalse(guide.Presentation.Reading.verticalScrollbar.gameObject.activeSelf, "A short hint must not display an empty scrollbar.");
    }

    [Test]
    public void ChangingViewportAndSafeAreaWhileVisibleMovesBothFrameAndHoleToLiveTarget()
    {
        Viewport(1920, 1080, new Rect(0, 0, 1920, 1080)); MapHUD hud = MakeHud();
        var guide = host.AddComponent<MapTutorialGuideOverlay>(); Configure(guide);
        guide.ShowFocused("行动点", "移动会消耗行动点。", null, "继续", () => Resolve(hud, MapTutorialFocusPart.ActionPoints));
        Fit(guide.Presentation); Rect before = guide.Presentation.Shade.hole;
        Viewport(2640, 1216, new Rect(175, 30, 2300, 1100));
        TutorialForestVisualTools.ConfigureCanvas(HudCanvas(), camera, pixels, -2.5f); hud.ApplyViewport(pixels, safe); Fit(guide.Presentation);
        Assert.AreNotEqual(before, guide.Presentation.Shade.hole);
        AssertFocus(guide.Presentation, Resolve(hud, MapTutorialFocusPart.ActionPoints).Value);
        AssertSafe(guide.Presentation);
    }

    [Test]
    public void CameraCanvasTargetMapsThroughTheTutorialCanvasOwnCamera()
    {
        Viewport(1920, 1080, new Rect(45, 30, 1830, 1020));
        var source = new GameObject("CameraHud", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler)).GetComponent<Canvas>();
        source.transform.SetParent(host.transform); source.renderMode = RenderMode.ScreenSpaceCamera; source.worldCamera = camera; source.planeDistance = 3;
        RectTransform cell = TutorialForestView.Rect(source.transform, "CameraTarget");
        cell.anchorMin = cell.anchorMax = new Vector2(.28f, .72f); cell.sizeDelta = new Vector2(210, 85);
        Canvas.ForceUpdateCanvases();
        Assert.IsTrue(TutorialHighlightCoordinateUtility.TryGetScreenRect(cell, out Rect sourceBounds));
        var overlayCamera = new GameObject("DifferentOverlayCamera", typeof(Camera)).GetComponent<Camera>();
        overlayCamera.transform.SetParent(host.transform); overlayCamera.transform.position = new Vector3(3, 0, -12);
        overlayCamera.orthographic = true; overlayCamera.orthographicSize = 7; overlayCamera.aspect = camera.aspect; overlayCamera.targetTexture = target;
        var guide = host.AddComponent<MapTutorialGuideOverlay>();
        guide.ShowFocused("相机画布", "阅读目标。", null, "继续", () => TutorialHighlightCoordinateUtility.TryGetScreenRect(cell, out Rect r) ? r : (Rect?)null);
        TutorialForestView view = guide.Presentation;
        TutorialForestVisualTools.ConfigureCanvas(view.Canvas, overlayCamera, pixels, -3);
        view.Root.position = new Vector3(3, 0, -3); view.Root.localScale = Vector3.one * (7f / 540f);
        Canvas.ForceUpdateCanvases(); view.ApplyViewport(pixels, safe); view.Advance(2);
        Assert.IsTrue(view.HasFocus);
        Vector3[] corners = new Vector3[4]; view.Focus.GetWorldCorners(corners);
        Vector2 min = RectTransformUtility.WorldToScreenPoint(overlayCamera, corners[0]);
        Vector2 max = RectTransformUtility.WorldToScreenPoint(overlayCamera, corners[2]);
        Assert.AreEqual(sourceBounds.xMin - 5, min.x, .3f); Assert.AreEqual(sourceBounds.yMin - 5, min.y, .3f);
        Assert.AreEqual(sourceBounds.xMax + 5, max.x, .3f); Assert.AreEqual(sourceBounds.yMax + 5, max.y, .3f);
        overlayCamera.targetTexture = null;
    }

    [Test]
    public void ClippedOrHiddenTargetsCannotLeaveAnInteractiveGhostHole()
    {
        Viewport(1920, 1080, new Rect(0, 0, 1920, 1080));
        var canvas = new GameObject("ClippedSource", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler)).GetComponent<Canvas>();
        canvas.transform.SetParent(host.transform);
        TutorialForestVisualTools.ConfigureCanvas(canvas, camera, pixels, -2.5f);
        RectTransform clip = TutorialForestView.Rect(canvas.transform, "Clip"); clip.sizeDelta = new Vector2(300, 180); clip.gameObject.AddComponent<RectMask2D>();
        RectTransform child = TutorialForestView.Rect(clip, "Target"); child.sizeDelta = new Vector2(200, 80); child.anchoredPosition = Vector2.right * 200;
        Canvas.ForceUpdateCanvases();
        Assert.IsTrue(TutorialHighlightCoordinateUtility.TryGetScreenRect(child, out Rect visible));
        Assert.Less(visible.width, 65); // Exactly the final 50 logical pixels survive this mask.
        var guide = host.AddComponent<MapTutorialGuideOverlay>(); guide.ShowTargetHighlight(visible);
        guide.TrackTarget(() => TutorialHighlightCoordinateUtility.TryGetScreenRect(child, out Rect r) ? r : (Rect?)null); Fit(guide.Presentation);
        Assert.IsTrue(guide.Presentation.HasFocus);
        Assert.IsFalse(guide.Presentation.Shade.IsRaycastLocationValid(visible.center, camera));
        child.anchoredPosition = Vector2.right * 500; guide.AdvancePresentation(.01f);
        Assert.IsFalse(guide.Presentation.HasFocus);
        Assert.IsTrue(guide.Presentation.Shade.IsRaycastLocationValid(visible.center, camera));
    }

    [Test]
    public void ExplanatorySpotlightBlocksTheHoleAndLongReadingKeepsContinueFixed()
    {
        Viewport(1600, 1200, new Rect(60, 50, 1470, 1090)); MapHUD hud = MakeHud();
        var guide = host.AddComponent<MapTutorialGuideOverlay>(); Configure(guide);
        string body = string.Join("\n", Enumerable.Repeat("较长的教程说明应允许完整阅读，保留触发条件和段落。", 35)) + "\n最后一条说明。";
        guide.ShowFocused("行动点", body, null, "继续", () => Resolve(hud, MapTutorialFocusPart.ActionPoints)); Fit(guide.Presentation);
        var view = guide.Presentation; Rect targetBounds = Resolve(hud, MapTutorialFocusPart.ActionPoints).Value;
        Assert.IsTrue(view.Shade.IsRaycastLocationValid(targetBounds.center, camera));
        Assert.Greater(view.BodyContent.rect.height, view.Reading.viewport.rect.height * 3);
        Assert.AreEqual(body, TutorialForestView.GetText(view.Body));
        Assert.IsTrue(view.Reading.verticalScrollbar.gameObject.activeSelf);
        Vector3 button = view.Primary.transform.position; view.Reading.verticalNormalizedPosition = 0; Canvas.ForceUpdateCanvases();
        Assert.AreEqual(button, view.Primary.transform.position); Assert.Less(view.Reading.verticalNormalizedPosition, .001f);
        AssertFocus(view, targetBounds);
    }

    [Test]
    public void AllThreeSurfacesUseSameImportedSkinAndPrimaryButton()
    {
        Viewport(1920, 1080, new Rect(0, 0, 1920, 1080));
        var menu = host.AddComponent<MainMenuTutorialOverlay>();
        menu.Show(null, null, null, hideSections: true); Fit(menu.Presentation);
        var map = host.AddComponent<MapTutorialGuideOverlay>(); map.Show("地图探索", "正文", null); Fit(map.Presentation);
        var battle = host.AddComponent<TutorialBattleGuideOverlay>(); battle.Show("战斗教程", "正文", "继续", null); Fit(battle.Presentation);
        Sprite frame = Resources.Load<Sprite>("TutorialUI/ForestWindow"), plaque = Resources.Load<Sprite>("TutorialUI/WoodTitle");
        Assert.IsNotNull(frame); Assert.Greater(frame.border.x, 0); Assert.IsNotNull(plaque);
        foreach (TutorialForestView view in new[] { menu.Presentation, map.Presentation, battle.Presentation })
        {
            Assert.AreSame(frame, view.Panel.Find("ForestFrame").GetComponent<Image>().sprite);
            Assert.AreSame(plaque, view.Panel.Find("TitlePlaque").GetComponent<Image>().sprite);
            Assert.IsNotNull(view.Shade.GetComponent<CanvasRenderer>());
            Assert.IsNotNull(view.Primary.GetComponent<MapCraftingButtonFeedback>());
        }
        Assert.IsNotNull(menu.Presentation.Panel.Find("AdventureIllustration").GetComponent<Image>().sprite);
        battle.Hide(false); map.Hide();
    }

    private MapHUD MakeHud()
    {
        var hud = host.AddComponent<MapHUD>();
        hud.ConfigureFont(AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath("c6e7143cd2df45b45a784ef8c61a210f")));
        hud.ShowTile(new BoardTileData { coord = new Vector2Int(1, 3), tileType = MapTileType.Battle }, 2, true);
        TutorialForestVisualTools.ConfigureCanvas(HudCanvas(), camera, pixels, -2.5f); hud.ApplyViewport(pixels, safe); Canvas.ForceUpdateCanvases(); return hud;
    }
    private Canvas HudCanvas() => host.GetComponentsInChildren<Canvas>(true).Single(c => c.name == "MapHudCanvas");
    private RectTransform Find(string name) => host.GetComponentsInChildren<RectTransform>(true).Single(t => t.name == name);
    private Vector2 Center(RectTransform t) => RectTransformUtility.WorldToScreenPoint(camera, t.TransformPoint(t.rect.center));
    private static Rect? Resolve(MapHUD hud, MapTutorialFocusPart part) => hud.TryGetTutorialTargetScreenRect(part, out Rect r) ? r : (Rect?)null;
    private static void Configure(MapTutorialGuideOverlay guide)
    {
        Font font = AssetDatabase.LoadAssetAtPath<Font>(TutorialForestVisualTools.BodyFontPath); guide.ConfigureFonts(font, font, font);
    }
    private void Fit(TutorialForestView view)
    {
        TutorialForestVisualTools.ConfigureCanvas(view.Canvas, camera, pixels, -3); Canvas.ForceUpdateCanvases(); view.ApplyViewport(pixels, safe); view.Advance(2); Canvas.ForceUpdateCanvases();
    }
    private void AssertFocus(TutorialForestView view, Rect expected)
    {
        Assert.IsTrue(view.HasFocus); Assert.AreEqual(expected.center.x, view.FocusScreenRect.center.x, .1f); Assert.AreEqual(expected.center.y, view.FocusScreenRect.center.y, .1f);
        Assert.AreEqual(view.Shade.hole.size, view.Focus.sizeDelta);
        Vector3[] corners = new Vector3[4]; view.Focus.GetWorldCorners(corners);
        Vector2 min = RectTransformUtility.WorldToScreenPoint(camera, corners[0]), max = RectTransformUtility.WorldToScreenPoint(camera, corners[2]);
        float pad = 5 * pixels.y / 1080f;
        Assert.AreEqual(expected.xMin - pad, min.x, .25f); Assert.AreEqual(expected.yMin - pad, min.y, .25f);
        Assert.AreEqual(expected.xMax + pad, max.x, .25f); Assert.AreEqual(expected.yMax + pad, max.y, .25f);
        if (view.Panel.gameObject.activeSelf)
        {
            view.Panel.GetWorldCorners(corners); min = RectTransformUtility.WorldToScreenPoint(camera, corners[0]); max = RectTransformUtility.WorldToScreenPoint(camera, corners[2]);
            Assert.IsFalse(Rect.MinMaxRect(min.x, min.y, max.x, max.y).Overlaps(expected));
        }
    }
    private void AssertSafe(TutorialForestView view)
    {
        foreach (RectTransform rect in new[] { view.Panel, (RectTransform)view.Primary.transform, view.Panel.Find("TitlePlaque") as RectTransform })
        {
            Vector3[] corners = new Vector3[4]; rect.GetWorldCorners(corners);
            foreach (Vector3 point in corners)
            {
                Vector2 p = RectTransformUtility.WorldToScreenPoint(camera, point);
                Assert.That(p.x, Is.InRange(safe.xMin - 1, safe.xMax + 1)); Assert.That(p.y, Is.InRange(safe.yMin - 1, safe.yMax + 1));
            }
        }
    }
}
