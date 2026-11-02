using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class BattlePresentationFramingTests
{
    [TestCase(1.333333f, 2f, 7f)]
    [TestCase(1.777778f, 2f, 7f)]
    [TestCase(2.222222f, 2f, 7f)]
    [TestCase(1.333333f, 6f, 2f)]
    [TestCase(2.222222f, 3f, 3f)]
    public void PerspectiveCloseupKeepsBothWholeActorsInsideTheFrame(float aspect, float allyHeight, float enemyHeight)
    {
        GameObject root = new GameObject("Closeup framing test");
        Texture2D texture = new Texture2D(2, 2);
        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, 2, 2), Vector2.one * 0.5f, 2f);
        try
        {
            BattlePresentationSystem presentation = root.AddComponent<BattlePresentationSystem>();
            Camera overview = CreateCamera(root.transform, "Overview");
            Camera closeup = CreateCamera(root.transform, "Closeup");
            closeup.aspect = aspect;
            Set(presentation, "battleCamera", overview);
            Set(presentation, "closeupCamera", closeup);
            Set(presentation, "closeupCameraOffset", new Vector3(0f, 2.2f, -10f));
            Set(presentation, "cameraFocusOffset", new Vector3(0f, -0.15f, 0f));
            Set(presentation, "closeupLookOffset", Vector3.zero);
            Set(presentation, "closeupLargeTargetVerticalBias", 0f);
            SpriteRenderer ally = CreateActor(root.transform, sprite, -2f, 1.8f, allyHeight);
            SpriteRenderer enemy = CreateActor(root.transform, sprite, 2f, 3.5f, enemyHeight);

            IEnumerator shot = (IEnumerator)typeof(BattlePresentationSystem)
                .GetMethod("RunCloseupShot", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(presentation, new object[] { 0, ally.transform, enemy.transform, Vector3.zero, 5f });
            Assert.IsTrue(shot.MoveNext());

            foreach (SpriteRenderer actor in new[] { ally, enemy })
            {
                Bounds bounds = actor.bounds;
                foreach (float x in new[] { bounds.min.x, bounds.max.x })
                foreach (float y in new[] { bounds.min.y, bounds.max.y })
                {
                    Vector3 viewport = closeup.WorldToViewportPoint(new Vector3(x, y, bounds.center.z));
                    Assert.That(viewport.x, Is.InRange(0.04f, 0.96f), actor.name + " horizontal clipping");
                    Assert.That(viewport.y, Is.InRange(0.12f, 0.90f), actor.name + " head/foot clipping");
                    Assert.Greater(viewport.z, closeup.nearClipPlane);
                }
            }
        }
        finally
        {
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(sprite);
            Object.DestroyImmediate(texture);
        }
    }

    private static Camera CreateCamera(Transform parent, string name)
    {
        GameObject item = new GameObject(name);
        item.transform.SetParent(parent);
        return item.AddComponent<Camera>();
    }

    private static SpriteRenderer CreateActor(Transform parent, Sprite sprite, float x, float width, float height)
    {
        GameObject item = new GameObject("Actor " + x);
        item.transform.SetParent(parent);
        item.transform.position = new Vector3(x, height * 0.5f, 0f);
        item.transform.localScale = new Vector3(width, height, 1f);
        SpriteRenderer renderer = item.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        return renderer;
    }

    private static void Set(BattlePresentationSystem target, string name, object value)
    {
        typeof(BattlePresentationSystem).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    }
}
