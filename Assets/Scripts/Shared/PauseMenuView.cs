using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Shared forest presentation; map and battle controllers retain their own run actions.</summary>
public sealed class PauseMenuView : MonoBehaviour
{
    private static readonly Color Ivory = new Color32(239, 230, 205, 255);
    private static readonly Color Gold = new Color32(225, 191, 122, 255);
    private static readonly Color Muted = new Color32(185, 199, 169, 255);
    private readonly List<Text> labels = new List<Text>();
    private RectTransform safeRoot, plaque, audio, musicRow, sfxRow, footerRule, closeRect, closeXRect;
    private RectTransform resumeRect, settingsRect, utilityRect, abandonRect, statusRect, escapeBadge;
    private Text title, context, soundHeading, audioHint, footerHint;
    private Text resumeText, utilityText, closeText, musicLabel, sfxLabel;
    private CanvasGroup panelGroup;
    private Image backdrop;
    private GameObject previousSelection;
    private bool rememberedSelection;
    private Vector2 viewportSize;
    private Rect viewportSafeArea;
    private float openAmount = 1, panelScale = 1;
    private bool battle;

    public Canvas Canvas { get; private set; }
    public RectTransform Panel { get; private set; }
    public Slider MusicSlider { get; private set; }
    public Slider SfxSlider { get; private set; }
    public Text MusicValue { get; private set; }
    public Text SfxValue { get; private set; }
    public Text SettingsText { get; private set; }
    public Text AbandonText { get; private set; }
    public Text StatusText { get; private set; }

    public static PauseMenuView Create(Transform owner, string name, int sortOrder, Vector2 referenceResolution,
        bool inBattle, Action resume, Action settings, Action utility, Action abandon)
    {
        var root = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
        root.transform.SetParent(owner, false);
        root.SetActive(false);
        var view = root.AddComponent<PauseMenuView>();
        view.Canvas = root.GetComponent<Canvas>();
        view.Canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        view.Canvas.sortingOrder = sortOrder;
        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = referenceResolution;
        scaler.matchWidthOrHeight = .5f;
        view.battle = inBattle;
        view.Build(resume, settings, utility, abandon);
        return view;
    }

    private void Build(Action resume, Action settings, Action utility, Action abandon)
    {
        var root = (RectTransform)transform;
        backdrop = Rect("Backdrop", root).gameObject.AddComponent<Image>();
        Stretch(backdrop.rectTransform);
        backdrop.color = new Color(.005f, .015f, .009f, .72f);
        safeRoot = Rect("SafeArea", root); Stretch(safeRoot);
        Panel = Rect("Panel", safeRoot);
        Panel.anchorMin = Panel.anchorMax = Panel.pivot = Vector2.one * .5f;
        Panel.gameObject.AddComponent<PauseMenuGraphic>().part = PauseMenuGraphic.Part.Window;
        panelGroup = Panel.gameObject.AddComponent<CanvasGroup>();

        plaque = Rect("TitlePlaque", Panel); plaque.gameObject.AddComponent<PauseMenuGraphic>().part = PauseMenuGraphic.Part.Title;
        title = Label("Title", plaque, 54, Gold, TextAnchor.MiddleCenter); Stretch(title.rectTransform, 48, 3);
        title.fontStyle = FontStyle.Bold;
        context = Label("Context", Panel, 28, Muted, TextAnchor.MiddleCenter);
        closeXRect = Button("CloseXButton", Panel, "×", resume, out _, false);

        audio = Rect("AudioSection", Panel); audio.gameObject.AddComponent<PauseMenuGraphic>().part = PauseMenuGraphic.Part.AudioPanel;
        var speaker = Rect("Speaker", audio); Skin(speaker, MainMenuSettingsGraphic.Part.Speaker); Place(speaker, 28, 27, 34, 34);
        soundHeading = Label("SoundHeading", audio, 38, Gold); Place(soundHeading.rectTransform, 78, 18, 280, 52);
        musicRow = AudioRow("MusicRow", audio, out var music, out var musicValue, out musicLabel);
        MusicSlider = music; MusicValue = musicValue;
        sfxRow = AudioRow("SfxRow", audio, out var sfx, out var sfxValue, out sfxLabel);
        SfxSlider = sfx; SfxValue = sfxValue;
        audioHint = Label("AudioHint", audio, 26, Muted, TextAnchor.MiddleCenter);

        resumeRect = Button("ResumeButton", Panel, string.Empty, resume, out resumeText, true, PauseMenuGraphic.Part.Play);
        settingsRect = Button("SettingsButton", Panel, string.Empty, settings, out var settingsLabel, false, PauseMenuGraphic.Part.Settings);
        SettingsText = settingsLabel;
        utilityRect = Button(battle ? "RetryButton" : "SaveAndReturnButton", Panel, string.Empty, utility, out utilityText,
            false, battle ? PauseMenuGraphic.Part.Retry : PauseMenuGraphic.Part.Save);
        abandonRect = Button("AbandonButton", Panel, string.Empty, abandon, out var abandonLabel, false, PauseMenuGraphic.Part.Leave, true);
        AbandonText = abandonLabel;

        statusRect = Rect("StatusWell", Panel);
        StatusText = Label("Status", statusRect, 28, Muted, TextAnchor.UpperLeft);
        Stretch(StatusText.rectTransform, 4, 4);
        footerRule = Rect("FooterRule", Panel);
        footerRule.gameObject.AddComponent<MapMerchantFrameGraphic>().style = MapMerchantFrameGraphic.Style.Divider;
        escapeBadge = Rect("EscapeBadge", Panel); Skin(escapeBadge, MainMenuSettingsGraphic.Part.Field);
        var escape = Label("EscapeKey", escapeBadge, 26, Gold, TextAnchor.MiddleCenter); escape.text = "ESC"; Stretch(escape.rectTransform);
        footerHint = Label("FooterHint", Panel, 28, Muted);
        closeRect = Button("CloseButton", Panel, string.Empty, resume, out closeText, false);
        foreach (var graphic in Panel.GetComponentsInChildren<Graphic>())
            if (graphic.GetComponent<Button>() == null && graphic.GetComponent<Slider>() == null && graphic.name != "Handle") graphic.raycastTarget = false;
    }

    public void ConfigureFonts(Font heading, Font body, Font button)
    {
        Font fallback = body != null ? body : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        foreach (Text label in labels) label.font = fallback;
        title.font = heading != null ? heading : fallback;
        foreach (Text label in new[] { resumeText, SettingsText, utilityText, AbandonText, closeText })
            label.font = button != null ? button : fallback;
    }

    public void RefreshLabels(string contextLabel, string resume, string utility, string abandon, string close, string music, string sfx)
    {
        title.text = GameText.Get("ui.pause.title", "旅途暂歇");
        context.text = contextLabel;
        soundHeading.text = GameText.Get("ui.settings.sound", "声音");
        audioHint.text = GameText.Get("ui.pause.audio_hint", "音量调整即时生效");
        footerHint.text = GameText.Get("ui.pause.return_hint", "返回游戏");
        resumeText.text = resume;
        SettingsText.text = GameText.Get("ui.menu.settings", "设置");
        utilityText.text = utility;
        AbandonText.text = abandon;
        closeText.text = close;
        musicLabel.text = music;
        sfxLabel.text = sfx;
        SetWarning(false);
    }

    public void SetWarning(bool warning)
    {
        StatusText.color = warning ? new Color32(243, 180, 134, 255) : Muted;
        if (!warning)
            StatusText.text = battle ? GameText.Get("ui.pause.retry_hint", "重试将重新开始当前战斗。")
                : GameText.Get("ui.pause.save_hint", "保存后，可从主菜单继续本次旅途。");
    }

    public void SetVisible(bool visible)
    {
        if (visible && !Canvas.gameObject.activeSelf)
        {
            previousSelection = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            rememberedSelection = true;
            openAmount = Application.isPlaying ? 0 : 1;
        }
        Canvas.gameObject.SetActive(visible);
        if (!visible) RestoreSelection();
        if (visible)
        {
            ApplyViewport(new Vector2(Screen.width, Screen.height), Screen.safeArea);
            ApplyMotion();
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(resumeRect.gameObject);
        }
    }

    private void OnDisable() => RestoreSelection();

    private void RestoreSelection()
    {
        if (rememberedSelection && EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(previousSelection != null && previousSelection.activeInHierarchy ? previousSelection : null);
        previousSelection = null;
        rememberedSelection = false;
    }

    private void LateUpdate()
    {
        if (viewportSize != new Vector2(Screen.width, Screen.height) || viewportSafeArea != Screen.safeArea)
            ApplyViewport(new Vector2(Screen.width, Screen.height), Screen.safeArea);
        if (openAmount < 1)
        {
            openAmount = Mathf.MoveTowards(openAmount, 1, Time.unscaledDeltaTime / .18f);
            ApplyMotion();
        }
    }

    private void ApplyMotion()
    {
        float ease = 1 - Mathf.Pow(1 - openAmount, 3);
        panelGroup.alpha = ease;
        Panel.localScale = Vector3.one * panelScale * Mathf.Lerp(.965f, 1, ease);
        backdrop.color = new Color(.005f, .015f, .009f, .72f * ease);
    }

    public void ApplyViewport(Vector2 size, Rect safeArea)
    {
        viewportSize = size; viewportSafeArea = safeArea;
        if (safeArea.width <= 0 || safeArea.height <= 0) safeArea = new Rect(Vector2.zero, size);
        Vector2 full = new Vector2(Mathf.Max(1, size.x), Mathf.Max(1, size.y));
        safeRoot.anchorMin = safeArea.min / full; safeRoot.anchorMax = safeArea.max / full;
        safeRoot.offsetMin = safeRoot.offsetMax = Vector2.zero;
        Canvas.ForceUpdateCanvases();
        bool stacked = safeRoot.rect.width < safeRoot.rect.height * 1.1f;
        float width = stacked ? 720 : 1140, height = stacked ? 1024 : 724;
        Panel.sizeDelta = new Vector2(width, height);
        panelScale = Mathf.Min(1, safeRoot.rect.width / (width + 64), safeRoot.rect.height / (height + 88));
        Place(plaque, (width - 520) / 2, -30, 520, 88);
        Place(context.rectTransform, 100, 65, width - 200, 40);
        Place(closeXRect, width - 90, 26, 56, 56);
        Place(audio, 42, 118, stacked ? 636 : 410, stacked ? 278 : 458);
        float rowWidth = stacked ? 278 : 346;
        Place(musicRow, 32, 96, rowWidth, 112);
        Place(sfxRow, stacked ? 326 : 32, stacked ? 96 : 246, rowWidth, 112);
        LayoutAudioRow(musicRow, rowWidth, stacked); LayoutAudioRow(sfxRow, rowWidth, stacked);
        Place(audioHint.rectTransform, 20, stacked ? 216 : 374, stacked ? 596 : 370, stacked ? 52 : 64);

        float x = stacked ? 42 : 492, actionWidth = stacked ? 636 : 606;
        float top = stacked ? 424 : 118;
        RectTransform[] actions = { resumeRect, settingsRect, utilityRect, abandonRect };
        for (int i = 0; i < actions.Length; i++)
            Place(actions[i], x, top + i * 100, actionWidth, 84);
        Place(statusRect, x + 6, top + 400, actionWidth - 12, 92);
        float footerY = stacked ? 912 : 616;
        Place(footerRule, 42, footerY, width - 84, 10);
        Place(escapeBadge, 48, footerY + 33, 80, 44);
        Place(footerHint.rectTransform, 146, footerY + 32, width - 384, 46);
        Place(closeRect, width - 224, footerY + 24, 176, 58);
        ApplyMotion();
    }

    private RectTransform AudioRow(string name, RectTransform parent, out Slider slider, out Text value, out Text label)
    {
        RectTransform row = Rect(name, parent);
        label = Label("Label", row, 34, Ivory);
        RectTransform badge = Rect("ValueBadge", row); Skin(badge, MainMenuSettingsGraphic.Part.Field);
        value = Label("Value", badge, 30, Gold, TextAnchor.MiddleCenter); Stretch(value.rectTransform, 4, 1);
        RectTransform hit = Rect(name == "MusicRow" ? "MusicSlider" : "SfxSlider", row);
        hit.gameObject.AddComponent<Image>().color = Color.clear;
        RectTransform track = Rect("Track", hit); Stretch(track, 0, 18); Skin(track, MainMenuSettingsGraphic.Part.Track);
        RectTransform fillArea = Rect("FillArea", hit); Stretch(fillArea, 22, 21);
        RectTransform fill = Rect("Fill", fillArea); Stretch(fill); Skin(fill, MainMenuSettingsGraphic.Part.Fill);
        RectTransform handleArea = Rect("HandleArea", hit); Stretch(handleArea, 22, 0);
        RectTransform handle = Rect("Handle", handleArea); handle.sizeDelta = new Vector2(44, 0);
        MainMenuSettingsGraphic grip = Skin(handle, MainMenuSettingsGraphic.Part.Grip); grip.raycastTarget = true;
        slider = hit.gameObject.AddComponent<Slider>();
        slider.transition = Selectable.Transition.None; slider.minValue = 0; slider.maxValue = 1;
        slider.fillRect = fill; slider.handleRect = handle; slider.targetGraphic = grip;
        hit.gameObject.AddComponent<MainMenuSettingsControl>().Bind(slider, grip, handle);
        return row;
    }

    private static void LayoutAudioRow(RectTransform row, float width, bool compact)
    {
        Place((RectTransform)row.Find("Label"), 0, 0, compact ? width : width - 112, 48);
        Place((RectTransform)row.Find("ValueBadge"), width - 96, compact ? 60 : 0, 96, 44);
        Place((RectTransform)row.GetComponentInChildren<Slider>().transform, 0, 60, compact ? width - 112 : width, 48);
    }

    private RectTransform Button(string name, Transform parent, string caption, Action action, out Text text,
        bool primary, PauseMenuGraphic.Part? icon = null, bool danger = false)
    {
        RectTransform root = Rect(name, parent);
        var face = root.gameObject.AddComponent<PauseMenuGraphic>();
        face.part = danger ? PauseMenuGraphic.Part.Danger : primary ? PauseMenuGraphic.Part.PrimaryButton
            : name == "SaveAndReturnButton" ? PauseMenuGraphic.Part.SaveButton : PauseMenuGraphic.Part.SecondaryButton;
        face.raycastTarget = true;
        var button = root.gameObject.AddComponent<Button>(); button.transition = Selectable.Transition.None; button.targetGraphic = face;
        button.onClick.AddListener(() => action?.Invoke());
        root.gameObject.AddComponent<MapCraftingButtonFeedback>().scaleFeedback = false;
        text = Label("Text", root, icon.HasValue ? 42 : 34, danger ? new Color32(244, 207, 177, 255) : Ivory, TextAnchor.MiddleCenter);
        if (icon.HasValue) text.resizeTextMinSize = 36;
        text.text = caption; Stretch(text.rectTransform, icon.HasValue ? 64 : 12, 4);
        text.fontStyle = FontStyle.Bold;
        if (icon.HasValue)
        {
            RectTransform mark = Rect("Icon", root);
            mark.anchorMin = mark.anchorMax = new Vector2(0, .5f); mark.pivot = Vector2.one * .5f;
            mark.anchoredPosition = new Vector2(38, 0); mark.sizeDelta = new Vector2(38, 38);
            mark.gameObject.AddComponent<PauseMenuGraphic>().part = icon.Value;
        }
        return root;
    }

    private Text Label(string name, Transform parent, int size, Color tint, TextAnchor alignment = TextAnchor.MiddleLeft)
    {
        var text = Rect(name, parent).gameObject.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); text.fontSize = size;
        text.color = tint; text.alignment = alignment; text.raycastTarget = false;
        text.resizeTextForBestFit = true; text.resizeTextMinSize = Mathf.Min(24, size); text.resizeTextMaxSize = size;
        labels.Add(text); return text;
    }

    private static MainMenuSettingsGraphic Skin(RectTransform rect, MainMenuSettingsGraphic.Part part)
    {
        var face = rect.gameObject.AddComponent<MainMenuSettingsGraphic>(); face.part = part; face.raycastTarget = false; return face;
    }

    private static RectTransform Rect(string name, Transform parent)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); rect.SetParent(parent, false); return rect;
    }

    private static void Stretch(RectTransform rect, float horizontal = 0, float vertical = 0)
    {
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(horizontal, vertical); rect.offsetMax = new Vector2(-horizontal, -vertical);
    }

    private static void Place(RectTransform rect, float x, float y, float width, float height)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(x, -y); rect.sizeDelta = new Vector2(width, height);
    }
}
