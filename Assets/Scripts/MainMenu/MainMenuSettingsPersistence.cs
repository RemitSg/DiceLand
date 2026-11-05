using System;
using System.IO;
using UnityEngine;

public enum BattleAdvanceMode
{
    Manual = 0,
    Automatic = 1,
    Fast = 2
}

[Serializable]
public sealed class MainMenuSettingsData
{
    [Range(0f, 1f)] public float musicVolume = 1f;
    [Range(0f, 1f)] public float sfxVolume = 1f;
    public bool isFullscreen = true;
    public bool vibrationEnabled = true;
    public BattleAdvanceMode battleAdvanceMode;
    public bool largeDetailText;
    public string languageCode = GameText.Chinese;
    public int qualityLevel = -1;
    public int resolutionWidth;
    public int resolutionHeight;
    public int refreshRate = 60;
    public int targetFrameRate = 120;

    public MainMenuSettingsData Clone()
    {
        return new MainMenuSettingsData
        {
            musicVolume = musicVolume,
            sfxVolume = sfxVolume,
            isFullscreen = isFullscreen,
            vibrationEnabled = vibrationEnabled,
            battleAdvanceMode = battleAdvanceMode,
            largeDetailText = largeDetailText,
            languageCode = languageCode,
            qualityLevel = qualityLevel,
            resolutionWidth = resolutionWidth,
            resolutionHeight = resolutionHeight,
            refreshRate = refreshRate,
            targetFrameRate = targetFrameRate
        };
    }
}

public static class MainMenuSettingsPersistence
{
    private const string SettingsFileName = "main_menu_settings.json";
    private const float LargeDetailFontScale = 1.25f;

    private static MainMenuSettingsData cachedSettings;

    public static MainMenuSettingsData Current
    {
        get
        {
            if (cachedSettings == null)
                cachedSettings = Load();

            return cachedSettings;
        }
    }

    private static string SettingsPath => Path.Combine(Application.persistentDataPath, SettingsFileName);

    public static bool SupportsRuntimeResolutionChanges
    {
        get
        {
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            return false;
#else
            return true;
#endif
        }
    }

    public static MainMenuSettingsData Load()
    {
        if (!File.Exists(SettingsPath))
        {
            MainMenuSettingsData defaults = CreateDefault();
            cachedSettings = defaults;
            return defaults.Clone();
        }

        try
        {
            string json = File.ReadAllText(SettingsPath);
            MainMenuSettingsData loaded = JsonUtility.FromJson<MainMenuSettingsData>(json);
            if (loaded == null)
                loaded = CreateDefault();

            EnsureValid(loaded);
            cachedSettings = loaded;
            return loaded.Clone();
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[MainMenuSettingsPersistence] Failed to load settings: {ex.Message}");
            MainMenuSettingsData defaults = CreateDefault();
            cachedSettings = defaults;
            return defaults.Clone();
        }
    }

    public static void Save(MainMenuSettingsData settings)
    {
        MainMenuSettingsData safeSettings = settings != null ? settings.Clone() : CreateDefault();
        EnsureValid(safeSettings);

        try
        {
            string directory = Path.GetDirectoryName(SettingsPath);
            if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            File.WriteAllText(SettingsPath, JsonUtility.ToJson(safeSettings, true));
            cachedSettings = safeSettings.Clone();
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[MainMenuSettingsPersistence] Failed to save settings: {ex.Message}");
        }
    }

    public static void Apply(MainMenuSettingsData settings)
    {
        MainMenuSettingsData safeSettings = settings != null ? settings.Clone() : CreateDefault();
        EnsureValid(safeSettings);

        ApplyAudioAndHaptics(safeSettings);

        safeSettings.qualityLevel = Mathf.Clamp(
            safeSettings.qualityLevel < 0 ? QualitySettings.GetQualityLevel() : safeSettings.qualityLevel,
            0,
            Mathf.Max(0, QualitySettings.names.Length - 1));

        QualitySettings.SetQualityLevel(safeSettings.qualityLevel, true);
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = safeSettings.targetFrameRate;
        Screen.fullScreen = safeSettings.isFullscreen;

        if (!SupportsRuntimeResolutionChanges)
        {
            safeSettings.resolutionWidth = Screen.width;
            safeSettings.resolutionHeight = Screen.height;
            safeSettings.refreshRate = GetCurrentRefreshRate();
            cachedSettings = safeSettings.Clone();
            return;
        }

        Resolution[] resolutions = Screen.resolutions;
        DisplayResolutionOption[] options = Array.ConvertAll(resolutions, resolution =>
            new DisplayResolutionOption(resolution.width, resolution.height, GetRefreshRate(resolution)));
        int matchedIndex = DisplayResolutionSelectionService.FindBestIndex(options,
            new DisplayResolutionOption(safeSettings.resolutionWidth, safeSettings.resolutionHeight, safeSettings.refreshRate));
        Resolution matchedResolution = matchedIndex >= 0 ? resolutions[matchedIndex] : Screen.currentResolution;

        safeSettings.resolutionWidth = matchedResolution.width;
        safeSettings.resolutionHeight = matchedResolution.height;
        safeSettings.refreshRate = GetRefreshRate(matchedResolution);

#if UNITY_2022_2_OR_NEWER
        FullScreenMode screenMode = safeSettings.isFullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
        RefreshRate ratio = matchedResolution.refreshRateRatio;
        if (ratio.numerator > 0 && ratio.denominator > 0)
            Screen.SetResolution(matchedResolution.width, matchedResolution.height, screenMode, ratio);
        else
            Screen.SetResolution(matchedResolution.width, matchedResolution.height, screenMode);
#else
        Screen.SetResolution(
            matchedResolution.width,
            matchedResolution.height,
            safeSettings.isFullscreen,
            matchedResolution.refreshRate);
#endif

        cachedSettings = safeSettings.Clone();
    }

    public static void SaveInRun(MainMenuSettingsData settings)
    {
        Save(settings);
        ApplyAudioAndHaptics(Current);
    }

    public static float DetailFontSize(float standardSize)
    {
        return Current.largeDetailText ? standardSize * LargeDetailFontScale : standardSize;
    }

    private static void ApplyAudioAndHaptics(MainMenuSettingsData settings)
    {
        // Each audio source applies its own channel volume.
        AudioListener.volume = 1f;
        GameHaptics.ApplySettings(settings.vibrationEnabled);
    }

    public static MainMenuSettingsData CreateDefault()
    {
        int width = SupportsRuntimeResolutionChanges ? Screen.currentResolution.width : Screen.width;
        int height = SupportsRuntimeResolutionChanges ? Screen.currentResolution.height : Screen.height;
        return new MainMenuSettingsData
        {
            musicVolume = 1f,
            sfxVolume = 1f,
            isFullscreen = Screen.fullScreen,
            vibrationEnabled = true,
            qualityLevel = Mathf.Clamp(QualitySettings.GetQualityLevel(), 0, Mathf.Max(0, QualitySettings.names.Length - 1)),
            resolutionWidth = width,
            resolutionHeight = height,
            refreshRate = GetCurrentRefreshRate(),
            targetFrameRate = 120
        };
    }

    public static void EnsureValid(MainMenuSettingsData settings)
    {
        if (settings == null)
            return;

        settings.musicVolume = Mathf.Clamp01(settings.musicVolume);
        settings.sfxVolume = Mathf.Clamp01(settings.sfxVolume);
        settings.languageCode = GameText.Normalize(settings.languageCode);
        if (!Enum.IsDefined(typeof(BattleAdvanceMode), settings.battleAdvanceMode))
            settings.battleAdvanceMode = BattleAdvanceMode.Manual;
        settings.qualityLevel = Mathf.Clamp(
            settings.qualityLevel < 0 ? QualitySettings.GetQualityLevel() : settings.qualityLevel,
            0,
            Mathf.Max(0, QualitySettings.names.Length - 1));

        if (settings.resolutionWidth <= 0 || settings.resolutionHeight <= 0)
        {
            settings.resolutionWidth = SupportsRuntimeResolutionChanges ? Screen.currentResolution.width : Screen.width;
            settings.resolutionHeight = SupportsRuntimeResolutionChanges ? Screen.currentResolution.height : Screen.height;
            settings.refreshRate = GetCurrentRefreshRate();
        }

        settings.refreshRate = Mathf.Max(30, settings.refreshRate);
        settings.targetFrameRate = NormalizeTargetFrameRate(settings.targetFrameRate > 0 ? settings.targetFrameRate : settings.refreshRate);
    }

    public static int NormalizeTargetFrameRate(int frameRate)
    {
        if (frameRate <= 0)
            return 60;

        int[] supportedRates = { 30, 45, 60, 90, 120, 144 };
        int closest = supportedRates[0];
        int closestDistance = Mathf.Abs(frameRate - closest);
        for (int i = 1; i < supportedRates.Length; i++)
        {
            int distance = Mathf.Abs(frameRate - supportedRates[i]);
            if (distance >= closestDistance)
                continue;

            closest = supportedRates[i];
            closestDistance = distance;
        }

        return closest;
    }

    private static int GetCurrentRefreshRate() => GetRefreshRate(Screen.currentResolution);

    internal static int GetRefreshRate(Resolution resolution)
    {
#if UNITY_2022_2_OR_NEWER
        double rate = resolution.refreshRateRatio.value;
#else
        double rate = resolution.refreshRate;
#endif
        return rate > 0 && rate <= int.MaxValue ? Mathf.Max(30, (int)Math.Round(rate)) : 60;
    }
}
