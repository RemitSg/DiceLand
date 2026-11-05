using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public partial class MainMenuSettingsOverlay : MonoBehaviour
{
    private const string FallbackFontName = "LegacyRuntime.ttf";

    private sealed class RuntimeResolutionOption
    {
        public string label;
        public int width;
        public int height;
        public int refreshRate;
    }

    private Canvas canvas;
    private RectTransform panelRect;
    private Slider musicSlider;
    private Slider sfxSlider;
    private Toggle fullscreenToggle;
    private Toggle vibrationToggle;
    private Dropdown qualityDropdown;
    private Dropdown resolutionDropdown;
    private Dropdown frameRateDropdown;
    private Dropdown battleAdvanceDropdown;
    private Dropdown detailSizeDropdown;
    private Dropdown languageDropdown;
    private Text musicValueText;
    private Text sfxValueText;
    private Text titleText;
    private Button saveButton;
    private Button closeButton;
    private readonly List<RuntimeResolutionOption> resolutionOptions = new List<RuntimeResolutionOption>();
    private readonly List<int> frameRateOptions = new List<int> { 30, 45, 60, 90, 120, 144 };
    private readonly List<Text> labelTexts = new List<Text>();
    private readonly List<Text> valueTexts = new List<Text>();
    private readonly List<Text> toggleStateTexts = new List<Text>();
    private readonly List<Text> dropdownTexts = new List<Text>();
    private readonly List<Text> buttonTexts = new List<Text>();
    private Font titleFontOverride;
    private Font bodyFontOverride;
    private Font labelFontOverride;
    private Font valueFontOverride;
    private Font toggleStateFontOverride;
    private Font dropdownFontOverride;
    private Font buttonFontOverride;

    private Action<MainMenuSettingsData> saveHandler;
    private bool gameplayOnly;
    private CanvasGroup pauseInputGroup;
    private bool previousPauseInteractable;
    private bool previousPauseRaycasts;

    public bool IsVisible => canvas != null && canvas.gameObject.activeSelf;
    public Canvas PresentationCanvas => canvas;

    public void ConfigureFonts(
        Font titleFont,
        Font bodyFont,
        Font labelFont,
        Font valueFont,
        Font toggleStateFont,
        Font dropdownFont,
        Font buttonFont)
    {
        titleFontOverride = titleFont;
        bodyFontOverride = bodyFont;
        labelFontOverride = labelFont;
        valueFontOverride = valueFont;
        toggleStateFontOverride = toggleStateFont;
        dropdownFontOverride = dropdownFont;
        buttonFontOverride = buttonFont;

        ApplyConfiguredFonts();
    }

    public void Show(MainMenuSettingsData currentSettings, Action<MainMenuSettingsData> onSave)
    {
        ShowSettings(currentSettings, onSave, false, 9500);
    }

    public void ShowInRun(Canvas pauseCanvas, Font font)
    {
        if (pauseCanvas == null || IsVisible) return;

        ConfigureForestFont(font);
        ShowSettings(MainMenuSettingsPersistence.Current, MainMenuSettingsPersistence.SaveInRun, true,
            Mathf.Max(9800, pauseCanvas.sortingOrder + 1));

        pauseInputGroup = pauseCanvas.GetComponent<CanvasGroup>();
        if (pauseInputGroup == null)
            pauseInputGroup = pauseCanvas.gameObject.AddComponent<CanvasGroup>();
        previousPauseInteractable = pauseInputGroup.interactable;
        previousPauseRaycasts = pauseInputGroup.blocksRaycasts;
        pauseInputGroup.interactable = false;
        pauseInputGroup.blocksRaycasts = false;
    }

    private void ShowSettings(MainMenuSettingsData currentSettings, Action<MainMenuSettingsData> onSave,
        bool inRun, int sortOrder)
    {
        EnsureUi();
        gameplayOnly = inRun;
        saveHandler = onSave;
        canvas.sortingOrder = sortOrder;
        audioSection.gameObject.SetActive(!inRun);
        displaySection.gameObject.SetActive(!inRun);
        languageRow.gameObject.SetActive(!inRun);
        RefreshLocalizedText();

        MainMenuSettingsData safeSettings = currentSettings != null ? currentSettings.Clone() : MainMenuSettingsPersistence.CreateDefault();
        vibrationToggle.SetIsOnWithoutNotify(safeSettings.vibrationEnabled);
        battleAdvanceDropdown.SetValueWithoutNotify((int)safeSettings.battleAdvanceMode);
        detailSizeDropdown.SetValueWithoutNotify(safeSettings.largeDetailText ? 1 : 0);
        if (!inRun)
        {
            languageDropdown.SetValueWithoutNotify(GameText.Normalize(safeSettings.languageCode) == GameText.English ? 1 : 0);
            PopulateResolutionOptions(safeSettings);
            musicSlider.SetValueWithoutNotify(safeSettings.musicVolume);
            sfxSlider.SetValueWithoutNotify(safeSettings.sfxVolume);
            fullscreenToggle.SetIsOnWithoutNotify(safeSettings.isFullscreen);
            qualityDropdown.SetValueWithoutNotify(Mathf.Clamp(safeSettings.qualityLevel, 0, Mathf.Max(0, qualityDropdown.options.Count - 1)));
            resolutionDropdown.SetValueWithoutNotify(FindResolutionIndex(safeSettings));
            frameRateDropdown.SetValueWithoutNotify(FindFrameRateIndex(safeSettings.targetFrameRate));
        }

        RefreshSliderTexts();
        RefreshSwitches();
        saveInvoked = false;
        RememberSelection();
        canvas.gameObject.SetActive(true);
        ApplyViewport(new Vector2(Screen.width, Screen.height), Screen.safeArea);
        MainMenuPageMotion.Show(canvas, null);
    }

    public void Hide()
    {
        CloseDropdowns();
        if (!MainMenuPageMotion.TryHide(canvas, HideImmediately)) HideImmediately();
    }

    internal void HideImmediately()
    {
        if (IsVisible)
            canvas.gameObject.SetActive(false);
        if (pauseInputGroup != null)
        {
            pauseInputGroup.interactable = previousPauseInteractable;
            pauseInputGroup.blocksRaycasts = previousPauseRaycasts;
        }
        pauseInputGroup = null;
        RestoreSelection();
    }

    private void OnDisable() => HideImmediately();

    private void OnDestroy()
    {
        if (canvas == null) return;
        if (Application.isPlaying) Destroy(canvas.gameObject);
        else DestroyImmediate(canvas.gameObject);
    }

    private void EnsureUi()
    {
        if (canvas == null) BuildForestUi();
    }

    private void PopulateResolutionOptions(MainMenuSettingsData currentSettings)
    {
        resolutionOptions.Clear();
        resolutionDropdown.ClearOptions();

        HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
        if (!MainMenuSettingsPersistence.SupportsRuntimeResolutionChanges)
        {
            resolutionOptions.Add(new RuntimeResolutionOption
            {
                width = Screen.width,
                height = Screen.height,
                refreshRate = Mathf.Max(30, currentSettings.refreshRate),
                label = GameText.Format("ui.settings.device_mode", "{width} × {height}（设备）", ("width", Screen.width), ("height", Screen.height))
            });
        }
        else
        {
            Resolution[] resolutions = Screen.resolutions;
            for (int i = resolutions.Length - 1; i >= 0; i--)
            {
                Resolution resolution = resolutions[i];
                int refreshRate = MainMenuSettingsPersistence.GetRefreshRate(resolution);
                string key = $"{resolution.width}x{resolution.height}@{refreshRate}";
                if (!seen.Add(key))
                    continue;

                resolutionOptions.Add(new RuntimeResolutionOption
                {
                    width = resolution.width,
                    height = resolution.height,
                    refreshRate = refreshRate,
                    label = $"{resolution.width} x {resolution.height} ({refreshRate}Hz)"
                });
            }

            if (resolutionOptions.Count == 0)
            {
                resolutionOptions.Add(new RuntimeResolutionOption
                {
                    width = currentSettings.resolutionWidth,
                    height = currentSettings.resolutionHeight,
                    refreshRate = currentSettings.refreshRate,
                    label = $"{currentSettings.resolutionWidth} x {currentSettings.resolutionHeight}"
                });
            }
        }

        List<string> labels = new List<string>(resolutionOptions.Count);
        for (int i = 0; i < resolutionOptions.Count; i++)
            labels.Add(resolutionOptions[i].label);

        resolutionDropdown.AddOptions(labels);
        resolutionDropdown.interactable = MainMenuSettingsPersistence.SupportsRuntimeResolutionChanges;
    }

    private int FindResolutionIndex(MainMenuSettingsData currentSettings)
    {
        int index = DisplayResolutionSelectionService.FindBestIndex(
            resolutionOptions.ConvertAll(option => new DisplayResolutionOption(option.width, option.height, option.refreshRate)),
            new DisplayResolutionOption(currentSettings.resolutionWidth, currentSettings.resolutionHeight, currentSettings.refreshRate));
        return Mathf.Max(0, index);
    }

    private void PopulateFrameRateOptions()
    {
        frameRateDropdown.ClearOptions();

        List<string> labels = new List<string>(frameRateOptions.Count);
        for (int i = 0; i < frameRateOptions.Count; i++)
            labels.Add($"{frameRateOptions[i]} FPS");

        frameRateDropdown.AddOptions(labels);
    }

    private int FindFrameRateIndex(int frameRate)
    {
        int normalized = MainMenuSettingsPersistence.NormalizeTargetFrameRate(frameRate);
        for (int i = 0; i < frameRateOptions.Count; i++)
        {
            if (frameRateOptions[i] == normalized)
                return i;
        }

        return Mathf.Clamp(frameRateOptions.IndexOf(120), 0, Mathf.Max(0, frameRateOptions.Count - 1));
    }

    private void RefreshSliderTexts()
    {
        if (musicValueText != null)
            musicValueText.text = $"{Mathf.RoundToInt(musicSlider.value * 100f)}%";
        if (sfxValueText != null)
            sfxValueText.text = $"{Mathf.RoundToInt(sfxSlider.value * 100f)}%";
    }

    private void HandleSaveClicked()
    {
        if (!IsVisible || saveInvoked || IsPageMoving()) return;
        MainMenuSettingsData data = gameplayOnly ? MainMenuSettingsPersistence.Current.Clone() : new MainMenuSettingsData();
        data.vibrationEnabled = vibrationToggle.isOn;
        data.battleAdvanceMode = (BattleAdvanceMode)battleAdvanceDropdown.value;
        data.largeDetailText = detailSizeDropdown.value == 1;
        if (!gameplayOnly)
        {
            data.languageCode = languageDropdown.value == 1 ? GameText.English : GameText.Chinese;
            data.musicVolume = musicSlider.value;
            data.sfxVolume = sfxSlider.value;
            data.isFullscreen = fullscreenToggle.isOn;
            data.qualityLevel = qualityDropdown.value;
            RuntimeResolutionOption selectedResolution = resolutionOptions[Mathf.Clamp(resolutionDropdown.value, 0, resolutionOptions.Count - 1)];
            data.resolutionWidth = selectedResolution.width;
            data.resolutionHeight = selectedResolution.height;
            data.refreshRate = selectedResolution.refreshRate;
            data.targetFrameRate = frameRateOptions[Mathf.Clamp(frameRateDropdown.value, 0, frameRateOptions.Count - 1)];
        }

        saveInvoked = true;
        saveHandler?.Invoke(data);
        Hide();
    }

    private void ApplyConfiguredFonts()
    {
        if (canvas == null)
            return;

        Font titleFont = ResolveTitleFont();
        Font labelFont = ResolveLabelFont();
        Font valueFont = ResolveValueFont();
        Font toggleStateFont = ResolveToggleStateFont();
        Font dropdownFont = ResolveDropdownFont();
        Font buttonFont = ResolveButtonFont();

        if (titleText != null)
            titleText.font = titleFont;

        ApplyFontToList(labelTexts, labelFont);
        ApplyFontToList(valueTexts, valueFont);
        ApplyFontToList(toggleStateTexts, toggleStateFont);
        ApplyFontToList(dropdownTexts, dropdownFont);
        ApplyFontToList(buttonTexts, buttonFont);
    }

    private Font ResolveTitleFont()
    {
        if (forestFont != null) return forestFont;
        return titleFontOverride != null ? titleFontOverride : ResolveFallbackFont();
    }

    private Font ResolveBodyFont()
    {
        if (forestFont != null) return forestFont;
        return bodyFontOverride != null ? bodyFontOverride : ResolveFallbackFont();
    }

    private Font ResolveLabelFont()
    {
        if (forestFont != null) return forestFont;
        return labelFontOverride != null ? labelFontOverride : ResolveBodyFont();
    }

    private Font ResolveValueFont()
    {
        if (forestFont != null) return forestFont;
        return valueFontOverride != null ? valueFontOverride : ResolveBodyFont();
    }

    private Font ResolveToggleStateFont()
    {
        if (forestFont != null) return forestFont;
        if (toggleStateFontOverride != null)
            return toggleStateFontOverride;
        if (valueFontOverride != null)
            return valueFontOverride;
        return ResolveBodyFont();
    }

    private Font ResolveDropdownFont()
    {
        if (forestFont != null) return forestFont;
        if (dropdownFontOverride != null)
            return dropdownFontOverride;
        if (valueFontOverride != null)
            return valueFontOverride;
        return ResolveBodyFont();
    }

    private Font ResolveButtonFont()
    {
        if (forestFont != null) return forestFont;
        if (buttonFontOverride != null)
            return buttonFontOverride;
        if (bodyFontOverride != null)
            return bodyFontOverride;
        return ResolveFallbackFont();
    }

    private static Font ResolveFallbackFont()
    {
        return Resources.GetBuiltinResource<Font>(FallbackFontName);
    }

    private static void ApplyFontToList(List<Text> texts, Font font)
    {
        for (int i = 0; i < texts.Count; i++)
        {
            if (texts[i] != null)
                texts[i].font = font;
        }
    }

}
