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

public sealed class MapCraftingOverlayPlayModeTests
{
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private GameObject host;
    private Component overlay;
    private object state;
    private ScriptableObject material;
    private readonly List<ScriptableObject> owned = new List<ScriptableObject>();
    private readonly List<ScriptableObject> recipes = new List<ScriptableObject>();
    private EventSystem events;
    private float previousTimeScale;
    private int calls;

    [SetUp]
    public void SetUp()
    {
        calls = 0; previousTimeScale = Time.timeScale;
        host = new GameObject("CraftingPlayTest");
        events = EventSystem.current != null ? EventSystem.current : host.AddComponent<EventSystem>();
        overlay = host.AddComponent(TypeOf("MapCraftingOverlay"));
        Type database = TypeOf("UnityEditor.AssetDatabase");
        string path = (string)database.GetMethod("GUIDToAssetPath", new[] { typeof(string) }).Invoke(null, new object[] { "c6e7143cd2df45b45a784ef8c61a210f" });
        object font = database.GetMethod("LoadAssetAtPath", new[] { typeof(string), typeof(Type) }).Invoke(null, new object[] { path, TypeOf("TMPro.TMP_FontAsset") });
        Invoke(overlay, "ConfigureFonts", font, font);
        state = Activator.CreateInstance(TypeOf("RunState")); Set(state, "money", 8);
        material = Make("LootItem_SO"); Set(material, "lootName", "测试矿石");
        for (int i = 0; i < 12; i++)
        {
            ScriptableObject recipe = Make("CraftingBlueprint_SO"); Set(recipe, "blueprintName", "卷轴 " + i);
            Set(recipe, "description", "记载着如何打造武器的卷轴。");
            ScriptableObject weapon = Make("Weapon_SO"); Set(weapon, "weaponName", "成品 " + i); Set(recipe, "targetWeapon", weapon);
            AddRequirement(recipe, material);
            Invoke(state, "AddBlueprint", recipe, 1); recipes.Add(recipe);
        }
        Show();
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        Time.timeScale = previousTimeScale;
        UnityEngine.Object.Destroy(host);
        foreach (ScriptableObject item in owned) UnityEngine.Object.Destroy(item);
        owned.Clear(); recipes.Clear();
        yield return null;
    }

    [UnityTest]
    public IEnumerator NativeRecipeClickChangesPreviewAndShortageCannotCallHandler()
    {
        yield return new WaitForSecondsRealtime(0.3f);
        Click(Find("Blueprint_01")); yield return null;
        Assert.AreEqual("成品 1", Get(Get(overlay, "weaponNameText"), "text"));
        Assert.AreEqual("缺材料", Get(Find("Blueprint_01").Find("Readiness").GetComponent(TypeOf("TMPro.TMP_Text")), "text"));
        Assert.IsFalse(((Button)Get(overlay, "craftButton")).interactable);
        Click(Find("CraftButton")); Assert.AreEqual(0, calls);
        Assert.AreEqual(8, Get(state, "money"));
        Assert.AreEqual("CraftingOverlayCanvas", Hits(new Vector2(3, 3))[0].gameObject.name);
    }

    [UnityTest]
    public IEnumerator TouchListAndMaterialScrollRemainIndependentWhilePaused()
    {
        Time.timeScale = 0;
        for (int i = 0; i < 8; i++)
        {
            ScriptableObject extra = Make("LootItem_SO"); Set(extra, "lootName", "额外材料 " + i); AddRequirement(recipes[0], extra);
        }
        Show(); yield return new WaitForSecondsRealtime(0.3f);
        Assert.Greater(((CanvasGroup)Get(overlay, "panelGroup")).alpha, 0.99f);
        ScrollRect list = (ScrollRect)Get(overlay, "blueprintScroll"), materials = (ScrollRect)Get(overlay, "materialScroll");
        Assert.IsTrue(list.verticalScrollbar.gameObject.activeSelf); Assert.IsTrue(materials.verticalScrollbar.gameObject.activeSelf);
        Vector2 point = RectTransformUtility.WorldToScreenPoint(null, list.viewport.TransformPoint(list.viewport.rect.center));
        PointerEventData touch = new PointerEventData(events) { pointerId = 0, button = PointerEventData.InputButton.Left, position = point, pressPosition = point };
        list.OnBeginDrag(touch); touch.position += Vector2.up * 150; touch.delta = Vector2.up * 150;
        list.OnDrag(touch); list.OnEndDrag(touch); yield return null;
        Assert.Greater(list.content.anchoredPosition.y, 0); Assert.AreEqual(0, materials.content.anchoredPosition.y, 0.1);
        list.StopMovement(); float before = list.content.anchoredPosition.y;
        materials.OnScroll(new PointerEventData(events) { scrollDelta = new Vector2(0, -8) }); yield return null;
        Assert.Greater(materials.content.anchoredPosition.y, 0); Assert.AreEqual(before, list.content.anchoredPosition.y, 0.1);
        Transform close = Find("CloseButton");
        ExecuteEvents.Execute(close.gameObject, new PointerEventData(events), ExecuteEvents.pointerEnterHandler);
        yield return new WaitForSecondsRealtime(0.15f); Assert.Greater(close.localScale.x, 1.01f);
        ExecuteEvents.Execute(close.gameObject, new PointerEventData(events) { button = PointerEventData.InputButton.Left }, ExecuteEvents.pointerDownHandler);
        yield return new WaitForSecondsRealtime(0.15f); Assert.Less(close.localScale.x, 0.98f);
        ExecuteEvents.Execute(close.gameObject, new PointerEventData(events), ExecuteEvents.pointerUpHandler);
        ExecuteEvents.Execute(close.gameObject, new PointerEventData(events), ExecuteEvents.pointerExitHandler);
        events.SetSelectedGameObject(null);
        yield return new WaitForSecondsRealtime(0.2f); Assert.AreEqual(1, close.localScale.x, 0.002f);
    }

    [UnityTest]
    public IEnumerator CraftDebouncesNativeClicksUpdatesCountsAndPreservesScrolledSelection()
    {
        Invoke(state, "AddLoot", material, 4); Show(); yield return new WaitForSecondsRealtime(0.3f);
        ScrollRect list = (ScrollRect)Get(overlay, "blueprintScroll");
        list.verticalNormalizedPosition = 0; yield return null;
        Click(Find("Blueprint_10")); yield return null;
        float before = list.content.anchoredPosition.y;
        Click(Find("CraftButton")); Click(Find("CraftButton")); yield return null;
        Assert.AreEqual(1, calls); Assert.AreEqual(2, Invoke(state, "GetLootAmount", material));
        Assert.AreEqual(1, Invoke(state, "GetWeaponAmount", Get(recipes[10], "targetWeapon")));
        Assert.AreEqual(1, Invoke(state, "GetBlueprintAmount", recipes[10]));
        Assert.AreSame(recipes[10], Get(overlay, "selectedBlueprint"));
        Assert.AreEqual(before, list.content.anchoredPosition.y, 0.2f);
        Assert.Greater((float)Get(overlay, "feedbackTime"), 0);
        yield return new WaitForSecondsRealtime(0.8f);
        Assert.IsTrue(((Button)Get(overlay, "craftButton")).interactable);
        Click(Find("CraftButton")); yield return null;
        Assert.AreEqual(2, calls); Assert.AreEqual(0, Invoke(state, "GetLootAmount", material));
        Assert.AreEqual("0 / 2", Get(Find("Material_00").Find("Count").GetComponent(TypeOf("TMPro.TMP_Text")), "text"));
        foreach (Transform card in list.content) Assert.AreEqual("缺材料", Get(card.Find("Readiness").GetComponent(TypeOf("TMPro.TMP_Text")), "text"));
        StringAssert.Contains("成功锻造", (string)Get(Get(overlay, "statusText"), "text"));
    }

    [UnityTest]
    public IEnumerator ClosingKeepsBackdropUntilFinishedAndReopeningStartsFreshWhilePaused()
    {
        Time.timeScale = 0; yield return new WaitForSecondsRealtime(0.3f);
        int hidden = 0; overlay.GetType().GetEvent("Hidden").AddEventHandler(overlay, (Action)(() => hidden++));
        Click(Find("CloseButton")); Invoke(overlay, "Hide");
        Assert.AreEqual(0, hidden); Assert.IsTrue((bool)Get(overlay, "IsVisible"));
        Assert.AreEqual("CraftingOverlayCanvas", Hits(new Vector2(3, 3))[0].gameObject.name);
        yield return new WaitForSecondsRealtime(0.25f);
        Assert.AreEqual(1, hidden); Assert.IsFalse((bool)Get(overlay, "IsVisible"));
        Assert.IsFalse(Hits(new Vector2(3, 3)).Any(h => h.gameObject.name == "CraftingOverlayCanvas"));
        Invoke(overlay, "Hide"); Assert.AreEqual(1, hidden);
        Show(); yield return new WaitForSecondsRealtime(0.3f);
        Assert.IsTrue((bool)Get(overlay, "IsVisible"));
        Assert.AreEqual("", Get(Get(overlay, "statusText"), "text"));
        Assert.Greater(((CanvasGroup)Get(overlay, "panelGroup")).alpha, 0.99f);
    }

    private void Show() => Invoke(overlay, "Show", state, (Func<object, string>)(recipe =>
    {
        calls++;
        object[] args = { state, recipe, null };
        TypeOf("RunCraftingService").GetMethod("TryCraft").Invoke(null, args);
        return (string)args[2];
    }));
    private ScriptableObject Make(string type) { ScriptableObject item = ScriptableObject.CreateInstance(TypeOf(type)); owned.Add(item); return item; }
    private static void AddRequirement(object recipe, object ingredient)
    {
        object requirement = Activator.CreateInstance(TypeOf("CraftingMaterialRequirement")); Set(requirement, "lootItem", ingredient); Set(requirement, "amount", 2);
        ((IList)Get(recipe, "materialRequirements")).Add(requirement);
    }
    private void Click(Transform target)
    {
        Canvas.ForceUpdateCanvases(); RectTransform rect = (RectTransform)target;
        Vector2 point = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
        List<RaycastResult> hits = Hits(point); Assert.IsNotEmpty(hits);
        Assert.IsTrue(hits[0].gameObject.transform == target || hits[0].gameObject.transform.IsChildOf(target), hits[0].gameObject.name);
        ExecuteEvents.ExecuteHierarchy(hits[0].gameObject, new PointerEventData(events) { position = point, button = PointerEventData.InputButton.Left }, ExecuteEvents.pointerClickHandler);
    }
    private List<RaycastResult> Hits(Vector2 point) { var hits = new List<RaycastResult>(); events.RaycastAll(new PointerEventData(events) { position = point }, hits); return hits; }
    private Transform Find(string name) => host.GetComponentsInChildren<Transform>(true).First(t => t.name == name && t.gameObject.activeInHierarchy);
    private static Type TypeOf(string name) => AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(name)).First(t => t != null);
    private static object Invoke(object target, string method, params object[] args) => target.GetType().GetMethod(method, Members).Invoke(target, args);
    private static void Set(object target, string field, object value) => target.GetType().GetField(field, Members).SetValue(target, value);
    private static object Get(object target, string name) { FieldInfo field = target.GetType().GetField(name, Members); return field != null ? field.GetValue(target) : target.GetType().GetProperty(name, Members).GetValue(target); }
}
#endif
