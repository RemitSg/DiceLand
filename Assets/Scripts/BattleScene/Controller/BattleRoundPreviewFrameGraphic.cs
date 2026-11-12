using UnityEngine;
using UnityEngine.UI;

internal sealed class BattleRoundPreviewFrameGraphic : MapMerchantFrameGraphic
{
    internal enum Shape { Row, Warning, Order }

    private Shape shape;
    private Color accent;
    private bool danger;
    private bool focused;

    public void SetAppearance(Shape newShape, Color newAccent, bool isDanger = false, bool isFocused = false)
    {
        if (shape == newShape && accent == newAccent && danger == isDanger && focused == isFocused)
            return;
        shape = newShape;
        accent = newAccent;
        danger = isDanger;
        focused = isFocused;
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        Rect r = GetPixelAdjustedRect();
        if (r.width < 2 || r.height < 2)
            return;

        if (shape == Shape.Order)
        {
            float radius = Mathf.Min(r.width, r.height) * 0.5f;
            Disc(mesh, r.center, radius, new Color32(12, 15, 12, 240));
            Disc(mesh, r.center, radius - 1.5f, accent);
            Disc(mesh, r.center, radius - 3.5f, Shade(accent, 0.22f));
            return;
        }

        if (shape == Shape.Warning)
        {
            Surface(mesh, r, 8, new Color32(81, 35, 25, 255), new Color32(44, 21, 15, 255));
            Band(mesh, Inset(r, 1), 8, 1.4f, accent, true);
            Vector2 center = new Vector2(r.xMin + 19f, r.center.y);
            Vector2 top = center + new Vector2(0, 9);
            Vector2 left = center + new Vector2(-9, -7);
            Vector2 right = center + new Vector2(9, -7);
            Stroke(mesh, top, left, 1.8f, accent);
            Stroke(mesh, left, right, 1.8f, accent);
            Stroke(mesh, right, top, 1.8f, accent);
            Stroke(mesh, center + Vector2.up * 4, center, 1.8f, accent);
            Disc(mesh, center + Vector2.down * 3, 1f, accent);
            return;
        }

        if (focused || danger)
        {
            float brightness = focused ? 0.26f : 0.12f;
            Surface(mesh, r, 7, Shade(accent, brightness), Shade(accent, brightness * 0.46f));
            if (focused)
                Band(mesh, Inset(r, 1), 7, 1.5f, Shade(accent, 0.8f), true);
        }
        else
        {
            Surface(mesh, r, 6, new Color32(24, 43, 30, 195), new Color32(12, 28, 20, 180));
            Stroke(mesh, new Vector2(r.xMin + 8, r.yMin), new Vector2(r.xMax - 8, r.yMin), 0.9f, new Color32(113, 93, 48, 115));
        }
    }
}
