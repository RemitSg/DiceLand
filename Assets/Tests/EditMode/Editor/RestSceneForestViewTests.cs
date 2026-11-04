using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class RestSceneForestViewTests
{
    [TestCase(1920, 1080, 0, 0)]
    [TestCase(2960, 1440, 100, 30)]
    [TestCase(1600, 1200, 0, 24)]
    [TestCase(1080, 1920, 50, 80)]
    public void SpriteAndSignProjectionStayAlignedAcrossAspectRatios(int width, int height, int insetX, int insetY)
    {
        var host = new GameObject("WaystationLayoutTest");
        try
        {
            var cameraObject = new GameObject("TestCamera", typeof(Camera)); cameraObject.transform.SetParent(host.transform);
            var camera = cameraObject.GetComponent<Camera>(); camera.orthographic = true; camera.orthographicSize = 4.705f;
            camera.aspect = (float)width / height; camera.transform.position = new Vector3(0, 0, -10);
            var world = new GameObject("World").transform; world.SetParent(host.transform);
            var canvas = new GameObject("Canvas", typeof(RectTransform)).GetComponent<RectTransform>(); canvas.SetParent(host.transform);
            canvas.sizeDelta = new Vector2(1080f * width / height, 1080f);
            var signs = new GameObject("Signs", typeof(RectTransform)).GetComponent<RectTransform>(); signs.SetParent(canvas, false);
            var view = host.AddComponent<RestSceneForestView>();
            view.Configure(null, null, null, null, camera, world, null, canvas, signs, null, null, new RestSceneForestView.StationVisual[0]);
            view.ApplyLayout(new Vector2(width, height), new Rect(insetX, insetY, width - 2 * insetX, height - 2 * insetY));
            foreach (var point in new[] { new Vector2(260, 525), new Vector2(610, 495), new Vector2(1069, 540), new Vector2(1486, 520) })
            {
                Vector2 local = new Vector2(point.x - 836, 470.5f - point.y);
                Vector3 projected = camera.WorldToViewportPoint(world.TransformPoint(local / 100f));
                Vector2 signPixel = (signs.anchoredPosition + local * signs.localScale.x + canvas.rect.size * 0.5f) / canvas.rect.size;
                Assert.That(projected.x, Is.EqualTo(signPixel.x).Within(0.00001f));
                Assert.That(projected.y, Is.EqualTo(signPixel.y).Within(0.00001f));
            }
        }
        finally { Object.DestroyImmediate(host); }
    }

    [TestCase(1920, 1080, 0, 0)]
    [TestCase(2960, 1440, 100, 30)]
    [TestCase(1600, 1200, 0, 24)]
    [TestCase(1080, 1920, 50, 80)]
    public void FittedCompositionKeepsAllFourStationsAndSignsInsideSafeArea(int width, int height, int insetX, int insetY)
    {
        Rect safe = new Rect(insetX, insetY, width - 2 * insetX, height - 2 * insetY);
        Rect fit = RestSceneForestView.FitComposition(safe, RestSceneForestView.ReferenceSize);
        Assert.That(fit.xMin, Is.GreaterThanOrEqualTo(safe.xMin - 0.01f));
        Assert.That(fit.xMax, Is.LessThanOrEqualTo(safe.xMax + 0.01f));
        Assert.That(fit.yMin, Is.GreaterThanOrEqualTo(safe.yMin - 0.01f));
        Assert.That(fit.yMax, Is.LessThanOrEqualTo(safe.yMax + 0.01f));
        Assert.That(fit.width / fit.height, Is.EqualTo(1672f / 941f).Within(0.0001f));
    }

    [TestCase("ActionPoints")]
    [TestCase("Forge")]
    [TestCase("Recruit")]
    [TestCase("Rest")]
    public void StationSourceContainsRealTransparentBackgroundAndVisibleObject(string name)
    {
        var texture = new Texture2D(2, 2);
        try
        {
            Assert.IsTrue(texture.LoadImage(File.ReadAllBytes("Assets/Resources/RestScene/" + name + ".png")));
            var pixels = texture.GetPixels32();
            Assert.That(pixels.Count(p => p.a == 0), Is.GreaterThan(pixels.Length / 2));
            Assert.That(pixels.Count(p => p.a > 240), Is.GreaterThan(pixels.Length / 40));
            Assert.AreEqual(0, pixels[0].a);
        }
        finally { Object.DestroyImmediate(texture); }
    }

    [Test]
    public void AuthoredSceneBindsFourIndependentSpritesCollidersControllersAndNativeSigns()
    {
        Scene scene = SceneManager.GetSceneByPath(RestSceneForestBakeTools.ScenePath);
        bool opened = !scene.isLoaded;
        if (opened) scene = EditorSceneManager.OpenScene(RestSceneForestBakeTools.ScenePath, OpenSceneMode.Additive);
        try
        {
            var view = RestSceneForestBakeTools.Find<RestSceneForestView>(scene);
            Assert.NotNull(view); Assert.AreEqual(4, view.Stations.Length);
            Assert.AreEqual(4, view.Stations.Select(v => v.station).Distinct().Count());
            foreach (var visual in view.Stations)
            {
                var controller = view.Controller(visual.station);
                Assert.NotNull(controller);
                Assert.AreSame(controller.transform.parent, view.WorldComposition);
                Assert.AreSame(view, visual.plaque.Owner);
                Assert.AreEqual(visual.station, visual.plaque.Station);
                Assert.IsInstanceOf<IRestSceneStationPresentation>(controller);
                var sprite = controller.GetComponent<SpriteRenderer>().sprite;
                Assert.AreEqual(visual.station.ToString(), sprite.name);
                Assert.That(controller.transform.localPosition.z,
                    Is.EqualTo(-controller.GetComponent<SpriteRenderer>().sortingOrder * 0.01f).Within(0.00001f));
                var collider = controller.GetComponent<PolygonCollider2D>();
                Assert.That(collider.pathCount, Is.GreaterThan(0));
                Assert.That(collider.bounds.size.sqrMagnitude, Is.GreaterThan(0.01f));
                Assert.IsFalse(sprite.texture.isReadable, "Runtime input must use the baked collider.");
            }
            foreach (Graphic graphic in view.Composition.GetComponentsInChildren<Graphic>(true))
            {
                Assert.NotNull(graphic.GetComponent<CanvasRenderer>(), graphic.name + " must render in a fresh scene.");
                if (graphic.GetComponent<RestSceneStationTarget>() == null) Assert.IsFalse(graphic.raycastTarget, graphic.name);
            }
        }
        finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
    }
}
