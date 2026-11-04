using UnityEngine;
using UnityEngine.UI;

/// <summary>Gold sign silhouettes matching the four functions in the reference art.</summary>
[AddComponentMenu("")]
[RequireComponent(typeof(CanvasRenderer))]
public sealed class RestSceneStationIcon : MaskableGraphic
{
    public RestSceneForestView.Station station;

    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        float s = Mathf.Min(rectTransform.rect.width, rectTransform.rect.height) / 48f;
        switch (station)
        {
            case RestSceneForestView.Station.ActionPoints:
                Polygon(mesh, s, -3,-23, 16,5, 3,5, 10,23, -9,23, -17,-4, -4,-4);
                break;
            case RestSceneForestView.Station.Forge:
                Polygon(mesh, s, -23,15, 22,15, 14,6, 4,3, 3,-9, 12,-15, 12,-20, -13,-20, -13,-15, -4,-9, -5,3, -16,7);
                break;
            case RestSceneForestView.Station.Recruit:
                Person(mesh, s, -15, -2, 0.76f); Person(mesh, s, 15, -2, 0.76f); Person(mesh, s, 0, 2, 1f);
                break;
            case RestSceneForestView.Station.Rest:
                // Join the outer moon arc to an inset inner arc, keeping the crescent opening transparent.
                int start = mesh.currentVertCount;
                for (int i = 0; i <= 24; i++)
                {
                    float a = Mathf.Lerp(65f, 295f, i / 24f) * Mathf.Deg2Rad;
                    Vector2 p = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                    mesh.AddVert(p * 21f * s, color, Vector2.zero);
                    float innerA = Mathf.Lerp(91f, 269f, i / 24f) * Mathf.Deg2Rad;
                    mesh.AddVert((new Vector2(9, 0) + new Vector2(Mathf.Cos(innerA), Mathf.Sin(innerA)) * 19f) * s, color, Vector2.zero);
                    if (i == 0) continue;
                    int n = start + i * 2; mesh.AddTriangle(n-2,n-1,n); mesh.AddTriangle(n-1,n+1,n);
                }
                Polygon(mesh, s, 12,19, 14,14, 20,12, 14,10, 12,4, 10,10, 4,12, 10,14);
                break;
        }
    }

    private void Person(VertexHelper mesh, float scale, float x, float y, float size)
    {
        int first = mesh.currentVertCount;
        mesh.AddVert(new Vector2(x, y + 10) * scale, color, Vector2.zero);
        for (int i = 0; i <= 16; i++)
        {
            float a = i * Mathf.PI * 2f / 16;
            mesh.AddVert((new Vector2(x, y + 10) + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 7.5f * size) * scale, color, Vector2.zero);
            if (i > 0) mesh.AddTriangle(first, first+i, first+i+1);
        }
        Polygon(mesh, scale, x-5*size,y+2, x+5*size,y+2, x+11*size,y-7, x+13*size,y-18, x-13*size,y-18, x-11*size,y-7);
    }

    private void Polygon(VertexHelper mesh, float scale, params float[] points)
    {
        int first = mesh.currentVertCount;
        Vector2 center = Vector2.zero;
        for (int i = 0; i < points.Length; i += 2) center += new Vector2(points[i], points[i+1]);
        center /= points.Length / 2;
        mesh.AddVert(center * scale, color, Vector2.zero);
        for (int i = 0; i < points.Length; i += 2) mesh.AddVert(new Vector2(points[i], points[i+1]) * scale, color, Vector2.zero);
        for (int i = 0; i < points.Length / 2; i++) mesh.AddTriangle(first, first+i+1, first+(i+1)%(points.Length/2)+1);
    }
}
