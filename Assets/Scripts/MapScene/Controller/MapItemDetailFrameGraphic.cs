using UnityEngine;
using UnityEngine.UI;

/// <summary>Item-specific title, metadata and quiet display engraving, using the shared forest grain.</summary>
[RequireComponent(typeof(CanvasRenderer))]
public sealed class MapItemDetailFrameGraphic : MapMerchantFrameGraphic
{
    public enum DetailStyle { Title, Engraving, Rarity, Category }
    public DetailStyle detailStyle;
    public Color accent = Color.gray;
    public bool electricElement;
    public float glow;
    private static readonly Color Gold = new Color32(197, 157, 78, 255);

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        Rect r = GetPixelAdjustedRect();
        if (r.width <= 0 || r.height <= 0) return;
        if (detailStyle == DetailStyle.Title)
        {
            Surface(vh, r, 30, new Color32(70, 46, 24, 255), new Color32(31, 23, 14, 255));
            Band(vh, Inset(r, 1), 30, 4, new Color32(240, 198, 109, 255), true);
            Band(vh, Inset(r, 8), 25, 1.5f, Gold, true);
            Gem(vh, new Vector2(r.xMin + 25, r.center.y), 9);
            Gem(vh, new Vector2(r.xMax - 25, r.center.y), 9);
            return;
        }
        if (detailStyle == DetailStyle.Rarity || detailStyle == DetailStyle.Category)
        {
            Color edge = detailStyle == DetailStyle.Rarity ? Color.Lerp(accent, Color.white, 0.24f)
                : electricElement ? new Color32(68, 163, 157, 255) : Gold;
            Surface(vh, r, 8, Color.Lerp(new Color32(24, 31, 25, 255), edge, 0.12f), new Color32(12, 23, 17, 255));
            Band(vh, Inset(r, 1), 8, 1.5f, edge, true);
            Band(vh, Inset(r, 5), 5, 0.7f, Shade(edge, 0.5f));
            // ResourceKeywordText supplies the actual attribute icon in the label.
            return;
        }
        Vector2 c = r.center;
        float radius = Mathf.Min(r.width, r.height) * 0.44f;
        Color faint = new Color(0.65f, 0.50f, 0.22f, 0.29f + glow * 0.12f);
        for (int i = 0; i < 96; i++)
        {
            float a = i * Mathf.PI / 48, b = (i + 1) * Mathf.PI / 48;
            Vector2 p = new Vector2(Mathf.Cos(a), Mathf.Sin(a)), q = new Vector2(Mathf.Cos(b), Mathf.Sin(b));
            Stroke(vh, c + p * radius, c + q * radius, 1, faint);
            Stroke(vh, c + p * radius * 0.88f, c + q * radius * 0.88f, 0.7f, Shade(faint, 0.7f));
            if (i % 24 == 0)
            {
                Diamond(vh, c + p * radius, 5, faint);
                Stroke(vh, c + p * radius * 1.03f, c + p * radius * 1.1f, 0.9f, faint);
            }
        }
    }

    private void Gem(VertexHelper vh, Vector2 p, float size)
    {
        Diamond(vh, p, size + 3, Gold);
        Diamond(vh, p, size + 1, new Color32(16, 31, 22, 255));
        Diamond(vh, p, size - 1, new Color32(42, 158, 134, 255));
        Stroke(vh, p + Vector2.up * (size - 2), p + Vector2.left * (size - 2), 1.5f, new Color32(154, 232, 199, 255));
    }
}
