using NUnit.Framework;
using UnityEngine;

public sealed class BattleBackdropCoverageTests
{
    [TestCase(1.333333f, -15f, 28f, false)]
    [TestCase(2.171946f, -12f, 65f, false)]
    [TestCase(2.444444f, 15f, 85f, false)]
    [TestCase(1.777778f, -18f, 45f, true)]
    public void OffCentreWideShotsCoverEveryViewportEdgeWithoutMovingThePainting(
        float aspect, float focusX, float fieldOfView, bool orthographic)
    {
        GameObject root = new GameObject("Backdrop coverage regression");
        GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        GameObject cameraObject = new GameObject("Probe camera");
        quad.transform.SetParent(root.transform);
        cameraObject.transform.SetParent(root.transform);
        quad.transform.SetPositionAndRotation(new Vector3(0f, 30f, 60f), Quaternion.Euler(7f, 0f, 0f));
        quad.transform.localScale = new Vector3(124f, 69.75f, 1f);
        MeshFilter filter = quad.GetComponent<MeshFilter>();
        Mesh original = filter.sharedMesh;
        Vector3 originalPosition = quad.transform.position;
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.transform.SetPositionAndRotation(new Vector3(focusX, 6f, -10f), Quaternion.Euler(12f, 0f, 0f));
        camera.aspect = aspect;
        camera.fieldOfView = fieldOfView;
        camera.orthographic = orthographic;
        camera.orthographicSize = 28f;
        camera.farClipPlane = 200f;
        BattleBackdropCoverageController coverage = root.AddComponent<BattleBackdropCoverageController>();
        coverage.Configure(filter, camera, null);
        try
        {
            coverage.InitializeCoverage();
            coverage.PrepareCamera(camera);
            Bounds expanded = filter.sharedMesh.bounds;
            Plane plane = new Plane(quad.transform.forward, quad.transform.position);
            foreach (float x in new[] { 0f, 0.5f, 1f })
            foreach (float y in new[] { 0f, 0.5f, 1f })
            {
                Ray ray = camera.ViewportPointToRay(new Vector3(x, y, 0f));
                Assert.IsTrue(plane.Raycast(ray, out float distance));
                Vector3 hit = quad.transform.InverseTransformPoint(ray.GetPoint(distance));
                Assert.That(hit.x, Is.InRange(expanded.min.x, expanded.max.x));
                Assert.That(hit.y, Is.InRange(expanded.min.y, expanded.max.y));
                Assert.Less(camera.WorldToViewportPoint(ray.GetPoint(distance)).z, camera.farClipPlane);
            }
            // UVs remain anchored to the original quad: expansion must not magnify or slide landmarks.
            Vector3[] vertices = filter.sharedMesh.vertices;
            Vector2[] uv = filter.sharedMesh.uv;
            for (int i = 0; i < vertices.Length; i++)
                Assert.That(uv[i], Is.EqualTo(new Vector2(vertices[i].x + 0.5f, vertices[i].y + 0.5f)));
            Assert.AreEqual(originalPosition, quad.transform.position);
            camera.transform.position += new Vector3(24f, 4f, -10f);
            coverage.PrepareCamera(camera);
            coverage.ReleaseCoverage();
            Assert.AreSame(original, filter.sharedMesh);
            coverage.InitializeCoverage();
            coverage.PrepareCamera(camera);
            coverage.ReleaseCoverage();
            Assert.AreSame(original, filter.sharedMesh);
        }
        finally
        {
            coverage.ReleaseCoverage();
            Object.DestroyImmediate(root);
        }
    }
}
