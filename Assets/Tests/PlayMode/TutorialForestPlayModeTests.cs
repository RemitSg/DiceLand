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

    [UnityTest]
    public IEnumerator BattleConnectionKeepsBothTargetsOpenAndCleansBothOnHide()
    {
        var overlay = host.AddComponent(TypeOf("TutorialBattleGuideOverlay"));
        Rect ally = new Rect(Screen.width * .1f, Screen.height * .3f, 70, 70);
        Rect enemy = new Rect(Screen.width * .7f, Screen.height * .4f, 70, 70);
        Call(overlay, "ShowTargetHighlight", ally);
        Call(overlay, "TrackAdditionalTarget", (Func<Rect?>)(() => enemy));
        Call(overlay, "AdvancePresentation", .3f);
        Assert.IsTrue(Allowed(ally.center));
        Assert.IsTrue(Allowed(enemy.center));
        Assert.IsFalse(Allowed((ally.center + enemy.center) * .5f));
        var shade = (Graphic)Get(Get(overlay, "Presentation"), "Shade");
        var filter = (ICanvasRaycastFilter)shade;
        Assert.IsFalse(filter.IsRaycastLocationValid(ally.center, null));
        Assert.IsFalse(filter.IsRaycastLocationValid(enemy.center, null));
        Assert.IsTrue(filter.IsRaycastLocationValid((ally.center + enemy.center) * .5f, null));
        Call(overlay, "Hide", false);
        Assert.IsTrue(Allowed(ally.center));
        Assert.IsTrue(Allowed(enemy.center));
        Assert.IsFalse(Visible(overlay));
        yield return null;
    }

    [UnityTest]
    public IEnumerator BattleFirstViewsAndPanelCloseAreGuidedThenRepeatedViewsAreFree()
    {
        var fixture = new BattleFlowPlayModeFixture();
        ScriptableObject definition = null;
        ScriptableObject paidSkill = null;
        try
        {
            fixture.BuildOneOnOneBattle(4, 1, 20);
            Call(TypeOf("TutorialBattleInteractionGuidance"), "Reset");
            object weapon = Call(fixture.Ally, "GetWeaponData");
            Array originalPool = (Array)Get(weapon, "skillPool");
            paidSkill = (ScriptableObject)UnityEngine.Object.Instantiate((UnityEngine.Object)originalPool.GetValue(0));
            paidSkill.GetType().GetField("energyCost").SetValue(paidSkill, 1);
            Array pool = Array.CreateInstance(TypeOf("EnemySkill_SO"), 2);
            pool.SetValue(originalPool.GetValue(0), 0);
            pool.SetValue(paidSkill, 1);
            weapon.GetType().GetField("skillPool").SetValue(weapon, pool);
            if (Camera.main == null)
            {
                var camera = new GameObject("TutorialInteractionCamera", typeof(Camera));
                camera.transform.SetParent(host.transform);
                camera.transform.position = new Vector3(0, 0, -10);
                camera.tag = "MainCamera";
                camera.GetComponent<Camera>().orthographic = true;
            }
            var enemy = (Component)fixture.Enemy;
            enemy.transform.position = new Vector3(2, 0, 0);
            var body = enemy.gameObject.AddComponent<BoxCollider2D>();
            body.size = new Vector2(1, 2);
            var click = enemy.gameObject.AddComponent(TypeOf("BattleEnemyDetailClickHandler"));
            Call(click, "Bind", enemy);
            definition = ScriptableObject.CreateInstance(TypeOf("TutorialBattleDefinition_SO"));
            var steps = (IList)Get(definition, "Steps");
            foreach (string action in new[]
            {
                "EnemyDetailViewed", "CombatExecutePressed", "EnemySkillViewed", "EnemySkillViewed",
                "AllySkillViewed", "SkillSwitched", "EnergyLineCreated"
            })
            {
                object step = Activator.CreateInstance(TypeOf("TutorialBattleStepDefinition"));
                step.GetType().GetField("stepId").SetValue(step, "first-use-" + steps.Count);
                step.GetType().GetField("sortOrder").SetValue(step, steps.Count);
                step.GetType().GetField("completionEvent").SetValue(step, Enum.Parse(TypeOf("TutorialBattleEventType"), action));
                steps.Add(step);
            }
            Call(TypeOf("TutorialBattleRuntime"), "BeginBattle", definition);
            object battle = fixture.BattleButton;
            object guide = Get(battle, "tutorialBattleGuideOverlay");
            object skills = Get(battle, "battleSkillInfoOverlay");
            Call(battle, "TryShowPendingTutorialStep");
            yield return ContinueGuide(guide);
            Assert.IsTrue((bool)Get(guide, "IsTargetHighlightVisible"), "The first enemy body click needs a mask.");
            Assert.IsTrue((bool)Call(battle, "ShouldConsumeTutorialPrepareStepInReadyPhase"),
                "Enemy details must be read before preparation.");
            Call(skills, "ShowEnemyDetail", enemy);
            Assert.AreEqual("CloseInformation", Get(battle, "activeTutorialHighlightTarget").ToString());
            Call(skills, "AdvancePresentation", .3f);
            ((Button)Get(skills, "footerClose")).onClick.Invoke();
            yield return WaitUntil(() => !(bool)Get(skills, "IsVisible"));
            yield return ContinueGuide(guide);
            fixture.ClickBattleButton();
            yield return ContinueGuide(guide);
            Assert.IsTrue((bool)Get(guide, "IsTargetHighlightVisible"), "The first enemy skill click needs a mask.");
            Call(skills, "ShowEnemySkillInfo", enemy, fixture.EnemyButton);
            Call(skills, "AdvancePresentation", .3f);
            Assert.AreEqual("CloseInformation", Get(battle, "activeTutorialHighlightTarget").ToString());
            ((Button)Get(skills, "footerClose")).onClick.Invoke();
            yield return WaitUntil(() => !(bool)Get(skills, "IsVisible"));
            yield return ContinueGuide(guide);
            Assert.IsFalse((bool)Get(guide, "IsTargetHighlightVisible"), "The repeated enemy skill action must be free.");
            Call(skills, "ShowEnemySkillInfo", enemy, fixture.EnemyButton);
            yield return null;
            Assert.IsFalse((bool)Get(guide, "IsTargetHighlightVisible"), "Closing the same panel again must be free.");
            var phase = battle.GetType().GetField("currentPhase", Members);
            phase.SetValue(battle, Enum.Parse(phase.FieldType, "Ready"));
            Assert.IsFalse((bool)Call(battle, "ShouldConsumeTutorialPrepareStepInReadyPhase"),
                "A repeated prepare must remain available before the next skill-view step.");
            phase.SetValue(battle, Enum.Parse(phase.FieldType, "Targeting"));
            Call(skills, "HideImmediately", true);
            yield return ContinueGuide(guide);
            Call(skills, "ShowAllySkillSelection", fixture.Ally, fixture.AllyButton);
            Call(skills, "AdvancePresentation", .3f);
            ((Button)Get(skills, "footerClose")).onClick.Invoke();
            yield return WaitUntil(() => !(bool)Get(skills, "IsVisible"));
            yield return ContinueGuide(guide);
            Call(skills, "ShowAllySkillSelection", fixture.Ally, fixture.AllyButton);
            Call(skills, "AdvancePresentation", .3f);
            Assert.AreEqual("SkillSelection", Get(battle, "activeTutorialHighlightTarget").ToString());
            IList choices = (IList)Get(skills, "choiceViews");
            ((Button)Get(choices[1], "button")).onClick.Invoke();
            Assert.IsFalse((bool)Get(guide, "IsTargetHighlightVisible"), "A successful switch must release the list mask.");
            Call(skills, "HideImmediately", true);
            yield return ContinueGuide(guide);
            Assert.AreEqual("Connection", Get(battle, "activeTutorialHighlightTarget").ToString());
            Call(guide, "AdvancePresentation", .3f);
            Assert.Greater(((Rect)Get(Get(guide, "Presentation"), "AdditionalFocusScreenRect")).width, 0);
            fixture.ForceContestedConnection();
            Call(battle, "HandleAllyConnectionChanged", fixture.AllyButton);
            Assert.IsFalse((bool)Get(guide, "IsTargetHighlightVisible"));
            Assert.IsNull(Get(TypeOf("TutorialBattleRuntime"), "CurrentStep"));
        }
        finally
        {
            fixture.TearDown();
            Call(TypeOf("TutorialBattleInteractionGuidance"), "Reset");
            if (definition != null) UnityEngine.Object.Destroy(definition);
            if (paidSkill != null) UnityEngine.Object.Destroy(paidSkill);
        }
    }

    private static IEnumerator ContinueGuide(object guide)
    {
        yield return WaitUntil(() => (bool)Get(guide, "IsModalVisible"));
        Call(guide, "AdvancePresentation", .3f);
        ((Button)Get(Get(guide, "Presentation"), "Primary")).onClick.Invoke();
        yield return WaitUntil(() => !(bool)Get(guide, "IsModalVisible"));
    }

    private static IEnumerator WaitUntil(Func<bool> condition)
    {
        float deadline = Time.realtimeSinceStartup + 8f;
        while (!condition() && Time.realtimeSinceStartup < deadline)
            yield return null;
        Assert.IsTrue(condition());
    }

    private static Type TypeOf(string name) => AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(name)).First(t => t != null);
    private static object Get(object target, string name)
    {
        Type type = target as Type ?? target.GetType();
        const BindingFlags flags = Members | BindingFlags.Static;
        return type.GetField(name, flags) is FieldInfo field ? field.GetValue(target) : type.GetProperty(name, flags).GetValue(target);
    }
    private static object Call(object target, string name, params object[] args)
    {
        Type type = target as Type ?? target.GetType();
        const BindingFlags flags = Members | BindingFlags.Static;
        return type.GetMethods(flags).Single(m => m.Name == name && m.GetParameters().Length == args.Length).Invoke(target, args);
    }
    private static bool Visible(object overlay) => (bool)Get(overlay, "IsVisible");
    private static FieldInfo SpeedField() => TypeOf("SpeedButton").GetField("InputSuppressed", BindingFlags.Public | BindingFlags.Static);
    private static bool InputSuppressed() => (bool)SpeedField().GetValue(null);
    private static bool Allowed(Vector2 point) => (bool)TypeOf("SpeedButton").GetMethod("IsScreenPointAllowedByTutorialHighlight").Invoke(null, new object[] { point });
}
#endif
