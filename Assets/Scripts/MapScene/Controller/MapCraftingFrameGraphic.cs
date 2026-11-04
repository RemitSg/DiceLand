using UnityEngine;
using UnityEngine.UI;

/// <summary>Forest brass, oak, jade and engraved forge surfaces, sharing the map's native grain.</summary>
[RequireComponent(typeof(CanvasRenderer))]
public sealed class MapCraftingFrameGraphic : MapMerchantFrameGraphic
{
    public new enum Style { Window, Title, Blueprint, Material, IconWell, Preview, Engraving, Badge, Action, Secondary }
    public Style craftingStyle;
    public float glow;
    private static readonly Color Brass = new Color32(186, 138, 60, 255);
    private static readonly Color LightGold = new Color32(245, 204, 113, 255);

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        Rect r = GetPixelAdjustedRect();
        if (r.width <= 0 || r.height <= 0) return;
        switch (craftingStyle)
        {
            case Style.Window:
                Surface(vh, r, 16, new Color32(13, 29, 19, 255), new Color32(6, 18, 12, 255));
                Band(vh, Inset(r, 1), 16, 9, new Color32(47, 29, 12, 255));
                Band(vh, Inset(r, 3), 14, 3.8f, LightGold, true);
                Band(vh, Inset(r, 9), 9, 2.5f, Brass, true);
                Band(vh, Inset(r, 15), 6, 1, new Color32(93, 84, 41, 255));
                Corners(vh, Inset(r, 14), 32, LightGold);
                for (int side = -1; side <= 1; side += 2)
                {
                    Vector2 top = new Vector2(side < 0 ? r.xMin + 16 : r.xMax - 16, r.yMax - 14);
                    Leaves(vh, top, new Vector2(-side, -0.32f), 1.1f);
                    Vector2 bottom = new Vector2(side < 0 ? r.xMin + 16 : r.xMax - 16, r.yMin + 14);
                    Leaves(vh, bottom, new Vector2(-side, 0.24f), 0.85f);
                }
                break;
            case Style.Title:
                Surface(vh, r, 44, new Color32(67, 43, 21, 255), new Color32(32, 22, 13, 255));
                Band(vh, Inset(r, 1), 44, 5, LightGold, true);
                Band(vh, Inset(r, 8), 40, 1.7f, Brass, true);
                Band(vh, Inset(r, 13), 35, 1, new Color32(125, 82, 36, 255));
                Gem(vh, new Vector2(r.xMin + 28, r.center.y), 13);
                Gem(vh, new Vector2(r.xMax - 28, r.center.y), 13);
                Anvil(vh, new Vector2(r.center.x, r.yMax + 15), 72, new Color32(117, 102, 72, 255));
                Leaves(vh, new Vector2(r.center.x - 18, r.yMax + 3), new Vector2(-1, 0.28f), 0.65f);
                Leaves(vh, new Vector2(r.center.x + 18, r.yMax + 3), new Vector2(1, 0.28f), 0.65f);
                break;
            case Style.Blueprint:
                Surface(vh, r, 8, selected ? new Color32(251, 211, 116, 255) : new Color32(31, 48, 31, 255),
                    selected ? new Color32(166, 115, 35, 255) : new Color32(12, 27, 18, 255));
                Band(vh, Inset(r, 1), 8, selected ? 3 : 1.5f, selected ? LightGold : Brass, true);
                Band(vh, Inset(r, 6), 5, 1, selected ? new Color32(255, 228, 157, 255) : new Color32(71, 75, 42, 255));
                CornerCaps(vh, Inset(r, 5), 12, Brass);
                if (selected) Gem(vh, new Vector2(r.xMax - 18, r.center.y), 8);
                break;
            case Style.Material:
            case Style.IconWell:
                Surface(vh, r, 9, new Color32(26, 42, 26, 255), new Color32(11, 25, 16, 255));
                Band(vh, Inset(r, 1), 8, 1.4f, Brass, true);
                Band(vh, Inset(r, 5), 5, 0.8f, new Color32(74, 79, 43, 255));
                CornerCaps(vh, Inset(r, 5), 12, new Color32(201, 162, 78, 230));
                break;
            case Style.Preview:
                Surface(vh, r, 12, new Color32(13, 34, 23, 255), new Color32(10, 29, 20, 255));
                Corners(vh, Inset(r, 5), 24, new Color32(100, 94, 46, 125));
                break;
            case Style.Engraving:
                Engraving(vh, r);
                break;
            case Style.Badge:
                Surface(vh, r, 9, new Color32(28, 42, 24, 255), new Color32(13, 28, 17, 255));
                Band(vh, Inset(r, 1), 8, 1.3f, Brass, true);
                break;
            case Style.Action:
                Surface(vh, r, 10, available ? new Color32(46, 97, 65, 255) : new Color32(49, 62, 49, 255),
                    available ? new Color32(19, 58, 36, 255) : new Color32(30, 43, 32, 255));
                ButtonTrim(vh, r, available ? LightGold : new Color32(129, 130, 98, 255));
                break;
            case Style.Secondary:
                Surface(vh, r, 10, new Color32(246, 213, 151, 255), new Color32(180, 133, 65, 255));
                ButtonTrim(vh, r, LightGold);
                break;
        }
    }

    private void ButtonTrim(VertexHelper vh, Rect r, Color gold)
    {
        Band(vh, Inset(r, 1), 10, 3, new Color32(69, 45, 19, 255));
        Band(vh, Inset(r, 3), 8, 2.3f, gold, true);
        Band(vh, Inset(r, 8), 5, 0.9f, gold * new Color(0.7f, 0.7f, 0.7f, 1));
        CornerCaps(vh, Inset(r, 12), 12, gold);
    }

    private void Engraving(VertexHelper vh, Rect r)
    {
        Vector2 c = r.center;
        float radius = Mathf.Min(r.width * 0.46f, r.height * 0.47f);
        Color faint = new Color(0.65f, 0.51f, 0.24f, 0.24f + glow * 0.35f);
        int first = vh.currentVertCount;
        Vertex(vh, c, new Color(0.18f, 0.43f, 0.28f, 0.20f + glow * 0.2f));
        for (int i = 0; i <= 64; i++)
        {
            float a = i * Mathf.PI / 32;
            Vertex(vh, c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius * 1.15f, Color.clear);
            if (i > 0) vh.AddTriangle(first, first + i, first + i + 1);
        }
        for (int i = 0; i < 96; i++)
        {
            float a = i * Mathf.PI / 48, b = (i + 1) * Mathf.PI / 48;
            Vector2 p = new Vector2(Mathf.Cos(a), Mathf.Sin(a)), q = new Vector2(Mathf.Cos(b), Mathf.Sin(b));
            Stroke(vh, c + p * radius, c + q * radius, 1.1f, faint);
            Stroke(vh, c + p * radius * 0.88f, c + q * radius * 0.88f, 0.7f, faint * new Color(1, 1, 1, 0.6f));
            if (i % 4 == 0) Stroke(vh, c + p * radius * 1.02f, c + p * radius * 1.055f, 1, faint);
            if (i % 24 == 0) Diamond(vh, c + p * radius, 7, faint);
        }
        Anvil(vh, c + new Vector2(0, -radius * 0.62f), 86, new Color(0.44f, 0.46f, 0.24f, 0.16f));
    }

    private void Gem(VertexHelper vh, Vector2 p, float radius)
    {
        Diamond(vh, p, radius + 3, LightGold); Diamond(vh, p, radius + 1, new Color32(26, 35, 23, 255));
        Diamond(vh, p, radius - 1, new Color32(37, 153, 129, 255));
        Stroke(vh, p + Vector2.up * (radius - 2), p + Vector2.left * (radius - 2), 1.8f, new Color32(158, 238, 201, 255));
    }

    private void Leaves(VertexHelper vh, Vector2 origin, Vector2 axis, float size)
    {
        axis.Normalize();
        Vector2 side = new Vector2(-axis.y, axis.x);
        Stroke(vh, origin, origin + axis * 108 * size, 1.8f, new Color32(114, 119, 43, 255));
        for (int i = 0; i < 7; i++)
        {
            Vector2 start = origin + axis * (i * 13 + 2) * size;
            Vector2 leaf = (axis * 22 + side * (i % 2 == 0 ? 25 : -22)) * size;
            Vector2 n = new Vector2(-leaf.y, leaf.x).normalized * 7 * size;
            int v = vh.currentVertCount;
            Vertex(vh, start, new Color32(34, 61, 24, 255));
            Vertex(vh, start + leaf * 0.45f + n, new Color32(92, 117, 37, 255));
            Vertex(vh, start + leaf, new Color32(153, 163, 66, 255));
            Vertex(vh, start + leaf * 0.45f - n, new Color32(55, 87, 28, 255));
            vh.AddTriangle(v, v + 1, v + 2); vh.AddTriangle(v, v + 2, v + 3);
            Stroke(vh, start, start + leaf * 0.85f, 0.8f, new Color32(148, 154, 57, 255));
        }
    }

    private void Anvil(VertexHelper vh, Vector2 c, float width, Color tint)
    {
        Vector2[] points = { new Vector2(-0.55f, 0.20f), new Vector2(0.50f, 0.20f), new Vector2(0.36f, -0.02f),
            new Vector2(0.12f, -0.07f), new Vector2(0.10f, -0.28f), new Vector2(0.28f, -0.34f),
            new Vector2(0.30f, -0.43f), new Vector2(-0.31f, -0.43f), new Vector2(-0.28f, -0.34f),
            new Vector2(-0.10f, -0.28f), new Vector2(-0.12f, -0.07f), new Vector2(-0.40f, 0.01f) };
        for (int i = 0; i < points.Length; i++)
            Stroke(vh, c + points[i] * width, c + points[(i + 1) % points.Length] * width, 3.5f, tint);
        Stroke(vh, c + new Vector2(-0.5f, 0.21f) * width, c + new Vector2(0.48f, 0.21f) * width, 2, Shade(tint, 1.6f));
    }
}
