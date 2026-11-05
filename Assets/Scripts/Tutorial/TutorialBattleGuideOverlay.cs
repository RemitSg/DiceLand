using System;
using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public class TutorialBattleGuideOverlay : MonoBehaviour
{
    [SerializeField] private TMP_FontAsset keywordFont, bodyFont;
    [SerializeField] private int canvasSortOrder = 9800;
    private TutorialForestView view;
    private Action continueCallback;
    private bool targetMode, timeScalePaused, ownsInput, previousSuppression;
    private float previousTimeScale = 1;
    private Rect activeTargetScreenRect;
    public bool IsVisible => view != null && view.Canvas != null && view.Canvas.gameObject.activeInHierarchy;
    public bool IsModalVisible => IsVisible && view.Panel.gameObject.activeSelf;
    public bool IsTargetHighlightVisible => IsVisible && targetMode;
    public TutorialForestView Presentation => view;

    private void EnsureUi()
    {
        if (view != null) return;
        view = new TutorialForestView("TutorialBattleGuideOverlay", transform, canvasSortOrder, true, false);
        view.SetContext(GameText.Get("tutorial.battle.context", "战斗教程")); view.SetTmpFonts(keywordFont, bodyFont);
        view.Primary.onClick.AddListener(Hide);
    }

    public void ConfigureFonts(TMP_FontAsset keyword, TMP_FontAsset body)
    {
        if (keyword != null) keywordFont = keyword;
        if (body != null) bodyFont = body;
        view?.SetTmpFonts(keywordFont, bodyFont);
    }

    public void Show(string title, string body, string buttonLabel, Action onContinue)
    {
        EnsureUi(); targetMode = false; continueCallback = onContinue;
        TutorialForestView.SetText(view.Title, string.IsNullOrWhiteSpace(title) ? "战斗教程" : title);
        TutorialForestView.SetText(view.Body, string.IsNullOrWhiteSpace(body) ? "暂无说明。" : body);
        TutorialForestView.SetText(view.PrimaryLabel, string.IsNullOrWhiteSpace(buttonLabel) ? "继续" : buttonLabel);
        view.Show(); view.Reading.StopMovement(); view.Reading.verticalNormalizedPosition = 1;
        if (!timeScalePaused) { previousTimeScale = Time.timeScale; timeScalePaused = true; Time.timeScale = 0; }
        if (!ownsInput) { previousSuppression = SpeedButton.InputSuppressed; ownsInput = true; SpeedButton.InputSuppressed = true; }
        SpeedButton.ClearTutorialAllowedScreenRect();
    }

    public void ShowTargetHighlight(Rect screenRect)
    {
        EnsureUi(); continueCallback = null; bool already = IsTargetHighlightVisible;
        targetMode = true; activeTargetScreenRect = screenRect;
        if (already) view.TrackTarget(ResolveStaticTarget);
        else view.ShowTarget(ResolveStaticTarget);
        Restore(); SpeedButton.SetTutorialAllowedScreenRect(screenRect);
    }

    public void TrackTarget(Func<Rect?> provider) { EnsureUi(); view.TrackTarget(provider); }
    private Rect? ResolveStaticTarget() => activeTargetScreenRect;
    public bool IsScreenPointInsideTarget(Vector2 screenPoint) => !IsTargetHighlightVisible || view.HasFocus && view.FocusScreenRect.Contains(screenPoint);
    public void Hide() { Hide(true); }
    public void Hide(bool invokeCallback)
    {
        if (invokeCallback && IsModalVisible && isActiveAndEnabled && Application.isPlaying)
        {
            view.Close(() => CompleteHide(true)); return;
        }
        CompleteHide(invokeCallback);
    }

    private void CompleteHide(bool invokeCallback)
    {
        Action callback = invokeCallback ? continueCallback : null;
        continueCallback = null; targetMode = false; view?.HideImmediately(); Restore();
        SpeedButton.ClearTutorialAllowedScreenRect(); callback?.Invoke();
    }

    private void Restore()
    {
        if (timeScalePaused) { Time.timeScale = previousTimeScale; timeScalePaused = false; }
        if (ownsInput) { SpeedButton.InputSuppressed = previousSuppression; ownsInput = false; }
    }

    private void LateUpdate() { AdvancePresentation(Time.unscaledDeltaTime); }
    public void AdvancePresentation(float delta)
    {
        view?.Advance(delta);
        if (IsTargetHighlightVisible) SpeedButton.SetTutorialAllowedScreenRect(view.HasFocus ? view.FocusScreenRect : Rect.zero);
    }
    private void OnDisable() { CompleteHide(false); }
    private void OnDestroy() { CompleteHide(false); view?.Dispose(); }
}
