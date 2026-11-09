using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public partial class MapEventOverlay
{
    [Header("Story Presentation")]
    [SerializeField, Range(12f, 100f)] private float charactersPerSecond = 42f;
    private float presentationTime, openingAge, closingAge, choiceAge, characterBudget, nextCharacterDelay;
    private int pageIndex, pageEnd;
    private bool typing, closing, executingSelection, inventoryWasVisible;
    private bool previewPlayback;
    private string narrationSource;
    private bool AnimateStory => Application.isPlaying || previewPlayback;
    private MapEventOptionData previewedOption;
    private Vector2 choicePosition;
    private Vector2 previewPosition;
    public bool IsTyping => typing;
    public bool IsClosing => closing;
    public bool IsNarrationComplete => !typing && pageIndex >= Mathf.Max(0, contentText != null ? contentText.textInfo.pageCount - 1 : 0);
    public bool AreChoicesReady => !isShowingResultStep && IsNarrationComplete && choiceAge >= .24f && !closing;
    private bool StoryIsObscured => inventoryHud != null && inventoryHud.IsInventoryVisible || RunItemAcquisitionOverlay.IsBlockingInput;
    private bool CanReceiveStoryInput => IsVisible && !closing && !StoryIsObscured && (!AnimateStory || openingAge >= .2f);

    public void ConfigurePreviewPlayback(bool enabled) => previewPlayback = enabled;

    private static string StoryText(string key, string chinese, string english)
        => GameText.Get("ui.event.story." + key, GameText.LanguageCode == GameText.English ? english : chinese);

    private void ResetPresentation()
    {
        typing = closing = executingSelection = inventoryWasVisible = false;
        presentationTime = openingAge = closingAge = choiceAge = characterBudget = 0;
        previewedOption = null;
        narrationSource = null;
        if (presentationGroup != null) { presentationGroup.alpha = 1; presentationGroup.blocksRaycasts = true; }
        if (contentText != null) contentText.maxVisibleCharacters = int.MaxValue;
    }

    private void BeginNarration(bool opening)
    {
        narrationSource = contentText.text;
        if (opening) { openingAge = AnimateStory ? 0 : 1; presentationGroup.alpha = openingAge; }
        pageIndex = 0;
        choiceAge = 0;
        choiceGroup.alpha = previewGroup.alpha = 0;
        choiceGroup.interactable = choiceGroup.blocksRaycasts = false;
        contentText.pageToDisplay = 1;
        contentText.maxVisibleCharacters = int.MaxValue;
        contentText.ForceMeshUpdate(true, true);
        StartPage();
        if (!AnimateStory)
        {
            pageIndex = Mathf.Max(0, contentText.textInfo.pageCount - 1);
            contentText.pageToDisplay = 1;
            CompleteNarration();
        }
        UpdateStoryControls();
    }

    private void ReflowNarration(bool hadReadAll)
    {
        if (narrationSource == null || narrationSource != contentText.text) return;
        int count = contentText.textInfo.pageCount;
        if (count == 0) return;
        if (hadReadAll) { pageIndex = count - 1; contentText.maxVisibleCharacters = int.MaxValue; typing = false; }
        else
        {
            int visible = contentText.maxVisibleCharacters;
            pageIndex = 0;
            while (pageIndex + 1 < count && visible > contentText.textInfo.pageInfo[pageIndex].lastCharacterIndex + 1) pageIndex++;
            pageEnd = contentText.textInfo.pageInfo[pageIndex].lastCharacterIndex + 1;
            typing = visible < pageEnd;
        }
        contentText.pageToDisplay = pageIndex + 1;
        pageEnd = contentText.textInfo.pageInfo[pageIndex].lastCharacterIndex + 1;
        if (!IsNarrationComplete) choiceAge = 0;
        UpdateStoryControls();
    }

    private void StartPage()
    {
        contentText.pageToDisplay = pageIndex + 1;
        contentText.ForceMeshUpdate(true);
        int count = contentText.textInfo.pageCount;
        int first = count > 0 ? contentText.textInfo.pageInfo[Mathf.Min(pageIndex, count - 1)].firstCharacterIndex : 0;
        pageEnd = count > 0 ? contentText.textInfo.pageInfo[Mathf.Min(pageIndex, count - 1)].lastCharacterIndex + 1 : 0;
        contentText.maxVisibleCharacters = Mathf.Max(0, first);
        characterBudget = nextCharacterDelay = 0;
        typing = pageEnd > first;
        UpdateStoryControls();
    }

    /// <summary>Finishes only the current page. A second intentional action advances, so skip never executes a choice.</summary>
    public void AdvanceDialogue()
    {
        if (!CanReceiveStoryInput) return;
        if (typing) { typing = false; contentText.maxVisibleCharacters = pageEnd; UpdateStoryControls(); return; }
        if (pageIndex + 1 < contentText.textInfo.pageCount) { pageIndex++; StartPage(); return; }
        if (isShowingResultStep) OnConfirmClicked();
    }

    /// <summary>Used by deterministic editor captures and accessibility skip; rewards still require confirmation.</summary>
    public void CompleteNarration()
    {
        typing = false;
        pageIndex = Mathf.Max(0, contentText.textInfo.pageCount - 1);
        contentText.pageToDisplay = pageIndex + 1;
        contentText.maxVisibleCharacters = int.MaxValue;
        choiceAge = 1;
        UpdateStoryControls();
    }

    private void BeginClose()
    {
        if (!AnimateStory) { Hide(); return; }
        closing = true; closingAge = 0; typing = false;
        choiceGroup.interactable = false;
        // Keep swallowing input through the entire fade, until Hidden performs the scene handoff.
        presentationGroup.blocksRaycasts = true;
    }

    public void AdvancePresentation(float deltaTime)
    {
        if (!IsVisible || float.IsNaN(deltaTime) || float.IsInfinity(deltaTime)) return;
        float dt = Mathf.Max(0, deltaTime);
        presentationTime += dt;
        if (closing)
        {
            closingAge += dt;
            presentationGroup.alpha = 1 - Smooth(closingAge / .28f);
            footerRect.localScale = Vector3.one * Mathf.Lerp(1, .992f, Smooth(closingAge / .28f));
            if (closingAge >= .28f) Hide();
            return;
        }
        openingAge += dt;
        presentationGroup.alpha = Smooth(openingAge / .38f);
        footerRect.localScale = Vector3.one * Mathf.Lerp(.985f, 1, Smooth(openingAge / .42f));
        bool obscured = StoryIsObscured;
        if (inventoryWasVisible && !obscured) RefreshOptionAvailability();
        inventoryWasVisible = obscured;
        if (!obscured && openingAge >= .26f)
        {
            if (typing)
            {
                // Returning after a long frame must not consume a whole unread page at once.
                characterBudget += Mathf.Min(dt, .1f);
                int safety = 0;
                while (typing && characterBudget >= nextCharacterDelay && safety++ < 120)
                {
                    characterBudget -= nextCharacterDelay;
                    int index = contentText.maxVisibleCharacters++;
                    char c = index < contentText.textInfo.characterCount ? contentText.textInfo.characterInfo[index].character : ' ';
                    nextCharacterDelay = 1f / Mathf.Max(12, charactersPerSecond);
                    if ("。！？!?…".IndexOf(c) >= 0) nextCharacterDelay += .13f;
                    else if ("，、；,:;".IndexOf(c) >= 0) nextCharacterDelay += .06f;
                    if (contentText.maxVisibleCharacters >= pageEnd) typing = false;
                }
            }
            if (IsNarrationComplete) choiceAge += dt;
        }
        float reveal = IsNarrationComplete ? Smooth(choiceAge / .24f) : 0;
        choiceGroup.alpha = reveal;
        choiceGroup.interactable = CanReceiveStoryInput && reveal >= .99f;
        choiceGroup.blocksRaycasts = reveal >= .99f;
        optionSection.anchoredPosition = choicePosition + Vector2.right * ((1 - reveal) * 22);
        UpdatePreviewAnchor();
        previewRect.anchoredPosition = Vector2.Lerp(previewRect.anchoredPosition, previewPosition, 1 - Mathf.Exp(-24 * dt));
        previewGroup.alpha = currentEvent != null && currentEvent.hideConsequences || isShowingResultStep ? 0 : reveal;
        previewGroup.blocksRaycasts = previewGroup.alpha >= .99f;
        illustrationRect.localScale = Vector3.one * (1.025f + (1 - Smooth(openingAge / .75f)) * .02f + Mathf.Sin(presentationTime * .17f) * .003f);
        previousIllustration.color = new Color(1, 1, 1, 1 - Smooth(illustrationAge / .45f));
        illustrationAge += dt;
        continuationMark.color = new Color(Gold.r, Gold.g, Gold.b, .55f + Mathf.Sin(presentationTime * 3) * .25f);
        UpdateStoryControls();
    }

    private void UpdateStoryControls()
    {
        if (choiceGroup == null) return;
        bool ready = IsNarrationComplete;
        if (!AnimateStory)
        {
            choiceGroup.alpha = 1;
            choiceGroup.interactable = true;
            previewGroup.alpha = currentEvent != null && currentEvent.hideConsequences || isShowingResultStep ? 0 : 1;
        }
        bool valid = selectedOption != null && (!optionPreviewLookup.TryGetValue(selectedOption, out EventOptionPreviewData preview) || preview == null || preview.isSelectable);
        if (previewedOption != null && optionPreviewLookup.TryGetValue(previewedOption, out EventOptionPreviewData hovered) && hovered != null && !hovered.isSelectable) valid = false;
        SetConfirmButtonInteractable(CanReceiveStoryInput && ready && (isShowingResultStep || valid));
        confirmButton.gameObject.SetActive(ready && !isShowingResultStep);
        dialogueHint.text = typing ? StoryText("reveal", "点击显示本段", "Click to reveal")
            : !ready ? StoryText("next", "点击继续", "Click to continue")
            : isShowingResultStep ? (pendingNextEvent != null ? StoryText("next", "点击继续", "Click to continue") : StoryText("close", "点击结束事件", "Click to finish"))
            : StoryText("choose", "请选择行动", "Choose an action");
        speakerText.text = isShowingResultStep ? StoryText("outcome", "事件后续", "Aftermath") : StoryText("narrator", "旁白", "Narrator");
        continuationMark.gameObject.SetActive(!typing);
    }

    internal void PreviewOption(MapEventOptionData option)
    {
        if (!CanReceiveStoryInput || !AreChoicesReady || option == null) return;
        previewedOption = option;
        if (!optionPreviewLookup.TryGetValue(option, out EventOptionPreviewData preview) || preview == null || preview.isSelectable) SelectOption(option);
        else { consequenceText.text = FormatOptionDetail(option, preview); SetConfirmButtonInteractable(false); RefreshOptionButtonVisuals(); layoutDirty = true; }
    }

    private void RefreshOptionAvailability()
    {
        if (isShowingResultStep) return;
        foreach (OptionRow row in optionRows)
        {
            MapEventOptionData option = optionLookup[row.button];
            EventOptionPreviewData preview = previewHandler?.Invoke(currentTile, currentEvent, option);
            optionPreviewLookup[option] = preview;
            row.button.interactable = preview == null || preview.isSelectable;
            row.label.text = FormatOptionLabel(option, preview);
        }
        if (selectedOption != null)
        {
            optionPreviewLookup.TryGetValue(selectedOption, out EventOptionPreviewData preview);
            consequenceText.text = FormatOptionDetail(selectedOption, preview);
            SetConfirmButtonInteractable(preview == null || preview.isSelectable);
        }
        RefreshOptionButtonVisuals();
        layoutDirty = true;
    }

    private void Update()
    {
        if (!Application.isPlaying || !IsVisible) return;
        AdvancePresentation(Time.unscaledDeltaTime);
        if (!CanReceiveStoryInput) return;
        GameObject focus = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) && (focus == null || focus == dialogueButton.gameObject))
        {
            if (isShowingResultStep || !IsNarrationComplete) AdvanceDialogue();
            else if (AreChoicesReady) OnConfirmClicked();
        }
    }

    private static float Smooth(float value) => Mathf.SmoothStep(0, 1, Mathf.Clamp01(value));
}
