using System;
using System.IO;
using UnityEngine;

[Serializable]
public sealed class MainMenuSettingsData
{
    [Range(0f, 1f)] public float musicVolume = 1f;
    [Range(0f, 1f)] public float sfxVolume = 1f;
    public bool isFullscreen = true;
    public bool vibrationEnabled = true;
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

        AudioListener.volume = Mathf.Clamp01(Mathf.Max(safeSettings.musicVolume, safeSettings.sfxVolume));

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
        Resolution matchedResolution = default;
        bool foundResolution = false;

        for (int i = 0; i < resolutions.Length; i++)
        {
            Resolution resolution = resolutions[i];
            if (resolution.width != safeSettings.resolutionWidth || resolution.height != safeSettings.resolutionHeight)
                continue;

            matchedResolution = resolution;
            if (resolution.refreshRateRatio.value >= safeSettings.refreshRate)
            {
                foundResolution = true;
                break;
            }

            foundResolution = true;
        }

        if (!foundResolution)
            matchedResolution = Screen.currentResolution;

#if UNITY_2022_2_OR_NEWER
        Screen.SetResolution(
            matchedResolution.width,
            matchedResolution.height,
            safeSettings.isFullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed,
            matchedResolution.refreshRateRatio);
#else
        Screen.SetResolution(
            matchedResolution.width,
            matchedResolution.height,
            safeSettings.isFullscreen,
            matchedResolution.refreshRate);
#endif

        cachedSettings = safeSettings.Clone();
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

    private static int GetCurrentRefreshRate()
    {
#if UNITY_2022_2_OR_NEWER
        return Mathf.Max(30, Mathf.RoundToInt((float)Screen.currentResolution.refreshRateRatio.value));
#else
        return Mathf.Max(30, Screen.currentResolution.refreshRate);
#endif
    }
}
