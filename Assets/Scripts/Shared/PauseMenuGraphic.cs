using UnityEngine;
using UnityEngine.UI;

/// <summary>Engraved pause-menu symbols and the muted red departure action.</summary>
public sealed class PauseMenuGraphic : MapMerchantFrameGraphic
{
    public enum Part { Window, Title, AudioPanel, PrimaryButton, SecondaryButton, SaveButton, Danger, Play, Settings, Save, Retry, Leave }
    public Part part;
    private static readonly Color Gold = new Color32(225, 191, 122, 255);

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        Rect r = GetPixelAdjustedRect();
        if (r.width <= 0 || r.height <= 0) return;
        if (part == Part.Window)
        {
            Surface(vh, r, 21, new Color32(20, 39, 27, 255), new Color32(8, 22, 15, 255));
            Band(vh, Inset(r, 1), 20, 10, new Color32(45, 29, 16, 255));
            Band(vh, Inset(r, 3), 18, 3, Gold, true);
            Band(vh, Inset(r, 10), 12, 2, new Color32(150, 111, 55, 255), true);
            Band(vh, Inset(r, 15), 8, 1, new Color32(91, 81, 43, 255));
            CornerCaps(vh, Inset(r, 19), 25, Gold);
            Vine(vh, new Vector2(r.xMin + 18, r.yMax - 13), new Vector2(1, -.2f), 1);
            Vine(vh, new Vector2(r.xMax - 18, r.yMin + 12), new Vector2(-1, .25f), .85f);
            return;
        }
        if (part == Part.Title)
        {
            Surface(vh, r, 34, new Color32(76, 46, 22, 255), new Color32(35, 23, 14, 255));
            Band(vh, Inset(r, 1), 33, 4, Gold, true);
            Band(vh, Inset(r, 9), 27, 1.3f, new Color32(146, 106, 52, 255), true);
            for (int i = 0; i < 3; i++)
                Stroke(vh, new Vector2(r.xMin + 56, r.yMin + 18 + i * 6), new Vector2(r.xMax - 56, r.yMin + 19 + i * 6), .6f, new Color32(163, 107, 51, 34));
            foreach (int side in new[] { -1, 1 })
            {
                Vector2 gem = new Vector2(r.center.x + side * (r.width / 2 - 27), r.center.y);
                Disc(vh, gem, 10, Gold); Disc(vh, gem, 7, new Color32(19, 83, 57, 255));
                Disc(vh, gem + new Vector2(-2, 2), 3, new Color32(91, 171, 112, 255));
            }
            return;
        }
        if (part == Part.AudioPanel)
        {
            Surface(vh, r, 16, new Color32(28, 37, 25, 255), new Color32(13, 26, 17, 255));
            Band(vh, Inset(r, 1), 15, 2.5f, Gold, true);
            Band(vh, Inset(r, 7), 11, 1, new Color32(125, 98, 52, 255));
            CornerCaps(vh, Inset(r, 12), 17, new Color32(167, 133, 71, 255));
            return;
        }
        if (part == Part.PrimaryButton || part == Part.SecondaryButton || part == Part.SaveButton || part == Part.Danger)
        {
            Color top = part == Part.Danger ? new Color32(91, 48, 37, 255) : part == Part.PrimaryButton ? new Color32(36, 104, 65, 255)
                : part == Part.SaveButton ? new Color32(32, 60, 39, 255) : new Color32(57, 49, 31, 255);
            Color bottom = part == Part.Danger ? new Color32(48, 27, 23, 255) : part == Part.PrimaryButton ? new Color32(13, 53, 34, 255)
                : part == Part.SaveButton ? new Color32(13, 33, 23, 255) : new Color32(28, 28, 19, 255);
            Surface(vh, r, 12, top, bottom);
            Band(vh, Inset(r, 1), 11, 3, new Color32(68, 44, 22, 255));
            Band(vh, Inset(r, 3), 9, 1.8f, part == Part.PrimaryButton ? new Color32(248, 213, 134, 255) : Gold, true);
            Band(vh, Inset(r, 8), 5, .8f, new Color32(130, 104, 54, 255));
            CornerCaps(vh, Inset(r, 11), 12, new Color32(158, 128, 69, 255));
            return;
        }

        Vector2 p = r.center;
        float s = Mathf.Min(r.width, r.height) / 40f;
        switch (part)
        {
            case Part.Play:
                Triangle(vh, p + new Vector2(-7, -11) * s, p + new Vector2(-7, 11) * s, p + new Vector2(11, 0) * s, Gold);
                break;
            case Part.Settings:
                Disc(vh, p, 10 * s, Gold);
                Disc(vh, p, 5 * s, new Color32(20, 40, 28, 255));
                for (int i = 0; i < 8; i++)
                {
                    float a = i * Mathf.PI / 4;
                    Vector2 d = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                    Stroke(vh, p + d * 9 * s, p + d * 15 * s, 4 * s, Gold);
                }
                break;
            case Part.Save:
                Band(vh, new Rect(p.x - 12 * s, p.y - 14 * s, 24 * s, 28 * s), 2 * s, 2 * s, Gold);
                Line(vh, p, s, -6, 13, -6, 3);
                Line(vh, p, s, -6, 3, 6, 3);
                Line(vh, p, s, 6, 3, 6, 13);
                Band(vh, new Rect(p.x - 7 * s, p.y - 13 * s, 14 * s, 9 * s), 1, 1.5f * s, Gold);
                break;
            case Part.Retry:
                for (int i = 0; i < 20; i++)
                {
                    float a = -.4f + i * .25f, b = a + .25f;
                    Stroke(vh, p + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 12 * s,
                        p + new Vector2(Mathf.Cos(b), Mathf.Sin(b)) * 12 * s, 2.3f * s, Gold);
                }
                Triangle(vh, p + new Vector2(3, -12) * s, p + new Vector2(13, -15) * s, p + new Vector2(10, -4) * s, Gold);
                break;
            case Part.Leave:
                Line(vh, p, s, -3, 13, -12, 13);
                Line(vh, p, s, -12, 13, -12, -13);
                Line(vh, p, s, -12, -13, -3, -13);
                Line(vh, p, s, -4, 0, 14, 0);
                Line(vh, p, s, 7, 7, 14, 0);
                Line(vh, p, s, 7, -7, 14, 0);
                break;
        }
    }

    private void Vine(VertexHelper vh, Vector2 origin, Vector2 direction, float scale)
    {
        direction.Normalize();
        Vector2 normal = new Vector2(-direction.y, direction.x);
        for (int i = 0; i < 7; i++)
        {
            Vector2 p = origin + direction * i * 14 * scale + normal * Mathf.Sin(i * .6f) * 7 * scale;
            Vector2 next = origin + direction * (i + 1) * 14 * scale + normal * Mathf.Sin((i + 1) * .6f) * 7 * scale;
            Stroke(vh, p, next, 2 * scale, new Color32(151, 136, 65, 255));
            Vector2 leaf = (direction * 20 + normal * (i % 2 == 0 ? 24 : -22)) * scale;
            Vector2 edge = new Vector2(-leaf.y, leaf.x).normalized * 7 * scale;
            Triangle(vh, p, p + leaf * .5f + edge, p + leaf, new Color32(83, 112, 49, 255));
            Triangle(vh, p, p + leaf, p + leaf * .5f - edge, new Color32(49, 79, 34, 255));
            Stroke(vh, p, p + leaf, .7f, new Color32(164, 161, 78, 180));
        }
    }

    private void Line(VertexHelper vh, Vector2 p, float s, float x1, float y1, float x2, float y2, float width = 2)
        => Stroke(vh, p + new Vector2(x1, y1) * s, p + new Vector2(x2, y2) * s, width * s, Gold);

    private void Triangle(VertexHelper vh, Vector2 a, Vector2 b, Vector2 c, Color tint)
    {
        int start = vh.currentVertCount;
        Vertex(vh, a, tint); Vertex(vh, b, tint); Vertex(vh, c, tint);
        vh.AddTriangle(start, start + 1, start + 2);
    }
}
