using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Extends only the distant painting, keeping its authored scale and world-space landmarks.</summary>
[DisallowMultipleComponent]
public sealed class BattleBackdropCoverageController : MonoBehaviour
{
    [SerializeField] private MeshFilter backdrop;
    [SerializeField] private Camera overviewCamera;
    [SerializeField] private Camera closeupCamera;
    [SerializeField, Range(0f, 0.2f)] private float viewportOverscan = 0.06f;

    private Mesh originalMesh;
    private Mesh coverageMesh;
    private Bounds authoredBounds;
    private readonly Vector3[] vertices = new Vector3[4];
    private readonly Vector2[] uv = new Vector2[4];
    private Rect previousRect;
    private bool hasPreviousRect;

    public void Configure(MeshFilter distantPainting, Camera overview, Camera closeup)
    {
        backdrop = distantPainting;
        overviewCamera = overview;
        closeupCamera = closeup;
    }

    private void OnEnable() => InitializeCoverage();

    public void InitializeCoverage()
    {
        if (coverageMesh != null || backdrop == null || backdrop.sharedMesh == null)
            return;
        originalMesh = backdrop.sharedMesh;
        authoredBounds = originalMesh.bounds;
        coverageMesh = new Mesh { name = "Runtime forest backdrop coverage" };
        coverageMesh.MarkDynamic();
        backdrop.sharedMesh = coverageMesh;
        UpdateMesh(Rect.MinMaxRect(authoredBounds.min.x, authoredBounds.min.y,
            authoredBounds.max.x, authoredBounds.max.y));
        RenderPipelineManager.beginCameraRendering += BeforeCameraRendering;
    }

    private void BeforeCameraRendering(ScriptableRenderContext context, Camera camera)
    {
        if (camera == overviewCamera || camera == closeupCamera)
            PrepareCamera(camera);
    }

    public void PrepareCamera(Camera camera)
    {
        if (camera == null || coverageMesh == null || backdrop == null)
            return;
        Transform planeTransform = backdrop.transform;
        Plane plane = new Plane(planeTransform.forward, planeTransform.TransformPoint(authoredBounds.center));
        Vector2 min = authoredBounds.min;
        Vector2 max = authoredBounds.max;
        for (int corner = 0; corner < 4; corner++)
        {
            Vector3 viewport = new Vector3((corner & 1) == 0 ? -viewportOverscan : 1f + viewportOverscan,
                (corner & 2) == 0 ? -viewportOverscan : 1f + viewportOverscan, 0f);
            Ray ray = camera.ViewportPointToRay(viewport);
            if (!plane.Raycast(ray, out float distance))
                return; // A camera facing away from this scenery must not create an unbounded mesh.
            Vector2 point = planeTransform.InverseTransformPoint(ray.GetPoint(distance));
            min = Vector2.Min(min, point);
            max = Vector2.Max(max, point);
        }
        Rect rect = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        if (!hasPreviousRect || rect != previousRect)
            UpdateMesh(rect);
    }

    private void UpdateMesh(Rect rect)
    {
        vertices[0] = new Vector3(rect.xMin, rect.yMin, authoredBounds.center.z);
        vertices[1] = new Vector3(rect.xMin, rect.yMax, authoredBounds.center.z);
        vertices[2] = new Vector3(rect.xMax, rect.yMax, authoredBounds.center.z);
        vertices[3] = new Vector3(rect.xMax, rect.yMin, authoredBounds.center.z);
        for (int i = 0; i < vertices.Length; i++)
            uv[i] = new Vector2((vertices[i].x - authoredBounds.min.x) / authoredBounds.size.x,
                (vertices[i].y - authoredBounds.min.y) / authoredBounds.size.y);
        coverageMesh.vertices = vertices;
        coverageMesh.uv = uv;
        if (!hasPreviousRect)
            coverageMesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
        coverageMesh.RecalculateBounds();
        previousRect = rect;
        hasPreviousRect = true;
    }

    private void OnDisable() => ReleaseCoverage();

    public void ReleaseCoverage()
    {
        RenderPipelineManager.beginCameraRendering -= BeforeCameraRendering;
        if (backdrop != null && backdrop.sharedMesh == coverageMesh)
            backdrop.sharedMesh = originalMesh;
        if (coverageMesh != null)
        {
            if (Application.isPlaying)
                Destroy(coverageMesh);
            else
                DestroyImmediate(coverageMesh);
        }
        coverageMesh = null;
        originalMesh = null;
        hasPreviousRect = false;
    }
}
