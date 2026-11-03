using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public sealed class MapEventPanelLayoutTests
{
    private GameObject host;
    private MapEventOverlay overlay;
    private MapEvent_SO data;
    private readonly List<Object> owned = new List<Object>();
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

    [SetUp]
    public void SetUp()
    {
        host = new GameObject("EventPanelTest");
        overlay = host.AddComponent<MapEventOverlay>();
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath("c6e7143cd2df45b45a784ef8c61a210f"));
        Assert.IsNotNull(font, "Use the real map Chinese font.");
        overlay.ConfigureFonts(font, font);
        data = Event("熄灭的营火");
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(host);
        foreach (Object item in owned) if (item != null) Object.DestroyImmediate(item);
        owned.Clear();
    }

    [TestCase(2340, 1080, 0, 0)]
    [TestCase(1920, 1080, 0, 0)]
    [TestCase(1600, 1200, 0, 0)]
    [TestCase(2400, 1080, 140, 50)]
    public void ReadingChoicesAndFooterRemainSeparateInsideSafeArea(int width, int height, int left, int right)
    {
        overlay.Show(null, data, null);
        Layout(width, height, new Rect(left, 24f, width - left - right, height - 48f));
        Rect panel = Bounds("Panel"), reading = Bounds("ContentScrollRect"), choices = Bounds("OptionScrollRect");
        Rect safe = Bounds("SafeArea");
        Contains(safe, panel);
        Contains(panel, reading);
        Contains(panel, choices);
        Contains(panel, Bounds("ConfirmButton"));
        Assert.IsFalse(reading.Overlaps(choices));
        Assert.IsFalse(choices.Overlaps(Bounds("Footer")));
        Assert.Greater(reading.width, panel.width * 0.85f);
        Assert.Greater(reading.height, panel.height * 0.44f, "The story must remain the dominant region.");
        Assert.AreEqual("确定", Find("ConfirmButton").GetComponentInChildren<TMP_Text>().text);
        Assert.IsNull(FindOptional("Illustration"));
        foreach (ScrollRect scroll in host.GetComponentsInChildren<ScrollRect>())
            Assert.IsFalse(scroll.verticalScrollbar.gameObject.activeSelf, "Short content must not display a full-height scrollbar.");
    }

    [TestCase("Title")]
    [TestCase("ContentText")]
    [TestCase("ConsequenceTitle")]
    [TestCase("ConsequenceText")]
    [TestCase("OptionSectionTitle")]
    [TestCase("EventMeta")]
    public void ProductionChineseFontActuallyBuildsVisibleText(string name)
    {
        overlay.Show(null, data, null);
        Layout();
        TMP_Text text = Find(name).GetComponent<TMP_Text>();
        text.ForceMeshUpdate(true, true);
        Assert.IsTrue(text.textInfo.characterInfo.Take(text.textInfo.characterCount).Any(c => c.isVisible), name);
        Assert.IsFalse(text.isTextTruncated, name);
        Assert.IsFalse(text.enableAutoSizing, "Keep readable body sizes.");
    }

    [TestCase(2340, 1080)]
    [TestCase(2048, 943)]
    [TestCase(1600, 1200)]
    public void ExistingMapSceneOverlayKeepsReadableContrastAndFullRemadeLayout(int width, int height)
    {
        MapEventVisualTools.CopyProductionSettings(overlay);
        MapEvent_SO tree = AssetDatabase.LoadAssetAtPath<MapEvent_SO>("Assets/Data/Events/002 发光的树洞/发光的树洞.asset");
        Assert.IsNotNull(tree);
        overlay.Show(null, tree, null);
        Layout(width, height);
        Color background = Find("Panel").GetComponent<Image>().color;
        TMP_Text story = Find("ContentText").GetComponent<TMP_Text>();
        Assert.GreaterOrEqual(background.a, 0.99f, "The story must not wash out against the bright forest.");
        Assert.Greater(background.g, background.r);
        Assert.Greater((Luminance(story.color) + 0.05f) / (Luminance(background) + 0.05f), 7f,
            "Catch serialized white-panel overrides, which new-component previews cannot detect.");
        Assert.GreaterOrEqual(story.fontSize, 32);
        Assert.GreaterOrEqual(Find("Title").GetComponent<TMP_Text>().fontSize, 52);
        Assert.AreEqual(tree.eventContent, story.text);
        Assert.AreEqual(3, host.GetComponentsInChildren<Button>().Count(b => b.name.StartsWith("Option_")));
        Rect panel = Bounds("Panel");
        Contains(Bounds("SafeArea"), panel);
        Contains(panel, Bounds("ContentScrollRect"));
        Contains(panel, Bounds("OptionScrollRect"));
        Contains(panel, Bounds("ConfirmButton"));
        Assert.IsFalse(Bounds("ContentScrollRect").Overlaps(Bounds("OptionScrollRect")));
        Assert.IsFalse(Bounds("OptionScrollRect").Overlaps(Bounds("Footer")));
        Assert.Greater(Bounds("ContentScrollRect").height, panel.height * 0.38f);
        story.ForceMeshUpdate(true, true);
        Assert.IsTrue(story.textInfo.characterInfo.Take(story.textInfo.characterCount).Any(c => c.isVisible));
        Assert.IsFalse(story.isTextTruncated);
    }

    private static float Luminance(Color color)
    {
        Color linear = color.linear;
        return linear.r * 0.2126f + linear.g * 0.7152f + linear.b * 0.0722f;
    }

    [Test]
    public void LongNarrativeAndManyWrappedChoicesRetainAllTextAndScrollIndependently()
    {
        data.eventContent = string.Join("\n\n", Enumerable.Repeat("这里像是别人刚离开不久的临时营地，旁边还有一些不知是遗落的东西还是遗弃的东西。", 20));
        data.options.Clear();
        for (int i = 0; i < 9; i++) data.options.Add(new MapEventOptionData { optionLabel = "需要完整保留的较长选项名称 " + i,
            consequenceDescription = string.Join("；", Enumerable.Repeat("恢复行动点，恢复队伍生命并继续后续事件", 8)) });
        overlay.Show(null, data, null);
        Layout();
        ScrollRect story = Find("ContentScrollRect").GetComponent<ScrollRect>();
        ScrollRect options = Find("OptionScrollRect").GetComponent<ScrollRect>();
        Assert.AreEqual(data.eventContent, Find("ContentText").GetComponent<TMP_Text>().text);
        Assert.Greater(story.content.rect.height, story.viewport.rect.height * 2f);
        Assert.Greater(options.content.rect.height, options.viewport.rect.height * 3f);
        for (int i = 0; i < 9; i++)
        {
            Transform row = Find("Option_" + (i + 1).ToString("00"));
            TMP_Text detail = row.Find("Detail").GetComponent<TMP_Text>();
            detail.ForceMeshUpdate();
            Assert.AreEqual(data.options[i].consequenceDescription, detail.text);
            Assert.GreaterOrEqual(((RectTransform)row).rect.height, detail.preferredHeight + 16f);
        }
        story.verticalNormalizedPosition = 0f;
        options.verticalNormalizedPosition = 0f;
        overlay.Show(null, Event("下一事件"), null);
        Layout();
        Assert.AreEqual(0f, story.content.anchoredPosition.y, 0.1f);
        Assert.AreEqual(0f, options.content.anchoredPosition.y, 0.1f);
        Assert.AreEqual(2, Find("OptionScrollRect").GetComponentsInChildren<Button>().Length);
    }

    [Test]
    public void UnavailableChoiceKeepsItsReasonAndSelectsFirstAvailableChoice()
    {
        data.options[0].requiredMoney = 25;
        data.options[0].requiredMoneyPercent = 50;
        overlay.Show(null, data, null, (tile, e, option) => option == data.options[0]
            ? new MapEventOverlay.EventOptionPreviewData { isSelectable = false, additionalDetail = "持有金币：0", unavailableReason = "金钱不足。" } : null);
        Layout();
        Button first = Find("Option_01").GetComponent<Button>();
        Assert.IsFalse(first.interactable);
        string detail = first.transform.Find("Detail").GetComponent<TMP_Text>().text;
        StringAssert.Contains("25 金钱", detail);
        StringAssert.Contains("50%", detail);
        StringAssert.Contains("持有金币：0", detail);
        StringAssert.Contains("金钱不足。", detail);
        Assert.AreEqual(data.options[1].consequenceDescription, Find("ConsequenceText").GetComponent<TMP_Text>().text);
        first.onClick.Invoke();
        Assert.AreEqual(data.options[1].consequenceDescription, Find("ConsequenceText").GetComponent<TMP_Text>().text);
    }

    [Test]
    public void NoAvailableChoiceDisablesConfirmationAndRetainsRequirements()
    {
        overlay.Show(null, data, null, (tile, e, option) => new MapEventOverlay.EventOptionPreviewData { isSelectable = false, unavailableReason = "条件不满足。" });
        Layout();
        Assert.IsFalse(Find("ConfirmButton").GetComponent<Button>().interactable);
        StringAssert.Contains("暂无可选项", Find("ConsequenceText").GetComponent<TMP_Text>().text);
    }

    [Test]
    public void NativeConfirmShowsExpandedResultThenFollowUpAndOnlyExecutesEachChoiceOnce()
    {
        MapEvent_SO child = Event("后续事件");
        data.options[0].nextEvent = child;
        int executions = 0, hidden = 0;
        overlay.Hidden += () => hidden++;
        overlay.Show(new BoardTileData(), data, (BoardTileData tile, MapEvent_SO e, MapEventOptionData option, out string message, out string result, out MapEvent_SO next) =>
        { executions++; message = "执行成功"; result = "完整的选择结果。"; next = option.nextEvent; return true; });
        Layout();
        float before = Bounds("ContentScrollRect").height;
        Button confirm = Find("ConfirmButton").GetComponent<Button>();
        confirm.onClick.Invoke();
        Layout();
        Assert.AreEqual(1, executions);
        Assert.AreEqual("完整的选择结果。", Find("ContentText").GetComponent<TMP_Text>().text);
        Assert.Greater(Bounds("ContentScrollRect").height, before + 150f);
        Assert.IsFalse(Find("OptionScrollRect").gameObject.activeSelf);
        Assert.AreEqual("继续", confirm.GetComponentInChildren<TMP_Text>().text);
        confirm.onClick.Invoke();
        Layout();
        Assert.AreEqual(1, executions);
        Assert.AreEqual("后续事件", Find("Title").GetComponent<TMP_Text>().text);
        Assert.IsTrue(Find("OptionScrollRect").gameObject.activeSelf);
        confirm.onClick.Invoke();
        confirm.onClick.Invoke();
        Assert.AreEqual(2, executions);
        Assert.IsFalse(overlay.IsVisible);
        overlay.Hide();
        Assert.AreEqual(1, hidden);
    }

    [Test]
    public void FailedSelectionKeepsChoicesAndFullErrorAboveFooter()
    {
        string error = string.Join("，", Enumerable.Repeat("当前选项未能执行，请检查条件", 5));
        overlay.Show(new BoardTileData(), data, (BoardTileData tile, MapEvent_SO e, MapEventOptionData option, out string message, out string result, out MapEvent_SO next) =>
        { message = error; result = null; next = null; return false; });
        Find("ConfirmButton").GetComponent<Button>().onClick.Invoke();
        Layout();
        Assert.AreEqual(error, Find("Status").GetComponent<TMP_Text>().text);
        Contains(Bounds("Footer"), Bounds("Status"));
        Assert.IsFalse(Bounds("Status").Overlaps(Bounds("ConsequenceScrollRect")));
        Assert.IsTrue(Find("OptionScrollRect").gameObject.activeSelf);
    }

    private MapEvent_SO Event(string title)
    {
        MapEvent_SO e = ScriptableObject.CreateInstance<MapEvent_SO>();
        owned.Add(e);
        e.eventName = title;
        e.eventContent = "一堆刚刚才熄灭的营火在路边堆着，还冒着丝丝细烟。\n这里像是别人刚离开不久的临时营地，旁边还有一些不知是遗落的东西还是遗弃的东西。";
        e.options.Add(new MapEventOptionData { optionLabel = "稍作休息", consequenceDescription = "恢复 3 行动点，恢复 10% 队伍生命" });
        e.options.Add(new MapEventOptionData { optionLabel = "拿点物资", consequenceDescription = "获得 1 个随机遗物" });
        return e;
    }

    private void Layout(int width = 2340, int height = 1080, Rect? safe = null)
    {
        Canvas canvas = host.GetComponentInChildren<Canvas>();
        canvas.GetComponent<CanvasScaler>().enabled = false;
        canvas.renderMode = RenderMode.WorldSpace;
        RectTransform rect = (RectTransform)canvas.transform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(1080f * width / height, 1080f);
        overlay.ApplyViewport(new Vector2(width, height), safe ?? new Rect(0f, 0f, width, height));
        typeof(MapEventOverlay).GetMethod("RefreshLayout", Private).Invoke(overlay, null);
        Canvas.ForceUpdateCanvases();
    }

    private Transform Find(string name) { return FindOptional(name) ?? throw new System.Exception("Missing " + name); }
    private Transform FindOptional(string name) { return host.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == name); }
    private Rect Bounds(string name)
    {
        Vector3[] corners = new Vector3[4];
        ((RectTransform)Find(name)).GetWorldCorners(corners);
        return Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
    }
    private static void Contains(Rect outer, Rect inner)
    {
        Assert.GreaterOrEqual(inner.xMin, outer.xMin - 0.2f);
        Assert.GreaterOrEqual(inner.yMin, outer.yMin - 0.2f);
        Assert.LessOrEqual(inner.xMax, outer.xMax + 0.2f);
        Assert.LessOrEqual(inner.yMax, outer.yMax + 0.2f);
    }
}
