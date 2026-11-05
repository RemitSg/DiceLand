#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

public sealed class TutorialForestPlayModeTests
{
    private GameObject host;
    private EventSystem events;
    private float oldTime;
    private bool oldInput;
    private const BindingFlags Members = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

    [SetUp]
    public void Setup()
    {
        oldTime = Time.timeScale; oldInput = InputSuppressed();
        SpeedField().SetValue(null, false);
        host = new GameObject("TutorialForestPlayTests"); events = EventSystem.current != null ? EventSystem.current : host.AddComponent<EventSystem>();
    }

    [UnityTearDown]
    public IEnumerator Cleanup()
    {
        UnityEngine.Object.Destroy(host); yield return null;
        Time.timeScale = oldTime; SpeedField().SetValue(null, oldInput);
    }

    [UnityTest]
    public IEnumerator MapContinuesOnceAfterUnscaledCloseAndRestoresExactTimeScale()
    {
        var overlay = host.AddComponent(TypeOf("MapTutorialGuideOverlay"));
        int continued = 0; Time.timeScale = .73f;
        Call(overlay, "Show", "行动点", "移动会消耗行动点。", (Action)(() => continued++), "继续");
        var view = Get(overlay, "Presentation");
        Assert.AreEqual(0, Time.timeScale); Assert.IsTrue(Visible(overlay));
        Assert.Less(((RectTransform)Get(view, "Panel")).GetComponent<CanvasGroup>().alpha, .01f);
        Call(overlay, "AdvancePresentation", .3f);
        Assert.AreEqual(1, ((RectTransform)Get(view, "Panel")).GetComponent<CanvasGroup>().alpha, .001f);
        Button button = (Button)Get(view, "Primary");
        button.onClick.Invoke(); button.onClick.Invoke();
        Assert.AreEqual(0, continued); Assert.AreEqual(0, Time.timeScale);
        Assert.IsTrue(Visible(overlay)); Assert.IsTrue((bool)Get(view, "IsClosing"));
        Assert.IsFalse(button.IsInteractable());
        float deadline = Time.realtimeSinceStartup + 1.5f;
        while (Visible(overlay) && Time.realtimeSinceStartup < deadline) yield return null;
        Assert.IsFalse(Visible(overlay)); Assert.AreEqual(1, continued); Assert.AreEqual(.73f, Time.timeScale, .0001f);
    }

    [UnityTest]
    public IEnumerator BattleKeepsInputBlockedDuringCloseThenTracksTheAllowedTarget()
    {
        var overlay = host.AddComponent(TypeOf("TutorialBattleGuideOverlay"));
        float w = Screen.width, h = Screen.height;
        Rect a = new Rect(w * .25f, h * .30f, w * .15f, h * .15f);
        Rect b = new Rect(w * .64f, h * .55f, w * .15f, h * .15f);
        int calls = 0; Time.timeScale = .61f;
        Call(overlay, "Show", "查看速度", "选择高亮的速度按钮。", "继续", (Action)(() => { calls++; Call(overlay, "ShowTargetHighlight", a); }));
        Call(overlay, "AdvancePresentation", .3f);
        Button button = (Button)Get(Get(overlay, "Presentation"), "Primary"); button.onClick.Invoke(); button.onClick.Invoke();
        Assert.IsTrue(InputSuppressed()); Assert.AreEqual(0, calls); Assert.AreEqual(0, Time.timeScale);
        float deadline = Time.realtimeSinceStartup + 1.5f;
        while ((bool)Get(overlay, "IsModalVisible") && Time.realtimeSinceStartup < deadline) yield return null;
        Assert.AreEqual(1, calls); Assert.AreEqual(.61f, Time.timeScale, .0001f); Assert.IsFalse(InputSuppressed());
        Assert.IsTrue((bool)Get(overlay, "IsTargetHighlightVisible"));
        Assert.IsTrue(Allowed(a.center)); Assert.IsFalse(Allowed(b.center));
        Call(overlay, "TrackTarget", (Func<Rect?>)(() => b)); Call(overlay, "AdvancePresentation", .1f);
        Assert.IsTrue(Allowed(b.center)); Assert.IsFalse(Allowed(a.center));
        ((Behaviour)overlay).enabled = false;
        Assert.IsFalse(Visible(overlay)); Assert.IsTrue(Allowed(a.center)); Assert.IsFalse(InputSuppressed());
    }

    [UnityTest]
    public IEnumerator WelcomePressFeedbackAndLaunchWorkWhilePausedAndReturnFocus()
    {
        var overlay = host.AddComponent(TypeOf("MainMenuTutorialOverlay"));
        var previous = new GameObject("PreviousMenuFocus", typeof(RectTransform), typeof(Button)); previous.transform.SetParent(host.transform);
        events.SetSelectedGameObject(previous); Time.timeScale = 0;
        int starts = 0, backs = 0;
        Call(overlay, "Show", null, (Action)(() => starts++), (Action)(() => backs++), "开始教程", "返回主菜单", "即将开始教程，是否开始？", "从地图探索开始。", true);
        Call(overlay, "AdvancePresentation", .3f);
        Button start = (Button)Get(Get(overlay, "Presentation"), "Primary");
        Assert.AreSame(start.gameObject, events.currentSelectedGameObject);
        var pointer = new PointerEventData(events) { button = PointerEventData.InputButton.Left };
        ExecuteEvents.Execute(start.gameObject, pointer, ExecuteEvents.pointerEnterHandler);
        ExecuteEvents.Execute(start.gameObject, pointer, ExecuteEvents.pointerDownHandler);
        float deadline = Time.realtimeSinceStartup + 1;
        while (start.transform.localScale.x > .99f && Time.realtimeSinceStartup < deadline) yield return null;
        Assert.Less(start.transform.localScale.x, .99f);
        ExecuteEvents.Execute(start.gameObject, pointer, ExecuteEvents.pointerUpHandler); start.onClick.Invoke(); start.onClick.Invoke();
        Assert.AreEqual(0, starts);
        deadline = Time.realtimeSinceStartup + 1.5f;
        while (Visible(overlay) && Time.realtimeSinceStartup < deadline) yield return null;
        Assert.IsFalse(Visible(overlay)); Assert.AreEqual(1, starts); Assert.AreEqual(0, backs);
        Assert.AreSame(previous, events.currentSelectedGameObject);
    }

    private static Type TypeOf(string name) => AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(name)).First(t => t != null);
    private static object Get(object target, string name) => target.GetType().GetField(name, Members) is FieldInfo field ? field.GetValue(target) : target.GetType().GetProperty(name, Members).GetValue(target);
    private static object Call(object target, string name, params object[] args) => target.GetType().GetMethods(Members).Single(m => m.Name == name && m.GetParameters().Length == args.Length).Invoke(target, args);
    private static bool Visible(object overlay) => (bool)Get(overlay, "IsVisible");
    private static FieldInfo SpeedField() => TypeOf("SpeedButton").GetField("InputSuppressed", BindingFlags.Public | BindingFlags.Static);
    private static bool InputSuppressed() => (bool)SpeedField().GetValue(null);
    private static bool Allowed(Vector2 point) => (bool)TypeOf("SpeedButton").GetMethod("IsScreenPointAllowedByTutorialHighlight").Invoke(null, new object[] { point });
}
#endif
