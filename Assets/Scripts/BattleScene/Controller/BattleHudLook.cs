using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Battle header presentation. Energy, reservations and round text still come from BattleButton.</summary>
[DisallowMultipleComponent]
public sealed class BattleHudLook : MonoBehaviour
{
    private BattleButton owner;
    private RectTransform canvasRect;
    private RectTransform energy;
    private RectTransform round;
    private Image energyFrame;
    private Image roundFrame;
    private Vector2 margin;
    private Vector2 energySize;
    private Vector2 roundSize;
    private Rect previousSafeArea;
    private Vector2 previousScreen;
    private Vector2 previousCanvasSize;

    public void Configure(BattleButton battle, RectTransform energyPanel, RectTransform roundPanel,
        TextMeshProUGUI energyText, TextMeshProUGUI roundText, Image normalFill, Image temporaryFill,
        Vector2 panelMargin, Vector2 configuredEnergySize, Vector2 configuredRoundSize)
    {
        owner = battle;
        BattleActionButtonLook.Apply(battle);
        canvasRect = GetComponent<RectTransform>();
        energy = energyPanel;
        round = roundPanel;
        margin = panelMargin;
        energySize = Vector2.Max(configuredEnergySize, new Vector2(380f, 100f));
        roundSize = Vector2.Max(configuredRoundSize, new Vector2(310f, 76f));

        energyFrame = DecoratePanel(energy, BattleHudOrnament.Shape.Panel, "BattleUI/ForestEnergyPlate");
        roundFrame = DecoratePanel(round, BattleHudOrnament.Shape.RoundPanel, "BattleUI/ForestRoundPlate");
        Jewel(energy, new Vector2(0f, 0.28f), Vector2.zero, new Vector2(18f, 24f));
        Jewel(round, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(20f, 24f));
        if (roundFrame == null)
        {
            Jewel(round, new Vector2(0f, 0.5f), new Vector2(4f, 0f), new Vector2(16f, 22f));
            Jewel(round, new Vector2(1f, 0.5f), new Vector2(-4f, 0f), new Vector2(16f, 22f));
        }
        StyleText(energyText);
        StyleText(roundText);
        energyText.alignment = TextAlignmentOptions.MidlineLeft;
        Stretch(energyText.rectTransform, new Vector2(29f, 46f), new Vector2(-27f, -13f));
        Stretch(roundText.rectTransform, new Vector2(36f, 10f), new Vector2(-36f, -10f));

        RectTransform track = normalFill.transform.parent as RectTransform;
        Image oldTrack = track.GetComponent<Image>();
        if (oldTrack != null) oldTrack.color = Color.clear;
        Stretch(track, new Vector2(29f, 20f), new Vector2(-29f, -64f));
        BattleHudOrnament well = Ornament("Inlaid energy socket", track, BattleHudOrnament.Shape.EnergyWell);
        Stretch(well.rectTransform, new Vector2(-3f, -3f), new Vector2(3f, 3f));
        well.transform.SetAsFirstSibling();
        DecorateFill(normalFill, BattleHudOrnament.Shape.EnergyFill);
        DecorateFill(temporaryFill, BattleHudOrnament.Shape.TemporaryFill);
        foreach (Graphic graphic in energy.GetComponentsInChildren<Graphic>(true)) graphic.raycastTarget = false;
        foreach (Graphic graphic in round.GetComponentsInChildren<Graphic>(true)) graphic.raycastTarget = false;
        RefreshLayout();
    }

    private void LateUpdate()
    {
        // The screen canvas deliberately has no actor parent. Remove it when its battle owner goes away.
        if (owner == null)
        {
            gameObject.SetActive(false);
            if (Application.isPlaying) Destroy(gameObject);
            else DestroyImmediate(gameObject);
            return;
        }
        Vector2 size = new Vector2(Screen.width, Screen.height);
        if (previousSafeArea != Screen.safeArea || previousScreen != size || previousCanvasSize != canvasRect.rect.size)
            RefreshLayout();
    }

    public void RefreshLayout()
    {
        ApplyLayout(Screen.safeArea, new Vector2(Screen.width, Screen.height));
    }

    public void ApplyLayout(Rect safeArea, Vector2 screenSize)
    {
        if (canvasRect == null || energy == null || round == null || screenSize.x <= 0f || screenSize.y <= 0f)
            return;
        Vector2 size = canvasRect.rect.size;
        if (size.x <= 0f || size.y <= 0f) return;
        previousSafeArea = safeArea;
        previousScreen = screenSize;
        previousCanvasSize = size;
        float left = Mathf.Clamp01(safeArea.xMin / screenSize.x) * size.x;
        float right = Mathf.Clamp01((screenSize.x - safeArea.xMax) / screenSize.x) * size.x;
        float top = Mathf.Clamp01((screenSize.y - safeArea.yMax) / screenSize.y) * size.y;
        float padding = Mathf.Max(16f, margin.x);
        float usable = Mathf.Max(1f, size.x - left - right - padding * 2f);
        Vector2 e = new Vector2(Mathf.Min(energySize.x, usable), energySize.y);
        Vector2 r = new Vector2(Mathf.Min(roundSize.x, usable), roundSize.y);
        energy.anchorMin = energy.anchorMax = energy.pivot = new Vector2(0f, 1f);
        energy.anchoredPosition = new Vector2(left + padding, -top - Mathf.Max(20f, margin.y));
        energy.sizeDelta = e;
        round.anchorMin = round.anchorMax = round.pivot = new Vector2(0.5f, 1f);
        // Leave room for the round plaque's raised jewel and the energy plaque's lower shadow.
        float secondRow = (usable - r.x) * 0.5f < e.x + 16f ? e.y + 24f : 0f;
        round.anchoredPosition = new Vector2((left - right) * 0.5f, energy.anchoredPosition.y - secondRow);
        round.sizeDelta = r;
        FitPaintedFrame(energyFrame, e.y);
        FitPaintedFrame(roundFrame, r.y);
    }

    private static Image DecoratePanel(RectTransform panel, BattleHudOrnament.Shape kind, string resource)
    {
        Image old = panel.GetComponent<Image>();
        if (old != null) old.color = Color.clear;
        Sprite[] sprites = Resources.LoadAll<Sprite>(resource);
        if (sprites.Length > 0)
        {
            GameObject go = new GameObject("Painted forest plate", typeof(RectTransform), typeof(CanvasRenderer));
            go.transform.SetParent(panel, false);
            Image image = go.AddComponent<Image>();
            image.sprite = sprites[0];
            image.type = Image.Type.Sliced;
            image.raycastTarget = false;
            Stretch(image.rectTransform, Vector2.zero, Vector2.zero);
            go.transform.SetAsFirstSibling();
            return image;
        }
        BattleHudOrnament frame = Ornament("Gilded dark plate", panel, kind);
        Stretch(frame.rectTransform, Vector2.zero, Vector2.zero);
        frame.transform.SetAsFirstSibling();
        return null;
    }

    private static void FitPaintedFrame(Image image, float height)
    {
        if (image == null || image.sprite == null) return;
        // Preserve the painted caps and side gems at the panel's height; only its middle stretches.
        image.pixelsPerUnitMultiplier = image.sprite.rect.height /
            (Mathf.Max(1f, height) * Mathf.Max(0.001f, image.pixelsPerUnit));
    }

    private static void StyleText(TextMeshProUGUI text)
    {
        text.color = Color.white;
        text.enableVertexGradient = true;
        text.colorGradient = new VertexGradient(new Color(1f, 0.94f, 0.55f), new Color(1f, 0.94f, 0.55f),
            new Color(1f, 0.62f, 0.12f), new Color(1f, 0.62f, 0.12f));
        text.enableAutoSizing = true;
        text.fontSizeMax = Mathf.Max(38f, text.fontSize);
        text.fontSizeMin = 22f;
        text.enableWordWrapping = false;
        Shadow shadow = text.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0.03f, 0.015f, 0f, 0.95f);
        shadow.effectDistance = new Vector2(1f, -2f);
    }

    private static void DecorateFill(Image fill, BattleHudOrnament.Shape shape)
    {
        fill.color = Color.clear;
        BattleHudOrnament glass = Ornament("Luminous glass", fill.rectTransform, shape);
        Stretch(glass.rectTransform, Vector2.zero, Vector2.zero);
    }

    private static void Jewel(RectTransform panel, Vector2 anchor, Vector2 position, Vector2 size)
    {
        BattleHudOrnament gem = Ornament("Turquoise setting", panel, BattleHudOrnament.Shape.Jewel);
        RectTransform rect = gem.rectTransform;
        rect.anchorMin = rect.anchorMax = anchor;
        rect.pivot = Vector2.one * 0.5f;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static BattleHudOrnament Ornament(string name, RectTransform parent, BattleHudOrnament.Shape kind)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
        go.transform.SetParent(parent, false);
        BattleHudOrnament graphic = go.AddComponent<BattleHudOrnament>();
        graphic.kind = kind;
        graphic.raycastTarget = false;
        return graphic;
    }

    private static void Stretch(RectTransform rect, Vector2 min, Vector2 max)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = min;
        rect.offsetMax = max;
    }
}
