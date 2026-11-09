using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class MapPauseMenuOverlay : MonoBehaviour
{
    private const string FallbackFontName = "LegacyRuntime.ttf";

    [Header("Input")]
    [SerializeField] private bool allowEscapeToggle = true;

    [Header("Canvas")]
    [SerializeField] private int canvasSortOrder = 9400;
    [SerializeField] private Vector2 referenceResolution = new Vector2(1920f, 1080f);

    [Header("Text")]
    [SerializeField] private string titleLabel = "地图菜单";
    [SerializeField] private string resumeLabel = "继续游戏";
    [SerializeField] private string saveAndReturnLabel = "保存并返回主菜单";
    [SerializeField] private string abandonLabel = "放弃游戏";
    [SerializeField] private string confirmAbandonLabel = "确认放弃";
    [SerializeField] private string closeLabel = "关闭";
    [SerializeField] private string musicLabel = "音乐";
    [SerializeField] private string sfxLabel = "音效";
    [SerializeField] private string abandonWarningText = "再次点击“确认放弃”将删除当前存档并返回主菜单。";

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
    private Text statusText;
    private Text abandonButtonText;
    private Text settingsButtonText;
    private MainMenuSettingsOverlay settingsOverlay;
    private Func<bool> canOpenEvaluator;
    private Action saveAndReturnHandler;
    private Action abandonHandler;
    private bool ignoreSliderEvents;
    private bool awaitingAbandonConfirmation;

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

        if (IsVisible)
        {
            if (settingsOverlay != null && settingsOverlay.IsVisible)
                settingsOverlay.Hide();
            else
                Hide();
            return;
        }

        if (CanOpen())
            Show();
    }

    public void Configure(Func<bool> canOpen, Action onSaveAndReturn, Action onAbandon)
    {
        canOpenEvaluator = canOpen;
        saveAndReturnHandler = onSaveAndReturn;
        abandonHandler = onAbandon;
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

    public void Show()
    {
        EnsureUi();
        RefreshPresentation();
        ApplyCurrentSettingsToSliders();
        ResetAbandonConfirmation();
        SetVisible(true);
    }

    public void Hide()
    {
        SaveCurrentSliderSettings();
        ResetAbandonConfirmation();
        SetVisible(false);
    }

    private bool CanOpen()
    {
        return canOpenEvaluator == null || canOpenEvaluator.Invoke();
    }

    private void HandleSaveAndReturnClicked()
    {
        SaveCurrentSliderSettings();
        SetVisible(false);
        saveAndReturnHandler?.Invoke();
    }

    private void HandleAbandonClicked()
    {
        if (!awaitingAbandonConfirmation)
        {
            awaitingAbandonConfirmation = true;
            view.SetWarning(true);
            if (statusText != null)
                statusText.text = GameText.Get("map.pause.abandon_warning", abandonWarningText);
            if (abandonButtonText != null)
                abandonButtonText.text = GameText.Get("map.pause.confirm_abandon", confirmAbandonLabel);
            return;
        }

        SaveCurrentSliderSettings();
        SetVisible(false);
        abandonHandler?.Invoke();
    }

    private void ResetAbandonConfirmation()
    {
        awaitingAbandonConfirmation = false;
        if (view != null)
            view.SetWarning(false);
        if (abandonButtonText != null)
            abandonButtonText.text = GameText.Get("map.pause.abandon", abandonLabel);
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
        if (!visible && settingsOverlay != null)
            settingsOverlay.HideImmediately();
        if (view != null)
            view.SetVisible(visible);
    }

    private void EnsureUi()
    {
        if (canvas != null) return;
        view = PauseMenuView.Create(transform, "MapPauseMenuCanvas", canvasSortOrder, referenceResolution,
            false, Hide, ShowSettings, HandleSaveAndReturnClicked, HandleAbandonClicked);
        canvas = view.Canvas;
        panelRect = view.Panel;
        musicSlider = view.MusicSlider;
        sfxSlider = view.SfxSlider;
        musicValueText = view.MusicValue;
        sfxValueText = view.SfxValue;
        settingsButtonText = view.SettingsText;
        statusText = view.StatusText;
        abandonButtonText = view.AbandonText;
        view.ConfigureFonts(ResolveTitleFont(), ResolveBodyFont(), ResolveButtonFont());
        RefreshPresentation();
        musicSlider.onValueChanged.AddListener(HandleSliderChanged);
        sfxSlider.onValueChanged.AddListener(HandleSliderChanged);
    }

    private void RefreshPresentation()
    {
        view.RefreshLabels(GameText.Get("map.pause.title", titleLabel), GameText.Get("ui.resume", resumeLabel),
            GameText.Get("map.pause.save_return", saveAndReturnLabel), GameText.Get("map.pause.abandon", abandonLabel),
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
