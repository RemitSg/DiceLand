using UnityEngine;
using UnityEngine.UI;

/// <summary>Small, non-blocking light pulses over the painted crystal, hearth and bedside lantern.</summary>
[AddComponentMenu("")]
[RequireComponent(typeof(CanvasRenderer))]
public sealed class RestSceneAmbientGlow : MaskableGraphic
{
    private void Update() { if (Application.isPlaying) SetVerticesDirty(); }

    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        float t = Application.isPlaying ? Time.unscaledTime : 0f;
        Glow(mesh, new Vector2(260f, 525f), new Vector2(30f, 48f), new Color(0.25f, 0.94f, 1f, 0.1f + Mathf.Sin(t * 1.6f) * 0.035f));
        Glow(mesh, new Vector2(610f, 495f), new Vector2(70f, 55f), new Color(1f, 0.59f, 0.15f, 0.08f + Mathf.Sin(t * 5.1f) * 0.025f));
        Glow(mesh, new Vector2(1560f, 430f), new Vector2(26f, 36f), new Color(1f, 0.78f, 0.36f, 0.08f + Mathf.Sin(t * 2f) * 0.02f));
    }

    private void Glow(VertexHelper mesh, Vector2 position, Vector2 radius, Color tint)
    {
        Vector2 center = new Vector2(position.x - 836f, 470.5f - position.y);
        int first = mesh.currentVertCount;
        mesh.AddVert(center, tint, Vector2.zero);
        tint.a = 0f;
        const int sides = 24;
        for (int i = 0; i <= sides; i++)
        {
            float angle = i * Mathf.PI * 2f / sides;
            mesh.AddVert(center + new Vector2(Mathf.Cos(angle) * radius.x, Mathf.Sin(angle) * radius.y), tint, Vector2.zero);
            if (i > 0) mesh.AddTriangle(first, first + i, first + i + 1);
        }
    }
}
