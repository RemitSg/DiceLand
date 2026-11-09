using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class MapEventPanelLayoutTests
{
    private GameObject host;
    private MapEventOverlay overlay;
    private MapEvent_SO data;
    private readonly List<Object> owned = new List<Object>();
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

    [SetUp] public void SetUp()
    {
        host = new GameObject("Story event regression"); overlay = host.AddComponent<MapEventOverlay>();
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath("c6e7143cd2df45b45a784ef8c61a210f"));
        overlay.ConfigureFonts(font, font); data = Event("熄灭的营火");
    }
    [TearDown] public void TearDown() { Object.DestroyImmediate(host); foreach (Object item in owned) if (item != null) Object.DestroyImmediate(item); owned.Clear(); }

    [TestCase(2340, 1080, 0, 0)]
    [TestCase(1920, 1080, 0, 0)]
    [TestCase(1280, 720, 0, 0)]
    [TestCase(1600, 1200, 0, 0)]
    [TestCase(2400, 1080, 140, 50)]
    public void ArtworkBleedsToEdgesAndAllReadingAndChoiceControlsRespectSafeArea(int width, int height, int left, int right)
    {
        overlay.Show(null, data, null); Layout(width, height, new Rect(left, 24, width - left - right, height - 48));
        Rect safe = Bounds("SafeArea"), panel = Bounds("Panel"); Contains(safe, panel);
        foreach (string name in new[] { "Footer", "ContentScrollRect", "OptionScrollRect", "ConfirmButton", "ConsequencePreview" }) Contains(panel, Bounds(name));
        Assert.IsFalse(Bounds("OptionScrollRect").Overlaps(Bounds("Footer")));
        Assert.IsFalse(Bounds("ConsequencePreview").Overlaps(Bounds("Footer")));
        Assert.Greater(Bounds("Footer").width, panel.width * .99f);
        Assert.GreaterOrEqual(Find("ContentText").GetComponent<TMP_Text>().fontSize, 36);
        Assert.AreEqual(TextOverflowModes.Page, Find("ContentText").GetComponent<TMP_Text>().overflowMode);
        Assert.IsNotNull(Find("Illustration").GetComponent<Image>().sprite);
        Assert.AreEqual("确认选择", Find("ConfirmButton").GetComponentInChildren<TMP_Text>().text);
        Assert.Greater(Find("Footer").GetComponent<Image>().color.a, .93f);
    }

    [Test]
    public void WrappedLabelsUseIdenticalButtonWidthsHeightsAndSpacing()
    {
        data.options[1].optionLabel = "仔细查看这片森林中留下的神秘古老符文";
        data.options.Add(new MapEventOptionData { optionLabel = "离开" });
        overlay.Show(null, data, null); Layout(1600, 1200);
        Rect[] rows = Enumerable.Range(1, 3).Select(i => Bounds("Option_" + i.ToString("00"))).ToArray();
        Assert.AreEqual(rows[0].width, rows[1].width, .01f); Assert.AreEqual(rows[0].height, rows[1].height, .01f);
        Assert.AreEqual(rows[0].height, rows[2].height, .01f);
        Assert.AreEqual(rows[0].yMin - rows[1].yMax, rows[1].yMin - rows[2].yMax, .01f);
        foreach (Transform row in host.GetComponentsInChildren<Transform>(true).Where(t => t.name.StartsWith("Option_")))
            Assert.IsFalse(row.Find("Detail").gameObject.activeSelf, "Consequences belong in the hover preview, keeping rows quiet.");
    }

    [Test]
    public void FullNarrativePaginatesWithoutShrinkingAndManyChoicesScroll()
    {
        data.eventContent = string.Join("\n\n", Enumerable.Repeat("这里像是别人刚离开不久的临时营地，旁边还有一些不知是遗落的东西还是遗弃的东西。", 30));
        data.options.Clear();
        for (int i = 0; i < 9; i++) data.options.Add(new MapEventOptionData { optionLabel = "保留完整选项 " + i, consequenceDescription = "恢复行动点与生命值。" });
        overlay.Show(null, data, null); Layout();
        TMP_Text story = Find("ContentText").GetComponent<TMP_Text>();
        Assert.AreEqual(data.eventContent, story.text); Assert.Greater(story.textInfo.pageCount, 5); Assert.IsFalse(story.enableAutoSizing);
        ScrollRect choices = Find("OptionScrollRect").GetComponent<ScrollRect>();
        Assert.Greater(choices.content.rect.height, choices.viewport.rect.height * 2); Assert.IsTrue(choices.verticalScrollbar.gameObject.activeSelf);
        Assert.IsFalse(Find("ContentScrollRect").GetComponent<ScrollRect>().vertical, "Story pages advance deliberately rather than scrolling while typing.");
    }

    [Test]
    public void HoverChangesPreviewAndSelectionButNeverExecutesAReward()
    {
        int executions = 0;
        overlay.Show(new BoardTileData(), data, (BoardTileData t, MapEvent_SO e, MapEventOptionData o, out string m, out string r, out MapEvent_SO n) =>
        { executions++; m = "获得 10 金钱"; r = "你收好了沿途拾取的物资。"; n = null; return true; });
        Layout();
        Find("Option_02").GetComponent<MapEventChoiceFeedback>().OnPointerEnter(null);
        Assert.AreEqual(0, executions); Assert.AreSame(data.options[1], Field("selectedOption"));
        StringAssert.Contains(data.options[1].consequenceDescription, Find("ConsequenceText").GetComponent<TMP_Text>().text);
        Find("ConfirmButton").GetComponent<Button>().onClick.Invoke();
        Assert.AreEqual(1, executions);
        StringAssert.Contains("你收好了", Find("ContentText").GetComponent<TMP_Text>().text);
        StringAssert.Contains("获得 10 金钱", Find("ContentText").GetComponent<TMP_Text>().text);
        Assert.IsFalse(Find("OptionScrollRect").gameObject.activeSelf);
    }

    [Test]
    public void UnavailableChoiceCanBeInspectedWithoutReplacingTheValidSelection()
    {
        data.options[0].requiredMoney = 25;
        overlay.Show(null, data, null, (t, e, o) => o == data.options[0]
            ? new MapEventOverlay.EventOptionPreviewData { isSelectable = false, unavailableReason = "金钱不足。", additionalDetail = "持有金币：0" } : null);
        Layout();
        Assert.IsFalse(Find("Option_01").GetComponent<Button>().interactable);
        Find("Option_01").GetComponent<MapEventChoiceFeedback>().OnPointerEnter(null);
        StringAssert.Contains("金钱不足", Find("ConsequenceText").GetComponent<TMP_Text>().text);
        StringAssert.Contains("25", Find("ConsequenceText").GetComponent<TMP_Text>().text);
        Assert.AreSame(data.options[1], Field("selectedOption"));
        Assert.IsFalse(Find("ConfirmButton").GetComponent<Button>().interactable);
        Find("Option_02").GetComponent<MapEventChoiceFeedback>().OnPointerEnter(null);
        Assert.IsTrue(Find("ConfirmButton").GetComponent<Button>().interactable);
    }

    [Test]
    public void ConcealedConsequencesRemainConcealedUntilTheActualResult()
    {
        data.hideConsequences = true;
        overlay.Show(null, data, null); Layout();
        Assert.IsFalse(Find("ConsequencePreview").gameObject.activeSelf);
        Assert.AreEqual(string.Empty, Find("ConsequenceText").GetComponent<TMP_Text>().text);
        typeof(MapEventOverlay).GetMethod("ShowResultStep", Private).Invoke(overlay, new object[] { data.options[0], "失去 5 金钱", "箱子里只有一张旧纸条。", null });
        StringAssert.Contains("失去 5 金钱", Find("ContentText").GetComponent<TMP_Text>().text);
    }

    [Test]
    public void NativeFollowUpUsesNextEventAndFinalHiddenFiresExactlyOnce()
    {
        MapEvent_SO child = Event("后续事件"); data.options[0].nextEvent = child;
        int executions = 0, hidden = 0; overlay.Hidden += () => hidden++;
        overlay.Show(new BoardTileData(), data, (BoardTileData t, MapEvent_SO e, MapEventOptionData o, out string m, out string r, out MapEvent_SO n) =>
        { executions++; m = "执行成功"; r = "完整的选择结果。"; n = o.nextEvent; return true; });
        Layout(); Button confirm = Find("ConfirmButton").GetComponent<Button>();
        confirm.onClick.Invoke(); Assert.AreEqual(1, executions); Assert.IsTrue(overlay.IsVisible);
        confirm.onClick.Invoke(); Assert.AreEqual("后续事件", Find("Title").GetComponent<TMP_Text>().text);
        Assert.AreEqual(1, executions);
        confirm.onClick.Invoke(); confirm.onClick.Invoke();
        Assert.AreEqual(2, executions); Assert.IsFalse(overlay.IsVisible); overlay.Hide(); Assert.AreEqual(1, hidden);
    }

    [Test]
    public void FailedSelectionKeepsChoicesAndTheCompleteReasonInScrollablePreview()
    {
        string error = string.Join("，", Enumerable.Repeat("当前条件不满足，请查看背包后重试", 8));
        overlay.Show(new BoardTileData(), data, (BoardTileData t, MapEvent_SO e, MapEventOptionData o, out string m, out string r, out MapEvent_SO n) =>
        { m = error; r = null; n = null; return false; });
        Find("ConfirmButton").GetComponent<Button>().onClick.Invoke(); Layout();
        StringAssert.Contains(error, Find("ConsequenceText").GetComponent<TMP_Text>().text);
        Assert.IsTrue(Find("OptionScrollRect").gameObject.activeSelf);
        Assert.IsTrue(Find("ConsequenceScrollRect").GetComponent<ScrollRect>().verticalScrollbar.gameObject.activeSelf);
    }

    [Test]
    public void AuthoredIllustrationOverridesFamilyAndFamilyHasStableIdentityForBranches()
    {
        data.eventId = "023"; Sprite medical = MapEventSceneArt.Resolve(data); Assert.IsNotNull(medical);
        data.eventId = "023_A_02"; Assert.AreSame(medical, MapEventSceneArt.Resolve(data));
        data.eventIllustration = MapForestArt.Pawn; Assert.AreSame(data.eventIllustration, MapEventSceneArt.Resolve(data));
    }

    [TestCase("014-1", "014")]
    [TestCase("025-1", "025")]
    public void LegacyHyphenBranchesReuseTheirOwnEventFamily(string branch, string parent)
    {
        data.eventId = parent; Sprite expected = MapEventSceneArt.Resolve(data);
        data.eventId = branch; Assert.AreSame(expected, MapEventSceneArt.Resolve(data));
    }

    [Test]
    public void EveryAuthoredEventHasItsFamilyPaintingOrAnExplicitOverride()
    {
        foreach (string guid in AssetDatabase.FindAssets("t:MapEvent_SO", new[] { "Assets/Data/Events" }))
        {
            MapEvent_SO item = AssetDatabase.LoadAssetAtPath<MapEvent_SO>(AssetDatabase.GUIDToAssetPath(guid));
            Sprite art = MapEventSceneArt.Resolve(item); Assert.IsNotNull(art, item.eventId);
            if (item.eventIllustration == null) StringAssert.StartsWith("Assets/Resources/EventUI/Scenes/", AssetDatabase.GetAssetPath(art), item.eventId);
        }
    }

    [Test]
    public void ResizePreservesTheRevealIndexAndALongFrameCannotSkipTheUnreadPage()
    {
        data.eventContent = string.Join("\n\n", Enumerable.Repeat("你拨开垂落的藤蔓，发现一只刻着古老符文的木箱，箱盖下透出温暖的微光。", 30));
        overlay.ConfigurePreviewPlayback(true); overlay.Show(null, data, null); Layout();
        overlay.AdvancePresentation(.5f);
        TMP_Text text = Find("ContentText").GetComponent<TMP_Text>(); int visible = text.maxVisibleCharacters;
        Assert.IsTrue(overlay.IsTyping); Layout(2340, 1080); Assert.AreEqual(visible, text.maxVisibleCharacters);
        overlay.AdvancePresentation(10); Assert.LessOrEqual(text.maxVisibleCharacters, visible + 6); Assert.IsTrue(overlay.IsTyping);
    }

    [Test]
    public void TabletFitsTheShortCampfireNarrativeAndItsNormalPreviewWithoutAnOrphanPageOrScrollbar()
    {
        MapEvent_SO campfire = AssetDatabase.LoadAssetAtPath<MapEvent_SO>("Assets/Data/Events/021 熄灭的营火/021 熄灭的营火.asset");
        Assert.IsNotNull(campfire); overlay.Show(null, campfire, null); Layout(1600, 1200);
        TMP_Text story = Find("ContentText").GetComponent<TMP_Text>();
        Assert.AreEqual(1, story.textInfo.pageCount, "A short event should not leave a few orphan characters on the next page.");
        ScrollRect preview = Find("ConsequenceScrollRect").GetComponent<ScrollRect>();
        Assert.IsFalse(preview.verticalScrollbar.gameObject.activeSelf, "Normal consequences must fit; reserve scrolling for genuinely long previews.");
        Assert.LessOrEqual(preview.content.rect.height, preview.viewport.rect.height + .5f);
    }

    private MapEvent_SO Event(string name)
    {
        MapEvent_SO e = ScriptableObject.CreateInstance<MapEvent_SO>(); owned.Add(e);
        e.eventName = name; e.eventContent = "一堆刚刚才熄灭的营火在路边堆着，还冒着丝丝细烟。\n旁边还有一些不知是遗落还是遗弃的东西。";
        e.options.Add(new MapEventOptionData { optionLabel = "稍作休息", consequenceDescription = "恢复 3 行动点，恢复 10% 队伍生命" });
        e.options.Add(new MapEventOptionData { optionLabel = "拿点物资", consequenceDescription = "获得 1 个随机遗物" });
        return e;
    }
    private void Layout(int w = 1920, int h = 1080, Rect? safe = null)
    {
        Canvas canvas = host.GetComponentInChildren<Canvas>(true); canvas.GetComponent<CanvasScaler>().enabled = false;
        canvas.renderMode = RenderMode.WorldSpace; ((RectTransform)canvas.transform).sizeDelta = new Vector2(w * 1080f / h, 1080);
        overlay.ApplyViewport(new Vector2(w, h), safe ?? new Rect(0, 0, w, h));
        for (int i = 0; i < 2; i++) { Canvas.ForceUpdateCanvases(); typeof(MapEventOverlay).GetMethod("RefreshLayout", Private).Invoke(overlay, null); }
    }
    private Transform Find(string name) => host.GetComponentsInChildren<Transform>(true).First(t => t.name == name);
    private object Field(string name) => typeof(MapEventOverlay).GetField(name, Private).GetValue(overlay);
    private Rect Bounds(string name)
    {
        Vector3[] corners = new Vector3[4]; ((RectTransform)Find(name)).GetWorldCorners(corners);
        return Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
    }
    private static void Contains(Rect outer, Rect inner)
    {
        Assert.GreaterOrEqual(inner.xMin, outer.xMin - 1); Assert.GreaterOrEqual(inner.yMin, outer.yMin - 1);
        Assert.LessOrEqual(inner.xMax, outer.xMax + 1); Assert.LessOrEqual(inner.yMax, outer.yMax + 1);
    }
}
