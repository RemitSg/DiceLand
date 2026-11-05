using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>One mesh for the contact flare, blade arcs, hot sparks and cooling debris.</summary>
[ExecuteAlways]
public sealed class BattleContactVfx : MonoBehaviour
{
    private enum Shape { Halo, Streak, Shard, Ribbon, Dust }
    private const int QuadBudget = 660;
    private const float ClashPlaybackSpeed = 1.22f;
    private static Shader contactShader;

    private readonly List<Vector3> vertices = new List<Vector3>();
    private readonly List<Vector2> uv = new List<Vector2>();
    private readonly List<Vector2> shapes = new List<Vector2>();
    private readonly List<Color> colors = new List<Color>();
    private readonly List<int> triangles = new List<int>();
    private Particle[] particles;
    private Mesh mesh;
    private MeshRenderer batchRenderer;
    private Material material;
    private Vector3 axis, normal;
    private Color accent, rim;
    private float size, floor, duration, power;
    private bool clash, compact;
    private uint randomState;

    private struct Particle
    {
        public Vector3 origin, velocity;
        public float delay, width, length, angle, spin;
        public float gravity, groundY, landAt, reboundSpeed, bounceDuration, life;
        public bool raisesDust;
        public Color color;
    }

    public float Elapsed { get; private set; }
    public bool IsComplete => Elapsed >= duration;
    public int ParticleCount => particles != null ? particles.Length : 0;
    public int GroundedParticleCount { get; private set; }
    public bool HasFallingBurst => !IsComplete && GroundedParticleCount < ParticleCount * 0.65f;
    public Bounds VisualBounds => batchRenderer.bounds;
    public bool HasVisibleGeometry => vertices.Count > 0;

    public static float LensThatFits(Camera camera, Bounds bounds)
    {
        float lens = camera.orthographic ? camera.orthographicSize : camera.fieldOfView;
        float expansion = 1;
        for (int corner = 0; corner < 8; corner++)
        {
            Vector3 sign = new Vector3((corner & 1) == 0 ? -1 : 1, (corner & 2) == 0 ? -1 : 1, (corner & 4) == 0 ? -1 : 1);
            Vector3 viewport = camera.WorldToViewportPoint(bounds.center + Vector3.Scale(bounds.extents, sign));
            expansion = Mathf.Max(expansion, Mathf.Max(Mathf.Abs(viewport.x - 0.5f), Mathf.Abs(viewport.y - 0.5f)) / 0.455f);
        }
        if (camera.orthographic)
            return lens * expansion;
        return 2 * Mathf.Atan(Mathf.Tan(lens * Mathf.Deg2Rad * 0.5f) * expansion) * Mathf.Rad2Deg;
    }

    public static bool IsSupported
    {
        get
        {
            if (contactShader == null)
                contactShader = Resources.Load<Shader>("BattleVfx/ContactLight");
            return contactShader != null && contactShader.isSupported;
        }
    }

    public static BattleContactVfx Spawn(Transform owner, Vector3 point, Vector3 direction, Color tint, float power,
        float bodyHeight, float groundY, int sortingLayer, int sortingOrder, bool clash, bool compact = false)
    {
        if (owner == null || !IsSupported)
            return null;

        var root = new GameObject(clash ? "Blade Clash Contact" : "Directional Hit Contact");
        root.layer = owner.gameObject.layer;
        SceneManager.MoveGameObjectToScene(root, owner.gameObject.scene);
        root.transform.position = point;
        root.transform.SetParent(owner, true);
        var effect = root.AddComponent<BattleContactVfx>();
        effect.Initialize(direction, tint, power, bodyHeight, groundY, sortingLayer, sortingOrder, clash, compact);
        return effect;
    }

    private void Initialize(Vector3 direction, Color tint, float intensity, float bodyHeight, float groundY,
        int sortingLayer, int sortingOrder, bool isClash, bool isCompact)
    {
        clash = isClash;
        compact = isCompact;
        power = Mathf.Clamp01(intensity);
        size = Mathf.Clamp(bodyHeight, 1.45f, 4.2f) * Mathf.Lerp(0.73f, 1.04f, power) * (compact ? 0.70f : 1f);
        floor = groundY - transform.position.y;
        direction.z = 0;
        axis = direction.sqrMagnitude > 0.001f ? direction.normalized : Vector3.right;
        normal = new Vector3(-axis.y, axis.x, 0);
        if (normal.y < 0)
            normal = -normal;
        accent = clash ? Color.Lerp(tint, new Color(1, 0.40f, 0.06f), 0.60f) : tint;
        rim = Color.Lerp(accent, new Color(1, 0.94f, 0.76f), clash ? 0.65f : 0.45f);

        mesh = new Mesh { name = "Battle contact batch" };
        mesh.MarkDynamic();
        gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
        material = new Material(contactShader) { name = "Battle contact light (runtime)" };
        batchRenderer = gameObject.AddComponent<MeshRenderer>();
        batchRenderer.sharedMaterial = material;
        batchRenderer.sortingLayerID = sortingLayer;
        batchRenderer.sortingOrder = sortingOrder;
        batchRenderer.shadowCastingMode = ShadowCastingMode.Off;
        batchRenderer.receiveShadows = false;

        randomState = unchecked((uint)(846217 + Mathf.RoundToInt(transform.position.x * 217) + Mathf.RoundToInt(power * 701)));
        if (randomState == 0)
            randomState = 1;
        BuildParticles();
        int capacity = Mathf.Min(QuadBudget, ParticleCount * (clash ? 2 : 1) + 64);
        vertices.Capacity = uv.Capacity = shapes.Capacity = colors.Capacity = capacity * 4;
        triangles.Capacity = capacity * 6;
        Advance(0);
    }

    private void BuildParticles()
    {
        int count = Mathf.RoundToInt(Mathf.Lerp(clash ? 156 : 82, clash ? 286 : 154, power));
        particles = new Particle[compact ? count / 2 : count];
        Vector3 forward = axis.x < 0 ? Vector3.left : Vector3.right;
        float force = Mathf.Lerp(0.75f, 1.13f, power);
        for (int i = 0; i < particles.Length; i++)
        {
            bool large = i % 9 == 0;
            bool late = i % 5 == 0;
            float azimuth = clash ? Range(-Mathf.PI, Mathf.PI) : Range(-1.32f, 1.32f);
            float depthSpread = clash ? 0.40f : 0.48f;
            Vector3 spread = forward * Mathf.Cos(azimuth) + Vector3.forward * (Mathf.Sin(azimuth) * depthSpread);
            float speed = late ? Range(2.2f, 5.0f) : Range(4.4f, 9.0f);
            if (clash)
                speed *= 1.20f;
            float lift = clash ? Range(-0.85f, 5.0f) : Range(0.25f, 4.25f);
            Vector3 velocity = (spread * speed + Vector3.up * lift) * (size * force);
            float width = size * (clash ? Range(0.0065f, 0.018f)
                : large ? Range(0.032f, 0.056f) : Range(0.008f, 0.027f));
            float length = width * Range(1.15f, 1.85f);
            float groundY = floor + (clash ? width * 0.55f : width * 0.9f);
            Vector3 origin = (axis * Range(-0.17f, 0.17f) + normal * Range(-0.18f, 0.19f)) * size;
            origin.y = Mathf.Max(origin.y, groundY + 0.02f);
            float gravity = size * Range(13.5f, 18.2f);
            float landingSpeed = Mathf.Sqrt(velocity.y * velocity.y + 2 * gravity * (origin.y - groundY));
            float landAt = (velocity.y + landingSpeed) / gravity;
            float reboundSpeed = landingSpeed * (clash ? Range(0.18f, 0.34f) : Range(0.15f, 0.27f));
            float bounceDuration = 2 * reboundSpeed / gravity;
            float delay = late ? Range(0.035f, 0.09f) : Range(0, 0.035f);
            float settledLifetime = clash ? Range(0.66f, 1.04f)
                : compact ? Range(0.14f, 0.23f) : Range(0.23f, 0.43f);
            float life = landAt + bounceDuration + settledLifetime;
            Color chunk = Color.Lerp(new Color(0.23f, 0.24f, 0.27f), new Color(0.78f, 0.70f, 0.57f), Range(0.17f, 0.93f));
            particles[i] = new Particle
            {
                origin = origin,
                velocity = velocity,
                delay = delay,
                life = life,
                width = width,
                length = length,
                angle = Range(0, Mathf.PI * 2),
                spin = Range(-16, 16),
                gravity = gravity,
                groundY = groundY,
                landAt = landAt,
                reboundSpeed = reboundSpeed,
                bounceDuration = bounceDuration,
                raisesDust = !clash && !compact && large,
                color = clash ? Color.Lerp(accent, rim, Range(0.12f, 0.93f)) : Color.Lerp(chunk, accent, 0.18f)
            };
            duration = Mathf.Max(duration, delay + life);
        }
    }

    private void Update()
    {
        if (!Application.isPlaying || Time.timeScale <= 0)
            return;
        Advance(Mathf.Min(Time.unscaledDeltaTime, 0.05f) * BattleDeathVfx.VisualTimeScale(Time.timeScale));
    }

    public void Advance(float delta)
    {
        Elapsed = Mathf.Min(duration, Elapsed + Mathf.Max(0, delta) * (clash ? ClashPlaybackSpeed : 1f));
        vertices.Clear();
        uv.Clear();
        shapes.Clear();
        colors.Clear();
        triangles.Clear();
        GroundedParticleCount = IsComplete ? ParticleCount : 0;
        if (!IsComplete)
        {
            DrawContact();
            foreach (Particle particle in particles)
                DrawParticle(particle);
            DrawDust();
        }

        mesh.Clear(false);
        if (vertices.Count > 0)
        {
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uv);
            mesh.SetUVs(1, shapes);
            mesh.SetColors(colors);
            mesh.SetTriangles(triangles, 0, true);
        }
        if (IsComplete && Application.isPlaying)
            Destroy(gameObject);
    }

    private void DrawContact()
    {
        float flash = Fade(0, clash ? 0.13f : 0.10f);
        float reach = EaseOut(Elapsed / 0.10f);
        Vector3 center = clash ? Vector3.zero : axis * size * (0.06f + reach * 0.12f);
        Quad(center, axis * size * (0.44f + reach * 0.29f), normal * size * (0.34f + reach * 0.12f),
            Tint(accent, flash * 0.78f), Shape.Halo);
        Quad(center, axis * size * 0.24f, normal * size * 0.23f, Tint(rim, flash), Shape.Halo);

        if (clash)
        {
            Vector3 diagonal = (axis + normal * 0.65f).normalized;
            Vector3 cross = (axis - normal * 0.94f).normalized;
            Blade(center, diagonal, size * (0.83f + reach * 0.39f), size * 0.082f, Tint(rim, flash));
            Blade(center, cross, size * (0.71f + reach * 0.28f), size * 0.065f, Tint(accent, flash * 0.95f));
            float sweep = Fade(0.016f, 0.22f);
            float radius = size * (0.26f + 0.64f * reach);
            Arc(axis * radius * 0.09f, axis * radius, normal * radius * 0.72f, 0.24f, 2.62f, size * 0.068f, Tint(rim, sweep));
            Arc(-axis * radius * 0.09f, axis * radius, normal * radius * 0.72f, 3.34f, 5.82f, size * 0.054f, Tint(accent, sweep * 0.87f));
        }
        else
        {
            Vector3 slash = (axis + normal * 0.32f).normalized;
            Blade(center + axis * size * 0.25f, slash, size * (0.58f + reach * 0.36f), size * 0.105f, Tint(rim, flash));
            Blade(center, (axis - normal * 0.56f).normalized, size * 0.45f, size * 0.054f, Tint(accent, flash));
            float sweep = Fade(0.018f, compact ? 0.13f : 0.20f);
            float radius = size * (0.23f + 0.42f * reach);
            Arc(center + axis * size * reach * 0.28f, axis * radius * 0.60f, normal * radius,
                -1.08f, 1.28f, size * 0.105f, Tint(accent, sweep));
            if (!compact)
                Arc(center + axis * size * reach * 0.65f, axis * radius * 0.43f, normal * radius * 0.77f,
                    -0.91f, 0.96f, size * 0.057f, Tint(rim, Fade(0.045f, 0.20f) * 0.74f));
        }
    }

    private void DrawParticle(Particle particle)
    {
        float time = Elapsed - particle.delay;
        if (time >= particle.landAt)
            GroundedParticleCount++;
        if (time < 0 || time >= particle.life)
            return;

        float settleAt = particle.landAt + particle.bounceDuration;
        float fade = 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(settleAt, particle.life, time));
        Vector3 point = ParticlePosition(particle, time);
        if (clash)
        {
            DrawSpark(particle, point, time, settleAt, fade);
            return;
        }

        float spinTime = Mathf.Min(time, settleAt);
        float angle = particle.angle + particle.spin * spinTime;
        Vector3 direction = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0);
        float tumble = 0.40f + 0.60f * Mathf.Abs(Mathf.Cos(angle * 1.63f));
        Vector3 edge = direction * particle.length;
        Vector3 face = new Vector3(-direction.y, direction.x, 0) * (particle.width * tumble);
        float nearFloor = 1 - Mathf.Clamp01((point.y - particle.groundY) / (particle.width * 2.5f));
        // The visible shard occupies the cut interior of its quad. Seat that edge against the floor.
        float support = (Mathf.Abs(edge.y) + Mathf.Abs(face.y)) * 0.80f + 0.006f;
        point.y += (floor + support - particle.groundY) * nearFloor;
        if (particle.raisesDust && nearFloor > 0)
        {
            Vector3 shadow = new Vector3(point.x, floor + 0.012f, point.z);
            Quad(shadow, Vector3.right * particle.length * 1.12f, Vector3.forward * particle.width * 0.95f,
                Tint(new Color(0.08f, 0.065f, 0.045f), fade * nearFloor * 0.55f), Shape.Dust);
        }
        Quad(point, edge, face, Tint(particle.color, fade), Shape.Shard);

        float landingAge = time - particle.landAt;
        if (particle.raisesDust && landingAge >= 0 && landingAge < 0.23f)
        {
            float spread = EaseOut(landingAge / 0.23f);
            Vector3 landing = ParticlePosition(particle, particle.landAt);
            landing.y = floor + particle.width * (0.28f + spread * 0.72f);
            Quad(landing, Vector3.right * particle.length * (1.5f + 2.3f * spread),
                Vector3.up * particle.width * (0.65f + spread), Tint(new Color(0.38f, 0.31f, 0.24f), (1 - spread) * 0.55f), Shape.Dust);
        }
    }

    private void DrawSpark(Particle particle, Vector3 point, float time, float settleAt, float fade)
    {
        float cooling = Mathf.Clamp01(time / Mathf.Max(0.1f, settleAt));
        Color hot = Color.Lerp(particle.color, new Color(1, 0.23f, 0.028f), cooling * 0.86f);
        float heat = fade * Mathf.Lerp(1.22f, 0.62f, cooling);
        if (time < settleAt)
        {
            Vector3 tail = ParticlePosition(particle, Mathf.Max(0, time - 0.046f));
            Vector3 span = (point - tail) * 0.5f;
            if (span.sqrMagnitude > 0.000004f)
            {
                Vector3 across = Vector3.Cross(Vector3.forward, span).normalized;
                Quad(tail + span, span, across * particle.width * 0.80f, Tint(hot, heat * 0.9f), Shape.Streak);
            }
        }
        float glow = particle.width * Mathf.Lerp(1.55f, 1.06f, cooling);
        Quad(point, Vector3.right * glow, Vector3.up * glow, Tint(hot, heat), Shape.Halo);
    }

    private static Vector3 ParticlePosition(Particle particle, float time)
    {
        // Solve the first impact at construction so every spark reaches the floor at any frame rate.
        float bounceTime = Mathf.Clamp(time - particle.landAt, 0, particle.bounceDuration);
        float travelTime = Mathf.Min(time, particle.landAt) + bounceTime * 0.34f;
        float travel = (1 - Mathf.Exp(-travelTime * 4.45f)) / 4.45f;
        Vector3 point = particle.origin + particle.velocity * travel;
        point.y = time < particle.landAt
            ? particle.origin.y + particle.velocity.y * time - 0.5f * particle.gravity * time * time
            : particle.groundY + particle.reboundSpeed * bounceTime - 0.5f * particle.gravity * bounceTime * bounceTime;
        return point;
    }

    private void DrawDust()
    {
        if (compact)
            return;
        float fade = Fade(0.032f, 0.37f) * Mathf.Clamp01((Elapsed - 0.032f) * 22);
        float travel = EaseOut(Elapsed / 0.37f);
        Vector3 center = axis * size * (clash ? 0.15f : 0.50f) * travel;
        center.y = floor + size * (0.09f + 0.12f * travel);
        Color dust = Color.Lerp(new Color(0.29f, 0.23f, 0.16f), accent, 0.22f);
        Quad(center, Vector3.right * size * (0.34f + 0.42f * travel), Vector3.up * size * (0.10f + 0.14f * travel),
            Tint(dust, fade * 0.32f * power), Shape.Dust);
    }

    private void Blade(Vector3 center, Vector3 direction, float length, float width, Color color)
    {
        Vector3 across = new Vector3(-direction.y, direction.x, 0);
        Quad(center, direction * length, across * width * 2.2f, Tint(color, color.a * 0.30f), Shape.Halo);
        Quad(center, direction * length, across * width, color, Shape.Streak);
    }

    private void Arc(Vector3 center, Vector3 horizontal, Vector3 vertical, float from, float to, float width, Color color)
    {
        if (color.a <= 0.001f)
            return;
        const int segments = 14;
        Vector3 inner = Vector3.zero, outer = Vector3.zero;
        for (int i = 0; i <= segments; i++)
        {
            float fraction = (float)i / segments;
            float angle = Mathf.Lerp(from, to, fraction);
            Vector3 radial = horizontal * Mathf.Cos(angle) + vertical * Mathf.Sin(angle);
            Vector3 band = radial.normalized * width * (0.28f + 0.72f * Mathf.Sin(fraction * Mathf.PI));
            Vector3 nextInner = center + radial - band;
            Vector3 nextOuter = center + radial + band;
            if (i > 0)
                AddQuad(inner, nextInner, nextOuter, outer, (float)(i - 1) / segments, fraction, color, Shape.Ribbon);
            inner = nextInner;
            outer = nextOuter;
        }
    }

    private void Quad(Vector3 center, Vector3 right, Vector3 up, Color color, Shape shape)
    {
        AddQuad(center - right - up, center + right - up, center + right + up, center - right + up, 0, 1, color, shape);
    }

    private void AddQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, float start, float end, Color color, Shape shape)
    {
        if (color.a <= 0.001f || vertices.Count >= QuadBudget * 4)
            return;
        int index = vertices.Count;
        vertices.Add(a);
        vertices.Add(b);
        vertices.Add(c);
        vertices.Add(d);
        uv.Add(new Vector2(start, 0));
        uv.Add(new Vector2(end, 0));
        uv.Add(new Vector2(end, 1));
        uv.Add(new Vector2(start, 1));
        for (int i = 0; i < 4; i++)
        {
            colors.Add(color);
            shapes.Add(new Vector2((float)shape, 0));
        }
        triangles.Add(index);
        triangles.Add(index + 1);
        triangles.Add(index + 2);
        triangles.Add(index);
        triangles.Add(index + 2);
        triangles.Add(index + 3);
    }

    private float Fade(float start, float end)
    {
        if (Elapsed < start || Elapsed >= end)
            return 0;
        float remaining = 1 - Mathf.InverseLerp(start, end, Elapsed);
        return remaining * remaining;
    }

    private static float EaseOut(float value)
    {
        float remaining = 1 - Mathf.Clamp01(value);
        return 1 - remaining * remaining * remaining;
    }

    private static Color Tint(Color color, float alpha)
    {
        color.a = alpha;
        return color;
    }

    private float Range(float minimum, float maximum)
    {
        randomState ^= randomState << 13;
        randomState ^= randomState >> 17;
        randomState ^= randomState << 5;
        return Mathf.Lerp(minimum, maximum, (randomState & 0xFFFFFF) / 16777215f);
    }

    private void OnDestroy()
    {
        if (Application.isPlaying)
        {
            Destroy(mesh);
            Destroy(material);
        }
        else
        {
            DestroyImmediate(mesh);
            DestroyImmediate(material);
        }
    }
}
