using UnityEngine;
using UnityEngine.UI;

public sealed class MapEventTooltipNotch : MaskableGraphic
{
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear(); Rect r = rectTransform.rect;
        Triangle(vh, new Vector2(r.xMin, r.yMin), new Vector2(r.xMax, r.center.y), new Vector2(r.xMin, r.yMax), new Color(.83f, .68f, .38f));
        Triangle(vh, new Vector2(r.xMin - 1, r.yMin + 2), new Vector2(r.xMax - 3, r.center.y), new Vector2(r.xMin - 1, r.yMax - 2), new Color(.018f, .042f, .026f));
    }
    private static void Triangle(VertexHelper vh, Vector2 a, Vector2 b, Vector2 c, Color tint)
    {
        int start = vh.currentVertCount;
        foreach (Vector2 point in new[] { a, b, c }) { UIVertex v = UIVertex.simpleVert; v.position = point; v.color = tint; vh.AddVert(v); }
        vh.AddTriangle(start, start + 1, start + 2);
    }
}
