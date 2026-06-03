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

    [Header("Panel")]
    [SerializeField] private Vector2 panelSize = new Vector2(640f, 650f);
    [SerializeField] private Color backdropColor = new Color(0f, 0f, 0f, 0.62f);
    [SerializeField] private Color panelColor = new Color(0.08f, 0.1f, 0.14f, 0.97f);
    [SerializeField] private Sprite panelBackgroundSprite;
    [SerializeField] private Vector2 panelPadding = new Vector2(44f, 40f);
    [SerializeField] private float panelSpacing = 22f;

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
    [SerializeField] private int titleFontSize = 42;
    [SerializeField] private int labelFontSize = 25;
    [SerializeField] private int valueFontSize = 21;
    [SerializeField] private int buttonFontSize = 25;
    [SerializeField] private int statusFontSize = 20;

    [Header("Colors")]
    [SerializeField] private Color titleColor = Color.white;
    [SerializeField] private Color labelColor = new Color(0.9f, 0.94f, 1f, 1f);
    [SerializeField] private Color valueColor = new Color(0.82f, 0.88f, 0.94f, 1f);
    [SerializeField] private Color statusColor = new Color(1f, 0.78f, 0.48f, 1f);
    [SerializeField] private Color buttonColor = new Color(0.18f, 0.24f, 0.34f, 1f);
    [SerializeField] private Color saveButtonColor = new Color(0.2f, 0.36f, 0.26f, 1f);
    [SerializeField] private Color abandonButtonColor = new Color(0.48f, 0.16f, 0.15f, 1f);
    [SerializeField] private Color buttonTextColor = Color.white;
    [SerializeField] private Color sliderBackgroundColor = new Color(0.16f, 0.17f, 0.2f, 1f);
    [SerializeField] private Color sliderFillColor = new Color(0.28f, 0.78f, 1f, 1f);
    [SerializeField] private Color sliderHandleColor = Color.white;

    [Header("Sprites")]
    [SerializeField] private Sprite buttonBackgroundSprite;
    [SerializeField] private Sprite saveButtonBackgroundSprite;
    [SerializeField] private Sprite abandonButtonBackgroundSprite;
    [SerializeField] private Sprite sliderBackgroundSprite;
    [SerializeField] private Sprite sliderFillSprite;
    [SerializeField] private Sprite sliderHandleSprite;

    [Header("Layout")]
    [SerializeField] private Vector2 buttonSize = new Vector2(430f, 58f);
    [SerializeField] private Vector2 sliderSize = new Vector2(330f, 24f);
    [SerializeField] private Vector2 sliderRowSize = new Vector2(500f, 52f);
    [SerializeField] private float buttonSpacing = 14f;

    private Canvas canvas;
    private RectTransform panelRect;
    private Slider musicSlider;
    private Slider sfxSlider;
    private Text musicValueText;
    private Text sfxValueText;
    private Text statusText;
    private Text abandonButtonText;
    private Func<bool> canOpenEvaluator;
    private Action saveAndReturnHandler;
    private Action abandonHandler;
    private bool ignoreSliderEvents;
    private bool awaitingAbandonConfirmation;

    public bool IsVisible => canvas != null && canvas.gameObject.activeSelf;

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

    public void Show()
    {
        EnsureUi();
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
            if (statusText != null)
                statusText.text = abandonWarningText;
            if (abandonButtonText != null)
                abandonButtonText.text = confirmAbandonLabel;
            return;
        }

        SaveCurrentSliderSettings();
        SetVisible(false);
        abandonHandler?.Invoke();
    }

    private void ResetAbandonConfirmation()
    {
        awaitingAbandonConfirmation = false;
        if (statusText != null)
            statusText.text = string.Empty;
        if (abandonButtonText != null)
            abandonButtonText.text = abandonLabel;
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
        MainMenuSettingsPersistence.Save(settings);
        MainMenuSettingsPersistence.Apply(settings);
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
        if (canvas != null)
            canvas.gameObject.SetActive(visible);
    }

    private void EnsureUi()
    {
        if (canvas != null)
            return;

        GameObject canvasGo = new GameObject("MapPauseMenuCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.transform.SetParent(transform, false);
        canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = canvasSortOrder;

        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = referenceResolution;
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform rootRect = canvasGo.GetComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        Image backdrop = CreateImage(rootRect, "Backdrop", backdropColor, null);
        RectTransform backdropRect = backdrop.rectTransform;
        backdropRect.anchorMin = Vector2.zero;
        backdropRect.anchorMax = Vector2.one;
        backdropRect.offsetMin = Vector2.zero;
        backdropRect.offsetMax = Vector2.zero;

        Image panelImage = CreateImage(rootRect, "Panel", panelColor, panelBackgroundSprite);
        panelRect = panelImage.rectTransform;
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = panelSize;

        VerticalLayoutGroup panelLayout = panelRect.gameObject.AddComponent<VerticalLayoutGroup>();
        panelLayout.padding = new RectOffset(
            Mathf.RoundToInt(panelPadding.x),
            Mathf.RoundToInt(panelPadding.x),
            Mathf.RoundToInt(panelPadding.y),
            Mathf.RoundToInt(panelPadding.y));
        panelLayout.spacing = panelSpacing;
        panelLayout.childAlignment = TextAnchor.UpperCenter;
        panelLayout.childControlWidth = false;
        panelLayout.childControlHeight = false;
        panelLayout.childForceExpandWidth = false;
        panelLayout.childForceExpandHeight = false;

        Text titleText = CreateText(panelRect, "Title", titleLabel, titleFontSize, ResolveTitleFont(), titleColor, TextAnchor.MiddleCenter);
        AddLayoutElement(titleText.rectTransform, panelSize.x - panelPadding.x * 2f, 72f);

        musicSlider = CreateSliderRow(musicLabel, out musicValueText);
        sfxSlider = CreateSliderRow(sfxLabel, out sfxValueText);

        statusText = CreateText(panelRect, "Status", string.Empty, statusFontSize, ResolveBodyFont(), statusColor, TextAnchor.MiddleCenter);
        AddLayoutElement(statusText.rectTransform, panelSize.x - panelPadding.x * 2f, 44f);

        RectTransform buttonsRoot = CreateRect("Buttons", panelRect, buttonSize);
        VerticalLayoutGroup buttonsLayout = buttonsRoot.gameObject.AddComponent<VerticalLayoutGroup>();
        buttonsLayout.spacing = buttonSpacing;
        buttonsLayout.childAlignment = TextAnchor.UpperCenter;
        buttonsLayout.childControlWidth = false;
        buttonsLayout.childControlHeight = false;
        buttonsLayout.childForceExpandWidth = false;
        buttonsLayout.childForceExpandHeight = false;
        AddLayoutElement(buttonsRoot, buttonSize.x, buttonSize.y * 4f + buttonSpacing * 3f);

        CreateButton(buttonsRoot, "ResumeButton", resumeLabel, buttonColor, buttonBackgroundSprite, Hide);
        CreateButton(buttonsRoot, "SaveAndReturnButton", saveAndReturnLabel, saveButtonColor, saveButtonBackgroundSprite, HandleSaveAndReturnClicked);
        Button abandonButton = CreateButton(buttonsRoot, "AbandonButton", abandonLabel, abandonButtonColor, abandonButtonBackgroundSprite, HandleAbandonClicked);
        abandonButtonText = abandonButton.GetComponentInChildren<Text>(true);
        CreateButton(buttonsRoot, "CloseButton", closeLabel, buttonColor, buttonBackgroundSprite, Hide);

        musicSlider.onValueChanged.AddListener(HandleSliderChanged);
        sfxSlider.onValueChanged.AddListener(HandleSliderChanged);
    }

    private Slider CreateSliderRow(string label, out Text valueText)
    {
        RectTransform row = CreateRect($"{label}Row", panelRect, sliderRowSize);
        HorizontalLayoutGroup rowLayout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        rowLayout.spacing = 16f;
        rowLayout.childAlignment = TextAnchor.MiddleCenter;
        rowLayout.childControlWidth = false;
        rowLayout.childControlHeight = false;
        rowLayout.childForceExpandWidth = false;
        rowLayout.childForceExpandHeight = false;
        AddLayoutElement(row, sliderRowSize.x, sliderRowSize.y);

        Text labelText = CreateText(row, "Label", label, labelFontSize, ResolveBodyFont(), labelColor, TextAnchor.MiddleLeft);
        AddLayoutElement(labelText.rectTransform, 88f, sliderRowSize.y);

        Slider slider = CreateSlider(row, $"{label}Slider", sliderSize);
        AddLayoutElement(slider.GetComponent<RectTransform>(), sliderSize.x, sliderRowSize.y);

        valueText = CreateText(row, "Value", "100%", valueFontSize, ResolveBodyFont(), valueColor, TextAnchor.MiddleRight);
        AddLayoutElement(valueText.rectTransform, 74f, sliderRowSize.y);

        return slider;
    }

    private Button CreateButton(Transform parent, string name, string label, Color color, Sprite sprite, Action onClick)
    {
        RectTransform rect = CreateRect(name, parent, buttonSize);
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        image.sprite = sprite;
        image.type = sprite != null ? Image.Type.Sliced : Image.Type.Simple;

        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(() => onClick?.Invoke());

        Text text = CreateText(rect, "Text", label, buttonFontSize, ResolveButtonFont(), buttonTextColor, TextAnchor.MiddleCenter);
        RectTransform textRect = text.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        text.raycastTarget = false;

        AddLayoutElement(rect, buttonSize.x, buttonSize.y);
        return button;
    }

    private Slider CreateSlider(Transform parent, string name, Vector2 size)
    {
        RectTransform root = CreateRect(name, parent, size);
        Image background = root.gameObject.AddComponent<Image>();
        background.color = sliderBackgroundColor;
        background.sprite = sliderBackgroundSprite;
        background.type = sliderBackgroundSprite != null ? Image.Type.Sliced : Image.Type.Simple;

        RectTransform fillArea = CreateRect("Fill Area", root, size);
        fillArea.anchorMin = Vector2.zero;
        fillArea.anchorMax = Vector2.one;
        fillArea.offsetMin = new Vector2(8f, 4f);
        fillArea.offsetMax = new Vector2(-8f, -4f);

        Image fillImage = CreateImage(fillArea, "Fill", sliderFillColor, sliderFillSprite);
        RectTransform fillRect = fillImage.rectTransform;
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        Image handleImage = CreateImage(root, "Handle", sliderHandleColor, sliderHandleSprite);
        RectTransform handleRect = handleImage.rectTransform;
        handleRect.anchorMin = new Vector2(0.5f, 0.5f);
        handleRect.anchorMax = new Vector2(0.5f, 0.5f);
        handleRect.pivot = new Vector2(0.5f, 0.5f);
        handleRect.sizeDelta = new Vector2(26f, 26f);

        Slider slider = root.gameObject.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.fillRect = fillRect;
        slider.handleRect = handleRect;
        slider.targetGraphic = handleImage;
        slider.direction = Slider.Direction.LeftToRight;
        return slider;
    }

    private static Image CreateImage(Transform parent, string name, Color color, Sprite sprite)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        Image image = go.GetComponent<Image>();
        image.color = color;
        image.sprite = sprite;
        image.type = sprite != null ? Image.Type.Sliced : Image.Type.Simple;
        return image;
    }

    private static RectTransform CreateRect(string name, Transform parent, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = size;
        return rect;
    }

    private static Text CreateText(Transform parent, string name, string value, int fontSize, Font font, Color color, TextAnchor alignment)
    {
        RectTransform rect = CreateRect(name, parent, Vector2.zero);
        Text text = rect.gameObject.AddComponent<Text>();
        text.font = font;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = color;
        text.text = value;
        return text;
    }

    private static void AddLayoutElement(RectTransform rect, float width, float height)
    {
        LayoutElement layoutElement = rect.gameObject.GetComponent<LayoutElement>();
        if (layoutElement == null)
            layoutElement = rect.gameObject.AddComponent<LayoutElement>();

        layoutElement.preferredWidth = width;
        layoutElement.minWidth = width;
        layoutElement.preferredHeight = height;
        layoutElement.minHeight = height;
    }

    private Font ResolveTitleFont()
    {
        return titleFont != null ? titleFont : ResolveFallbackFont();
    }

    private Font ResolveBodyFont()
    {
        return bodyFont != null ? bodyFont : ResolveFallbackFont();
    }

    private Font ResolveButtonFont()
    {
        if (buttonFont != null)
            return buttonFont;
        if (bodyFont != null)
            return bodyFont;
        return ResolveFallbackFont();
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
        DontDestroyOnLoad(eventSystemGo);
    }
}
