using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed partial class MapBattleTilePreviewOverlay
{
    private static readonly Color Ivory = new Color(0.96f, 0.91f, 0.76f);
    private static readonly Color Quiet = new Color(0.63f, 0.66f, 0.57f);
    private static readonly Color Gold = new Color(0.77f, 0.59f, 0.29f);
    private static readonly Color Failure = new Color(1f, 0.60f, 0.48f);
    private Canvas canvas;
    private RectTransform panel, designRoot, crestRect, watermarkRect, headerDivider, statusViewport;
    private Image classIcon, confirmIcon;
    private TextMeshProUGUI battleClassText, dangerText, ratingText, dangerHeadingText, attributeHeadingText;
    private TextMeshProUGUI metadataText, explorationText, statusText, confirmLabelText, returnLabelText;
    private readonly TextMeshProUGUI[] attributeTexts = new TextMeshProUGUI[3];
    private Button confirmButton, returnButton;
    private ScrollRect statusScroll;
    private bool compactLayout;
    private MapBattlePreviewTheme theme;

    public void ConfigureTheme(MapBattlePreviewTheme value) { theme = value; ApplyFonts(); }

    private void EnsureUi()
    {
        EventSystemUtility.EnsureSingleEventSystem();
        if (canvas != null) return;
        GameObject go = new GameObject("BattleTilePreviewCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        go.transform.SetParent(transform, false);
        canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = CanvasSortingOrder;
        CanvasScaler scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = ReferenceResolution;
        scaler.matchWidthOrHeight = 0.5f;
        panel = CreateRect("PreviewPanel", go.transform);
        designRoot = CreateRect("HeraldicDesign", panel);
        Frame("HeraldicFrame", designRoot, MapBattlePreviewFrameGraphic.Style.Panel);
        Image watermark = Icon("CrossedSwordsWatermark", designRoot, MapForestArt.Get("Elite"));
        watermark.color = new Color(0.38f, 0.40f, 0.24f, 0.010f);
        watermarkRect = watermark.rectTransform;
        Image crest = Icon("HeraldicCrest", designRoot, Resources.Load<Sprite>("MapUI/BattlePreviewCrest"));
        crestRect = crest.rectTransform;
        classIcon = Icon("BattleClassIcon", designRoot, MapForestArt.Get("Elite"));
        metadataText = Text("MetadataText", designRoot, 24, Gold);
        battleClassText = Text("BattleClassText", designRoot, 60, Ivory);
        battleClassText.fontStyle = FontStyles.Bold;
        battleClassText.characterSpacing = 3f;
        headerDivider = CreateRect("HeaderDivider", designRoot);
        MapHudGlyph divider = headerDivider.gameObject.AddComponent<MapHudGlyph>();
        divider.shape = MapHudGlyph.Shape.Divider;
        divider.color = Gold;
        divider.raycastTarget = false;
        dangerHeadingText = Text("DangerHeadingText", designRoot, 24, Ivory);
        dangerHeadingText.text = "危险度";
        dangerText = Text("DangerText", designRoot, 56, Ivory);
        ratingText = Text("DangerRatingText", designRoot, 30, Ivory);
        attributeHeadingText = Text("AttributeHeadingText", designRoot, 24, Ivory);
        attributeHeadingText.text = "属性倾向";
        for (int i = 0; i < attributeTexts.Length; i++) attributeTexts[i] = Text("Attribute" + i, designRoot, 36, Ivory);
        explorationText = Text("ExplorationText", designRoot, 23, Quiet);

        statusViewport = CreateRect("StatusViewport", designRoot);
        statusViewport.gameObject.AddComponent<Image>().color = Color.clear;
        statusViewport.gameObject.AddComponent<RectMask2D>();
        statusScroll = statusViewport.gameObject.AddComponent<ScrollRect>();
        statusScroll.viewport = statusViewport;
        statusScroll.horizontal = false;
        statusScroll.vertical = true;
        statusScroll.movementType = ScrollRect.MovementType.Clamped;
        statusScroll.scrollSensitivity = 24f;
        statusText = Text("StatusText", statusViewport, 23, Quiet);
        statusText.alignment = TextAlignmentOptions.TopLeft;
        statusText.enableWordWrapping = true;
        statusText.enableAutoSizing = false;
        statusText.overflowMode = TextOverflowModes.Overflow;
        statusScroll.content = statusText.rectTransform;

        RectTransform confirm = CreateRect("ConfirmButton", designRoot);
        MapBattlePreviewFrameGraphic buttonFace = Frame("ConfirmFace", confirm, MapBattlePreviewFrameGraphic.Style.Button);
        confirmButton = confirm.gameObject.AddComponent<Button>();
        confirmButton.targetGraphic = buttonFace;
        confirmButton.colors = ButtonColours();
        confirmIcon = Icon("ConfirmSwords", confirm, MapForestArt.Get("Elite"));
        confirmLabelText = Text("ConfirmLabel", confirm, 38, Ivory);
        confirmLabelText.fontStyle = FontStyles.Bold;
        RectTransform back = CreateRect("ReturnButton", designRoot);
        MapBattlePreviewFrameGraphic backFace = Frame("ReturnFace", back, MapBattlePreviewFrameGraphic.Style.Return);
        returnButton = back.gameObject.AddComponent<Button>();
        returnButton.targetGraphic = backFace;
        returnButton.colors = ButtonColours();
        returnLabelText = Text("ReturnLabel", back, 29, Ivory);
        confirmButton.onClick.AddListener(OnConfirmClicked);
        returnButton.onClick.AddListener(OnReturnClicked);
        ApplyFonts();
        compactLayout = false;
        designRoot.sizeDelta = LandscapePanelSize;
        LayoutContent();
        canvas.gameObject.SetActive(false);
    }

    private void LayoutPanel(Vector2 screen, Rect safe)
    {
        float factor = Mathf.Sqrt(screen.x / ReferenceResolution.x * screen.y / ReferenceResolution.y);
        float width = screen.x / factor, height = screen.y / factor;
        float left = safe.xMin / factor, right = (screen.x - safe.xMax) / factor;
        float bottom = safe.yMin / factor, top = (screen.y - safe.yMax) / factor;
        compactLayout = screen.x / screen.y < LandscapeAspectThreshold;
        Vector2 size = compactLayout ? PortraitPanelSize : LandscapePanelSize;
        float scale = Mathf.Min(1f, (safe.width / factor - 48f) / size.x, (safe.height / factor - 48f) / size.y);
        scale = Mathf.Max(0.01f, scale);
        designRoot.sizeDelta = size;
        designRoot.localScale = Vector3.one * scale;
        panel.sizeDelta = size * scale;
        if (!compactLayout)
        {
            panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(1f, 0.5f);
            panel.anchoredPosition = new Vector2(-40f - right, bottom + (height - top - bottom) * 0.5f - height * 0.5f);
        }
        else
        {
            panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(0.5f, 0f);
            panel.anchoredPosition = new Vector2((left - right) * 0.5f, 24f + bottom);
        }
        designRoot.anchorMin = designRoot.anchorMax = designRoot.pivot = new Vector2(0.5f, 0.5f);
        designRoot.anchoredPosition = Vector2.zero;
        LayoutContent();
    }

    private void LayoutContent()
    {
        if (designRoot == null) return;
        int count = 0;
        foreach (TMP_Text label in attributeTexts) if (label != null && label.gameObject.activeSelf) count++;
        if (!compactLayout)
        {
            Place(crestRect, 0, -12, 200, 300);
            Place(classIcon.rectTransform, 42, 40, 120, 120);
            Place(watermarkRect, 280, 248, 260, 260);
            Place(metadataText.rectTransform, 214, 48, 332, 42);
            Place(battleClassText.rectTransform, 190, 102, 365, 90);
            Place(headerDivider, 174, 222, 370, 10);
            Place(dangerHeadingText.rectTransform, 130, 270, 220, 36);
            Place(dangerText.rectTransform, 130, 310, 240, 82);
            Place(ratingText.rectTransform, 380, 334, 140, 46);
            Place(attributeHeadingText.rectTransform, 130, 414, 155, 40);
            for (int i = 0; i < attributeTexts.Length; i++)
            {
                float w = count <= 1 ? 230f : 412f / Mathf.Max(1, count);
                Place(attributeTexts[i].rectTransform, count <= 1 ? 292f : 130f + i * w, count <= 1 ? 406f : 454f, w - 6f, 54f);
                Size(attributeTexts[i], count <= 1 ? 38f : 32f);
            }
            Place(explorationText.rectTransform, 130, count <= 1 ? 485 : 505, 414, 36);
            Place(statusViewport, 130, 546, 414, 56);
            Place(confirmButton.transform as RectTransform, 98, 610, 446, 86);
            Place(returnButton.transform as RectTransform, 190, 700, 250, 52);
            Size(battleClassText, 64); Size(dangerText, 52);
        }
        else
        {
            Place(crestRect, 0, -6, 150, 225);
            Place(classIcon.rectTransform, 33, 34, 90, 90);
            Place(watermarkRect, 570, 130, 220, 220);
            Place(metadataText.rectTransform, 170, 28, 660, 40);
            Place(battleClassText.rectTransform, 166, 70, 670, 78);
            Place(headerDivider, 166, 151, 670, 10);
            Place(dangerHeadingText.rectTransform, 170, 170, 230, 34);
            Place(dangerText.rectTransform, 170, 204, 200, 62);
            Place(ratingText.rectTransform, 380, 218, 110, 38);
            Place(attributeHeadingText.rectTransform, 520, 170, 300, 34);
            for (int i = 0; i < attributeTexts.Length; i++)
            {
                float w = 320f / Mathf.Max(1, count);
                Place(attributeTexts[i].rectTransform, 520 + i * w, 211, w - 6, 50);
                Size(attributeTexts[i], 32);
            }
            Place(explorationText.rectTransform, 170, 274, 670, 38);
            Place(statusViewport, 170, 312, 670, 48);
            Place(confirmButton.transform as RectTransform, 470, 365, 350, 76);
            Place(returnButton.transform as RectTransform, 180, 373, 250, 56);
            Size(battleClassText, 54); Size(dangerText, 46);
        }
        Place(confirmIcon.rectTransform, compactLayout ? 75 : 112, 22, 40, 40);
        Place(confirmLabelText.rectTransform, compactLayout ? 130 : 168, 9, compactLayout ? 210 : 250, 64);
        Place(returnLabelText.rectTransform, 44, 0, 192, 52);
        RefreshStatusLayout();
    }

    private void RefreshStatusLayout()
    {
        if (statusText == null || statusViewport == null) return;
        bool visible = !string.IsNullOrWhiteSpace(statusText.text);
        statusViewport.gameObject.SetActive(visible);
        if (!visible) return;
        float width = statusViewport.sizeDelta.x;
        float height = Mathf.Max(statusViewport.sizeDelta.y, statusText.GetPreferredValues(width, 0f).y + 4f);
        Place(statusText.rectTransform, 0, 0, width, height);
        statusScroll.verticalNormalizedPosition = 1f;
    }

    private void ApplyFonts()
    {
        Font(battleClassText, theme != null && theme.titleFont != null ? theme.titleFont : keywordFont);
        TMP_FontAsset action = theme != null && theme.actionFont != null ? theme.actionFont : keywordFont;
        Font(confirmLabelText, action); Font(returnLabelText, action);
        Font(metadataText, bodyFont); Font(dangerText, bodyFont); Font(ratingText, bodyFont);
        Font(dangerHeadingText, bodyFont); Font(attributeHeadingText, bodyFont); Font(explorationText, bodyFont); Font(statusText, bodyFont);
        foreach (TMP_Text label in attributeTexts) Font(label, attributeFont);
    }

    private static void Font(TMP_Text text, TMP_FontAsset font) { if (text != null && font != null) text.font = font; }
    private static void Size(TMP_Text text, float value) { text.fontSize = text.fontSizeMax = value; text.fontSizeMin = value * 0.72f; }

    private static RectTransform CreateRect(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    private static TextMeshProUGUI Text(string name, Transform parent, float size, Color colour)
    {
        TextMeshProUGUI text = CreateRect(name, parent).gameObject.AddComponent<TextMeshProUGUI>();
        text.color = colour;
        text.raycastTarget = false;
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.enableAutoSizing = true;
        Size(text, size);
        ResourceKeywordText.Attach(text);
        return text;
    }

    private static Image Icon(string name, Transform parent, Sprite sprite)
    {
        Image image = CreateRect(name, parent).gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
        image.raycastTarget = false;
        return image;
    }

    private static MapBattlePreviewFrameGraphic Frame(string name, RectTransform parent, MapBattlePreviewFrameGraphic.Style style)
    {
        MapBattlePreviewFrameGraphic graphic = CreateRect(name, parent).gameObject.AddComponent<MapBattlePreviewFrameGraphic>();
        graphic.style = style;
        graphic.rectTransform.anchorMin = Vector2.zero;
        graphic.rectTransform.anchorMax = Vector2.one;
        graphic.rectTransform.offsetMin = graphic.rectTransform.offsetMax = Vector2.zero;
        return graphic;
    }

    private static void Place(RectTransform rect, float x, float y, float width, float height)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(x, -y);
        rect.sizeDelta = new Vector2(width, height);
    }

    private static ColorBlock ButtonColours()
    {
        ColorBlock colours = ColorBlock.defaultColorBlock;
        colours.normalColor = Color.white;
        colours.highlightedColor = new Color(1f, 0.96f, 0.82f);
        colours.pressedColor = new Color(0.72f, 0.64f, 0.56f);
        colours.disabledColor = new Color(0.42f, 0.43f, 0.42f, 0.8f);
        colours.fadeDuration = 0.10f;
        return colours;
    }
}
