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

public sealed class MapMerchantOverlayPlayModeTests
{
    private GameObject host;
    private Component overlay;
    private object state, tile;
    private EventSystem events;
    private float previousTimeScale;
    private int bought, refreshed;
    private readonly List<ScriptableObject> owned = new List<ScriptableObject>();
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    [SetUp]
    public void SetUp()
    {
        bought = refreshed = 0;
        previousTimeScale = Time.timeScale;
        host = new GameObject("MerchantPlayTest");
        events = EventSystem.current != null ? EventSystem.current : host.AddComponent<EventSystem>();
        overlay = host.AddComponent(TypeOf("MapMerchantOverlay"));
        // This test assembly deliberately has no dependency on Assembly-CSharp or TMP.
        // Load the real project face in editor runs through the public AssetDatabase API.
        Type database = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("UnityEditor.AssetDatabase")).FirstOrDefault(t => t != null);
        if (database != null)
        {
            string path = (string)database.GetMethod("GUIDToAssetPath", new[] { typeof(string) }).Invoke(null, new object[] { "c6e7143cd2df45b45a784ef8c61a210f" });
            object font = database.GetMethod("LoadAssetAtPath", new[] { typeof(string), typeof(Type) }).Invoke(null, new object[] { path, TypeOf("TMPro.TMP_FontAsset") });
            Invoke("ConfigureFonts", font, font);
        }
        state = Activator.CreateInstance(TypeOf("RunState")); Set(state, "money", 100);
        tile = Activator.CreateInstance(TypeOf("BoardTileData"));
        IList offers = (IList)Get(tile, "merchantOffers");
        for (int i = 0; i < 12; i++)
        {
            ScriptableObject item = ScriptableObject.CreateInstance(TypeOf("LootItem_SO")); owned.Add(item);
            Set(item, "lootName", "Stock " + i);
            Set(item, "rarity", Enum.ToObject(item.GetType().GetField("rarity").FieldType, i % 5));
            Set(item, "description", string.Join("\n", Enumerable.Repeat("获得金币，恢复行动点，获得钥匙。", 32)));
            object offer = Activator.CreateInstance(TypeOf("MerchantOfferState"));
            Set(offer, "lootItem", item); Set(offer, "price", 20); offers.Add(offer);
        }
        Invoke("Show", state, tile,
            (Func<object, object, string>)((t, offer) => { bought++; Set(offer, "isSoldOut", true); Set(state, "money", (int)Get(state, "money") - 20); return "支付 20 金币。"; }),
            (Func<object, string>)(t => { refreshed++; Set(state, "money", (int)Get(state, "money") - 5); ((IList)Get(t, "merchantOffers")).RemoveAt(0); return "支付 5 金币。"; }),
            (Func<object, int>)(t => 5));
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        Time.timeScale = previousTimeScale;
        UnityEngine.Object.Destroy(host);
        foreach (ScriptableObject item in owned) UnityEngine.Object.Destroy(item);
        owned.Clear();
        yield return null;
    }

    [UnityTest]
    public IEnumerator NativeSelectionPreservesRarityAndBackdropBlocksMapUntilClosed()
    {
        yield return null; yield return null;
        object name = Get(overlay, "goodsNameText");
        Assert.AreEqual("Stock 0", Get(name, "text"));
        Click(Find("Offer_01"));
        yield return null;
        Assert.AreEqual("Stock 1", Get(name, "text"));
        Assert.AreEqual(new Color32(56, 178, 86, 255), (Color32)(Color)Get(name, "color"));
        Assert.AreEqual(100, Get(state, "money")); Assert.AreEqual(0, bought);
        Assert.AreEqual("MerchantOverlayCanvas", Hits(new Vector2(3, 3))[0].gameObject.name);
        int hidden = 0;
        overlay.GetType().GetEvent("Hidden").AddEventHandler(overlay, (Action)(() => hidden++));
        Click(Find("CloseButton"));
        yield return null;
        Assert.IsFalse((bool)Get(overlay, "IsVisible"));
        Invoke("Hide"); Assert.AreEqual(1, hidden);
        Assert.IsFalse(Hits(new Vector2(3, 3)).Any(h => h.gameObject.name == "MerchantOverlayCanvas"));
    }

    [UnityTest]
    public IEnumerator NativeBuyAndRefreshInvokeOnceAndRebindNewStock()
    {
        yield return null; yield return null;
        Click(Find("BuyButton"));
        yield return null;
        Assert.AreEqual(1, bought); Assert.AreEqual(80, Get(state, "money"));
        Assert.IsFalse(((Button)Get(overlay, "buyButton")).interactable);
        Click(Find("BuyButton")); yield return null;
        Assert.AreEqual(1, bought);
        Click(Find("RefreshButton")); yield return null;
        Assert.AreEqual(1, refreshed); Assert.AreEqual(75, Get(state, "money"));
        Assert.AreEqual("Stock 1", Get(Get(overlay, "goodsNameText"), "text"));
        Assert.IsTrue(((Button)Get(overlay, "buyButton")).interactable);
        Assert.AreEqual("支付 5 金币。", Get(Get(overlay, "statusText"), "text"));
    }

    [UnityTest]
    public IEnumerator StockAndDetailsScrollIndependentlyWhilePaused()
    {
        Time.timeScale = 0;
        yield return null; yield return null;
        ScrollRect stock = (ScrollRect)Get(overlay, "offerScrollRect"), detail = (ScrollRect)Get(overlay, "detailScrollRect");
        Assert.IsTrue(stock.verticalScrollbar.gameObject.activeSelf);
        Assert.IsTrue(detail.verticalScrollbar.gameObject.activeSelf);
        stock.OnScroll(new PointerEventData(events) { scrollDelta = new Vector2(0, -8) });
        yield return null;
        Assert.Greater(stock.content.anchoredPosition.y, 0);
        Assert.AreEqual(0, detail.content.anchoredPosition.y, 0.1);
        float stockPosition = stock.content.anchoredPosition.y;
        detail.OnScroll(new PointerEventData(events) { scrollDelta = new Vector2(0, -8) });
        yield return null;
        Assert.Greater(detail.content.anchoredPosition.y, 0);
        Assert.AreEqual(stockPosition, stock.content.anchoredPosition.y, 0.1);
        Assert.IsTrue(((Button)Get(overlay, "buyButton")).gameObject.activeInHierarchy);
    }

    private void Click(Transform target)
    {
        Canvas.ForceUpdateCanvases(); RectTransform rect = (RectTransform)target;
        Vector2 point = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
        List<RaycastResult> hits = Hits(point); Assert.IsNotEmpty(hits);
        Assert.IsTrue(hits[0].gameObject.transform == target || hits[0].gameObject.transform.IsChildOf(target), hits[0].gameObject.name);
        ExecuteEvents.ExecuteHierarchy(hits[0].gameObject, new PointerEventData(events) { position = point, button = PointerEventData.InputButton.Left }, ExecuteEvents.pointerClickHandler);
    }
    private List<RaycastResult> Hits(Vector2 point) { List<RaycastResult> hits = new List<RaycastResult>(); events.RaycastAll(new PointerEventData(events) { position = point }, hits); return hits; }
    private Transform Find(string name) => host.GetComponentsInChildren<Transform>(true).First(t => t.name == name);
    private object Invoke(string method, params object[] args) => overlay.GetType().GetMethod(method, Members).Invoke(overlay, args);
    private static Type TypeOf(string name) => AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(name)).First(t => t != null);
    private static void Set(object target, string field, object value) => target.GetType().GetField(field, Members).SetValue(target, value);
    private static object Get(object target, string name) { FieldInfo field = target.GetType().GetField(name, Members); return field != null ? field.GetValue(target) : target.GetType().GetProperty(name, Members).GetValue(target); }
}
