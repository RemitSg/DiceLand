using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

public sealed class MapResourceFeedbackPlayModeTests
{
    private const BindingFlags Members = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
    private GameObject host;
    private Component hud, feedback, points;
    private object state, member;
    private float timeScale;

    [SetUp]
    public void SetUp()
    {
        timeScale = Time.timeScale;
        Time.timeScale = 0f;
        host = new GameObject("MapResourceFeedbackPlayTest");
        points = host.AddComponent(TypeOf("ActionPointSystem"));
        Invoke(points, "Initialize", 7);
        hud = host.AddComponent(TypeOf("MapHUD"));
        state = Activator.CreateInstance(TypeOf("RunState"));
        Set(state, "money", 100);
        member = Activator.CreateInstance(TypeOf("RunPartyMemberState"));
        Set(member, "memberId", "FeedbackMember");
        Set(member, "currentHP", 20);
        Set(member, "maxHP", 20);
        ((IList)Get(state, "partyMembers")).Add(member);
        Refresh();
        feedback = host.GetComponent(TypeOf("MapResourceFeedback"));
        Assert.IsNotNull(feedback);
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        Time.timeScale = timeScale;
        UnityEngine.Object.Destroy(host);
        yield return null;
    }

    [UnityTest]
    public IEnumerator PausedHudGainsCoinsOnArrivalAndRefreshDoesNotSnapTheCounter()
    {
        yield return null;
        Assert.AreEqual("100", Text("Money"));
        Set(state, "money", 153);
        Refresh();
        Assert.AreEqual("100", Text("Money"));
        yield return new WaitForSecondsRealtime(0.2f);
        Assert.AreEqual("100", Text("Money"));
        Assert.Greater(host.GetComponentsInChildren<Image>().Count(x => x.name.StartsWith("FlyingCoin_")), 0);
        Refresh();
        Assert.AreEqual("100", Text("Money"));
        yield return new WaitForSecondsRealtime(0.57f);
        int partial = int.Parse(Text("Money"));
        Assert.That(partial, Is.InRange(101, 152));
        yield return new WaitForSecondsRealtime(0.65f);
        Assert.AreEqual("153", Text("Money"));
        Assert.IsFalse((bool)Get(feedback, "IsMoneyAnimating"));
        Assert.AreEqual(153, Get(state, "money"));
    }

    [UnityTest]
    public IEnumerator SpendThenRapidGainAndDebtFinishExactlyAndRestoreStyle()
    {
        Component money = Find("Money").GetComponent(TypeOf("TMPro.TMP_Text"));
        Color normal = (Color)Get(money, "color");
        Set(state, "money", 65);
        Refresh();
        yield return new WaitForSecondsRealtime(0.2f);
        Assert.That(int.Parse(Text("Money")), Is.InRange(66, 99));
        Color red = (Color)Get(money, "color");
        Assert.Greater(red.r, red.g);
        Set(state, "money", 145);
        Refresh();
        Set(state, "money", -12);
        Refresh();
        yield return new WaitForSecondsRealtime(2.8f);
        Assert.AreEqual("-12", Text("Money"));
        Assert.AreEqual(normal, Get(money, "color"));
        Assert.AreEqual(Vector3.one, Find("Money").localScale);
        Assert.AreEqual(-12, Get(state, "money"));
    }

    [UnityTest]
    public IEnumerator PausedHealthTransitionsSurviveRepeatedHudRefreshAndHealInterruption()
    {
        Image fill = Find("HpBar").Find("Fill").GetComponent<Image>();
        Assert.AreEqual(1f, fill.rectTransform.anchorMax.x);
        Set(member, "currentHP", 5);
        Refresh();
        yield return new WaitForSecondsRealtime(0.18f);
        float hurt = fill.rectTransform.anchorMax.x;
        Assert.That(hurt, Is.InRange(0.26f, 0.99f));
        Image trail = Find("HpBar").Find("FeedbackTrail").GetComponent<Image>();
        Assert.Greater(trail.rectTransform.anchorMax.x, hurt);
        Refresh();
        Assert.AreEqual(hurt, fill.rectTransform.anchorMax.x, 0.001f);
        Set(member, "currentHP", 19);
        Refresh();
        yield return new WaitForSecondsRealtime(1f);
        Assert.AreEqual(0.95f, fill.rectTransform.anchorMax.x, 0.001f);
        Assert.AreEqual("19/20", Text("HpText"));
        Assert.AreEqual(0f, trail.color.a);
        Assert.AreEqual(19, Get(member, "currentHP"));
        Graphic edge = Find("HealthScreenEdge").GetComponent<Graphic>();
        Assert.AreEqual(0f, edge.color.a);
    }

    [UnityTest]
    public IEnumerator ResourceParticlesDoNotBlockNativeUiAndHidingTheHudDropsStaleChanges()
    {
        EventSystem events = EventSystem.current != null ? EventSystem.current : host.AddComponent<EventSystem>();
        Set(state, "money", 400);
        Refresh();
        yield return null;
        Transform inventory = Find("InventoryButton");
        RectTransform rect = (RectTransform)inventory;
        Vector2 point = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
        var hits = new System.Collections.Generic.List<RaycastResult>();
        events.RaycastAll(new PointerEventData(events) { position = point }, hits);
        Assert.IsNotEmpty(hits);
        Assert.IsTrue(hits[0].gameObject.transform == inventory || hits[0].gameObject.transform.IsChildOf(inventory));
        ExecuteEvents.ExecuteHierarchy(hits[0].gameObject, new PointerEventData(events), ExecuteEvents.pointerClickHandler);
        Assert.IsTrue(Find("InventoryPanel").gameObject.activeSelf);
        host.SetActive(false);
        yield return null;
        host.SetActive(true);
        Refresh();
        yield return new WaitForSecondsRealtime(0.2f);
        Assert.AreEqual("400", Text("Money"));
        Assert.IsFalse((bool)Get(feedback, "IsMoneyAnimating"));
        Assert.IsFalse(host.GetComponentsInChildren<Image>().Any(x => x.name.StartsWith("FlyingCoin_")));
    }

    [UnityTest]
    public IEnumerator MutedSfxCreatesNoPlaybackAndUnmutingUsesTheUiAudioSource()
    {
        object settings = TypeOf("MainMenuSettingsPersistence").GetProperty("Current").GetValue(null);
        float originalVolume = (float)Get(settings, "sfxVolume");
        try
        {
            Set(settings, "sfxVolume", 0f);
            Set(state, "money", 125);
            Refresh();
            yield return new WaitForSecondsRealtime(1.4f);
            Assert.IsNull(host.GetComponent<AudioSource>());
            Set(settings, "sfxVolume", 0.35f);
            Set(state, "money", 110);
            Refresh();
            yield return null;
            AudioSource source = host.GetComponent<AudioSource>();
            Assert.IsNotNull(source);
            Assert.AreEqual(0f, source.spatialBlend);
            Assert.IsFalse(source.playOnAwake);
            Assert.IsTrue(source.ignoreListenerPause);
        }
        finally { Set(settings, "sfxVolume", originalVolume); }
    }

    private void Refresh() => Invoke(hud, "Refresh", state, points);
    private string Text(string name) => (string)Get(Find(name).GetComponent(TypeOf("TMPro.TMP_Text")), "text");
    private Transform Find(string name) => host.GetComponentsInChildren<Transform>(true).First(x => x.name == name);
    private static object Invoke(object target, string method, params object[] args) => target.GetType().GetMethod(method, Members).Invoke(target, args);
    private static Type TypeOf(string name) => AppDomain.CurrentDomain.GetAssemblies().Select(x => x.GetType(name)).First(x => x != null);
    private static void Set(object target, string field, object value) => target.GetType().GetField(field, Members).SetValue(target, value);
    private static object Get(object target, string name)
    {
        FieldInfo field = target.GetType().GetField(name, Members);
        return field != null ? field.GetValue(target) : target.GetType().GetProperty(name, Members).GetValue(target);
    }
}
