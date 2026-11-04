using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public partial class MapTreasureOverlay : MonoBehaviour
{
    public delegate bool TreasureOptionSelectionHandler(BoardTileData tile, TreasureRewardChoice choice, out TreasureRewardResult result);
    public delegate bool RelicChoiceConfirmHandler(BoardTileData tile, BattleSettlementRelicChoiceData choiceData, out string message);
    public delegate bool DirectRelicChoiceConfirmHandler(BattleSettlementRelicChoiceData choiceData, out string message);
    public event Action Hidden;

    [Header("Font")]
    [SerializeField] private TMP_FontAsset keywordFont;
    [SerializeField] private TMP_FontAsset bodyFont;
    [SerializeField] private int titleFontSize = 38;
    [SerializeField] private int bodyFontSize = 24;
    [SerializeField] private int buttonFontSize = 24;

    [Header("Panel")]
    [SerializeField] private Vector2 panelSize = new Vector2(1480f, 830f);
    [SerializeField] private Vector2 panelOffset = Vector2.zero;
    [SerializeField] private Sprite panelBackgroundSprite;
    [SerializeField] private Color backdropColor = new Color(0f, 0f, 0f, 0.58f);
    [SerializeField] private Color panelColor = new Color(0.1f, 0.08f, 0.05f, 0.96f);
    [SerializeField] private Color accentColor = new Color(1f, 0.78f, 0.28f, 1f);
    [SerializeField] private Color textColor = new Color(0.96f, 0.94f, 0.86f, 1f);

    [Header("Cards")]
    [SerializeField] private Vector2 cardSize = new Vector2(396f, 566f);
    [SerializeField] private float cardSpacing = 32f;
    [SerializeField] private Sprite optionBackgroundSprite;
    [SerializeField] private Sprite disabledOptionBackgroundSprite;
    [SerializeField] private Color cardColor = new Color(0.2f, 0.15f, 0.08f, 0.96f);
    [SerializeField] private Color disabledCardColor = new Color(0.18f, 0.18f, 0.18f, 0.92f);

    [Header("Reward Icons")]
    [SerializeField] private Sprite moneyIcon;
    [SerializeField] private Sprite relicBagIcon;
    [SerializeField] private Sprite relicBagCommonIcon;
    [SerializeField] private Sprite relicBagUncommonIcon;
    [SerializeField] private Sprite relicBagRareIcon;
    [SerializeField] private Sprite relicBagEpicIcon;
    [SerializeField] private Sprite relicBagLegendaryIcon;
    [SerializeField] private Vector2 rewardIconSize = new Vector2(96f, 96f);
    [SerializeField] private Vector2 rewardIconOffset = new Vector2(0f, -86f);
    [SerializeField] private Color rewardIconColor = Color.white;

    [Header("Buttons")]
    [SerializeField] private Vector2 buttonSize = new Vector2(290f, 66f);
    [SerializeField] private Color buttonColor = new Color(1f, 0.78f, 0.28f, 1f);
    [SerializeField] private Color buttonTextColor = Color.black;
    [SerializeField] private Color disabledButtonColor = new Color(0.38f, 0.38f, 0.38f, 0.95f);
    [SerializeField] private string chooseLabel = "\u9886\u53d6";
    [SerializeField] private string continueLabel = "\u7ee7\u7eed";

    private Canvas canvas;
    private RectTransform root;
    private RectTransform panel;
    private Image panelImage;
    private RectTransform cardRoot;
    private TextMeshProUGUI titleText;
    private TextMeshProUGUI subtitleText;
    private TextMeshProUGUI resultText;
    private Button continueButton;
    private readonly List<GameObject> spawnedCards = new List<GameObject>();
    private Button confirmRelicChoiceButton;
    private Button refreshRelicChoiceButton;

    private BoardTileData currentTile;
    private TreasureOptionSelectionHandler selectionHandler;
    private RelicChoiceConfirmHandler relicChoiceConfirmHandler;
    private DirectRelicChoiceConfirmHandler directRelicChoiceConfirmHandler;
    private BattleSettlementRelicChoiceData activeRelicChoiceData;
    private bool isShowingResult;
    private bool handlingAction;
    private bool closeAfterRelicConfirm, requireDeselectForRefresh;
    private IReadOnlyList<TreasureRewardChoice> activeChoices;

    public bool IsVisible => canvas != null && canvas.gameObject.activeInHierarchy && panel != null && panel.gameObject.activeInHierarchy;

    public void ConfigureFonts(TMP_FontAsset keyword, TMP_FontAsset body)
    {
        if (keyword != null)
            keywordFont = keyword;
        if (body != null)
            bodyFont = body;

        ApplyFonts();
    }

    public void ConfigureBackgroundSprites(Sprite panelSprite, Sprite optionSprite, Sprite disabledOptionSprite = null)
    {
        if (panelSprite != null)
            panelBackgroundSprite = panelSprite;
        if (optionSprite != null)
            optionBackgroundSprite = optionSprite;
        if (disabledOptionSprite != null)
            disabledOptionBackgroundSprite = disabledOptionSprite;

        ApplyPanelBackground();
    }

    public void ConfigureRewardIcons(Sprite moneyRewardIcon, Sprite relicBagRewardIcon)
    {
        if (moneyRewardIcon != null)
            moneyIcon = moneyRewardIcon;
        if (relicBagRewardIcon != null)
            relicBagIcon = relicBagRewardIcon;
    }

    public void ConfigureRelicBagIcons(
        Sprite commonIcon,
        Sprite uncommonIcon,
        Sprite rareIcon,
        Sprite epicIcon,
        Sprite legendaryIcon)
    {
        if (commonIcon != null)
            relicBagCommonIcon = commonIcon;
        if (uncommonIcon != null)
            relicBagUncommonIcon = uncommonIcon;
        if (rareIcon != null)
            relicBagRareIcon = rareIcon;
        if (epicIcon != null)
            relicBagEpicIcon = epicIcon;
        if (legendaryIcon != null)
            relicBagLegendaryIcon = legendaryIcon;
    }

    public void Show(
        BoardTileData tile,
        IReadOnlyList<TreasureRewardChoice> choices,
        TreasureOptionSelectionHandler onSelect,
        RelicChoiceConfirmHandler onConfirmRelicChoice)
    {
        EnsureUi();
        currentTile = tile;
        closeAfterRelicConfirm = requireDeselectForRefresh = false;
        selectionHandler = onSelect;
        relicChoiceConfirmHandler = onConfirmRelicChoice;
        directRelicChoiceConfirmHandler = null;
        activeRelicChoiceData = null;
        isShowingResult = false;
        gameObject.SetActive(true);
        canvas.gameObject.SetActive(true);
        panel.gameObject.SetActive(true);
        cardRoot.gameObject.SetActive(true);
        resultText.gameObject.SetActive(false);
        continueButton.gameObject.SetActive(false);
        if (refreshRelicChoiceButton != null) refreshRelicChoiceButton.gameObject.SetActive(false);
        if (confirmRelicChoiceButton != null)
            confirmRelicChoiceButton.gameObject.SetActive(false);
        titleText.text = "宝藏馈赠";
        subtitleText.text = choices != null && choices.Count == 3 ? "请从三份馈赠中选择一份" : "请选择一份宝藏馈赠";
        RebuildCards(choices);
        ApplyFonts();
        BeginPresentation();
    }

    public void ShowRelicChoice(BattleSettlementRelicChoiceData choiceData, DirectRelicChoiceConfirmHandler onConfirmRelicChoice)
    {
        EnsureUi();
        currentTile = null;
        closeAfterRelicConfirm = requireDeselectForRefresh = false;
        selectionHandler = null;
        relicChoiceConfirmHandler = null;
        directRelicChoiceConfirmHandler = onConfirmRelicChoice;
        isShowingResult = false;
        gameObject.SetActive(true);
        canvas.gameObject.SetActive(true);
        panel.gameObject.SetActive(true);
        cardRoot.gameObject.SetActive(true);
        resultText.gameObject.SetActive(false);
        continueButton.gameObject.SetActive(false);
        ShowRelicChoice(choiceData, "请选择一个初始遗物。");
        BeginPresentation();
    }

    public void Hide()
    {
        if (!IsVisible || closing) return;
        closing = true;
        if (windowGroup != null) windowGroup.interactable = false;
        if (!Application.isPlaying) FinishHide();
    }

    public void HideImmediately() { FinishHide(); }

    private void FinishHide()
    {
        bool wasVisible = IsVisible;
        isShowingResult = false;
        selectionHandler = null;
        relicChoiceConfirmHandler = null;
        directRelicChoiceConfirmHandler = null;
        activeRelicChoiceData = null;
        activeChoices = null;
        currentTile = null;
        closing = false;
        if (panel != null) panel.gameObject.SetActive(false);
        if (canvas != null) canvas.gameObject.SetActive(false);
        if (wasVisible) Hidden?.Invoke();
    }

    private void EnsureUi()
    {
        if (canvas == null) EnsureForestUi();
    }

    private void ClearCards()
    {
        foreach (GameObject card in spawnedCards)
        {
            if (card == null) continue;
            card.SetActive(false);
            if (Application.isPlaying) Destroy(card);
            else DestroyImmediate(card);
        }
        spawnedCards.Clear();
        relicViews.Clear();
    }

    private void RebuildCards(IReadOnlyList<TreasureRewardChoice> choices)
    {
        activeChoices = choices;
        BuildRewardCards(choices);
    }

    private void OnChoiceClicked(TreasureRewardChoice choice)
    {
        if (!IsVisible || closing || handlingAction || isShowingResult || activeRelicChoiceData != null ||
            selectionHandler == null || currentTile == null || choice == null || !choice.isAvailable || !ContainsChoice(choice)) return;
        handlingAction = true;
        try
        {
            bool success = selectionHandler.Invoke(currentTile, choice, out TreasureRewardResult result);
            string message = result != null ? result.message : string.Empty;
            if (!success || result == null || !result.success)
            {
                subtitleText.text = string.IsNullOrWhiteSpace(message) ? "当前选项暂无法领取。" : message;
                return;
            }
            activeChoices = null;
            selectionHandler = null;
            if (result.relicChoiceData != null && result.relicChoiceData.HasChoices)
            {
                ShowRelicChoice(result.relicChoiceData, message);
                return;
            }
            PresentResult(message, ResolveRewardIcon(choice));
        }
        finally { handlingAction = false; }
    }

    private bool ContainsChoice(TreasureRewardChoice choice)
    {
        if (activeChoices != null)
            foreach (TreasureRewardChoice candidate in activeChoices) if (ReferenceEquals(candidate, choice)) return true;
        return false;
    }

    private void ShowRelicChoice(BattleSettlementRelicChoiceData choiceData, string message)
    {
        activeRelicChoiceData = choiceData;
        activeChoices = null;
        cardRoot.gameObject.SetActive(true);
        footerText.gameObject.SetActive(false);
        if (activeRelicChoiceData != null)
            activeRelicChoiceData.selectedChoice = null;

        titleText.text = CompactRelicTitle(choiceData.title);
        subtitleText.text = string.IsNullOrWhiteSpace(message) ? "\u8bf7\u4ece\u9057\u7269\u888b\u4e2d\u9009\u62e9 1 \u4e2a\u9057\u7269\u3002" : message;
        resultText.gameObject.SetActive(false);
        continueButton.gameObject.SetActive(false);
        confirmRelicChoiceButton.gameObject.SetActive(true);
        RebuildRelicChoiceCards(choiceData);
        RefreshRelicSelection();
        UpdateRelicRefreshButton();
        ApplyFonts();
    }

    private void RefreshRelicChoice()
    {
        if (closing || handlingAction || isShowingResult) return;
        if (requireDeselectForRefresh && activeRelicChoiceData?.selectedChoice != null) return;
        if (!BattleRelicChoiceSelectionService.TryRefresh(activeRelicChoiceData,
            new System.Random(UnityEngine.Random.Range(int.MinValue, int.MaxValue)))) return;
        ShowRelicChoice(activeRelicChoiceData, "遗物选项已刷新，请重新选择。");
    }

    private void UpdateRelicRefreshButton()
    {
        bool visible = activeRelicChoiceData != null && activeRelicChoiceData.refreshesRemaining > 0 && !activeRelicChoiceData.rewardApplied;
        refreshRelicChoiceButton.gameObject.SetActive(visible);
        refreshRelicChoiceButton.interactable = BattleRelicChoiceSelectionService.CanRefresh(activeRelicChoiceData)
            && (!requireDeselectForRefresh || activeRelicChoiceData.selectedChoice == null);
        refreshRelicChoiceButton.GetComponentInChildren<TMP_Text>().text = "刷新 (" + (activeRelicChoiceData != null ? activeRelicChoiceData.refreshesRemaining : 0) + ")";
        confirmRelicChoiceButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(visible ? -(buttonSize.x * 0.5f + 18f) : 0f, 48f);
    }

    private void RebuildRelicChoiceCards(BattleSettlementRelicChoiceData choiceData)
    {
        BuildRelicCards(choiceData);
    }

    private void SelectRelicChoice(BattleSettlementLootEntry entry)
    {
        if (closing || handlingAction || isShowingResult || activeRelicChoiceData == null || activeRelicChoiceData.rewardApplied || entry == null || !activeRelicChoiceData.choices.Contains(entry))
            return;

        activeRelicChoiceData.selectedChoice = activeRelicChoiceData.selectedChoice == entry ? null : entry;
        subtitleText.text = activeRelicChoiceData.selectedChoice == null
            ? "\u5df2\u53d6\u6d88\u9009\u62e9\uff0c\u8bf7\u4ece\u9057\u7269\u888b\u4e2d\u9009\u62e9 1 \u4e2a\u9057\u7269\u3002"
            : $"\u5df2\u9009\u62e9\uff1a{entry.displayName}";
        RefreshRelicSelection();
    }

    private void ConfirmRelicChoice()
    {
        if (!IsVisible || closing || handlingAction || isShowingResult || activeRelicChoiceData == null ||
            activeRelicChoiceData.rewardApplied || (relicChoiceConfirmHandler == null && directRelicChoiceConfirmHandler == null)) return;
        if (activeRelicChoiceData.selectedChoice == null)
        {
            subtitleText.text = !string.IsNullOrWhiteSpace(activeRelicChoiceData.requiredMessage)
                ? activeRelicChoiceData.requiredMessage : "请先选择一个遗物。";
            return;
        }
        handlingAction = true;
        try
        {
            bool success = currentTile != null && relicChoiceConfirmHandler != null
                ? relicChoiceConfirmHandler.Invoke(currentTile, activeRelicChoiceData, out string message)
                : directRelicChoiceConfirmHandler.Invoke(activeRelicChoiceData, out message);
            if (!success)
            {
                subtitleText.text = string.IsNullOrWhiteSpace(message) ? "遗物领取失败。" : message;
                return;
            }
            if (closeAfterRelicConfirm) Hide();
            else PresentResult(message, activeRelicChoiceData.selectedChoice.icon);
        }
        finally { handlingAction = false; }
    }

    private string BuildChoiceDetail(TreasureRewardChoice choice)
    {
        if (choice == null)
            return string.Empty;

        string detail = string.IsNullOrWhiteSpace(choice.description) ? "\u6682\u65e0\u8bf4\u660e" : choice.description;
        if (!choice.isAvailable && !string.IsNullOrWhiteSpace(choice.unavailableReason))
            detail = $"{detail}\n<color=#B0B0B0>{choice.unavailableReason}</color>";
        return detail;
    }

    private Sprite ResolveRewardIcon(TreasureRewardChoice choice)
    {
        if (choice == null)
            return null;

        if (choice.iconOverride != null) return choice.iconOverride;
        if (choice.relic != null) return choice.relic.icon;
        switch (choice.rewardType)
        {
            case TreasureRewardType.Wealth:
                return moneyIcon;
            case TreasureRewardType.Knowledge:
                return choice.blueprint != null ? choice.blueprint.DisplayIcon : null;
            case TreasureRewardType.Power:
                return ResolveRelicBagIcon(choice.relicBagRarity);
            default:
                return null;
        }
    }

    private Sprite ResolveRelicBagIcon(LootRarity rarity)
    {
        Sprite fallback = relicBagIcon;
        if (fallback == null && art != null && art.relicBags != null && (int)rarity >= 0 && (int)rarity < art.relicBags.Length)
            fallback = art.relicBags[(int)rarity];
        switch (rarity)
        {
            case LootRarity.Common:
                return relicBagCommonIcon != null ? relicBagCommonIcon : fallback;
            case LootRarity.Uncommon:
                return relicBagUncommonIcon != null ? relicBagUncommonIcon : fallback;
            case LootRarity.Rare:
                return relicBagRareIcon != null ? relicBagRareIcon : fallback;
            case LootRarity.Epic:
                return relicBagEpicIcon != null ? relicBagEpicIcon : fallback;
            case LootRarity.Legendary:
                return relicBagLegendaryIcon != null ? relicBagLegendaryIcon : fallback;
            default:
                return fallback;
        }
    }

    private void ApplyPanelBackground()
    {
        ApplyForestPanelBackground();
    }

    private Button BuildButton(RectTransform parent, string name, Vector2 anchoredPosition, string label, UnityEngine.Events.UnityAction action)
    {
        return BuildForestButton(parent, name, anchoredPosition, label, action);
    }

    private TextMeshProUGUI CreateText(RectTransform parent, string name, Vector2 anchoredPosition, Vector2 size, int fontSize, Color color, string value, bool keyword = false)
    {
        RectTransform rect = CreateRect(name, parent, new Vector2(0f, 1f), anchoredPosition, size);
        TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = fontSize;
        text.color = color;
        text.richText = true;
        text.alignment = TextAlignmentOptions.TopLeft;
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.raycastTarget = false;
        text.font = keyword && keywordFont != null ? keywordFont : (bodyFont != null ? bodyFont : keywordFont);
        ResourceKeywordText.Attach(text);
        return text;
    }

    private RectTransform CreateRect(string name, RectTransform parent, Vector2 anchorPivot, Vector2 anchoredPosition, Vector2 size)
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

    private void ApplyFonts()
    {
        if (canvas == null)
            return;

        TMP_Text[] texts = canvas.GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            TMP_Text text = texts[i];
            if (text == null)
                continue;

            bool useKeyword = string.Equals(text.name, "Title", StringComparison.Ordinal)
                || string.Equals(text.name, "ChoiceTitle", StringComparison.Ordinal)
                || string.Equals(text.name, "Label", StringComparison.Ordinal)
                || string.Equals(text.name, "Category", StringComparison.Ordinal);
            TMP_FontAsset font = useKeyword && keywordFont != null ? keywordFont : bodyFont;
            if (font != null)
                text.font = font;
        }
    }
}
