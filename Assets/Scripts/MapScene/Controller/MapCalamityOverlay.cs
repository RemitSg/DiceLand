using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class MapCalamityOverlay : MonoBehaviour
{
    [Header("Font")]
    [SerializeField] private TMP_FontAsset keywordFont;
    [SerializeField] private TMP_FontAsset bodyFont;
    [SerializeField] private int titleFontSize = 38;
    [SerializeField] private int bodyFontSize = 24;
    [SerializeField] private int buttonFontSize = 26;

    [Header("Panel")]
    [SerializeField] private Vector2 panelSize = new Vector2(1240f, 700f);
    [SerializeField] private Vector2 panelOffset = Vector2.zero;
    [SerializeField] private Sprite panelBackgroundSprite;
    [SerializeField] private Color panelBackgroundColor = new Color(0.08f, 0.1f, 0.14f, 0.96f);
    [SerializeField] private Color backdropColor = new Color(0f, 0f, 0f, 0.66f);
    [SerializeField] private Sprite backdropBackgroundSprite;
    [SerializeField] private Color backdropBackgroundColor = new Color(1f, 1f, 1f, 0.72f);

    [Header("Cards")]
    [SerializeField] private Vector2 cardSize = new Vector2(520f, 470f);
    [SerializeField] private float cardSpacing = 32f;
    [SerializeField] private Vector2 cardRootOffset = new Vector2(0f, -118f);
    [SerializeField] private Sprite cardBackgroundSprite;
    [SerializeField] private Color cardBackgroundColor = new Color(0.18f, 0.16f, 0.12f, 0.96f);
    [SerializeField] private Color cardTextColor = Color.white;
    [SerializeField] private Color cardAccentColor = new Color(1f, 0.83f, 0.35f, 1f);
    [SerializeField] private Sprite fallbackIconSprite;

    [Header("Buttons")]
    [SerializeField] private Sprite chooseButtonSprite;
    [SerializeField] private Color chooseButtonColor = new Color(1f, 0.83f, 0.35f, 1f);
    [SerializeField] private Color chooseButtonTextColor = Color.black;
    [SerializeField] private Vector2 chooseButtonSize = new Vector2(180f, 56f);
    [SerializeField] private string chooseButtonLabel = "\u9009\u62e9";

    private Canvas canvas;
    private RectTransform rootRect;
    private RectTransform panelRect;
    private Image backdropBackgroundImage;
    private TextMeshProUGUI titleText;
    private TextMeshProUGUI subtitleText;
    private RectTransform cardRoot;
    private readonly List<GameObject> spawnedCards = new List<GameObject>();

    private Action<Calamity_SO> chooseHandler;

    public bool IsVisible =>
        canvas != null &&
        canvas.gameObject.activeInHierarchy &&
        panelRect != null &&
        panelRect.gameObject.activeInHierarchy;

    public void ConfigureFonts(TMP_FontAsset keyword, TMP_FontAsset body)
    {
        if (keyword != null)
            keywordFont = keyword;
        if (body != null)
            bodyFont = body;

        ApplyFonts();
    }

    public void ConfigureBackdropSprite(Sprite sprite)
    {
        if (sprite != null)
            backdropBackgroundSprite = sprite;

        ApplyBackdropSprite();
    }

    public void Show(int calamityTier, IReadOnlyList<Calamity_SO> choices, Action<Calamity_SO> onChoose)
    {
        EnsureUi();
        chooseHandler = onChoose;
        canvas.gameObject.SetActive(true);
        panelRect.gameObject.SetActive(true);

        if (titleText != null)
            titleText.text = "\u707e\u5384\u964d\u4e34";
        if (subtitleText != null)
            subtitleText.text = $"\u707e\u5384\u7b49\u7ea7 {Mathf.Max(1, calamityTier)}\uff0c\u8bf7\u4ece\u4e0b\u5217\u707e\u5384\u4e2d\u9009\u62e9\u5176\u4e00\u627f\u53d7\u3002";

        RebuildCards(choices);
        ApplyFonts();
    }

    public void Hide()
    {
        if (panelRect != null)
            panelRect.gameObject.SetActive(false);
        if (canvas != null)
            canvas.gameObject.SetActive(false);
        chooseHandler = null;
    }

    private void EnsureUi()
    {
        if (canvas != null)
            return;

        GameObject canvasGo = new GameObject("CalamityOverlayCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(Image));
        canvasGo.transform.SetParent(transform, false);
        canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9100;

        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        rootRect = canvasGo.GetComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        Image blocker = canvasGo.GetComponent<Image>();
        blocker.color = backdropColor;

        backdropBackgroundImage = CreateStretchImage("MidnightBackdrop", rootRect);
        ApplyBackdropSprite();

        panelRect = CreateRect("Panel", rootRect, new Vector2(0.5f, 0.5f), panelOffset, panelSize);
        Image panelImage = panelRect.gameObject.AddComponent<Image>();
        panelImage.color = panelBackgroundColor;
        panelImage.sprite = panelBackgroundSprite;
        panelImage.type = panelBackgroundSprite != null ? Image.Type.Sliced : Image.Type.Simple;

        titleText = CreateText(panelRect, "Title", new Vector2(0f, -28f), new Vector2(panelSize.x - 80f, 42f), titleFontSize, Color.white, string.Empty, true);
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.rectTransform.anchorMin = new Vector2(0.5f, 1f);
        titleText.rectTransform.anchorMax = new Vector2(0.5f, 1f);
        titleText.rectTransform.pivot = new Vector2(0.5f, 1f);

        subtitleText = CreateText(panelRect, "Subtitle", new Vector2(0f, -82f), new Vector2(panelSize.x - 120f, 70f), bodyFontSize, cardAccentColor, string.Empty);
        subtitleText.alignment = TextAlignmentOptions.Center;
        subtitleText.enableWordWrapping = true;
        subtitleText.rectTransform.anchorMin = new Vector2(0.5f, 1f);
        subtitleText.rectTransform.anchorMax = new Vector2(0.5f, 1f);
        subtitleText.rectTransform.pivot = new Vector2(0.5f, 1f);

        cardRoot = CreateRect("CardRoot", panelRect, new Vector2(0.5f, 1f), cardRootOffset, new Vector2(panelSize.x - 120f, panelSize.y - 180f));
        cardRoot.anchorMin = new Vector2(0.5f, 1f);
        cardRoot.anchorMax = new Vector2(0.5f, 1f);
        cardRoot.pivot = new Vector2(0.5f, 1f);

        ApplyFonts();
        canvas.gameObject.SetActive(false);
    }

    private void ApplyBackdropSprite()
    {
        if (backdropBackgroundImage == null)
            return;

        bool hasSprite = backdropBackgroundSprite != null;
        backdropBackgroundImage.gameObject.SetActive(hasSprite);
        if (!hasSprite)
            return;

        backdropBackgroundImage.sprite = backdropBackgroundSprite;
        backdropBackgroundImage.color = backdropBackgroundColor;
        backdropBackgroundImage.type = Image.Type.Simple;
        backdropBackgroundImage.preserveAspect = false;
    }

    private void RebuildCards(IReadOnlyList<Calamity_SO> choices)
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
            Calamity_SO calamity = choices[i];
            if (calamity == null)
                continue;

            RectTransform card = CreateRect($"CalamityCard_{i}", cardRoot, new Vector2(0.5f, 1f), new Vector2(startX + i * (cardSize.x + cardSpacing), 0f), cardSize);
            card.anchorMin = new Vector2(0.5f, 1f);
            card.anchorMax = new Vector2(0.5f, 1f);
            card.pivot = new Vector2(0.5f, 1f);

            Image background = card.gameObject.AddComponent<Image>();
            background.color = cardBackgroundColor;
            background.sprite = cardBackgroundSprite;
            background.type = cardBackgroundSprite != null ? Image.Type.Sliced : Image.Type.Simple;

            bool hasIllustration = calamity.icon != null || fallbackIconSprite != null;
            if (hasIllustration)
            {
                Image iconImage = CreateRect("Icon", card, new Vector2(0f, 1f), new Vector2(18f, -18f), new Vector2(96f, 96f)).gameObject.AddComponent<Image>();
                iconImage.sprite = calamity.icon != null ? calamity.icon : fallbackIconSprite;
                iconImage.preserveAspect = true;
                iconImage.color = Color.white;
            }

            float headerX = hasIllustration ? 126f : 18f;
            float headerWidth = hasIllustration ? cardSize.x - 144f : cardSize.x - 36f;

            TextMeshProUGUI tierText = CreateText(card, "Tier", new Vector2(headerX, -18f), new Vector2(headerWidth, 28f), bodyFontSize - 2, cardAccentColor, $"\u707e\u5384\u7b49\u7ea7 {Mathf.Max(1, calamity.calamityTier)}", true);
            tierText.alignment = TextAlignmentOptions.Center;

            string displayName = string.IsNullOrWhiteSpace(calamity.DisplayName) ? calamity.UniqueId : calamity.DisplayName;
            TextMeshProUGUI nameText = CreateText(card, "Name", new Vector2(18f, -52f), new Vector2(cardSize.x - 36f, 56f), titleFontSize - 6, cardTextColor, displayName);
            nameText.alignment = TextAlignmentOptions.Center;
            nameText.enableWordWrapping = true;
            nameText.overflowMode = TextOverflowModes.Overflow;

            TextMeshProUGUI rarityText = CreateText(card, "Rarity", new Vector2(headerX, -94f), new Vector2(headerWidth, 28f), bodyFontSize - 2, cardAccentColor, BuildColoredRarityLabel(calamity.rarity), true);
            rarityText.alignment = TextAlignmentOptions.Center;

            TextMeshProUGUI descriptionText = CreateText(card, "Description", new Vector2(18f, -136f), new Vector2(cardSize.x - 36f, 220f), bodyFontSize, cardTextColor, string.IsNullOrWhiteSpace(calamity.description) ? "\u6682\u65e0\u707e\u5384\u63cf\u8ff0\u3002" : calamity.description);
            descriptionText.enableWordWrapping = true;
            descriptionText.overflowMode = TextOverflowModes.Overflow;

            BuildChooseButton(card, new Vector2(-18f, 18f), calamity);
            spawnedCards.Add(card.gameObject);
        }
    }

    private Button BuildChooseButton(RectTransform parent, Vector2 anchoredPosition, Calamity_SO calamity)
    {
        RectTransform rect = CreateRect("ChooseButton", parent, new Vector2(1f, 0f), anchoredPosition, chooseButtonSize);
        rect.anchorMin = new Vector2(1f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(1f, 0f);

        Image image = rect.gameObject.AddComponent<Image>();
        image.color = chooseButtonColor;
        image.sprite = chooseButtonSprite;
        image.type = chooseButtonSprite != null ? Image.Type.Sliced : Image.Type.Simple;

        Button button = rect.gameObject.AddComponent<Button>();
        button.onClick.AddListener(() =>
        {
            Action<Calamity_SO> handler = chooseHandler;
            Hide();
            handler?.Invoke(calamity);
        });

        TextMeshProUGUI label = CreateText(rect, "Label", Vector2.zero, chooseButtonSize, buttonFontSize, chooseButtonTextColor, chooseButtonLabel, true);
        label.alignment = TextAlignmentOptions.Center;
        label.rectTransform.anchorMin = Vector2.zero;
        label.rectTransform.anchorMax = Vector2.one;
        label.rectTransform.offsetMin = Vector2.zero;
        label.rectTransform.offsetMax = Vector2.zero;
        return button;
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

            text.font = text.name == "Title" || text.name == "Name" || text.name == "Tier" || text.name == "Rarity" || text.name == "Label"
                ? (keywordFont != null ? keywordFont : bodyFont)
                : (bodyFont != null ? bodyFont : keywordFont);
            text.UpdateMeshPadding();
            text.SetAllDirty();
        }
    }

    private TextMeshProUGUI CreateText(RectTransform parent, string name, Vector2 anchoredPosition, Vector2 size, int fontSize, Color color, string value, bool richText = false)
    {
        RectTransform rect = CreateRect(name, parent, new Vector2(0f, 1f), anchoredPosition, size);
        TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = fontSize;
        text.color = color;
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.richText = richText;
        text.alignment = TextAlignmentOptions.TopLeft;
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

    private Image CreateStretchImage(string name, RectTransform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return go.GetComponent<Image>();
    }

    private string BuildColoredRarityLabel(LootRarity rarity)
    {
        Color rarityColor;
        switch (rarity)
        {
            case LootRarity.Trash:
                rarityColor = new Color(0.49f, 0.33f, 0.19f, 1f);
                break;
            case LootRarity.Uncommon:
                rarityColor = new Color(0.38f, 0.9f, 0.42f, 1f);
                break;
            case LootRarity.Rare:
                rarityColor = new Color(0.36f, 0.68f, 1f, 1f);
                break;
            case LootRarity.Epic:
                rarityColor = new Color(0.76f, 0.42f, 1f, 1f);
                break;
            case LootRarity.Legendary:
                rarityColor = new Color(1f, 0.84f, 0.24f, 1f);
                break;
            default:
                rarityColor = new Color(0.78f, 0.78f, 0.78f, 1f);
                break;
        }

        return $"<color=#{ColorUtility.ToHtmlStringRGB(rarityColor)}>{rarity}</color>";
    }
}
