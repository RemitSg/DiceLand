using UnityEngine;
using UnityEngine.UI;

/// <summary>Brass-edged skill cards and a small energy bolt, using the shared forest surface grain.</summary>
public sealed class MapWeaponDetailGraphic : MapMerchantFrameGraphic
{
    public enum Part { Card, Energy }
    public Part part;
    private float focus;

    public void SetFocus(float value)
    {
        if (Mathf.Abs(focus - value) < 0.002f) return;
        focus = value; SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear(); Rect r = GetPixelAdjustedRect();
        if (r.width <= 0 || r.height <= 0) return;
        if (part == Part.Energy)
        {
            Vector2[] points = { new Vector2(.10f,.5f), new Vector2(-.35f,-.08f), new Vector2(-.04f,-.08f),
                new Vector2(-.15f,-.5f), new Vector2(.35f,.12f), new Vector2(.05f,.12f), new Vector2(.22f,.5f) };
            foreach (Vector2 point in points) Vertex(vh, r.center + Vector2.Scale(point, r.size), new Color32(253, 203, 72, 255));
            vh.AddTriangle(0,1,2); vh.AddTriangle(2,3,4); vh.AddTriangle(2,4,5); vh.AddTriangle(0,2,5); vh.AddTriangle(0,5,6);
            return;
        }
        Color gold = Color.Lerp(new Color32(191, 154, 72, 255), new Color32(248, 213, 125, 255), focus);
        Surface(vh, r, 13, new Color(.033f + focus*.01f, .12f + focus*.018f, .066f), new Color32(5, 24, 13, 255));
        Band(vh, Inset(r, 1), 12, 2.1f, gold, true);
        Band(vh, Inset(r, 6), 8, .8f, new Color32(91, 100, 47, 255));
        Corners(vh, Inset(r, 6), 13, gold);
    }
}
