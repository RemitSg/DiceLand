using UnityEngine;
using UnityEngine.UI;

/// <summary>Small scalable feedback meshes, with no textures, materials, or raycast surfaces.</summary>
[RequireComponent(typeof(CanvasRenderer))]
public sealed class MapFeedbackGraphic : MaskableGraphic
{
    public enum Shape { Ring, Spark, Vignette }
    public Shape shape;

    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        Rect rect = rectTransform.rect;
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
}
