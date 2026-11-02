using UnityEngine;
using UnityEngine.UI;

/// <summary>Small scalable feedback meshes, with no textures, materials, or raycast surfaces.</summary>
[RequireComponent(typeof(CanvasRenderer))]
public sealed class MapFeedbackGraphic : MaskableGraphic
{
    public enum Shape { Ring, Spark, Vignette, WalletHalo, SoftDot }
    public Shape shape;
    public float animationPhase, impactPulse, completion = 1f;

    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        Rect rect = rectTransform.rect;
        if (shape == Shape.SoftDot)
        {
            SoftDisc(mesh, rect.center, rect.size * 0.5f, color);
            return;
        }
        if (shape == Shape.WalletHalo)
        {
            Vector2 radius = rect.size * 0.46f;
            // Quiet brass crescents surround the currency icon; the amount stays unobscured.
            Arc(mesh, rect.center, radius, 26f, 94f, 1.5f, Alpha(color, 0.35f));
            Arc(mesh, rect.center, radius, 206f, 94f, 1.5f, Alpha(color, 0.3f));
            float orbit = animationPhase * 52f;
            Arc(mesh, rect.center, radius * 0.81f, orbit - 40f, 66f, 1.7f, Alpha(color, 0.72f));
            Arc(mesh, rect.center, radius * 0.81f, orbit + 140f, 45f, 1.1f, Alpha(color, 0.4f));
            Color light = new Color(1f, 0.95f, 0.75f, color.a);
            SoftDisc(mesh, rect.center, new Vector2(13f, 11f), Alpha(light, impactPulse * 0.3f));
            Diamond(mesh, rect.center, new Vector2(5.5f, 8f), Alpha(light, impactPulse * 0.85f));
            for (int i = 0; i < 3; i++)
            {
                float angle = (orbit + 30f + i * 120f) * Mathf.Deg2Rad;
                Vector2 point = rect.center + new Vector2(Mathf.Cos(angle) * radius.x, Mathf.Sin(angle) * radius.y);
                Diamond(mesh, point, new Vector2(2f, 3.2f), Alpha(light, 0.36f));
            }
            if (completion < 1f)
            {
                float fade = Mathf.Pow(1f - completion, 2f);
                Vector2 wave = new Vector2(22f, 18f) * Mathf.Lerp(0.85f, 1.9f, completion);
                Arc(mesh, rect.center, wave, -72f, 300f, 1.6f, Alpha(light, fade * 0.6f));
            }
            return;
        }
        if (shape == Shape.Vignette)
        {
            Vector2[] outer = { new Vector2(rect.xMin, rect.yMin), new Vector2(rect.xMin, rect.yMax),
                new Vector2(rect.xMax, rect.yMax), new Vector2(rect.xMax, rect.yMin) };
            float inset = Mathf.Min(rect.width, rect.height) * 0.14f;
            for (int i = 0; i < 4; i++)
            {
                Vector2 inner = outer[i] + new Vector2(i < 2 ? inset : -inset, i == 0 || i == 3 ? inset : -inset);
                mesh.AddVert(outer[i], color, Vector2.zero);
                mesh.AddVert(inner, new Color(color.r, color.g, color.b, 0f), Vector2.zero);
            }
            for (int i = 0; i < 4; i++)
            {
                int next = (i + 1) % 4;
                mesh.AddTriangle(i * 2, next * 2, i * 2 + 1);
                mesh.AddTriangle(next * 2, next * 2 + 1, i * 2 + 1);
            }
            return;
        }
        if (shape == Shape.Spark)
        {
            mesh.AddVert(rect.center, color, Vector2.zero);
            for (int i = 0; i < 8; i++)
            {
                float angle = i * Mathf.PI / 4f;
                float radius = i % 2 == 0 ? 0.5f : 0.13f;
                mesh.AddVert(rect.center + new Vector2(Mathf.Cos(angle) * rect.width, Mathf.Sin(angle) * rect.height) * radius,
                    new Color(color.r, color.g, color.b, 0f), Vector2.zero);
            }
            for (int i = 0; i < 8; i++) mesh.AddTriangle(0, i + 1, (i + 1) % 8 + 1);
            return;
        }
        const int segments = 32;
        for (int i = 0; i <= segments; i++)
        {
            float angle = i * Mathf.PI * 2f / segments;
            Vector2 direction = new Vector2(Mathf.Cos(angle) * rect.width, Mathf.Sin(angle) * rect.height) * 0.5f;
            mesh.AddVert(rect.center + direction, new Color(color.r, color.g, color.b, 0f), Vector2.zero);
            mesh.AddVert(rect.center + direction * 0.82f, color, Vector2.zero);
            mesh.AddVert(rect.center + direction * 0.68f, new Color(color.r, color.g, color.b, 0f), Vector2.zero);
        }
        for (int i = 0; i < segments; i++)
            for (int j = 0; j < 2; j++)
            {
                int first = i * 3 + j;
                mesh.AddTriangle(first, first + 3, first + 1);
                mesh.AddTriangle(first + 1, first + 3, first + 4);
            }
    }

    private static Color Alpha(Color tint, float opacity) => new Color(tint.r, tint.g, tint.b, tint.a * opacity);

    private static void Diamond(VertexHelper mesh, Vector2 center, Vector2 radius, Color tint)
    {
        int first = mesh.currentVertCount;
        mesh.AddVert(center, tint, Vector2.zero);
        mesh.AddVert(center + new Vector2(-radius.x, 0), Alpha(tint, 0f), Vector2.zero);
        mesh.AddVert(center + new Vector2(0, radius.y), Alpha(tint, 0f), Vector2.zero);
        mesh.AddVert(center + new Vector2(radius.x, 0), Alpha(tint, 0f), Vector2.zero);
        mesh.AddVert(center + new Vector2(0, -radius.y), Alpha(tint, 0f), Vector2.zero);
        for (int i = 0; i < 4; i++) mesh.AddTriangle(first, first + i + 1, first + (i + 1) % 4 + 1);
    }

    private static void SoftDisc(VertexHelper mesh, Vector2 center, Vector2 radius, Color tint)
    {
        const int segments = 20;
        int first = mesh.currentVertCount;
        mesh.AddVert(center, tint, Vector2.zero);
        for (int i = 0; i <= segments; i++)
        {
            float angle = i * Mathf.PI * 2f / segments;
            mesh.AddVert(center + new Vector2(Mathf.Cos(angle) * radius.x, Mathf.Sin(angle) * radius.y), Alpha(tint, 0f), Vector2.zero);
        }
        for (int i = 0; i < segments; i++) mesh.AddTriangle(first, first + i + 1, first + i + 2);
    }

    private static void Arc(VertexHelper mesh, Vector2 center, Vector2 radius, float start, float sweep, float width, Color tint)
    {
        int segments = Mathf.Max(8, Mathf.CeilToInt(sweep / 6f));
        int first = mesh.currentVertCount;
        for (int i = 0; i <= segments; i++)
        {
            float t = i / (float)segments, angle = (start + sweep * t) * Mathf.Deg2Rad;
            Vector2 normal = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            Vector2 point = center + new Vector2(normal.x * radius.x, normal.y * radius.y);
            Color mid = Alpha(tint, Mathf.Sin(t * Mathf.PI));
            mesh.AddVert(point + normal * width, Alpha(tint, 0f), Vector2.zero);
            mesh.AddVert(point, mid, Vector2.zero);
            mesh.AddVert(point - normal * width, Alpha(tint, 0f), Vector2.zero);
        }
        for (int i = 0; i < segments; i++)
            for (int band = 0; band < 2; band++)
            {
                int index = first + i * 3 + band;
                mesh.AddTriangle(index, index + 3, index + 1);
                mesh.AddTriangle(index + 1, index + 3, index + 4);
            }
    }
}
