using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>A disposable visual snapshot: gameplay death and source hiding remain owned by BattleFeedbackSystem.</summary>
[ExecuteAlways]
public sealed class BattleDeathVfx : MonoBehaviour
{
    public const float Lifetime = 2.25f;
    public const float MinimumVisualTimeScale = 0.6f;
    public float Elapsed { get; private set; }
    public bool IsComplete => Elapsed >= Lifetime;
    public int FragmentCount => particles.Count;
    public Vector3 TravelDirection => push;
    public Vector3 RecoilOffset => Recoil(Elapsed);
    private readonly List<Material> ownedMaterials = new List<Material>();
    private readonly List<Material> silhouettes = new List<Material>();
    private readonly List<Particle> particles = new List<Particle>();
    private Mesh mesh;
    private Vector3[] vertices;
    private Vector2[] uv, kinds;
    private Color[] colors;
    private int[] indices;
    private int quads;
    private float height, width, floor;
    private Color primary, gold;
    private Vector3 push, spread;
    private Transform silhouetteRoot;
    private uint randomState = 145981;
    private const int QuadBudget = 1024;

    private struct Particle
    {
        public Vector3 origin, velocity;
        public float delay, life, size, angle, spin, phase;
        public int kind;
        public Color color;
    }

    public static bool TryPlay(Transform actor, Vector3 direction, bool enemy, int order = 420, int detail = 36)
    {
        if (actor == null) return false;
        Shader dissolve = Resources.Load<Shader>("BattleVfx/DeathDissolve");
        Shader light = Resources.Load<Shader>("BattleVfx/DeathLight");
        if (dissolve == null || light == null || !dissolve.isSupported || !light.isSupported) return false;
        GameObject root = new GameObject(enemy ? "Enemy Soul Dissolution" : "Ally Soul Dissolution");
        root.layer = actor.gameObject.layer;
        SceneManager.MoveGameObjectToScene(root, actor.gameObject.scene);
        BattleDeathVfx effect = root.AddComponent<BattleDeathVfx>();
        effect.Initialize(actor, direction, enemy, order, detail, dissolve, light);
        return true;
    }

    private void Initialize(Transform actor, Vector3 direction, bool enemy, int order, int detail, Shader dissolve, Shader light)
    {
        Bounds bounds = new Bounds(actor.position + Vector3.up, new Vector3(1.5f, 2.5f, 0.2f));
        bool found = false;
        foreach (SpriteRenderer source in actor.GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (!BattleActorVisualUtility.IsActorBodyRenderer(actor, source) || !source.enabled || !source.gameObject.activeInHierarchy || source.sprite == null) continue;
            if (!found) { bounds = source.bounds; found = true; } else bounds.Encapsulate(source.bounds);
        }
        transform.position = bounds.center;
        height = Mathf.Clamp(bounds.size.y, 0.8f, 8f);
        width = Mathf.Clamp(bounds.size.x, height * 0.35f, height * 1.5f);
        floor = bounds.min.y - bounds.center.y + 0.04f;
        direction.z = 0;
        push = direction.sqrMagnitude > 0.001f ? direction.normalized : Vector3.up;
        spread = new Vector3(-push.y, push.x, 0);
        silhouetteRoot = new GameObject("Recoil silhouette pivot").transform;
        silhouetteRoot.SetParent(transform, false); silhouetteRoot.localPosition = Vector3.up * floor;
        primary = enemy ? new Color(0.68f, 0.3f, 1f) : new Color(0.2f, 0.88f, 1f);
        gold = enemy ? new Color(1f, 0.64f, 0.22f) : new Color(1f, 0.87f, 0.48f);
        foreach (SpriteRenderer source in actor.GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (!BattleActorVisualUtility.IsActorBodyRenderer(actor, source) || !source.enabled || !source.gameObject.activeInHierarchy || source.sprite == null) continue;
            GameObject copy = new GameObject("Dissolving silhouette"); copy.layer = source.gameObject.layer;
            copy.transform.SetParent(silhouetteRoot, false);
            copy.transform.SetPositionAndRotation(source.transform.position, source.transform.rotation);
            copy.transform.localScale = source.transform.lossyScale;
            SpriteRenderer snapshot = copy.AddComponent<SpriteRenderer>();
            snapshot.sprite = source.sprite; snapshot.color = source.color;
            snapshot.flipX = source.flipX; snapshot.flipY = source.flipY;
            snapshot.drawMode = source.drawMode; snapshot.size = source.size;
            snapshot.sortingLayerID = source.sortingLayerID; snapshot.sortingOrder = Mathf.Max(order, source.sortingOrder);
            Material material = Own(new Material(dissolve) { name = "Death silhouette (runtime)" });
            material.SetColor("_SoulColor", primary); material.SetColor("_EdgeColor", gold);
            material.SetVector("_BodyBounds", new Vector4(-0.04f, height, -width * 0.5f, width));
            Texture2D alpha = source.sprite.associatedAlphaSplitTexture;
            if (alpha != null) { material.SetTexture("_AlphaTex", alpha); material.SetFloat("_UseExternalAlpha", 1); }
            snapshot.sharedMaterial = material; silhouettes.Add(material);
        }
        mesh = new Mesh { name = "Death light and fragment batch" }; mesh.MarkDynamic();
        vertices = new Vector3[QuadBudget * 4]; uv = new Vector2[vertices.Length]; kinds = new Vector2[vertices.Length]; colors = new Color[vertices.Length];
        indices = new int[QuadBudget * 6];
        for (int i = 0; i < QuadBudget; i++)
        {
            int v = i * 4, t = i * 6;
            uv[v] = new Vector2(0, 0); uv[v + 1] = new Vector2(1, 0); uv[v + 2] = new Vector2(1, 1); uv[v + 3] = new Vector2(0, 1);
            indices[t] = v; indices[t + 1] = v + 1; indices[t + 2] = v + 2;
            indices[t + 3] = v; indices[t + 4] = v + 2; indices[t + 5] = v + 3;
        }
        gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
        MeshRenderer renderer = gameObject.AddComponent<MeshRenderer>(); renderer.sharedMaterial = Own(new Material(light) { name = "Death light (runtime)" });
        renderer.sortingOrder = order + 2; renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
        SpriteRenderer body = actor.GetComponentInChildren<SpriteRenderer>(); if (body != null) renderer.sortingLayerID = body.sortingLayerID;
        int count = Mathf.Clamp(detail * 3, 72, 180);
        for (int i = 0; i < count; i++)
        {
            float x = Rand(-1, 1), y = Rand(-0.47f, 0.48f), angle = Rand(0, Mathf.PI * 2);
            bool shard = i % 3 == 0, star = i % 11 == 0;
            bool impact = i % 4 == 0;
            float delay = impact ? Rand(0.015f, 0.075f) : Rand(0.09f, 0.30f);
            Vector3 origin = new Vector3(x * width * 0.42f, y * height, Rand(-0.08f, 0.08f));
            if (impact) origin = -push * width * 0.25f + spread * y * height * 0.6f;
            Vector3 velocity = (push * Rand(2.8f, 5.2f) + spread * Rand(-0.65f, 0.65f) + Vector3.up * Rand(0.12f, 0.45f)) * height;
            particles.Add(new Particle {
                origin = RecoilMatrix(delay).MultiplyPoint3x4(origin), velocity = velocity,
                delay = delay, life = Rand(0.75f, 1.65f),
                size = height * (star ? Rand(0.08f, 0.14f) : shard ? Rand(0.026f, 0.062f) : Rand(0.012f, 0.038f)),
                angle = shard ? Mathf.Atan2(velocity.y, velocity.x) - Mathf.PI * 0.5f : angle,
                spin = Rand(-3, 3), phase = Rand(0, 6), kind = star ? 1 : shard ? 2 : 0,
                color = i % 3 == 0 ? gold : Color.Lerp(primary, Color.white, Rand(0.05f, 0.35f))
            });
        }
        Advance(0);
    }

    private Material Own(Material material) { ownedMaterials.Add(material); return material; }
    private float Rand(float min, float max) { randomState ^= randomState << 13; randomState ^= randomState >> 17; randomState ^= randomState << 5; return Mathf.Lerp(min, max, (randomState & 0xFFFFFF) / 16777215f); }
    public static float VisualTimeScale(float battleTimeScale) => battleTimeScale > 0 ? Mathf.Max(MinimumVisualTimeScale, battleTimeScale) : 0;
    private void Update() { if (Application.isPlaying) Advance(Mathf.Min(Time.unscaledDeltaTime, 0.05f) * VisualTimeScale(Time.timeScale)); }

    /// <summary>The same simulation is used by runtime, deterministic render capture and lifecycle checks.</summary>
    public void Advance(float delta)
    {
        Elapsed = Mathf.Min(Lifetime, Elapsed + Mathf.Max(0, delta));
        silhouetteRoot.localPosition = Vector3.up * floor + Recoil(Elapsed);
        silhouetteRoot.localRotation = RecoilRotation(Elapsed);
        silhouetteRoot.localScale = RecoilScale(Elapsed);
        foreach (Material silhouette in silhouettes)
        {
            silhouette.SetFloat("_Age", Elapsed);
            silhouette.SetMatrix("_DissolveWorldToLocal", silhouetteRoot.worldToLocalMatrix);
        }
        quads = 0;
        float flash = Mathf.Sin(Mathf.Clamp01(Elapsed / 0.18f) * Mathf.PI);
        Quad(Vector3.zero, push * height * (0.6f + Elapsed), spread * height * 0.32f, WithAlpha(primary, flash * 0.3f), 0);
        Segment(-spread * height * 0.32f - push * height * 0.08f, spread * height * 0.32f + push * height * 0.08f,
            height * 0.018f * flash, WithAlpha(gold, flash * 0.75f));
        Ring(0.015f, 0.30f, 0.62f, gold, false);
        Ring(0.05f, 0.42f, 0.5f, primary, true);
        foreach (Particle p in particles)
        {
            float t = Elapsed - p.delay, u = t / p.life;
            if (t < 0 || u >= 1) continue;
            Vector3 point = Position(p, t);
            float fade = Mathf.SmoothStep(0, 1, Mathf.Clamp01(t * 14)) * Mathf.Pow(1 - u, 1.25f);
            float scale = p.size * (0.55f + Mathf.Sin(u * Mathf.PI) * 0.45f);
            float a = p.angle + p.spin * t;
            Vector3 right = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0) * scale;
            Vector3 up = new Vector3(-Mathf.Sin(a), Mathf.Cos(a), 0) * scale * (p.kind == 2 ? 1.9f : 1);
            if (p.kind == 2)
            {
                Vector3 tail = Position(p, Mathf.Max(0, t - 0.09f));
                Segment(tail, point, height * 0.009f * fade, WithAlpha(p.color, fade * 0.6f));
            }
            Quad(point, right, up, WithAlpha(p.color, fade), p.kind);
        }
        float ribbonFade = Mathf.SmoothStep(0, 1, Mathf.Clamp01((Elapsed - 0.04f) * 13)) * Mathf.Clamp01((0.95f - Elapsed) / 0.5f);
        if (ribbonFade > 0)
        {
            for (int strand = 0; strand < 3; strand++)
            {
                Vector3 previous = Wake(0, strand);
                for (int step = 1; step <= 22; step++)
                {
                    float u = step / 22f; Vector3 next = Wake(u, strand);
                    Segment(previous, next, height * 0.012f * Mathf.Sin(u * Mathf.PI), WithAlpha(strand == 1 ? gold : primary, ribbonFade * Mathf.Sin(u * Mathf.PI) * 0.7f));
                    previous = next;
                }
            }
        }
        for (int i = quads * 4; i < vertices.Length; i++) { vertices[i] = Vector3.zero; colors[i] = Color.clear; }
        mesh.vertices = vertices; mesh.uv = uv; mesh.uv2 = kinds; mesh.colors = colors;
        mesh.SetTriangles(indices, 0, quads * 6, 0, false);
        mesh.bounds = new Bounds(push * height * 0.85f + Vector3.up * height * 0.4f, Vector3.one * height * 8);
        if (IsComplete && Application.isPlaying) Destroy(gameObject);
    }

    private Vector3 Position(Particle p, float time) => p.origin + p.velocity * ((1 - Mathf.Exp(-time * 3.8f)) / 3.8f)
        + Vector3.up * height * Mathf.Pow(Mathf.Max(0, time - 0.25f), 2) * 0.22f
        + spread * (Mathf.Sin(time * 4 + p.phase) - Mathf.Sin(p.phase)) * height * 0.025f;

    private Vector3 Recoil(float time) => push * height * 0.62f * (1 - Mathf.Exp(-time * 8))
        + Vector3.up * height * 0.08f * Mathf.Sin(Mathf.Clamp01(time / 0.65f) * Mathf.PI);

    private Quaternion RecoilRotation(float time) => Quaternion.Euler(0, 0, -push.x * 18 * (1 - Mathf.Exp(-time * 12)));
    private Vector3 RecoilScale(float time)
    {
        float compression = Mathf.Sin(Mathf.Clamp01(time / 0.13f) * Mathf.PI);
        return new Vector3(1 - 0.09f * compression, 1 + 0.05f * compression, 1);
    }

    private Matrix4x4 RecoilMatrix(float time) => Matrix4x4.TRS(Vector3.up * floor + Recoil(time), RecoilRotation(time), RecoilScale(time)) * Matrix4x4.Translate(Vector3.down * floor);

    private Vector3 Wake(float u, int strand)
    {
        float length = height * (0.1f + (1 - Mathf.Exp(-Elapsed * 5)) * 1.65f);
        float fan = Mathf.Sin(u * Mathf.PI) * (strand - 1) * height * 0.12f;
        float ripple = Mathf.Sin(u * 8 - Elapsed * 9 + strand) * height * 0.025f * Mathf.Sin(u * Mathf.PI);
        return push * (u * length) + spread * (fan + ripple) + Vector3.up * (u * Elapsed * height * 0.13f);
    }

    private void Ring(float delay, float life, float size, Color color, bool ground)
    {
        float u = (Elapsed - delay) / life; if (u < 0 || u >= 1) return;
        float radius = height * size * (0.2f + Mathf.Sqrt(u) * 0.8f);
        Quad(ground ? new Vector3(0, floor, 0) : push * height * u * 0.35f,
            ground ? Vector3.right * radius : push * radius * 0.32f,
            ground ? Vector3.forward * radius : spread * radius,
            WithAlpha(color, Mathf.Sin(Mathf.Pow(u, 0.5f) * Mathf.PI) * (ground ? 0.65f : 0.45f)), 3);
    }

    private void Segment(Vector3 start, Vector3 end, float thickness, Color color)
    {
        Vector3 along = (end - start) * 0.5f;
        Vector3 across = new Vector3(-along.y, along.x, 0).normalized * thickness;
        Quad((start + end) * 0.5f, along, across, color, 4);
    }

    private void Quad(Vector3 center, Vector3 right, Vector3 up, Color color, int kind)
    {
        if (quads >= QuadBudget || color.a <= 0.0001f) return;
        int v = quads++ * 4;
        vertices[v] = center - right - up; vertices[v + 1] = center + right - up; vertices[v + 2] = center + right + up; vertices[v + 3] = center - right + up;
        for (int i = 0; i < 4; i++) { colors[v + i] = color; kinds[v + i] = new Vector2(kind, 0); }
    }

    private static Color WithAlpha(Color color, float alpha) { color.a = alpha; return color; }
    private void OnDestroy()
    {
        foreach (Material material in ownedMaterials) Release(material);
        Release(mesh);
    }
    private static void Release(Object value) { if (value == null) return; if (Application.isPlaying) Destroy(value); else DestroyImmediate(value); }
}
