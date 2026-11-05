using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class MapTutorialGuideOverlay : MonoBehaviour
{
    [SerializeField] private int canvasSortOrder = 9650;
    [SerializeField] private Font titleFont, bodyFont, buttonFont;
    private TutorialForestView view;
    private Canvas canvas;
    private RectTransform panelRoot;
    private Text titleText, bodyText, buttonText;
    private Button continueButton;
    private Action continueHandler;
    private float previousTimeScale = 1;
    private bool timeScalePaused, targetMode;
    private Rect activeScreenRect;
    public bool IsVisible => canvas != null && canvas.gameObject.activeInHierarchy;
    public bool IsModalVisible => IsVisible && panelRoot.gameObject.activeSelf;
    public bool IsTargetHighlightVisible => IsVisible && targetMode;
    public TutorialForestView Presentation => view;

    public void ConfigureFonts(Font title, Font body, Font button)
    {
        if (title != null) titleFont = title;
        if (body != null) bodyFont = body;
        if (button != null) buttonFont = button;
        view?.SetLegacyFonts(titleFont, bodyFont, buttonFont);
    }

    private void EnsureUi()
    {
        if (view != null) return;
        if (Application.isPlaying && EventSystem.current == null)
        {
            var events = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            DontDestroyOnLoad(events);
        }
        view = new TutorialForestView("MapTutorialGuideCanvas", transform, canvasSortOrder, false, false);
        canvas = view.Canvas; panelRoot = view.Panel;
        titleText = (Text)view.Title; bodyText = (Text)view.Body;
        buttonText = (Text)view.PrimaryLabel; continueButton = view.Primary;
        view.SetContext("地图探索"); view.SetLegacyFonts(titleFont, bodyFont, buttonFont);
        continueButton.onClick.AddListener(Continue);
    }

    public void Show(string title, string body, Action onContinue, string buttonLabel = "继续")
    {
        ShowFocused(title, body, onContinue, buttonLabel, null);
    }

    public void Show(string title, string body, Action onContinue, string buttonLabel, bool showHighlight, Vector2 highlightAnchor, Vector2 highlightSize)
    {
        ShowFocused(title, body, onContinue, buttonLabel, null);
        if (showHighlight) view.ShowLegacyHighlight(highlightAnchor, highlightSize);
    }

    public void ShowFocused(string title, string body, Action onContinue, string buttonLabel, Func<Rect?> target, Func<Rect?> keepClear = null, string context = "地图探索")
    {
        if (!gameObject.activeSelf) gameObject.SetActive(true);
        EnsureUi(); targetMode = false;
        titleText.text = string.IsNullOrWhiteSpace(title) ? "教程" : title;
        bodyText.text = title == "坐标与类型" && body != null
            ? body.Replace("坐标与类型", "<color=#E6BF68>坐标与类型</color>").Replace("“战斗”", "<color=#F19A86>“战斗”</color>") : body ?? string.Empty;
        buttonText.text = string.IsNullOrWhiteSpace(buttonLabel) ? "继续" : buttonLabel;
        continueHandler = onContinue;
        view.SetContext(context == "地图探索" ? GameText.Get("tutorial.map.context", "地图探索") : context);
        view.Show(target != null, target, keepClear);
        view.Reading.StopMovement(); view.Reading.verticalNormalizedPosition = 1;
        if (!timeScalePaused) { previousTimeScale = Time.timeScale; timeScalePaused = true; Time.timeScale = 0; }
    }

    public void ShowTargetHighlight(Rect screenRect)
    {
        if (!gameObject.activeSelf) gameObject.SetActive(true);
        EnsureUi(); bool already = IsTargetHighlightVisible; targetMode = true;
        continueHandler = null; activeScreenRect = screenRect;
        if (already) view.TrackTarget(ResolveStaticTarget);
        else view.ShowTarget(ResolveStaticTarget);
        RestoreTimeScale();
    }

    public void TrackTarget(Func<Rect?> provider) { EnsureUi(); view.TrackTarget(provider); }
    private Rect? ResolveStaticTarget() => activeScreenRect;

    private void Continue()
    {
        if (view == null || view.IsClosing || !IsModalVisible) return;
        Action callback = continueHandler; continueHandler = null;
        view.Close(() => { Hide(); callback?.Invoke(); });
    }

    public void Hide()
    {
        continueHandler = null; targetMode = false; view?.HideImmediately(); RestoreTimeScale();
    }

    private void RestoreTimeScale()
    {
        if (!timeScalePaused) return;
        Time.timeScale = previousTimeScale; timeScalePaused = false;
    }

    private void LateUpdate() { AdvancePresentation(Time.unscaledDeltaTime); }
    public void AdvancePresentation(float delta) { view?.Advance(delta); }
    private void OnDisable() { Hide(); }
    private void OnDestroy() { Hide(); view?.Dispose(); }
}
