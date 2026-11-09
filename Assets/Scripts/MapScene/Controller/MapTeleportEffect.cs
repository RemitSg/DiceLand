using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>A deterministic batch of portal rings, spiralling motes and arrival sparks. Also owns the pawn's temporary pose.</summary>
[ExecuteAlways]
public sealed class MapTeleportEffect : MonoBehaviour
{
    public const float RelocationTime = .58f;
    public const float Duration = 1.8f;
    private static readonly Color Jade = new Color(.19f, .93f, .72f);
    private static readonly Color Gold = new Color(1f, .81f, .38f);
    private readonly List<Vector3> vertices = new List<Vector3>(4096);
    private readonly List<Vector2> uv = new List<Vector2>(4096);
    private readonly List<Vector2> shapes = new List<Vector2>(4096);
    private readonly List<Color> colors = new List<Color>(4096);
    private readonly List<int> triangles = new List<int>(6144);
    private Transform pawn, visual;
    private MapViewController view;
    private Vector3 visualPosition, visualScale;
    private Quaternion visualRotation;
    private SpriteRenderer[] sprites;
    private Color[] spriteColors;
    private Mesh mesh;
    private Material material;
    private Mote[] motes;
    private float size;
    private bool initialized;

    private struct Mote { public float angle, radius, height, speed, delay, width; public bool gold; }
    public event Action Finished;
    public event Action Completed;
    public Vector3 Origin { get; private set; }
    public Vector3 Destination { get; private set; }
    public float Elapsed { get; private set; }
    public bool HasRelocated { get; private set; }
    public bool IsComplete { get; private set; }
    public bool HasVisibleGeometry => vertices.Count > 0;
    public int ParticleCount => motes != null ? motes.Length * 2 : 0;

    public static MapTeleportEffect Spawn(Transform owner, Transform pawn, Transform visual, Vector3 destination, float tileSize, MapViewController view)
    {
        var root = new GameObject("Map Calamity Teleport");
        root.layer = pawn.gameObject.layer;
        SceneManager.MoveGameObjectToScene(root, pawn.gameObject.scene);
        root.transform.SetParent(owner, false);
        root.transform.position = Vector3.zero;
        var effect = root.AddComponent<MapTeleportEffect>();
        effect.Initialize(pawn, visual, destination, tileSize, view);
        return effect;
    }

    private void Initialize(Transform pawnRoot, Transform visualRoot, Vector3 destination, float tileSize, MapViewController mapView)
    {
        pawn = pawnRoot; visual = visualRoot; view = mapView;
        Origin = pawn.position; Destination = destination;
        size = Mathf.Max(.2f, tileSize) * .72f;
        if (visual != null)
        {
            visualPosition = visual.localPosition; visualRotation = visual.localRotation; visualScale = visual.localScale;
            sprites = visual.GetComponentsInChildren<SpriteRenderer>(true);
            spriteColors = new Color[sprites.Length];
            for (int i = 0; i < sprites.Length; i++) spriteColors[i] = sprites[i].color;
        }
        mesh = new Mesh { name = "Map teleport rings and particles" }; mesh.MarkDynamic();
        gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = gameObject.AddComponent<MeshRenderer>();
        Shader shader = Resources.Load<Shader>("BattleVfx/ContactLight");
        if (shader != null)
        {
            material = new Material(shader) { name = "Map teleport light (runtime)" };
            renderer.sharedMaterial = material;
        }
        else renderer.enabled = false;
        renderer.sortingOrder = 110;
        if (sprites != null && sprites.Length > 0) renderer.sortingLayerID = sprites[0].sortingLayerID;
        renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
        // Local RNG leaves encounter generation and subsequent calamity rolls untouched.
        uint rng = 45173;
        motes = new Mote[72];
        for (int i = 0; i < motes.Length; i++)
        {
            motes[i] = new Mote { angle = Range(ref rng, 0, Mathf.PI * 2), radius = Range(ref rng, .35f, .95f),
                height = Range(ref rng, .1f, .9f), speed = Range(ref rng, 4f, 9f), delay = Range(ref rng, 0, .12f),
                width = Range(ref rng, .015f, .045f), gold = i % 4 == 0 };
        }
        initialized = true;
        Advance(0);
    }

    private void Update()
    {
        if (Application.isPlaying) Advance(Time.unscaledDeltaTime);
    }

    public void Advance(float deltaTime)
    {
        if (!initialized || IsComplete) return;
        if (pawn == null) { Finish(false); return; }
        if (!float.IsNaN(deltaTime) && !float.IsInfinity(deltaTime)) Elapsed += Mathf.Max(0, deltaTime);
        if (!HasRelocated && Elapsed >= RelocationTime)
        {
            HasRelocated = true;
            pawn.position = Destination;
            view?.FocusOnPosition(Destination);
        }
        ApplyPawnPose();
        vertices.Clear(); uv.Clear(); shapes.Clear(); colors.Clear(); triangles.Clear();
        if (Elapsed < Duration)
        {
            DrawPortal(Origin, Elapsed / RelocationTime, false);
            DrawPortal(Destination, (Elapsed - RelocationTime) / (Duration - RelocationTime), true);
        }
        mesh.Clear(false);
        if (vertices.Count > 0)
        {
            mesh.SetVertices(vertices); mesh.SetUVs(0, uv); mesh.SetUVs(1, shapes); mesh.SetColors(colors); mesh.SetTriangles(triangles, 0, true);
        }
        if (Elapsed >= Duration)
        {
            Finish(true);
            if (Application.isPlaying) Destroy(gameObject);
        }
    }

    private void ApplyPawnPose()
    {
        if (visual == null) return;
        float alpha, scale;
        if (!HasRelocated)
        {
            float t = Mathf.InverseLerp(.12f, RelocationTime, Elapsed);
            alpha = 1 - Mathf.SmoothStep(0, 1, t);
            scale = Mathf.Lerp(1, .06f, t * t);
            visual.localPosition = visualPosition + Vector3.up * (t * .28f);
            visual.localRotation = visualRotation * Quaternion.Euler(0, 0, Mathf.Sin(t * Mathf.PI) * 7);
            visual.localScale = Vector3.Scale(visualScale, new Vector3(scale, Mathf.Lerp(1, 1.45f, t) * scale, 1));
        }
        else
        {
            float t = Mathf.Clamp01((Elapsed - RelocationTime) / .52f);
            alpha = Mathf.SmoothStep(0, 1, t * 1.6f);
            float u = t - 1;
            scale = Mathf.LerpUnclamped(.12f, 1, 1 + 2.70158f * u * u * u + 1.70158f * u * u);
            visual.localScale = visualScale * scale;
            visual.localPosition = visualPosition + Vector3.up * (Mathf.Sin(t * Mathf.PI) * .14f);
            visual.localRotation = visualRotation;
        }
        for (int i = 0; i < sprites.Length; i++)
            if (sprites[i] != null) { Color c = spriteColors[i]; c.a *= alpha; sprites[i].color = c; }
    }

    private void DrawPortal(Vector3 position, float t, bool arrival)
    {
        if (t < 0 || t >= 1) return;
        position.z = -.35f;
        Vector3 foot = position + Vector3.down * (size * .35f);
        float fade = arrival ? Mathf.Pow(1 - t, 1.15f) : Mathf.Sin(t * Mathf.PI);
        float radius = size * (arrival ? .3f + Mathf.Sqrt(t) * .9f : Mathf.Lerp(.85f, .08f, t * t));
        Quad(foot, Vector3.right * radius, Vector3.up * radius * .4f, Tint(Jade, fade * .6f), 0);
        Ring(foot, radius, .4f, size * .025f, Tint(Jade, fade * .85f), t * 3);
        Ring(foot, radius * .78f, .4f, size * .014f, Tint(Gold, fade * .75f), -t * 2);
        float pulse = arrival ? Mathf.Pow(1 - Mathf.Clamp01(t * 3), 2) : Mathf.Pow(t, 3);
        Quad(position + Vector3.up * .15f, Vector3.right * size * .47f, Vector3.up * size * .85f, Tint(Jade, pulse * .8f), 0);
        for (int i = 0; i < 12; i++)
        {
            float a = i * Mathf.PI / 6 + t * (arrival ? -2 : 3);
            Vector3 direction = new Vector3(Mathf.Cos(a), Mathf.Sin(a) * .4f, 0);
            Vector3 center = foot + direction * radius;
            Quad(center, direction.normalized * size * .055f, new Vector3(-direction.y, direction.x, 0).normalized * size * .015f, Tint(Gold, fade), 1);
        }
        foreach (Mote mote in motes)
        {
            float p = arrival ? t - mote.delay : Mathf.Clamp01((t - mote.delay) / (1 - mote.delay));
            if (p < 0 || p > 1) continue;
            float angle = mote.angle + (arrival ? -1 : 1) * p * mote.speed;
            float spread = size * mote.radius * (arrival ? .12f + p * 1.15f : 1 - p);
            Vector3 radial = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle) * .55f, 0);
            Vector3 center = foot + radial * spread + Vector3.up * (size * mote.height * Mathf.Sin(p * Mathf.PI));
            float light = Mathf.Sin(p * Mathf.PI) * fade;
            Color tint = Tint(mote.gold ? Gold : Jade, light * .9f);
            Vector3 tangent = new Vector3(-Mathf.Sin(angle), Mathf.Cos(angle), 0);
            Quad(center, tangent * size * mote.width * 2.5f, radial.normalized * size * mote.width * .55f, tint, 1);
            Quad(center, Vector3.right * size * mote.width * 1.7f, Vector3.up * size * mote.width * 1.7f, tint, 0);
        }
    }

    private void Ring(Vector3 center, float radius, float squash, float width, Color tint, float rotation)
    {
        for (int i = 0; i < 56; i++)
        {
            float a = i * Mathf.PI * 2 / 56 + rotation, b = (i + 1) * Mathf.PI * 2 / 56 + rotation;
            Vector3 d1 = new Vector3(Mathf.Cos(a), Mathf.Sin(a) * squash, 0), d2 = new Vector3(Mathf.Cos(b), Mathf.Sin(b) * squash, 0);
            AddQuad(center + d1 * (radius - width), center + d2 * (radius - width), center + d2 * (radius + width), center + d1 * (radius + width), tint, 3);
        }
    }

    private void Quad(Vector3 center, Vector3 right, Vector3 up, Color tint, float shape)
        => AddQuad(center - right - up, center + right - up, center + right + up, center - right + up, tint, shape);

    private void AddQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color tint, float shape)
    {
        if (tint.a < .001f) return;
        int start = vertices.Count;
        vertices.Add(transform.InverseTransformPoint(a)); vertices.Add(transform.InverseTransformPoint(b));
        vertices.Add(transform.InverseTransformPoint(c)); vertices.Add(transform.InverseTransformPoint(d));
        uv.Add(Vector2.zero); uv.Add(Vector2.right); uv.Add(Vector2.one); uv.Add(Vector2.up);
        for (int i = 0; i < 4; i++) { colors.Add(tint); shapes.Add(new Vector2(shape, 0)); }
        triangles.Add(start); triangles.Add(start + 1); triangles.Add(start + 2); triangles.Add(start); triangles.Add(start + 2); triangles.Add(start + 3);
    }

    public void CompleteImmediately() => Finish(false);

    private void Finish(bool notify)
    {
        if (!initialized || IsComplete) return;
        IsComplete = true;
        HasRelocated = true;
        if (pawn != null) pawn.position = Destination;
        if (visual != null) { visual.localPosition = visualPosition; visual.localRotation = visualRotation; visual.localScale = visualScale; }
        if (sprites != null)
            for (int i = 0; i < sprites.Length; i++) if (sprites[i] != null) sprites[i].color = spriteColors[i];
        vertices.Clear();
        if (mesh != null) mesh.Clear(false);
        Action finished = Finished, completed = Completed;
        Finished = Completed = null;
        finished?.Invoke();
        if (notify) completed?.Invoke();
    }

    private void OnDisable()
    {
        bool interrupted = initialized && !IsComplete;
        Finish(false);
        if (interrupted && Application.isPlaying) Destroy(gameObject);
    }
    private void OnDestroy()
    {
        Finish(false);
        if (Application.isPlaying) { Destroy(mesh); Destroy(material); }
        else { DestroyImmediate(mesh); DestroyImmediate(material); }
    }

    private static Color Tint(Color color, float alpha) { color.a = Mathf.Clamp01(alpha); return color; }
    private static float Range(ref uint rng, float min, float max)
    {
        rng ^= rng << 13; rng ^= rng >> 17; rng ^= rng << 5;
        return Mathf.Lerp(min, max, (rng & 0xffffff) / 16777215f);
    }
}
