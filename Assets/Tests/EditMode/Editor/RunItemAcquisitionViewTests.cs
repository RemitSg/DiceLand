using System;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public sealed class RunItemAcquisitionViewTests
{
    private GameObject host;
    private RunItemAcquisitionOverlay overlay;
    private RunState state;
    private Relic_SO relic;
    private CraftingBlueprint_SO blueprint;
    private LootItem_SO ore;
    private TMP_FontAsset font;

    [SetUp]
    public void SetUp()
    {
        host = new GameObject("Acquisition view test"); overlay = host.AddComponent<RunItemAcquisitionOverlay>();
        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath("c6e7143cd2df45b45a784ef8c61a210f"));
        relic = ScriptableObject.CreateInstance<Relic_SO>(); relic.relicName = "新获得的遗物";
        relic.description = string.Join("\n", Enumerable.Repeat("完整遗物效果与触发条件。", 18)); relic.additionalDescription = "保留补充规则与例外。";
        blueprint = ScriptableObject.CreateInstance<CraftingBlueprint_SO>(); blueprint.blueprintName = "新获得的图纸";
        ore = ScriptableObject.CreateInstance<LootItem_SO>(); ore.lootName = "铁矿";
        blueprint.materialRequirements.Add(new CraftingMaterialRequirement { lootItem = ore, amount = 3 });
        state = new RunState(); state.AddRelic(relic, 2); state.AddBlueprint(blueprint);
        overlay.Configure(state, font, font); overlay.ProcessPending();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(host); Object.DestroyImmediate(relic); Object.DestroyImmediate(blueprint); Object.DestroyImmediate(ore);
    }

    [Test]
    public void ReceiptViewReusesFullDescriptionsAndAcknowledgesEachItemWithoutRegranting()
    {
        Assert.IsTrue(overlay.IsVisible);
        Assert.AreEqual("获得遗物", Text("Heading").text);
        Assert.AreEqual("本次获得 ×2", Text("Quantity").text);
        Assert.AreEqual("下一件", CloseLabel());
        Assert.AreEqual(relic.description, Text("Description").text);
        Assert.AreEqual(relic.additionalDescription, Text("RelicAdditionalDescription").text);
        Assert.IsFalse(overlay.DetailView.ModalRoot.GetComponent<Button>().interactable);
        overlay.DetailView.Hide();
        Assert.AreEqual("获得图纸", Text("Heading").text);
        Assert.That(Text("Description").text, Does.Contain("0 / 3").And.Contain("还缺 3"));
        Assert.AreEqual("继续", CloseLabel());
        overlay.DetailView.Hide();
        Assert.IsFalse(overlay.IsVisible); Assert.AreEqual(0, state.ItemAcquisitions.PendingCount);
        Assert.AreEqual(2, state.GetRelicAmount(relic)); Assert.AreEqual(1, state.GetBlueprintAmount(blueprint));
    }

    [Test]
    public void RebindingSuspendsWithoutAcknowledgingUnseenReceipts()
    {
        RunState snapshot = state.Clone();
        overlay.Configure(new RunState(), font, font); overlay.ProcessPending();
        Assert.IsFalse(overlay.IsVisible); Assert.AreEqual(2, state.ItemAcquisitions.PendingCount);
        overlay.Configure(snapshot, font, font); overlay.ProcessPending();
        Assert.AreEqual("获得遗物", Text("Heading").text);
        overlay.DetailView.Hide(); overlay.DetailView.Hide();
        Assert.AreEqual(0, state.ItemAcquisitions.PendingCount);
    }

    [Test]
    public void VisibleBlueprintMaterialsRefreshAsBalancesChangeWithoutClosingThePage()
    {
        overlay.DetailView.Hide();
        Assert.That(Text("Description").text, Does.Contain("0 / 3"));
        state.AddLoot(ore, 3);
        overlay.DetailView.ConfigureInventoryContext(state);
        Assert.IsTrue(overlay.IsVisible);
        Assert.That(Text("Description").text, Does.Contain("3 / 3").And.Contain("材料齐备"));
        Assert.AreEqual(3, state.GetLootAmount(ore));
    }

    [TestCase(2340,1080,0,0)]
    [TestCase(1920,1080,0,0)]
    [TestCase(1600,1200,0,0)]
    [TestCase(2400,1080,140,50)]
    [TestCase(1080,1920,50,30)]
    public void AcquisitionMetadataDescriptionAndControlsFitEverySafeArea(int width,int height,int left,int right)
    {
        Canvas canvas = overlay.PresentationCanvas; canvas.GetComponent<CanvasScaler>().enabled = false; canvas.renderMode = RenderMode.WorldSpace;
        RectTransform root = (RectTransform)canvas.transform;
        root.anchorMin = root.anchorMax = root.pivot = Vector2.one * .5f; root.sizeDelta = new Vector2(1080f * width / height, 1080);
        overlay.DetailView.ApplyViewport(new Vector2(width,height), new Rect(left,24,width-left-right,height-48)); Canvas.ForceUpdateCanvases();
        Rect safe = Bounds(Find("ItemDetailSafeArea")), panel = Bounds(overlay.DetailView.transform);
        Contains(safe, panel); Contains(safe, Bounds(Find("TitlePlaque")));
        Contains(panel, Bounds(Find("QuantityBadge"))); Contains(panel, Bounds(Find("CloseButton")));
        Assert.IsFalse(Bounds(Find("DescriptionWell")).Overlaps(Bounds(Find("CloseButton"))));
        ScrollRect scroll = Find("DescriptionScroll").GetComponent<ScrollRect>();
        Assert.Greater(scroll.content.rect.height, scroll.viewport.rect.height);
    }

    private Transform Find(string name) => overlay.DetailView.ModalRoot.GetComponentsInChildren<Transform>(true).First(t => t.name == name);
    private TMP_Text Text(string name) => Find(name).GetComponent<TMP_Text>();
    private string CloseLabel() => Find("CloseButton").GetComponentInChildren<TMP_Text>().text;
    private static Rect Bounds(Transform t) { var c=new Vector3[4]; ((RectTransform)t).GetWorldCorners(c); return Rect.MinMaxRect(c[0].x,c[0].y,c[2].x,c[2].y); }
    private static void Contains(Rect outer,Rect inner) { Assert.GreaterOrEqual(inner.xMin,outer.xMin-.2f); Assert.GreaterOrEqual(inner.yMin,outer.yMin-.2f); Assert.LessOrEqual(inner.xMax,outer.xMax+.2f); Assert.LessOrEqual(inner.yMax,outer.yMax+.2f); }
}
