using UnityEngine;
using UnityEngine.UI;

/// <summary>Small UGUI ornaments, drawn without textures or input interception.</summary>
[AddComponentMenu("")]
[RequireComponent(typeof(CanvasRenderer))]
public sealed class MainMenuOrnament : MaskableGraphic
{
    public enum Shape { Parchment, Divider, Diamond, Book, Ability, Settings, People, Exit, Swords, Shade, Motes }
    public Shape kind;
    private static readonly Color Gold = new Color(0.72f, 0.51f, 0.23f);

    private void Update()
    {
        if (kind == Shape.Motes && Application.isPlaying)
            SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        Rect r = rectTransform.rect;
        if (kind == Shape.Shade)
        {
            Gradient(mesh, r, new Color(0.005f, 0.018f, 0.024f, 0.32f), Color.clear, true);
            return;
        }
        if (kind == Shape.Parchment)
        {
            Plaque(mesh, r, new Color(0.22f, 0.13f, 0.045f));
            Plaque(mesh, Inset(r, 3f), Gold);
            Plaque(mesh, Inset(r, 6f), new Color(0.33f, 0.20f, 0.075f));
            Plaque(mesh, Inset(r, 8f), new Color(0.97f, 0.85f, 0.59f));
            Rect inner = Inset(r, 15f);
            Gradient(mesh, new Rect(inner.xMin + 22f, inner.yMin, inner.width - 44f, inner.height),
                new Color(1f, 0.95f, 0.75f, 0.85f), new Color(0.94f, 0.75f, 0.41f, 0.1f), false);
            Diamond(mesh, new Vector2(r.xMin + 18f, r.center.y), 9f, new Color(0.98f, 0.80f, 0.40f), false);
            Diamond(mesh, new Vector2(r.xMax - 18f, r.center.y), 9f, new Color(0.98f, 0.80f, 0.40f), false);
            return;
        }
        if (kind == Shape.Divider)
        {
            Stroke(mesh, new Vector2(r.xMin + 10f, 0f), new Vector2(r.xMax - 10f, 0f), 1.4f, color);
            Diamond(mesh, new Vector2(r.xMin + 6f, 0f), 6f, color, false);
            Diamond(mesh, new Vector2(r.xMax - 6f, 0f), 6f, color, false);
            return;
        }
        if (kind == Shape.Motes)
        {
            float time = Application.isPlaying ? Time.unscaledTime : 0f;
            for (int i = 0; i < 22; i++)
            {
                float x = Mathf.Repeat(i * 0.618034f + 0.1f, 1f);
                float y = Mathf.Repeat(i * 0.381966f + time * (0.008f + i % 3 * 0.002f), 1f);
                float alpha = Mathf.Sin(y * Mathf.PI) * (0.18f + 0.12f * Mathf.Sin(time + i));
                Diamond(mesh, new Vector2(r.xMin + r.width * x, r.yMin + r.height * y), 1.5f + i % 3,
                    new Color(1f, 0.85f, 0.48f, alpha), true);
            }
            return;
        }

        // Icons are described in a -24..24 coordinate space.
        float scale = Mathf.Min(r.width, r.height) / 52f;
        switch (kind)
        {
            case Shape.Diamond:
                Diamond(mesh, Vector2.zero, 19f * scale, color, false);
                Diamond(mesh, Vector2.zero, 10f * scale, color, true);
                break;
            case Shape.Book:
                Path(mesh, scale, -22,-19, -22,18, -6,21, 0,16, 6,21, 22,18, 22,-19, 6,-16, 0,-22, -6,-16, -22,-19);
                Path(mesh, scale, 0,16, 0,-17);
                Path(mesh, scale, -17,10, -7,12); Path(mesh, scale, -17,3, -7,5);
                Path(mesh, scale, 7,12, 17,10); Path(mesh, scale, 7,5, 17,3);
                break;
            case Shape.Ability:
                Ring(mesh, Vector2.zero, 21f * scale, 1.6f * scale, color);
                Path(mesh, scale, 0,-22, 0,20, -8,11, 0,15, 8,11);
                Path(mesh, scale, -16,7, -9,-2, 0,-5, 9,-2, 16,7);
                Diamond(mesh, new Vector2(0f, 20f) * scale, 4f * scale, color, true);
                break;
            case Shape.Settings:
                Ring(mesh, Vector2.zero, 14f * scale, 5f * scale, color);
                for (int i = 0; i < 8; i++)
                {
                    Vector2 direction = new Vector2(Mathf.Sin(i * Mathf.PI / 4), Mathf.Cos(i * Mathf.PI / 4));
                    Stroke(mesh, direction * 14f * scale, direction * 23f * scale, 8f * scale, color);
                }
                break;
            case Shape.People:
                Ring(mesh, new Vector2(0, 10) * scale, 6f * scale, 6f * scale, color);
                Ring(mesh, new Vector2(-16, 5) * scale, 4.5f * scale, 4.5f * scale, color);
                Ring(mesh, new Vector2(16, 5) * scale, 4.5f * scale, 4.5f * scale, color);
                Stroke(mesh, new Vector2(0,-2) * scale, new Vector2(0,-16) * scale, 18f * scale, color);
                Stroke(mesh, new Vector2(-17,-5) * scale, new Vector2(-17,-16) * scale, 11f * scale, color);
                Stroke(mesh, new Vector2(17,-5) * scale, new Vector2(17,-16) * scale, 11f * scale, color);
                break;
            case Shape.Exit:
                Path(mesh, scale, 0,21, -18,21, -18,-21, 0,-21);
                Path(mesh, scale, -6,0, 23,0, 14,9); Path(mesh, scale, 23,0, 14,-9);
                break;
            case Shape.Swords:
                Path(mesh, scale, -19,-19, 17,18, 22,22, 19,12, -14,-20);
                Path(mesh, scale, 19,-19, -17,18, -22,22, -19,12, 14,-20);
                Path(mesh, scale, -21,-10, -10,-21); Path(mesh, scale, 21,-10, 10,-21);
                break;
        }
    }

    private void Path(VertexHelper mesh, float scale, params float[] points)
    {
        for (int i = 2; i < points.Length; i += 2)
            Stroke(mesh, new Vector2(points[i-2], points[i-1]) * scale,
                new Vector2(points[i], points[i+1]) * scale, 2.3f * scale, color);
    }

    private static Rect Inset(Rect r, float amount) => new Rect(r.x + amount, r.y + amount, r.width - amount * 2f, r.height - amount * 2f);

    private void Plaque(VertexHelper mesh, Rect r, Color tint)
    {
        float tip = Mathf.Min(26f, r.height * 0.3f);
        Vector2[] points = { new Vector2(r.xMin, r.center.y), new Vector2(r.xMin + tip,r.yMax),
            new Vector2(r.xMax-tip,r.yMax), new Vector2(r.xMax,r.center.y),
            new Vector2(r.xMax-tip,r.yMin), new Vector2(r.xMin+tip,r.yMin) };
        int index = mesh.currentVertCount;
        mesh.AddVert(r.center, tint * color, Vector2.zero);
        foreach (Vector2 p in points) mesh.AddVert(p, tint * color, Vector2.zero);
        for (int i = 0; i < points.Length; i++) mesh.AddTriangle(index, index + 1 + i, index + 1 + (i+1) % points.Length);
    }

    private static void Gradient(VertexHelper mesh, Rect r, Color first, Color second, bool horizontal)
    {
        int index = mesh.currentVertCount;
        mesh.AddVert(new Vector2(r.xMin,r.yMin), first, Vector2.zero);
        mesh.AddVert(new Vector2(r.xMin,r.yMax), horizontal ? first : second, Vector2.zero);
        mesh.AddVert(new Vector2(r.xMax,r.yMax), second, Vector2.zero);
        mesh.AddVert(new Vector2(r.xMax,r.yMin), horizontal ? second : first, Vector2.zero);
        mesh.AddTriangle(index,index+1,index+2); mesh.AddTriangle(index+2,index+3,index);
    }

    private static void Diamond(VertexHelper mesh, Vector2 p, float size, Color tint, bool filled)
    {
        if (filled) Quad(mesh,p+Vector2.left*size,p+Vector2.up*size,p+Vector2.right*size,p+Vector2.down*size,tint);
        else
        {
            Stroke(mesh,p+Vector2.left*size,p+Vector2.up*size,1.6f,tint);
            Stroke(mesh,p+Vector2.up*size,p+Vector2.right*size,1.6f,tint);
            Stroke(mesh,p+Vector2.right*size,p+Vector2.down*size,1.6f,tint);
            Stroke(mesh,p+Vector2.down*size,p+Vector2.left*size,1.6f,tint);
        }
    }

    private static void Ring(VertexHelper mesh, Vector2 center, float radius, float width, Color tint)
    {
        for (int i = 0; i < 32; i++)
        {
            float a = i * Mathf.PI / 16f, b = (i+1) * Mathf.PI / 16f;
            Stroke(mesh,center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius,
                center+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*radius,width,tint);
        }
    }

    private static void Stroke(VertexHelper mesh, Vector2 a, Vector2 b, float width, Color tint)
    {
        Vector2 cross = new Vector2(-(b-a).y,(b-a).x).normalized * width * 0.5f;
        Quad(mesh,a-cross,a+cross,b+cross,b-cross,tint);
    }

    private static void Quad(VertexHelper mesh, Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color tint)
    {
        int index = mesh.currentVertCount;
        mesh.AddVert(a,tint,Vector2.zero); mesh.AddVert(b,tint,Vector2.zero);
        mesh.AddVert(c,tint,Vector2.zero); mesh.AddVert(d,tint,Vector2.zero);
        mesh.AddTriangle(index,index+1,index+2); mesh.AddTriangle(index+2,index+3,index);
    }
}
