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

public sealed class MapTreasureRemasterPlayModeTests
{
    private object fixture;
    private Component overlay;
    private GameObject host;
    private float oldTimeScale;
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    [SetUp]
    public void SetUp()
    {
        oldTimeScale = Time.timeScale;
        fixture = TypeOf("MapTreasureVisualTools").GetMethod("CreatePlayFixture").Invoke(null, null);
        overlay = (Component)Get(fixture, "overlay"); host = (GameObject)Get(fixture, "host");
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        Time.timeScale = oldTimeScale;
        fixture.GetType().GetMethod("Dispose").Invoke(fixture, null);
        yield return null;
    }

    [UnityTest]
    public IEnumerator PausedOpeningNativeClaimAndClosingKeepTheMapShieldUntilFinished()
    {
        Time.timeScale = 0;
        yield return WaitFor(() => ((CanvasGroup)Get(overlay, "windowGroup")).alpha > 0.99f);
        Assert.Greater(((CanvasGroup)Get(overlay, "windowGroup")).alpha, 0.99f);
        Click(Find("TreasureChoice_0").Find("ChooseButton"));
        Assert.AreEqual(1, Get(fixture, "claimCalls"));
        Assert.AreEqual(Get(fixture, "expectedMoney"), Get(Get(fixture, "state"), "money"));
        Assert.IsTrue((bool)Get(Get(fixture, "tile"), "isResolved"));
        yield return null;
        Click(Find("ContinueButton"));
        Assert.IsTrue((bool)Get(overlay, "IsVisible"));
        Assert.IsFalse(((CanvasGroup)Get(overlay, "windowGroup")).interactable);
        Assert.AreEqual("TreasureOverlayCanvas", Hits(new Vector2(3, 3))[0].gameObject.name);
        yield return WaitFor(() => !(bool)Get(overlay, "IsVisible"));
        Assert.IsFalse((bool)Get(overlay, "IsVisible")); Assert.AreEqual(1, Get(fixture, "hiddenCalls"));
        fixture.GetType().GetMethod("Show").Invoke(fixture, new object[] { 3 });
        yield return WaitFor(() => ((CanvasGroup)Get(overlay, "windowGroup")).alpha > 0.99f);
        Assert.IsTrue(Find("CardRoot").gameObject.activeSelf); Assert.IsFalse(Find("Result").gameObject.activeSelf);
        Assert.AreEqual(1, Get(fixture, "hiddenCalls"));
    }

    [UnityTest]
    public IEnumerator CardsStaggerAndButtonsLiftPressAndRecoverWhilePaused()
    {
        Time.timeScale = 0;
        // Warm the actual fonts and artwork before observing a fresh card entrance.
        yield return WaitFor(() => Find("TreasureChoice_2").GetComponent<CanvasGroup>().alpha > 0.99f);
        fixture.GetType().GetMethod("Show").Invoke(fixture, new object[] { 3 });
        yield return null;
        Assert.Greater(Find("TreasureChoice_0").GetComponent<CanvasGroup>().alpha, Find("TreasureChoice_2").GetComponent<CanvasGroup>().alpha);
        yield return WaitFor(() => Find("TreasureChoice_2").GetComponent<CanvasGroup>().alpha > 0.99f && ((CanvasGroup)Get(overlay, "windowGroup")).alpha > 0.99f);
        Transform target = Find("TreasureChoice_1").Find("ChooseButton");
        var pointer = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left };
        ExecuteEvents.Execute(target.gameObject, pointer, ExecuteEvents.pointerEnterHandler);
        yield return new WaitForSecondsRealtime(0.14f); Assert.Greater(target.localScale.x, 1.01f);
        ExecuteEvents.Execute(target.gameObject, pointer, ExecuteEvents.pointerDownHandler);
        yield return new WaitForSecondsRealtime(0.14f); Assert.Less(target.localScale.x, 0.98f);
        ExecuteEvents.Execute(target.gameObject, pointer, ExecuteEvents.pointerUpHandler);
        ExecuteEvents.Execute(target.gameObject, pointer, ExecuteEvents.pointerExitHandler);
        EventSystem.current.SetSelectedGameObject(null);
        yield return new WaitForSecondsRealtime(0.2f); Assert.AreEqual(1, target.localScale.x, 0.003f);
        Assert.AreEqual(0, Get(fixture, "claimCalls"));
    }

    [UnityTest]
    public IEnumerator NativeBagSelectionRequiresConfirmationAndClaimsExactlyOnce()
    {
        yield return WaitFor(() => ((CanvasGroup)Get(overlay, "windowGroup")).alpha > 0.99f);
        Click(Find("TreasureChoice_2").Find("ChooseButton"));
        Assert.AreEqual(1, Get(fixture, "claimCalls")); Assert.IsFalse((bool)Get(Get(fixture, "tile"), "isResolved"));
        Assert.IsFalse(Find("ConfirmRelicChoiceButton").GetComponent<Button>().IsInteractable());
        yield return WaitFor(() => Find("TreasureRelicChoice_2").GetComponent<CanvasGroup>().alpha > 0.99f);
        Click(Find("TreasureRelicChoice_0").Find("SelectButton"));
        Assert.IsTrue(Find("ConfirmRelicChoiceButton").GetComponent<Button>().IsInteractable());
        yield return new WaitForSecondsRealtime(0.15f);
        Assert.IsTrue(Find("TreasureRelicChoice_0").GetComponent<Outline>().enabled);
        Button confirm = Find("ConfirmRelicChoiceButton").GetComponent<Button>();
        Click(confirm.transform); confirm.onClick.Invoke();
        Assert.AreEqual(1, Get(fixture, "confirmCalls")); Assert.IsTrue((bool)Get(Get(fixture, "tile"), "isResolved"));
        Assert.IsFalse(Find("CardRoot").gameObject.activeSelf); Assert.IsTrue(Find("ContinueButton").gameObject.activeSelf);
    }

    [UnityTest]
    public IEnumerator TouchHorizontalScrollExposesAdditionalChoicesWithoutClaiming()
    {
        Time.timeScale = 0;
        fixture.GetType().GetMethod("Show").Invoke(fixture, new object[] { 6 });
        yield return WaitFor(() => Find("TreasureChoice_5").GetComponent<CanvasGroup>().alpha > 0.99f && ((CanvasGroup)Get(overlay, "windowGroup")).alpha > 0.99f);
        ScrollRect scroll = Find("CardRoot").GetComponent<ScrollRect>();
        Vector2 point = RectTransformUtility.WorldToScreenPoint(null, scroll.viewport.TransformPoint(scroll.viewport.rect.center));
        PointerEventData touch = new PointerEventData(EventSystem.current) { pointerId = 0, button = PointerEventData.InputButton.Left, position = point, pressPosition = point };
        scroll.OnBeginDrag(touch); touch.position += Vector2.left * 220; touch.delta = Vector2.left * 220;
        scroll.OnDrag(touch); scroll.OnEndDrag(touch); yield return null;
        Assert.Less(scroll.content.anchoredPosition.x, -1); Assert.AreEqual(0, Get(fixture, "claimCalls"));
        scroll.StopMovement(); scroll.horizontalNormalizedPosition = 1; yield return null;
        Transform last = Find("TreasureChoice_5").Find("ChooseButton");
        Vector2 lastPoint = RectTransformUtility.WorldToScreenPoint(null, ((RectTransform)last).TransformPoint(((RectTransform)last).rect.center));
        Assert.AreSame(last.gameObject, ExecuteEvents.GetEventHandler<IPointerClickHandler>(Hits(lastPoint)[0].gameObject));
    }

    [UnityTest]
    public IEnumerator EliteBagUsesTheSameNativeSelectionPanelAndDefersTheGrantToReturn()
    {
        object battleFixture = TypeOf("MapTreasureVisualTools").GetMethod("CreateBattlePlayFixture").Invoke(null, null);
        try
        {
            yield return new WaitForSecondsRealtime(0.35f);
            Click(((Button)Get(battleFixture, "bagButton")).transform);
            Component shared = (Component)Get(battleFixture, "Shared"); Assert.IsNotNull(shared);
            yield return WaitFor(() => ((CanvasGroup)Get(shared, "windowGroup")).alpha > 0.99f);
            Transform card = shared.GetComponentsInChildren<Transform>(true).First(t => t.name == "TreasureRelicChoice_0");
            Click(card.Find("SelectButton"));
            Click(shared.GetComponentsInChildren<Transform>(true).First(t => t.name == "ConfirmRelicChoiceButton"));
            Assert.IsFalse((bool)Get(Get(battleFixture, "choice"), "rewardApplied"));
            yield return WaitFor(() => !(bool)Get(shared, "IsVisible"));
            Assert.IsFalse((bool)Get(shared, "IsVisible"));
            Assert.IsTrue((bool)battleFixture.GetType().GetMethod("ApplyReturn").Invoke(battleFixture, null));
            Assert.IsTrue((bool)battleFixture.GetType().GetMethod("ApplyReturn").Invoke(battleFixture, null));
            var rewards = (IList)Get(Get(battleFixture, "encounter"), "resolvedRelics");
            Assert.AreEqual(1, rewards.Count); Assert.AreEqual(1, Get(rewards[0], "amount"));
        }
        finally { battleFixture.GetType().GetMethod("Dispose").Invoke(battleFixture, null); }
    }

    private Transform Find(string name) => host.GetComponentsInChildren<Transform>(true).First(t => t.name == name);
    private static IEnumerator WaitFor(Func<bool> ready)
    {
        float deadline = Time.realtimeSinceStartup + 8;
        while (!ready()) { Assert.Less(Time.realtimeSinceStartup, deadline, "UI transition did not finish"); yield return null; }
    }
    private static Type TypeOf(string name) => AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(name)).First(t => t != null);
    private static object Get(object source, string name)
    {
        Type type = source.GetType(); var field = type.GetField(name, Members);
        return field != null ? field.GetValue(source) : type.GetProperty(name, Members).GetValue(source);
    }
    private static List<RaycastResult> Hits(Vector2 point)
    {
        var hits = new List<RaycastResult>(); EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = point }, hits); return hits;
    }
    private static void Click(Transform transform)
    {
        Canvas.ForceUpdateCanvases(); RectTransform rect = (RectTransform)transform;
        Vector2 point = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
        var hits = Hits(point); Assert.IsNotEmpty(hits, "No native UGUI hit at " + transform.name);
        GameObject target = ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject);
        Assert.AreSame(transform.gameObject, target, "Unexpected native UGUI target");
        var pointer = new PointerEventData(EventSystem.current) { position = point, pressPosition = point, button = PointerEventData.InputButton.Left, pointerPress = target };
        ExecuteEvents.Execute(target, pointer, ExecuteEvents.pointerDownHandler);
        ExecuteEvents.Execute(target, pointer, ExecuteEvents.pointerUpHandler);
        ExecuteEvents.Execute(target, pointer, ExecuteEvents.pointerClickHandler);
    }
}
#endif
