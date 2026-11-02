using UnityEngine;
using UnityEngine.UI;

/// <summary>Merchant surfaces with aged brass bevels, quiet grain, and restrained corner ornaments.</summary>
[RequireComponent(typeof(CanvasRenderer))]
public class MapMerchantFrameGraphic : MaskableGraphic
{
    public enum Style { Panel, Inset, IconWell, Badge, Wallet, SecondaryButton, GoldButton, Divider }
    public Style style;
    public bool jadeFace;
    public bool leadingDiamond;
    protected bool selected, available = true;
    private static Texture2D surfaceGrain;

    public override Texture mainTexture => Grain;

    public void SetState(bool isSelected, bool isAvailable)
    {
        selected = isSelected;
        available = isAvailable;
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        Rect r = GetPixelAdjustedRect();
        if (r.width <= 0 || r.height <= 0) return;
        switch (style)
        {
            case Style.Panel:
                Surface(vh, r, 13, new Color(0.050f, 0.112f, 0.070f), new Color(0.027f, 0.063f, 0.039f));
                Band(vh, Inset(r, 1), 12, 4, new Color(0.15f, 0.11f, 0.055f));
                Band(vh, Inset(r, 3), 11, 2.2f, new Color(0.72f, 0.55f, 0.29f), true);
                Band(vh, Inset(r, 8), 8, 1.2f, new Color(0.32f, 0.29f, 0.16f));
                Band(vh, Inset(r, 13), 5, 1, new Color(0.71f, 0.59f, 0.33f));
                Corners(vh, Inset(r, 19), 20, new Color(0.78f, 0.64f, 0.35f));
                Sprig(vh, new Vector2(r.xMin + 24, r.yMax - 62), 1);
                Sprig(vh, new Vector2(r.xMax - 24, r.yMin + 62), -1);
                break;
            case Style.IconWell:
                Surface(vh, r, 7, selected ? new Color(0.078f, 0.18f, 0.12f) : new Color(0.105f, 0.115f, 0.083f),
                    new Color(0.030f, 0.044f, 0.031f));
                Band(vh, Inset(r, 0.5f), 7, 1, new Color(0.39f, 0.37f, 0.24f, 0.72f), true);
                Band(vh, Inset(r, 4), 4, 0.7f, new Color(0.19f, 0.25f, 0.16f, 0.65f));
                CornerCaps(vh, Inset(r, 4), 9, new Color(0.42f, 0.43f, 0.27f, 0.55f));
                break;
            case Style.Badge:
                Surface(vh, r, 4, new Color(0.13f, 0.14f, 0.10f, 0.76f), new Color(0.075f, 0.086f, 0.060f, 0.76f));
                Band(vh, Inset(r, 0.5f), 4, 0.9f, new Color(0.49f, 0.47f, 0.32f, 0.65f), true);
                break;
            case Style.GoldButton:
                Surface(vh, r, 10, new Color(0.98f, 0.79f, 0.42f), new Color(0.69f, 0.46f, 0.18f));
                Band(vh, Inset(r, 1), 10, 3, new Color(0.35f, 0.22f, 0.075f));
                Band(vh, Inset(r, 3), 8, 2.3f, new Color(1f, 0.86f, 0.53f), true);
                Band(vh, Inset(r, 7), 5, 1, new Color(0.51f, 0.32f, 0.10f));
                Band(vh, Inset(r, 10), 4, 0.8f, new Color(1f, 0.82f, 0.43f, 0.82f));
                CornerCaps(vh, Inset(r, 13), r.width > 100 ? 13 : 8, new Color(0.61f, 0.40f, 0.15f, 0.86f));
                break;
            case Style.SecondaryButton:
                Surface(vh, r, 9, jadeFace ? new Color(0.095f, 0.24f, 0.15f) : new Color(0.24f, 0.19f, 0.115f),
                    jadeFace ? new Color(0.034f, 0.11f, 0.066f) : new Color(0.12f, 0.091f, 0.052f));
                Band(vh, Inset(r, 1), 9, 2, new Color(0.49f, 0.38f, 0.20f), true);
                Band(vh, Inset(r, 5), 6, 0.9f, new Color(0.73f, 0.60f, 0.32f, 0.85f));
                CornerCaps(vh, Inset(r, 7), 12, new Color(0.77f, 0.65f, 0.37f));
                break;
            case Style.Divider:
                Divider(vh, r);
                break;
            default:
                Surface(vh, r, 9, new Color(0.036f, 0.086f, 0.054f), new Color(0.020f, 0.053f, 0.032f));
                Band(vh, Inset(r, 1), 8, 1.6f, new Color(0.53f, 0.43f, 0.23f), true);
                Band(vh, Inset(r, 5), 5, 0.8f, new Color(0.34f, 0.34f, 0.20f, 0.72f));
                if (style != Style.Wallet) Corners(vh, Inset(r, 9), 12, new Color(0.63f, 0.53f, 0.29f));
                break;
        }
    }

    protected void Surface(VertexHelper vh, Rect r, float cut, Color top, Color bottom)
    {
        Vector2[] points = Points(r, cut);
        int first = vh.currentVertCount;
        Vertex(vh, r.center, Color.Lerp(bottom, top, 0.55f), true);
        foreach (Vector2 p in points) Vertex(vh, p, Color.Lerp(bottom, top, Mathf.InverseLerp(r.yMin, r.yMax, p.y)), true);
        for (int i = 0; i < 8; i++) vh.AddTriangle(first, first + i + 1, first + 1 + (i + 1) % 8);
    }

    protected void Band(VertexHelper vh, Rect r, float cut, float thickness, Color tint, bool bevel = false)
    {
        if (r.width <= thickness * 2 || r.height <= thickness * 2) return;
        Vector2[] outer = Points(r, cut), inner = Points(Inset(r, thickness), Mathf.Max(1, cut - thickness * 0.6f));
        int first = vh.currentVertCount;
        for (int i = 0; i < 8; i++)
        {
            Color edge = bevel ? Shade(tint, Mathf.Lerp(0.57f, 1.18f, Mathf.InverseLerp(r.yMin, r.yMax, outer[i].y))) : tint;
            Vertex(vh, outer[i], edge);
            Vertex(vh, inner[i], bevel ? Shade(edge, 0.77f) : edge);
        }
        for (int i = 0; i < 8; i++)
        {
            int a = first + i * 2, b = first + (i + 1) % 8 * 2;
            vh.AddTriangle(a, b, b + 1); vh.AddTriangle(a, b + 1, a + 1);
        }
    }

    protected void Corners(VertexHelper vh, Rect r, float size, Color tint)
    {
        CornerCaps(vh, r, size, tint);
        for (int x = -1; x <= 1; x += 2)
            for (int y = -1; y <= 1; y += 2)
            {
                Vector2 p = new Vector2(x < 0 ? r.xMin : r.xMax, y < 0 ? r.yMin : r.yMax);
                Vector2 inward = new Vector2(-x, -y);
                Diamond(vh, p + inward * 3, 2.2f, tint);
                Stroke(vh, p + new Vector2(-x * size, -y * 3), p + new Vector2(-x * (size + 5), -y * 7), 1, tint);
                Stroke(vh, p + new Vector2(-x * 3, -y * size), p + new Vector2(-x * 7, -y * (size + 5)), 1, tint);
            }
    }

    protected void CornerCaps(VertexHelper vh, Rect r, float length, Color tint)
    {
        for (int x = -1; x <= 1; x += 2)
            for (int y = -1; y <= 1; y += 2)
            {
                Vector2 p = new Vector2(x < 0 ? r.xMin : r.xMax, y < 0 ? r.yMin : r.yMax);
                Stroke(vh, p + new Vector2(-x * 5, 0), p + new Vector2(-x * length, 0), 1.3f, tint);
                Stroke(vh, p + new Vector2(0, -y * 5), p + new Vector2(0, -y * length), 1.3f, tint);
                Stroke(vh, p + new Vector2(-x * 5, 0), p + new Vector2(0, -y * 5), 1.6f, tint);
            }
    }

    protected void Stroke(VertexHelper vh, Vector2 a, Vector2 b, float width, Color tint)
    {
        Vector2 n = new Vector2(-(b - a).y, (b - a).x).normalized * width * 0.5f;
        int first = vh.currentVertCount;
        Vertex(vh, a - n, tint); Vertex(vh, a + n, tint); Vertex(vh, b + n, tint); Vertex(vh, b - n, tint);
        vh.AddTriangle(first, first + 1, first + 2); vh.AddTriangle(first, first + 2, first + 3);
    }

    protected void Disc(VertexHelper vh, Vector2 center, float radius, Color tint)
    {
        int first = vh.currentVertCount;
        Vertex(vh, center, tint);
        for (int i = 0; i < 33; i++)
        {
            float angle = i * Mathf.PI / 16;
            Vertex(vh, center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius, tint);
            if (i > 0) vh.AddTriangle(first, first + i, first + i + 1);
        }
    }

    protected void Diamond(VertexHelper vh, Vector2 p, float radius, Color tint)
    {
        int first = vh.currentVertCount;
        Vertex(vh, p + Vector2.up * radius, tint); Vertex(vh, p + Vector2.right * radius, tint);
        Vertex(vh, p + Vector2.down * radius, tint); Vertex(vh, p + Vector2.left * radius, tint);
        vh.AddTriangle(first, first + 1, first + 2); vh.AddTriangle(first, first + 2, first + 3);
    }

    private void Divider(VertexHelper vh, Rect r)
    {
        Color gold = new Color(0.64f, 0.52f, 0.28f, 0.85f);
        if (r.height > r.width)
        {
            Stroke(vh, new Vector2(r.center.x, r.yMin + 7), new Vector2(r.center.x, r.yMax - 7), 0.9f, gold);
            Diamond(vh, new Vector2(r.center.x, r.yMin + 3), 2.5f, gold);
            Diamond(vh, new Vector2(r.center.x, r.yMax - 3), 2.5f, gold);
        }
        else
        {
            float start = r.xMin + (leadingDiamond ? 19 : 0);
            Stroke(vh, new Vector2(start, r.center.y - 1.1f), new Vector2(r.xMax, r.center.y - 1.1f), 1.3f, new Color(0, 0, 0, 0.5f));
            Stroke(vh, new Vector2(start, r.center.y), new Vector2(r.xMax, r.center.y), 1, gold);
            if (leadingDiamond)
            {
                Diamond(vh, new Vector2(r.xMin + 6, r.center.y), 4.5f, gold);
                Diamond(vh, new Vector2(r.xMin + 6, r.center.y), 2.3f, new Color(0.025f, 0.06f, 0.035f));
            }
        }
    }

    private void Sprig(VertexHelper vh, Vector2 origin, int direction)
    {
        Vector2 stem = new Vector2(7, 41) * direction;
        Stroke(vh, origin - stem * 0.4f, origin + stem, 1.3f, new Color(0.23f, 0.27f, 0.11f, 0.8f));
        for (int i = 0; i < 5; i++)
        {
            Vector2 basePoint = origin + stem * (i * 0.23f);
            Vector2 axis = new Vector2(i % 2 == 0 ? 15 : -12, 12) * direction;
            Vector2 side = new Vector2(-axis.y, axis.x).normalized * 5.5f;
            int first = vh.currentVertCount;
            Vertex(vh, basePoint, new Color(0.10f, 0.16f, 0.065f));
            Vertex(vh, basePoint + axis * 0.45f + side, new Color(0.18f, 0.25f, 0.10f));
            Vertex(vh, basePoint + axis, new Color(0.23f, 0.29f, 0.12f));
            Vertex(vh, basePoint + axis * 0.45f - side, new Color(0.085f, 0.14f, 0.06f));
            vh.AddTriangle(first, first + 1, first + 2); vh.AddTriangle(first, first + 2, first + 3);
            Stroke(vh, basePoint, basePoint + axis * 0.83f, 0.7f, new Color(0.29f, 0.32f, 0.13f, 0.65f));
        }
    }

    protected void Vertex(VertexHelper vh, Vector2 p, Color tint, bool textured = false)
    {
        UIVertex vertex = UIVertex.simpleVert;
        vertex.position = p; vertex.color = tint * color;
        vertex.uv0 = textured ? p / 320f + new Vector2(0.37f, 0.19f) : Vector2.one * (1.5f / 128f);
        vh.AddVert(vertex);
    }

    protected static Rect Inset(Rect r, float inset) => new Rect(r.x + inset, r.y + inset, r.width - inset * 2, r.height - inset * 2);
    protected static Color Shade(Color c, float gain) => new Color(c.r * gain, c.g * gain, c.b * gain, c.a);
    private static Vector2[] Points(Rect r, float cut)
    {
        cut = Mathf.Min(cut, Mathf.Min(r.width, r.height) * 0.25f);
        return new[] { new Vector2(r.xMin + cut, r.yMin), new Vector2(r.xMax - cut, r.yMin), new Vector2(r.xMax, r.yMin + cut),
            new Vector2(r.xMax, r.yMax - cut), new Vector2(r.xMax - cut, r.yMax), new Vector2(r.xMin + cut, r.yMax),
            new Vector2(r.xMin, r.yMax - cut), new Vector2(r.xMin, r.yMin + cut) };
    }

    private static Texture2D Grain
    {
        get
        {
            if (surfaceGrain != null) return surfaceGrain;
            const int size = 128;
            Color32[] pixels = new Color32[size * size];
            uint seed = 13871;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    seed = unchecked(seed * 1664525u + 1013904223u);
                    float fibres = Mathf.Sin((x * 3 + y * 5) * (Mathf.PI * 2 / size)) * 5;
                    byte value = (byte)Mathf.Clamp(230 + (seed >> 26) * 0.3f + fibres, 0, 255);
                    if (x < 4 && y < 4) value = 255;
                    pixels[y * size + x] = new Color32(value, value, value, 255);
                }
            surfaceGrain = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "DiceLandMerchantSurface", hideFlags = HideFlags.DontSave,
                filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Repeat
            };
            surfaceGrain.SetPixels32(pixels); surfaceGrain.Apply(false, true);
            return surfaceGrain;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ReleaseGrain()
    {
        if (surfaceGrain == null) return;
        if (Application.isPlaying) Destroy(surfaceGrain); else DestroyImmediate(surfaceGrain);
        surfaceGrain = null;
    }
}
