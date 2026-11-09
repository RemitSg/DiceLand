using UnityEngine;
using UnityEngine.UI;

/// <summary>The drawn hole and input hole share the same canvas-local rectangle.</summary>
[RequireComponent(typeof(CanvasRenderer))]
public sealed class TutorialSpotlightGraphic : MaskableGraphic, ICanvasRaycastFilter
{
    public Rect hole;
    public bool hasHole, blockHole = true;
    private Rect additionalHole;
    private bool hasAdditionalHole;

    public void SetAdditionalHole(Rect value, bool visible)
    {
        if (additionalHole == value && hasAdditionalHole == visible) return;
        additionalHole = value;
        hasAdditionalHole = visible;
        SetVerticesDirty();
    }
    public void SetHole(Rect value, bool visible, bool modal)
    {
        if (hole == value && hasHole == visible && blockHole == modal) return;
        hole = value; hasHole = visible; blockHole = modal; SetVerticesDirty();
    }
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear(); Rect r = rectTransform.rect;
        if (!hasHole) { Quad(vh, r); return; }
        OutsideAdditionalHole(vh, Rect.MinMaxRect(r.xMin, hole.yMax, r.xMax, r.yMax));
        OutsideAdditionalHole(vh, Rect.MinMaxRect(r.xMin, r.yMin, r.xMax, hole.yMin));
        OutsideAdditionalHole(vh, Rect.MinMaxRect(r.xMin, hole.yMin, hole.xMin, hole.yMax));
        OutsideAdditionalHole(vh, Rect.MinMaxRect(hole.xMax, hole.yMin, r.xMax, hole.yMax));
    }

    private void OutsideAdditionalHole(VertexHelper vh, Rect rect)
    {
        if (!hasAdditionalHole || !TutorialHighlightCoordinateUtility.Intersect(rect, additionalHole, out Rect cut))
        {
            Quad(vh, rect);
            return;
        }
        Quad(vh, Rect.MinMaxRect(rect.xMin, cut.yMax, rect.xMax, rect.yMax));
        Quad(vh, Rect.MinMaxRect(rect.xMin, rect.yMin, rect.xMax, cut.yMin));
        Quad(vh, Rect.MinMaxRect(rect.xMin, cut.yMin, cut.xMin, cut.yMax));
        Quad(vh, Rect.MinMaxRect(cut.xMax, cut.yMin, rect.xMax, cut.yMax));
    }
    private void Quad(VertexHelper vh, Rect r)
    {
        if (r.width <= 0 || r.height <= 0) return;
        int first = vh.currentVertCount;
        foreach (Vector2 p in new[] { new Vector2(r.xMin,r.yMin), new Vector2(r.xMin,r.yMax), new Vector2(r.xMax,r.yMax), new Vector2(r.xMax,r.yMin) })
        { var vertex = UIVertex.simpleVert; vertex.position = p; vertex.color = color; vh.AddVert(vertex); }
        vh.AddTriangle(first,first+1,first+2); vh.AddTriangle(first,first+2,first+3);
    }
    public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
    {
        if (blockHole || !hasHole) return true;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenPoint, eventCamera, out Vector2 local))
            return true;
        return !hole.Contains(local) && !(hasAdditionalHole && additionalHole.Contains(local));
    }
}
