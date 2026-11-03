using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed partial class MapBattleTilePreviewOverlay : MonoBehaviour
{
    private const int CanvasSortingOrder = 8010;
    private const float LandscapeAspectThreshold = 1.45f;
    private static readonly Vector2 ReferenceResolution = new Vector2(1920f, 1080f);
    private static readonly Vector2 LandscapePanelSize = new Vector2(600f, 810f);
    private static readonly Vector2 PortraitPanelSize = new Vector2(920f, 520f);

    [Header("Fonts")]
    [SerializeField] private TMP_FontAsset keywordFont;
    [SerializeField] private TMP_FontAsset bodyFont;
    [SerializeField] private TMP_FontAsset attributeFont;

    public event Action ConfirmRequested;
    public event Action CancelRequested;

    private MapBattleTilePreviewContext currentContext;
    private MapHUD mapHud;
    private bool interactionEnabled = true;
    private Vector2 cachedScreenSize;
    private Rect cachedSafeArea;
    private bool hasCachedLayout, isApplyingResponsiveLayout;

    public bool IsVisible => canvas != null && canvas.gameObject.activeInHierarchy;

    public void ConfigureFonts(TMP_FontAsset keyword, TMP_FontAsset body, TMP_FontAsset attribute)
    {
        if (keyword != null) keywordFont = keyword;
        if (body != null) bodyFont = body;
        if (attribute != null) attributeFont = attribute;
        ApplyFonts();
    }

    public void ConfigureMapHud(MapHUD hud)
    {
        if (mapHud == hud) return;
        if (mapHud != null && IsVisible) mapHud.SetBattlePreviewDetailsSuppressed(false);
        mapHud = hud;
        if (mapHud != null && IsVisible) mapHud.SetBattlePreviewDetailsSuppressed(true);
    }

    public void Show(MapBattleTilePreviewContext context)
    {
        EnsureUi();
        currentContext = context;
        interactionEnabled = true;
        gameObject.SetActive(true);
        canvas.gameObject.SetActive(true);
        SetDetailsSuppressed(true);
        ResetResponsiveLayoutCache();
        RefreshResponsiveLayoutIfChanged(GetCurrentScreenSize(), Screen.safeArea);
        RenderContext();
    }

    public void SetInteractionEnabled(bool enabled)
    {
        interactionEnabled = enabled;
        RefreshButtonStates();
    }

    public void ShowFailure(string message)
    {
        if (!IsVisible) return;
        interactionEnabled = true;
        statusText.text = VisibleReason(string.IsNullOrWhiteSpace(message) ? "战斗预览当前不可用。" : message);
        statusText.color = Failure;
        RefreshStatusLayout();
        RefreshButtonStates();
    }

    public void Hide()
    {
        currentContext = null;
        interactionEnabled = true;
        ResetResponsiveLayoutCache();
        if (statusText != null) { statusText.text = string.Empty; statusText.color = Quiet; }
        if (canvas != null) canvas.gameObject.SetActive(false);
        SetDetailsSuppressed(false);
    }

    public void ApplyResponsiveLayout(Vector2 screenSize) => ApplyResponsiveLayout(screenSize, new Rect(Vector2.zero, screenSize));
    public void ApplyResponsiveLayout() => ApplyResponsiveLayout(GetCurrentScreenSize(), Screen.safeArea);

    public void ApplyResponsiveLayout(Vector2 screenSize, Rect safeArea)
    {
        EnsureUi();
        NormalizeLayoutInputs(ref screenSize, ref safeArea);
        ApplyResponsiveLayoutInternal(screenSize, safeArea);
    }

    public bool RefreshResponsiveLayoutIfChanged(Vector2 screenSize, Rect safeArea)
    {
        if (!IsVisible || isApplyingResponsiveLayout) return false;
        NormalizeLayoutInputs(ref screenSize, ref safeArea);
        if (hasCachedLayout && cachedScreenSize == screenSize && cachedSafeArea == safeArea) return false;
        ApplyResponsiveLayoutInternal(screenSize, safeArea);
        return true;
    }

    private void ApplyResponsiveLayoutInternal(Vector2 screenSize, Rect safeArea)
    {
        if (isApplyingResponsiveLayout) return;
        isApplyingResponsiveLayout = true;
        cachedScreenSize = screenSize;
        cachedSafeArea = safeArea;
        hasCachedLayout = true;
        try { LayoutPanel(screenSize, safeArea); }
        finally { isApplyingResponsiveLayout = false; }
    }

    public void HandleCancelInput()
    {
        if (IsVisible && interactionEnabled) CancelRequested?.Invoke();
    }

    private void LateUpdate()
    {
        if (IsVisible) RefreshResponsiveLayoutIfChanged(GetCurrentScreenSize(), Screen.safeArea);
    }

    private void OnRectTransformDimensionsChange()
    {
        if (IsVisible) RefreshResponsiveLayoutIfChanged(GetCurrentScreenSize(), Screen.safeArea);
    }

    private void SetDetailsSuppressed(bool value)
    { if (mapHud != null) mapHud.SetBattlePreviewDetailsSuppressed(value); }

    private void OnDisable() { SetDetailsSuppressed(false); }

    private void OnDestroy()
    {
        SetDetailsSuppressed(false);
        if (confirmButton != null) confirmButton.onClick.RemoveListener(OnConfirmClicked);
        if (returnButton != null) returnButton.onClick.RemoveListener(OnReturnClicked);
    }

    private void RenderContext()
    {
        bool valid = IsValidContext(currentContext);
        MapTileType type = valid ? currentContext.tile.tileType : MapTileType.Battle;
        battleClassText.text = valid ? GetBattleClassLabel(type) : "战斗预览";
        classIcon.sprite = MapForestArt.Icon(type);
        classIcon.enabled = classIcon.sprite != null;
        if (valid)
        {
            BoardTileData tile = currentContext.tile;
            metadataText.text = $"{RegionName(tile.dangerBand)} · [{tile.coord.x},{tile.coord.y}]";
            explorationText.text = $"{(tile.isVisited ? "已探索" : "未探索")} · {(tile.isResolved ? "已解决" : "未解决")}";
            int danger = MapDangerRatingService.ClampRating(currentContext.dangerRating);
            dangerText.text = MapDangerRatingService.GetRatingPhrase(danger);
            dangerText.color = danger <= 1 ? new Color(0.66f, 0.86f, 0.68f)
                : danger <= 4 ? new Color(0.98f, 0.43f, 0.30f) : new Color(1f, 0.42f, 0.50f);
            ratingText.text = $"{danger}/8";
            SetAttributes(currentContext.attributeNames);
            statusText.text = currentContext.canConfirm ? string.Empty
                : VisibleReason(string.IsNullOrWhiteSpace(currentContext.blockReason) ? "当前无法进入战斗。" : currentContext.blockReason);
        }
        else
        {
            metadataText.text = explorationText.text = dangerText.text = ratingText.text = string.Empty;
            SetAttributes(Array.Empty<string>());
            statusText.text = "预览不可用：该格子当前无法进入战斗。";
        }
        // Cost remains in the context and commit flow; the approved button has no cost suffix.
        confirmLabelText.text = "进入战斗";
        returnLabelText.text = "返回地图";
        statusText.color = Quiet;
        ApplyFonts();
        LayoutContent();
        RefreshButtonStates();
    }

    private void SetAttributes(string[] names)
    {
        int count = 0;
        for (int i = 0; names != null && i < names.Length && count < attributeTexts.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(names[i])) continue;
            TextMeshProUGUI label = attributeTexts[count++];
            label.text = names[i].Trim();
            label.gameObject.SetActive(true);
        }
        for (int i = count; i < attributeTexts.Length; i++)
        { attributeTexts[i].text = string.Empty; attributeTexts[i].gameObject.SetActive(false); }
        attributeHeadingText.gameObject.SetActive(count > 0);
    }

    private void RefreshButtonStates()
    {
        if (confirmButton == null || returnButton == null) return;
        confirmButton.interactable = interactionEnabled && IsValidContext(currentContext) && currentContext.canConfirm;
        returnButton.interactable = interactionEnabled;
        confirmLabelText.color = confirmButton.interactable ? Ivory : Quiet;
        returnLabelText.color = returnButton.interactable ? Ivory : Quiet;
        confirmIcon.color = confirmButton.interactable ? Color.white : new Color(1f, 1f, 1f, 0.4f);
    }

    private static bool IsValidContext(MapBattleTilePreviewContext context) =>
        context != null && context.canPreview && context.tile != null && context.tile.IsHostileTile;

    private static string GetBattleClassLabel(MapTileType type) =>
        type == MapTileType.Elite ? "精英战斗" : type == MapTileType.Boss ? "首领战斗" : "普通战斗";

    private static string RegionName(MapDangerBand band)
    {
        switch (band)
        {
            case MapDangerBand.Outer: return "外围";
            case MapDangerBand.Mid: return "中层";
            case MapDangerBand.Inner: return "内层";
            case MapDangerBand.Core: return "核心";
            default: return "未知";
        }
    }

    private static string VisibleReason(string message)
    {
        // Action-point cost and insufficiency are deliberately absent from this preview.
        // Confirmation still uses the unmodified context and commit-time resource checks.
        if (string.IsNullOrEmpty(message)) return string.Empty;
        if (message.Contains("行动点") || message.Contains("行动资源") ||
            message.IndexOf("action point", StringComparison.OrdinalIgnoreCase) >= 0) return string.Empty;
        for (int at = message.IndexOf("AP", StringComparison.OrdinalIgnoreCase); at >= 0;
            at = message.IndexOf("AP", at + 2, StringComparison.OrdinalIgnoreCase))
            if ((at == 0 || !LatinWordCharacter(message[at - 1])) &&
                (at + 2 == message.Length || !LatinWordCharacter(message[at + 2]))) return string.Empty;
        return message;
    }

    private static bool LatinWordCharacter(char value) =>
        value >= 'a' && value <= 'z' || value >= 'A' && value <= 'Z' || value == '_';

    private static Vector2 GetCurrentScreenSize() => new Vector2(Screen.width, Screen.height);

    private static void NormalizeLayoutInputs(ref Vector2 size, ref Rect safe)
    {
        if (size.x <= 0f || size.y <= 0f) { size = ReferenceResolution; safe = new Rect(Vector2.zero, size); return; }
        float left = Mathf.Clamp(safe.xMin, 0f, size.x), right = Mathf.Clamp(safe.xMax, 0f, size.x);
        float bottom = Mathf.Clamp(safe.yMin, 0f, size.y), top = Mathf.Clamp(safe.yMax, 0f, size.y);
        safe = right > left && top > bottom ? Rect.MinMaxRect(left, bottom, right, top) : new Rect(Vector2.zero, size);
    }

    private void ResetResponsiveLayoutCache()
    { cachedScreenSize = Vector2.zero; cachedSafeArea = default; hasCachedLayout = false; }

    private void OnConfirmClicked() { if (confirmButton.interactable) ConfirmRequested?.Invoke(); }
    private void OnReturnClicked() { if (returnButton.interactable) CancelRequested?.Invoke(); }
}
