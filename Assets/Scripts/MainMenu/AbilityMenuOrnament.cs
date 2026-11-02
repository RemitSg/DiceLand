using UnityEngine;
using UnityEngine.UI;

/// <summary>Resolution-independent decoration; never participates in UI hit testing.</summary>
[AddComponentMenu("")]
[RequireComponent(typeof(CanvasRenderer))]
public sealed class AbilityMenuOrnament : MaskableGraphic
{
    public enum Shape { Tree, Divider, Diamond }
    public Shape kind;

    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        Rect rect = rectTransform.rect;
        if (kind == Shape.Divider)
        {
            float half = rect.width * 0.5f;
            Stroke(mesh, new Vector2(-half, 0f), new Vector2(-12f, 0f), 1.2f);
            Stroke(mesh, new Vector2(12f, 0f), new Vector2(half, 0f), 1.2f);
            Diamond(mesh, Vector2.zero, 5f, false);
            Diamond(mesh, new Vector2(-12f, 0f), 2.1f, true);
            Diamond(mesh, new Vector2(12f, 0f), 2.1f, true);
            return;
        }
        if (kind == Shape.Diamond)
        {
            Diamond(mesh, Vector2.zero, Mathf.Min(rect.width, rect.height) * 0.46f, false);
            Diamond(mesh, Vector2.zero, Mathf.Min(rect.width, rect.height) * 0.19f, true);
            return;
        }

        float unit = Mathf.Min(rect.width, rect.height);
        Ring(mesh, unit * 0.475f, 1.3f);
        Ring(mesh, unit * 0.438f, 0.8f);
        for (int i = 0; i < 12; i++)
        {
            float angle = i * Mathf.PI / 6f;
            Vector2 p = new Vector2(Mathf.Sin(angle), Mathf.Cos(angle)) * unit * 0.457f;
            Diamond(mesh, p, unit * 0.007f, false);
        }
        Curve(mesh, new Vector2(-0.025f, -0.29f) * unit, new Vector2(0.06f, -0.04f) * unit, new Vector2(-0.045f, 0.21f) * unit, unit * 0.018f);
        Curve(mesh, new Vector2(0.045f, -0.29f) * unit, new Vector2(-0.04f, -0.01f) * unit, new Vector2(0.025f, 0.24f) * unit, unit * 0.009f);
        for (int side = -1; side <= 1; side += 2)
        {
            for (int i = 0; i < 6; i++)
            {
                float y = -0.13f + i * 0.054f;
                Vector2 start = new Vector2(0f, y) * unit;
                Vector2 bend = new Vector2(side * (0.1f + (5 - i) * 0.028f), y + 0.015f) * unit;
                Vector2 end = new Vector2(side * (0.13f + (5 - i) * 0.038f), y + 0.15f) * unit;
                Curve(mesh, start, bend, end, unit * (0.008f - i * 0.0008f));
                Leaf(mesh, end, new Vector2(side * 0.6f, 1f).normalized, unit * 0.035f);
                Leaf(mesh, Vector2.Lerp(bend, end, 0.63f), new Vector2(-side * 0.3f, 1f).normalized, unit * 0.026f);
                Vector2 root = new Vector2(side * (0.13f + i * 0.023f), -0.29f - (i % 3) * 0.018f) * unit;
                Curve(mesh, new Vector2(side * 0.02f, -0.2f) * unit, new Vector2(side * 0.07f, -0.29f) * unit, root, unit * 0.004f);
            }
        }
        Leaf(mesh, new Vector2(0.025f, 0.26f) * unit, Vector2.up, unit * 0.042f);
    }

    private void Ring(VertexHelper mesh, float radius, float width)
    {
        for (int i = 0; i < 96; i++)
        {
            float a = i * Mathf.PI * 2f / 96f;
            float b = (i + 1) * Mathf.PI * 2f / 96f;
            Stroke(mesh, new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius, new Vector2(Mathf.Cos(b), Mathf.Sin(b)) * radius, width);
        }
    }

    private void Curve(VertexHelper mesh, Vector2 start, Vector2 control, Vector2 end, float width)
    {
        Vector2 previous = start;
        for (int i = 1; i <= 16; i++)
        {
            float t = i / 16f;
            Vector2 point = (1f - t) * (1f - t) * start + 2f * (1f - t) * t * control + t * t * end;
            Stroke(mesh, previous, point, Mathf.Lerp(width, width * 0.3f, t));
            previous = point;
        }
    }

    private void Leaf(VertexHelper mesh, Vector2 point, Vector2 direction, float length)
    {
        Vector2 cross = new Vector2(-direction.y, direction.x) * length * 0.27f;
        Quad(mesh, point - direction * length * 0.5f, point + cross, point + direction * length * 0.5f, point - cross);
    }

    private void Diamond(VertexHelper mesh, Vector2 point, float radius, bool fill)
    {
        Vector2 left = point + Vector2.left * radius;
        Vector2 top = point + Vector2.up * radius;
        Vector2 right = point + Vector2.right * radius;
        Vector2 bottom = point + Vector2.down * radius;
        if (fill)
            Quad(mesh, left, top, right, bottom);
        else
        {
            Stroke(mesh, left, top, 1.7f);
            Stroke(mesh, top, right, 1.7f);
            Stroke(mesh, right, bottom, 1.7f);
            Stroke(mesh, bottom, left, 1.7f);
        }
    }

    private void Stroke(VertexHelper mesh, Vector2 start, Vector2 end, float width)
    {
        Vector2 normal = new Vector2(-(end - start).y, (end - start).x).normalized * width * 0.5f;
        Quad(mesh, start - normal, start + normal, end + normal, end - normal);
    }

    private void Quad(VertexHelper mesh, Vector2 a, Vector2 b, Vector2 c, Vector2 d)
    {
        int index = mesh.currentVertCount;
        mesh.AddVert(a, color, Vector2.zero);
        mesh.AddVert(b, color, Vector2.zero);
        mesh.AddVert(c, color, Vector2.zero);
        mesh.AddVert(d, color, Vector2.zero);
        mesh.AddTriangle(index, index + 1, index + 2);
        mesh.AddTriangle(index + 2, index + 3, index);
    }
}
