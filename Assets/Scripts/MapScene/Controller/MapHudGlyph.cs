using UnityEngine;
using UnityEngine.UI;

/// <summary>Small scalable, non-interactive map HUD marks.</summary>
[ExecuteAlways]
[RequireComponent(typeof(CanvasRenderer))]
public sealed class MapHudGlyph : MaskableGraphic
{
    public enum Shape { Eye, Check, Book, Divider, Refresh }
    public Shape shape;

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        Rect r = GetPixelAdjustedRect();
        Vector2 c = r.center;
        float s = Mathf.Min(r.width, r.height);
        if (shape == Shape.Refresh)
        {
            const float start = 45f, sweep = 285f;
            for (int i = 0; i < 32; i++)
            {
                float a = (start + sweep * i / 32f) * Mathf.Deg2Rad;
                float b = (start + sweep * (i + 1) / 32f) * Mathf.Deg2Rad;
                Stroke(vh, c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * s * 0.32f,
                    c + new Vector2(Mathf.Cos(b), Mathf.Sin(b)) * s * 0.32f, s * 0.075f);
            }
            float end = (start + sweep) * Mathf.Deg2Rad;
            Vector2 point = c + new Vector2(Mathf.Cos(end), Mathf.Sin(end)) * s * 0.32f;
            Vector2 tangent = new Vector2(-Mathf.Sin(end), Mathf.Cos(end)), normal = new Vector2(-tangent.y, tangent.x);
            Stroke(vh, point, point - tangent * s * 0.19f + normal * s * 0.13f, s * 0.075f);
            Stroke(vh, point, point - tangent * s * 0.19f - normal * s * 0.13f, s * 0.075f);
        }
        else if (shape == Shape.Divider)
        {
            Stroke(vh, new Vector2(r.xMin, c.y), new Vector2(c.x - 10f, c.y), 1f);
            Stroke(vh, new Vector2(c.x + 10f, c.y), new Vector2(r.xMax, c.y), 1f);
            Vector2[] p = { c + Vector2.up * 4f, c + Vector2.right * 4f, c + Vector2.down * 4f, c + Vector2.left * 4f };
            for (int i = 0; i < 4; i++) Stroke(vh, p[i], p[(i + 1) % 4], 1.2f);
        }
        else if (shape == Shape.Check)
        {
            Stroke(vh, c + new Vector2(-0.32f, 0f) * s, c + new Vector2(-0.06f, -0.24f) * s, s * 0.10f);
            Stroke(vh, c + new Vector2(-0.06f, -0.24f) * s, c + new Vector2(0.34f, 0.28f) * s, s * 0.10f);
        }
        else if (shape == Shape.Eye)
        {
            for (int i = 0; i < 16; i++)
            {
                float a = i * Mathf.PI * 2f / 16f, b = (i + 1) * Mathf.PI * 2f / 16f;
                Stroke(vh, c + new Vector2(Mathf.Cos(a) * 0.43f, Mathf.Sin(a) * 0.25f) * s,
                    c + new Vector2(Mathf.Cos(b) * 0.43f, Mathf.Sin(b) * 0.25f) * s, s * 0.055f);
            }
            for (int i = 0; i < 12; i++)
                Triangle(vh, c, c + new Vector2(Mathf.Cos(i * Mathf.PI / 6f), Mathf.Sin(i * Mathf.PI / 6f)) * s * 0.12f,
                    c + new Vector2(Mathf.Cos((i + 1) * Mathf.PI / 6f), Mathf.Sin((i + 1) * Mathf.PI / 6f)) * s * 0.12f);
        }
        else
        {
            Stroke(vh, c + new Vector2(-0.4f, 0.32f) * s, c + new Vector2(-0.4f, -0.32f) * s, s * 0.06f);
            Stroke(vh, c + new Vector2(0.4f, 0.32f) * s, c + new Vector2(0.4f, -0.32f) * s, s * 0.06f);
            Stroke(vh, c + Vector2.up * s * 0.22f, c + Vector2.down * s * 0.40f, s * 0.06f);
            for (int side = -1; side <= 1; side += 2)
            {
                Stroke(vh, c + Vector2.up * s * 0.22f, c + new Vector2(side * 0.4f, 0.32f) * s, s * 0.06f);
                Stroke(vh, c + Vector2.down * s * 0.40f, c + new Vector2(side * 0.4f, -0.32f) * s, s * 0.06f);
            }
        }
    }

    private void Stroke(VertexHelper vh, Vector2 a, Vector2 b, float width)
    {
        Vector2 n = new Vector2(-(b - a).y, (b - a).x).normalized * width * 0.5f;
        int i = vh.currentVertCount;
        Vertex(vh, a - n); Vertex(vh, a + n); Vertex(vh, b + n); Vertex(vh, b - n);
        vh.AddTriangle(i, i + 1, i + 2); vh.AddTriangle(i, i + 2, i + 3);
    }

    private void Triangle(VertexHelper vh, Vector2 a, Vector2 b, Vector2 c)
    {
        int i = vh.currentVertCount;
        Vertex(vh, a); Vertex(vh, b); Vertex(vh, c);
        vh.AddTriangle(i, i + 1, i + 2);
    }

    private void Vertex(VertexHelper vh, Vector2 p)
    {
        UIVertex v = UIVertex.simpleVert;
        v.position = p; v.color = color; vh.AddVert(v);
    }
}
