#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public sealed class GameSettingsFeedbackPlayModeTests
{
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    private readonly List<string> cues = new List<string>();
    private Type haptics, persistence, musicType;
    private object originalSettings, previousPlayer;
    private GameObject host;
    private BattleFlowPlayModeFixture battle;
    private float originalScale, originalFixedStep, originalListener;
    private int originalQuality, originalVsync, originalFrameRate;
    private int cancellations;
    private Action onPulse;

    [SetUp]
    public void SetUp()
    {
        cues.Clear(); cancellations = 0; onPulse = null;
        originalScale = Time.timeScale; originalFixedStep = Time.fixedDeltaTime;
        originalListener = AudioListener.volume;
        originalQuality = QualitySettings.GetQualityLevel(); originalVsync = QualitySettings.vSyncCount;
        originalFrameRate = Application.targetFrameRate;
        haptics = TypeOf("GameHaptics"); persistence = TypeOf("MainMenuSettingsPersistence"); musicType = TypeOf("GameMusicPlayer");
        originalSettings = persistence.GetProperty("Current").GetValue(null);
        object draft = Invoke(originalSettings, "Clone"); Set(draft, "vibrationEnabled", true);
        Set(draft, "battleAdvanceMode", Enum.Parse(TypeOf("BattleAdvanceMode"), "Manual"));
        persistence.GetField("cachedSettings", Members).SetValue(null, draft);
        previousPlayer = musicType.GetField("instance", Members).GetValue(null);
        musicType.GetField("instance", Members).SetValue(null, null);
        haptics.GetMethod("Stop").Invoke(null, null);
        Type cueType = TypeOf("GameHapticCue"), pulseType = TypeOf("GameHapticPulse");
        object callback = GetType().GetMethod("MakeOutput", Members).MakeGenericMethod(cueType, pulseType).Invoke(this, null);
        haptics.GetField("outputOverride", Members).SetValue(null, callback);
        haptics.GetField("cancelOverride", Members).SetValue(null, (Action)(() => cancellations++));
        host = new GameObject("Settings feedback fixture");
    }

    private Action<TCue, TPulse> MakeOutput<TCue, TPulse>() => (cue, pulse) => { cues.Add(cue.ToString()); onPulse?.Invoke(); };

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        haptics.GetField("outputOverride", Members).SetValue(null, null);
        haptics.GetField("cancelOverride", Members).SetValue(null, null);
        haptics.GetMethod("Stop").Invoke(null, null);
        battle?.TearDown(); battle = null;
        var player = musicType.GetField("instance", Members).GetValue(null) as Component;
        if (player != null && !ReferenceEquals(player, previousPlayer)) Object.Destroy(player.gameObject);
        Object.Destroy(host);
        yield return null;
        persistence.GetField("cachedSettings", Members).SetValue(null, originalSettings);
        musicType.GetField("instance", Members).SetValue(null, previousPlayer);
        QualitySettings.SetQualityLevel(originalQuality, true); QualitySettings.vSyncCount = originalVsync;
        Application.targetFrameRate = originalFrameRate; AudioListener.volume = originalListener;
        Time.timeScale = originalScale; Time.fixedDeltaTime = originalFixedStep;
    }

    [UnityTest]
    public IEnumerator MusicAndSfxKeepTheirOwnEffectiveGainAcrossIndependentSliderChanges()
    {
        Component player = host.AddComponent(musicType);
        AudioSource music = (AudioSource)Get(player, "musicSource");
        GameObject voiceObject = new GameObject("Silent death voice"); voiceObject.transform.SetParent(host.transform);
        AudioSource voice = voiceObject.AddComponent<AudioSource>(); Set(player, "deathAudioSource", voice);
        foreach (Vector2 pair in new[] { new Vector2(.5f, .5f), new Vector2(.5f, .1f), new Vector2(.1f, .5f),
                     new Vector2(0f, .8f), new Vector2(.7f, 0f) })
        {
            object data = Invoke(originalSettings, "Clone");
            Set(data, "musicVolume", pair.x); Set(data, "sfxVolume", pair.y);
            Set(data, "isFullscreen", Screen.fullScreen); Set(data, "qualityLevel", originalQuality);
            Set(data, "resolutionWidth", Screen.currentResolution.width); Set(data, "resolutionHeight", Screen.currentResolution.height);
            persistence.GetMethod("Apply").Invoke(null, new[] { data });
            yield return null;
            Invoke(player, "ApplyDeathSoundVolume");
            Assert.AreEqual(1f, AudioListener.volume, .0001f, "The listener must not reapply either channel slider.");
            Assert.AreEqual(pair.x, music.volume * AudioListener.volume, .0001f);
            Assert.AreEqual(pair.y * .95f, voice.volume * AudioListener.volume, .0001f);
        }
    }

    [UnityTest]
    public IEnumerator DisablingTheSavedSettingCancelsCurrentFeedbackAndBlocksEveryCue()
    {
        Play("Clash"); Assert.AreEqual(1, cues.Count);
        object settings = Invoke(persistence.GetProperty("Current").GetValue(null), "Clone");
        Set(settings, "vibrationEnabled", false); Set(settings, "isFullscreen", Screen.fullScreen);
        persistence.GetMethod("Apply").Invoke(null, new[] { settings });
        Assert.Greater(cancellations, 0);
        foreach (string cue in new[] { "CoinArrived", "Clash", "EnemyDefeated", "AllyDefeated" }) Play(cue);
        yield return null;
        Assert.AreEqual(1, cues.Count);
    }

    [UnityTest]
    public IEnumerator RealClashAndLethalEnemyHitEmitTheirDistinctCuesInOrder()
    {
        BuildBattle(8, 1, 4, 30);
        battle.ClickBattleButton();
        Assert.IsEmpty(cues, "Preparing and selecting targets must not vibrate.");
        battle.ForceContestedConnection(); battle.ClickBattleButton();
        yield return battle.WaitUntilCombatSettles();
        CollectionAssert.AreEqual(new[] { "Clash", "EnemyDefeated" }, cues);
    }

    [UnityTest]
    public IEnumerator RealAllyDeathEmitsOnceAndFurtherDamageDoesNotReplayIt()
    {
        BuildBattle(1, 30, 20, 6);
        SetAdvanceMode("Fast");
        battle.ClickBattleButton(); battle.ForceEnemySingleAttackConnection(); battle.ClickBattleButton();
        yield return battle.WaitUntilCombatSettles();
        CollectionAssert.AreEqual(new[] { "AllyDefeated" }, cues);
        Invoke(battle.Ally, "TakeDamage", 100, 0, -1);
        Assert.AreEqual(1, cues.Count);
    }

    [UnityTest]
    public IEnumerator ManualClashCanSwitchToAutomaticWhileTheBattleMenuIsOpen()
    {
        BuildBattle(8, 1, 4, 30);
        SetAdvanceMode("Manual");
        battle.ClickBattleButton(); battle.ForceContestedConnection(); battle.ClickBattleButton();
        yield return new WaitForSeconds(1f);
        Assert.IsTrue(battle.IsResolvingCombat);
        Assert.IsEmpty(cues, "Manual mode must wait before revealing the clash result.");

        object pause = Get(battle.BattleButton, "battlePauseMenuOverlay");
        Invoke(pause, "Show");
        SetAdvanceMode("Automatic");
        yield return new WaitForSeconds(1f);
        Assert.IsEmpty(cues, "The open pause menu must keep the automatic range timer suspended.");
        Invoke(pause, "SetVisible", false);
        yield return battle.WaitUntilCombatSettles();
        CollectionAssert.AreEqual(new[] { "Clash", "EnemyDefeated" }, cues);

        object presentation = Get(battle.BattleButton, "battlePresentationSystem");
        Assert.AreEqual(0.6f, (float)Invoke(presentation, "GetPresentationDelay", 0.6f), 0.001f);
        SetAdvanceMode("Fast");
        Assert.AreEqual(0.3f, (float)Invoke(presentation, "GetPresentationDelay", 0.6f), 0.001f);
    }

    [UnityTest]
    public IEnumerator BothPauseMenusSaveOnlyRunPreferencesAndRestoreTheirInputOnReturn()
    {
        string path = Path.Combine(Application.persistentDataPath, "main_menu_settings.json");
        byte[] previousFile = File.Exists(path) ? File.ReadAllBytes(path) : null;
        try
        {
            object data = Invoke(originalSettings, "Clone");
            Set(data, "musicVolume", 0.37f); Set(data, "sfxVolume", 0.63f);
            Set(data, "isFullscreen", !Screen.fullScreen);
            Set(data, "targetFrameRate", originalFrameRate == 90 ? 60 : 90);
            Set(data, "resolutionWidth", 1280); Set(data, "resolutionHeight", 720); Set(data, "refreshRate", 75);
            Set(data, "vibrationEnabled", true); Set(data, "largeDetailText", false);
            Set(data, "battleAdvanceMode", Enum.Parse(TypeOf("BattleAdvanceMode"), "Manual"));
            persistence.GetMethod("Save").Invoke(null, new[] { data });
            bool fullscreenBefore = Screen.fullScreen;

            foreach (string type in new[] { "MapPauseMenuOverlay", "BattlePauseMenuOverlay" })
            {
                var menuHost = new GameObject(type + " fixture");
                menuHost.transform.SetParent(host.transform);
                Component pause = menuHost.AddComponent(TypeOf(type));
                Invoke(pause, "Show");
                Canvas pauseCanvas = (Canvas)Get(pause, "canvas");
                Button entry = pauseCanvas.GetComponentsInChildren<Button>().Single(button => button.name == "SettingsButton");
                entry.onClick.Invoke();

                Component settings = menuHost.GetComponent(TypeOf("MainMenuSettingsOverlay"));
                CanvasGroup pauseInput = pauseCanvas.GetComponent<CanvasGroup>();
                Assert.IsFalse(pauseInput.interactable);
                Assert.IsFalse(pauseInput.blocksRaycasts);
                Assert.IsFalse(((RectTransform)Get(settings, "audioSection")).gameObject.activeSelf);
                Assert.IsFalse(((RectTransform)Get(settings, "displaySection")).gameObject.activeSelf);

                var advance = (Dropdown)Get(settings, "battleAdvanceDropdown");
                var detail = (Dropdown)Get(settings, "detailSizeDropdown");
                var vibration = (Toggle)Get(settings, "vibrationToggle");
                int loadedMode = type == "MapPauseMenuOverlay" ? 0 : 2;
                Assert.AreEqual(loadedMode, advance.value);
                Assert.AreEqual(loadedMode == 0 ? 0 : 1, detail.value);
                Assert.AreEqual(loadedMode == 0, vibration.isOn);
                advance.value = 1;
                ((Button)Get(settings, "closeButton")).onClick.Invoke();
                yield return WaitForSettingsMotion(settings);
                Assert.IsFalse((bool)Get(settings, "IsVisible"));
                Assert.IsTrue(pauseInput.interactable);
                Assert.IsTrue(pauseInput.blocksRaycasts);

                entry.onClick.Invoke();
                yield return WaitForSettingsMotion(settings);
                Assert.AreEqual(loadedMode, advance.value, "Returning must discard the draft.");
                int newMode = loadedMode == 0 ? 2 : 1;
                advance.value = newMode; detail.value = loadedMode == 0 ? 1 : 0; vibration.isOn = loadedMode != 0;
                ((Button)Get(settings, "saveButton")).onClick.Invoke();
                yield return WaitForSettingsMotion(settings);

                object saved = persistence.GetMethod("Load").Invoke(null, null);
                Assert.AreEqual(newMode, Convert.ToInt32(Get(saved, "battleAdvanceMode")));
                Assert.AreEqual(loadedMode == 0, Get(saved, "largeDetailText"));
                Assert.AreEqual(loadedMode != 0, Get(saved, "vibrationEnabled"));
                Assert.AreEqual(0.37f, (float)Get(saved, "musicVolume"), 0.001f);
                Assert.AreEqual(0.63f, (float)Get(saved, "sfxVolume"), 0.001f);
                foreach (string field in new[] { "isFullscreen", "qualityLevel", "resolutionWidth", "resolutionHeight", "refreshRate", "targetFrameRate" })
                    Assert.AreEqual(Get(data, field), Get(saved, field), field);
                Assert.AreEqual(fullscreenBefore, Screen.fullScreen);
                Assert.AreEqual(originalFrameRate, Application.targetFrameRate);
                Assert.IsTrue((bool)Get(pause, "IsVisible"));
                Assert.IsTrue(pauseInput.interactable);
                Object.Destroy(menuHost);
                yield return null;
            }
        }
        finally
        {
            if (previousFile == null) File.Delete(path);
            else File.WriteAllBytes(path, previousFile);
        }
    }

    private static IEnumerator WaitForSettingsMotion(Component settings)
    {
        var canvas = (Canvas)Get(settings, "PresentationCanvas");
        Component motion = canvas.GetComponent(TypeOf("MainMenuPageMotion"));
        float deadline = Time.realtimeSinceStartup + 5f;
        while ((bool)Get(motion, "IsTransitioning") && Time.realtimeSinceStartup < deadline)
            yield return null;
        Assert.IsFalse((bool)Get(motion, "IsTransitioning"));
    }

    [UnityTest]
    public IEnumerator DisabledFeedbackRemainsSilentDuringActualCombat()
    {
        Set(persistence.GetProperty("Current").GetValue(null), "vibrationEnabled", false);
        BuildBattle(8, 1, 4, 30);
        battle.ClickBattleButton(); battle.ForceContestedConnection(); battle.ClickBattleButton();
        yield return battle.WaitUntilCombatSettles();
        Assert.IsTrue(battle.EnemyIsDead); Assert.IsEmpty(cues);
    }

    [UnityTest]
    public IEnumerator CoinsVibrateOnlyAfterArrivalAndTheWalletTextHasBeenCreditedEvenWhilePaused()
    {
        Time.timeScale = 0f;
        Component points = host.AddComponent(TypeOf("ActionPointSystem")); Invoke(points, "Initialize", 7);
        Component hud = host.AddComponent(TypeOf("MapHUD"));
        object run = Activator.CreateInstance(TypeOf("RunState")); Set(run, "money", 100);
        Invoke(hud, "Refresh", run, points);
        Component feedback = host.GetComponent(TypeOf("MapResourceFeedback"));
        feedback.GetType().GetProperty("AutomaticUpdate").SetValue(feedback, false);
        Component wallet = host.GetComponentsInChildren<Component>(true)
            .First(c => c != null && c.name == "Money" && c.GetType().Name == "TextMeshProUGUI");
        int creditedAtPulse = 0;
        onPulse = () => { creditedAtPulse = int.Parse((string)Get(wallet, "text")); };
        Set(run, "money", 153); Invoke(hud, "Refresh", run, points);
        Assert.IsEmpty(cues);
        Invoke(feedback, "Advance", .5f); Assert.IsEmpty(cues); Assert.AreEqual("100", Get(wallet, "text"));
        Invoke(feedback, "Advance", .24f);
        CollectionAssert.AreEqual(new[] { "CoinArrived" }, cues);
        Assert.That(creditedAtPulse, Is.InRange(101, 152));
        Invoke(feedback, "Advance", 1f);
        Assert.AreEqual("153", Get(wallet, "text"));
        Assert.AreEqual(1, cues.Count, "Multiple arrivals in one frame share a light pulse.");
        haptics.GetMethod("Stop").Invoke(null, null); cues.Clear();
        Set(run, "money", 103); Invoke(hud, "Refresh", run, points); Invoke(feedback, "Advance", 1f);
        Assert.AreEqual("103", Get(wallet, "text")); Assert.IsEmpty(cues, "Spending must not play a gain pulse.");
        yield return null;
    }

    [UnityTest]
    public IEnumerator FocusLossCancelsAndTheEditorWithoutAnOutputDeviceIsSilent()
    {
        Play("Clash");
        haptics.GetMethod("HandleFocusChanged", Members).Invoke(null, new object[] { false });
        Assert.AreEqual(1, cancellations);
        haptics.GetField("outputOverride", Members).SetValue(null, null);
        foreach (string cue in new[] { "CoinArrived", "Clash", "EnemyDefeated", "AllyDefeated" }) Play(cue);
        yield return null;
        Assert.AreEqual(1, cues.Count);
    }

    private void BuildBattle(int allyPower, int enemyPower, int enemyHp, int allyHp)
    {
        battle = new BattleFlowPlayModeFixture(); battle.BuildOneOnOneBattle(allyPower, enemyPower, enemyHp, allyHp);
        SetAdvanceMode("Automatic");
        // The disposable fixture has no scene-authored settlement font references.
        // Use the same real font as the dedicated settlement integration tests.
        Type database = TypeOf("UnityEditor.AssetDatabase");
        string fontPath = (string)database.GetMethod("GUIDToAssetPath", new[] { typeof(string) })
            .Invoke(null, new object[] { "c6e7143cd2df45b45a784ef8c61a210f" });
        object font = database.GetMethod("LoadAssetAtPath", new[] { typeof(string), typeof(Type) })
            .Invoke(null, new object[] { fontPath, TypeOf("TMPro.TMP_FontAsset") });
        Assert.IsNotNull(font, "The battle fixture requires the project settlement font.");
        Invoke(Get(battle.BattleButton, "battleSettlementOverlay"), "ConfigureFonts", font, font);
    }

    private void SetAdvanceMode(string mode)
    {
        Set(persistence.GetProperty("Current").GetValue(null), "battleAdvanceMode", Enum.Parse(TypeOf("BattleAdvanceMode"), mode));
    }

    private void Play(string cue) => haptics.GetMethod("Play").Invoke(null, new[] { Enum.Parse(TypeOf("GameHapticCue"), cue) });
    private static Type TypeOf(string name) => AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(name)).First(t => t != null);
    private static object Invoke(object target, string method, params object[] args) => target.GetType().GetMethod(method, Members).Invoke(target, args);
    private static void Set(object target, string field, object value) => target.GetType().GetField(field, Members).SetValue(target, value);
    private static object Get(object target, string name)
    {
        FieldInfo field = target.GetType().GetField(name, Members);
        return field != null ? field.GetValue(target) : target.GetType().GetProperty(name, Members).GetValue(target);
    }
}
#endif
