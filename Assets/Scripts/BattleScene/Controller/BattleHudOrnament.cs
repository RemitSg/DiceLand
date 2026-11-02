using UnityEngine;
using UnityEngine.UI;

/// <summary>Small, texture-free metal and glass details for the battle HUD.</summary>
[AddComponentMenu("")]
public sealed class BattleHudOrnament : MaskableGraphic
{
    public enum Shape { Panel, EnergyWell, EnergyFill, TemporaryFill, Jewel, RoundPanel, HealthFrame, HealthFill }
    public Shape kind;

    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        Rect r = rectTransform.rect;
        if (r.width < 0.1f || r.height < 0.1f)
            return;
        if (kind == Shape.Jewel)
        {
            Diamond(mesh, r.center, r.width * 0.5f, r.height * 0.5f, new Color(0.25f, 0.14f, 0.04f));
            Diamond(mesh, r.center, r.width * 0.4f, r.height * 0.4f, new Color(1f, 0.83f, 0.38f));
            Diamond(mesh, r.center, r.width * 0.27f, r.height * 0.29f, new Color(0.02f, 0.48f, 0.79f));
            Triangle(mesh, r.center + Vector2.up * r.height * 0.26f,
                r.center + Vector2.left * r.width * 0.24f, r.center, new Color(0.57f, 0.96f, 1f));
            return;
        }
        if (kind == Shape.Panel || kind == Shape.RoundPanel)
        {
            float cut = kind == Shape.RoundPanel ? r.height * 0.46f : 13f;
            Rect shadow = r; shadow.y -= 4f;
            Plate(mesh, shadow, cut, new Color(0f, 0f, 0f, 0.48f), new Color(0f, 0f, 0f, 0.48f));
            Plate(mesh, r, cut, new Color(0.20f, 0.10f, 0.025f), new Color(0.35f, 0.22f, 0.065f));
            Plate(mesh, Inset(r, 1.4f), cut - 1f, new Color(0.52f, 0.30f, 0.08f), new Color(1f, 0.86f, 0.45f));
            Plate(mesh, Inset(r, 3.4f), cut - 2f, new Color(0.035f, 0.032f, 0.021f), new Color(0.10f, 0.085f, 0.043f));
            Plate(mesh, Inset(r, 5.4f), cut - 3f, new Color(0.58f, 0.37f, 0.10f), new Color(0.78f, 0.61f, 0.29f));
            Plate(mesh, Inset(r, 6.5f), cut - 4f, new Color(0.012f, 0.020f, 0.019f), new Color(0.036f, 0.042f, 0.033f));
            for (int x = -1; x <= 1; x += 2)
            for (int y = -1; y <= 1; y += 2)
                Vine(mesh, new Vector2(x < 0 ? r.xMin + cut + 6f : r.xMax - cut - 6f,
                    y < 0 ? r.yMin + 3f : r.yMax - 3f), new Vector2(x, y));
            return;
        }
        if (kind == Shape.HealthFrame)
        {
            Rect shadow = r; shadow.y -= 2f;
            Plate(mesh, shadow, 7f, new Color(0,0,0,0.55f), new Color(0,0,0,0.55f));
            Plate(mesh, r, 7f, new Color(0.24f,0.13f,0.035f), new Color(0.95f,0.77f,0.40f));
            Plate(mesh, Inset(r,1.5f), 6f, new Color(0.015f,0.03f,0.028f), new Color(0.06f,0.075f,0.065f));
            return;
        }
        if (kind == Shape.HealthFill)
        {
            Plate(mesh, r, 4f, new Color(0.25f,0.42f,0.45f), new Color(0.85f,1f,0.90f));
            Rect sheen = new Rect(r.xMin + 1f, r.yMax - 3f, Mathf.Max(0f,r.width - 2f), 1f);
            Plate(mesh, sheen, 0f, new Color(1f,1f,1f,0.45f), new Color(1f,1f,1f,0.45f));
            return;
        }
        if (kind == Shape.EnergyWell)
        {
            Plate(mesh, r, r.height * 0.48f, new Color(0.17f, 0.29f, 0.31f), new Color(0.83f, 0.90f, 0.69f));
            Plate(mesh, Inset(r, 1.5f), r.height * 0.45f, new Color(0.015f, 0.06f, 0.10f), new Color(0.015f, 0.06f, 0.10f));
            return;
        }

        bool temporary = kind == Shape.TemporaryFill;
        Color bottom = temporary ? new Color(0.35f, 0.08f, 0.77f) : new Color(0.015f, 0.31f, 0.85f);
        Color top = temporary ? new Color(0.86f, 0.57f, 1f) : new Color(0.25f, 0.91f, 1f);
        Plate(mesh, r, Mathf.Min(r.height * 0.48f, r.width * 0.25f), bottom, top);
        Rect core = new Rect(r.xMin + 2f, r.yMin + r.height * 0.2f, Mathf.Max(0f,r.width - 4f), r.height * 0.5f);
        Plate(mesh, core, r.height * 0.2f, temporary ? new Color(0.68f,0.27f,1f) : new Color(0.02f,0.74f,1f),
            temporary ? new Color(0.88f,0.66f,1f) : new Color(0.32f,0.96f,1f));
        Rect shine = new Rect(r.xMin + 1f, r.yMax - 3.5f, Mathf.Max(0f, r.width - 2f), 1.6f);
        Plate(mesh, shine, 0f, new Color(0.82f, 0.98f, 1f, 0.8f), new Color(0.82f, 0.98f, 1f, 0.8f));
    }

    private static Rect Inset(Rect r, float amount) => new Rect(r.x + amount, r.y + amount,
        Mathf.Max(0f, r.width - 2f * amount), Mathf.Max(0f, r.height - 2f * amount));

    private void Plate(VertexHelper mesh, Rect r, float cut, Color bottom, Color top)
    {
        if (r.width <= 0f || r.height <= 0f) return;
        cut = Mathf.Clamp(cut, 0f, Mathf.Min(r.width, r.height) * 0.48f);
        Vector2[] points = {
            new Vector2(r.xMin + cut, r.yMin), new Vector2(r.xMax - cut, r.yMin),
            new Vector2(r.xMax, r.yMin + cut), new Vector2(r.xMax, r.yMax - cut),
            new Vector2(r.xMax - cut, r.yMax), new Vector2(r.xMin + cut, r.yMax),
            new Vector2(r.xMin, r.yMax - cut), new Vector2(r.xMin, r.yMin + cut)
        };
        int first = mesh.currentVertCount;
        mesh.AddVert(r.center, Color.Lerp(bottom, top, 0.5f) * color, Vector2.zero);
        foreach (Vector2 p in points)
            mesh.AddVert(p, Color.Lerp(bottom, top, (p.y - r.yMin) / r.height) * color, Vector2.zero);
        for (int i = 0; i < points.Length; i++)
            mesh.AddTriangle(first, first + 1 + i, first + 1 + (i + 1) % points.Length);
    }

    private void Diamond(VertexHelper mesh, Vector2 p, float halfWidth, float halfHeight, Color tint)
    {
        Triangle(mesh, p + Vector2.left * halfWidth, p + Vector2.up * halfHeight,
            p + Vector2.right * halfWidth, tint);
        Triangle(mesh, p + Vector2.left * halfWidth, p + Vector2.right * halfWidth,
            p + Vector2.down * halfHeight, tint * new Color(0.75f, 0.80f, 0.85f, 1f));
    }

    private void Triangle(VertexHelper mesh, Vector2 a, Vector2 b, Vector2 c, Color tint)
    {
        int i = mesh.currentVertCount;
        mesh.AddVert(a, tint * color, Vector2.zero);
        mesh.AddVert(b, tint * color, Vector2.zero);
        mesh.AddVert(c, tint * color, Vector2.zero);
        mesh.AddTriangle(i, i + 1, i + 2);
    }

    private void Stroke(VertexHelper mesh, Vector2 a, Vector2 b, float width, Color tint)
    {
        Vector2 n = new Vector2(a.y - b.y, b.x - a.x).normalized * width * 0.5f;
        Triangle(mesh, a - n, a + n, b + n, tint);
        Triangle(mesh, a - n, b + n, b - n, tint);
    }

    private void Vine(VertexHelper mesh, Vector2 p, Vector2 direction)
    {
        Color gold = new Color(0.94f,0.71f,0.30f);
        Vector2 bend = p + new Vector2(direction.x * 9f, -direction.y * 3f);
        Vector2 end = p + new Vector2(direction.x * 16f, -direction.y * 12f);
        Stroke(mesh,p,bend,2.4f,gold);
        Stroke(mesh,bend,end,1.8f,gold);
        Triangle(mesh,bend,bend + new Vector2(-direction.x * 6f,-direction.y * 8f),
            bend + new Vector2(direction.x * 3f,-direction.y * 5f),new Color(0.65f,0.42f,0.12f));
        Diamond(mesh,p,3f,2f,new Color(1f,0.88f,0.51f));
    }
}
