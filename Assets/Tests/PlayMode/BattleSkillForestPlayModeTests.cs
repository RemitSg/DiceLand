#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

public sealed class BattleSkillForestPlayModeTests
{
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private GameObject host;
    private Component overlay, ally;
    private object sourceButton;
    private EventSystem events;
    private float oldTime;
    private bool oldInput, oldLock;
    private readonly List<UnityEngine.Object> transient = new List<UnityEngine.Object>();

    [SetUp]
    public void Setup()
    {
        oldTime = Time.timeScale; oldInput = StaticFlag("InputSuppressed"); oldLock = StaticFlag("CombatLocked");
        SetFlag("InputSuppressed", false); SetFlag("CombatLocked", false);
        host = new GameObject("SkillForestPlayTests"); events = EventSystem.current != null ? EventSystem.current : host.AddComponent<EventSystem>();
        overlay = host.AddComponent(TypeOf("BattleSkillInfoOverlay"));
        Type database = TypeOf("UnityEditor.AssetDatabase");
        string fontPath = (string)database.GetMethod("GUIDToAssetPath", new[] { typeof(string) }).Invoke(null, new object[] { "c6e7143cd2df45b45a784ef8c61a210f" });
        object font = Asset(fontPath, "TMPro.TMP_FontAsset"); Call(overlay, "ConfigureFonts", font, font);
        object weapon = Asset("Assets/Data/Weapons/007 烬冠剑/007 烬冠剑.asset", "Weapon_SO");
        ally = (Component)TypeOf("BattleSkillVisualTools").GetMethod("CreateAlly").Invoke(null, new[] { host.transform, weapon });
        sourceButton = ((IEnumerable)Get(ally, "SpeedButtons")).Cast<object>().First();
        Call(ally, "SetSelectedSkillIndexForButton", sourceButton, 3, -1);
        Call(overlay, "ShowAllySkillSelection", ally, sourceButton);
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        Time.timeScale = oldTime;
        UnityEngine.Object.Destroy(host);
        foreach (UnityEngine.Object item in transient) if (item != null) UnityEngine.Object.Destroy(item);
        transient.Clear(); yield return null;
        SetFlag("InputSuppressed", oldInput); SetFlag("CombatLocked", oldLock);
    }

    [UnityTest]
    public IEnumerator StunnedSlotRejectsOpenAndStaleSkillChoiceThenRecovers()
    {
        yield return new WaitForSecondsRealtime(0.30f);
        object selected = Call(ally, "GetSelectedSkillForButton", sourceButton);
        Button choice = host.GetComponentsInChildren<Button>().First(b => b.name == "Skill_0");
        Call(sourceButton, "SetActionDisabled", true);
        choice.onClick.Invoke();
        Assert.IsNull(Call(ally, "GetSelectedSkillForButton", sourceButton));
        Call(overlay, "Hide", false);
        Call(overlay, "ShowAllySkillSelection", ally, sourceButton);
        Assert.IsFalse(Visible(), "A stunned speed slot cannot reopen the skill selector.");
        Call(sourceButton, "SetActionDisabled", false);
        Assert.AreSame(selected, Call(ally, "GetSelectedSkillForButton", sourceButton));
        Call(overlay, "ShowAllySkillSelection", ally, sourceButton);
        yield return new WaitForSecondsRealtime(0.30f);
        Assert.IsTrue(Visible());
    }

    [UnityTest]
    public IEnumerator PausedClosingBlocksUnderlyingInputAndNotifiesOnce()
    {
        Time.timeScale = 0; yield return new WaitForSecondsRealtime(0.30f);
        Assert.Greater(((CanvasGroup)Get(overlay, "panelGroup")).alpha, 0.99f);
        int hidden = 0; overlay.GetType().GetEvent("Hidden").AddEventHandler(overlay, (Action)(() => hidden++));
        ((Button)Get(overlay, "footerClose")).onClick.Invoke(); Call(overlay, "Hide");
        Assert.IsTrue(Visible()); Assert.IsTrue(StaticFlag("InputSuppressed")); Assert.AreEqual(0, hidden);
        Assert.AreEqual("Backdrop", Hits(new Vector2(3, 3)).First().gameObject.name);
        // The background editor can render fewer than five frames per second. Wait for
        // rendered frames instead of sampling before LateUpdate on a fixed timer tick.
        float deadline = Time.realtimeSinceStartup + 1f;
        while (Visible() && Time.realtimeSinceStartup < deadline) yield return null;
        Assert.IsFalse(Visible()); Assert.IsFalse(StaticFlag("InputSuppressed")); Assert.AreEqual(1, hidden);
        Call(overlay, "Hide"); Assert.AreEqual(1, hidden);
    }

    [UnityTest]
    public IEnumerator ReopeningCancelsCloseAndDisablingReleasesSuppression()
    {
        yield return new WaitForSecondsRealtime(0.28f);
        int hidden = 0; overlay.GetType().GetEvent("Hidden").AddEventHandler(overlay, (Action)(() => hidden++));
        Call(overlay, "Hide"); yield return new WaitForSecondsRealtime(0.035f);
        Call(overlay, "ShowAllySkillSelection", ally, sourceButton); yield return new WaitForSecondsRealtime(0.24f);
        Assert.IsTrue(Visible()); Assert.AreEqual(0, hidden); Assert.IsTrue(((CanvasGroup)Get(overlay, "panelGroup")).interactable);
        Assert.AreEqual(1, host.GetComponentsInChildren<Canvas>(true).Length);
        ((Behaviour)overlay).enabled = false; yield return null;
        Assert.IsFalse(StaticFlag("InputSuppressed")); Assert.IsFalse(Visible()); Assert.AreEqual(1, hidden);
    }

    [UnityTest]
    public IEnumerator TouchAndWheelReachLongDescriptionWhileFooterStaysFixed()
    {
        ScriptableObject skill = UnityEngine.Object.Instantiate((ScriptableObject)Asset("Assets/Data/Weapons/007 烬冠剑/冠焰裁断.asset", "EnemySkill_SO")); transient.Add(skill);
        skill.GetType().GetField("description").SetValue(skill, string.Join("\n", Enumerable.Repeat("长说明完整保留触发、目标、数值与延迟。", 48)));
        Call(ally, "GrantTemporarySkill", skill, true); Call(overlay, "RefreshCurrentView");
        yield return new WaitForSecondsRealtime(0.30f);
        ScrollRect scroll = (ScrollRect)Get(overlay, "detailScrollRect");
        Assert.Greater(scroll.content.rect.height, scroll.viewport.rect.height * 3);
        RectTransform footer = (RectTransform)((Button)Get(overlay, "footerClose")).transform;
        Vector3 footerPosition = footer.position;
        Vector2 point = RectTransformUtility.WorldToScreenPoint(null, scroll.viewport.TransformPoint(scroll.viewport.rect.center));
        var touch = new PointerEventData(events) { pointerId = 3, button = PointerEventData.InputButton.Left, position = point, pressPosition = point };
        ExecuteEvents.Execute(scroll.gameObject, touch, ExecuteEvents.initializePotentialDrag);
        ExecuteEvents.Execute(scroll.gameObject, touch, ExecuteEvents.beginDragHandler);
        touch.position += Vector2.up * 160; touch.delta = Vector2.up * 160;
        ExecuteEvents.Execute(scroll.gameObject, touch, ExecuteEvents.dragHandler);
        ExecuteEvents.Execute(scroll.gameObject, touch, ExecuteEvents.endDragHandler);
        yield return null; Assert.Less(scroll.verticalNormalizedPosition, 0.99f);
        var wheel = new PointerEventData(events) { position = point, scrollDelta = new Vector2(0, -200) };
        ExecuteEvents.Execute(scroll.gameObject, wheel, ExecuteEvents.scrollHandler);
        yield return null; Assert.Less(scroll.verticalNormalizedPosition, 0.5f);
        scroll.StopMovement(); scroll.verticalNormalizedPosition = 0; yield return null;
        Assert.AreEqual(footerPosition, footer.position); Assert.IsTrue(Visible());
        Assert.IsTrue(Hits(RectTransformUtility.WorldToScreenPoint(null, footer.TransformPoint(footer.rect.center))).Any(h => h.gameObject.name == "FooterCloseButton"));
    }

    [UnityTest]
    public IEnumerator HoverAutoScrollReachesItsLastRuleWhileDiceRemainFixed()
    {
        Call(overlay, "Hide", false); Time.timeScale = 0;
        object data = Asset("Assets/Data/Enemies/Enemies/014 BloodRat/BloodRat.asset", "EnemyData_SO");
        object skill = Asset("Assets/Data/Enemies/Enemies/027 UtilityPole/HighVoltage.asset", "EnemySkill_SO");
        Component enemy = (Component)TypeOf("BattleSkillVisualTools").GetMethod("CreateEnemy").Invoke(null, new[] { host.transform, data, skill });
        object[] buttons = ((IEnumerable)Get(enemy, "SpeedButtons")).Cast<object>().ToArray();
        Call(overlay, "ShowEnemyHoverSkillInfo", enemy, buttons[0]);
        yield return new WaitForSecondsRealtime(0.18f);
        RectTransform content = (RectTransform)Get(overlay, "hoverClauseContent"), viewport = (RectTransform)Get(overlay, "hoverClauseViewport");
        RectTransform ranges = ((Component)Get(overlay, "hoverDetailText")).GetComponent<RectTransform>(); Vector3 rangesPosition = ranges.position;
        Assert.Greater(content.rect.height, viewport.rect.height);
        Call(Get(overlay, "hoverClauseAutoScroller"), "Configure", viewport, content, Get(overlay, "hoverClauseText"), 2000f, 0f, 10f);
        yield return new WaitForSecondsRealtime(0.24f);
        Assert.Greater(content.anchoredPosition.y, 0);
        Assert.GreaterOrEqual(content.anchoredPosition.y + viewport.rect.height, content.rect.height - 1);
        Assert.AreEqual(rangesPosition, ranges.position); Assert.IsFalse(StaticFlag("InputSuppressed"));
        Call(overlay, "ShowEnemyHoverSkillInfo", enemy, buttons[1]); Assert.AreEqual(Vector2.zero, content.anchoredPosition);
        Call(overlay, "HideEnemyHoverSkillInfo", buttons[0]); Assert.IsTrue(((RectTransform)Get(overlay, "hoverPanelRoot")).gameObject.activeSelf);
        Call(overlay, "HideEnemyHoverSkillInfo", buttons[1]); Assert.IsFalse(((RectTransform)Get(overlay, "hoverPanelRoot")).gameObject.activeSelf);
    }

    [UnityTest]
    public IEnumerator PressFeedbackUsesUnscaledTimeAndSelectionUpdatesInPlace()
    {
        Time.timeScale = 0; yield return new WaitForSecondsRealtime(0.30f);
        Button button = host.GetComponentsInChildren<Button>().First(b => b.name == "Skill_0");
        var pointer = new PointerEventData(events) { button = PointerEventData.InputButton.Left };
        ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerEnterHandler);
        ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerDownHandler);
        yield return new WaitForSecondsRealtime(0.12f); Assert.Less(button.transform.localScale.x, 0.99f);
        ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerUpHandler); button.onClick.Invoke();
        yield return new WaitForSecondsRealtime(0.20f);
        Assert.AreEqual("掠火斩", Get(Call(ally, "GetSelectedSkillForButton", sourceButton), "skillName"));
        Assert.IsTrue(button != null && button.gameObject.activeInHierarchy, "Selection must not destroy a button under an active pointer.");
        Assert.Greater(button.transform.localScale.x, 1);
    }

    private bool Visible() => (bool)Get(overlay, "IsVisible");

    [UnityTest]
    public IEnumerator EnemyProfileRevealsAndCancelsWhilePausedAndRestoresPreviousFocus()
    {
        Call(overlay, "HideImmediately", false);
        GameObject previous = new GameObject("PreviousFocus", typeof(RectTransform), typeof(Button)); previous.transform.SetParent(host.transform, false);
        events.SetSelectedGameObject(previous);
        Time.timeScale = 0;
        ShowProfile(false);
        Assert.Less(((CanvasGroup)Get(overlay, "enemyPortraitGroup")).alpha, .01f);
        Assert.AreSame(((Button)Get(overlay, "footerClose")).gameObject, events.currentSelectedGameObject);
        yield return new WaitForSecondsRealtime(.35f);
        Assert.AreEqual(1, ((CanvasGroup)Get(overlay, "enemyPortraitGroup")).alpha, .001f);
        Assert.AreEqual(Vector2.zero, ((RectTransform)Get(overlay, "enemyPortraitMotion")).anchoredPosition);
        int hidden = 0; overlay.GetType().GetEvent("Hidden").AddEventHandler(overlay, (Action)(() => hidden++));
        ExecuteEvents.Execute(events.currentSelectedGameObject, new BaseEventData(events), ExecuteEvents.cancelHandler);
        Assert.IsTrue(Visible()); Assert.IsTrue(StaticFlag("InputSuppressed"));
        Assert.IsFalse(((CanvasGroup)Get(overlay, "panelGroup")).interactable);
        float deadline = Time.realtimeSinceStartup + 1;
        while (Visible() && Time.realtimeSinceStartup < deadline) yield return null;
        Assert.IsFalse(Visible()); Assert.IsFalse(StaticFlag("InputSuppressed")); Assert.AreEqual(1, hidden);
        Assert.AreSame(previous, events.currentSelectedGameObject);
    }

    [UnityTest]
    public IEnumerator EnemyProfileTouchAndWheelReadToEndWhilePortraitAndCloseStayFixed()
    {
        Call(overlay, "HideImmediately", false); ShowProfile(true); Time.timeScale = 0;
        yield return new WaitForSecondsRealtime(.35f);
        ScrollRect scroll = (ScrollRect)Get(overlay, "enemyProfileScroll");
        Assert.Greater(scroll.content.rect.height, scroll.viewport.rect.height * 3);
        RectTransform portrait = (RectTransform)Get(overlay, "enemyPortraitWell");
        RectTransform close = (RectTransform)((Button)Get(overlay, "footerClose")).transform;
        Vector3 portraitPosition = portrait.position, closePosition = close.position;
        Vector2 point = RectTransformUtility.WorldToScreenPoint(null, scroll.viewport.TransformPoint(scroll.viewport.rect.center));
        var touch = new PointerEventData(events) { pointerId = 4, button = PointerEventData.InputButton.Left, position = point, pressPosition = point };
        ExecuteEvents.Execute(scroll.gameObject, touch, ExecuteEvents.initializePotentialDrag);
        ExecuteEvents.Execute(scroll.gameObject, touch, ExecuteEvents.beginDragHandler);
        touch.position += Vector2.up * 180; touch.delta = Vector2.up * 180;
        ExecuteEvents.Execute(scroll.gameObject, touch, ExecuteEvents.dragHandler);
        ExecuteEvents.Execute(scroll.gameObject, touch, ExecuteEvents.endDragHandler);
        yield return null; Assert.Less(scroll.verticalNormalizedPosition, .999f);
        ExecuteEvents.Execute(scroll.gameObject, new PointerEventData(events) { position = point, scrollDelta = new Vector2(0, -500) }, ExecuteEvents.scrollHandler);
        yield return null; Assert.Less(scroll.verticalNormalizedPosition, .5f);
        scroll.StopMovement(); scroll.verticalNormalizedPosition = 0; yield return null;
        Assert.AreEqual(portraitPosition, portrait.position); Assert.AreEqual(closePosition, close.position);
        Assert.IsTrue(Hits(RectTransformUtility.WorldToScreenPoint(null, close.TransformPoint(close.rect.center))).Any(h => h.gameObject.name == "FooterCloseButton"));
        Assert.IsTrue(StaticFlag("InputSuppressed"));
    }

    [UnityTest]
    public IEnumerator EnemyProfilePassiveHoverUsesUnscaledFeedbackWithoutMovingText()
    {
        Call(overlay, "HideImmediately", false); ShowProfile(false); Time.timeScale = 0;
        yield return new WaitForSecondsRealtime(.35f);
        Component frame = host.GetComponentsInChildren(TypeOf("BattleEnemyProfileGraphic")).First(c => c.gameObject.name == "EnemyPassive_0");
        Vector3 position = frame.transform.position;
        ExecuteEvents.Execute(frame.gameObject, new PointerEventData(events), ExecuteEvents.pointerEnterHandler);
        yield return new WaitForSecondsRealtime(.18f); Assert.Greater((float)Get(frame, "emphasis"), .95f);
        Assert.AreEqual(position, frame.transform.position);
        ExecuteEvents.Execute(frame.gameObject, new PointerEventData(events), ExecuteEvents.pointerExitHandler);
        yield return new WaitForSecondsRealtime(.18f); Assert.Less((float)Get(frame, "emphasis"), .05f);
    }

    [UnityTest]
    public IEnumerator OpenEnemyDescriptionReflowsWhenDetailTextGetsLarger()
    {
        Type persistence = TypeOf("MainMenuSettingsPersistence");
        FieldInfo cache = persistence.GetField("cachedSettings", BindingFlags.Static | BindingFlags.NonPublic);
        object original = persistence.GetProperty("Current").GetValue(null);
        object settings = Call(original, "Clone");
        FieldInfo largeText = settings.GetType().GetField("largeDetailText");
        largeText.SetValue(settings, false);
        cache.SetValue(null, settings);
        try
        {
            Call(overlay, "HideImmediately", false);
            ShowProfile(true);
            object description = Get(overlay, "enemyProfileDescription");
            var scroll = (ScrollRect)Get(overlay, "enemyProfileScroll");
            float standardSize = (float)Get(description, "fontSize");
            float standardHeight = scroll.content.rect.height;

            largeText.SetValue(settings, true);
            yield return null;
            yield return null;
            Assert.AreEqual(standardSize * 1.25f, (float)Get(description, "fontSize"), 0.001f);
            Assert.Greater(scroll.content.rect.height, standardHeight);
            Assert.IsTrue(scroll.verticalScrollbar.gameObject.activeSelf);

            largeText.SetValue(settings, false);
            yield return null;
            yield return null;
            Assert.AreEqual(standardSize, (float)Get(description, "fontSize"), 0.001f);
            Assert.AreEqual(standardHeight, scroll.content.rect.height, 0.1f);
        }
        finally
        {
            cache.SetValue(null, original);
        }
    }

    private void ShowProfile(bool longText)
    {
        ScriptableObject data = (ScriptableObject)Asset("Assets/Data/Enemies/Enemies/014 BloodRat/BloodRat.asset", "EnemyData_SO");
        if (longText)
        {
            data = UnityEngine.Object.Instantiate(data); transient.Add(data);
            data.GetType().GetField("enemyDescription").SetValue(data, string.Join("\n", Enumerable.Repeat("长描述保留完整条件与说明，不截断末尾内容。", 60)));
        }
        object skill = Asset("Assets/Data/Enemies/Enemies/014 BloodRat/RotfangBite.asset", "EnemySkill_SO");
        object enemy = TypeOf("BattleSkillVisualTools").GetMethod("CreateEnemy").Invoke(null, new[] { host.transform, data, skill });
        Call(overlay, "ShowEnemyDetail", enemy);
    }
    private List<RaycastResult> Hits(Vector2 point)
    {
        var hits = new List<RaycastResult>(); ((Canvas)Get(overlay, "canvas")).GetComponent<GraphicRaycaster>().Raycast(new PointerEventData(events) { position = point }, hits); return hits;
    }
    private static object Asset(string path, string type) => TypeOf("UnityEditor.AssetDatabase").GetMethod("LoadAssetAtPath", new[] { typeof(string), typeof(Type) }).Invoke(null, new object[] { path, TypeOf(type) });
    private static Type TypeOf(string name) => AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(name)).First(t => t != null);
    private static object Get(object target, string name) => target.GetType().GetField(name, Members) is FieldInfo field ? field.GetValue(target) : target.GetType().GetProperty(name, Members).GetValue(target);
    private static object Call(object target, string name, params object[] args) => target.GetType().GetMethods(Members).Single(m => m.Name == name && m.GetParameters().Length == args.Length).Invoke(target, args);
    private static bool StaticFlag(string name) => (bool)TypeOf("SpeedButton").GetField(name, BindingFlags.Public | BindingFlags.Static).GetValue(null);
    private static void SetFlag(string name, bool value) => TypeOf("SpeedButton").GetField(name, BindingFlags.Public | BindingFlags.Static).SetValue(null, value);
}
#endif
