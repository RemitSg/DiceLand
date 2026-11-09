using UnityEngine;
using UnityEngine.UI;

/// <summary>Quiet edge shading keeps typography legible while leaving the painting luminous.</summary>
[RequireComponent(typeof(CanvasRenderer))]
public sealed class MapEventAtmosphereGraphic : MaskableGraphic
{
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear(); Rect r = rectTransform.rect;
        Gradient(vh, new Rect(r.x, r.y + r.height * .60f, r.width, r.height * .40f), 0, .66f);
        Gradient(vh, new Rect(r.x, r.y, r.width, r.height * .38f), .65f, 0);
    }
    private static void Gradient(VertexHelper vh, Rect rect, float bottom, float top)
    {
        int start = vh.currentVertCount;
        Vector2[] corners = { new Vector2(rect.xMin, rect.yMin), new Vector2(rect.xMax, rect.yMin), new Vector2(rect.xMax, rect.yMax), new Vector2(rect.xMin, rect.yMax) };
        for (int i = 0; i < 4; i++)
        {
            UIVertex vertex = UIVertex.simpleVert; vertex.position = corners[i]; vertex.color = new Color(.008f, .022f, .014f, i < 2 ? bottom : top); vh.AddVert(vertex);
        }
        vh.AddTriangle(start, start + 1, start + 2); vh.AddTriangle(start, start + 2, start + 3);
    }
}
