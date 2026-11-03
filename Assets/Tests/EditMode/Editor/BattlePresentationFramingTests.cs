using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
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

    [TestCase(0f)]
    [TestCase(0.5f)]
    [TestCase(1f)]
    public void EnemySpeedAnchorUsesTheAlignedSpriteHeightForEveryPivot(float pivotY)
    {
        GameObject root = new GameObject("Enemy speed anchor regression");
        root.SetActive(false);
        Texture2D texture = new Texture2D(2, 20);
        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, 2, 20), new Vector2(0.5f, pivotY), 2f);
        EnemyData_SO data = ScriptableObject.CreateInstance<EnemyData_SO>();
        try
        {
            data.enemySprite = sprite;
            data.enemyVisualScale = 1.3f;
            data.enemyVisualOffset = Vector3.up * 0.4f;
            data.baseSpeedButtonCount = 1;
            BattleEnemyController enemy = root.AddComponent<BattleEnemyController>();
            enemy.enemyData = data;
            GameObject badge = new GameObject("Speed badge", typeof(BoxCollider2D), typeof(SpeedButton));
            badge.transform.SetParent(root.transform, false);
            enemy.RegisterSpeedButtons(new[] { badge.GetComponent<SpeedButton>() });
            Assert.That(badge.transform.localPosition.y, Is.EqualTo(16.35f).Within(0.001f),
                "The bottom alignment offset must not be added a second time.");
        }
        finally
        {
            Object.DestroyImmediate(root); Object.DestroyImmediate(data);
            Object.DestroyImmediate(sprite); Object.DestroyImmediate(texture);
        }
    }

    [TestCase(1.333333f, false)]
    [TestCase(2.4f, false)]
    [TestCase(1.333333f, true)]
    [TestCase(2.4f, true)]
    public void LargeEnemySpeedGroupFitsSafeAreaAndRecoversItsAnchorAfterZoom(float aspect, bool orthographic)
    {
        GameObject root = new GameObject("Large enemy speed viewport regression");
        root.SetActive(false);
        GameObject cameraObject = new GameObject("Speed viewport camera");
        Texture2D texture = new Texture2D(2, 40);
        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, 2, 40), Vector2.one * 0.5f, 2f);
        EnemyData_SO data = ScriptableObject.CreateInstance<EnemyData_SO>();
        RenderTexture target = new RenderTexture(Mathf.RoundToInt(1080f * aspect), 1080, 0);
        bool previousLock = SpeedButton.CombatLocked;
        try
        {
            SpeedButton.CombatLocked = false;
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.targetTexture = target;
            camera.transform.position = new Vector3(0f, 3f, -24f);
            camera.orthographic = orthographic;
            camera.orthographicSize = 6f;
            camera.fieldOfView = 28f;
            camera.pixelRect = new Rect(0f, 0f, 1080f * aspect, 1080f);
            camera.aspect = aspect;
            Rect safe = new Rect(90f, 20f, camera.pixelWidth - 150f, 1040f);
            data.enemySprite = sprite;
            data.baseSpeedButtonCount = 3;
            data.baseHP = 20;
            BattleEnemyController enemy = root.AddComponent<BattleEnemyController>();
            enemy.enemyData = data;
            var buttons = new System.Collections.Generic.List<SpeedButton>();
            for (int i = 0; i < 3; i++)
            {
                GameObject badge = new GameObject("Badge " + i, typeof(BoxCollider2D), typeof(SpeedButton));
                badge.transform.SetParent(root.transform, false);
                badge.GetComponent<BoxCollider2D>().size = new Vector2(0.7f, 0.8f);
                buttons.Add(badge.GetComponent<SpeedButton>());
            }
            enemy.RegisterSpeedButtons(buttons);
            root.transform.position = new Vector3(14f, 0f, 0f);
            root.SetActive(true);
            Vector3 actorPosition = root.transform.position;
            enemy.FitSpeedButtonsToCamera(camera, safe);
            foreach (SpeedButton button in buttons)
            {
                Bounds bounds = button.GetViewportBounds();
                foreach (float x in new[] { bounds.min.x, bounds.max.x })
                foreach (float y in new[] { bounds.min.y, bounds.max.y })
                {
                    Vector3 screen = camera.WorldToScreenPoint(new Vector3(x, y, bounds.center.z));
                    Assert.That(screen.x, Is.InRange(safe.xMin + 17f, safe.xMax - 17f));
                    Assert.That(screen.y, Is.InRange(safe.yMin + 17f, safe.yMax - 128f));
                }
            }
            float groupSpacing = buttons[1].transform.localPosition.x - buttons[0].transform.localPosition.x;
            Assert.That(groupSpacing, Is.EqualTo(0.82f).Within(0.001f));
            Assert.AreEqual(actorPosition, root.transform.position);
            enemy.FitSpeedButtonsToCamera(camera, safe);
            Assert.That(buttons[1].transform.localPosition.x - buttons[0].transform.localPosition.x,
                Is.EqualTo(groupSpacing).Within(0.001f), "Repeated fitting must not accumulate drift.");
            camera.orthographicSize = 30f;
            camera.fieldOfView = 110f;
            enemy.FitSpeedButtonsToCamera(camera, safe);
            Assert.That(buttons[0].transform.localPosition.y, Is.EqualTo(24f).Within(0.001f));
        }
        finally
        {
            SpeedButton.CombatLocked = previousLock;
            Object.DestroyImmediate(root); Object.DestroyImmediate(cameraObject);
            Object.DestroyImmediate(target);
            Object.DestroyImmediate(data); Object.DestroyImmediate(sprite); Object.DestroyImmediate(texture);
        }
    }

    [TestCase(1.777778f, false, false)]
    [TestCase(1.777778f, true, false)]
    [TestCase(1.333333f, false, true)]
    [TestCase(2.4f, false, true)]
    [TestCase(1.333333f, true, true)]
    [TestCase(2.4f, true, true)]
    public void EnemySpeedHalosClearTheRenderedBodyWithNegativeOffsets(float aspect, bool orthographic, bool tall)
    {
        GameObject root = new GameObject("Enemy badge clearance regression");
        root.SetActive(false);
        GameObject cameraObject = new GameObject("Badge clearance camera");
        Texture2D texture = new Texture2D(6, tall ? 20 : 5);
        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.one * 0.5f, 1f);
        EnemyData_SO data = ScriptableObject.CreateInstance<EnemyData_SO>();
        RenderTexture target = new RenderTexture(Mathf.RoundToInt(1080f * aspect), 1080, 0);
        bool previousLock = SpeedButton.CombatLocked;
        try
        {
            SpeedButton.CombatLocked = false;
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.targetTexture = target;
            camera.transform.position = new Vector3(0f, 3f, -24f);
            camera.orthographic = orthographic;
            camera.orthographicSize = 6f;
            camera.fieldOfView = 28f;
            camera.aspect = aspect;
            Rect safe = new Rect(90f, 20f, camera.pixelWidth - 150f, 1040f);
            root.AddComponent<PolygonCollider2D>();
            SpriteRenderer body = new GameObject("Rendered body", typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
            body.transform.SetParent(root.transform, false);
            body.sprite = sprite;
            data.enemySprite = sprite;
            data.baseSpeedButtonCount = 3;
            data.speedButtonOffset = new Vector2(0f, -3f);
            data.baseHP = 20;
            BattleEnemyController enemy = root.AddComponent<BattleEnemyController>();
            enemy.enemyData = data;
            var buttons = new System.Collections.Generic.List<SpeedButton>();
            for (int i = 0; i < 3; i++)
            {
                GameObject badge = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Button.prefab"), root.transform);
                buttons.Add(badge.GetComponent<SpeedButton>());
            }
            enemy.RegisterSpeedButtons(buttons);
            root.transform.position = new Vector3(4f, 0f, 0f);
            root.SetActive(true);
            camera.transform.rotation = Quaternion.Euler(3f, 0f, 0f);
            body.transform.localPosition += Vector3.up;
            foreach (SpeedButton button in buttons)
                button.GetComponent<BattleSpeedButtonLook>().RefreshLook();
            Vector3 actorPosition = root.transform.position;
            enemy.FitSpeedButtonsToCamera(camera, safe);
            Rect bodyRect = ProjectBounds(camera, body.bounds);
            Rect group = ProjectBounds(camera, buttons[0].GetViewportBounds());
            foreach (SpeedButton button in buttons)
            {
                Rect badge = ProjectBounds(camera, button.GetViewportBounds());
                group = Rect.MinMaxRect(Mathf.Min(group.xMin, badge.xMin), Mathf.Min(group.yMin, badge.yMin),
                    Mathf.Max(group.xMax, badge.xMax), Mathf.Max(group.yMax, badge.yMax));
                Assert.That(badge.xMin, Is.GreaterThanOrEqualTo(safe.xMin + 17f));
                Assert.That(badge.xMax, Is.LessThanOrEqualTo(safe.xMax - 17f));
                Assert.That(badge.yMin, Is.GreaterThanOrEqualTo(safe.yMin + 17f));
                Assert.That(badge.yMax, Is.LessThanOrEqualTo(safe.yMax - 128f));
            }
            Rect clearance = Rect.MinMaxRect(bodyRect.xMin - 5f, bodyRect.yMin - 5f, bodyRect.xMax + 5f, bodyRect.yMax + 5f);
            Assert.IsFalse(group.Overlaps(clearance), "The complete halo group must leave a visible gap around the body.");
            if (!tall)
                Assert.Greater(group.yMin, bodyRect.yMax + 5f, "Use the head clearance whenever it is available.");
            else
                Assert.IsTrue(group.xMax < bodyRect.xMin || group.xMin > bodyRect.xMax, "A tall body needs side clearance.");
            Vector3 fitted = buttons[0].transform.position;
            for (int frame = 0; frame < 120; frame++)
                enemy.FitSpeedButtonsToCamera(camera, safe);
            Assert.That(Vector3.Distance(fitted, buttons[0].transform.position), Is.LessThan(0.001f));
            Assert.That(buttons[0].transform.localPosition.z, Is.EqualTo(0f).Within(0.001f));
            Assert.That(buttons[1].transform.localPosition.x - buttons[0].transform.localPosition.x, Is.EqualTo(0.82f).Within(0.001f));
            Assert.AreEqual(actorPosition, root.transform.position);
        }
        finally
        {
            SpeedButton.CombatLocked = previousLock;
            Object.DestroyImmediate(root); Object.DestroyImmediate(cameraObject); Object.DestroyImmediate(target);
            Object.DestroyImmediate(data); Object.DestroyImmediate(sprite); Object.DestroyImmediate(texture);
        }
    }

    private static Rect ProjectBounds(Camera camera, Bounds bounds)
    {
        Vector2 min = Vector2.one * float.PositiveInfinity, max = Vector2.one * float.NegativeInfinity;
        for (int corner = 0; corner < 8; corner++)
        {
            Vector3 point = camera.WorldToScreenPoint(bounds.center + Vector3.Scale(bounds.extents,
                new Vector3((corner & 1) == 0 ? -1f : 1f, (corner & 2) == 0 ? -1f : 1f, (corner & 4) == 0 ? -1f : 1f)));
            min = Vector2.Min(min, point); max = Vector2.Max(max, point);
        }
        return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
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
