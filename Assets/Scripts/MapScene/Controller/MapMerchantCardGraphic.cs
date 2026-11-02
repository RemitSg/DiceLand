using UnityEngine;
using UnityEngine.UI;

/// <summary>Bevelled stock card; its selection frame does not tint item names.</summary>
public sealed class MapMerchantCardGraphic : MapMerchantFrameGraphic
{
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        Rect r = GetPixelAdjustedRect();
        if (r.width <= 0 || r.height <= 0) return;
        Color top = selected ? new Color(0.095f, 0.25f, 0.16f) : new Color(0.12f, 0.105f, 0.074f);
        Color bottom = selected ? new Color(0.046f, 0.14f, 0.086f) : new Color(0.047f, 0.049f, 0.034f);
        if (!available) { top = Shade(top, 0.60f); bottom = Shade(bottom, 0.60f); }
        Surface(vh, r, 10, top, bottom);
        Color brass = selected ? new Color(1f, 0.79f, 0.35f) : new Color(0.57f, 0.43f, 0.25f);
        Band(vh, Inset(r, 0.7f), 10, selected ? 3.1f : 2.1f, brass, !selected);
        Band(vh, Inset(r, 4.5f), 7, 1, selected ? new Color(0.79f, 0.70f, 0.39f) : new Color(0.23f, 0.23f, 0.14f));
        CornerCaps(vh, Inset(r, 6), 14, selected ? new Color(1f, 0.87f, 0.48f) : new Color(0.70f, 0.53f, 0.31f, 0.86f));
        if (!selected) return;
        Vector2 center = new Vector2(r.xMax - 25, r.yMax - 25);
        Disc(vh, center, 19, new Color(0.045f, 0.19f, 0.13f));
        Color mark = new Color(0.58f, 0.91f, 0.75f);
        for (int i = 0; i < 32; i++)
        {
            float a = i * Mathf.PI / 16f, b = (i + 1) * Mathf.PI / 16f;
            Stroke(vh, center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 15.5f, center + new Vector2(Mathf.Cos(b), Mathf.Sin(b)) * 15.5f, 1.8f, mark);
        }
        Stroke(vh, center + new Vector2(-7f, 0), center + new Vector2(-2f, -5f), 2.6f, mark);
        Stroke(vh, center + new Vector2(-2f, -5f), center + new Vector2(8f, 7f), 2.6f, mark);
    }
}
