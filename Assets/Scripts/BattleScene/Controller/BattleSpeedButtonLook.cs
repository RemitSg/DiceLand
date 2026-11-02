using UnityEngine;
using UnityEngine.Rendering;

/// <summary>A sharp badge with a soft emissive halo, independent of scene depth of field and Bloom.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(SpeedButton))]
public sealed class BattleSpeedButtonLook : MonoBehaviour
{
    [SerializeField] private Material glowMaterial;
    [SerializeField] private Mesh glowQuad;
    private SpeedButton button;
    private SpriteRenderer[] bodies;
    private MeshRenderer[] halos;
    private MaterialPropertyBlock properties;
    private MaterialPropertyBlock badgeProperties;
    private Vector4[] badgeBounds;
    private static readonly int IntensityId = Shader.PropertyToID("_Intensity");
    private static readonly int BoundsId = Shader.PropertyToID("_BadgeBounds");
    private static readonly int PressedId = Shader.PropertyToID("_Pressed");

    public void Configure(Material material, Mesh quad)
    {
        ReleaseLook();
        glowMaterial = material;
        glowQuad = quad;
    }

    private void LateUpdate()
    {
        if (Application.isPlaying)
            RefreshLook();
    }

    public void RefreshLook()
    {
        if (button == null)
            button = GetComponent<SpeedButton>();
        if (button == null || button.UsesHeadlessExecution || glowMaterial == null || glowQuad == null)
        {
            ReleaseLook();
            return;
        }
        if (halos == null)
        {
            bodies = new[] { Body(button.unpressedButton), Body(button.pressedButton) };
            halos = new MeshRenderer[bodies.Length];
            badgeBounds = new Vector4[bodies.Length];
            properties = new MaterialPropertyBlock();
            badgeProperties = new MaterialPropertyBlock();
            for (int i = 0; i < bodies.Length; i++)
                halos[i] = CreateHalo(bodies[i], out badgeBounds[i]);
        }
        for (int i = 0; i < halos.Length; i++)
        {
            if (halos[i] == null || bodies[i] == null)
                continue;
            halos[i].gameObject.layer = bodies[i].gameObject.layer;
            halos[i].enabled = bodies[i].enabled;
            halos[i].sortingLayerID = bodies[i].sortingLayerID;
            halos[i].sortingOrder = bodies[i].sortingOrder - 1;
            properties.SetFloat(IntensityId, i == 1 ? 1.25f : button.ConnectedTarget != null ? 1.15f : 1f);
            halos[i].SetPropertyBlock(properties);
            bodies[i].GetPropertyBlock(badgeProperties);
            badgeProperties.SetVector(BoundsId, badgeBounds[i]);
            badgeProperties.SetFloat(PressedId, i == 1 ? 1f : 0f);
            bodies[i].SetPropertyBlock(badgeProperties);
        }
    }

    private static SpriteRenderer Body(GameObject target) => target != null ? target.GetComponent<SpriteRenderer>() : null;

    private MeshRenderer CreateHalo(SpriteRenderer source, out Vector4 bounds)
    {
        bounds = new Vector4(-1f, -1f, 2f, 2f);
        if (source == null || source.sprite == null)
            return null;
        // Sprite.bounds includes the source image's large transparent margins. Fit its tight mesh.
        Vector2[] points = source.sprite.vertices;
        if (points.Length == 0)
            return null;
        Vector2 min = points[0], max = points[0];
        foreach (Vector2 point in points)
        {
            min = Vector2.Min(min, point);
            max = Vector2.Max(max, point);
        }
        Vector2 center = (min + max) * 0.5f;
        bounds = new Vector4(min.x, min.y, Mathf.Max(0.001f, max.x - min.x), Mathf.Max(0.001f, max.y - min.y));
        GameObject halo = new GameObject("Speed badge soft glow");
        halo.transform.SetParent(source.transform, false);
        halo.transform.localPosition = new Vector3(source.flipX ? -center.x : center.x,
            source.flipY ? -center.y : center.y, 0.002f);
        halo.transform.localScale = new Vector3((max.x - min.x) * 1.6f, (max.y - min.y) * 1.6f, 1f);
        halo.AddComponent<MeshFilter>().sharedMesh = glowQuad;
        MeshRenderer renderer = halo.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = glowMaterial;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        return renderer;
    }

    private void OnDisable() => ReleaseLook();

    public void ReleaseLook()
    {
        if (halos == null)
            return;
        foreach (MeshRenderer halo in halos)
        {
            if (halo == null)
                continue;
            halo.gameObject.SetActive(false);
            if (Application.isPlaying) Destroy(halo.gameObject);
            else DestroyImmediate(halo.gameObject);
        }
        halos = null;
        bodies = null;
        properties = null;
        badgeProperties = null;
        badgeBounds = null;
    }
}
