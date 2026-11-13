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
        HideOwnedInventory();
        ClearReforgeSelection();
        ResetPresentation();
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
        ChangeIllustration(eventData);
        BeginNarration(true);
    }

    public void Hide()
    {
        bool wasVisible = IsVisible;
        ResetPresentation();
        HideOwnedInventory();
        ClearReforgeSelection();
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
        titleText.text = currentEvent != null ? currentEvent.LocalizedName : GameText.Get("ui.event.title", "事件");
        contentText.text = currentEvent != null && !string.IsNullOrWhiteSpace(currentEvent.eventContent)
            ? currentEvent.Localize("body", currentEvent.eventContent) : GameText.Get("ui.event.empty", "暂无事件内容。");
        eventMetaText.text = GameText.Get("ui.event.encounter", "旅途事件");
        statusText.text = string.Empty;
        optionScrollRect.gameObject.SetActive(true);
        optionSection.gameObject.SetActive(true);
        consequenceTitleText.text = GameText.Get("ui.event.preview", "后果预览");
        SetButtonLabel(StoryText("confirm_choice", "确认选择", "Confirm choice"));
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
        UpdateReforgeAvailability();
        ResetScroll(optionScrollRect);
        layoutDirty = true;
    }

    private void SelectOption(MapEventOptionData option)
    {
        if (option != null && optionPreviewLookup.TryGetValue(option, out EventOptionPreviewData preview) && preview != null && !preview.isSelectable) return;
        selectedOption = option;
        previewedOption = option;
        statusText.text = string.Empty;
        consequenceTitleText.text = GameText.Get("ui.event.preview", "后果预览");
        RefreshOptionButtonVisuals();
        EventOptionPreviewData selectedPreview = null;
        if (option != null) optionPreviewLookup.TryGetValue(option, out selectedPreview);
        consequenceText.text = option != null ? FormatOptionDetail(option, selectedPreview) : canLeaveReforgeEvent
            ? GameText.Get("ui.event.reforge.no_materials", "背包里没有可以重铸的物品。") : GameText.Get("ui.event.no_choices", "当前事件暂无可选项。");
        SetConfirmButtonInteractable(canLeaveReforgeEvent || (option != null && (selectedPreview == null || selectedPreview.isSelectable)));
        UpdateChoiceButtonLabel();
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
        if (!CanReceiveStoryInput || executingSelection) return;
        if (!IsNarrationComplete) { AdvanceDialogue(); return; }
        if (isShowingResultStep)
        {
            if (pendingNextEvent != null)
            {
                if (nestedEventDepth >= MaxNestedEventDepth)
                {
                    pendingNextEvent = null;
                    contentText.text = GameText.Get("map.event.this_event_has_too_many_nested_branches", "事件嵌套层数过深，请检查事件配置。");
                    RefreshLayout();
                    BeginNarration(false);
                    return;
                }
                ShowFollowUpEvent(pendingNextEvent);
                return;
            }
            BeginClose();
            return;
        }
        if (!AreChoicesReady) return;
        RefreshOptionAvailability();
        if (canLeaveReforgeEvent) { BeginClose(); return; }
        if (selectedOption == null || selectionHandler == null || currentTile == null || currentEvent == null) return;
        if (!confirmButton.interactable) return;
        if (MapEventInventoryReforgeService.RequiresItem(selectedOption) && SelectedReforgeItem == null)
        {
            OpenReforgePicker(selectedOption);
            return;
        }
        executingSelection = true;
        try
        {
            using (MapFeedbackOrigin.FromText(consequenceText, confirmButton.transform as RectTransform))
            {
                if (selectionHandler.Invoke(currentTile, currentEvent, selectedOption, out string message, out string resultContent, out MapEvent_SO nextEventOverride))
                    ShowResultStep(selectedOption, message, resultContent, nextEventOverride);
                else
                {
                    string reason = string.IsNullOrWhiteSpace(message) ? GameText.Get("ui.event.choice_failed", "当前选项未能执行。") : message;
                    consequenceTitleText.text = StoryText("failed", "暂时无法执行", "Unable to proceed");
                    consequenceText.text = reason + "\n\n" + consequenceText.text;
                    ResetScroll(consequenceScrollRect);
                    layoutDirty = true;
                }
            }
        }
        finally { executingSelection = false; }
    }

    private void ShowResultStep(MapEventOptionData option, string message, string resultContent, MapEvent_SO nextEventOverride)
    {
        pendingNextEvent = nextEventOverride != null ? nextEventOverride : option?.nextEvent;
        if (option?.openFollowUpImmediately == true && pendingNextEvent != null && nestedEventDepth < MaxNestedEventDepth)
        {
            ShowFollowUpEvent(pendingNextEvent);
            return;
        }
        isShowingResultStep = true;
        optionScrollRect.gameObject.SetActive(false);
        optionSection.gameObject.SetActive(false);
        string result = !string.IsNullOrWhiteSpace(resultContent) ? resultContent
            : !string.IsNullOrWhiteSpace(option?.resultContent) ? currentEvent.LocalizeOption(option, "result", option.resultContent)
            : currentEvent.hideConsequences ? GameText.Get("ui.event.resolved", "事件已触发。")
            : !string.IsNullOrWhiteSpace(message) ? message : currentEvent.LocalizeOption(option, "preview", option?.consequenceDescription);
        contentText.text = string.IsNullOrWhiteSpace(result) ? GameText.Get("ui.event.resolved", "事件已触发。") : result;
        if (!string.IsNullOrWhiteSpace(message) && !contentText.text.Contains(message)) contentText.text += "\n\n" + message;
        eventMetaText.text = GameText.Get("ui.event.result", "事件结果");
        consequenceTitleText.text = GameText.Get("ui.event.outcome", "选择结果");
        consequenceText.text = !string.IsNullOrWhiteSpace(message) ? message : GameText.Get("ui.event.continue_hint", "请选择继续以收起事件界面。");
        statusText.text = string.Empty;
        SetButtonLabel(pendingNextEvent != null ? GameText.Get("ui.continue", "继续") : StoryText("finish", "结束事件", "Finish event"));
        SetConfirmButtonInteractable(true);
        ResetScroll(contentScrollRect);
        ResetScroll(consequenceScrollRect);
        layoutDirty = true;
        RefreshLayout();
        BeginNarration(false);
    }

    private void ShowFollowUpEvent(MapEvent_SO eventData)
    {
        ClearReforgeSelection();
        currentEvent = eventData;
        nestedEventDepth++;
        selectedOption = null;
        isShowingResultStep = false;
        pendingNextEvent = null;
        RefreshHeader();
        RefreshOptionList();
        SelectOption(GetFirstSelectableOption());
        RefreshLayout();
        ChangeIllustration(eventData);
        BeginNarration(false);
    }

    private string FormatOptionLabel(MapEventOptionData option, EventOptionPreviewData preview)
    {
        string label = currentEvent.LocalizeOption(option, "label", option.DisplayLabel);
        return preview != null && !preview.isSelectable ? label + GameText.Get("ui.event.unavailable", " [不可选]") : label;
    }

    private string FormatOptionDetail(MapEventOptionData option, EventOptionPreviewData preview)
    {
        if (currentEvent != null && currentEvent.hideConsequences) return string.Empty;
        string empty = GameText.Get("ui.event.no_preview", "暂无后果说明。");
        if (option == null) return empty;
        string detail = !string.IsNullOrWhiteSpace(option.consequenceDescription)
            ? currentEvent.LocalizeOption(option, "preview", option.consequenceDescription) : empty;
        if (option.requiredMoney > 0) detail = GameText.Format("map.event.cost_gold", "需要支付 {requiredMoney} 金钱\n{detail}", ("requiredMoney", option.requiredMoney), ("detail", detail));
        if (option.requiredMoneyPercent > 0) detail = GameText.Format("map.event.cost_of_your_gold", "需要支付当前金钱的 {requiredMoneyPercent}%\n{detail}", ("requiredMoneyPercent", option.requiredMoneyPercent), ("detail", detail));
        if (option.nextEvent != null && !option.openFollowUpImmediately) detail += GameText.Format("map.event.next_event", "\n继续事件：{localizedName}", ("localizedName", option.nextEvent.LocalizedName));
        if (option == reforgeMaterialOption && reforgeMaterial != null)
            detail += "\n\n" + GameText.Format("ui.event.reforge.selected", "已选材料：{name}\n点击此选项可更换。", ("name", MapEventInventoryReforgeService.ItemName(reforgeMaterial)));
        if (preview != null)
        {
            if (!string.IsNullOrWhiteSpace(preview.additionalDetail)) detail += "\n" + preview.additionalDetail;
            if (!preview.isSelectable && !string.IsNullOrWhiteSpace(preview.unavailableReason)) detail += "\n" + preview.unavailableReason;
        }
        return detail;
    }
}
