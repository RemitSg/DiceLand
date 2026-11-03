using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Planar sun silhouettes, separate from the existing ambient contact ellipses.</summary>
[DisallowMultipleComponent]
public sealed class BattleActorSunShadowController : MonoBehaviour
{
    [SerializeField] private Transform allies;
    [SerializeField] private Transform enemies;
    [SerializeField] private Material shadowMaterial;
    [SerializeField] private BattleActorFormLibrary formLibrary;
    [SerializeField] private Light sunlight;
    [SerializeField] private float groundHeight = 0.06f;

    private sealed class Entry
    {
        public Transform actor;
        public SpriteRenderer source;
        public MeshRenderer shadow;
        public MeshFilter filter;
    }

    private sealed class Geometry
    {
        public Mesh mesh;
        public Texture2D texture;
        public Vector4 uvRect;
    }

    private readonly List<Transform> actors = new List<Transform>();
    private readonly List<Entry> entries = new List<Entry>();
    private readonly Dictionary<Sprite, Geometry> geometry = new Dictionary<Sprite, Geometry>();
    private MaterialPropertyBlock properties;
    private Transform shadowRoot;
    private float nextRefresh;
    private static readonly int LightingId = Shader.PropertyToID("_DiceLandForestLighting");
    private static readonly int SunId = Shader.PropertyToID("_DiceLandForestSunDirection");
    private static readonly int CasterId = Shader.PropertyToID("_CasterToWorld");
    private static readonly int TextureId = Shader.PropertyToID("_MainTex");
    private static readonly int TexelId = Shader.PropertyToID("_MainTex_TexelSize");
    private static readonly int AlphaId = Shader.PropertyToID("_AlphaTex");
    private static readonly int ExternalAlphaId = Shader.PropertyToID("_UseExternalAlpha");
    private static readonly int MaskId = Shader.PropertyToID("_ShadowMask");
    private static readonly int MaskMetricsId = Shader.PropertyToID("_MaskMetrics");
    private static readonly int WorldToUvId = Shader.PropertyToID("_WorldToUV");
    private static readonly int FlipId = Shader.PropertyToID("_Flip");
    private static readonly int UvRectId = Shader.PropertyToID("_UVRect");
    private static readonly int GroundId = Shader.PropertyToID("_GroundHeight");

    public void Configure(Transform allyRoot, Transform enemyRoot, Material material, BattleActorFormLibrary forms, Light sun)
    {
        ReleaseShadows();
        allies = allyRoot; enemies = enemyRoot; shadowMaterial = material; formLibrary = forms; sunlight = sun;
    }

    private void LateUpdate() => UpdateShadows();

    public void UpdateShadows()
    {
        if (shadowMaterial == null)
            return;
        if (Time.unscaledTime >= nextRefresh)
        {
            actors.Clear();
            BattleActorVisualUtility.CollectActorRoots(allies, actors);
            BattleActorVisualUtility.CollectActorRoots(enemies, actors);
            foreach (Transform actor in actors)
            {
                SpriteRenderer body = null;
                foreach (SpriteRenderer candidate in actor.GetComponentsInChildren<SpriteRenderer>(true))
                    if (candidate.GetComponentInParent<SpeedButton>() == null && candidate.GetComponentInParent<Weapon>() == null
                        && (body == null || candidate.sortingOrder > body.sortingOrder))
                        body = candidate;
                Entry entry = entries.Find(item => item.actor == actor);
                if (entry != null) entry.source = body;
                else if (body != null) entries.Add(new Entry { actor = actor, source = body });
            }
            nextRefresh = Time.unscaledTime + 0.5f;
        }
        Vector3 sun = sunlight != null ? -sunlight.transform.forward : (Vector3)Shader.GetGlobalVector(SunId);
        if (sun.sqrMagnitude < 0.001f) sun = Vector3.up;
        for (int index = entries.Count - 1; index >= 0; index--)
        {
            Entry entry = entries[index];
            if (entry.actor == null)
            {
                if (entry.shadow != null) Release(entry.shadow.gameObject);
                entries.RemoveAt(index);
                continue;
            }
            SpriteRenderer source = entry.source;
            bool visible = Shader.GetGlobalFloat(LightingId) > 0.001f && source != null && source.sprite != null
                && source.enabled && !source.forceRenderingOff && source.gameObject.activeInHierarchy && source.color.a > 0f;
            if (entry.shadow != null) entry.shadow.enabled = visible;
            if (!visible) continue;
            if (properties == null) properties = new MaterialPropertyBlock();
            if (entry.shadow == null)
            {
                if (shadowRoot == null)
                {
                    shadowRoot = new GameObject("Runtime sun shadows").transform;
                    shadowRoot.SetParent(transform, false);
                }
                GameObject item = new GameObject("Sun shadow " + entry.actor.name);
                item.transform.SetParent(shadowRoot, false);
                entry.filter = item.AddComponent<MeshFilter>();
                entry.shadow = item.AddComponent<MeshRenderer>();
                entry.shadow.shadowCastingMode = ShadowCastingMode.Off;
                entry.shadow.receiveShadows = false;
                entry.shadow.lightProbeUsage = LightProbeUsage.Off;
                entry.shadow.reflectionProbeUsage = ReflectionProbeUsage.Off;
            }
            Sprite sprite = source.sprite;
            Texture2D texture = sprite.texture;
            if (!geometry.TryGetValue(sprite, out Geometry shape) || shape.texture != texture)
            {
                if (shape != null) Release(shape.mesh);
                shape = BuildGeometry(sprite);
                geometry[sprite] = shape;
            }
            entry.filter.sharedMesh = shape.mesh;
            entry.shadow.sharedMaterial = shadowMaterial;
            entry.shadow.gameObject.layer = source.gameObject.layer;
            Bounds local = shape.mesh.bounds, projected = default;
            for (int corner = 0; corner < 4; corner++)
            {
                Vector3 point = local.center + Vector3.Scale(local.extents,
                    new Vector3((corner & 1) == 0 ? -1f : 1f, (corner & 2) == 0 ? -1f : 1f, 0f));
                point.x *= source.flipX ? -1f : 1f; point.y *= source.flipY ? -1f : 1f;
                point = source.transform.TransformPoint(point);
                point -= sun * ((point.y - groundHeight) / Mathf.Max(0.2f, sun.y));
                point.y = groundHeight;
                if (corner == 0) projected = new Bounds(point, Vector3.zero);
                else projected.Encapsulate(point);
            }
            projected.Expand(0.08f);
            entry.shadow.bounds = projected; // GPU projection must also be visible to CPU frustum culling.
            properties.Clear();
            properties.SetMatrix(CasterId, source.transform.localToWorldMatrix);
            properties.SetTexture(TextureId, texture);
            properties.SetVector(TexelId, new Vector4(1f / texture.width, 1f / texture.height, texture.width, texture.height));
            Texture2D alpha = sprite.associatedAlphaSplitTexture;
            properties.SetTexture(AlphaId, alpha != null ? alpha : texture);
            properties.SetFloat(ExternalAlphaId, alpha != null ? 1f : 0f);
            properties.SetVector(FlipId, new Vector4(source.flipX ? -1f : 1f, source.flipY ? -1f : 1f, source.color.a, 0f));
            properties.SetFloat(GroundId, groundHeight);
            properties.SetVector(UvRectId, shape.uvRect);
            Texture2D mask = formLibrary != null ? formLibrary.FindShadowMask(sprite) : null;
            properties.SetTexture(MaskId, mask != null ? mask : texture);
            properties.SetVector(MaskMetricsId, mask != null
                ? new Vector4(mask.width, mask.height, mask.mipmapCount - 1, 1f) : Vector4.zero);
            float xScale = Mathf.Max(0.001f, source.transform.TransformVector(Vector3.right).magnitude);
            float yScale = Mathf.Max(0.001f, source.transform.TransformVector(Vector3.up).magnitude);
            properties.SetVector(WorldToUvId, new Vector4(sprite.pixelsPerUnit / (texture.width * xScale),
                sprite.pixelsPerUnit / (texture.height * yScale), 0f, 0f));
            entry.shadow.SetPropertyBlock(properties);
        }
    }

    private static Geometry BuildGeometry(Sprite sprite)
    {
        Mesh mesh = new Mesh { name = "Sun shadow silhouette " + sprite.name };
        Vector2[] uv = sprite.uv;
        Vector2 uvMin = Vector2.one, uvMax = Vector2.zero;
        foreach (Vector2 point in uv) { uvMin = Vector2.Min(uvMin, point); uvMax = Vector2.Max(uvMax, point); }
        if (sprite.packed)
        {
            Vector2[] points = sprite.vertices;
            Vector3[] vertices = new Vector3[points.Length];
            for (int i = 0; i < points.Length; i++) vertices[i] = points[i];
            ushort[] indices = sprite.triangles;
            int[] triangles = new int[indices.Length];
            for (int i = 0; i < indices.Length; i++) triangles[i] = indices[i];
            mesh.vertices = vertices; mesh.uv = uv; mesh.triangles = triangles;
        }
        else
        {
            Rect rect = sprite.rect;
            uvMin = new Vector2(rect.x / sprite.texture.width, rect.y / sprite.texture.height);
            uvMax = new Vector2(rect.xMax / sprite.texture.width, rect.yMax / sprite.texture.height);
            float ppu = sprite.pixelsPerUnit;
            Vector2 lo = -sprite.pivot / ppu, hi = lo + rect.size / ppu;
            float pad = Mathf.Max(rect.width, rect.height) / ppu * 0.05f;
            lo -= Vector2.one * pad; hi += Vector2.one * pad;
            mesh.vertices = new[] { new Vector3(lo.x,lo.y,0f),new Vector3(lo.x,hi.y,0f),new Vector3(hi.x,hi.y,0f),new Vector3(hi.x,lo.y,0f) };
            Vector2 uvLo = new Vector2((sprite.pivot.x + lo.x * ppu + rect.x) / sprite.texture.width,
                (sprite.pivot.y + lo.y * ppu + rect.y) / sprite.texture.height);
            Vector2 uvHi = new Vector2((sprite.pivot.x + hi.x * ppu + rect.x) / sprite.texture.width,
                (sprite.pivot.y + hi.y * ppu + rect.y) / sprite.texture.height);
            mesh.uv = new[] { uvLo,new Vector2(uvLo.x,uvHi.y),uvHi,new Vector2(uvHi.x,uvLo.y) };
            mesh.triangles = new[] { 0,1,2,0,2,3 };
        }
        mesh.RecalculateBounds();
        return new Geometry { mesh = mesh, texture = sprite.texture, uvRect = new Vector4(uvMin.x,uvMin.y,uvMax.x,uvMax.y) };
    }

    private void OnDisable() => ReleaseShadows();

    public void ReleaseShadows()
    {
        if (shadowRoot != null) { shadowRoot.gameObject.SetActive(false); Release(shadowRoot.gameObject); }
        foreach (Geometry shape in geometry.Values) Release(shape.mesh);
        shadowRoot = null; geometry.Clear(); entries.Clear(); actors.Clear(); nextRefresh = 0f;
    }

    private static void Release(Object value)
    {
        if (Application.isPlaying) Destroy(value);
        else DestroyImmediate(value);
    }
}
