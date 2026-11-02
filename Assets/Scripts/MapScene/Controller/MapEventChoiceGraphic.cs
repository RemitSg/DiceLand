using UnityEngine;
using UnityEngine.UI;

/// <summary>Reusable chamfered choice row with a geometric selection ring; no event-specific art.</summary>
[RequireComponent(typeof(CanvasRenderer))]
public sealed class MapEventChoiceGraphic : MaskableGraphic
{
    private bool selected, available = true;

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
        Color edge = selected ? new Color(0.91f, 0.76f, 0.40f) : new Color(0.43f, 0.36f, 0.23f);
        Color fill = selected ? new Color(0.065f, 0.15f, 0.105f) : new Color(0.025f, 0.043f, 0.035f);
        if (!available) { edge *= 0.7f; fill *= 0.7f; }
        Polygon(vh, r, 9f, edge);
        float border = selected ? 2.5f : 1.2f;
        Polygon(vh, new Rect(r.x + border, r.y + border, r.width - border * 2f, r.height - border * 2f), 8f, fill);
        Color rule = new Color(edge.r, edge.g, edge.b, 0.65f);
        Stroke(vh, new Vector2(r.xMin + 96f, r.yMin + 18f), new Vector2(r.xMin + 96f, r.yMax - 18f), 1f, rule);
        Stroke(vh, new Vector2(r.xMin + 366f, r.yMin + 18f), new Vector2(r.xMin + 366f, r.yMax - 18f), 1f, rule);
        Vector2 center = new Vector2(r.xMax - 33f, r.yMax - Mathf.Min(41f, r.height * 0.5f));
        Color mark = selected ? new Color(0.56f, 0.87f, 0.69f) : new Color(0.45f, 0.48f, 0.42f);
        for (int i = 0; i < 40; i++)
        {
            float a = i * Mathf.PI * 2f / 40f, b = (i + 1) * Mathf.PI * 2f / 40f;
            Stroke(vh, center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 18f,
                center + new Vector2(Mathf.Cos(b), Mathf.Sin(b)) * 18f, selected ? 2.5f : 1.8f, mark);
        }
        if (selected)
        {
            Stroke(vh, center + new Vector2(-9f, 0f), center + new Vector2(-2f, -7f), 3f, mark);
            Stroke(vh, center + new Vector2(-2f, -7f), center + new Vector2(10f, 8f), 3f, mark);
        }
    }

    private void Polygon(VertexHelper vh, Rect r, float cut, Color tint)
    {
        Vector2[] points = { new Vector2(r.xMin + cut, r.yMin), new Vector2(r.xMax - cut, r.yMin),
            new Vector2(r.xMax, r.yMin + cut), new Vector2(r.xMax, r.yMax - cut), new Vector2(r.xMax - cut, r.yMax),
            new Vector2(r.xMin + cut, r.yMax), new Vector2(r.xMin, r.yMax - cut), new Vector2(r.xMin, r.yMin + cut) };
        int start = vh.currentVertCount;
        Vertex(vh, r.center, tint);
        foreach (Vector2 point in points) Vertex(vh, point, tint);
        for (int i = 0; i < points.Length; i++) vh.AddTriangle(start, start + 1 + i, start + 1 + (i + 1) % points.Length);
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
