using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(BoxCollider))]
public class BoardTileView : MonoBehaviour
{
    private BoardManager owner;
    private BoardTileData tileData;
    private SpriteRenderer baseRenderer;
    private SpriteRenderer innerGlowRenderer;
    private SpriteRenderer highlightRenderer;
    private SpriteRenderer frameRenderer;
    private SpriteMask iconGlowMask;
    private SpriteRenderer iconGlowRenderer;
    private SpriteRenderer iconRenderer;
    private TextMeshPro mainLabel;
    private TextMeshPro previewLabel;
    private BoxCollider boxCollider;

    public BoardTileData TileData => tileData;

    public bool TryGetScreenHighlightRect(Camera targetCamera, out Rect screenRect, float paddingPixels = 18f)
    {
        screenRect = default;
        if (targetCamera == null)
            return false;

        EnsureVisuals();
        Bounds bounds = boxCollider != null
            ? boxCollider.bounds
            : new Bounds(transform.position, transform.lossyScale);

        Vector3 min = bounds.min;
        Vector3 max = bounds.max;
        Vector3[] corners =
        {
            new Vector3(min.x, min.y, min.z),
            new Vector3(min.x, min.y, max.z),
            new Vector3(min.x, max.y, min.z),
            new Vector3(min.x, max.y, max.z),
            new Vector3(max.x, min.y, min.z),
            new Vector3(max.x, min.y, max.z),
            new Vector3(max.x, max.y, min.z),
            new Vector3(max.x, max.y, max.z)
        };

        float minX = float.PositiveInfinity;
        float minY = float.PositiveInfinity;
        float maxX = float.NegativeInfinity;
        float maxY = float.NegativeInfinity;
        bool hasVisibleCorner = false;
        for (int i = 0; i < corners.Length; i++)
        {
            Vector3 screenPoint = targetCamera.WorldToScreenPoint(corners[i]);
            if (screenPoint.z < 0f)
                continue;

            hasVisibleCorner = true;
            minX = Mathf.Min(minX, screenPoint.x);
            minY = Mathf.Min(minY, screenPoint.y);
            maxX = Mathf.Max(maxX, screenPoint.x);
            maxY = Mathf.Max(maxY, screenPoint.y);
        }

        if (!hasVisibleCorner)
            return false;

        float padding = Mathf.Max(0f, paddingPixels);
        screenRect = Rect.MinMaxRect(minX - padding, minY - padding, maxX + padding, maxY + padding);
        return screenRect.width > 0.5f && screenRect.height > 0.5f;
    }

    public void Setup(BoardManager boardManager, BoardTileData data)
    {
        owner = boardManager;
        tileData = data;
        EnsureVisuals();
        Refresh();
    }

    public void Refresh()
    {
        if (owner == null || tileData == null)
            return;

        EnsureVisuals();

        transform.position = owner.GridToWorld(tileData.coord);
        transform.localScale = new Vector3(owner.TileVisualSize.x, owner.TileVisualSize.y, 1f);

        if (baseRenderer != null)
        {
            baseRenderer.sprite = owner.DefaultTileSprite;
            baseRenderer.color = owner.GetTileBaseColor(tileData);
            baseRenderer.sortingOrder = owner.TileSortingOrder;
        }

        if (innerGlowRenderer != null)
        {
            innerGlowRenderer.sprite = owner.TileInnerGlowSprite;
            innerGlowRenderer.color = owner.GetTileInnerGlowColor(tileData);
            innerGlowRenderer.sortingOrder = owner.TileSortingOrder + 1;
            innerGlowRenderer.transform.localScale = owner.TileInnerGlowLocalScale;
            innerGlowRenderer.gameObject.SetActive(!tileData.isVisited || owner.ShouldKeepPersistentPresentation(tileData));
        }

        if (highlightRenderer != null)
        {
            highlightRenderer.sprite = owner.DefaultTileSprite;
            highlightRenderer.color = owner.GetTileHighlightColor(tileData);
            highlightRenderer.sortingOrder = owner.TileSortingOrder - 2;
            highlightRenderer.gameObject.SetActive(owner.ShouldHighlightTile(tileData));
        }

        if (frameRenderer != null)
        {
            Sprite frameSprite = owner.TileFrameSprite;
            frameRenderer.sprite = frameSprite;
            frameRenderer.color = owner.GetTileFrameColor(tileData);
            frameRenderer.sortingOrder = owner.TileSortingOrder + 3;
            frameRenderer.transform.localPosition = owner.TileFrameLocalOffset;
            frameRenderer.transform.localScale = owner.TileFrameLocalScale;
            frameRenderer.gameObject.SetActive(frameSprite != null);
        }

        if (iconGlowMask != null)
        {
            Sprite maskSprite = owner.TileIconGlowMaskSprite;
            iconGlowMask.sprite = maskSprite;
            iconGlowMask.transform.localPosition = owner.TileIconGlowMaskLocalOffset;
            iconGlowMask.transform.localScale = owner.TileIconGlowMaskLocalScale;
            iconGlowMask.frontSortingOrder = owner.TileSortingOrder + 5;
            iconGlowMask.backSortingOrder = owner.TileSortingOrder + 3;
            iconGlowMask.gameObject.SetActive(maskSprite != null);
        }

        Sprite iconSprite = owner.GetTileIcon(tileData);
        if (iconGlowRenderer != null)
        {
            iconGlowRenderer.sprite = owner.TileInnerGlowSprite;
            iconGlowRenderer.color = owner.GetTileIconGlowColor(tileData);
            iconGlowRenderer.sortingOrder = owner.TileSortingOrder + 4;
            iconGlowRenderer.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
            iconGlowRenderer.transform.localPosition = owner.TileIconGlowLocalOffset;
            iconGlowRenderer.transform.localScale = owner.TileIconGlowLocalScale;
            iconGlowRenderer.gameObject.SetActive(iconSprite != null);
        }

        if (iconRenderer != null)
        {
            iconRenderer.sprite = iconSprite != null ? iconSprite : owner.DefaultTileSprite;
            iconRenderer.color = iconSprite != null ? owner.IconTintColor : owner.GetFallbackIconColor(tileData);
            iconRenderer.sortingOrder = owner.TileSortingOrder + 5;
            iconRenderer.transform.localPosition = owner.TileIconLocalOffset;
            iconRenderer.transform.localScale = owner.TileIconLocalScale;
            iconRenderer.gameObject.SetActive(iconSprite != null);
        }

        if (mainLabel != null)
        {
            if (owner.WorldTextFont != null)
                mainLabel.font = owner.WorldTextFont;
            mainLabel.text = owner.GetTilePrimaryText(tileData, iconSprite == null);
            mainLabel.color = owner.GetTilePrimaryTextColor(tileData);
            mainLabel.gameObject.SetActive(!string.IsNullOrEmpty(mainLabel.text));
        }

        if (previewLabel != null)
        {
            if (owner.WorldTextFont != null)
                previewLabel.font = owner.WorldTextFont;
            previewLabel.text = owner.GetTilePreviewText(tileData);
            previewLabel.color = owner.GetTilePreviewTextColor(tileData);
            previewLabel.gameObject.SetActive(!string.IsNullOrEmpty(previewLabel.text));
        }
    }

    private void EnsureVisuals()
    {
        if (baseRenderer == null)
        {
            if (!TryGetComponent(out baseRenderer))
                baseRenderer = gameObject.AddComponent<SpriteRenderer>();
        }

        if (boxCollider == null || !boxCollider)
            boxCollider = GetComponent<BoxCollider>();
        if (boxCollider == null || !boxCollider)
            boxCollider = gameObject.AddComponent<BoxCollider>();

        boxCollider.size = new Vector3(1f, 1f, 0.2f);
        boxCollider.center = Vector3.zero;

        if (highlightRenderer == null)
        {
            GameObject go = new GameObject("Highlight");
            go.transform.SetParent(transform, false);
            highlightRenderer = go.AddComponent<SpriteRenderer>();
            go.transform.localScale = new Vector3(1.12f, 1.12f, 1f);
        }

        if (innerGlowRenderer == null)
        {
            GameObject go = new GameObject("InnerGlow");
            go.transform.SetParent(transform, false);
            innerGlowRenderer = go.AddComponent<SpriteRenderer>();
            go.transform.localScale = Vector3.one;
        }

        if (frameRenderer == null)
        {
            GameObject go = new GameObject("Frame");
            go.transform.SetParent(transform, false);
            frameRenderer = go.AddComponent<SpriteRenderer>();
            go.transform.localScale = Vector3.one;
        }

        if (iconGlowMask == null)
        {
            GameObject go = new GameObject("IconGlowMask");
            go.transform.SetParent(transform, false);
            iconGlowMask = go.AddComponent<SpriteMask>();
            go.transform.localScale = Vector3.one;
        }

        if (iconGlowRenderer == null)
        {
            GameObject go = new GameObject("IconGlow");
            go.transform.SetParent(transform, false);
            iconGlowRenderer = go.AddComponent<SpriteRenderer>();
            iconGlowRenderer.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
            go.transform.localScale = Vector3.one;
        }

        if (iconRenderer == null)
        {
            GameObject go = new GameObject("Icon");
            go.transform.SetParent(transform, false);
            iconRenderer = go.AddComponent<SpriteRenderer>();
            go.transform.localPosition = Vector3.zero;
            go.transform.localScale = Vector3.one;
        }

        if (mainLabel == null)
            mainLabel = CreateText("MainLabel", new Vector3(0f, 0.08f, 0f), 4.2f, FontStyles.Bold);

        if (previewLabel == null)
            previewLabel = CreateText("PreviewLabel", new Vector3(0f, -0.28f, 0f), 2.3f, FontStyles.Normal);
    }

    private TextMeshPro CreateText(string name, Vector3 localPosition, float fontSize, FontStyles fontStyle)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(transform, false);
        go.transform.localPosition = localPosition;
        TextMeshPro text = go.AddComponent<TextMeshPro>();
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.enableWordWrapping = false;
        text.sortingOrder = owner != null ? owner.TileSortingOrder + 2 : 2;
        RectTransform rect = text.rectTransform;
        rect.sizeDelta = new Vector2(4f, 2f);
        return text;
    }
}
