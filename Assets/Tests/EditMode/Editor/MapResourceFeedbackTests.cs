using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public sealed class MapResourceFeedbackTests
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private GameObject host;
    private MapResourceFeedback feedback;
    private TMP_Text wallet;
    private RunState state;
    private TMP_FontAsset font;

    [SetUp]
    public void SetUp()
    {
        host = new GameObject("MapFeedbackTest");
        GameObject canvas = new GameObject("HUD", typeof(RectTransform), typeof(Canvas));
        canvas.transform.SetParent(host.transform, false);
        canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath("c6e7143cd2df45b45a784ef8c61a210f"));
        wallet = Label(canvas.transform, "Money", "100");
        wallet.color = new Color32(240, 204, 107, 255);
        feedback = host.AddComponent<MapResourceFeedback>();
        state = new RunState { money = 100 };
        feedback.Bind(state, wallet);
    }

    [TearDown]
    public void TearDown() => Object.DestroyImmediate(host);

    [Test]
    public void InitialBindingAndRepeatedRefreshDoNotAwardCoins()
    {
        feedback.Bind(state, wallet);
        feedback.ObserveMoney(100);
        feedback.Advance(10f);
        Assert.AreEqual("100", wallet.text);
        Assert.IsFalse(feedback.IsMoneyAnimating);
        Assert.IsNull(Get("feedbackCanvas"));
    }

    [TestCase(1)]
    [TestCase(3)]
    [TestCase(107)]
    [TestCase(1000000)]
    public void GainIsCreditedOnlyOnLandingWithExactWeightedTotal(int amount)
    {
        state.money += amount;
        feedback.ObserveMoney(state.money);
        Assert.IsTrue(host.GetComponentsInChildren<MapFeedbackGraphic>().Where(x => x.name == "CoinGlint").All(x => x.color.a == 0f),
            "Delayed coins must not leave default-white glints at the canvas centre.");
        feedback.Bind(state, wallet); // Ordinary HUD refresh must not snap an active animation.
        feedback.ObserveMoney(state.money);
        int count = (int)Get("liveCoins");
        Assert.That(count, Is.InRange(1, Math.Min(18, amount)));
        feedback.Advance(0.3f);
        Assert.AreEqual(100, feedback.DisplayedMoney, "No coin has reached the wallet yet.");
        feedback.Advance(0.43f);
        Assert.Greater(feedback.DisplayedMoney, 100);
        if (count > 1) Assert.Less(feedback.DisplayedMoney, state.money);
        feedback.Advance(1f);
        Assert.AreEqual(state.money, feedback.DisplayedMoney);
        Assert.IsFalse(feedback.IsMoneyAnimating);
        Assert.AreEqual(0, host.GetComponentsInChildren<Image>().Count(x => x.name.StartsWith("FlyingCoin_")));
    }

    [Test]
    public void SpendingTurnsRedAndCountsDownThenRestoresTheWalletStyle()
    {
        Color original = wallet.color;
        state.money = -25; // Existing positive-balance credit rules can produce debt.
        feedback.ObserveMoney(-25);
        Assert.Greater(wallet.color.r, original.r);
        feedback.Advance(0.24f);
        Assert.That(feedback.DisplayedMoney, Is.InRange(-24, 99));
        Assert.Greater(wallet.color.r, wallet.color.g);
        feedback.Advance(1f);
        feedback.Advance(1f);
        Assert.AreEqual("-25", wallet.text);
        Assert.AreEqual(original, wallet.color);
        Assert.AreEqual(Vector3.one, wallet.rectTransform.localScale);
        Assert.AreEqual(-25, state.money);
    }

    [Test]
    public void RapidMixedChangesStayBoundedAndFinishAtTheAuthoritativeValue()
    {
        for (int i = 0; i < 120; i++)
        {
            state.money = i % 2 == 0 ? 300 + i : -20 - i;
            feedback.ObserveMoney(state.money);
        }
        Assert.LessOrEqual(((IList)Get("moneyQueue")).Count, 4);
        for (int i = 0; i < 8; i++) feedback.Advance(2f);
        Assert.AreEqual(state.money, feedback.DisplayedMoney);
        Assert.IsFalse(feedback.IsMoneyAnimating);
        Assert.LessOrEqual(((IList)Get("coins")).Count, 18);
        Assert.LessOrEqual(((IList)Get("decorations")).Count, 40);
    }

    [Test]
    public void LargeSignedMoneyChangesDoNotOverflow()
    {
        state.money = int.MinValue;
        feedback.Bind(new RunState { money = int.MinValue }, wallet);
        feedback.ObserveMoney(int.MaxValue);
        feedback.Advance(2f);
        Assert.AreEqual(int.MaxValue, feedback.DisplayedMoney);
        feedback.ObserveMoney(int.MinValue);
        feedback.Advance(2f);
        Assert.AreEqual(int.MinValue, feedback.DisplayedMoney);
    }

    [Test]
    public void NewRunDropsStaleCoinsWithoutModifyingSavedValues()
    {
        state.money = 207;
        feedback.ObserveMoney(207);
        feedback.Advance(0.2f);
        RunState next = new RunState { money = 8 };
        feedback.Bind(next, wallet);
        feedback.Advance(2f);
        Assert.AreEqual("8", wallet.text);
        Assert.IsFalse(feedback.IsMoneyAnimating);
        Assert.IsEmpty(host.GetComponentsInChildren<Image>().Where(x => x.name.StartsWith("FlyingCoin_")));
        Assert.AreEqual(207, state.money);
        Assert.AreEqual(8, next.money);
    }

    [Test]
    public void HealthHasDamageTrailThenSmoothlyRetargetsHealingAndMemberReplacement()
    {
        Image fill = new GameObject("HP", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        fill.transform.SetParent(wallet.transform.parent, false);
        TMP_Text hp = Label(wallet.transform.parent, "HpText", string.Empty);
        RunPartyMemberState member = new RunPartyMemberState { maxHP = 20, currentHP = 20 };
        feedback.PresentHealth(member, fill, hp, 20, 20);
        Assert.AreEqual(1f, fill.rectTransform.anchorMax.x);
        member.currentHP = 6;
        feedback.PresentHealth(member, fill, hp, 6, 20);
        feedback.Advance(0.24f);
        float injured = fill.rectTransform.anchorMax.x;
        Assert.That(injured, Is.InRange(0.31f, 0.99f));
        Image trail = fill.transform.parent.Find("FeedbackTrail").GetComponent<Image>();
        Assert.Greater(trail.rectTransform.anchorMax.x, injured);
        member.currentHP = 18;
        feedback.PresentHealth(member, fill, hp, 18, 20);
        Assert.AreEqual(injured, fill.rectTransform.anchorMax.x);
        feedback.Advance(0.24f);
        Assert.Greater(fill.color.g, fill.color.r, "The healing pulse must read as green.");
        feedback.Advance(0.76f);
        Assert.AreEqual(0.9f, fill.rectTransform.anchorMax.x, 0.0001f);
        Assert.AreEqual("18/20", hp.text);
        feedback.PresentHealth(new RunPartyMemberState(), fill, hp, 2, 10);
        Assert.AreEqual(0.2f, fill.rectTransform.anchorMax.x, 0.0001f);
        Assert.AreEqual("2/10", hp.text);
        Assert.AreEqual(18, member.currentHP);
    }

    [Test]
    public void FeedbackCanvasCannotCapturePointerEvents()
    {
        feedback.ObserveMoney(120);
        Canvas canvas = (Canvas)Get("feedbackCanvas");
        Assert.Greater(canvas.sortingOrder, 9040);
        Assert.IsFalse(canvas.GetComponent<CanvasGroup>().blocksRaycasts);
        Assert.IsEmpty(canvas.GetComponentsInChildren<GraphicRaycaster>(true));
        Assert.IsTrue(canvas.GetComponentsInChildren<Graphic>(true).All(x => !x.raycastTarget));
    }

    [Test]
    public void MoneyOriginUsesVisibleChineseTextAndScopeRestoresAfterExceptions()
    {
        TMP_Text source = Label(wallet.transform.parent, "Reward", "获得 25 金币，并恢复行动点。");
        ResourceKeywordText.Attach(source);
        Canvas.ForceUpdateCanvases();
        Assert.IsTrue(MapFeedbackOrigin.TryGetMoneyPosition(source, out Vector2 point));
        Assert.That(point.x, Is.InRange(0f, 1f));
        Assert.That(point.y, Is.InRange(0f, 1f));
        Assert.IsNull(MapFeedbackOrigin.Current);
        using (MapFeedbackOrigin.FromText(source))
        {
            Assert.AreEqual(point, MapFeedbackOrigin.Current.Value);
            Assert.Throws<InvalidOperationException>(() =>
            {
                using (MapFeedbackOrigin.FromText(null)) throw new InvalidOperationException();
            });
            Assert.AreEqual(point, MapFeedbackOrigin.Current.Value);
        }
        Assert.IsNull(MapFeedbackOrigin.Current);
        source.gameObject.SetActive(false);
        Assert.IsFalse(MapFeedbackOrigin.TryGetMoneyPosition(source, out _));
    }

    [Test]
    public void CoinAndAllFiveShortSoundsAreImportedAndAvailableInPlayerResources()
    {
        Sprite sprite = Resources.Load<Sprite>("MapUI/FeedbackCoin");
        Assert.IsNotNull(sprite);
        Assert.LessOrEqual(sprite.texture.width, 256);
        foreach (string name in new[] { "CoinRelease", "CoinArrival", "CoinSpend", "Damage", "Heal" })
        {
            AudioClip clip = Resources.Load<AudioClip>("SFX/Map" + name);
            Assert.IsNotNull(clip, name);
            Assert.That(clip.length, Is.InRange(0.1f, 0.6f));
            Assert.AreEqual(1, clip.channels);
        }
    }

    [Test]
    public void ScrolledOffRewardWordsAreNotUsedAsAnOrigin()
    {
        RectTransform viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D)).GetComponent<RectTransform>();
        viewport.SetParent(wallet.transform.parent, false);
        viewport.sizeDelta = new Vector2(300f, 60f);
        TMP_Text source = Label(viewport, "ScrolledReward", "获得 25 金币。");
        source.rectTransform.anchoredPosition = new Vector2(0f, -400f);
        Canvas.ForceUpdateCanvases();
        Assert.IsFalse(MapFeedbackOrigin.TryGetMoneyPosition(source, out _));
    }

    [Test]
    public void ScreenFeedbackAndGlintsHaveCanvasRenderersAndVisibleMeshColours()
    {
        Image fill = new GameObject("HP", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        fill.transform.SetParent(wallet.transform.parent, false);
        RunPartyMemberState member = new RunPartyMemberState();
        feedback.PresentHealth(member, fill, null, 20, 20);
        feedback.PresentHealth(member, fill, null, 5, 20);
        feedback.ObserveMoney(125);
        feedback.Advance(0.24f);
        Canvas.ForceUpdateCanvases();
        foreach (MapFeedbackGraphic graphic in host.GetComponentsInChildren<MapFeedbackGraphic>())
        {
            Assert.IsNotNull(graphic.GetComponent<CanvasRenderer>(), graphic.name);
            graphic.SetAllDirty();
            graphic.Rebuild(CanvasUpdate.PreRender);
            if (graphic.color.a <= 0f) continue;
            Mesh mesh = graphic.canvasRenderer.GetMesh();
            Assert.Greater(mesh.vertexCount, 0, graphic.name);
            Assert.IsTrue(mesh.colors32.Any(x => x.a > 0), graphic.name);
        }
    }

    [Test]
    public void EventSelectionOnlyPaysOnConfirmationAndCapturesItsRewardWords()
    {
        MapEventOverlay overlay = host.AddComponent<MapEventOverlay>();
        overlay.ConfigureFonts(font, font);
        MapEvent_SO data = ScriptableObject.CreateInstance<MapEvent_SO>();
        try
        {
            data.eventName = "动效检查";
            data.eventContent = "这份奖励只在确认后发放。";
            data.options.Add(new MapEventOptionData { optionLabel = "收下金币", consequenceDescription = "获得 37 金币。" });
            Vector2? captured = null;
            int calls = 0;
            overlay.Show(new BoardTileData { tileType = MapTileType.Event }, data,
                (BoardTileData tile, MapEvent_SO evt, MapEventOptionData option, out string message, out string result, out MapEvent_SO next) =>
                {
                    calls++;
                    captured = MapFeedbackOrigin.Current;
                    state.money += 37;
                    feedback.ObserveMoney(state.money);
                    message = result = "获得 37 金币。";
                    next = null;
                    return true;
                });
            Canvas.ForceUpdateCanvases();
            Assert.AreEqual(100, state.money);
            Assert.IsFalse(feedback.IsMoneyAnimating);
            Button confirm = (Button)typeof(MapEventOverlay).GetField("confirmButton", Private).GetValue(overlay);
            confirm.onClick.Invoke();
            Assert.AreEqual(1, calls);
            Assert.IsNotNull(captured);
            Assert.IsNull(MapFeedbackOrigin.Current);
            object active = Get("activeMoney");
            Assert.AreEqual(captured.Value, active.GetType().GetField("origin").GetValue(active));
            confirm.onClick.Invoke();
            Assert.AreEqual(1, calls, "Continuing a result must not pay twice.");
            feedback.Advance(2f);
            Assert.AreEqual(137, feedback.DisplayedMoney);
        }
        finally { Object.DestroyImmediate(data); }
    }

    private object Get(string name) => typeof(MapResourceFeedback).GetField(name, Private).GetValue(feedback);
    private TMP_Text Label(Transform parent, string name, string value)
    {
        TMP_Text label = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TMP_Text>();
        label.transform.SetParent(parent, false);
        label.rectTransform.sizeDelta = new Vector2(700f, 90f);
        label.font = font;
        label.fontSize = 30;
        label.text = value;
        return label;
    }
}
