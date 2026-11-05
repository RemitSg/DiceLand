using UnityEngine;
using UnityEngine.UI;

/// <summary>Skill-specific jade selection faces and dice/type glyphs over the shared forest grain.</summary>
[RequireComponent(typeof(CanvasRenderer))]
public sealed class BattleSkillFrameGraphic : MapMerchantFrameGraphic
{
    public enum SkillStyle { Choice, Die, Attack, Defense, Check, HoverPanel }
    public SkillStyle skillStyle;
    public bool defense;
    private static readonly Color Gold = new Color32(230, 188, 98, 255);

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear(); Rect r = GetPixelAdjustedRect();
        if (r.width <= 0 || r.height <= 0) return;
        if (skillStyle == SkillStyle.HoverPanel)
        {
            Surface(vh, r, 12, new Color32(13, 29, 19, 255), new Color32(6, 18, 12, 255));
            Band(vh, Inset(r, 1), 12, 6, new Color32(49, 32, 15, 255));
            Band(vh, Inset(r, 3), 10, 2.6f, Gold, true);
            Band(vh, Inset(r, 10), 5, 1, new Color32(122, 104, 57, 255));
            CornerCaps(vh, Inset(r, 11), 15, Gold);
            return;
        }
        if (skillStyle == SkillStyle.Choice)
        {
            Surface(vh, r, 9, selected ? new Color32(25, 76, 59, 255) : new Color32(29, 42, 29, 255),
                selected ? new Color32(10, 43, 32, 255) : new Color32(11, 24, 16, 255));
            Color edge = selected ? Gold : new Color32(158, 133, 76, 255);
            if (!available) edge = new Color32(85, 90, 72, 255);
            Band(vh, Inset(r, 1), 8, selected ? 3 : 1.4f, edge, true);
            Band(vh, Inset(r, 6), 5, 0.8f, Shade(edge, 0.5f));
            CornerCaps(vh, Inset(r, 7), 14, Shade(edge, 0.9f));
            return;
        }
        float size = Mathf.Min(r.width, r.height); Vector2 c = r.center;
        Color tint = defense || skillStyle == SkillStyle.Defense ? new Color32(109, 183, 238, 255) : new Color32(238, 105, 94, 255);
        if (skillStyle == SkillStyle.Check)
        {
            Stroke(vh, c + new Vector2(-0.35f, 0) * size, c + new Vector2(-0.08f, -0.25f) * size, size * 0.12f, Gold);
            Stroke(vh, c + new Vector2(-0.08f, -0.25f) * size, c + new Vector2(0.39f, 0.31f) * size, size * 0.12f, Gold);
            return;
        }
        if (skillStyle == SkillStyle.Die)
        {
            Rect die = new Rect(c.x - size * 0.4f, c.y - size * 0.4f, size * 0.8f, size * 0.8f);
            Surface(vh, die, size * 0.13f, Shade(tint, 0.82f), Shade(tint, 0.40f));
            Band(vh, Inset(die, 1), size * 0.12f, 1.4f, Shade(tint, 1.25f), true);
            foreach (Vector2 p in new[] { new Vector2(-0.19f, 0.19f), new Vector2(0.19f, 0.19f), Vector2.zero, new Vector2(-0.19f, -0.19f), new Vector2(0.19f, -0.19f) })
                Diamond(vh, c + p * size, size * 0.052f, new Color32(249, 235, 202, 255));
        }
        else if (skillStyle == SkillStyle.Defense)
        {
            Vector2[] points = { new Vector2(-0.33f, 0.30f), new Vector2(0, 0.40f), new Vector2(0.33f, 0.30f), new Vector2(0.28f, -0.12f), new Vector2(0, -0.39f), new Vector2(-0.28f, -0.12f) };
            for (int i = 0; i < points.Length; i++) Stroke(vh, c + points[i] * size, c + points[(i + 1) % points.Length] * size, size * 0.06f, tint);
            Stroke(vh, c + Vector2.up * size * 0.27f, c + Vector2.down * size * 0.25f, size * 0.04f, tint);
        }
        else
        {
            Vector2 axis = new Vector2(0.68f, 0.74f), side = new Vector2(-axis.y, axis.x);
            Stroke(vh, c - axis * size * 0.19f, c + axis * size * 0.38f, size * 0.14f, new Color32(240, 224, 180, 255));
            Stroke(vh, c - axis * size * 0.29f - side * size * 0.18f, c - axis * size * 0.29f + side * size * 0.18f, size * 0.06f, Gold);
            Stroke(vh, c - axis * size * 0.45f, c - axis * size * 0.25f, size * 0.07f, Gold);
        }
    }
}
