using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public sealed class PauseMenuForestTests
{
    private GameObject host;
    private Font font;
    private MainMenuSettingsData previousSettings;
    private byte[] previousFile;
    private string settingsPath;
    private float listenerVolume;
    private EventSystem previousEventSystem;
    private EventSystem fixtureEvents;

    [SetUp]
    public void SetUp()
    {
        previousSettings = MainMenuSettingsPersistence.Current;
        settingsPath = Path.Combine(Application.persistentDataPath, "main_menu_settings.json");
        previousFile = File.Exists(settingsPath) ? File.ReadAllBytes(settingsPath) : null;
        listenerVolume = AudioListener.volume;
        host = new GameObject("Pause menu forest fixture");
        previousEventSystem = EventSystem.current;
        var events = new GameObject("PauseTestEvents", typeof(EventSystem));
        events.transform.SetParent(host.transform);
        fixtureEvents = events.GetComponent<EventSystem>();
        // EditMode does not register this non-ExecuteAlways component automatically.
        typeof(EventSystem).GetMethod("OnEnable", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(fixtureEvents, null);
        EventSystem.current = fixtureEvents;
        font = AssetDatabase.LoadAssetAtPath<Font>(MainMenuVisualTools.FontPath);
    }

    [TearDown]
    public void TearDown()
    {
        foreach (PauseMenuView view in host.GetComponentsInChildren<PauseMenuView>(true)) view.SetVisible(false);
        typeof(EventSystem).GetMethod("OnDisable", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(fixtureEvents, null);
        Object.DestroyImmediate(host);
        if (previousEventSystem != null) EventSystem.current = previousEventSystem;
        typeof(MainMenuSettingsPersistence).GetField("cachedSettings", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, previousSettings);
        if (previousFile == null) { if (File.Exists(settingsPath)) File.Delete(settingsPath); }
        else File.WriteAllBytes(settingsPath, previousFile);
        AudioListener.volume = listenerVolume;
        GameHaptics.ApplySettings(previousSettings.vibrationEnabled);
    }

    [TestCase(false, 2340, 1080)]
    [TestCase(true, 2340, 1080)]
    [TestCase(false, 1280, 720)]
    [TestCase(true, 1280, 720)]
    [TestCase(false, 800, 1080)]
    [TestCase(true, 800, 1080)]
    [TestCase(false, 1080, 1920)]
    [TestCase(true, 1080, 1920)]
    public void ActionsAndSliderEndpointsRemainInsideTheSafeArea(bool battle, int width, int height)
    {
        PauseMenuView view = Create(battle);
        Canvas canvas = view.Canvas;
        canvas.GetComponent<CanvasScaler>().enabled = false;
        canvas.renderMode = RenderMode.WorldSpace;
        ((RectTransform)canvas.transform).sizeDelta = new Vector2(1080f * width / height, 1080);
        view.ApplyViewport(new Vector2(width, height), new Rect(40, 30, width - 80, height - 60));
        Canvas.ForceUpdateCanvases();
        Rect safe = Bounds(canvas.transform.Find("SafeArea"));
        Rect panel = Bounds(view.Panel);
        Contains(safe, panel);
        Contains(safe, Bounds(view.Panel.Find("TitlePlaque")));
        foreach (Selectable control in canvas.GetComponentsInChildren<Selectable>()) Contains(panel, Bounds(control.transform));
        Button[] actions = canvas.GetComponentsInChildren<Button>().Where(button => button.name != "CloseXButton").ToArray();
        for (int i = 0; i < actions.Length; i++)
            for (int j = i + 1; j < actions.Length; j++)
                Assert.IsFalse(Bounds(actions[i].transform).Overlaps(Bounds(actions[j].transform)), actions[i].name + " overlaps " + actions[j].name);
        foreach (Slider slider in new[] { view.MusicSlider, view.SfxSlider })
            foreach (float value in new[] { 0f, 1f })
            {
                slider.SetValueWithoutNotify(value); Canvas.ForceUpdateCanvases();
                Contains(Bounds(slider.transform), Bounds(slider.handleRect));
            }
        Assert.AreEqual(6, canvas.GetComponentsInChildren<Button>().Length);
        Assert.IsNotNull(actions.Single(button => button.name == (battle ? "RetryButton" : "SaveAndReturnButton")));
        Assert.IsTrue(canvas.GetComponentsInChildren<Text>().All(text => text.font == font && !text.raycastTarget));
        string[] names = { "ResumeButton", "SettingsButton", battle ? "RetryButton" : "SaveAndReturnButton", "AbandonButton" };
        Rect[] rows = names.Select(name => Bounds(Button(view, name).transform)).ToArray();
        Rect[] icons = names.Select(name => Bounds(Button(view, name).transform.Find("Icon"))).ToArray();
        Rect[] captions = names.Select(name => Bounds(Button(view, name).transform.Find("Text"))).ToArray();
        for (int i = 1; i < rows.Length; i++)
        {
            Assert.AreEqual(rows[0].x, rows[i].x, .01f);
            Assert.AreEqual(rows[0].width, rows[i].width, .01f);
            Assert.AreEqual(rows[0].height, rows[i].height, .01f);
            Assert.AreEqual(rows[0].center.y - rows[1].center.y, rows[i - 1].center.y - rows[i].center.y, .01f);
            Assert.AreEqual(icons[0].center.x, icons[i].center.x, .01f);
            Assert.AreEqual(captions[0].center.x, captions[i].center.x, .01f);
        }
    }

    [Test]
    public void MapSaveAndTwoStepAbandonKeepTheirCallbacksAndResetConfirmationWhenClosed()
    {
        var menu = host.AddComponent<MapPauseMenuOverlay>();
        menu.ConfigureSettingsFont(font);
        int saved = 0, abandoned = 0;
        menu.Configure(() => true, () => saved++, () => abandoned++);
        menu.Show();
        PauseMenuView view = host.GetComponentInChildren<PauseMenuView>();
        Button abandon = Button(view, "AbandonButton");
        abandon.onClick.Invoke();
        Assert.AreEqual(0, abandoned); Assert.IsTrue(menu.IsVisible);
        StringAssert.Contains("确认", view.AbandonText.text);
        StringAssert.Contains("删除", view.StatusText.text);
        Button(view, "CloseButton").onClick.Invoke();
        Assert.IsFalse(menu.IsVisible);
        menu.Show();
        abandon.onClick.Invoke(); Assert.AreEqual(0, abandoned);
        abandon.onClick.Invoke(); Assert.AreEqual(1, abandoned); Assert.IsFalse(menu.IsVisible);
        menu.Show();
        Button(view, "SaveAndReturnButton").onClick.Invoke();
        Assert.AreEqual(1, saved); Assert.AreEqual(1, abandoned); Assert.IsFalse(menu.IsVisible);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void AudioChangesKeepOtherPreferencesAndClosingRestoresPreviousSelection(bool battle)
    {
        var selected = new GameObject("PreviousSelection", typeof(RectTransform), typeof(Button));
        selected.transform.SetParent(host.transform);
        EventSystem.current.SetSelectedGameObject(selected);
        PauseMenuView view = Create(battle);
        Assert.AreEqual("ResumeButton", EventSystem.current.currentSelectedGameObject.name);
        view.MusicSlider.value = .26f; view.SfxSlider.value = .64f;
        Assert.AreEqual("26%", view.MusicValue.text); Assert.AreEqual("64%", view.SfxValue.text);
        Assert.AreEqual(.26f, MainMenuSettingsPersistence.Current.musicVolume, .001f);
        Assert.AreEqual(.64f, MainMenuSettingsPersistence.Current.sfxVolume, .001f);
        Assert.AreEqual(previousSettings.battleAdvanceMode, MainMenuSettingsPersistence.Current.battleAdvanceMode);
        Assert.AreEqual(previousSettings.resolutionWidth, MainMenuSettingsPersistence.Current.resolutionWidth);
        Assert.AreEqual(previousSettings.targetFrameRate, MainMenuSettingsPersistence.Current.targetFrameRate);
        Button(view, "ResumeButton").onClick.Invoke();
        Assert.AreEqual(selected, EventSystem.current.currentSelectedGameObject);
        Assert.IsFalse(view.Canvas.gameObject.activeSelf);
        Assert.IsFalse((bool)typeof(BattlePauseMenuOverlay).GetProperty("IsOpen", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null));
    }

    private PauseMenuView Create(bool battle)
    {
        if (battle) { var menu = host.AddComponent<BattlePauseMenuOverlay>(); menu.ConfigureSettingsFont(font); menu.Show(); }
        else { var menu = host.AddComponent<MapPauseMenuOverlay>(); menu.ConfigureSettingsFont(font); menu.Show(); }
        return host.GetComponentInChildren<PauseMenuView>();
    }

    private static Button Button(PauseMenuView view, string name) => view.GetComponentsInChildren<Button>().Single(button => button.name == name);
    private static Rect Bounds(Transform transform)
    {
        var corners = new Vector3[4]; ((RectTransform)transform).GetWorldCorners(corners);
        return Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
    }
    private static void Contains(Rect outer, Rect inner)
    {
        Assert.GreaterOrEqual(inner.xMin, outer.xMin - .25f); Assert.GreaterOrEqual(inner.yMin, outer.yMin - .25f);
        Assert.LessOrEqual(inner.xMax, outer.xMax + .25f); Assert.LessOrEqual(inner.yMax, outer.yMax + .25f);
    }
}
