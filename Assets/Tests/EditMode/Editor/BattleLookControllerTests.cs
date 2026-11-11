using System.Reflection;
using NUnit.Framework;
using UnityEditor;
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
    private EnemyData_SO framingEnemyData;
    private RenderTexture framingTarget;

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
        if (framingEnemyData != null) Object.DestroyImmediate(framingEnemyData);
        if (framingTarget != null) Object.DestroyImmediate(framingTarget);
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

    [TestCase(1.333333f, false)]
    [TestCase(1.777778f, false)]
    [TestCase(2.171946f, false)]
    [TestCase(2.444444f, false)]
    [TestCase(1.333333f, true)]
    [TestCase(2.444444f, true)]
    public void LargeEnemyWidensOverviewAndKeepsTheWholeSpeedRowAboveItsHead(float aspect, bool orthographic)
    {
        bool previousLock = SpeedButton.CombatLocked;
        try
        {
            SpeedButton.CombatLocked = false;
            ConfigureFramingCamera(aspect, orthographic);
            look.PrepareCamera(overview);
            Vector3 normalPosition = overview.transform.position;
            float normalSize = overview.orthographicSize;
            BattleEnemyController enemy = CreateFramingEnemy();
            Vector3 actorPosition = enemy.transform.position;
            float authoredX = enemy.SpeedButtons[0].transform.localPosition.x;
            // Reproduce the existing sideways fallback before the camera gets a chance to widen.
            enemy.FitSpeedButtonsToCamera(overview, Screen.safeArea);
            look.PrepareCamera(overview);
            Assert.IsTrue(BattleActorVisualUtility.TryGetActorSpriteBounds(enemy.transform, out Bounds body));
            Rect bodyRect = ProjectFramingBounds(body);
            Rect safe = BattleEnemyController.GetSpeedButtonSafeArea(overview, Screen.safeArea);
            foreach (SpeedButton button in enemy.SpeedButtons)
            {
                Rect badge = ProjectFramingBounds(button.GetViewportBounds());
                Assert.Greater(badge.yMin, bodyRect.yMax + 5f, "A large enemy still needs its badges above its head.");
                Assert.GreaterOrEqual(badge.xMin, safe.xMin - 0.1f);
                Assert.LessOrEqual(badge.xMax, safe.xMax + 0.1f);
                Assert.LessOrEqual(badge.yMax, safe.yMax + 0.1f);
            }
            Assert.GreaterOrEqual(bodyRect.xMin, safe.xMin - 0.1f);
            Assert.LessOrEqual(bodyRect.xMax, safe.xMax + 0.1f);
            Assert.GreaterOrEqual(bodyRect.yMin, safe.yMin - 0.1f);
            Assert.That(enemy.SpeedButtons[0].transform.localPosition.x, Is.EqualTo(authoredX).Within(0.001f));
            Assert.AreEqual(actorPosition, enemy.transform.position);
            if (orthographic) Assert.Greater(overview.orthographicSize, normalSize);
            else Assert.Less(overview.transform.position.z, normalPosition.z);

            Vector3 fittedPosition = overview.transform.position;
            float fittedSize = overview.orthographicSize;
            for (int frame = 0; frame < 120; frame++) look.PrepareCamera(overview);
            Assert.That(Vector3.Distance(fittedPosition, overview.transform.position), Is.LessThan(0.001f));
            Assert.That(overview.orthographicSize, Is.EqualTo(fittedSize).Within(0.001f));
            Camera overlay = root.transform.Find("Battle World UI Camera").GetComponent<Camera>();
            Assert.AreEqual(overview.projectionMatrix, overlay.projectionMatrix);
            Assert.AreEqual(overview.transform.position, overlay.transform.position);

            // The same enemy changing to a small form must release the extra view space.
            framingEnemyData.enemyVisualScale = 0.25f;
            enemy.GetComponentInChildren<SpriteRenderer>().transform.localScale = new Vector3(0.25f, 0.25f, 1f);
            enemy.ApplyEnemyData(framingEnemyData);
            look.PrepareCamera(overview);
            Assert.That(Vector3.Distance(normalPosition, overview.transform.position), Is.LessThan(0.001f));
            Assert.That(overview.orthographicSize, Is.EqualTo(normalSize).Within(0.001f));
        }
        finally { SpeedButton.CombatLocked = previousLock; }
    }

    [Test]
    public void OverviewFramingSurvivesAspectChangesAndLeavesCombatCameraMotionIntact()
    {
        bool previousLock = SpeedButton.CombatLocked;
        try
        {
            SpeedButton.CombatLocked = false;
            ConfigureFramingCamera(2.444444f, false);
            BattleEnemyController enemy = CreateFramingEnemy();
            foreach (float aspect in new[] { 2.444444f, 1.333333f, 2.171946f })
            {
                overview.aspect = aspect;
                look.PrepareCamera(overview);
                Rect body = ProjectFramingBounds(enemy.GetComponentInChildren<SpriteRenderer>().bounds);
                Assert.Greater(ProjectFramingBounds(enemy.SpeedButtons[0].GetViewportBounds()).yMin, body.yMax + 5f);
            }
            Vector3 fittedPosition = overview.transform.position;
            SpeedButton.CombatLocked = true;
            overview.transform.position += Vector3.right * 0.3f;
            Vector3 impactPosition = overview.transform.position;
            look.PrepareCamera(overview);
            Assert.AreEqual(impactPosition, overview.transform.position);
            Vector3 closeupPosition = closeup.transform.position;
            float closeupSize = closeup.orthographicSize;
            look.PrepareCamera(closeup);
            Assert.AreEqual(closeupPosition, closeup.transform.position);
            Assert.AreEqual(closeupSize, closeup.orthographicSize);
            overview.transform.position = fittedPosition;
            SpeedButton.CombatLocked = false;
            look.ReleaseLook();
            Assert.That(overview.transform.position.z, Is.EqualTo(-24f).Within(0.001f));
        }
        finally { SpeedButton.CombatLocked = previousLock; }
    }

    [TestCase(1.777778f)]
    [TestCase(2.171946f)]
    [TestCase(2.444444f)]
    public void ThunderLordArtworkAndItsConfiguredNegativeOffsetKeepTheBadgesAboveTheHead(float aspect)
    {
        bool previousLock = SpeedButton.CombatLocked;
        try
        {
            SpeedButton.CombatLocked = false;
            ConfigureFramingCamera(aspect, false);
            EnemyData_SO source = AssetDatabase.LoadAssetAtPath<EnemyData_SO>("Assets/Data/Enemies/Enemies/011 ThunderLord/ThunderLord.asset");
            BattleEnemyController enemy = CreateFramingEnemy(source);
            look.PrepareCamera(overview);
            Assert.IsTrue(BattleActorVisualUtility.TryGetActorSpriteBounds(enemy.transform, out Bounds body));
            Rect bodyRect = ProjectFramingBounds(body);
            Rect safe = BattleEnemyController.GetSpeedButtonSafeArea(overview, Screen.safeArea);
            foreach (SpeedButton button in enemy.SpeedButtons)
            {
                Rect badge = ProjectFramingBounds(button.GetViewportBounds());
                Assert.Greater(badge.yMin, bodyRect.yMax + 5f);
                Assert.LessOrEqual(badge.yMax, safe.yMax + 0.1f);
            }
            float spacing = enemy.SpeedButtons[1].transform.localPosition.x - enemy.SpeedButtons[0].transform.localPosition.x;
            Assert.That(spacing, Is.EqualTo(0.82f).Within(0.001f));
        }
        finally { SpeedButton.CombatLocked = previousLock; }
    }

    [Test]
    public void CloseupKeepsForestForegroundInItsAuthoredPositionWhileTheCameraMoves()
    {
        look.ReleaseLook();
        SpriteRenderer midground = new GameObject("Forest midground").AddComponent<SpriteRenderer>();
        midground.transform.SetParent(root.transform, false);
        SpriteRenderer foreground = new GameObject("Left foreground foliage").AddComponent<SpriteRenderer>();
        foreground.transform.SetParent(root.transform, false);
        Vector3 authoredPosition = new Vector3(-8.3f, 2.7f, -8.4f);
        foreground.transform.localPosition = authoredPosition;
        foreground.sprite = sprite;
        foreground.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Scenes/SampleScene/Forest/Near Forest Foliage.mat");
        Set("midground", midground);
        look.InitializeLook();

        overview.aspect = closeup.aspect = 16f / 9f;
        closeup.orthographicSize = 5f;
        closeup.transform.position = new Vector3(-8f, 3f, -11f);
        look.PrepareCamera(overview);
        Vector3 overviewPosition = foreground.transform.localPosition;
        Assert.AreNotEqual(authoredPosition, overviewPosition);

        look.PrepareCamera(closeup);
        Assert.AreEqual(authoredPosition, foreground.transform.localPosition);
        closeup.transform.position += new Vector3(2f, -0.5f, 0f);
        closeup.orthographicSize = 6f;
        look.PrepareCamera(closeup);
        Assert.AreEqual(authoredPosition, foreground.transform.localPosition);

        look.PrepareCamera(overview);
        Assert.AreEqual(overviewPosition, foreground.transform.localPosition);
    }

    private void ConfigureFramingCamera(float aspect, bool orthographic)
    {
        overview.orthographic = orthographic;
        overview.transform.SetPositionAndRotation(new Vector3(0f, 3f, -24f), Quaternion.Euler(3f, 0f, 0f));
        framingTarget = new RenderTexture(Mathf.RoundToInt(1080f * aspect), 1080, 0);
        overview.targetTexture = framingTarget;
        overview.aspect = aspect;
    }

    private BattleEnemyController CreateFramingEnemy(EnemyData_SO source = null)
    {
        GameObject group = new GameObject("Framing enemies");
        group.transform.SetParent(root.transform, false);
        Set("enemies", group.transform);
        GameObject actor = new GameObject("Oversized enemy");
        actor.SetActive(false);
        actor.transform.SetParent(group.transform, false);
        actor.transform.position = new Vector3(6f, 0f, 0f);
        actor.AddComponent<PolygonCollider2D>();
        GameObject body = new GameObject("Body", typeof(SpriteRenderer));
        body.transform.SetParent(actor.transform, false);
        framingEnemyData = source != null ? Object.Instantiate(source) : ScriptableObject.CreateInstance<EnemyData_SO>();
        if (source == null)
        {
            framingEnemyData.enemySprite = sprite;
            framingEnemyData.enemyVisualScale = 9f;
            framingEnemyData.baseHP = 20;
            framingEnemyData.baseSpeedButtonCount = 2;
        }
        float scale = source != null ? 1.15f * source.enemyVisualScale : 9f;
        body.transform.localScale = new Vector3(scale, scale, 1f);
        body.GetComponent<SpriteRenderer>().sprite = framingEnemyData.enemySprite;
        BattleEnemyController enemy = actor.AddComponent<BattleEnemyController>();
        enemy.enemyData = framingEnemyData;
        var buttons = new System.Collections.Generic.List<SpeedButton>();
        for (int i = 0; i < 2; i++)
        {
            GameObject badge = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Button.prefab"), actor.transform);
            buttons.Add(badge.GetComponent<SpeedButton>());
        }
        enemy.RegisterSpeedButtons(buttons);
        enemy.ConfigureSpeedButtonLayout(buttons[0], SpeedButton.ButtonType.Enemy, new Vector2(0f, 0.8f), Vector2.zero, 0.82f);
        actor.SetActive(true);
        foreach (SpeedButton button in buttons) button.GetComponent<BattleSpeedButtonLook>().RefreshLook();
        return enemy;
    }

    private Rect ProjectFramingBounds(Bounds bounds)
    {
        Vector2 min = Vector2.one * float.PositiveInfinity, max = Vector2.one * float.NegativeInfinity;
        for (int corner = 0; corner < 8; corner++)
        {
            Vector3 screen = overview.WorldToScreenPoint(bounds.center + Vector3.Scale(bounds.extents,
                new Vector3((corner & 1) == 0 ? -1f : 1f, (corner & 2) == 0 ? -1f : 1f, (corner & 4) == 0 ? -1f : 1f)));
            min = Vector2.Min(min, screen);
            max = Vector2.Max(max, screen);
        }
        return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
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
