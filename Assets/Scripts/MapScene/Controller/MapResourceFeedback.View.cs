using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public partial class MapResourceFeedback
{
    private sealed class CoinView
    {
        public Image image;
        public MapFeedbackGraphic glint;
        public float delay;
        public long value;
        public bool arrived;
        public Vector2 origin, scatter;
    }

    private sealed class Decoration
    {
        public RectTransform root;
        public Graphic graphic;
        public float age, lifetime;
        public Vector2 origin, drift;
        public Color tint;
        public bool ring;
    }

    private Canvas feedbackCanvas;
    private RectTransform feedbackRoot;
    private MapFeedbackGraphic screenEdge;
    private float screenFlashAge, screenFlashStrength;
    private Color screenFlashColor;
    private Sprite coinSprite;
    private int liveCoins;
    private readonly List<CoinView> coins = new List<CoinView>();
    private readonly List<Decoration> decorations = new List<Decoration>();

    private void EnsureVisuals()
    {
        if (feedbackCanvas != null) return;
        GameObject host = new GameObject("MapResourceFeedbackCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(CanvasGroup));
        host.transform.SetParent(transform, false);
        feedbackCanvas = host.GetComponent<Canvas>();
        feedbackCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        feedbackCanvas.sortingOrder = 9200;
        feedbackRoot = (RectTransform)host.transform;
        CanvasScaler scaler = host.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(2340f, 1080f);
        scaler.matchWidthOrHeight = 1f;
        CanvasGroup group = host.GetComponent<CanvasGroup>();
        group.blocksRaycasts = false;
        group.interactable = false;
        screenEdge = CreateGraphic("HealthScreenEdge", MapFeedbackGraphic.Shape.Vignette, feedbackRoot);
        screenEdge.rectTransform.anchorMin = Vector2.zero;
        screenEdge.rectTransform.anchorMax = Vector2.one;
        screenEdge.rectTransform.offsetMin = screenEdge.rectTransform.offsetMax = Vector2.zero;
        screenEdge.color = Color.clear;
        coinSprite = Resources.Load<Sprite>("MapUI/FeedbackCoin") ?? MapForestArt.Get("Coins");
        Canvas.ForceUpdateCanvases();
    }

    private void BeginCoins(int count, long amount, Vector2 origin)
    {
        liveCoins = count;
        for (int i = 0; i < count; i++)
        {
            if (coins.Count <= i)
            {
                Image image = NewRect("FlyingCoin_" + i, feedbackRoot, new Vector2(52f, 52f)).gameObject.AddComponent<Image>();
                image.sprite = coinSprite;
                image.preserveAspect = true;
                image.raycastTarget = false;
                MapFeedbackGraphic glint = CreateGraphic("CoinGlint", MapFeedbackGraphic.Shape.Spark, image.rectTransform);
                glint.rectTransform.sizeDelta = new Vector2(58f, 58f);
                coins.Add(new CoinView { image = image, glint = glint });
            }
            CoinView coin = coins[i];
            coin.image.gameObject.SetActive(true);
            coin.image.rectTransform.SetAsLastSibling();
            coin.image.color = Color.clear;
            coin.glint.color = Color.clear;
            coin.image.rectTransform.anchoredPosition = LocalPosition(origin);
            coin.delay = i * 0.027f;
            coin.origin = origin;
            coin.arrived = false;
            coin.value = (i + 1L) * amount / count - i * amount / count;
            float angle = i * 2.399963f;
            coin.scatter = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (24f + i % 4 * 13f);
        }
        SpawnRing(origin, Gold);
    }

    private bool AdvanceCoins()
    {
        bool allArrived = true;
        Vector2 destination = LocalPosition(WalletPosition());
        for (int i = 0; i < liveCoins; i++)
        {
            CoinView coin = coins[i];
            if (coin.arrived) continue;
            float age = moneyAge - coin.delay;
            if (age < CoinFlightSeconds) allArrived = false;
            if (age < 0f) continue;
            if (age >= CoinFlightSeconds)
            {
                coin.arrived = true;
                coin.image.gameObject.SetActive(false);
                displayedMoney += coin.value;
                walletPulse = 0.65f;
                Vector2 position = WalletPosition();
                SpawnRing(position, Gold);
                SpawnSpark(position, i);
                PlaySound("CoinArrival", 0.97f + i * 0.018f, 0.34f);
                continue;
            }
            float scatterT = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(age / 0.17f));
            Vector2 start = LocalPosition(coin.origin), scattered = start + coin.scatter;
            float fly = Mathf.Clamp01((age - 0.13f) / (CoinFlightSeconds - 0.13f));
            float curve = fly * fly;
            Vector2 path = Vector2.Lerp(Vector2.Lerp(start, scattered, scatterT), destination, curve);
            path.y += Mathf.Sin(fly * Mathf.PI) * (45f + i % 3 * 16f);
            RectTransform rect = coin.image.rectTransform;
            rect.anchoredPosition = path;
            float size = Mathf.Lerp(0.56f, 1f, scatterT) * Mathf.Lerp(1f, 0.64f, fly);
            rect.localScale = new Vector3(size * (0.77f + 0.23f * Mathf.Abs(Mathf.Cos(age * 11f + i))), size, 1f);
            rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(age * 8f + i) * 22f);
            coin.image.color = new Color(1f, 1f, 1f, Mathf.Clamp01(age / 0.065f));
            coin.glint.color = new Color(1f, 0.91f, 0.62f, 0.18f + 0.24f * Mathf.Pow(Mathf.Max(0f, Mathf.Sin(age * 16f + i)), 6f));
        }
        return allArrived;
    }

    private void SpawnPopup(Vector2 origin, string text, Color tint)
    {
        Decoration item = GetDecoration(true);
        TMP_Text label = (TMP_Text)item.graphic;
        label.font = wallet != null ? wallet.font : TMP_Settings.defaultFontAsset;
        label.text = text;
        label.fontSize = 34;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Center;
        label.enableWordWrapping = false;
        label.outlineWidth = 0.14f;
        label.outlineColor = new Color(0.06f, 0.05f, 0.02f, 0.75f);
        item.root.sizeDelta = new Vector2(300f, 60f);
        ActivateDecoration(item, origin, tint, new Vector2(0f, 48f), 0.85f, false);
    }

    private void SpawnRing(Vector2 origin, Color tint)
    {
        Decoration item = GetDecoration(false);
        ((MapFeedbackGraphic)item.graphic).shape = MapFeedbackGraphic.Shape.Ring;
        item.root.sizeDelta = new Vector2(60f, 60f);
        ActivateDecoration(item, origin, tint, Vector2.zero, 0.32f, true);
    }

    private void SpawnSpark(Vector2 origin, int index)
    {
        Decoration item = GetDecoration(false);
        ((MapFeedbackGraphic)item.graphic).shape = MapFeedbackGraphic.Shape.Spark;
        item.root.sizeDelta = new Vector2(30f, 30f);
        float angle = index * 2.4f;
        ActivateDecoration(item, origin, Gold, new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 58f, 0.32f, false);
    }

    private Decoration GetDecoration(bool text)
    {
        Decoration free = decorations.Find(x => !x.root.gameObject.activeSelf && (x.graphic is TMP_Text) == text);
        if (free != null) return free;
        // Recycle the oldest of the same kind during large multi-member or burst updates.
        int count = 0;
        Decoration oldest = null;
        foreach (Decoration item in decorations)
            if ((item.graphic is TMP_Text) == text)
            {
                count++;
                if (oldest == null || item.age > oldest.age) oldest = item;
            }
        if (count >= (text ? 12 : 28)) return oldest;
        RectTransform rect = NewRect(text ? "ResourceDelta" : "ResourceSpark", feedbackRoot, Vector2.one);
        Graphic graphic = text ? (Graphic)rect.gameObject.AddComponent<TextMeshProUGUI>() : rect.gameObject.AddComponent<MapFeedbackGraphic>();
        graphic.raycastTarget = false;
        Decoration created = new Decoration { root = rect, graphic = graphic };
        decorations.Add(created);
        return created;
    }

    private void ActivateDecoration(Decoration item, Vector2 origin, Color tint, Vector2 drift, float lifetime, bool ring)
    {
        item.age = 0f;
        item.lifetime = lifetime;
        item.origin = origin;
        item.drift = drift;
        item.tint = tint;
        item.ring = ring;
        item.root.gameObject.SetActive(true);
        item.root.SetAsLastSibling();
        item.root.anchoredPosition = LocalPosition(origin);
        item.root.localScale = Vector3.one;
        item.graphic.color = tint;
        item.graphic.SetVerticesDirty();
    }

    private void FlashScreen(bool healing, float severity)
    {
        float strength = healing ? 0.12f : Mathf.Lerp(0.16f, 0.3f, Mathf.Clamp01(severity));
        // Damage wins over simultaneous healing, rather than flashing alternating colours.
        if (screenFlashAge < 0.2f && screenFlashStrength > 0f && screenFlashColor == Loss && healing) return;
        screenFlashColor = healing ? Healing : Loss;
        screenFlashStrength = strength;
        screenFlashAge = 0f;
    }

    private void AdvanceDecorations(float seconds)
    {
        if (feedbackCanvas == null) return;
        foreach (Decoration item in decorations)
        {
            if (!item.root.gameObject.activeSelf) continue;
            item.age += seconds;
            float t = Mathf.Clamp01(item.age / item.lifetime);
            if (t >= 1f) { item.root.gameObject.SetActive(false); continue; }
            item.root.anchoredPosition = LocalPosition(item.origin) + item.drift * Mathf.SmoothStep(0f, 1f, t);
            item.root.localScale = Vector3.one * (item.ring ? Mathf.Lerp(0.65f, 1.9f, t) : 1f + Mathf.Sin(t * Mathf.PI) * 0.07f);
            item.graphic.color = new Color(item.tint.r, item.tint.g, item.tint.b, item.tint.a * (1f - t) * Mathf.Clamp01((1f - t) * 3f));
        }
        screenFlashAge += seconds;
        float flash = Mathf.Sin(Mathf.Clamp01(screenFlashAge / 0.5f) * Mathf.PI);
        screenEdge.color = new Color(screenFlashColor.r, screenFlashColor.g, screenFlashColor.b, flash * screenFlashStrength);
        if (screenFlashAge >= 0.5f) screenFlashStrength = 0f;
    }

    private Vector2 WalletPosition() => wallet != null
        ? MapFeedbackOrigin.NormalizedPosition(wallet.rectTransform, wallet.rectTransform.rect.center) : new Vector2(0.09f, 0.8f);

    private Vector2 LocalPosition(Vector2 normalized) => new Vector2(
        (normalized.x - 0.5f) * feedbackRoot.rect.width, (normalized.y - 0.5f) * feedbackRoot.rect.height);

    private static RectTransform NewRect(string name, Transform parent, Vector2 size)
    {
        RectTransform rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        return rect;
    }

    private static MapFeedbackGraphic CreateGraphic(string name, MapFeedbackGraphic.Shape shape, Transform parent)
    {
        MapFeedbackGraphic graphic = NewRect(name, parent, Vector2.one).gameObject.AddComponent<MapFeedbackGraphic>();
        graphic.shape = shape;
        graphic.raycastTarget = false;
        return graphic;
    }

    private void ClearDecorations()
    {
        foreach (CoinView coin in coins) if (coin.image != null) coin.image.gameObject.SetActive(false);
        foreach (Decoration item in decorations) if (item.root != null) item.root.gameObject.SetActive(false);
        liveCoins = 0;
        screenFlashStrength = 0f;
        if (screenEdge != null) screenEdge.color = Color.clear;
    }
}
