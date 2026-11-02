using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Builds a shallow stone terrace without changing combat positions or colliders.</summary>
[DisallowMultipleComponent]
public sealed class BattleForestStageController : MonoBehaviour
{
    // Shared with the authoring tool's shaft bases, in x + 0.42*z projected ground coordinates.
    public static readonly Vector2 CanopyOpenings = new Vector2(-9f, 5.5f);
    [SerializeField] private MeshRenderer originalGround;
    [SerializeField] private Material stoneMaterial;
    [SerializeField, Range(0f, 1f)] private float forestLighting = 0.92f;

    private static readonly int LightingId = Shader.PropertyToID("_DiceLandForestLighting");
    private static readonly int OpeningsId = Shader.PropertyToID("_DiceLandForestOpenings");
    private readonly List<Mesh> meshes = new List<Mesh>();
    private GameObject stageRoot;
    private bool originalGroundEnabled;
    private float previousLighting;
    private Vector4 previousOpenings;
    private bool initialized;

    public void Configure(MeshRenderer ground, Material stone)
    {
        originalGround = ground;
        stoneMaterial = stone;
    }

    private void OnEnable()
    {
        InitializeStage();
    }

    public void InitializeStage()
    {
        if (originalGround == null || stoneMaterial == null || initialized)
            return;
        initialized = true;
        previousLighting = Shader.GetGlobalFloat(LightingId);
        previousOpenings = Shader.GetGlobalVector(OpeningsId);
        Shader.SetGlobalFloat(LightingId, forestLighting);
        Shader.SetGlobalVector(OpeningsId, new Vector4(CanopyOpenings.x, CanopyOpenings.y, 0f, 0f));
        originalGroundEnabled = originalGround.enabled;
        originalGround.enabled = false;
        stageRoot = new GameObject("Runtime forest terrace");
        stageRoot.transform.SetParent(transform, false);

        // The battle lane stays at the existing floor height. Thickness extends downward.
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        AddHorizontal(vertices, triangles, -36f, 36f, -7f, 8.5f, 0.045f);
        AddFront(vertices, triangles, -36f, 36f, -7f, -0.62f, 0.045f);
        AddHorizontal(vertices, triangles, -36.8f, 36.8f, -7.8f, -7f, -0.24f);
        AddFront(vertices, triangles, -36.8f, 36.8f, -7.8f, -0.78f, -0.24f);
        AddHorizontal(vertices, triangles, -37.4f, 37.4f, -8.6f, -7.8f, -0.53f);
        AddFront(vertices, triangles, -37.4f, 37.4f, -8.6f, -0.9f, -0.53f);
        CreateMesh("Weathered battle terrace and steps", vertices, triangles, stoneMaterial);

        vertices = new List<Vector3>();
        triangles = new List<int>();
        AddHorizontal(vertices, triangles, -90f, 90f, -100f, -8.6f, -0.92f);
        // Meet the terrace at exactly the same height, then settle gently into the woodland.
        // The shared world-space material blends across this edge without a geometric crack.
        AddQuad(vertices, triangles, new Vector3(-90f, 0.045f, 8.5f), new Vector3(-90f, -0.08f, 15f),
            new Vector3(90f, -0.08f, 15f), new Vector3(90f, 0.045f, 8.5f));
        AddHorizontal(vertices, triangles, -90f, 90f, 15f, 55f, -0.08f);
        AddHorizontal(vertices, triangles, -90f, -36f, -8.6f, 8.5f, -0.92f);
        AddHorizontal(vertices, triangles, 36f, 90f, -8.6f, 8.5f, -0.92f);
        CreateMesh("Lower forest floor", vertices, triangles, originalGround.sharedMaterial);
    }

    private void CreateMesh(string name, List<Vector3> vertices, List<int> triangles, Material material)
    {
        Mesh mesh = new Mesh { name = name };
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        meshes.Add(mesh);
        GameObject part = new GameObject(name);
        part.transform.SetParent(stageRoot.transform, false);
        part.AddComponent<MeshFilter>().sharedMesh = mesh;
        MeshRenderer renderer = part.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = true;
    }

    private static void AddHorizontal(List<Vector3> v, List<int> t, float x0, float x1, float z0, float z1, float height)
    {
        AddQuad(v, t, new Vector3(x0, height, z0), new Vector3(x0, height, z1),
            new Vector3(x1, height, z1), new Vector3(x1, height, z0));
    }

    private static void AddFront(List<Vector3> v, List<int> t, float x0, float x1, float z, float bottom, float top)
    {
        AddQuad(v, t, new Vector3(x0, bottom, z), new Vector3(x0, top, z),
            new Vector3(x1, top, z), new Vector3(x1, bottom, z));
    }

    private static void AddQuad(List<Vector3> v, List<int> t, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
    {
        int i = v.Count;
        v.AddRange(new[] { a, b, c, d });
        t.AddRange(new[] { i, i + 1, i + 2, i, i + 2, i + 3 });
    }

    private void OnDisable()
    {
        ReleaseStage();
    }

    public void ReleaseStage()
    {
        if (!initialized)
            return;
        if (originalGround != null)
            originalGround.enabled = originalGroundEnabled;
        Shader.SetGlobalFloat(LightingId, previousLighting);
        Shader.SetGlobalVector(OpeningsId, previousOpenings);
        if (stageRoot != null)
        {
            stageRoot.SetActive(false);
            Release(stageRoot);
        }
        foreach (Mesh mesh in meshes)
            Release(mesh);
        meshes.Clear();
        stageRoot = null;
        initialized = false;
    }

    private static void Release(Object value)
    {
        if (Application.isPlaying) Destroy(value);
        else DestroyImmediate(value);
    }
}
