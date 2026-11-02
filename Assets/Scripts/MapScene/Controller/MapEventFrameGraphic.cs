using UnityEngine;
using UnityEngine.UI;

/// <summary>Thin brass frame and gold action face, drawn at UI resolution without stretched corner art.</summary>
[RequireComponent(typeof(CanvasRenderer))]
public sealed class MapEventFrameGraphic : MaskableGraphic
{
    public bool buttonFace;

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        Rect r = GetPixelAdjustedRect();
        if (buttonFace)
        {
            Fill(vh, r, 10f, new Color(0.93f, 0.74f, 0.38f), new Color(0.67f, 0.45f, 0.17f));
            Band(vh, Inset(r, 1f), 9f, 2f, new Color(0.99f, 0.85f, 0.52f));
            Band(vh, Inset(r, 6f), 5f, 1f, new Color(0.38f, 0.25f, 0.10f));
        }
        else
        {
            Band(vh, Inset(r, 2f), 14f, 3f, new Color(0.60f, 0.45f, 0.21f));
            Band(vh, Inset(r, 7f), 10f, 1.3f, new Color(0.90f, 0.75f, 0.42f));
            Band(vh, Inset(r, 13f), 6f, 1f, new Color(0.31f, 0.30f, 0.17f));
        }
        float inset = buttonFace ? 11f : 20f;
        float length = buttonFace ? 9f : 22f;
        Color gold = new Color(0.86f, 0.70f, 0.37f);
        for (int x = -1; x <= 1; x += 2)
            for (int y = -1; y <= 1; y += 2)
            {
                Vector2 corner = new Vector2(x < 0 ? r.xMin + inset : r.xMax - inset, y < 0 ? r.yMin + inset : r.yMax - inset);
                Stroke(vh, corner, corner - Vector2.right * x * length, 1.5f, gold);
                Stroke(vh, corner, corner - Vector2.up * y * length, 1.5f, gold);
            }
    }

    private static Rect Inset(Rect r, float inset) { return new Rect(r.x + inset, r.y + inset, r.width - inset * 2f, r.height - inset * 2f); }

    private static Vector2[] Points(Rect r, float cut)
    {
        return new[] { new Vector2(r.xMin + cut, r.yMin), new Vector2(r.xMax - cut, r.yMin),
            new Vector2(r.xMax, r.yMin + cut), new Vector2(r.xMax, r.yMax - cut), new Vector2(r.xMax - cut, r.yMax),
            new Vector2(r.xMin + cut, r.yMax), new Vector2(r.xMin, r.yMax - cut), new Vector2(r.xMin, r.yMin + cut) };
    }

    private void Fill(VertexHelper vh, Rect r, float cut, Color top, Color bottom)
    {
        Vector2[] points = Points(r, cut);
        int start = vh.currentVertCount;
        Vertex(vh, r.center, Color.Lerp(bottom, top, 0.5f));
        foreach (Vector2 point in points) Vertex(vh, point, Color.Lerp(bottom, top, Mathf.InverseLerp(r.yMin, r.yMax, point.y)));
        for (int i = 0; i < 8; i++) vh.AddTriangle(start, start + 1 + i, start + 1 + (i + 1) % 8);
    }

    private void Band(VertexHelper vh, Rect r, float cut, float thickness, Color tint)
    {
        Vector2[] outside = Points(r, cut), inside = Points(Inset(r, thickness), Mathf.Max(1f, cut - thickness * 0.5f));
        int start = vh.currentVertCount;
        for (int i = 0; i < 8; i++) { Vertex(vh, outside[i], tint); Vertex(vh, inside[i], tint); }
        for (int i = 0; i < 8; i++)
        {
            int a = start + i * 2, b = start + ((i + 1) % 8) * 2;
            vh.AddTriangle(a, b, b + 1);
            vh.AddTriangle(a, b + 1, a + 1);
        }
    }

    private void Stroke(VertexHelper vh, Vector2 a, Vector2 b, float width, Color tint)
    {
        Vector2 n = new Vector2(-(b - a).y, (b - a).x).normalized * width * 0.5f;
        int start = vh.currentVertCount;
        Vertex(vh, a - n, tint); Vertex(vh, a + n, tint); Vertex(vh, b + n, tint); Vertex(vh, b - n, tint);
        vh.AddTriangle(start, start + 1, start + 2);
        vh.AddTriangle(start, start + 2, start + 3);
    }

    private void Vertex(VertexHelper vh, Vector2 point, Color tint)
    {
        UIVertex vertex = UIVertex.simpleVert;
        vertex.position = point;
        vertex.color = tint * color;
        vh.AddVert(vertex);
    }
}
