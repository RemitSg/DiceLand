using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class BattleForestForegroundFramingTests
{
    private GameObject root, cameraObject;
    private Texture2D texture;
    private Sprite sprite;
    private Material near, ordinary;
    private SpriteRenderer left, right, tree;
    private Camera camera;
    private readonly BattleForestForegroundFraming framing = new BattleForestForegroundFraming();

    [SetUp] public void SetUp()
    {
        root = new GameObject("Foreground framing fixture");
        cameraObject = new GameObject("Foreground framing camera"); camera = cameraObject.AddComponent<Camera>();
        texture = new Texture2D(16, 8);
        sprite = Sprite.Create(texture, new Rect(0, 0, 16, 8), new Vector2(.3f, .65f), 2f, 0, SpriteMeshType.FullRect);
        near = new Material(AssetDatabase.LoadAssetAtPath<Material>("Assets/Scenes/SampleScene/Forest/Near Forest Foliage.mat"));
        ordinary = new Material(Shader.Find("DiceLand/Battle Forest Foliage"));
        left = Card("Left", new Vector3(-8.3f, 2.7f, -8.4f), new Vector3(-.9f, .9f, 1)); left.flipY = true;
        right = Card("Right", new Vector3(9.4f, 1.9f, -8.4f), new Vector3(.72f, -.72f, 1)); right.flipX = true;
        tree = Card("Midground tree", new Vector3(0, 4, 18), Vector3.one); tree.sharedMaterial = ordinary;
        camera.nearClipPlane = .1f; camera.farClipPlane = 200f; camera.fieldOfView = 35f;
    }

    [TearDown] public void TearDown()
    {
        framing.Release(); Object.DestroyImmediate(root); Object.DestroyImmediate(cameraObject);
        Object.DestroyImmediate(sprite); Object.DestroyImmediate(texture); Object.DestroyImmediate(near); Object.DestroyImmediate(ordinary);
    }

    [TestCase(1.333333f, false, 0f)]
    [TestCase(2.171053f, false, 12f)]
    [TestCase(2.444444f, false, -12f)]
    [TestCase(1.333333f, true, 0f)]
    [TestCase(2.444444f, true, 12f)]
    public void PulledBackShotsKeepCardEdgesOutsideTheViewWithoutStretchingOrDrifting(float aspect, bool orthographic, float offsetX)
    {
        root.transform.SetPositionAndRotation(new Vector3(2, 0, 0), Quaternion.Euler(0, 8, 6));
        root.transform.localScale = new Vector3(1.2f, .9f, 1);
        camera.transform.SetPositionAndRotation(new Vector3(offsetX, 10.3f, -61f), Quaternion.Euler(5, 0, 7));
        camera.aspect = aspect; camera.orthographic = orthographic; camera.orthographicSize = 16f;
        Vector3 leftStart = left.transform.localPosition, rightStart = right.transform.localPosition;
        Vector3 leftScale = left.transform.localScale, rightScale = right.transform.localScale;
        Quaternion leftRotation = left.transform.localRotation, rightRotation = right.transform.localRotation;
        Vector3 treeStart = tree.transform.localPosition, cameraStart = camera.transform.position;
        Vector2[] vertices = sprite.vertices;
        framing.Initialize(root.transform); framing.PrepareCamera(camera);
        AssertOutsideEdges(left, true); AssertOutsideEdges(right, false);
        Vector3 fittedLeft = left.transform.localPosition, fittedRight = right.transform.localPosition;
        for (int frame = 0; frame < 120; frame++) framing.PrepareCamera(camera);
        Assert.That(Vector3.Distance(fittedLeft, left.transform.localPosition), Is.LessThan(.0001f));
        Assert.That(Vector3.Distance(fittedRight, right.transform.localPosition), Is.LessThan(.0001f));
        Assert.AreEqual(leftScale, left.transform.localScale); Assert.AreEqual(rightScale, right.transform.localScale);
        Assert.AreEqual(leftRotation, left.transform.localRotation); Assert.AreEqual(rightRotation, right.transform.localRotation);
        Assert.AreEqual(treeStart, tree.transform.localPosition); Assert.AreEqual(cameraStart, camera.transform.position);
        CollectionAssert.AreEqual(vertices, sprite.vertices);
        framing.Release();
        Assert.AreEqual(leftStart, left.transform.localPosition); Assert.AreEqual(rightStart, right.transform.localPosition);
    }

    [Test] public void AlreadyCoveredViewsKeepTheirAuthoredLayoutAndCameraSwitchesRestoreIt()
    {
        Vector3 a = left.transform.localPosition, b = right.transform.localPosition;
        camera.transform.position = new Vector3(0, 3, -12); camera.aspect = 1.777778f;
        framing.Initialize(root.transform); framing.PrepareCamera(camera);
        Assert.AreEqual(a, left.transform.localPosition); Assert.AreEqual(b, right.transform.localPosition);
        camera.transform.position = new Vector3(0, 10, -61); framing.PrepareCamera(camera);
        Assert.AreNotEqual(a, left.transform.localPosition);
        camera.transform.position = new Vector3(0, 3, 5); framing.PrepareCamera(camera);
        Assert.AreEqual(a, left.transform.localPosition); Assert.AreEqual(b, right.transform.localPosition);
        camera.transform.position = new Vector3(0, 10, -61); framing.PrepareCamera(camera);
        Vector3 external = new Vector3(1, 2, 3); left.transform.localPosition = external;
        framing.Release();
        Assert.AreEqual(external, left.transform.localPosition); Assert.AreEqual(b, right.transform.localPosition);
    }

    private SpriteRenderer Card(string name, Vector3 position, Vector3 scale)
    {
        SpriteRenderer renderer = new GameObject(name).AddComponent<SpriteRenderer>();
        renderer.transform.SetParent(root.transform, false); renderer.transform.localPosition = position;
        renderer.transform.localScale = scale; renderer.sprite = sprite; renderer.sharedMaterial = near;
        return renderer;
    }

    private void AssertOutsideEdges(SpriteRenderer renderer, bool onLeft)
    {
        Vector3[] corners = renderer.sprite.vertices.Select(v => camera.WorldToViewportPoint(renderer.transform.TransformPoint(
            new Vector3(renderer.flipX ? -v.x : v.x, renderer.flipY ? -v.y : v.y, 0f)))).ToArray();
        Assert.IsTrue(corners.All(p => p.z > camera.nearClipPlane));
        Assert.IsTrue(corners.OrderBy(p => p.y).Take(2).All(p => p.y < -.02f), "The whole lower edge must stay outside the view.");
        Assert.IsTrue((onLeft ? corners.OrderBy(p => p.x) : corners.OrderByDescending(p => p.x)).Take(2)
            .All(p => onLeft ? p.x < -.02f : p.x > 1.02f), "The outer edge must stay outside the view.");
    }
}
