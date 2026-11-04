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

public sealed class MapInventoryDetailPlayModeTests
{
    private const BindingFlags Members = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
    private GameObject host;
    private Component view;
    private EventSystem events;
    private ScriptableObject loot, relic;
    private float oldTimeScale;

    [SetUp]
    public void SetUp()
    {
        oldTimeScale = Time.timeScale;
        host = new GameObject("ItemDetailPlayTests", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
        host.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        host.GetComponent<Canvas>().sortingOrder = 30000;
        events = EventSystem.current != null ? EventSystem.current : host.AddComponent<EventSystem>();
        view = (Component)TypeOf("MapInventoryDetailView").GetMethod("Create").Invoke(null, new object[] { (RectTransform)host.transform, "InventoryDetail" });
        Type database = TypeOf("UnityEditor.AssetDatabase");
        string path = (string)database.GetMethod("GUIDToAssetPath", new[] { typeof(string) }).Invoke(null, new object[] { "c6e7143cd2df45b45a784ef8c61a210f" });
        object font = database.GetMethod("LoadAssetAtPath", new[] { typeof(string), typeof(Type) }).Invoke(null, new object[] { path, TypeOf("TMPro.TMP_FontAsset") });
        Invoke("ConfigureFonts", font, font);
        loot = ScriptableObject.CreateInstance(TypeOf("LootItem_SO")); Set(loot, "lootName", "磁电晶"); Set(loot, "description", "拿在手上感觉有电流缓缓在皮肤上流动");
        relic = ScriptableObject.CreateInstance(TypeOf("Relic_SO")); Set(relic, "relicName", "长说明遗物");
        Set(relic, "description", string.Join("\n", Enumerable.Repeat("获得金币，恢复行动点。保留多段完整机制说明。", 24)));
        Set(relic, "additionalDescription", "特殊条件与联动说明，不能被底部按钮遮住。");
        Show(loot, "RunLootState", "lootItem");
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        Time.timeScale = oldTimeScale;
        UnityEngine.Object.Destroy(host); UnityEngine.Object.Destroy(loot); UnityEngine.Object.Destroy(relic);
        yield return null;
    }

    [UnityTest]
    public IEnumerator PausedCloseRetainsBackdropUntilItFinishesAndOnlyNotifiesOnce()
    {
        Time.timeScale = 0; yield return new WaitForSecondsRealtime(0.32f);
        Assert.Greater(((CanvasGroup)Get(view, "panelGroup")).alpha, 0.99f);
        int hidden = 0; view.GetType().GetEvent("Hidden").AddEventHandler(view, (Action)(() => hidden++));
        Click(Find("CloseButton")); Invoke("Hide");
        Assert.AreEqual(0, hidden); Assert.IsTrue((bool)Get(view, "IsVisible"));
        Assert.AreEqual("InventoryDetailModal", Hits(new Vector2(3, 3))[0].gameObject.name);
        yield return new WaitForSecondsRealtime(0.24f);
        Assert.AreEqual(1, hidden); Assert.IsFalse((bool)Get(view, "IsVisible"));
        Invoke("Hide"); Assert.AreEqual(1, hidden);
        Show(loot, "RunLootState", "lootItem"); yield return new WaitForSecondsRealtime(0.3f);
        Assert.IsTrue((bool)Get(view, "IsVisible"));
        Assert.IsFalse(((Button)Get(view, "actionButton")).gameObject.activeSelf);
    }

    [UnityTest]
    public IEnumerator TouchAndWheelScrollDoNotDismissOrObscureTheFooter()
    {
        Show(relic, "RunRelicState", "relicData"); yield return new WaitForSecondsRealtime(0.3f);
        ScrollRect scroll = (ScrollRect)Get(view, "descriptionScroll");
        Vector2 point = ScreenPoint(scroll.viewport);
        var touch = new PointerEventData(events) { pointerId = 3, button = PointerEventData.InputButton.Left, position = point, pressPosition = point };
        ExecuteEvents.Execute(scroll.gameObject, touch, ExecuteEvents.initializePotentialDrag);
        ExecuteEvents.Execute(scroll.gameObject, touch, ExecuteEvents.beginDragHandler);
        touch.position += Vector2.up * 140; touch.delta = Vector2.up * 140;
        ExecuteEvents.Execute(scroll.gameObject, touch, ExecuteEvents.dragHandler);
        ExecuteEvents.Execute(scroll.gameObject, touch, ExecuteEvents.endDragHandler);
        yield return null;
        Assert.Less(scroll.verticalNormalizedPosition, 0.99f);
        Assert.IsTrue((bool)Get(view, "IsVisible"));
        ExecuteEvents.Execute(scroll.gameObject, new PointerEventData(events) { position = point, scrollDelta = new Vector2(0, -6) }, ExecuteEvents.scrollHandler);
        scroll.verticalNormalizedPosition = 0; yield return null;
        Assert.IsTrue(((Component)Get(view, "relicAdditionalDescriptionText")).gameObject.activeInHierarchy);
        Click(Find("CloseButton")); yield return new WaitForSecondsRealtime(0.24f);
        Assert.IsFalse((bool)Get(view, "IsVisible"));
    }

    [UnityTest]
    public IEnumerator PausedButtonFeedbackScalesAroundItsCenterAndDisabledActionsCannotRun()
    {
        Time.timeScale = 0; yield return new WaitForSecondsRealtime(0.3f);
        int calls = 0;
        Invoke("ConfigureActionButton", "无法装备", false, (UnityEngine.Events.UnityAction)(() => calls++), "当前队伍中没有适配该武器的角色。");
        Button action = (Button)Get(view, "actionButton");
        Click(action.transform); Assert.AreEqual(0, calls);
        Transform close = Find("CloseButton"); Vector3 center = close.TransformPoint(((RectTransform)close).rect.center);
        var pointer = new PointerEventData(events) { button = PointerEventData.InputButton.Left, position = ScreenPoint((RectTransform)close) };
        ExecuteEvents.Execute(close.gameObject, pointer, ExecuteEvents.pointerEnterHandler);
        yield return new WaitForSecondsRealtime(0.12f);
        Assert.Greater(close.localScale.x, 1.01f);
        ExecuteEvents.Execute(close.gameObject, pointer, ExecuteEvents.pointerDownHandler);
        yield return new WaitForSecondsRealtime(0.12f);
        Assert.Less(close.localScale.x, 0.98f);
        Assert.That(Vector3.Distance(center, close.TransformPoint(((RectTransform)close).rect.center)), Is.LessThan(0.01f));
        ExecuteEvents.Execute(close.gameObject, pointer, ExecuteEvents.pointerUpHandler);
        ExecuteEvents.Execute(close.gameObject, pointer, ExecuteEvents.pointerExitHandler);
    }

    [UnityTest]
    public IEnumerator ReopeningDuringCloseCancelsTheOldCloseAndPanelClicksStayInside()
    {
        yield return new WaitForSecondsRealtime(0.3f);
        int hidden = 0; view.GetType().GetEvent("Hidden").AddEventHandler(view, (Action)(() => hidden++));
        Click(Find("Title")); Assert.IsTrue((bool)Get(view, "IsVisible"));
        Invoke("Hide"); yield return new WaitForSecondsRealtime(0.05f);
        Show(relic, "RunRelicState", "relicData"); yield return new WaitForSecondsRealtime(0.3f);
        Assert.AreEqual(0, hidden); Assert.IsTrue((bool)Get(view, "IsVisible"));
        Assert.AreEqual("长说明遗物", Get(Get(view, "titleText"), "text"));
        Assert.AreEqual("InventoryDetailModal", Hits(new Vector2(3, 3))[0].gameObject.name);
        ExecuteEvents.ExecuteHierarchy(Hits(new Vector2(3, 3))[0].gameObject, new PointerEventData(events) { button = PointerEventData.InputButton.Left }, ExecuteEvents.pointerClickHandler);
        yield return new WaitForSecondsRealtime(0.24f);
        Assert.AreEqual(1, hidden);
    }

    private void Show(ScriptableObject asset, string stateType, string field)
    {
        object state = Activator.CreateInstance(TypeOf(stateType)); Set(state, field, asset); Set(state, "amount", 1);
        view.GetType().GetMethods().Single(m => m.Name == "Show" && m.GetParameters()[0].ParameterType == state.GetType()).Invoke(view, new[] { state, null });
    }
    private void Click(Transform target)
    {
        Vector2 point = ScreenPoint((RectTransform)target); List<RaycastResult> hits = Hits(point); Assert.IsNotEmpty(hits);
        // Text is non-intercepting: it may resolve to the owning panel or button.
        ExecuteEvents.ExecuteHierarchy(hits[0].gameObject, new PointerEventData(events) { position = point, button = PointerEventData.InputButton.Left }, ExecuteEvents.pointerClickHandler);
    }
    private static Vector2 ScreenPoint(RectTransform rect) => RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
    private List<RaycastResult> Hits(Vector2 point)
    {
        Canvas.ForceUpdateCanvases(); var hits = new List<RaycastResult>(); events.RaycastAll(new PointerEventData(events) { position = point }, hits); return hits;
    }
    private Transform Find(string name) => host.GetComponentsInChildren<Transform>(true).First(t => t.name == name);
    private object Invoke(string method, params object[] args) => view.GetType().GetMethod(method, Members).Invoke(view, args);
    private static Type TypeOf(string name) => AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(name)).First(t => t != null);
    private static void Set(object target, string name, object value) => target.GetType().GetField(name, Members).SetValue(target, value);
    private static object Get(object target, string name) { FieldInfo field = target.GetType().GetField(name, Members); return field != null ? field.GetValue(target) : target.GetType().GetProperty(name, Members).GetValue(target); }
}
#endif
