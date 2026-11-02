using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

public sealed class MainMenuMotionPlayModeTests
{
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private GameObject host;
    private Component view;
    private readonly List<ScriptableObject> configurations = new List<ScriptableObject>();
    private readonly List<string> actions = new List<string>();
    private readonly List<GameObject> pages = new List<GameObject>();
    private int starts;
    private float previousTimeScale;

    [SetUp]
    public void SetUp()
    {
        previousTimeScale = Time.timeScale;
        host = new GameObject("MainMenuMotionTestHost");
        actions.Clear();
        starts = 0;
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        Time.timeScale = previousTimeScale;
        UnityEngine.Object.Destroy(host);
        foreach (GameObject page in pages) if (page != null) UnityEngine.Object.Destroy(page);
        foreach (ScriptableObject config in configurations) UnityEngine.Object.Destroy(config);
        pages.Clear();
        configurations.Clear();
        yield return null;
        yield return null;
    }

    [UnityTest]
    public IEnumerator HoverRetargetsContinuouslyAndSettlesWithUnscaledTime()
    {
        CreateView();
        Time.timeScale = 0f;
        RectTransform frame = Frame();
        Vector2 initial = frame.anchoredPosition;
        Invoke(view, "Hover", ActionId("Settings"));
        Assert.AreEqual(initial, frame.anchoredPosition);
        for (int i = 0; i < 4; i++) yield return null;
        Assert.Less(frame.anchoredPosition.y, initial.y);
        Vector2 moving = frame.anchoredPosition;
        Invoke(view, "Hover", ActionId("Tutorial"));
        Invoke(view, "Hover", ActionId("Ability"));
        Assert.AreEqual(moving, frame.anchoredPosition);
        Vector2 target = ((RectTransform)Button("Ability").transform).anchoredPosition;
        yield return Until(() => Vector2.Distance(frame.anchoredPosition, target) < 0.1f);
        Assert.Less(Button("Ability").transform.Find("Label").GetComponent<Text>().color.r, 0.4f);
        Assert.IsFalse(frame.GetComponent<Graphic>().raycastTarget);
    }

    [UnityTest]
    public IEnumerator DifficultyCrossfadeRejectsQueuedLaunchThenStartsExactlyOnce()
    {
        CreateView();
        Invoke(view, "ShowDifficulty");
        Transform menu = Composition().Find("Menu");
        Transform difficulty = Composition().Find("Difficulty");
        Assert.IsTrue(menu.gameObject.activeSelf, "The old page must remain visible during the crossfade.");
        Assert.IsTrue(difficulty.gameObject.activeSelf);
        Button start = difficulty.Find("StartAdventure").GetComponent<Button>();
        start.onClick.Invoke();
        Button("Ability").onClick.Invoke();
        Assert.AreEqual(0, starts);
        Assert.IsEmpty(actions);
        yield return Until(() => !Get<bool>(view, "IsPageTransitioning"));
        Assert.IsFalse(menu.gameObject.activeSelf);
        Assert.AreEqual(1f, difficulty.GetComponent<CanvasGroup>().alpha);
        start.onClick.Invoke();
        start.onClick.Invoke();
        Assert.AreEqual(1, starts);
    }

    [UnityTest]
    public IEnumerator ReturningDuringDifficultyEntranceRestoresInputAndDemoEntry()
    {
        CreateView();
        Time.timeScale = 0f;
        Invoke(view, "ShowDifficulty");
        yield return null;
        Invoke(view, "HideDifficulty");
        Invoke(view, "HideDifficulty");
        yield return Until(() => !Get<bool>(view, "IsPageTransitioning"));
        Assert.IsFalse(Get<bool>(view, "IsDifficultyVisible"));
        Assert.IsFalse(Composition().Find("Difficulty").gameObject.activeSelf);
        Assert.AreEqual(1f, Composition().Find("Menu").GetComponent<CanvasGroup>().alpha);
        Assert.IsTrue(Button("DemoTest").gameObject.activeInHierarchy);
        Button("Ability").onClick.Invoke();
        CollectionAssert.AreEqual(new[] { "Ability" }, actions);
    }

    [UnityTest]
    public IEnumerator ClosingPageDuringEntranceBlocksInputAndCompletesOnceAtTimeScaleZero()
    {
        Time.timeScale = 0f;
        Canvas page = CreatePage();
        CanvasGroup group = page.GetComponent<CanvasGroup>();
        group.alpha = 0.8f;
        int returning = 0, closed = 0;
        Show(page, () => returning++);
        Assert.AreEqual(0f, group.alpha);
        Assert.IsFalse(group.interactable);
        Assert.IsTrue(group.blocksRaycasts);
        yield return null;
        Action close = () => { closed++; page.gameObject.SetActive(false); };
        Assert.IsTrue(Hide(page, close));
        Assert.IsTrue(Hide(page, close));
        Assert.AreEqual(1, returning);
        Assert.AreEqual(0, closed);
        yield return Until(() => closed == 1);
        Assert.IsFalse(page.gameObject.activeSelf);
        Assert.That(group.alpha, Is.EqualTo(0.8f).Within(0.001f));
        Assert.IsTrue(group.interactable);
        page.gameObject.SetActive(true);
        Show(page, () => returning++);
        yield return Until(() => !Get<bool>(Motion(page), "IsTransitioning"));
        Assert.That(group.alpha, Is.EqualTo(0.8f).Within(0.001f));
        Assert.AreEqual(1, page.transform.childCount, "Reopening must reuse the motion root.");
        Assert.IsNotNull(page.transform.Find("PageMotionRoot/Content"));
        Assert.AreEqual(1, closed);
    }

    [UnityTest]
    public IEnumerator ReopeningDuringReturnPreservesPoseAndCancelsStaleClose()
    {
        Canvas page = CreatePage();
        CanvasGroup group = page.GetComponent<CanvasGroup>();
        int closed = 0;
        Show(page, null);
        yield return Until(() => !Get<bool>(Motion(page), "IsTransitioning"));
        Assert.IsTrue(Hide(page, () => closed++));
        yield return null;
        float alpha = group.alpha;
        Vector2 position = page.transform.Find("PageMotionRoot").GetComponent<RectTransform>().anchoredPosition;
        Show(page, null);
        Assert.AreEqual(alpha, group.alpha);
        Assert.AreEqual(position, page.transform.Find("PageMotionRoot").GetComponent<RectTransform>().anchoredPosition);
        yield return Until(() => !Get<bool>(Motion(page), "IsTransitioning"));
        Assert.AreEqual(0, closed);
        Assert.AreEqual(1f, group.alpha);
        Assert.IsTrue(group.interactable);
        Assert.IsFalse(Get<bool>(Motion(page), "IsClosing"));
    }

    [UnityTest]
    public IEnumerator TutorialBlocksEarlyAndDuplicateStartsAndReturnsAfterFade()
    {
        Component tutorial = host.AddComponent(RuntimeType("MainMenuTutorialOverlay"));
        int started = 0, returned = 0;
        Invoke(tutorial, "Show", null, (Action)(() => started++), (Action)(() => returned++), null, null, null, null, true);
        Canvas page = Get<Canvas>(tutorial, "PresentationCanvas");
        Show(page, null);
        Button start = Get<Text>(tutorial, "DebugStartButtonText").GetComponentInParent<Button>();
        start.onClick.Invoke();
        Assert.AreEqual(0, started);
        yield return Until(() => !Get<bool>(Motion(page), "IsTransitioning"));
        start.onClick.Invoke();
        start.onClick.Invoke();
        Assert.AreEqual(1, started);
        Invoke(tutorial, "Hide");
        Invoke(tutorial, "Hide");
        Assert.IsTrue(Get<bool>(tutorial, "IsVisible"));
        Assert.AreEqual(0, returned);
        yield return Until(() => !Get<bool>(tutorial, "IsVisible"));
        Assert.AreEqual(1, returned);
    }

    [UnityTest]
    public IEnumerator AbilitySettingsAndCreditsAnimateAndCleanUpWithOwner()
    {
        var ownedCanvases = new List<Canvas>();
        foreach (string name in new[] { "AbilityMenuOverlay", "MainMenuSettingsOverlay", "MainMenuContributorsOverlay" })
        {
            Component overlay = host.AddComponent(RuntimeType(name));
            if (name == "AbilityMenuOverlay") Invoke(overlay, "Show", null, null, null, null);
            else if (name == "MainMenuSettingsOverlay") Invoke(overlay, "Show", null, null);
            else Invoke(overlay, "Show", new object[] { null });
            Canvas page = Get<Canvas>(overlay, "PresentationCanvas");
            ownedCanvases.Add(page);
            Show(page, null);
            Assert.AreEqual(0f, page.GetComponent<CanvasGroup>().alpha, name);
            yield return Until(() => !Get<bool>(Motion(page), "IsTransitioning"));
            Assert.AreEqual(1f, page.GetComponent<CanvasGroup>().alpha, name);
            Invoke(overlay, "Hide");
            Assert.IsTrue(Get<bool>(overlay, "IsVisible"), name);
            yield return Until(() => !Get<bool>(overlay, "IsVisible"));
        }
        UnityEngine.Object.Destroy(host);
        yield return null;
        yield return null;
        foreach (Canvas page in ownedCanvases) Assert.IsTrue(page == null, "Destroying the owner must release the independent page canvas.");
    }

    private void CreateView()
    {
        Type configType = RuntimeType("DifficultyConfig_SO");
        Array configs = Array.CreateInstance(configType, 1);
        ScriptableObject config = ScriptableObject.CreateInstance(configType);
        configType.GetField("difficultyName").SetValue(config, "Easy");
        configurations.Add(config);
        configs.SetValue(config, 0);
        view = host.AddComponent(RuntimeType("MainMenuForestView"));
        Type actionType = RuntimeType("MainMenuSceneController+MenuAction");
        Delegate action = (Delegate)GetType().GetMethod("ActionCallback", Members).MakeGenericMethod(actionType).Invoke(this, null);
        Delegate start = (Delegate)GetType().GetMethod("StartCallback", Members).MakeGenericMethod(configType).Invoke(this, null);
        Invoke(view, "Build", null, null, configs, action, start);
        Invoke(view, "SetContinueAvailable", true);
    }

    private Action<T> ActionCallback<T>() => value => actions.Add(value.ToString());
    private Action<T> StartCallback<T>() => value => starts++;
    private object ActionId(string name) => Enum.Parse(RuntimeType("MainMenuSceneController+MenuAction"), name);
    private Button Button(string name) => (Button)Invoke(view, "GetButton", ActionId(name));
    private Transform Composition() => Get<Canvas>(view, "Canvas").transform.Find("SafeArea/Composition");
    private RectTransform Frame() => Composition().Find("Menu/SelectionFrame").GetComponent<RectTransform>();

    private Canvas CreatePage()
    {
        GameObject page = new GameObject("TestMenuPage", typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup));
        pages.Add(page);
        new GameObject("Content", typeof(RectTransform)).transform.SetParent(page.transform, false);
        return page.GetComponent<Canvas>();
    }

    private static Component Motion(Canvas page) => page.GetComponent(RuntimeType("MainMenuPageMotion"));
    private static void Show(Canvas page, Action returning) => RuntimeType("MainMenuPageMotion").GetMethod("Show").Invoke(null, new object[] { page, returning });
    private static bool Hide(Canvas page, Action closed) => (bool)RuntimeType("MainMenuPageMotion").GetMethod("TryHide").Invoke(null, new object[] { page, closed });
    private static T Get<T>(object target, string name) => (T)target.GetType().GetProperty(name, Members).GetValue(target);
    private static object Invoke(object target, string name, params object[] args) => target.GetType().GetMethod(name, Members).Invoke(target, args);

    private static Type RuntimeType(string name)
    {
        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type type = assembly.GetType(name);
            if (type != null) return type;
        }
        throw new InvalidOperationException("Missing runtime type " + name);
    }

    private static IEnumerator Until(Func<bool> condition)
    {
        float deadline = Time.realtimeSinceStartup + 4f;
        while (!condition() && Time.realtimeSinceStartup < deadline) yield return null;
        Assert.IsTrue(condition(), "Menu transition did not settle within four seconds.");
    }
}
