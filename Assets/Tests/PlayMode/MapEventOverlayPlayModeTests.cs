using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

public sealed class MapEventOverlayPlayModeTests
{
    private GameObject host;
    private Component overlay;
    private ScriptableObject data;
    private EventSystem events;
    private float timeScale;
    private int executions, hidden;
    private bool allowPaidChoice;
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    [SetUp] public void SetUp()
    {
        timeScale = Time.timeScale; executions = hidden = 0; allowPaidChoice = false;
        host = new GameObject("StoryEventPlayTest"); overlay = host.AddComponent(TypeOf("MapEventOverlay"));
        data = ScriptableObject.CreateInstance(TypeOf("MapEvent_SO"));
        Set(data, "eventName", "Forest encounter"); Set(data, "eventId", "test.story.overlay");
        Set(data, "eventIllustration", Resources.Load<Sprite>("EventUI/Scenes/023"));
        Set(data, "eventContent", string.Join("\n\n", Enumerable.Repeat("You find an old supply chest beside the forest path. Its brass clasp carries a faint warm light, and the air smells of herbs. Take a moment before deciding what to do.", 6)));
        IList options = (IList)Get(data, "options");
        for (int i = 0; i < 8; i++)
        {
            object option = Activator.CreateInstance(TypeOf("MapEventOptionData"));
            Set(option, "optionLabel", "Choice " + (i + 1)); Set(option, "consequenceDescription", "Reward for choice " + (i + 1)); options.Add(option);
        }
        events = EventSystem.current != null ? EventSystem.current : host.AddComponent<EventSystem>();
        overlay.GetType().GetEvent("Hidden").AddEventHandler(overlay, (Action)(() => hidden++));
        Show();
    }
    [UnityTearDown] public IEnumerator TearDown()
    {
        Time.timeScale = timeScale; UnityEngine.Object.Destroy(host); UnityEngine.Object.Destroy(data); yield return null;
    }

    [UnityTest] public IEnumerator OpeningAndTypewriterContinueWhilePausedAndSkipDoesNotChooseAnAction()
    {
        Time.timeScale = 0; Advance(.5f); yield return null;
        Assert.IsTrue((bool)Get(overlay, "IsTyping"));
        Component text = Find("ContentText").GetComponent(TypeOf("TMPro.TextMeshProUGUI"));
        Assert.Greater((int)Get(text, "maxVisibleCharacters"), 0);
        Assert.AreEqual(0, Find("OptionSection").GetComponent<CanvasGroup>().alpha, .01f);
        Click(Find("Footer"));
        Assert.IsFalse((bool)Get(overlay, "IsTyping")); Assert.IsFalse((bool)Get(overlay, "IsNarrationComplete"));
        int visible = (int)Get(text, "maxVisibleCharacters");
        Click(Find("Footer"));
        Assert.IsTrue((bool)Get(overlay, "IsTyping")); Advance(.12f);
        Assert.Greater((int)Get(text, "maxVisibleCharacters"), visible); Assert.AreEqual(0, executions);
    }

    [UnityTest] public IEnumerator NativeHoverAndConfirmationApplyOnceThenReadResultsBeforeClosing()
    {
        Invoke(overlay, "CompleteNarration"); Advance(.5f); yield return null;
        ExecuteEvents.Execute(Find("Option_02").gameObject, new PointerEventData(events), ExecuteEvents.pointerEnterHandler);
        StringAssert.Contains("choice 2", (string)Get(Find("ConsequenceText").GetComponent(TypeOf("TMPro.TextMeshProUGUI")), "text"));
        Assert.AreEqual(0, executions);
        Click(Find("ConfirmButton"));
        Assert.AreEqual(1, executions); Assert.IsTrue((bool)Get(overlay, "IsTyping"));
        Assert.IsTrue((bool)Get(overlay, "IsVisible")); Assert.IsFalse(Find("OptionScrollRect").gameObject.activeInHierarchy);
        Invoke(overlay, "OnConfirmClicked"); // A second queued click may reveal text, never replay the choice.
        Assert.AreEqual(1, executions);
        StringAssert.Contains("You leave with the supplies", (string)Get(Find("ContentText").GetComponent(TypeOf("TMPro.TextMeshProUGUI")), "text"));
        Invoke(overlay, "CompleteNarration"); Click(Find("Footer"));
        Assert.IsTrue((bool)Get(overlay, "IsClosing")); Assert.IsTrue((bool)Get(overlay, "IsVisible")); Assert.AreEqual(0, hidden);
        List<RaycastResult> hits = Hits(new Vector2(4, 4)); Assert.IsNotEmpty(hits); Assert.AreEqual("EventOverlayCanvas", hits[0].gameObject.name);
        Advance(.3f); Assert.IsFalse((bool)Get(overlay, "IsVisible")); Assert.AreEqual(1, hidden);
        Invoke(overlay, "Hide"); Assert.AreEqual(1, hidden);
    }

    [UnityTest] public IEnumerator BackpackPausesNarrationAndRechecksConditionsAfterReturning()
    {
        Component hud = host.AddComponent(TypeOf("MapHUD"));
        object state = Activator.CreateInstance(TypeOf("RunState")); Set(state, "money", 20);
        Invoke(hud, "Refresh", state, null); Invoke(overlay, "ConfigureInventory", hud);
        Show(BuildPreview()); Advance(.5f); yield return null;
        Assert.IsFalse(Find("Option_01").GetComponent<Button>().interactable);
        object selection = Get(overlay, "selectedOption");
        Component text = Find("ContentText").GetComponent(TypeOf("TMPro.TextMeshProUGUI"));
        int visible = (int)Get(text, "maxVisibleCharacters");
        Click(Find("InventoryButton")); Advance(1);
        Assert.IsTrue((bool)Get(hud, "IsInventoryVisible")); Assert.AreEqual(visible, (int)Get(text, "maxVisibleCharacters"));
        Canvas inventory = host.GetComponentsInChildren<Canvas>(true).First(c => c.name == "InventoryLayer");
        Assert.Greater(inventory.sortingOrder, Find("EventOverlayCanvas").GetComponent<Canvas>().sortingOrder);
        allowPaidChoice = true; Invoke(hud, "HideInventoryPanel"); Advance(.1f);
        Assert.IsFalse((bool)Get(hud, "IsInventoryVisible")); Assert.IsTrue(Find("Option_01").GetComponent<Button>().interactable);
        Assert.AreSame(selection, Get(overlay, "selectedOption")); Assert.AreEqual(0, executions);
    }

    [UnityTest] public IEnumerator InterruptedOpeningCanBeReopenedWithoutStaleTextOrHiddenCallbacks()
    {
        Advance(.1f); Invoke(overlay, "Hide"); Advance(5);
        Assert.IsFalse((bool)Get(overlay, "IsVisible")); Assert.AreEqual(1, hidden);
        Show(); Assert.IsTrue((bool)Get(overlay, "IsTyping")); Advance(.5f); yield return null;
        Assert.IsTrue((bool)Get(overlay, "IsVisible")); Assert.IsTrue((bool)Get(overlay, "IsTyping"));
        Assert.AreEqual(1, hidden); Assert.AreEqual(0, executions);
    }

    private void Show(Delegate preview = null) => Invoke(overlay, "Show", Activator.CreateInstance(TypeOf("BoardTileData")), data, BuildSelection(), preview);
    private void Advance(float delta) => Invoke(overlay, "AdvancePresentation", delta);
    private Delegate BuildSelection()
    {
        Type type = overlay.GetType().GetNestedType("EventOptionSelectionHandler");
        ParameterExpression[] args = type.GetMethod("Invoke").GetParameters().Select(p => Expression.Parameter(p.ParameterType, p.Name)).ToArray();
        return Expression.Lambda(type, Expression.Block(
            Expression.Call(Expression.Constant(this), GetType().GetMethod("RecordExecution", Members)),
            Expression.Assign(args[3], Expression.Constant("Received supplies.")),
            Expression.Assign(args[4], Expression.Constant("You leave with the supplies and follow the path deeper into the forest.")),
            Expression.Assign(args[5], Expression.Constant(null, TypeOf("MapEvent_SO"))), Expression.Constant(true)), args).Compile();
    }
    private Delegate BuildPreview()
    {
        Type type = overlay.GetType().GetNestedType("EventOptionPreviewHandler"), valueType = overlay.GetType().GetNestedType("EventOptionPreviewData");
        ParameterExpression[] args = type.GetMethod("Invoke").GetParameters().Select(p => Expression.Parameter(p.ParameterType, p.Name)).ToArray();
        object first = ((IList)Get(data, "options"))[0];
        Expression available = Expression.OrElse(Expression.NotEqual(args[2], Expression.Constant(first, TypeOf("MapEventOptionData"))), Expression.Field(Expression.Constant(this), GetType().GetField("allowPaidChoice", Members)));
        return Expression.Lambda(type, Expression.MemberInit(Expression.New(valueType), Expression.Bind(valueType.GetField("isSelectable"), available)), args).Compile();
    }
    private void RecordExecution() => executions++;
    private void Click(Transform target)
    {
        Canvas.ForceUpdateCanvases(); RectTransform rect = (RectTransform)target;
        Vector2 point = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
        List<RaycastResult> hits = Hits(point); Assert.IsNotEmpty(hits);
        Assert.IsTrue(hits[0].gameObject.transform.IsChildOf(target) || hits[0].gameObject.transform == target, "Top native hit: " + hits[0].gameObject.name);
        ExecuteEvents.ExecuteHierarchy(hits[0].gameObject, new PointerEventData(events) { position = point, button = PointerEventData.InputButton.Left }, ExecuteEvents.pointerClickHandler);
    }
    private List<RaycastResult> Hits(Vector2 point) { var hits = new List<RaycastResult>(); events.RaycastAll(new PointerEventData(events) { position = point }, hits); return hits; }
    private Transform Find(string name) => host.GetComponentsInChildren<Transform>(true).First(t => t.name == name);
    private static Type TypeOf(string name) => AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(name)).First(t => t != null);
    private static object Invoke(object target, string name, params object[] args) => target.GetType().GetMethod(name, Members).Invoke(target, args);
    private static void Set(object target, string name, object value) => target.GetType().GetField(name, Members).SetValue(target, value);
    private static object Get(object target, string name) { FieldInfo field = target.GetType().GetField(name, Members); return field != null ? field.GetValue(target) : target.GetType().GetProperty(name, Members).GetValue(target); }
}
