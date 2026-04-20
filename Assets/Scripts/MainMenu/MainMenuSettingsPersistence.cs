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
            refreshRate = refreshRate
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
        Screen.fullScreen = safeSettings.isFullscreen;

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
        Resolution current = Screen.currentResolution;
        return new MainMenuSettingsData
        {
            musicVolume = 1f,
            sfxVolume = 1f,
            isFullscreen = Screen.fullScreen,
            vibrationEnabled = true,
            qualityLevel = Mathf.Clamp(QualitySettings.GetQualityLevel(), 0, Mathf.Max(0, QualitySettings.names.Length - 1)),
            resolutionWidth = current.width,
            resolutionHeight = current.height,
            refreshRate = Mathf.RoundToInt((float)current.refreshRateRatio.value)
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
            Resolution current = Screen.currentResolution;
            settings.resolutionWidth = current.width;
            settings.resolutionHeight = current.height;
            settings.refreshRate = Mathf.RoundToInt((float)current.refreshRateRatio.value);
        }

        settings.refreshRate = Mathf.Max(30, settings.refreshRate);
    }
}
