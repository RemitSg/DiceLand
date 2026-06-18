using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class MapTreasureOverlay : MonoBehaviour
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
    [SerializeField] private Vector2 panelSize = new Vector2(1180f, 660f);
    [SerializeField] private Vector2 panelOffset = Vector2.zero;
    [SerializeField] private Sprite panelBackgroundSprite;
    [SerializeField] private Color backdropColor = new Color(0f, 0f, 0f, 0.58f);
    [SerializeField] private Color panelColor = new Color(0.1f, 0.08f, 0.05f, 0.96f);
    [SerializeField] private Color accentColor = new Color(1f, 0.78f, 0.28f, 1f);
    [SerializeField] private Color textColor = new Color(0.96f, 0.94f, 0.86f, 1f);

    [Header("Cards")]
    [SerializeField] private Vector2 cardSize = new Vector2(330f, 380f);
    [SerializeField] private float cardSpacing = 28f;
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
    [SerializeField] private Vector2 buttonSize = new Vector2(180f, 56f);
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

    private BoardTileData currentTile;
    private TreasureOptionSelectionHandler selectionHandler;
    private RelicChoiceConfirmHandler relicChoiceConfirmHandler;
    private DirectRelicChoiceConfirmHandler directRelicChoiceConfirmHandler;
    private BattleSettlementRelicChoiceData activeRelicChoiceData;
    private bool isShowingResult;

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
        if (confirmRelicChoiceButton != null)
            confirmRelicChoiceButton.gameObject.SetActive(false);
        titleText.text = "\u53d1\u73b0\u5b9d\u85cf";
        subtitleText.text = "\u5b9d\u85cf\u7684\u5185\u5bb9\u5df2\u663e\u73b0\uff0c\u8bf7\u5728\u4e09\u4efd\u9988\u8d60\u4e2d\u9009\u62e9\u4e00\u4efd\u3002";
        RebuildCards(choices);
        ApplyFonts();
    }

    public void ShowRelicChoice(BattleSettlementRelicChoiceData choiceData, DirectRelicChoiceConfirmHandler onConfirmRelicChoice)
    {
        EnsureUi();
        currentTile = null;
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
    }

    public void Hide()
    {
        bool wasVisible = IsVisible;
        isShowingResult = false;
        selectionHandler = null;
        relicChoiceConfirmHandler = null;
        directRelicChoiceConfirmHandler = null;
        activeRelicChoiceData = null;
        currentTile = null;
        if (panel != null)
            panel.gameObject.SetActive(false);
        if (canvas != null)
            canvas.gameObject.SetActive(false);
        if (wasVisible)
            Hidden?.Invoke();
    }

    private void EnsureUi()
    {
        if (canvas != null)
            return;

        GameObject canvasGo = new GameObject("TreasureOverlayCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(Image));
        canvasGo.transform.SetParent(transform, false);
        canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 8020;

        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        root = canvasGo.GetComponent<RectTransform>();
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.offsetMin = Vector2.zero;
        root.offsetMax = Vector2.zero;
        canvasGo.GetComponent<Image>().color = backdropColor;

        panel = CreateRect("Panel", root, new Vector2(0.5f, 0.5f), panelOffset, panelSize);
        panelImage = panel.gameObject.AddComponent<Image>();
        ApplyPanelBackground();

        titleText = CreateText(panel, "Title", new Vector2(0f, -30f), new Vector2(panelSize.x - 80f, 46f), titleFontSize, textColor, string.Empty, true);
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.rectTransform.anchorMin = new Vector2(0.5f, 1f);
        titleText.rectTransform.anchorMax = new Vector2(0.5f, 1f);
        titleText.rectTransform.pivot = new Vector2(0.5f, 1f);

        subtitleText = CreateText(panel, "Subtitle", new Vector2(0f, -86f), new Vector2(panelSize.x - 140f, 70f), bodyFontSize, accentColor, string.Empty);
        subtitleText.alignment = TextAlignmentOptions.Center;
        subtitleText.enableWordWrapping = true;
        subtitleText.rectTransform.anchorMin = new Vector2(0.5f, 1f);
        subtitleText.rectTransform.anchorMax = new Vector2(0.5f, 1f);
        subtitleText.rectTransform.pivot = new Vector2(0.5f, 1f);

        cardRoot = CreateRect("CardRoot", panel, new Vector2(0.5f, 1f), new Vector2(0f, -178f), new Vector2(panelSize.x - 100f, 410f));
        cardRoot.anchorMin = new Vector2(0.5f, 1f);
        cardRoot.anchorMax = new Vector2(0.5f, 1f);
        cardRoot.pivot = new Vector2(0.5f, 1f);

        resultText = CreateText(panel, "Result", new Vector2(0f, -220f), new Vector2(panelSize.x - 160f, 180f), bodyFontSize + 2, textColor, string.Empty);
        resultText.alignment = TextAlignmentOptions.Center;
        resultText.enableWordWrapping = true;
        resultText.rectTransform.anchorMin = new Vector2(0.5f, 1f);
        resultText.rectTransform.anchorMax = new Vector2(0.5f, 1f);
        resultText.rectTransform.pivot = new Vector2(0.5f, 1f);
        resultText.gameObject.SetActive(false);

        continueButton = BuildButton(panel, "ContinueButton", new Vector2(0f, 48f), continueLabel, Hide);
        continueButton.GetComponent<RectTransform>().anchorMin = new Vector2(0.5f, 0f);
        continueButton.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 0f);
        continueButton.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0f);
        continueButton.gameObject.SetActive(false);

        confirmRelicChoiceButton = BuildButton(panel, "ConfirmRelicChoiceButton", new Vector2(0f, 48f), "\u786e\u8ba4", ConfirmRelicChoice);
        confirmRelicChoiceButton.GetComponent<RectTransform>().anchorMin = new Vector2(0.5f, 0f);
        confirmRelicChoiceButton.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 0f);
        confirmRelicChoiceButton.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0f);
        confirmRelicChoiceButton.gameObject.SetActive(false);

        ApplyFonts();
        canvas.gameObject.SetActive(false);
    }

    private void RebuildCards(IReadOnlyList<TreasureRewardChoice> choices)
    {
        for (int i = 0; i < spawnedCards.Count; i++)
            Destroy(spawnedCards[i]);
        spawnedCards.Clear();

        int count = choices != null ? choices.Count : 0;
        if (count <= 0)
            return;

        float totalWidth = count * cardSize.x + Mathf.Max(0, count - 1) * cardSpacing;
        float startX = -totalWidth * 0.5f + cardSize.x * 0.5f;
        for (int i = 0; i < count; i++)
        {
            TreasureRewardChoice choice = choices[i];
            if (choice == null)
                continue;

            RectTransform card = CreateRect($"TreasureChoice_{i}", cardRoot, new Vector2(0.5f, 1f), new Vector2(startX + i * (cardSize.x + cardSpacing), 0f), cardSize);
            card.anchorMin = new Vector2(0.5f, 1f);
            card.anchorMax = new Vector2(0.5f, 1f);
            card.pivot = new Vector2(0.5f, 1f);

            Image background = card.gameObject.AddComponent<Image>();
            background.color = choice.isAvailable ? cardColor : disabledCardColor;
            Sprite cardBackground = choice.isAvailable ? optionBackgroundSprite : (disabledOptionBackgroundSprite != null ? disabledOptionBackgroundSprite : optionBackgroundSprite);
            background.sprite = cardBackground;
            background.type = cardBackground != null ? Image.Type.Sliced : Image.Type.Simple;

            TextMeshProUGUI title = CreateText(card, "ChoiceTitle", new Vector2(18f, -24f), new Vector2(cardSize.x - 36f, 42f), titleFontSize - 6, choice.isAvailable ? accentColor : Color.gray, choice.title, true);
            title.alignment = TextAlignmentOptions.Center;

            Sprite rewardIcon = ResolveRewardIcon(choice);
            if (rewardIcon != null)
            {
                Image iconImage = CreateRect("RewardIcon", card, new Vector2(0.5f, 1f), rewardIconOffset, rewardIconSize).gameObject.AddComponent<Image>();
                iconImage.sprite = rewardIcon;
                iconImage.preserveAspect = true;
                iconImage.color = choice.isAvailable ? rewardIconColor : Color.gray;
            }

            float detailTop = rewardIcon != null ? -194f : -96f;
            float detailHeight = rewardIcon != null ? 92f : 160f;
            TextMeshProUGUI detail = CreateText(card, "ChoiceDetail", new Vector2(24f, detailTop), new Vector2(cardSize.x - 48f, detailHeight), bodyFontSize, choice.isAvailable ? textColor : Color.gray, BuildChoiceDetail(choice), true);
            detail.enableWordWrapping = true;
            detail.overflowMode = TextOverflowModes.Overflow;
            detail.alignment = TextAlignmentOptions.Center;

            Button chooseButton = BuildButton(card, "ChooseButton", new Vector2(0f, 28f), chooseLabel, () => OnChoiceClicked(choice));
            RectTransform buttonRect = chooseButton.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0.5f, 0f);
            buttonRect.anchorMax = new Vector2(0.5f, 0f);
            buttonRect.pivot = new Vector2(0.5f, 0f);
            chooseButton.interactable = choice.isAvailable;
            Image image = chooseButton.GetComponent<Image>();
            if (image != null && !choice.isAvailable)
                image.color = disabledButtonColor;

            spawnedCards.Add(card.gameObject);
        }
    }

    private void OnChoiceClicked(TreasureRewardChoice choice)
    {
        if (isShowingResult || selectionHandler == null || currentTile == null || choice == null)
            return;

        bool success = selectionHandler.Invoke(currentTile, choice, out TreasureRewardResult result);
        string message = result != null ? result.message : string.Empty;
        if (!success || result == null || !result.success)
        {
            subtitleText.text = string.IsNullOrWhiteSpace(message) ? "\u5f53\u524d\u9009\u9879\u6682\u65e0\u6cd5\u9886\u53d6\u3002" : message;
            return;
        }

        if (result.relicChoiceData != null && result.relicChoiceData.HasChoices)
        {
            ShowRelicChoice(result.relicChoiceData, message);
            return;
        }

        isShowingResult = true;
        cardRoot.gameObject.SetActive(false);
        resultText.gameObject.SetActive(true);
        continueButton.gameObject.SetActive(true);
        if (confirmRelicChoiceButton != null)
            confirmRelicChoiceButton.gameObject.SetActive(false);
        subtitleText.text = "\u5b9d\u85cf\u5df2\u7ecf\u56de\u5e94\u4e86\u4f60\u7684\u9009\u62e9\u3002";
        resultText.text = string.IsNullOrWhiteSpace(message) ? "\u5df2\u9886\u53d6\u5b9d\u85cf\u5956\u52b1\u3002" : message;
    }

    private void ShowRelicChoice(BattleSettlementRelicChoiceData choiceData, string message)
    {
        activeRelicChoiceData = choiceData;
        if (activeRelicChoiceData != null)
            activeRelicChoiceData.selectedChoice = null;

        titleText.text = !string.IsNullOrWhiteSpace(choiceData.title) ? choiceData.title : "\u9057\u7269\u888b";
        subtitleText.text = string.IsNullOrWhiteSpace(message) ? "\u8bf7\u4ece\u9057\u7269\u888b\u4e2d\u9009\u62e9 1 \u4e2a\u9057\u7269\u3002" : message;
        resultText.gameObject.SetActive(false);
        continueButton.gameObject.SetActive(false);
        confirmRelicChoiceButton.gameObject.SetActive(true);
        RebuildRelicChoiceCards(choiceData);
        ApplyFonts();
    }

    private void RebuildRelicChoiceCards(BattleSettlementRelicChoiceData choiceData)
    {
        for (int i = 0; i < spawnedCards.Count; i++)
            Destroy(spawnedCards[i]);
        spawnedCards.Clear();

        IReadOnlyList<BattleSettlementLootEntry> choices = choiceData != null ? choiceData.choices : null;
        int count = choices != null ? choices.Count : 0;
        if (count <= 0)
            return;

        float totalWidth = count * cardSize.x + Mathf.Max(0, count - 1) * cardSpacing;
        float startX = -totalWidth * 0.5f + cardSize.x * 0.5f;
        for (int i = 0; i < count; i++)
        {
            BattleSettlementLootEntry entry = choices[i];
            if (entry == null)
                continue;

            RectTransform card = CreateRect($"TreasureRelicChoice_{i}", cardRoot, new Vector2(0.5f, 1f), new Vector2(startX + i * (cardSize.x + cardSpacing), 0f), cardSize);
            card.anchorMin = new Vector2(0.5f, 1f);
            card.anchorMax = new Vector2(0.5f, 1f);
            card.pivot = new Vector2(0.5f, 1f);

            Image background = card.gameObject.AddComponent<Image>();
            background.color = cardColor;
            background.sprite = optionBackgroundSprite;
            background.type = optionBackgroundSprite != null ? Image.Type.Sliced : Image.Type.Simple;
            Button cardButton = card.gameObject.AddComponent<Button>();
            cardButton.onClick.AddListener(() => SelectRelicChoice(entry));

            TextMeshProUGUI title = CreateText(card, "ChoiceTitle", new Vector2(18f, -24f), new Vector2(cardSize.x - 36f, 42f), titleFontSize - 8, accentColor, entry.displayName, true);
            title.alignment = TextAlignmentOptions.Center;

            if (entry.icon != null)
            {
                Image iconImage = CreateRect("RewardIcon", card, new Vector2(0.5f, 1f), rewardIconOffset, rewardIconSize).gameObject.AddComponent<Image>();
                iconImage.sprite = entry.icon;
                iconImage.preserveAspect = true;
                iconImage.color = rewardIconColor;
            }

            string rarity = TreasureRewardService.GetColoredRarityLabel(entry.rarity);
            TextMeshProUGUI detail = CreateText(card, "ChoiceDetail", new Vector2(24f, -194f), new Vector2(cardSize.x - 48f, 92f), bodyFontSize, textColor, $"{rarity}\n{entry.description}", true);
            detail.enableWordWrapping = true;
            detail.overflowMode = TextOverflowModes.Overflow;
            detail.alignment = TextAlignmentOptions.Center;

            spawnedCards.Add(card.gameObject);
        }
    }

    private void SelectRelicChoice(BattleSettlementLootEntry entry)
    {
        if (activeRelicChoiceData == null || entry == null)
            return;

        activeRelicChoiceData.selectedChoice = activeRelicChoiceData.selectedChoice == entry ? null : entry;
        subtitleText.text = activeRelicChoiceData.selectedChoice == null
            ? "\u5df2\u53d6\u6d88\u9009\u62e9\uff0c\u8bf7\u4ece\u9057\u7269\u888b\u4e2d\u9009\u62e9 1 \u4e2a\u9057\u7269\u3002"
            : $"\u5df2\u9009\u62e9\uff1a{entry.displayName}";
    }

    private void ConfirmRelicChoice()
    {
        if (activeRelicChoiceData == null || (relicChoiceConfirmHandler == null && directRelicChoiceConfirmHandler == null))
            return;

        if (activeRelicChoiceData.selectedChoice == null)
        {
            subtitleText.text = !string.IsNullOrWhiteSpace(activeRelicChoiceData.requiredMessage)
                ? activeRelicChoiceData.requiredMessage
                : "\u8bf7\u5148\u9009\u62e9\u4e00\u4e2a\u9057\u7269\u3002";
            return;
        }

        bool success = currentTile != null && relicChoiceConfirmHandler != null
            ? relicChoiceConfirmHandler.Invoke(currentTile, activeRelicChoiceData, out string message)
            : directRelicChoiceConfirmHandler.Invoke(activeRelicChoiceData, out message);
        if (!success)
        {
            subtitleText.text = string.IsNullOrWhiteSpace(message) ? "\u9057\u7269\u9886\u53d6\u5931\u8d25\u3002" : message;
            return;
        }

        isShowingResult = true;
        cardRoot.gameObject.SetActive(false);
        resultText.gameObject.SetActive(true);
        continueButton.gameObject.SetActive(true);
        confirmRelicChoiceButton.gameObject.SetActive(false);
        subtitleText.text = "\u5b9d\u85cf\u5df2\u7ecf\u56de\u5e94\u4e86\u4f60\u7684\u9009\u62e9\u3002";
        resultText.text = string.IsNullOrWhiteSpace(message) ? "\u5df2\u9886\u53d6\u9057\u7269\u3002" : message;
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
        switch (rarity)
        {
            case LootRarity.Common:
                return relicBagCommonIcon != null ? relicBagCommonIcon : relicBagIcon;
            case LootRarity.Uncommon:
                return relicBagUncommonIcon != null ? relicBagUncommonIcon : relicBagIcon;
            case LootRarity.Rare:
                return relicBagRareIcon != null ? relicBagRareIcon : relicBagIcon;
            case LootRarity.Epic:
                return relicBagEpicIcon != null ? relicBagEpicIcon : relicBagIcon;
            case LootRarity.Legendary:
                return relicBagLegendaryIcon != null ? relicBagLegendaryIcon : relicBagIcon;
            default:
                return relicBagIcon;
        }
    }

    private void ApplyPanelBackground()
    {
        if (panelImage == null)
            return;

        panelImage.color = panelColor;
        panelImage.sprite = panelBackgroundSprite;
        panelImage.type = panelBackgroundSprite != null ? Image.Type.Sliced : Image.Type.Simple;
    }

    private Button BuildButton(RectTransform parent, string name, Vector2 anchoredPosition, string label, UnityEngine.Events.UnityAction action)
    {
        RectTransform rect = CreateRect(name, parent, new Vector2(1f, 0f), anchoredPosition, buttonSize);
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = buttonColor;
        Button button = rect.gameObject.AddComponent<Button>();
        button.onClick.AddListener(action);

        TextMeshProUGUI text = CreateText(rect, "Label", Vector2.zero, buttonSize, buttonFontSize, buttonTextColor, label, true);
        text.alignment = TextAlignmentOptions.Center;
        text.rectTransform.anchorMin = Vector2.zero;
        text.rectTransform.anchorMax = Vector2.one;
        text.rectTransform.offsetMin = Vector2.zero;
        text.rectTransform.offsetMax = Vector2.zero;
        return button;
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
                || string.Equals(text.name, "Label", StringComparison.Ordinal);
            TMP_FontAsset font = useKeyword && keywordFont != null ? keywordFont : bodyFont;
            if (font != null)
                text.font = font;
        }
    }
}
