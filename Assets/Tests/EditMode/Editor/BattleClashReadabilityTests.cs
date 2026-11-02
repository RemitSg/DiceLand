using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class BattleClashReadabilityTests
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

    [TestCase(400f, 1800f)]
    [TestCase(884f, 1920f)]
    [TestCase(1080f, 2400f)]
    [TestCase(1200f, 1600f)]
    public void DiceScreenHeightDoesNotDependOnCameraOrCanvasAspect(float height, float width)
    {
        GameObject owner = new GameObject("Dice typography test");
        owner.SetActive(false);
        try
        {
            BattleFeedbackSystem feedback = owner.AddComponent<BattleFeedbackSystem>();
            Camera overview = Child(owner, "Overview").AddComponent<Camera>();
            Camera closeup = Child(owner, "Closeup").AddComponent<Camera>();
            overview.enabled = closeup.enabled = true;
            RectTransform overlay = Child(owner, "Overlay", typeof(RectTransform)).GetComponent<RectTransform>();
            overlay.sizeDelta = new Vector2(width, height);
            RectTransform label = Child(owner, "Dice", typeof(RectTransform)).GetComponent<RectTransform>();
            label.SetParent(overlay, false);
            label.sizeDelta = new Vector2(326f, 183f);
            Transform actor = Child(owner, "Actor").transform;
            actor.position = Vector3.forward * 10f;
            Set(feedback, "overlayRoot", overlay);
            MethodInfo update = typeof(BattleFeedbackSystem).GetMethod("UpdateFloatingElementPosition", Private);

            foreach (float fov in new[] { 18f, 28f, 65f })
            foreach (Camera camera in new[] { overview, closeup })
            {
                camera.fieldOfView = fov;
                camera.orthographic = camera == overview;
                camera.orthographicSize = fov * 0.1f;
                feedback.SetPresentationCamera(camera, camera == overview ? 0.75f : 2.5f);
                Assert.IsTrue((bool)update.Invoke(feedback, new object[] { label, actor, Vector3.zero }));
                Assert.That(label.localScale.x / overlay.rect.height, Is.EqualTo(1f / 1080f).Within(0.000001f));
                Assert.That(label.localScale.y, Is.EqualTo(label.localScale.x));
            }
        }
        finally
        {
            Object.DestroyImmediate(owner);
        }
    }

    [TestCase(false)]
    [TestCase(true)]
    public void InterruptedOverviewImpactRestoresExactPoseAndLens(bool orthographic)
    {
        GameObject owner = new GameObject("Impact recovery test");
        owner.SetActive(false);
        try
        {
            BattlePresentationSystem presentation = owner.AddComponent<BattlePresentationSystem>();
            Camera camera = Child(owner, "Camera").AddComponent<Camera>();
            camera.orthographic = orthographic;
            camera.transform.SetPositionAndRotation(new Vector3(2f, 3f, -12f), Quaternion.Euler(7f, 4f, 2f));
            camera.fieldOfView = 38f;
            camera.orthographicSize = 5f;
            Vector3 position = camera.transform.position;
            Quaternion rotation = camera.transform.rotation;
            Set(presentation, "battleCamera", camera);
            IEnumerator impact = (IEnumerator)typeof(BattlePresentationSystem).GetMethod("PlayCameraImpactBeat", Private)
                .Invoke(presentation, new object[] { camera, Vector3.zero, 12f, true });
            Assert.IsTrue(impact.MoveNext());
            Assert.Greater(Vector3.Distance(position, camera.transform.position), 0.001f);
            typeof(BattlePresentationSystem).GetMethod("StopCameraImpactImmediate", Private).Invoke(presentation, null);
            Assert.That(Vector3.Distance(position, camera.transform.position), Is.LessThan(0.00001f));
            Assert.That(Quaternion.Angle(rotation, camera.transform.rotation), Is.LessThan(0.0001f));
            Assert.That(camera.fieldOfView, Is.EqualTo(38f));
            Assert.That(camera.orthographicSize, Is.EqualTo(5f));
            Assert.IsNull(typeof(BattlePresentationSystem).GetField("cameraBeatCamera", Private).GetValue(presentation));
        }
        finally
        {
            Object.DestroyImmediate(owner);
        }
    }

    [TestCase(false)]
    [TestCase(true)]
    public void ImpactDisplacementKeepsTheSameScreenFractionForDifferentFraming(bool isHit)
    {
        GameObject owner = new GameObject("Impact framing test");
        owner.SetActive(false);
        try
        {
            BattlePresentationSystem presentation = owner.AddComponent<BattlePresentationSystem>();
            Camera camera = Child(owner, "Camera").AddComponent<Camera>();
            MethodInfo amplitude = typeof(BattlePresentationSystem).GetMethod("ResolveImpactWorldAmplitude", Private);
            foreach (float depth in new[] { 5f, 10f, 25f })
            foreach (float fov in new[] { 18f, 50f })
            {
                camera.fieldOfView = fov;
                Vector3 focus = Vector3.forward * depth;
                float displacement = (float)amplitude.Invoke(presentation, new object[] { camera, focus, isHit });
                Vector3 before = camera.WorldToViewportPoint(focus);
                Vector3 after = camera.WorldToViewportPoint(focus + camera.transform.up * displacement);
                Assert.That(after.y - before.y, Is.EqualTo(isHit ? 0.022f : 0.014f).Within(0.00001f));
            }
        }
        finally
        {
            Object.DestroyImmediate(owner);
        }
    }

    private static GameObject Child(GameObject parent, string name, params System.Type[] types)
    {
        GameObject child = new GameObject(name, types);
        child.transform.SetParent(parent.transform, false);
        return child;
    }

    private static void Set(object target, string name, object value)
    {
        target.GetType().GetField(name, Private).SetValue(target, value);
    }
}
