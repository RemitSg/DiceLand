using UnityEngine;
using UnityEngine.EventSystems;

public partial class BattleSkillInfoOverlay
{
    private bool closing, ownsInput;
    private float openProgress, switchProgress = 1, hoverProgress, hoverRefreshTimer, panelFit = 1;
    private GameObject previousSelection;
    private Vector2 viewportPixels;
    private Rect safePixels;

    private void OpenForestPanel()
    {
        bool visible = IsVisible;
        if (!visible && EventSystem.current != null) previousSelection = EventSystem.current.currentSelectedGameObject;
        HideEnemyHoverSkillInfo();
        closing = false; ownsInput = true; SpeedButton.InputSuppressed = true;
        panelGroup.interactable = true; panelGroup.blocksRaycasts = true;
        backdropButton.gameObject.SetActive(true); panelRoot.gameObject.SetActive(true);
        openProgress = !visible && Application.isPlaying ? 0 : 1;
        LayoutForestPanel(); ApplyViewport(new Vector2(Screen.width, Screen.height), Screen.safeArea); ApplySkillMotion();
    }

    public void HideImmediately(bool invokeCallback = true)
    {
        bool visible = IsVisible;
        closing = false; openProgress = 0;
        if (panelRoot != null) panelRoot.gameObject.SetActive(false);
        if (backdropButton != null) backdropButton.gameObject.SetActive(false);
        HideEnemyHoverSkillInfo();
        if (detailScrollRect != null) detailScrollRect.StopMovement();
        if (enemyProfileScroll != null) enemyProfileScroll.StopMovement();
        if (skillListScroll != null) skillListScroll.StopMovement();
        if (ownsInput) { SpeedButton.InputSuppressed = false; ownsInput = false; }
        currentAlly = null; currentEnemy = null; currentAllySourceButton = null; currentEnemySourceButton = null; displayedSkill = null;
        if (previousSelection != null && previousSelection.activeInHierarchy && EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(previousSelection);
        previousSelection = null;
        System.Action callback = hideCallback; hideCallback = null;
        if (invokeCallback) callback?.Invoke();
        if (visible) Hidden?.Invoke();
    }

    /// <summary>Unscaled animation also supports deterministic editor previews without entering Play Mode.</summary>
    public void AdvancePresentation(float delta)
    {
        delta = Mathf.Max(0, delta);
        if (IsVisible)
        {
            openProgress = Mathf.MoveTowards(openProgress, closing ? 0 : 1, delta / (closing ? 0.16f : 0.22f));
            switchProgress = Mathf.MoveTowards(switchProgress, 1, delta / 0.16f);
            ApplySkillMotion();
            if (closing && openProgress <= 0) HideImmediately();
        }
        if (currentHoverEnemy != null)
        {
            hoverProgress = Mathf.MoveTowards(hoverProgress, 1, delta / 0.12f); ApplyHoverTransform();
            hoverRefreshTimer += delta;
            if (hoverRefreshTimer >= 0.2f) { hoverRefreshTimer = 0; RefreshHover(false); }
        }
        else if (hoverPanelRoot != null && hoverPanelRoot.gameObject.activeSelf) HideEnemyHoverSkillInfo();
    }

    private void ApplySkillMotion()
    {
        if (panelRoot == null) return;
        float ease = 1 - Mathf.Pow(1 - openProgress, 3);
        panelGroup.alpha = ease;
        panelRoot.localScale = Vector3.one * (panelFit * Mathf.Lerp(0.965f, 1, ease));
        panelRoot.anchoredPosition = Vector2.down * ((1 - ease) * 16 * panelFit);
        if (shade != null) shade.color = new Color(0.004f, 0.012f, 0.008f, ease * 0.60f);
        if (contentGroup != null) contentGroup.alpha = Mathf.Lerp(0.72f, 1, 1 - Mathf.Pow(1 - switchProgress, 3));
        ApplyEnemyProfileMotion(ease);
    }

    public void ApplyViewport(Vector2 pixels, Rect safeArea)
    {
        if (safeRoot == null || pixels.x <= 0 || pixels.y <= 0) return;
        viewportPixels = pixels; safePixels = safeArea;
        safeRoot.anchorMin = new Vector2(Mathf.Clamp01(safeArea.xMin / pixels.x), Mathf.Clamp01(safeArea.yMin / pixels.y));
        safeRoot.anchorMax = new Vector2(Mathf.Clamp01(safeArea.xMax / pixels.x), Mathf.Clamp01(safeArea.yMax / pixels.y));
        safeRoot.offsetMin = safeRoot.offsetMax = Vector2.zero;
        Vector2 canvasSize = canvasRoot.rect.size;
        if (canvasSize.x < 1 || canvasSize.y < 1) canvasSize = new Vector2(1920, 1080);
        Vector2 available = Vector2.Scale(canvasSize, safeRoot.anchorMax - safeRoot.anchorMin);
        float horizontalMargin = viewMode == SkillViewMode.EnemyData ? 100 : 80;
        float verticalMargin = viewMode == SkillViewMode.EnemyData ? 140 : 96;
        panelFit = Mathf.Min(1, available.x / (Mathf.Max(1, frameState.width) + horizontalMargin), available.y / (Mathf.Max(1, frameState.height) + verticalMargin));
        ApplySkillMotion(); ApplyHoverTransform();
    }

    private void ApplyHoverTransform()
    {
        if (hoverPanelRoot == null) return;
        float ease = 1 - Mathf.Pow(1 - hoverProgress, 3);
        Vector2 available = Vector2.Scale(canvasRoot.rect.size, safeRoot.anchorMax - safeRoot.anchorMin);
        float scale = Mathf.Min(1, Mathf.Max(1, available.x) / (hoverPanelRoot.sizeDelta.x + 48), Mathf.Max(1, available.y) / (hoverPanelRoot.sizeDelta.y + 48));
        hoverPanelRoot.localScale = Vector3.one * scale;
        hoverPanelRoot.anchoredPosition = new Vector2(-28, -24 + (1 - ease) * 7);
        hoverGroup.alpha = ease;
    }
}
