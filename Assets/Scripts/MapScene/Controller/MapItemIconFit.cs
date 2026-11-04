using UnityEngine;
using UnityEngine.UI;

/// <summary>Fits the imported sprite silhouette, rather than its transparent canvas, inside an item icon.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Image))]
public sealed class MapItemIconFit : BaseMeshEffect
{
    public static void Attach(Image image)
    {
        if (image != null && image.GetComponent<MapItemIconFit>() == null)
            image.gameObject.AddComponent<MapItemIconFit>();
    }

    public override void ModifyMesh(VertexHelper mesh)
    {
        if (!IsActive()) return;
        Image image = graphic as Image;
        if (image == null || image.type != Image.Type.Simple) return;
        Sprite sprite = image.overrideSprite;
        if (sprite == null) return;

        // Tight sprite geometry is available even on non-readable textures and packed atlases.
        // Reuse its UVs so UI alignment never changes the source art, pivot or world renderers.
        Vector2[] vertices = sprite.vertices;
        Vector2[] uv = sprite.uv;
        if (vertices.Length == 0 || vertices.Length != uv.Length) return;
        Vector2 min = vertices[0], max = vertices[0];
        for (int i = 1; i < vertices.Length; i++)
        {
            min = Vector2.Min(min, vertices[i]);
            max = Vector2.Max(max, vertices[i]);
        }

        Rect rect = image.GetPixelAdjustedRect();
        Vector2 size = max - min;
        if (size.x <= 0 || size.y <= 0 || rect.width <= 0 || rect.height <= 0) return;
        float scale = Mathf.Min(rect.width / size.x, rect.height / size.y);
        Vector2 center = (min + max) * 0.5f;
        mesh.Clear();
        for (int i = 0; i < vertices.Length; i++)
        {
            UIVertex vertex = UIVertex.simpleVert;
            vertex.position = (vertices[i] - center) * scale + rect.center;
            vertex.uv0 = uv[i];
            vertex.color = image.color;
            mesh.AddVert(vertex);
        }
        ushort[] triangles = sprite.triangles;
        for (int i = 0; i < triangles.Length; i += 3)
            mesh.AddTriangle(triangles[i], triangles[i + 1], triangles[i + 2]);
    }
}
