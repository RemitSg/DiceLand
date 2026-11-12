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

    [UnityTest]
    public IEnumerator RangeDetailsCanBeOpenedByPointerWhileOverlayOwnsInput()
    {
        yield return new WaitForSecondsRealtime(0.30f);
        Canvas.ForceUpdateCanvases();
        Button toggle = host.GetComponentsInChildren<Button>().First(b => b.name == "DiceRangeToggle");
        Transform table = toggle.transform.parent.Find("DiceRangeBreakdown");
        Assert.IsFalse(table.gameObject.activeSelf);
        var rect = (RectTransform)toggle.transform;
        Vector2 point = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
        Assert.AreEqual(toggle.gameObject, Hits(point).First().gameObject);

        var pointer = new PointerEventData(events) { position = point, button = PointerEventData.InputButton.Left };
        ExecuteEvents.Execute(toggle.gameObject, pointer, ExecuteEvents.pointerClickHandler);
        Assert.IsTrue(table.gameObject.activeSelf);
        Assert.IsTrue(Visible());
        Assert.IsTrue(StaticFlag("InputSuppressed"));

        ExecuteEvents.Execute(toggle.gameObject, pointer, ExecuteEvents.pointerClickHandler);
        Assert.IsFalse(table.gameObject.activeSelf);
    }

    private bool Visible() => (bool)Get(overlay, "IsVisible");

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
