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

    [TestCase(false,.14f)]
    [TestCase(false,.28f)]
    [TestCase(true,.22f)]
    [TestCase(true,.40f)]
    public void ImpactDisplacementKeepsTheSameScreenFractionForDifferentFraming(bool isHit,float configuredAmplitude)
    {
        GameObject owner = new GameObject("Impact framing test");
        owner.SetActive(false);
        try
        {
            BattlePresentationSystem presentation = owner.AddComponent<BattlePresentationSystem>();
            Camera camera = Child(owner, "Camera").AddComponent<Camera>();
            Set(presentation,isHit?"hitShakeAmplitude":"clashShakeAmplitude",configuredAmplitude);
            MethodInfo amplitude = typeof(BattlePresentationSystem).GetMethod("ResolveImpactWorldAmplitude", Private);
            foreach (float depth in new[] { 5f, 10f, 25f })
            foreach (float fov in new[] { 18f, 50f })
            {
                camera.fieldOfView = fov;
                Vector3 focus = Vector3.forward * depth;
                float displacement = (float)amplitude.Invoke(presentation, new object[] { camera, focus, isHit });
                Vector3 before = camera.WorldToViewportPoint(focus);
                Vector3 after = camera.WorldToViewportPoint(focus + camera.transform.up * displacement);
                Assert.That(after.y - before.y, Is.EqualTo(configuredAmplitude*.1f).Within(0.00001f));
            }
        }
        finally
        {
            Object.DestroyImmediate(owner);
        }
    }

    [TestCase(1920f,1080f)]
    [TestCase(2960f,1440f)]
    [TestCase(1600f,1200f)]
    public void ImpactGlowTracksWorldContactAndKeepsScreenHeightAcrossAspects(float width,float height)
    {
        GameObject owner = new GameObject("Impact glow layout"); owner.SetActive(false);
        BattleFeedbackSystem feedback = owner.AddComponent<BattleFeedbackSystem>();
        try
        {
            Camera camera = PreparePulseFixture(owner,feedback,width,height);
            Vector3 contact = camera.ViewportToWorldPoint(new Vector3(.7f,.6f,10));
            BeginPulse(feedback,contact,1);
            var image = PulseImage(feedback);
            Assert.IsFalse(image.raycastTarget);
            Assert.AreSame(typeof(BattleFeedbackSystem).GetField("particleTexture",Private).GetValue(feedback),image.sprite.texture);
            Assert.That(image.rectTransform.sizeDelta.y/height,Is.EqualTo(.82f).Within(.00001f));
            Assert.That(image.rectTransform.sizeDelta.x,Is.EqualTo(image.rectTransform.sizeDelta.y).Within(.001f));
            Assert.That(image.rectTransform.anchoredPosition.x,Is.EqualTo(width*.2f).Within(.01f));
            Assert.That(image.rectTransform.anchoredPosition.y,Is.EqualTo(height*.1f).Within(.01f));
            Vector2 first = image.rectTransform.anchoredPosition;
            camera.transform.position = Vector3.right;
            typeof(BattleFeedbackSystem).GetMethod("UpdateImpactPulse",Private).Invoke(feedback,new object[]{0f});
            Assert.Greater(Vector2.Distance(first,image.rectTransform.anchoredPosition),1f);
            BeginPulse(feedback,Vector3.back*10,1);
            Assert.IsFalse(image.gameObject.activeSelf,"A contact behind the camera must not become a centered flash.");
        }
        finally { DisposePulseFixture(owner,feedback); }
    }

    [Test]
    public void ImpactGlowReusesOneImageAndCannotStackOrSkipItsFirstFrame()
    {
        GameObject owner = new GameObject("Impact glow envelope"); owner.SetActive(false);
        BattleFeedbackSystem feedback = owner.AddComponent<BattleFeedbackSystem>();
        try
        {
            PreparePulseFixture(owner,feedback,1920,1080);
            BeginPulse(feedback,null,1);
            var image=PulseImage(feedback);Sprite sprite=image.sprite;
            for(int i=0;i<20;i++)BeginPulse(feedback,null,.45f);
            Assert.AreSame(image,PulseImage(feedback));Assert.AreSame(sprite,image.sprite);
            Assert.AreEqual(1,image.transform.parent.GetComponentsInChildren<UnityEngine.UI.Image>(true).Length);
            Assert.That(image.color.a,Is.EqualTo(.12f).Within(.0001f));
            MethodInfo update=typeof(BattleFeedbackSystem).GetMethod("UpdateImpactPulse",Private);
            update.Invoke(feedback,new object[]{1f});
            Assert.That(image.color.a,Is.EqualTo(.12f).Within(.0001f),"A long triggering frame still has one visible beat.");
            Set(feedback,"impactPulseStartFrame",-1);
            update.Invoke(feedback,new object[]{.07f});
            Assert.That(image.color.a,Is.EqualTo(.03f).Within(.0001f));
            update.Invoke(feedback,new object[]{.08f});
            Assert.IsFalse(image.gameObject.activeSelf);
            Assert.AreEqual(0,image.color.a);
        }
        finally { DisposePulseFixture(owner,feedback); }
    }

    [Test]
    public void ImpactGlowClearsWhenFeedbackOrPresentationIsInterrupted()
    {
        GameObject owner=new GameObject("Impact glow interruption");owner.SetActive(false);
        BattleFeedbackSystem feedback=owner.AddComponent<BattleFeedbackSystem>();
        try
        {
            Camera camera=PreparePulseFixture(owner,feedback,1920,1080);
            BeginPulse(feedback,null,1);var image=PulseImage(feedback);
            typeof(BattleFeedbackSystem).GetMethod("OnDisable",Private).Invoke(feedback,null);
            Assert.IsFalse(image.gameObject.activeSelf);Assert.AreEqual(0,image.color.a);
            BeginPulse(feedback,null,1);
            BattlePresentationSystem presentation=owner.AddComponent<BattlePresentationSystem>();
            Set(presentation,"battleFeedbackSystem",feedback);Set(presentation,"battleCamera",camera);
            typeof(BattlePresentationSystem).GetMethod("StopActiveCloseupShotImmediate",Private).Invoke(presentation,null);
            Assert.IsFalse(image.gameObject.activeSelf);Assert.AreEqual(0,image.color.a);
        }
        finally { DisposePulseFixture(owner,feedback); }
    }

    private static Camera PreparePulseFixture(GameObject owner,BattleFeedbackSystem feedback,float width,float height)
    {
        var overlay=Child(owner,"Pulse overlay",typeof(RectTransform)).GetComponent<RectTransform>();
        overlay.sizeDelta=new Vector2(width,height);
        var canvas=Child(owner,"Fixture canvas",typeof(RectTransform),typeof(Canvas)).GetComponent<Canvas>();
        Camera camera=Child(owner,"Pulse camera").AddComponent<Camera>();camera.aspect=width/height;
        Set(feedback,"overlayRoot",overlay);Set(feedback,"overlayCanvas",canvas);Set(feedback,"worldCamera",camera);
        return camera;
    }

    private static void BeginPulse(BattleFeedbackSystem feedback,Vector3? position,float strength)
    {
        typeof(BattleFeedbackSystem).GetMethod("BeginImpactPulse",Private).Invoke(feedback,new object[]{position,new Color(1,.82f,.42f,1),strength});
    }

    private static UnityEngine.UI.Image PulseImage(BattleFeedbackSystem feedback)
    {
        return (UnityEngine.UI.Image)typeof(BattleFeedbackSystem).GetField("impactPulseImage",Private).GetValue(feedback);
    }

    private static void DisposePulseFixture(GameObject owner,BattleFeedbackSystem feedback)
    {
        typeof(BattleFeedbackSystem).GetMethod("OnDestroy",Private).Invoke(feedback,null);
        Object.DestroyImmediate(owner);
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
