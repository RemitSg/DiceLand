using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Readable dossier insets over the painted frame. Decorations never determine content height.</summary>
[RequireComponent(typeof(CanvasRenderer))]
public sealed class BattleEnemyProfileGraphic : MapMerchantFrameGraphic, IPointerEnterHandler, IPointerExitHandler
{
    public enum ProfileStyle { Section, Passive, Identifier, Badge, Divider, Portrait }
    public ProfileStyle profileStyle;
    public Color accent = new Color32(241, 200, 95, 255);
    public bool hasHeading = true;
    private bool hovered;
    private float emphasis;
    private static readonly Color Gold = new Color32(205, 162, 73, 255);
    private static readonly Color GoldLight = new Color32(247, 217, 132, 255);

    public void OnPointerEnter(PointerEventData data) { hovered = true; }
    public void OnPointerExit(PointerEventData data) { hovered = false; }
    protected override void OnDisable() { hovered = false; emphasis = 0; base.OnDisable(); }
    private void Update()
    {
        float next = Mathf.MoveTowards(emphasis, hovered ? 1 : 0, Time.unscaledDeltaTime / .13f);
        if (Mathf.Approximately(next, emphasis)) return;
        emphasis = next; SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear(); Rect r = GetPixelAdjustedRect();
        if (r.width < 1 || r.height < 1) return;
        switch (profileStyle)
        {
            case ProfileStyle.Passive:
                Surface(vh, r, 11, Color.Lerp(new Color32(10, 38, 21, 248), new Color32(23, 55, 32, 255), emphasis * .6f), new Color32(4, 23, 13, 248));
                Band(vh, Inset(r, 1), 11, 2.1f, Color.Lerp(Gold, GoldLight, emphasis * .7f), true);
                Band(vh, Inset(r, 6), 7, .9f, new Color32(120, 114, 50, 220));
                Corners(vh, Inset(r, 9), 15, new Color32(174, 144, 61, 210));
                if (hasHeading) { Diamond(vh, new Vector2(r.xMin + 32, r.yMax - 38), 7, accent); Diamond(vh, new Vector2(r.xMin + 32, r.yMax - 38), 3, GoldLight); }
                break;
            case ProfileStyle.Section:
                float labelWidth = Mathf.Min(223, r.width - 18);
                Rect plate = new Rect(r.xMin + 14, r.yMin + 2, labelWidth - 14, r.height - 4);
                Surface(vh, plate, 10, new Color32(42, 45, 22, 255), new Color32(16, 29, 16, 255));
                Band(vh, plate, 10, 1.5f, Gold, true);
                Band(vh, Inset(plate, 4), 7, .6f, new Color32(116, 100, 42, 230));
                Stroke(vh, new Vector2(r.xMin + labelWidth + 12, r.center.y), new Vector2(r.xMax, r.center.y), 1.3f, Gold);
                Diamond(vh, new Vector2(r.xMin + labelWidth + 9, r.center.y), 3, GoldLight);
                Leaf(vh, new Vector2(r.xMin + 18, r.center.y - 2), new Vector2(-20, 10), 6, new Color32(102, 131, 40, 255));
                Leaf(vh, new Vector2(r.xMin + 18, r.center.y - 2), new Vector2(-9, -13), 5, new Color32(80, 109, 35, 255));
                break;
            case ProfileStyle.Badge:
            case ProfileStyle.Identifier:
                Surface(vh, r, 10, new Color32(26, 47, 26, 255), new Color32(7, 26, 15, 255));
                Band(vh, Inset(r, 1), 10, 1.7f, GoldLight, true);
                Band(vh, Inset(r, 5), 7, .8f, Gold);
                if (profileStyle == ProfileStyle.Identifier)
                    foreach (int side in new[] { -1, 1 })
                    {
                        Vector2 p = new Vector2(side < 0 ? r.xMin - 10 : r.xMax + 10, r.center.y);
                        Diamond(vh, p, 5, Gold); Diamond(vh, p, 2.5f, new Color32(12, 40, 25, 255));
                    }
                break;
            case ProfileStyle.Divider:
                float x = r.center.x;
                Stroke(vh, new Vector2(x, r.yMin + 7), new Vector2(x, r.center.y - 13), 1.3f, Gold);
                Stroke(vh, new Vector2(x, r.center.y + 13), new Vector2(x, r.yMax - 7), 1.3f, Gold);
                Diamond(vh, r.center, 7, GoldLight); Diamond(vh, r.center, 3.8f, new Color32(7, 35, 19, 255));
                Disc(vh, new Vector2(x, r.yMin + 4), 2, GoldLight); Disc(vh, new Vector2(x, r.yMax - 4), 2, GoldLight);
                break;
            case ProfileStyle.Portrait:
                Ellipse(vh, new Vector2(r.center.x, r.center.y - r.height * .08f), new Vector2(r.width * .48f, r.height * .42f), new Color(0.23f, .43f, .18f, .15f));
                Ellipse(vh, new Vector2(r.center.x - 15, r.yMin + 20), new Vector2(r.width * .36f, 27), new Color(0, .015f, 0, .55f));
                break;
        }
    }

    private void Ellipse(VertexHelper vh, Vector2 center, Vector2 radii, Color tint)
    {
        int first = vh.currentVertCount; Vertex(vh, center, tint);
        for (int i = 0; i <= 64; i++)
        {
            float angle = i * Mathf.PI / 32;
            Vertex(vh, center + new Vector2(Mathf.Cos(angle) * radii.x, Mathf.Sin(angle) * radii.y), new Color(tint.r, tint.g, tint.b, 0));
            if (i > 0) vh.AddTriangle(first, first + i, first + i + 1);
        }
    }

    private void Leaf(VertexHelper vh, Vector2 p, Vector2 direction, float breadth, Color tint)
    {
        Vector2 n = new Vector2(-direction.y, direction.x).normalized * breadth;
        int first = vh.currentVertCount;
        Vertex(vh, p, Shade(tint, .6f)); Vertex(vh, p + direction * .5f + n, tint);
        Vertex(vh, p + direction, Shade(tint, 1.4f)); Vertex(vh, p + direction * .5f - n, Shade(tint, .7f));
        vh.AddTriangle(first, first + 1, first + 2); vh.AddTriangle(first, first + 2, first + 3);
        Stroke(vh, p, p + direction * .9f, .8f, Shade(tint, 1.5f));
    }
}
