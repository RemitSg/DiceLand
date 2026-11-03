using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Small shared-material ground shadows keep illustrated combatants grounded.</summary>
[DisallowMultipleComponent]
public sealed class BattleContactShadowController : MonoBehaviour
{
    [SerializeField] private Transform allies;
    [SerializeField] private Transform enemies;
    [SerializeField] private Material shadowMaterial;
    [SerializeField] private float groundHeight = 0.055f;

    private sealed class Entry
    {
        public Transform actor;
        public SpriteRenderer[] sprites;
        public MeshRenderer shadow;
        public MeshRenderer secondShadow;
    }

    private struct Support
    {
        public float minX, maxX, bottom;
    }

    private struct Footprint
    {
        public bool split;
        public Support first, second;
    }

    private readonly List<Entry> entries = new List<Entry>();
    private readonly List<Transform> scratchActors = new List<Transform>();
    private readonly Dictionary<Sprite, Footprint> footprints = new Dictionary<Sprite, Footprint>();
    private MaterialPropertyBlock properties;
    private static readonly int StrengthId = Shader.PropertyToID("_ContactStrength");
    private Transform shadowRoot;
    private Mesh quad;
    private float nextRefresh;

    public void Configure(Transform allyRoot, Transform enemyRoot, Material material)
    {
        ReleaseShadows();
        allies = allyRoot;
        enemies = enemyRoot;
        shadowMaterial = material;
    }

    private void LateUpdate()
    {
        UpdateShadows();
    }

    public void UpdateShadows()
    {
        if (shadowMaterial == null)
            return;
        if (shadowRoot == null)
        {
            shadowRoot = new GameObject("Runtime contact shadows").transform;
            shadowRoot.SetParent(transform, false);
            quad = new Mesh { name = "Ground shadow quad" };
            quad.vertices = new[] { new Vector3(-0.5f, 0, -0.5f), new Vector3(-0.5f, 0, 0.5f), new Vector3(0.5f, 0, 0.5f), new Vector3(0.5f, 0, -0.5f) };
            quad.uv = new[] { Vector2.zero, Vector2.up, Vector2.one, Vector2.right };
            quad.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            quad.RecalculateBounds();
        }
        if (Time.unscaledTime >= nextRefresh || entries.Count == 0)
        {
            Collect(allies);
            Collect(enemies);
            nextRefresh = Time.unscaledTime + 0.5f;
        }
        for (int i = entries.Count - 1; i >= 0; i--)
        {
            Entry entry = entries[i];
            if (entry.actor == null)
            {
                if (entry.shadow != null)
                    Release(entry.shadow.gameObject);
                if (entry.secondShadow != null)
                    Release(entry.secondShadow.gameObject);
                entries.RemoveAt(i);
                continue;
            }
            Bounds bounds = default;
            bool visible = false;
            SpriteRenderer singleBody = null;
            int visibleBodies = 0;
            foreach (SpriteRenderer sprite in entry.sprites)
            {
                if (sprite == null || sprite.sprite == null || !sprite.enabled || sprite.forceRenderingOff
                    || !sprite.gameObject.activeInHierarchy || sprite.color.a <= 0f)
                    continue;
                if (!visible)
                    bounds = sprite.bounds;
                else
                    bounds.Encapsulate(sprite.bounds);
                visible = true;
                singleBody = sprite;
                visibleBodies++;
            }
            entry.shadow.enabled = visible;
            if (entry.secondShadow != null) entry.secondShadow.enabled = false;
            if (!visible)
                continue;
            if (visibleBodies == 1 && singleBody.drawMode == SpriteDrawMode.Simple && !singleBody.flipY
                && singleBody.transform.TransformDirection(Vector3.up).y > 0.85f
                && singleBody.transform.TransformVector(Vector3.up).y > 0f)
            {
                Sprite sprite = singleBody.sprite;
                if (!footprints.TryGetValue(sprite, out Footprint footprint))
                    footprints[sprite] = footprint = BuildFootprint(sprite);
                if (footprint.split)
                {
                    if (entry.secondShadow == null)
                        entry.secondShadow = CreateShadow("Contact shadow " + entry.actor.name + " foot 2");
                    entry.secondShadow.enabled = true;
                    PlaceFoot(entry.shadow, singleBody, bounds, footprint.first);
                    PlaceFoot(entry.secondShadow, singleBody, bounds, footprint.second);
                    continue;
                }
            }
            float width = Mathf.Clamp(bounds.size.x * 0.52f, 0.55f, 4.5f);
            entry.shadow.transform.position = new Vector3(bounds.center.x, groundHeight, bounds.center.z + 0.12f);
            entry.shadow.transform.localScale = new Vector3(width, 1f, Mathf.Max(0.32f, width * 0.30f));
            SetStrength(entry.shadow, 1f);
        }
    }

    private void PlaceFoot(MeshRenderer shadow, SpriteRenderer body, Bounds bounds, Support support)
    {
        Vector3 local = new Vector3((support.minX + support.maxX) * 0.5f * (body.flipX ? -1f : 1f), support.bottom, 0f);
        Vector3 sole = body.transform.TransformPoint(local);
        float lift = Mathf.Max(0f, sole.y - groundHeight);
        float relativeLift = lift / Mathf.Max(0.1f, bounds.size.y);
        float width = Mathf.Clamp((support.maxX - support.minX)
            * body.transform.TransformVector(Vector3.right).magnitude * 1.08f, 0.28f, 4.5f);
        width *= 1f + Mathf.Clamp01(relativeLift) * 1.5f;
        shadow.transform.position = new Vector3(sole.x, groundHeight, sole.z + 0.07f);
        shadow.transform.localScale = new Vector3(width, 1f, Mathf.Max(0.20f, width * 0.32f));
        SetStrength(shadow, body.color.a / (1f + relativeLift * 5f));
    }

    private void SetStrength(MeshRenderer shadow, float strength)
    {
        if (properties == null) properties = new MaterialPropertyBlock();
        properties.Clear(); properties.SetFloat(StrengthId, strength); shadow.SetPropertyBlock(properties);
    }

    private static Footprint BuildFootprint(Sprite sprite)
    {
        Vector2[] vertices = sprite.vertices;
        if (vertices.Length < 8) return default;
        float minX = float.MaxValue, maxX = float.MinValue, minY = float.MaxValue, maxY = float.MinValue;
        foreach (Vector2 vertex in vertices)
        {
            minX = Mathf.Min(minX, vertex.x); maxX = Mathf.Max(maxX, vertex.x);
            minY = Mathf.Min(minY, vertex.y); maxY = Mathf.Max(maxY, vertex.y);
        }
        float width = maxX - minX;
        if (width < 0.001f || maxY - minY < 0.001f) return default;
        var low = new List<Vector2>();
        foreach (Vector2 vertex in vertices)
            if (vertex.y <= minY + (maxY - minY) * 0.10f) low.Add(vertex);
        low.Sort((a,b) => a.x.CompareTo(b.x));
        int split = -1, gaps = 0;
        for (int i = 1; i < low.Count; i++)
            if (low[i].x - low[i-1].x > width * 0.24f) { split = i; gaps++; }
        // Two separated lower contours are a useful sole estimate; other silhouettes keep the broad ellipse.
        if (gaps != 1 || split < 2 || low.Count - split < 2) return default;
        Support first = BuildSupport(low, 0, split), second = BuildSupport(low, split, low.Count);
        if (first.maxX - first.minX < width * 0.06f || second.maxX - second.minX < width * 0.06f) return default;
        return new Footprint { split = true, first = first, second = second };
    }

    private static Support BuildSupport(List<Vector2> points, int start, int end)
    {
        float bottom = float.MaxValue;
        for (int i = start; i < end; i++) bottom = Mathf.Min(bottom, points[i].y);
        return new Support { minX = points[start].x, maxX = points[end-1].x, bottom = bottom };
    }

    private MeshRenderer CreateShadow(string name)
    {
        GameObject item = new GameObject(name);
        item.transform.SetParent(shadowRoot, false);
        item.AddComponent<MeshFilter>().sharedMesh = quad;
        MeshRenderer renderer = item.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = shadowMaterial;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        return renderer;
    }

    private void Collect(Transform root)
    {
        if (root == null)
            return;
        scratchActors.Clear();
        BattleActorVisualUtility.CollectActorRoots(root, scratchActors);
        foreach (Transform actor in scratchActors)
        {
            if (entries.Exists(item => item.actor == actor))
                continue;
            List<SpriteRenderer> sprites = new List<SpriteRenderer>();
            foreach (SpriteRenderer sprite in actor.GetComponentsInChildren<SpriteRenderer>(true))
                if (BattleActorVisualUtility.IsActorBodyRenderer(actor, sprite))
                    sprites.Add(sprite);
            if (sprites.Count == 0)
                continue;
            MeshRenderer renderer = CreateShadow("Contact shadow " + actor.name);
            entries.Add(new Entry { actor = actor, sprites = sprites.ToArray(), shadow = renderer });
        }
    }

    private void OnDisable()
    {
        ReleaseShadows();
    }

    public void ReleaseShadows()
    {
        if (shadowRoot != null)
            Release(shadowRoot.gameObject);
        if (quad != null)
            Release(quad);
        shadowRoot = null;
        quad = null;
        entries.Clear();
        footprints.Clear();
        nextRefresh = 0f;
    }

    private static void Release(Object value)
    {
        if (Application.isPlaying)
            Destroy(value);
        else
            DestroyImmediate(value);
    }
}
