using System;
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
    private TMP_FontAsset sharedFont;

    private RectTransform layoutRoot;
    private Slider masterSlider;
    private Slider effectsSlider;
    private Button fullscreenButton;
    private Button vibrationButton;
    private Button qualityButton;
    private Button resetButton;
    private Button backButton;

    private float masterVolume = 0.8f;
    private float effectsVolume = 0.8f;
    private bool fullscreenEnabled = true;
    private bool vibrationEnabled = true;
    private int qualityIndex;
    private bool initialized;

    public void Initialize(DemoScenesGalleryManager manager)
    {
        owner = manager;

        if (!initialized)
        {
            sharedFont = GetComponentsInChildren<TextMeshProUGUI>(true)[0].font;
            PrepareThemedBackground();
            BuildRuntimeLayout();
            HookEvents();
            initialized = true;
        }

        LoadSettings();
        ApplySettings();
        RefreshVisuals();
    }

    private void OnDestroy()
    {
        if (masterSlider != null)
            masterSlider.onValueChanged.RemoveListener(HandleMasterSliderChanged);

        if (effectsSlider != null)
            effectsSlider.onValueChanged.RemoveListener(HandleEffectsSliderChanged);

        if (fullscreenButton != null)
            fullscreenButton.onClick.RemoveListener(ToggleFullscreen);

        if (vibrationButton != null)
            vibrationButton.onClick.RemoveListener(ToggleVibration);

        if (qualityButton != null)
            qualityButton.onClick.RemoveListener(CycleQuality);

        if (resetButton != null)
            resetButton.onClick.RemoveListener(ResetDefaults);

        if (backButton != null)
            backButton.onClick.RemoveListener(GoBack);
    }

    private void PrepareThemedBackground()
    {
        Transform themedRoot = transform.Find("Settings");
        if (themedRoot == null)
            return;

        for (int i = 0; i < themedRoot.childCount; i++)
        {
            Transform child = themedRoot.GetChild(i);
            child.gameObject.SetActive(string.Equals(child.name, "Background", StringComparison.Ordinal));
        }

        Transform background = themedRoot.Find("Background");
        if (background == null)
            return;

        for (int i = 0; i < background.childCount; i++)
            background.GetChild(i).gameObject.SetActive(false);
    }

    private void BuildRuntimeLayout()
    {
        layoutRoot = new GameObject("RuntimeSettingsLayout", typeof(RectTransform)).GetComponent<RectTransform>();
        layoutRoot.SetParent(transform, false);
        layoutRoot.anchorMin = Vector2.zero;
        layoutRoot.anchorMax = Vector2.one;
        layoutRoot.offsetMin = Vector2.zero;
        layoutRoot.offsetMax = Vector2.zero;
        layoutRoot.localScale = Vector3.one;

        CreateLabel(layoutRoot, "SettingsTitle", "Settings", new Vector2(0f, 330f), new Vector2(640f, 80f), 56, FontStyles.Bold, TextAlignmentOptions.Center);

        CreateLabel(layoutRoot, "MasterLabel", "Master Volume", new Vector2(0f, 215f), new Vector2(560f, 40f), 32, FontStyles.Bold, TextAlignmentOptions.Center);
        masterSlider = CreateSlider(layoutRoot, "MasterSlider", new Vector2(0f, 155f), new Vector2(640f, 28f));

        CreateLabel(layoutRoot, "EffectsLabel", "Effects Volume", new Vector2(0f, 80f), new Vector2(560f, 40f), 32, FontStyles.Bold, TextAlignmentOptions.Center);
        effectsSlider = CreateSlider(layoutRoot, "EffectsSlider", new Vector2(0f, 20f), new Vector2(640f, 28f));

        fullscreenButton = CreateButton(layoutRoot, "FullscreenButton", new Vector2(-230f, -120f), new Vector2(260f, 78f), "Fullscreen");
        vibrationButton = CreateButton(layoutRoot, "VibrationButton", new Vector2(230f, -120f), new Vector2(260f, 78f), "Vibration");
        qualityButton = CreateButton(layoutRoot, "QualityButton", new Vector2(-230f, -235f), new Vector2(260f, 78f), "Graphics");
        resetButton = CreateButton(layoutRoot, "ResetButton", new Vector2(230f, -235f), new Vector2(260f, 78f), "Defaults");
        backButton = CreateButton(layoutRoot, "BackButton", new Vector2(0f, -355f), new Vector2(300f, 82f), "Back");
    }

    private void HookEvents()
    {
        masterSlider.onValueChanged.RemoveListener(HandleMasterSliderChanged);
        masterSlider.onValueChanged.AddListener(HandleMasterSliderChanged);

        effectsSlider.onValueChanged.RemoveListener(HandleEffectsSliderChanged);
        effectsSlider.onValueChanged.AddListener(HandleEffectsSliderChanged);

        fullscreenButton.onClick.RemoveListener(ToggleFullscreen);
        fullscreenButton.onClick.AddListener(ToggleFullscreen);

        vibrationButton.onClick.RemoveListener(ToggleVibration);
        vibrationButton.onClick.AddListener(ToggleVibration);

        qualityButton.onClick.RemoveListener(CycleQuality);
        qualityButton.onClick.AddListener(CycleQuality);

        resetButton.onClick.RemoveListener(ResetDefaults);
        resetButton.onClick.AddListener(ResetDefaults);

        backButton.onClick.RemoveListener(GoBack);
        backButton.onClick.AddListener(GoBack);
    }

    private void LoadSettings()
    {
        masterVolume = PlayerPrefs.GetFloat(MasterVolumeKey, 0.8f);
        effectsVolume = PlayerPrefs.GetFloat(EffectsVolumeKey, 0.8f);
        fullscreenEnabled = PlayerPrefs.GetInt(FullscreenKey, Screen.fullScreen ? 1 : 0) != 0;
        vibrationEnabled = PlayerPrefs.GetInt(VibrationKey, 1) != 0;

        int maxQualityIndex = Mathf.Max(0, QualitySettings.names.Length - 1);
        int defaultQualityIndex = Mathf.Clamp(QualitySettings.GetQualityLevel(), 0, maxQualityIndex);
        qualityIndex = Mathf.Clamp(PlayerPrefs.GetInt(QualityIndexKey, defaultQualityIndex), 0, maxQualityIndex);
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
        masterSlider.SetValueWithoutNotify(masterVolume);
        effectsSlider.SetValueWithoutNotify(effectsVolume);

        SetButtonLabel(fullscreenButton, fullscreenEnabled ? "Fullscreen: On" : "Fullscreen: Off");
        SetButtonLabel(vibrationButton, vibrationEnabled ? "Vibration: On" : "Vibration: Off");
        SetButtonLabel(qualityButton, $"Graphics: {GetQualityLabel()}");
        SetButtonLabel(resetButton, "Defaults");
        SetButtonLabel(backButton, "Back");
    }

    private void HandleMasterSliderChanged(float value)
    {
        masterVolume = value;
        ApplySettings();
        SaveSettings();
    }

    private void HandleEffectsSliderChanged(float value)
    {
        effectsVolume = value;
        ApplySettings();
        SaveSettings();
    }

    private void ToggleFullscreen()
    {
        fullscreenEnabled = !fullscreenEnabled;
        ApplySettings();
        SaveSettings();
        RefreshVisuals();
    }

    private void ToggleVibration()
    {
        vibrationEnabled = !vibrationEnabled;
        SaveSettings();
        RefreshVisuals();
    }

    private void CycleQuality()
    {
        int qualityCount = Mathf.Max(1, QualitySettings.names.Length);
        qualityIndex = (qualityIndex + 1) % qualityCount;
        ApplySettings();
        SaveSettings();
        RefreshVisuals();
    }

    private void ResetDefaults()
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

    private void GoBack()
    {
        if (owner != null)
            owner.ShowItem(0, true);
    }

    private string GetQualityLabel()
    {
        if (QualitySettings.names == null || QualitySettings.names.Length == 0)
            return "Auto";

        return QualitySettings.names[Mathf.Clamp(qualityIndex, 0, QualitySettings.names.Length - 1)];
    }

    private TextMeshProUGUI CreateLabel(Transform parent, string objectName, string text, Vector2 anchoredPosition, Vector2 size, float fontSize, FontStyles style, TextAlignmentOptions alignment)
    {
        GameObject labelObject = new GameObject(objectName, typeof(RectTransform));
        labelObject.transform.SetParent(parent, false);

        RectTransform rectTransform = labelObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = size;

        TextMeshProUGUI textComponent = labelObject.AddComponent<TextMeshProUGUI>();
        textComponent.font = sharedFont;
        textComponent.text = text;
        textComponent.fontSize = fontSize;
        textComponent.fontStyle = style;
        textComponent.alignment = alignment;
        textComponent.color = Color.white;
        textComponent.raycastTarget = false;
        return textComponent;
    }

    private Slider CreateSlider(Transform parent, string objectName, Vector2 anchoredPosition, Vector2 size)
    {
        GameObject root = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(Slider));
        root.transform.SetParent(parent, false);

        RectTransform rectTransform = root.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = size;

        Image backgroundImage = root.GetComponent<Image>();
        backgroundImage.color = new Color(0.15f, 0.17f, 0.24f, 0.95f);

        Slider slider = root.GetComponent<Slider>();
        slider.transition = Selectable.Transition.ColorTint;

        RectTransform fillArea = new GameObject("Fill Area", typeof(RectTransform)).GetComponent<RectTransform>();
        fillArea.SetParent(root.transform, false);
        fillArea.anchorMin = Vector2.zero;
        fillArea.anchorMax = Vector2.one;
        fillArea.offsetMin = new Vector2(12f, 6f);
        fillArea.offsetMax = new Vector2(-12f, -6f);

        RectTransform fill = new GameObject("Fill", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
        fill.SetParent(fillArea, false);
        fill.anchorMin = Vector2.zero;
        fill.anchorMax = Vector2.one;
        fill.offsetMin = Vector2.zero;
        fill.offsetMax = Vector2.zero;
        fill.GetComponent<Image>().color = new Color(0.23f, 0.68f, 0.96f, 1f);

        RectTransform handleArea = new GameObject("Handle Area", typeof(RectTransform)).GetComponent<RectTransform>();
        handleArea.SetParent(root.transform, false);
        handleArea.anchorMin = Vector2.zero;
        handleArea.anchorMax = Vector2.one;
        handleArea.offsetMin = new Vector2(12f, 0f);
        handleArea.offsetMax = new Vector2(-12f, 0f);

        RectTransform handle = new GameObject("Handle", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
        handle.SetParent(handleArea, false);
        handle.anchorMin = new Vector2(0f, 0.5f);
        handle.anchorMax = new Vector2(0f, 0.5f);
        handle.pivot = new Vector2(0.5f, 0.5f);
        handle.sizeDelta = new Vector2(28f, 40f);
        handle.GetComponent<Image>().color = new Color(1f, 0.85f, 0.3f, 1f);

        slider.fillRect = fill;
        slider.handleRect = handle;
        slider.targetGraphic = handle.GetComponent<Image>();
        slider.direction = Slider.Direction.LeftToRight;

        return slider;
    }

    private Button CreateButton(Transform parent, string objectName, Vector2 anchoredPosition, Vector2 size, string label)
    {
        GameObject root = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(Button));
        root.transform.SetParent(parent, false);

        RectTransform rectTransform = root.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = size;

        Image image = root.GetComponent<Image>();
        image.color = new Color(0.18f, 0.52f, 0.83f, 0.96f);

        Button button = root.GetComponent<Button>();
        button.transition = Selectable.Transition.ColorTint;
        ColorBlock colors = button.colors;
        colors.normalColor = image.color;
        colors.highlightedColor = new Color(0.24f, 0.6f, 0.92f, 1f);
        colors.pressedColor = new Color(0.12f, 0.38f, 0.64f, 1f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;

        CreateLabel(root.transform, "Label", label, Vector2.zero, size - new Vector2(24f, 18f), 30f, FontStyles.Bold, TextAlignmentOptions.Center);
        return button;
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
