using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public sealed class BattleLookControllerTests
{
    private GameObject root;
    private VolumeProfile original;
    private Texture2D texture;
    private Sprite sprite;
    private BattleLookController look;
    private Volume volume;
    private Camera overview;
    private Camera closeup;

    [SetUp]
    public void SetUp()
    {
        root = new GameObject("Battle look test");
        root.SetActive(false);
        overview = CreateCamera("Overview", new Vector3(0f, 3f, -20f));
        closeup = CreateCamera("Closeup", new Vector3(0f, 3f, -4f));
        volume = root.AddComponent<Volume>();
        original = ScriptableObject.CreateInstance<VolumeProfile>();
        original.Add<DepthOfField>(true).gaussianStart.Override(34f);
        original.Add<MotionBlur>(true);
        volume.sharedProfile = original;
        look = root.AddComponent<BattleLookController>();
        Set("overviewCamera", overview);
        Set("closeupCamera", closeup);
        Set("lookVolume", volume);

        GameObject actors = new GameObject("Actors");
        actors.transform.SetParent(root.transform);
        Set("allies", actors.transform);
        texture = new Texture2D(2, 2);
        sprite = Sprite.Create(texture, new Rect(0, 0, 2, 2), Vector2.one * 0.5f, 1f);
        AddActor(actors.transform, new Vector3(0f, 1f, 0f));
        AddActor(actors.transform, new Vector3(1f, 1f, 6f));
        AddActor(actors.transform, new Vector3(1000f, 1f, 60f)); // Off screen scenery must not pull focus.
        root.SetActive(true);
        if (volume.sharedProfile == original)
            Invoke("OnEnable");
    }

    [TearDown]
    public void TearDown()
    {
        Invoke("OnDisable");
        Object.DestroyImmediate(root);
        foreach (VolumeComponent component in original.components)
            Object.DestroyImmediate(component);
        Object.DestroyImmediate(original);
        Object.DestroyImmediate(sprite);
        Object.DestroyImmediate(texture);
    }

    [Test]
    public void FocusKeepsTheWholeVisibleFormationSharpAndTracksEachCamera()
    {
        look.PrepareCamera(overview);
        Assert.IsTrue(volume.sharedProfile.TryGet(out DepthOfField depth));
        Assert.GreaterOrEqual(depth.gaussianStart.value, 28f);
        Assert.Less(depth.gaussianStart.value, 30f);
        Assert.Greater(depth.gaussianEnd.value, depth.gaussianStart.value);
        float overviewStart = depth.gaussianStart.value;

        look.PrepareCamera(closeup);
        Assert.Less(depth.gaussianStart.value, overviewStart - 10f);
        Assert.GreaterOrEqual(depth.gaussianStart.value, 12f);
    }

    [Test]
    public void OverviewKeepsMidgroundSharpWhileCloseupStillTracksCombatants()
    {
        GameObject scenery = new GameObject("Midground ruin");
        scenery.transform.SetParent(root.transform);
        scenery.transform.position = new Vector3(0f, 3f, 14f);
        SpriteRenderer renderer = scenery.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        Set("midground", renderer);

        look.PrepareCamera(overview);
        Assert.IsTrue(volume.sharedProfile.TryGet(out DepthOfField depth));
        Assert.AreEqual(DepthOfFieldMode.Gaussian, depth.mode.value);
        Assert.GreaterOrEqual(depth.gaussianStart.value, 36f);
        Assert.LessOrEqual(depth.gaussianStart.value, 37f);
        Assert.AreEqual(24f, depth.gaussianEnd.value - depth.gaussianStart.value, 0.001f);

        look.PrepareCamera(closeup);
        Assert.Less(depth.gaussianStart.value, 14f, "Closeup focus must exclude the scenery layer.");
        Assert.AreEqual(12f, depth.gaussianEnd.value - depth.gaussianStart.value, 0.001f);

        scenery.SetActive(false);
        look.PrepareCamera(overview);
        Assert.Less(depth.gaussianStart.value, 30f, "Inactive scenery must not extend the sharp range.");
    }

    [Test]
    public void RuntimeFocusDoesNotModifyTheSharedAssetAndDisableRestoresIt()
    {
        Assert.AreNotSame(original, volume.sharedProfile);
        original.TryGet(out DepthOfField assetDepth);
        volume.sharedProfile.TryGet(out DepthOfField runtimeDepth);
        Assert.AreNotSame(assetDepth, runtimeDepth);
        look.PrepareCamera(closeup);
        Assert.AreEqual(34f, assetDepth.gaussianStart.value);

        Invoke("OnDisable");
        Assert.AreSame(original, volume.sharedProfile);
        Assert.AreEqual(34f, assetDepth.gaussianStart.value);
    }

    [Test]
    public void WorldUiCameraFollowsBothLensesWithoutPostProcessingAndRestoresMasks()
    {
        Camera overlay = root.transform.Find("Battle World UI Camera").GetComponent<Camera>();
        UniversalAdditionalCameraData data = overlay.GetUniversalAdditionalCameraData();
        Assert.AreEqual(CameraRenderType.Overlay, data.renderType);
        Assert.IsFalse(data.renderPostProcessing);
        Assert.Contains(overlay, overview.GetUniversalAdditionalCameraData().cameraStack);
        Assert.Contains(overlay, closeup.GetUniversalAdditionalCameraData().cameraStack);
        Assert.AreEqual(0, overview.cullingMask & (1 << 5));

        closeup.orthographic = false;
        closeup.fieldOfView = 24f;
        look.PrepareCamera(closeup);
        Assert.AreEqual(closeup.transform.position, overlay.transform.position);
        Assert.AreEqual(closeup.projectionMatrix, overlay.projectionMatrix);

        Invoke("OnDisable");
        Assert.AreEqual(-1, overview.cullingMask);
        Assert.AreEqual(-1, closeup.cullingMask);
        Assert.IsEmpty(overview.GetUniversalAdditionalCameraData().cameraStack);
    }

    [TestCase(0)]
    [TestCase(9)]
    public void DetachedIntentArrowsShareTheUiCameraAndRestoreTheirOriginalLayer(int originalLayer)
    {
        GameObject buttonObject = new GameObject("Layer witness speed button");
        buttonObject.transform.SetParent(root.transform.Find("Actors"));
        buttonObject.layer = originalLayer;
        SpeedButton button = buttonObject.AddComponent<SpeedButton>();
        GameObject arrow = new GameObject("Detached intent arrow witness");
        arrow.layer = originalLayer;
        MeshRenderer renderer = arrow.AddComponent<MeshRenderer>();
        renderer.sortingOrder = 42;
        FieldInfo field = typeof(SpeedButton).GetField("arrowObj", BindingFlags.Instance | BindingFlags.NonPublic);
        field.SetValue(button, arrow);
        try
        {
            look.PrepareCamera(overview);
            Assert.AreEqual(5, buttonObject.layer);
            Assert.AreEqual(5, arrow.layer, "The base camera must not draw an arrow that the UI camera then covers.");
            Assert.IsNull(arrow.transform.parent);
            Assert.AreEqual(42, renderer.sortingOrder);
            look.PrepareCamera(closeup);
            look.ReleaseLook();
            Assert.AreEqual(originalLayer, arrow.layer);
            Assert.AreEqual(originalLayer, buttonObject.layer);
        }
        finally
        {
            field.SetValue(button, null);
            Object.DestroyImmediate(arrow);
        }
    }

    private Camera CreateCamera(string name, Vector3 position)
    {
        GameObject item = new GameObject(name);
        item.transform.SetParent(root.transform);
        item.transform.position = position;
        Camera camera = item.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 10f;
        return camera;
    }

    private void AddActor(Transform parent, Vector3 position)
    {
        GameObject actor = new GameObject("Actor");
        actor.transform.SetParent(parent);
        actor.transform.position = position;
        actor.AddComponent<SpriteRenderer>().sprite = sprite;
    }

    private void Set(string name, object value)
    {
        typeof(BattleLookController).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(look, value);
    }

    private void Invoke(string name)
    {
        typeof(BattleLookController).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(look, null);
    }
}
