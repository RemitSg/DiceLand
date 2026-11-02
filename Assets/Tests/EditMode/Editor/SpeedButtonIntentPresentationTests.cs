using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class SpeedButtonIntentPresentationTests
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private readonly List<SpeedButton> buttons = new List<SpeedButton>();
    private GameObject cameraObject;
    private bool previousCombatLock;
    private bool previousInputSuppression;

    [SetUp]
    public void SetUp()
    {
        previousCombatLock = SpeedButton.CombatLocked;
        previousInputSuppression = SpeedButton.InputSuppressed;
        SpeedButton.CombatLocked = false;
        SpeedButton.InputSuppressed = false;
    }

    [TearDown]
    public void TearDown()
    {
        foreach (SpeedButton button in buttons)
        {
            if (button == null) continue;
            foreach (string name in new[] { "arrowObj", "dashTexture", "solidTexture", "curveRuntimeMaterial",
                "hoverOutlineMaterial", "arrowMesh", "arrowMaterial" })
            {
                FieldInfo field = typeof(SpeedButton).GetField(name, Private);
                Object value = field.GetValue(button) as Object;
                field.SetValue(button, null);
                if (value != null) Object.DestroyImmediate(value);
            }
            Object.DestroyImmediate(button.gameObject);
        }
        buttons.Clear();
        if (cameraObject != null) Object.DestroyImmediate(cameraObject);
        SpeedButton.CombatLocked = previousCombatLock;
        SpeedButton.InputSuppressed = previousInputSuppression;
    }

    [TestCase(false)]
    [TestCase(true)]
    public void ConnectedIntentsRemainSolidWhenCurveRenderingIsReinitialized(bool contested)
    {
        SpeedButton enemy = Create("Enemy intent", SpeedButton.ButtonType.Enemy, new Vector3(4f, 3f, 0f));
        SpeedButton ally = Create("Ally target", SpeedButton.ButtonType.Ally, new Vector3(-4f, 2f, 0f));
        enemy.EnemyDrawLineTo(ally);
        if (contested)
        {
            ally.IsInteractable = true;
            ally.Speed = 10;
            Assert.IsTrue(ally.TryAssignTarget(enemy));
        }
        LineRenderer line = enemy.GetComponent<LineRenderer>();
        Color color = line.startColor;
        Invoke(enemy, "SetupCurveRenderingStyle");
        Invoke(enemy, "SetupCurveRenderingStyle");
        Assert.AreSame(ally, enemy.ConnectedTarget);
        Assert.AreEqual(1, line.sharedMaterial.mainTexture.width, "Committed intents must keep their solid texture after validation/reload setup.");
        Assert.AreEqual(LineTextureMode.Stretch, line.textureMode);
        Assert.AreEqual(color, line.startColor);
        Assert.That(line.widthMultiplier, Is.EqualTo(0.1f).Within(0.0001f));
    }

    [Test]
    public void UncommittedDragPreviewStillUsesItsDashedTexture()
    {
        SpeedButton ally = Create("Uncommitted preview", SpeedButton.ButtonType.Ally, Vector3.zero);
        ally.StartCurve();
        Invoke(ally, "SetupCurveRenderingStyle");
        LineRenderer line = ally.GetComponent<LineRenderer>();
        Assert.IsNull(ally.ConnectedTarget);
        Assert.AreEqual(LineTextureMode.Tile, line.textureMode);
        Assert.Greater(line.sharedMaterial.mainTexture.width, 1);
    }

    [Test]
    public void ArrowDrawUsesTheOwnersCurrentCameraLayerWithoutJoiningItsSortingGroup()
    {
        SpeedButton enemy = Create("Enemy with UI layer", SpeedButton.ButtonType.Enemy, new Vector3(4f, 3f, 1f));
        SpeedButton ally = Create("Target", SpeedButton.ButtonType.Ally, new Vector3(-3f, 2f, 0f));
        enemy.gameObject.layer = 5;
        enemy.EnemyDrawLineTo(ally);
        GameObject arrow = (GameObject)typeof(SpeedButton).GetField("arrowObj", Private).GetValue(enemy);
        Assert.IsNotNull(arrow);
        Assert.AreEqual(5, arrow.layer);
        Assert.IsNull(arrow.transform.parent, "Detached arrows must retain their global SortingGroup ordering.");
        enemy.gameObject.layer = 9;
        Invoke(enemy, "DrawConnectedCurve", Color.red);
        Assert.AreEqual(9, arrow.layer);
        Assert.AreSame(ally, enemy.ConnectedTarget);
    }

    [TestCase(false, 1.777778f)]
    [TestCase(false, 2.222222f)]
    [TestCase(false, 1.333333f)]
    [TestCase(true, 1.777778f)]
    [TestCase(true, 2.222222f)]
    [TestCase(true, 1.333333f)]
    public void CommittedCurvesStayVisibleAfterAViewOrLensChange(bool orthographic, float aspect)
    {
        cameraObject = new GameObject("Intent framing camera", typeof(Camera));
        Camera camera = cameraObject.GetComponent<Camera>();
        camera.enabled = false;
        camera.transform.position = new Vector3(0f, 0f, -10f);
        camera.aspect = aspect;
        camera.fieldOfView = 40f;
        camera.orthographic = orthographic;
        camera.orthographicSize = 5f;
        SpeedButton enemy = Create("High enemy slot", SpeedButton.ButtonType.Enemy,
            camera.ViewportToWorldPoint(new Vector3(0.85f, 0.84f, 12f)));
        SpeedButton ally = Create("Lower ally slot", SpeedButton.ButtonType.Ally,
            camera.ViewportToWorldPoint(new Vector3(0.15f, 0.45f, 12f)));
        typeof(SpeedButton).GetField("controlPointOffset", Private).SetValue(enemy, 6f);
        enemy.EnemyDrawLineTo(ally);
        enemy.RefreshIntentForCamera(camera);
        AssertCurveVisible(enemy, ally, camera);
        LineRenderer line = enemy.GetComponent<LineRenderer>();
        Vector3 originalMiddle = line.GetPosition(line.positionCount / 2);
        camera.fieldOfView = 32f;
        camera.orthographicSize = 4f;
        enemy.RefreshIntentForCamera(camera);
        AssertCurveVisible(enemy, ally, camera);
        Assert.Greater(Vector3.Distance(originalMiddle, line.GetPosition(line.positionCount / 2)), 0.01f);
        Assert.AreSame(ally, enemy.ConnectedTarget);
        Assert.AreEqual(1, line.sharedMaterial.mainTexture.width);
        Assert.IsTrue(enemy.ArrowVisual.activeInHierarchy);
    }

    private static void AssertCurveVisible(SpeedButton enemy, SpeedButton ally, Camera camera)
    {
        LineRenderer line = enemy.GetComponent<LineRenderer>();
        Assert.That(Vector3.Distance(line.GetPosition(0), enemy.transform.position), Is.LessThan(0.001f));
        Assert.That(Vector3.Distance(line.GetPosition(line.positionCount - 1), ally.transform.position), Is.LessThan(0.001f));
        for (int i = 0; i < line.positionCount; i++)
        {
            Vector3 view = camera.WorldToViewportPoint(line.GetPosition(i));
            Assert.That(view.x, Is.InRange(-0.001f, 1.001f));
            Assert.That(view.y, Is.InRange(-0.001f, 1.001f));
        }
    }

    private SpeedButton Create(string name, SpeedButton.ButtonType type, Vector3 position)
    {
        GameObject root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Button.prefab"));
        root.name = name;
        root.transform.position = position;
        SpeedButton button = root.GetComponent<SpeedButton>();
        buttons.Add(button);
        if (typeof(SpeedButton).GetField("lineRenderer", Private).GetValue(button) == null)
            Invoke(button, "Awake");
        button.SetButtonType(type);
        return button;
    }

    private static void Invoke(SpeedButton button, string name, params object[] arguments)
    {
        typeof(SpeedButton).GetMethod(name, Private).Invoke(button, arguments);
    }
}
