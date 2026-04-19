using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuSettingsPanelController : MonoBehaviour
{
    private const string MasterVolumeKey = "mainmenu.settings.master_volume";
    private const string EffectsVolumeKey = "mainmenu.settings.effects_volume";
    private const string FullscreenKey = "mainmenu.settings.fullscreen";
    private const string VibrationKey = "mainmenu.settings.vibration";
    private const string QualityIndexKey = "mainmenu.settings.quality_index";

    private DemoScenesGalleryManager owner;

    private Slider masterVolumeSlider;
    private Slider effectsVolumeSlider;
    private Button backButton;
    private Button fullscreenButton;
    private Button vibrationButton;
    private Button qualityButton;
    private Button resetButton;

    private float masterVolume = 1f;
    private float effectsVolume = 1f;
    private bool fullscreenEnabled = true;
    private bool vibrationEnabled = true;
    private int qualityIndex;
    private bool initialized;

    public void Initialize(DemoScenesGalleryManager manager)
    {
        owner = manager;

        if (!initialized)
        {
            CacheControls();
            NormalizeStaticLabels();
            HookEvents();
            initialized = true;
        }

        LoadSettings();
        ApplySettings();
        RefreshVisuals();
    }

    private void OnDestroy()
    {
        if (masterVolumeSlider != null)
            masterVolumeSlider.onValueChanged.RemoveListener(HandleMasterVolumeChanged);

        if (effectsVolumeSlider != null)
            effectsVolumeSlider.onValueChanged.RemoveListener(HandleEffectsVolumeChanged);

        if (backButton != null)
            backButton.onClick.RemoveListener(HandleBackClicked);

        if (fullscreenButton != null)
            fullscreenButton.onClick.RemoveListener(HandleFullscreenClicked);

        if (vibrationButton != null)
            vibrationButton.onClick.RemoveListener(HandleVibrationClicked);

        if (qualityButton != null)
            qualityButton.onClick.RemoveListener(HandleQualityClicked);

        if (resetButton != null)
            resetButton.onClick.RemoveListener(HandleResetClicked);
    }

    private void CacheControls()
    {
        List<Slider> sliders = GetComponentsInChildren<Slider>(true)
            .OrderByDescending(GetVisualY)
            .ToList();

        masterVolumeSlider = sliders.ElementAtOrDefault(0);
        effectsVolumeSlider = sliders.ElementAtOrDefault(1);

        foreach (Slider slider in sliders)
            slider.interactable = true;

        Button[] buttons = GetComponentsInChildren<Button>(true);
        List<Button> blueGrayButtons = new List<Button>();
        foreach (Button button in buttons)
        {
            if (button == null)
                continue;

            button.interactable = true;

            switch (button.gameObject.name)
            {
                case "Button_Back":
                    backButton = button;
                    break;
                case "Button_Connect":
                    fullscreenButton = button;
                    break;
                case "Button_Connected":
                    vibrationButton = button;
                    break;
                case "Button_BlueGray":
                    blueGrayButtons.Add(button);
                    break;
            }
        }

        blueGrayButtons = blueGrayButtons
            .OrderByDescending(GetVisualY)
            .ToList();

        qualityButton = blueGrayButtons.ElementAtOrDefault(0);
        resetButton = blueGrayButtons.ElementAtOrDefault(1);
    }

    private void NormalizeStaticLabels()
    {
        ReplaceText("Music", "Master Volume");
        ReplaceText("Sound Fx", "Effects Volume");
        ReplaceText("Push Alram", "Fullscreen");
        ReplaceText("Vibration", "Vibration");
        ReplaceText("English", "Graphics");
        ReplaceText("About", "Reset");

        SetButtonLabel(backButton, "Back");
    }

    private void HookEvents()
    {
        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.onValueChanged.RemoveListener(HandleMasterVolumeChanged);
            masterVolumeSlider.onValueChanged.AddListener(HandleMasterVolumeChanged);
        }

        if (effectsVolumeSlider != null)
        {
            effectsVolumeSlider.onValueChanged.RemoveListener(HandleEffectsVolumeChanged);
            effectsVolumeSlider.onValueChanged.AddListener(HandleEffectsVolumeChanged);
        }

        if (backButton != null)
        {
            backButton.onClick.RemoveListener(HandleBackClicked);
            backButton.onClick.AddListener(HandleBackClicked);
        }

        if (fullscreenButton != null)
        {
            fullscreenButton.onClick.RemoveListener(HandleFullscreenClicked);
            fullscreenButton.onClick.AddListener(HandleFullscreenClicked);
        }

        if (vibrationButton != null)
        {
            vibrationButton.onClick.RemoveListener(HandleVibrationClicked);
            vibrationButton.onClick.AddListener(HandleVibrationClicked);
        }

        if (qualityButton != null)
        {
            qualityButton.onClick.RemoveListener(HandleQualityClicked);
            qualityButton.onClick.AddListener(HandleQualityClicked);
        }

        if (resetButton != null)
        {
            resetButton.onClick.RemoveListener(HandleResetClicked);
            resetButton.onClick.AddListener(HandleResetClicked);
        }
    }

    private void LoadSettings()
    {
        masterVolume = PlayerPrefs.GetFloat(MasterVolumeKey, 0.8f);
        effectsVolume = PlayerPrefs.GetFloat(EffectsVolumeKey, 0.8f);
        fullscreenEnabled = PlayerPrefs.GetInt(FullscreenKey, Screen.fullScreen ? 1 : 0) != 0;
        vibrationEnabled = PlayerPrefs.GetInt(VibrationKey, 1) != 0;

        int maxQualityIndex = Mathf.Max(0, QualitySettings.names.Length - 1);
        int defaultQuality = Mathf.Clamp(QualitySettings.GetQualityLevel(), 0, maxQualityIndex);
        qualityIndex = Mathf.Clamp(PlayerPrefs.GetInt(QualityIndexKey, defaultQuality), 0, maxQualityIndex);
    }

    private void ApplySettings()
    {
        AudioListener.volume = Mathf.Clamp01((masterVolume + effectsVolume) * 0.5f);
        Screen.fullScreen = fullscreenEnabled;
        QualitySettings.SetQualityLevel(qualityIndex, true);
    }

    private void SaveSettings()
    {
        PlayerPrefs.SetFloat(MasterVolumeKey, masterVolume);
        PlayerPrefs.SetFloat(EffectsVolumeKey, effectsVolume);
        PlayerPrefs.SetInt(FullscreenKey, fullscreenEnabled ? 1 : 0);
        PlayerPrefs.SetInt(VibrationKey, vibrationEnabled ? 1 : 0);
        PlayerPrefs.SetInt(QualityIndexKey, qualityIndex);
        PlayerPrefs.Save();
    }

    private void RefreshVisuals()
    {
        if (masterVolumeSlider != null)
            masterVolumeSlider.SetValueWithoutNotify(masterVolume);

        if (effectsVolumeSlider != null)
            effectsVolumeSlider.SetValueWithoutNotify(effectsVolume);

        SetButtonLabel(fullscreenButton, fullscreenEnabled ? "On" : "Off");
        SetButtonLabel(vibrationButton, vibrationEnabled ? "On" : "Off");
        SetButtonLabel(qualityButton, GetQualityLabel());
        SetButtonLabel(resetButton, "Defaults");
    }

    private void HandleMasterVolumeChanged(float value)
    {
        masterVolume = value;
        ApplySettings();
        SaveSettings();
    }

    private void HandleEffectsVolumeChanged(float value)
    {
        effectsVolume = value;
        ApplySettings();
        SaveSettings();
    }

    private void HandleBackClicked()
    {
        if (owner != null)
            owner.ShowItem(0, true);
    }

    private void HandleFullscreenClicked()
    {
        fullscreenEnabled = !fullscreenEnabled;
        ApplySettings();
        SaveSettings();
        RefreshVisuals();
    }

    private void HandleVibrationClicked()
    {
        vibrationEnabled = !vibrationEnabled;
        SaveSettings();
        RefreshVisuals();
    }

    private void HandleQualityClicked()
    {
        int qualityCount = Mathf.Max(1, QualitySettings.names.Length);
        qualityIndex = (qualityIndex + 1) % qualityCount;
        ApplySettings();
        SaveSettings();
        RefreshVisuals();
    }

    private void HandleResetClicked()
    {
        masterVolume = 0.8f;
        effectsVolume = 0.8f;
        fullscreenEnabled = true;
        vibrationEnabled = true;
        qualityIndex = Mathf.Clamp(QualitySettings.names.Length - 1, 0, Mathf.Max(0, QualitySettings.names.Length - 1));

        ApplySettings();
        SaveSettings();
        RefreshVisuals();
    }

    private string GetQualityLabel()
    {
        string[] qualityNames = QualitySettings.names;
        if (qualityNames == null || qualityNames.Length == 0)
            return "Auto";

        int clampedIndex = Mathf.Clamp(qualityIndex, 0, qualityNames.Length - 1);
        return qualityNames[clampedIndex];
    }

    private static float GetVisualY(Component component)
    {
        RectTransform rectTransform = component != null ? component.transform as RectTransform : null;
        return rectTransform != null ? rectTransform.position.y : 0f;
    }

    private void ReplaceText(string current, string replacement)
    {
        TextMeshProUGUI[] textComponents = GetComponentsInChildren<TextMeshProUGUI>(true);
        for (int i = 0; i < textComponents.Length; i++)
        {
            TextMeshProUGUI textComponent = textComponents[i];
            if (textComponent == null)
                continue;

            if (string.Equals(textComponent.text, current, StringComparison.OrdinalIgnoreCase))
                textComponent.text = replacement;
        }
    }

    private void SetButtonLabel(Button button, string label)
    {
        if (button == null)
            return;

        TextMeshProUGUI textComponent = button.GetComponentInChildren<TextMeshProUGUI>(true);
        if (textComponent != null)
            textComponent.text = label;
    }
}
