using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class BattlePauseMenuOverlay : MonoBehaviour
{
    private const string FallbackFontName = "LegacyRuntime.ttf";
    private static BattlePauseMenuOverlay openMenu;

    internal static bool IsOpen => openMenu != null && openMenu.IsVisible;

    [Header("Scene Flow")]
    [SerializeField] private string mainMenuSceneName = BattleExitFlowService.DefaultMainMenuSceneName;
    [SerializeField] private bool allowEscapeToggle = true;

    [Header("Run Settlement")]
    [SerializeField] private string runSettlementSceneName = RunSettlementFlowService.DefaultSettlementSceneName;
    [SerializeField] private RunScoreConfig_SO runScoreConfig;

    [Header("Canvas")]
    [SerializeField] private int canvasSortOrder = 9400;
    [SerializeField] private Vector2 referenceResolution = new Vector2(1920f, 1080f);

    [Header("Text")]
    [SerializeField] private string titleLabel = "战斗菜单";
    [SerializeField] private string resumeLabel = "继续游戏";
    [SerializeField] private string retryLabel = "重试战斗";
    [SerializeField] private string abandonLabel = "放弃战斗";
    [SerializeField] private string closeLabel = "关闭";
    [SerializeField] private string musicLabel = "音乐";
    [SerializeField] private string sfxLabel = "音效";

    [Header("Fonts")]
    [SerializeField] private Font titleFont;
    [SerializeField] private Font bodyFont;
    [SerializeField] private Font buttonFont;

    private Font settingsFont;
    private PauseMenuView view;
    private Canvas canvas;
    private RectTransform panelRect;
    private Slider musicSlider;
    private Slider sfxSlider;
    private Text musicValueText;
    private Text sfxValueText;
    private Text settingsButtonText;
    private MainMenuSettingsOverlay settingsOverlay;
    private bool ignoreSliderEvents;

    public bool IsVisible => canvas != null && canvas.gameObject.activeSelf;
    public Canvas PresentationCanvas => canvas;
    public void ApplyViewport(Vector2 size, Rect safeArea) => view?.ApplyViewport(size, safeArea);

    private void Awake()
    {
        EnsureEventSystem();
        EnsureUi();
        SetVisible(false);
        ApplyCurrentSettingsToSliders();
    }

    private void Update()
    {
        if (!allowEscapeToggle || !Input.GetKeyDown(KeyCode.Escape))
            return;

        if (settingsOverlay != null && settingsOverlay.IsVisible)
            settingsOverlay.Hide();
        else if (IsVisible)
            Hide();
        else
            Show();
    }

    public void ConfigureSettingsFont(Font font)
    {
        settingsFont = font;
        EnsureUi();
        view.ConfigureFonts(ResolveTitleFont(), ResolveBodyFont(), ResolveButtonFont());
    }

    private void ShowSettings()
    {
        if (settingsOverlay == null)
            settingsOverlay = gameObject.AddComponent<MainMenuSettingsOverlay>();
        settingsOverlay.ShowInRun(canvas, settingsButtonText.font);
    }

    private void OnDisable() => SetVisible(false);

    private void OnDestroy()
    {
        if (canvas == null) return;
        if (Application.isPlaying) Destroy(canvas.gameObject);
        else DestroyImmediate(canvas.gameObject);
    }

    public void Show()
    {
        EnsureUi();
        RefreshPresentation();
        ApplyCurrentSettingsToSliders();
        SetVisible(true);
    }

    public void Hide()
    {
        SaveCurrentSliderSettings();
        SetVisible(false);
    }

    private void HandleRetryClicked()
    {
        SaveCurrentSliderSettings();
        BattleEncounterRuntime.RequeueActiveEncounter();
        TutorialBattleRuntime.Clear();
        SpeedButton.CombatLocked = false;
        SpeedButton.InputSuppressed = false;
        SetVisible(false);
        SceneCurtainTransitionController.Instance.TransitionToScene(SceneManager.GetActiveScene().name);
    }

    private void HandleAbandonClicked()
    {
        SaveCurrentSliderSettings();
        if (TryExecuteRunSettlementForAbandon())
            return;

        BattleExitFlowService.ExecuteAbandonToMainMenu(
            mainMenuSceneName,
            sceneName => SceneCurtainTransitionController.Instance.TransitionToScene(sceneName),
            () => SetVisible(false));
    }

    private bool TryExecuteRunSettlementForAbandon()
    {
        BattleEncounterData activeEncounter = BattleEncounterRuntime.ActiveEncounter;
        if (activeEncounter == null || activeEncounter.isTutorialEncounter)
            return false;

        if (!MapRunRuntime.TryGetSnapshot(activeEncounter.sourceSceneName, out MapRunSnapshot snapshot) || snapshot == null || snapshot.runState == null)
            return false;

        RunState settlementRunState = snapshot.runState;
        RunSettlementFlowService.SyncResolvedPartyMembers(settlementRunState, activeEncounter);

        RunSettlementFlowService.ExecuteSettlement(new RunSettlementFlowRequest
        {
            runState = settlementRunState,
            scoreConfig = runScoreConfig,
            runCleared = false,
            settlementSceneName = runSettlementSceneName,
            returnSceneName = mainMenuSceneName,
            deleteSavedRun = MainMenuRunSaveSystem.DeleteSavedRun,
            clearRuntimeState = () =>
            {
                MapRunRuntime.Clear(activeEncounter.sourceSceneName);
                BattleEncounterRuntime.Clear();
                MainMenuRuntime.ClearTutorialRequest();
                TutorialBattleRuntime.Clear();
                SpeedButton.CombatLocked = false;
                SpeedButton.InputSuppressed = false;
                SetVisible(false);
            },
            transitionToScene = sceneName => SceneCurtainTransitionController.Instance.TransitionToScene(sceneName)
        });
        return true;
    }

    private void ApplyCurrentSettingsToSliders()
    {
        if (musicSlider == null || sfxSlider == null)
            return;

        MainMenuSettingsData settings = MainMenuSettingsPersistence.Current;
        ignoreSliderEvents = true;
        musicSlider.SetValueWithoutNotify(settings.musicVolume);
        sfxSlider.SetValueWithoutNotify(settings.sfxVolume);
        ignoreSliderEvents = false;
        RefreshSliderTexts();
    }

    private void SaveCurrentSliderSettings()
    {
        if (musicSlider == null || sfxSlider == null)
            return;

        MainMenuSettingsData settings = MainMenuSettingsPersistence.Current.Clone();
        settings.musicVolume = musicSlider.value;
        settings.sfxVolume = sfxSlider.value;
        MainMenuSettingsPersistence.SaveInRun(settings);
    }

    private void HandleSliderChanged(float _)
    {
        RefreshSliderTexts();
        if (!ignoreSliderEvents)
            SaveCurrentSliderSettings();
    }

    private void RefreshSliderTexts()
    {
        if (musicValueText != null && musicSlider != null)
            musicValueText.text = $"{Mathf.RoundToInt(musicSlider.value * 100f)}%";
        if (sfxValueText != null && sfxSlider != null)
            sfxValueText.text = $"{Mathf.RoundToInt(sfxSlider.value * 100f)}%";
    }

    private void SetVisible(bool visible)
    {
        if (visible)
            openMenu = this;
        else
        {
            if (openMenu == this) openMenu = null;
            if (settingsOverlay != null) settingsOverlay.HideImmediately();
        }
        if (view != null)
            view.SetVisible(visible);
    }

    private void EnsureUi()
    {
        if (canvas != null) return;
        view = PauseMenuView.Create(transform, "BattlePauseMenuCanvas", canvasSortOrder, referenceResolution,
            true, Hide, ShowSettings, HandleRetryClicked, HandleAbandonClicked);
        canvas = view.Canvas;
        panelRect = view.Panel;
        musicSlider = view.MusicSlider;
        sfxSlider = view.SfxSlider;
        musicValueText = view.MusicValue;
        sfxValueText = view.SfxValue;
        settingsButtonText = view.SettingsText;
        view.ConfigureFonts(ResolveTitleFont(), ResolveBodyFont(), ResolveButtonFont());
        RefreshPresentation();
        musicSlider.onValueChanged.AddListener(HandleSliderChanged);
        sfxSlider.onValueChanged.AddListener(HandleSliderChanged);
    }

    private void RefreshPresentation()
    {
        view.RefreshLabels(GameText.Get("battle.pause.title", titleLabel), GameText.Get("ui.resume", resumeLabel),
            GameText.Get("battle.pause.retry", retryLabel), GameText.Get("battle.pause.abandon", abandonLabel),
            GameText.Get("ui.close", closeLabel), GameText.Get("ui.pause.music", musicLabel), GameText.Get("ui.pause.sfx", sfxLabel));
    }

    private Font ResolveTitleFont()
    {
        return titleFont != null ? titleFont : ResolveBodyFont();
    }

    private Font ResolveBodyFont()
    {
        return bodyFont != null ? bodyFont : settingsFont != null ? settingsFont : ResolveFallbackFont();
    }

    private Font ResolveButtonFont()
    {
        if (buttonFont != null)
            return buttonFont;
        if (bodyFont != null)
            return bodyFont;
        return ResolveBodyFont();
    }

    private static Font ResolveFallbackFont()
    {
        return Resources.GetBuiltinResource<Font>(FallbackFontName);
    }

    private static void EnsureEventSystem()
    {
        if (EventSystem.current != null)
            return;

        GameObject eventSystemGo = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        if (Application.isPlaying) DontDestroyOnLoad(eventSystemGo);
    }
}
