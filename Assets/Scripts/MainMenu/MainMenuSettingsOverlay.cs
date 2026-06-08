using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class MainMenuSettingsOverlay : MonoBehaviour
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

    public bool IsVisible => canvas != null && canvas.gameObject.activeSelf;

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
        EnsureUi();
        saveHandler = onSave;

        MainMenuSettingsData safeSettings = currentSettings != null ? currentSettings.Clone() : MainMenuSettingsPersistence.CreateDefault();
        PopulateResolutionOptions(safeSettings);

        musicSlider.SetValueWithoutNotify(safeSettings.musicVolume);
        sfxSlider.SetValueWithoutNotify(safeSettings.sfxVolume);
        fullscreenToggle.SetIsOnWithoutNotify(safeSettings.isFullscreen);
        vibrationToggle.SetIsOnWithoutNotify(safeSettings.vibrationEnabled);
        qualityDropdown.SetValueWithoutNotify(Mathf.Clamp(safeSettings.qualityLevel, 0, Mathf.Max(0, qualityDropdown.options.Count - 1)));
        resolutionDropdown.SetValueWithoutNotify(FindResolutionIndex(safeSettings));
        frameRateDropdown.SetValueWithoutNotify(FindFrameRateIndex(safeSettings.targetFrameRate));

        RefreshSliderTexts();
        canvas.gameObject.SetActive(true);
    }

    public void Hide()
    {
        if (canvas != null)
            canvas.gameObject.SetActive(false);
    }

    private void EnsureUi()
    {
        if (canvas != null)
            return;

        Font titleFont = ResolveTitleFont();
        Font labelFont = ResolveLabelFont();
        Font valueFont = ResolveValueFont();
        Font toggleStateFont = ResolveToggleStateFont();
        Font dropdownFont = ResolveDropdownFont();
        Font buttonFont = ResolveButtonFont();

        GameObject canvasGo = new GameObject("MainMenuSettingsOverlay", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));

        canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9500;

        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform rootRect = canvasGo.GetComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        panelRect = CreateRect("Panel", rootRect, new Vector2(1f, 0.5f), new Vector2(-96f, 0f), new Vector2(860f, 860f));
        Image panelImage = panelRect.gameObject.AddComponent<Image>();
        panelImage.color = new Color(0.08f, 0.1f, 0.14f, 0.96f);

        titleText = CreateText("Title", panelRect, new Vector2(0f, -56f), new Vector2(680f, 72f), "Settings", 42, titleFont, TextAnchor.MiddleCenter, Color.white);

        float leftX = -300f;
        float rightX = 86f;
        float valueX = 344f;
        float startY = -150f;
        float rowGap = 86f;

        labelTexts.Add(CreateText("MusicLabel", panelRect, new Vector2(leftX, startY), new Vector2(220f, 40f), "Music", 28, labelFont, TextAnchor.MiddleLeft, Color.white));
        musicSlider = CreateSlider("MusicSlider", panelRect, new Vector2(rightX, startY), new Vector2(360f, 24f));
        musicValueText = CreateText("MusicValue", panelRect, new Vector2(valueX, startY), new Vector2(96f, 36f), "100%", 22, valueFont, TextAnchor.MiddleRight, new Color(0.85f, 0.85f, 0.85f, 1f));
        valueTexts.Add(musicValueText);

        labelTexts.Add(CreateText("SfxLabel", panelRect, new Vector2(leftX, startY - rowGap), new Vector2(220f, 40f), "SFX", 28, labelFont, TextAnchor.MiddleLeft, Color.white));
        sfxSlider = CreateSlider("SfxSlider", panelRect, new Vector2(rightX, startY - rowGap), new Vector2(360f, 24f));
        sfxValueText = CreateText("SfxValue", panelRect, new Vector2(valueX, startY - rowGap), new Vector2(96f, 36f), "100%", 22, valueFont, TextAnchor.MiddleRight, new Color(0.85f, 0.85f, 0.85f, 1f));
        valueTexts.Add(sfxValueText);

        labelTexts.Add(CreateText("FullscreenLabel", panelRect, new Vector2(leftX, startY - rowGap * 2f), new Vector2(220f, 40f), "Fullscreen", 28, labelFont, TextAnchor.MiddleLeft, Color.white));
        fullscreenToggle = CreateToggle("FullscreenToggle", panelRect, new Vector2(rightX - 98f, startY - rowGap * 2f), toggleStateFont, out Text fullscreenStateText);
        toggleStateTexts.Add(fullscreenStateText);

        labelTexts.Add(CreateText("VibrationLabel", panelRect, new Vector2(leftX, startY - rowGap * 3f), new Vector2(220f, 40f), "Vibration", 28, labelFont, TextAnchor.MiddleLeft, Color.white));
        vibrationToggle = CreateToggle("VibrationToggle", panelRect, new Vector2(rightX - 98f, startY - rowGap * 3f), toggleStateFont, out Text vibrationStateText);
        toggleStateTexts.Add(vibrationStateText);

        labelTexts.Add(CreateText("QualityLabel", panelRect, new Vector2(leftX, startY - rowGap * 4f), new Vector2(220f, 40f), "Quality", 28, labelFont, TextAnchor.MiddleLeft, Color.white));
        qualityDropdown = CreateDropdown("QualityDropdown", panelRect, new Vector2(rightX, startY - rowGap * 4f), new Vector2(360f, 48f), dropdownFont, out Text qualityCaptionText, out Text qualityItemText);
        dropdownTexts.Add(qualityCaptionText);
        dropdownTexts.Add(qualityItemText);
        qualityDropdown.ClearOptions();
        qualityDropdown.AddOptions(new List<string>(QualitySettings.names));

        labelTexts.Add(CreateText("ResolutionLabel", panelRect, new Vector2(leftX, startY - rowGap * 5f), new Vector2(220f, 40f), "Resolution", 28, labelFont, TextAnchor.MiddleLeft, Color.white));
        resolutionDropdown = CreateDropdown("ResolutionDropdown", panelRect, new Vector2(rightX, startY - rowGap * 5f), new Vector2(360f, 48f), dropdownFont, out Text resolutionCaptionText, out Text resolutionItemText);
        dropdownTexts.Add(resolutionCaptionText);
        dropdownTexts.Add(resolutionItemText);

        labelTexts.Add(CreateText("FrameRateLabel", panelRect, new Vector2(leftX, startY - rowGap * 6f), new Vector2(220f, 40f), "Frame Rate", 28, labelFont, TextAnchor.MiddleLeft, Color.white));
        frameRateDropdown = CreateDropdown("FrameRateDropdown", panelRect, new Vector2(rightX, startY - rowGap * 6f), new Vector2(360f, 48f), dropdownFont, out Text frameRateCaptionText, out Text frameRateItemText);
        dropdownTexts.Add(frameRateCaptionText);
        dropdownTexts.Add(frameRateItemText);
        PopulateFrameRateOptions();

        saveButton = CreateButton("SaveButton", panelRect, new Vector2(-110f, 58f), new Vector2(220f, 64f), "Save", buttonFont, out Text saveButtonText);
        closeButton = CreateButton("CloseButton", panelRect, new Vector2(120f, 58f), new Vector2(220f, 64f), "Close", buttonFont, out Text closeButtonText);
        buttonTexts.Add(saveButtonText);
        buttonTexts.Add(closeButtonText);

        musicSlider.minValue = 0f;
        musicSlider.maxValue = 1f;
        sfxSlider.minValue = 0f;
        sfxSlider.maxValue = 1f;

        musicSlider.onValueChanged.AddListener(_ => RefreshSliderTexts());
        sfxSlider.onValueChanged.AddListener(_ => RefreshSliderTexts());
        saveButton.onClick.AddListener(HandleSaveClicked);
        closeButton.onClick.AddListener(Hide);

        ApplyConfiguredFonts();
        canvas.gameObject.SetActive(false);
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
                label = $"{Screen.width} x {Screen.height} (Device)"
            });
        }
        else
        {
            Resolution[] resolutions = Screen.resolutions;
            for (int i = resolutions.Length - 1; i >= 0; i--)
            {
                Resolution resolution = resolutions[i];
                string key = $"{resolution.width}x{resolution.height}@{Mathf.RoundToInt((float)resolution.refreshRateRatio.value)}";
                if (!seen.Add(key))
                    continue;

                resolutionOptions.Add(new RuntimeResolutionOption
                {
                    width = resolution.width,
                    height = resolution.height,
                    refreshRate = Mathf.RoundToInt((float)resolution.refreshRateRatio.value),
                    label = $"{resolution.width} x {resolution.height} ({Mathf.RoundToInt((float)resolution.refreshRateRatio.value)}Hz)"
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
        for (int i = 0; i < resolutionOptions.Count; i++)
        {
            RuntimeResolutionOption option = resolutionOptions[i];
            if (option.width == currentSettings.resolutionWidth &&
                option.height == currentSettings.resolutionHeight)
            {
                return i;
            }
        }

        return 0;
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
        MainMenuSettingsData data = new MainMenuSettingsData
        {
            musicVolume = musicSlider.value,
            sfxVolume = sfxSlider.value,
            isFullscreen = fullscreenToggle.isOn,
            vibrationEnabled = vibrationToggle.isOn,
            qualityLevel = qualityDropdown.value
        };

        int resolutionIndex = Mathf.Clamp(resolutionDropdown.value, 0, Mathf.Max(0, resolutionOptions.Count - 1));
        RuntimeResolutionOption selectedResolution = resolutionOptions[Mathf.Clamp(resolutionIndex, 0, resolutionOptions.Count - 1)];
        data.resolutionWidth = selectedResolution.width;
        data.resolutionHeight = selectedResolution.height;
        data.refreshRate = selectedResolution.refreshRate;
        int frameRateIndex = Mathf.Clamp(frameRateDropdown.value, 0, Mathf.Max(0, frameRateOptions.Count - 1));
        data.targetFrameRate = frameRateOptions[Mathf.Clamp(frameRateIndex, 0, frameRateOptions.Count - 1)];

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
        return titleFontOverride != null ? titleFontOverride : ResolveFallbackFont();
    }

    private Font ResolveBodyFont()
    {
        return bodyFontOverride != null ? bodyFontOverride : ResolveFallbackFont();
    }

    private Font ResolveLabelFont()
    {
        return labelFontOverride != null ? labelFontOverride : ResolveBodyFont();
    }

    private Font ResolveValueFont()
    {
        return valueFontOverride != null ? valueFontOverride : ResolveBodyFont();
    }

    private Font ResolveToggleStateFont()
    {
        if (toggleStateFontOverride != null)
            return toggleStateFontOverride;
        if (valueFontOverride != null)
            return valueFontOverride;
        return ResolveBodyFont();
    }

    private Font ResolveDropdownFont()
    {
        if (dropdownFontOverride != null)
            return dropdownFontOverride;
        if (valueFontOverride != null)
            return valueFontOverride;
        return ResolveBodyFont();
    }

    private Font ResolveButtonFont()
    {
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

    private static RectTransform CreateRect(string name, RectTransform parent, Vector2 anchorPivot, Vector2 anchoredPosition, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchorPivot;
        rect.anchorMax = anchorPivot;
        rect.pivot = anchorPivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
        return rect;
    }

    private static Text CreateText(string name, RectTransform parent, Vector2 anchoredPosition, Vector2 size, string value, int fontSize, Font font, TextAnchor alignment, Color color)
    {
        RectTransform rect = CreateRect(name, parent, new Vector2(0.5f, 1f), anchoredPosition, size);
        Text text = rect.gameObject.AddComponent<Text>();
        text.font = font;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = color;
        text.text = value;
        return text;
    }

    private static Slider CreateSlider(string name, RectTransform parent, Vector2 anchoredPosition, Vector2 size)
    {
        RectTransform root = CreateRect(name, parent, new Vector2(0.5f, 1f), anchoredPosition, size);

        Image background = root.gameObject.AddComponent<Image>();
        background.color = new Color(0.2f, 0.2f, 0.24f, 1f);

        GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(root, false);
        RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.offsetMin = new Vector2(8f, 4f);
        fillAreaRect.offsetMax = new Vector2(-8f, -4f);

        GameObject fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fill.transform.SetParent(fillArea.transform, false);
        RectTransform fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        fill.GetComponent<Image>().color = new Color(0.28f, 0.78f, 1f, 1f);

        GameObject handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
        handle.transform.SetParent(root, false);
        RectTransform handleRect = handle.GetComponent<RectTransform>();
        handleRect.sizeDelta = new Vector2(24f, 24f);
        handle.GetComponent<Image>().color = Color.white;

        Slider slider = root.gameObject.AddComponent<Slider>();
        slider.fillRect = fillRect;
        slider.handleRect = handleRect;
        slider.targetGraphic = handle.GetComponent<Image>();
        slider.direction = Slider.Direction.LeftToRight;
        return slider;
    }

    private static Toggle CreateToggle(string name, RectTransform parent, Vector2 anchoredPosition, Font font, out Text stateText)
    {
        RectTransform root = CreateRect(name, parent, new Vector2(0.5f, 1f), anchoredPosition, new Vector2(240f, 40f));

        GameObject background = new GameObject("Background", typeof(RectTransform), typeof(Image));
        background.transform.SetParent(root, false);
        RectTransform backgroundRect = background.GetComponent<RectTransform>();
        backgroundRect.anchorMin = new Vector2(0f, 0.5f);
        backgroundRect.anchorMax = new Vector2(0f, 0.5f);
        backgroundRect.pivot = new Vector2(0f, 0.5f);
        backgroundRect.anchoredPosition = Vector2.zero;
        backgroundRect.sizeDelta = new Vector2(30f, 30f);
        background.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.24f, 1f);

        GameObject checkmark = new GameObject("Checkmark", typeof(RectTransform), typeof(Image));
        checkmark.transform.SetParent(background.transform, false);
        RectTransform checkmarkRect = checkmark.GetComponent<RectTransform>();
        checkmarkRect.anchorMin = new Vector2(0.5f, 0.5f);
        checkmarkRect.anchorMax = new Vector2(0.5f, 0.5f);
        checkmarkRect.pivot = new Vector2(0.5f, 0.5f);
        checkmarkRect.sizeDelta = new Vector2(18f, 18f);
        checkmark.GetComponent<Image>().color = new Color(0.28f, 0.78f, 1f, 1f);

        stateText = CreateText("Label", root, new Vector2(120f, 0f), new Vector2(180f, 32f), "Enabled", 20, font, TextAnchor.MiddleLeft, new Color(0.9f, 0.9f, 0.9f, 1f));
        Text stateLabel = stateText;

        Toggle toggle = root.gameObject.AddComponent<Toggle>();
        toggle.targetGraphic = background.GetComponent<Image>();
        toggle.graphic = checkmark.GetComponent<Image>();
        toggle.onValueChanged.AddListener(isOn => stateLabel.text = isOn ? "Enabled" : "Disabled");
        return toggle;
    }

    private static Dropdown CreateDropdown(string name, RectTransform parent, Vector2 anchoredPosition, Vector2 size, Font font, out Text captionText, out Text itemText)
    {
        RectTransform root = CreateRect(name, parent, new Vector2(0.5f, 1f), anchoredPosition, size);
        Image background = root.gameObject.AddComponent<Image>();
        background.color = new Color(0.2f, 0.2f, 0.24f, 1f);

        Dropdown dropdown = root.gameObject.AddComponent<Dropdown>();
        dropdown.targetGraphic = background;

        captionText = CreateText("Label", root, Vector2.zero, new Vector2(size.x - 20f, size.y - 8f), string.Empty, 20, font, TextAnchor.MiddleLeft, Color.white);
        captionText.rectTransform.anchorMin = Vector2.zero;
        captionText.rectTransform.anchorMax = Vector2.one;
        captionText.rectTransform.offsetMin = new Vector2(12f, 4f);
        captionText.rectTransform.offsetMax = new Vector2(-12f, -4f);
        dropdown.captionText = captionText;

        GameObject template = new GameObject("Template", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
        template.transform.SetParent(root, false);
        template.SetActive(false);

        RectTransform templateRect = template.GetComponent<RectTransform>();
        templateRect.anchorMin = new Vector2(0f, 0f);
        templateRect.anchorMax = new Vector2(1f, 0f);
        templateRect.pivot = new Vector2(0.5f, 1f);
        templateRect.anchoredPosition = new Vector2(0f, -size.y);
        templateRect.sizeDelta = new Vector2(0f, 220f);

        Image templateImage = template.GetComponent<Image>();
        templateImage.color = new Color(0.12f, 0.12f, 0.15f, 1f);

        ScrollRect scrollRect = template.GetComponent<ScrollRect>();
        scrollRect.horizontal = false;

        GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
        viewport.transform.SetParent(template.transform, false);
        RectTransform viewportRect = viewport.GetComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = Vector2.zero;
        viewportRect.offsetMax = Vector2.zero;
        viewport.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.02f);
        viewport.GetComponent<Mask>().showMaskGraphic = false;

        GameObject content = new GameObject("Content", typeof(RectTransform));
        content.transform.SetParent(viewport.transform, false);
        RectTransform contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = new Vector2(0f, 28f);

        GameObject item = new GameObject("Item", typeof(RectTransform), typeof(Toggle));
        item.transform.SetParent(content.transform, false);
        RectTransform itemRect = item.GetComponent<RectTransform>();
        itemRect.anchorMin = new Vector2(0f, 1f);
        itemRect.anchorMax = new Vector2(1f, 1f);
        itemRect.pivot = new Vector2(0.5f, 1f);
        itemRect.sizeDelta = new Vector2(0f, 28f);

        Image itemBackground = item.AddComponent<Image>();
        itemBackground.color = new Color(0.18f, 0.18f, 0.22f, 1f);

        GameObject itemCheckmark = new GameObject("Item Checkmark", typeof(RectTransform), typeof(Image));
        itemCheckmark.transform.SetParent(item.transform, false);
        RectTransform itemCheckmarkRect = itemCheckmark.GetComponent<RectTransform>();
        itemCheckmarkRect.anchorMin = new Vector2(0f, 0.5f);
        itemCheckmarkRect.anchorMax = new Vector2(0f, 0.5f);
        itemCheckmarkRect.pivot = new Vector2(0f, 0.5f);
        itemCheckmarkRect.anchoredPosition = new Vector2(8f, 0f);
        itemCheckmarkRect.sizeDelta = new Vector2(16f, 16f);
        itemCheckmark.GetComponent<Image>().color = new Color(0.28f, 0.78f, 1f, 1f);

        itemText = CreateText("Item Label", itemRect, Vector2.zero, new Vector2(size.x - 40f, 28f), "Option", 18, font, TextAnchor.MiddleLeft, Color.white);
        itemText.rectTransform.anchorMin = Vector2.zero;
        itemText.rectTransform.anchorMax = Vector2.one;
        itemText.rectTransform.offsetMin = new Vector2(30f, 0f);
        itemText.rectTransform.offsetMax = new Vector2(-8f, 0f);

        Toggle itemToggle = item.GetComponent<Toggle>();
        itemToggle.targetGraphic = itemBackground;
        itemToggle.graphic = itemCheckmark.GetComponent<Image>();

        dropdown.template = templateRect;
        dropdown.itemText = itemText;
        dropdown.captionText = captionText;
        scrollRect.content = contentRect;
        scrollRect.viewport = viewportRect;

        return dropdown;
    }

    private static Button CreateButton(string name, RectTransform parent, Vector2 anchoredPosition, Vector2 size, string label, Font font, out Text buttonText)
    {
        RectTransform rect = CreateRect(name, parent, new Vector2(0.5f, 0f), anchoredPosition, size);
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = new Color(0.28f, 0.78f, 1f, 1f);

        Button button = rect.gameObject.AddComponent<Button>();

        buttonText = CreateText("Label", rect, Vector2.zero, size, label, 22, font, TextAnchor.MiddleCenter, Color.black);
        buttonText.rectTransform.anchorMin = Vector2.zero;
        buttonText.rectTransform.anchorMax = Vector2.one;
        buttonText.rectTransform.offsetMin = Vector2.zero;
        buttonText.rectTransform.offsetMax = Vector2.zero;
        return button;
    }
}
