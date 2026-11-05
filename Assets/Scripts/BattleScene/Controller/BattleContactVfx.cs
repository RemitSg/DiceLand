using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>One mesh for the contact flare, blade arcs, hot sparks and cooling debris.</summary>
[ExecuteAlways]
public sealed class BattleContactVfx : MonoBehaviour
{
    private enum Shape { Halo, Streak, Shard, Ribbon, Dust }
    private const int QuadBudget = 240;
    private static Shader contactShader;

    private readonly List<Vector3> vertices = new List<Vector3>(QuadBudget * 4);
    private readonly List<Vector2> uv = new List<Vector2>(QuadBudget * 4);
    private readonly List<Vector2> shapes = new List<Vector2>(QuadBudget * 4);
    private readonly List<Color> colors = new List<Color>(QuadBudget * 4);
    private readonly List<int> triangles = new List<int>(QuadBudget * 6);
    private Spark[] sparks;
    private Mesh mesh;
    private Material material;
    private Vector3 axis, normal;
    private Color accent, rim;
    private float size, floor, duration, power;
    private bool clash, compact;
    private uint randomState;

    private struct Spark
    {
        public Vector3 origin, velocity;
        public float delay, life, width, length, spin;
        public Shape shape;
        public Color color;
    }

    public float Elapsed { get; private set; }
    public bool IsComplete => Elapsed >= duration;
    public int SparkCount => sparks != null ? sparks.Length : 0;
    public Bounds VisualBounds => GetComponent<MeshRenderer>().bounds;
    public bool HasVisibleGeometry => vertices.Count > 0;

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
        duration = clash ? 0.64f : compact ? 0.43f : 0.56f;
        floor = Mathf.Min(-size * 0.12f, groundY - transform.position.y);
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
        var renderer = gameObject.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.sortingLayerID = sortingLayer;
        renderer.sortingOrder = sortingOrder;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;

        randomState = unchecked((uint)(846217 + Mathf.RoundToInt(transform.position.x * 217) + Mathf.RoundToInt(power * 701)));
        if (randomState == 0)
            randomState = 1;
        BuildSparks();
        Advance(0);
    }

    private void BuildSparks()
    {
        int count = Mathf.RoundToInt(Mathf.Lerp(clash ? 42 : 28, clash ? 76 : 62, power));
        sparks = new Spark[compact ? count / 2 : count];
        for (int i = 0; i < sparks.Length; i++)
        {
            bool shard = i % 4 == 1;
            bool ember = i % 4 == 3;
            float side = clash && i % 2 == 0 ? -1 : 1;
            Vector3 heading = (axis * side * Range(0.55f, 1.4f) + normal * Range(-0.92f, 1.12f)).normalized;
            float speed = ember ? Range(1.2f, 2.8f) : Range(3.4f, 7.2f);
            float delay = ember ? Range(0.065f, 0.17f) : Range(0, 0.032f);
            sparks[i] = new Spark
            {
                origin = (axis * Range(-0.13f, 0.13f) + normal * Range(-0.18f, 0.18f)) * size,
                velocity = heading * speed * size,
                delay = delay,
                life = Mathf.Min(duration - delay, ember ? Range(0.23f, 0.46f) : Range(0.14f, 0.34f)),
                width = size * (shard ? Range(0.018f, 0.042f) : Range(0.008f, 0.022f)),
                length = size * (ember ? Range(0.017f, 0.045f) : Range(0.045f, 0.16f)),
                spin = Range(-9, 9),
                shape = shard ? Shape.Shard : Shape.Streak,
                color = i % 3 == 0 ? rim : accent
            };
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
        Elapsed = Mathf.Min(duration, Elapsed + Mathf.Max(0, delta));
        vertices.Clear();
        uv.Clear();
        shapes.Clear();
        colors.Clear();
        triangles.Clear();
        if (!IsComplete)
        {
            DrawContact();
            foreach (Spark spark in sparks)
                DrawSpark(spark);
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
        float flash = Fade(0, clash ? 0.115f : 0.09f);
        float reach = EaseOut(Elapsed / 0.16f);
        Vector3 center = clash ? Vector3.zero : axis * size * (0.06f + reach * 0.12f);
        Quad(center, axis * size * (0.44f + reach * 0.29f), normal * size * (0.34f + reach * 0.12f),
            Tint(accent, flash * 0.78f), Shape.Halo);
        Quad(center, axis * size * 0.24f, normal * size * 0.23f, Tint(rim, flash), Shape.Halo);

        if (clash)
        {
            Vector3 diagonal = (axis + normal * 0.65f).normalized;
            Vector3 cross = (axis - normal * 0.94f).normalized;
            Blade(center, diagonal, size * (0.65f + reach * 0.36f), size * 0.071f, Tint(rim, flash));
            Blade(center, cross, size * (0.56f + reach * 0.22f), size * 0.057f, Tint(accent, flash * 0.9f));
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
            float sweep = Fade(0.018f, compact ? 0.16f : 0.25f);
            float radius = size * (0.23f + 0.42f * reach);
            Arc(center + axis * size * reach * 0.28f, axis * radius * 0.60f, normal * radius,
                -1.08f, 1.28f, size * 0.105f, Tint(accent, sweep));
            if (!compact)
                Arc(center + axis * size * reach * 0.65f, axis * radius * 0.43f, normal * radius * 0.77f,
                    -0.91f, 0.96f, size * 0.057f, Tint(rim, Fade(0.045f, 0.26f) * 0.84f));
        }
    }

    private void DrawSpark(Spark spark)
    {
        float time = Elapsed - spark.delay;
        if (time < 0 || time >= spark.life)
            return;
        float progress = time / spark.life;
        float opacity = Mathf.Min(1, time * 115 + 0.24f) * Mathf.Pow(1 - progress, 1.4f);
        Vector3 point = SparkPosition(spark, time);
        Vector3 previous = SparkPosition(spark, Mathf.Max(0, time - 0.028f));
        Vector3 along = point - previous;
        if (along.sqrMagnitude < 0.000001f)
            along = spark.velocity;
        along = along.normalized;
        if (spark.shape == Shape.Shard)
            along = Quaternion.Euler(0, 0, spark.spin * time * 40) * along;
        Vector3 across = new Vector3(-along.y, along.x, 0);
        Color color = Color.Lerp(spark.color, rim, 0.70f * (1 - progress));
        if (spark.shape == Shape.Streak && along.sqrMagnitude > 0)
        {
            Vector3 tail = point - along * spark.length * (1 + (1 - progress) * 2.7f);
            Vector3 span = (point - tail) * 0.5f;
            Quad(tail + span, span, across * spark.width * 0.68f, Tint(color, opacity * 0.72f), Shape.Streak);
        }
        Quad(point, along * spark.length * Mathf.Lerp(0.85f, 0.26f, progress), across * spark.width,
            Tint(color, opacity), spark.shape);
    }

    private Vector3 SparkPosition(Spark spark, float time)
    {
        Vector3 point = spark.origin + spark.velocity * ((1 - Mathf.Exp(-time * 4.8f)) / 4.8f);
        point.y -= size * 4.1f * time * time;
        if (point.y < floor)
            point.y = floor + (floor - point.y) * 0.23f * (1 - time / spark.life);
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
