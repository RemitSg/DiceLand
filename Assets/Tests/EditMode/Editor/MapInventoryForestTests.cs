using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public sealed class MapInventoryForestTests
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private GameObject host;
    private MapHUD hud;
    private readonly List<ScriptableObject> temporary = new List<ScriptableObject>();

    [SetUp] public void SetUp()
    {
        host = new GameObject("InventoryForestTests");
        hud = host.AddComponent<MapHUD>(); hud.InitializeInventoryOnly();
    }

    [TearDown] public void TearDown()
    {
        Object.DestroyImmediate(host);
        foreach (ScriptableObject asset in temporary) Object.DestroyImmediate(asset);
        temporary.Clear();
    }

    [TestCase(2340, 1080, 0, 0)]
    [TestCase(1920, 1080, 0, 0)]
    [TestCase(1600, 1200, 0, 0)]
    [TestCase(2400, 1080, 140, 50)]
    [TestCase(1080, 2340, 28, 30)]
    public void SafeAreaLayoutKeepsWholeCollectionAccessibleAndNeverCapsInventory(int width, int height, int left, int right)
    {
        var state = new RunState();
        for (int i = 0; i < 65; i++)
        {
            LootItem_SO item = ScriptableObject.CreateInstance<LootItem_SO>(); temporary.Add(item);
            item.lootName = "很长的冒险材料名称与完整后缀" + i;
            state.AddLoot(item, 12345 + i);
        }
        hud.Refresh(state, null); hud.ShowInventoryPanel();
        Rect safe = new Rect(left, 24, width - left - right, height - 48);
        hud.ApplyViewport(new Vector2(width, height), safe);
        Canvas.ForceUpdateCanvases();
        RectTransform panel = Field<RectTransform>("inventoryPanelRect");
        Vector2 available = safe.size * (1080f / height);
        Assert.LessOrEqual(panel.sizeDelta.x, available.x);
        Assert.LessOrEqual(panel.sizeDelta.y + 80, available.y);
        Assert.AreEqual(new Vector2(0.5f, 0.5f), panel.anchorMin);
        RectTransform safeRect = Field<RectTransform>("inventorySafeArea");
        Assert.That(safeRect.anchorMin.x, Is.EqualTo(safe.xMin / width).Within(0.0001f));
        Assert.That(safeRect.anchorMax.x, Is.EqualTo(safe.xMax / width).Within(0.0001f));
        var slots = Field<List<GameObject>>("inventorySlotObjects");
        Assert.AreEqual(65, slots.Count);
        Assert.Greater(Field<RectTransform>("inventoryGridRoot").sizeDelta.y, Field<RectTransform>("inventoryViewportRoot").sizeDelta.y);
        Assert.IsTrue(Field<ScrollRect>("inventoryScrollRect").vertical);
        int columns = Field<int>("inventoryGridColumns");
        Assert.That(columns, Is.InRange(2, 8));
        if (width >= 2300 && height <= 1080) Assert.AreEqual(8, columns);
        TMP_Text label = slots[0].transform.Find("ItemName").GetComponent<TMP_Text>();
        Assert.IsTrue(label.enableWordWrapping);
        Assert.AreEqual(2, label.maxVisibleLines);
        StringAssert.Contains("12345", slots[0].transform.Find("Count").GetComponent<TMP_Text>().text);
        slots[64].GetComponent<Button>().onClick.Invoke();
        Assert.AreSame(state.lootInventory[64], Field<object>("selectedInventoryState"));
        MapInventoryDetailView detail = host.GetComponentInChildren<MapInventoryDetailView>();
        Assert.IsTrue(detail.IsVisible);
        Assert.AreSame(Field<RectTransform>("inventoryLayer"), detail.transform.parent.parent.parent);
    }

    [Test]
    public void TabsKeepOriginalDetailsAndCloseRemovesAllInputBlockers()
    {
        hud.Refresh(MapInventoryVisualTools.CreateState(), null); hud.ShowInventoryPanel();
        foreach (string tab in new[] { "lootTabButton", "weaponTabButton", "blueprintTabButton", "relicTabButton" })
        {
            Field<Button>(tab).onClick.Invoke();
            var slots = Field<List<GameObject>>("inventorySlotObjects");
            Assert.Greater(slots.Count, 0, tab);
            slots[0].GetComponent<Button>().onClick.Invoke();
            Assert.IsTrue(host.GetComponentInChildren<MapInventoryDetailView>().IsVisible);
            Field<MapInventoryDetailView>("runtimeInventoryDetailView").HideImmediately();
            Assert.IsTrue(hud.IsInventoryVisible);
        }
        int hidden = 0; hud.InventoryHidden += () => hidden++;
        hud.CloseInventoryPanel(); hud.HideInventoryPanel();
        Assert.IsFalse(hud.IsInventoryVisible);
        Assert.IsFalse(Field<RectTransform>("inventoryLayer").gameObject.activeSelf);
        Assert.AreEqual(1, hidden);
        Assert.IsNull(host.transform.Find("MapHudCanvas/SafeArea/MapHudContent/TopLeftPanel"));
    }

    [Test]
    public void EventEntryOpensAboveEventAndReturnsWithoutAdvancingItsChoice()
    {
        MapEventOverlay overlay = host.AddComponent<MapEventOverlay>();
        MapTreasureArt art = MapTreasureArt.Load(); overlay.ConfigureFonts(art.headingFont, art.bodyFont);
        MapEvent_SO data = ScriptableObject.CreateInstance<MapEvent_SO>(); temporary.Add(data);
        data.options.Add(new MapEventOptionData { optionId = "Inspect", optionLabel = "观察遗迹", consequenceDescription = "保留当前选择" });
        data.options.Add(new MapEventOptionData { optionId = "Leave", optionLabel = "离开" });
        overlay.ConfigureInventory(hud); overlay.Show(null, data, null);
        hud.Refresh(new RunState(), null);
        object before = typeof(MapEventOverlay).GetField("selectedOption", Private).GetValue(overlay);
        Assert.IsNotNull(before);
        Button entry = (Button)typeof(MapEventOverlay).GetField("inventoryEntry", Private).GetValue(overlay);
        Assert.AreSame(Resources.Load<Sprite>("InventoryUI/EntryButton"), entry.GetComponent<Image>().sprite);
        entry.onClick.Invoke();
        Assert.IsTrue(hud.IsInventoryVisible); Assert.IsTrue(overlay.IsVisible);
        Assert.Greater(Field<Canvas>("inventoryCanvas").sortingOrder, overlay.GetComponentsInChildren<Canvas>().Single(c => c.name == "EventOverlayCanvas").sortingOrder);
        hud.CloseInventoryPanel();
        Assert.IsTrue(overlay.IsVisible); Assert.IsFalse(hud.IsInventoryVisible);
        Assert.AreSame(before, typeof(MapEventOverlay).GetField("selectedOption", Private).GetValue(overlay));
        Assert.AreSame(data, typeof(MapEventOverlay).GetField("currentEvent", Private).GetValue(overlay));
        entry.onClick.Invoke(); overlay.Hide();
        Assert.IsFalse(hud.IsInventoryVisible);
    }

    private T Field<T>(string name) => (T)typeof(MapHUD).GetField(name, Private).GetValue(hud);
}
