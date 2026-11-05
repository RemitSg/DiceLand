using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

public sealed class BattleDeathCameraTests
{
    private GameObject host, actor;
    private Camera camera;
    private BattleDeathCameraMotion motion;
    private Texture2D texture;
    private Sprite sprite;
    private readonly List<BattleDeathVfx> effects = new List<BattleDeathVfx>();

    [SetUp]
    public void SetUp()
    {
        host = new GameObject("Death camera fixture"); camera = host.AddComponent<Camera>();
        camera.transform.position = new Vector3(2, 1, -8); camera.fieldOfView = 50; camera.aspect = 16f / 9;
        actor = new GameObject("Death camera actor"); actor.transform.position = new Vector3(2, 1, 0);
        texture = new Texture2D(8, 16); var pixels = new Color[128]; for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
        texture.SetPixels(pixels); texture.Apply(); sprite = Sprite.Create(texture, new Rect(0, 0, 8, 16), Vector2.one * 0.5f, 8);
        actor.AddComponent<SpriteRenderer>().sprite = sprite;
        motion = host.AddComponent<BattleDeathCameraMotion>(); motion.Configure(() => camera);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(host);
        foreach (BattleDeathVfx effect in effects) if (effect != null) Object.DestroyImmediate(effect.gameObject);
        effects.Clear(); Object.DestroyImmediate(actor); Object.DestroyImmediate(sprite); Object.DestroyImmediate(texture);
    }

    private BattleDeathVfx Spawn(Vector3 direction)
    {
        BattleDeathVfx effect = BattleDeathVfx.Spawn(actor.transform, direction, true, 420, 559);
        Assert.NotNull(effect); effects.Add(effect); motion.Play(effect); return effect;
    }

    private void Frame(float delta)
    {
        motion.RestoreBasePose();
        foreach (BattleDeathVfx effect in effects) if (effect != null) effect.Advance(delta);
        motion.Advance(delta); motion.ApplyPose();
    }

    private void AssertInside(Bounds bounds)
    {
        for (int i = 0; i < 8; i++)
        {
            Vector3 corner = bounds.center + Vector3.Scale(bounds.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
            Vector3 view = camera.WorldToViewportPoint(corner);
            Assert.Greater(view.z, camera.nearClipPlane);
            Assert.That(view.x, Is.InRange(0.074f, 0.926f), "Effect clipped horizontally");
            Assert.That(view.y, Is.InRange(0.074f, 0.926f), "Effect clipped vertically");
        }
    }

    [Test]
    public void LethalBeatPushesInAndRollsThenRestoresTheExactBaseWithoutDrift()
    {
        Vector3 position = camera.transform.position; Quaternion rotation = camera.transform.rotation; float lens = camera.fieldOfView;
        Spawn(Vector3.right); Frame(0.1f);
        Assert.Greater(Vector3.Distance(position, camera.transform.position), 0.01f);
        Assert.Greater(Quaternion.Angle(rotation, camera.transform.rotation), 0.1f);
        Assert.Less(camera.fieldOfView, lens);
        Vector3 once = camera.transform.position; float onceLens = camera.fieldOfView;
        motion.ApplyPose(); Assert.Less(Vector3.Distance(once, camera.transform.position), 0.0001f); Assert.AreEqual(onceLens, camera.fieldOfView, 0.0001f);
        Frame(3); Frame(BattleDeathCameraMotion.RecoveryDuration + 0.01f);
        Assert.IsFalse(motion.IsActive); Assert.Less(Vector3.Distance(position, camera.transform.position), 0.0001f);
        Assert.Less(Quaternion.Angle(rotation, camera.transform.rotation), 0.001f); Assert.AreEqual(lens, camera.fieldOfView, 0.0001f);
    }

    [TestCase(false, 1.333333f)] [TestCase(false, 1.777778f)] [TestCase(false, 2.166667f)]
    [TestCase(true, 1.333333f)] [TestCase(true, 1.777778f)] [TestCase(true, 2.166667f)]
    public void EveryVisibleFragmentAndTrailFitsForTheEntireBurst(bool orthographic, float aspect)
    {
        camera.orthographic = orthographic; camera.orthographicSize = 1.2f; camera.aspect = aspect;
        camera.transform.SetPositionAndRotation(new Vector3(0, 1.7f, -4), Quaternion.Euler(9, 0, 0)); camera.fieldOfView = 25;
        BattleDeathVfx effect = Spawn(Vector3.right);
        for (int frame = 0; frame < 90; frame++)
        {
            Frame(1f / 30);
            if (effect.HasVisibleGeometry) AssertInside(effect.VisualBounds);
        }
        Assert.IsTrue(effect.IsComplete); Assert.IsFalse(motion.IsActive);
    }

    [Test]
    public void ConsecutiveKillsShareOneContinuousEnvelopeAndWaitForTheLastTail()
    {
        BattleDeathVfx first = Spawn(Vector3.right); Frame(0.2f);
        actor.transform.position = new Vector3(-3, 1, 0);
        BattleDeathVfx second = Spawn(Vector3.left);
        Assert.IsTrue(motion.IsActive); Assert.AreEqual(1, host.GetComponents<BattleDeathCameraMotion>().Length);
        Frame(0.1f); AssertInside(first.VisualBounds); AssertInside(second.VisualBounds);
        first.Advance(3); Frame(0.1f); Assert.IsTrue(motion.IsActive, "The second kill still owns a visible tail");
        second.Advance(3); Frame(0.1f); Assert.IsTrue(motion.IsActive, "Return smoothly after the last tail");
        Frame(0.4f); Assert.IsFalse(motion.IsActive);
    }

    [Test]
    public void RecoveryBeginsContinuouslyFromTheFittedLensAndDolly()
    {
        camera.transform.position = new Vector3(0, 1.7f, -4); camera.fieldOfView = 25; camera.aspect = 4f / 3;
        BattleDeathVfx effect = Spawn(Vector3.right);
        for (int frame = 0; frame < 105; frame++) Frame(0.02f);
        Vector3 fittedPosition = camera.transform.position; float fittedLens = camera.fieldOfView;
        Assert.Greater(fittedLens, 30, "The burst should have widened the original lens");
        motion.RestoreBasePose(); effect.Advance(3f); motion.Advance(0.001f); motion.ApplyPose();
        Assert.IsTrue(motion.IsActive);
        Assert.AreEqual(fittedLens, camera.fieldOfView, 0.05f, "Recovery must not drop the fitted lens immediately");
        Assert.Less(Vector3.Distance(fittedPosition, camera.transform.position), 0.02f, "Recovery must not drop the fitted dolly immediately");
        Frame(0.4f); Assert.IsFalse(motion.IsActive); Assert.AreEqual(25, camera.fieldOfView, 0.0001f);
    }

    [Test]
    public void DisableRestoresThePoseAndNeverChangesBattleTime()
    {
        Vector3 position = camera.transform.position; float lens = camera.fieldOfView;
        float timeScale = Time.timeScale, step = Time.fixedDeltaTime;
        Spawn(Vector3.left); Frame(0.15f); motion.enabled = false;
        Assert.IsFalse(motion.IsActive); Assert.AreEqual(position, camera.transform.position); Assert.AreEqual(lens, camera.fieldOfView);
        Assert.AreEqual(timeScale, Time.timeScale); Assert.AreEqual(step, Time.fixedDeltaTime);
    }
}
