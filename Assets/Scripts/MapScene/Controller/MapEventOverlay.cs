using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public partial class MapEventOverlay : MonoBehaviour
{
    private const int MaxNestedEventDepth = MapEventOptionService.MaxNestedEventDepth;
    private const int EventOverlayCanvasSortingOrder = 9040;

    public sealed class EventOptionPreviewData
    {
        public bool isSelectable = true;
        public string additionalDetail;
        public string unavailableReason;
    }

    public delegate bool EventOptionSelectionHandler(BoardTileData tile, MapEvent_SO eventData, MapEventOptionData optionData, out string message, out string resultContent, out MapEvent_SO nextEventOverride);
    public delegate EventOptionPreviewData EventOptionPreviewHandler(BoardTileData tile, MapEvent_SO eventData, MapEventOptionData optionData);
    public event Action Hidden;

    [Header("Font")]
    [SerializeField] private TMP_FontAsset keywordFont;
    [SerializeField] private TMP_FontAsset bodyFont;
    [SerializeField] private int titleFontSize = 52;
    [SerializeField] private int bodyFontSize = 32;
    [SerializeField] private int buttonFontSize = 32;

    [Header("Panel")]
    [SerializeField] private Vector2 panelSize = new Vector2(1280f, 920f);
    [SerializeField] private Vector2 panelOffset = Vector2.zero;
    [SerializeField] private Color panelBackgroundColor = new Color(0.035f, 0.090f, 0.061f, 1f);
    [SerializeField] private Color backdropColor = new Color(0.005f, 0.012f, 0.009f, 0.64f);

    private BoardTileData currentTile;
    private MapEvent_SO currentEvent;
    private MapEventOptionData selectedOption;
    private EventOptionSelectionHandler selectionHandler;
    private EventOptionPreviewHandler previewHandler;
    private bool isShowingResultStep;
    private MapEvent_SO pendingNextEvent;
    private int nestedEventDepth;
    private readonly List<Button> optionButtons = new List<Button>();
    private readonly Dictionary<Button, MapEventOptionData> optionLookup = new Dictionary<Button, MapEventOptionData>();
    private readonly Dictionary<MapEventOptionData, EventOptionPreviewData> optionPreviewLookup = new Dictionary<MapEventOptionData, EventOptionPreviewData>();

    public bool IsVisible => canvas != null && canvas.gameObject.activeInHierarchy && panelRect != null && panelRect.gameObject.activeInHierarchy;

    public void ConfigureFonts(TMP_FontAsset keyword, TMP_FontAsset body)
    {
        if (keyword != null) keywordFont = keyword;
        if (body != null) bodyFont = body;
        ApplyFonts();
        layoutDirty = true;
    }

    public void Show(BoardTileData tile, MapEvent_SO eventData, EventOptionSelectionHandler onSelect, EventOptionPreviewHandler onPreview = null)
    {
        EnsureUi();
        currentTile = tile;
        currentEvent = eventData;
        selectionHandler = onSelect;
        previewHandler = onPreview;
        selectedOption = null;
        isShowingResultStep = false;
        pendingNextEvent = null;
        nestedEventDepth = 0;
        gameObject.SetActive(true);
        canvas.gameObject.SetActive(true);
        panelRect.gameObject.SetActive(true);
        RefreshHeader();
        RefreshOptionList();
        SelectOption(GetFirstSelectableOption());
        ApplyViewport(new Vector2(Screen.width, Screen.height), Screen.safeArea);
        RefreshLayout();
    }

    public void Hide()
    {
        bool wasVisible = IsVisible;
        isShowingResultStep = false;
        selectedOption = null;
        pendingNextEvent = null;
        nestedEventDepth = 0;
        previewHandler = null;
        optionPreviewLookup.Clear();
        if (panelRect != null) panelRect.gameObject.SetActive(false);
        if (canvas != null) canvas.gameObject.SetActive(false);
        if (wasVisible) Hidden?.Invoke();
    }

    private void RefreshHeader()
    {
        titleText.text = currentEvent != null ? currentEvent.DisplayName : "事件";
        contentText.text = currentEvent != null && !string.IsNullOrWhiteSpace(currentEvent.eventContent) ? currentEvent.eventContent : "暂无事件内容。";
        eventMetaText.text = "旅途事件";
        statusText.text = string.Empty;
        optionScrollRect.gameObject.SetActive(true);
        optionSection.gameObject.SetActive(true);
        consequenceTitleText.text = "后果预览";
        SetButtonLabel("确定");
        ResetScroll(contentScrollRect);
        layoutDirty = true;
    }

    private void RefreshOptionList()
    {
        foreach (Button button in optionButtons)
        {
            if (button == null) continue;
            button.gameObject.SetActive(false);
            if (Application.isPlaying) Destroy(button.gameObject);
            else DestroyImmediate(button.gameObject);
        }
        optionButtons.Clear();
        optionLookup.Clear();
        optionPreviewLookup.Clear();
        optionRows.Clear();
        IReadOnlyList<MapEventOptionData> options = currentEvent != null ? currentEvent.GetValidOptions() : Array.Empty<MapEventOptionData>();
        for (int i = 0; i < options.Count; i++)
        {
            MapEventOptionData option = options[i];
            if (option == null) continue;
            EventOptionPreviewData preview = previewHandler?.Invoke(currentTile, currentEvent, option);
            optionPreviewLookup[option] = preview;
            Button button = BuildOptionButton(option, preview, i + 1);
            optionButtons.Add(button);
            optionLookup[button] = option;
        }
        ResetScroll(optionScrollRect);
        layoutDirty = true;
    }

    private void SelectOption(MapEventOptionData option)
    {
        if (option != null && optionPreviewLookup.TryGetValue(option, out EventOptionPreviewData preview) && preview != null && !preview.isSelectable) return;
        selectedOption = option;
        RefreshOptionButtonVisuals();
        EventOptionPreviewData selectedPreview = null;
        if (option != null) optionPreviewLookup.TryGetValue(option, out selectedPreview);
        consequenceText.text = option != null ? FormatOptionDetail(option, selectedPreview) : "当前事件暂无可选项。";
        SetConfirmButtonInteractable(option != null && (selectedPreview == null || selectedPreview.isSelectable));
        ResetScroll(consequenceScrollRect);
        layoutDirty = true;
    }

    private MapEventOptionData GetFirstSelectableOption()
    {
        foreach (Button button in optionButtons)
            if (button.interactable && optionLookup.TryGetValue(button, out MapEventOptionData option)) return option;
        return null;
    }

    private void OnConfirmClicked()
    {
        if (isShowingResultStep)
        {
            if (pendingNextEvent != null)
            {
                if (nestedEventDepth >= MaxNestedEventDepth)
                {
                    statusText.text = "事件嵌套层数过深，请检查事件配置。";
                    layoutDirty = true;
                    return;
                }
                ShowFollowUpEvent(pendingNextEvent);
                return;
            }
            Hide();
            return;
        }
        if (selectedOption == null || selectionHandler == null || currentTile == null || currentEvent == null) return;
        if (selectionHandler.Invoke(currentTile, currentEvent, selectedOption, out string message, out string resultContent, out MapEvent_SO nextEventOverride))
            ShowResultStep(selectedOption, message, resultContent, nextEventOverride);
        else
        {
            statusText.text = string.IsNullOrWhiteSpace(message) ? "当前选项未能执行。" : message;
            layoutDirty = true;
        }
    }

    private void ShowResultStep(MapEventOptionData option, string message, string resultContent, MapEvent_SO nextEventOverride)
    {
        isShowingResultStep = true;
        pendingNextEvent = nextEventOverride != null ? nextEventOverride : option?.nextEvent;
        optionScrollRect.gameObject.SetActive(false);
        optionSection.gameObject.SetActive(false);
        string result = !string.IsNullOrWhiteSpace(resultContent) ? resultContent
            : !string.IsNullOrWhiteSpace(option?.resultContent) ? option.resultContent
            : !string.IsNullOrWhiteSpace(message) ? message : option?.consequenceDescription;
        contentText.text = string.IsNullOrWhiteSpace(result) ? "事件已触发。" : result;
        eventMetaText.text = "事件结果";
        consequenceTitleText.text = "选择结果";
        consequenceText.text = !string.IsNullOrWhiteSpace(message) ? message : "请选择继续以收起事件界面。";
        statusText.text = string.Empty;
        SetButtonLabel("继续");
        SetConfirmButtonInteractable(true);
        ResetScroll(contentScrollRect);
        ResetScroll(consequenceScrollRect);
        layoutDirty = true;
        RefreshLayout();
    }

    private void ShowFollowUpEvent(MapEvent_SO eventData)
    {
        currentEvent = eventData;
        nestedEventDepth++;
        selectedOption = null;
        isShowingResultStep = false;
        pendingNextEvent = null;
        RefreshHeader();
        RefreshOptionList();
        SelectOption(GetFirstSelectableOption());
        RefreshLayout();
    }

    private static string FormatOptionLabel(MapEventOptionData option, EventOptionPreviewData preview)
    {
        string label = option.DisplayLabel;
        return preview != null && !preview.isSelectable ? label + " [不可选]" : label;
    }

    private static string FormatOptionDetail(MapEventOptionData option, EventOptionPreviewData preview)
    {
        if (option == null) return "暂无后果说明。";
        string detail = !string.IsNullOrWhiteSpace(option.consequenceDescription) ? option.consequenceDescription : "暂无后果说明。";
        if (option.requiredMoney > 0) detail = $"需要支付 {option.requiredMoney} 金钱\n{detail}";
        if (option.requiredMoneyPercent > 0) detail = $"需要支付当前金钱的 {option.requiredMoneyPercent}%\n{detail}";
        if (option.nextEvent != null) detail += $"\n继续事件：{option.nextEvent.DisplayName}";
        if (preview != null)
        {
            if (!string.IsNullOrWhiteSpace(preview.additionalDetail)) detail += "\n" + preview.additionalDetail;
            if (!preview.isSelectable && !string.IsNullOrWhiteSpace(preview.unavailableReason)) detail += "\n" + preview.unavailableReason;
        }
        return detail;
    }
}
